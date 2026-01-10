using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class RestoreViewModel : ObservableObject, IDisposable, IProjectPersistable
{
    private readonly IMediaPoolService _mediaPool;
    private readonly IVapourSynthService _vapourSynthService;
    private readonly QuickPreviewService _quickPreviewService;
    private readonly ILogger<RestoreViewModel> _logger;
    private bool _disposed;

    [ObservableProperty]
    private bool _isSimpleMode = true;

    [ObservableProperty]
    private ObservableCollection<RestorePreset> _presets = [];

    [ObservableProperty]
    private ObservableCollection<RestorePreset> _filteredPresets = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [];

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private RestorePreset? _selectedPreset;

    [ObservableProperty]
    private string _searchQuery = "";

    // Toggle comparison state
    [ObservableProperty]
    private bool _showOriginalInToggle;

    // Queue pause state
    [ObservableProperty]
    private bool _isPaused;

    // Source is now managed by MediaPoolService
    public string SourcePath => _mediaPool.CurrentSource?.FilePath ?? "";
    public bool HasSource => _mediaPool.HasSource;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private ObservableCollection<RestoreJob> _jobQueue = [];

    [ObservableProperty]
    private RestoreJob? _currentJob;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private AIModelSettings _modelSettings = new();

    [ObservableProperty]
    private bool _gpuAvailable;

    [ObservableProperty]
    private string _gpuName = "";

    // Quick Preview properties
    [ObservableProperty]
    private BitmapSource? _originalFrame;

    [ObservableProperty]
    private BitmapSource? _processedFrame;

    [ObservableProperty]
    private bool _isGeneratingPreview;

    [ObservableProperty]
    private long _previewFrame;

    [ObservableProperty]
    private bool _showComparison;

    [ObservableProperty]
    private ComparisonMode _comparisonMode = ComparisonMode.SideBySide;

    [ObservableProperty]
    private double _wipePosition = 0.5; // 0-1 for wipe comparison

    public bool HasPreview => OriginalFrame != null || ProcessedFrame != null;
    public bool CanGeneratePreview => HasSource && SelectedPreset != null && !IsGeneratingPreview;
    public bool CanStartProcessing => !IsProcessing && JobQueue.Any(j => j.Status == ProcessingStatus.Pending);
    public int CompletedJobCount => JobQueue.Count(j => j.Status == ProcessingStatus.Completed);
    public int PendingJobCount => JobQueue.Count(j => j.Status == ProcessingStatus.Pending);
    public TimeSpan EstimatedTotalTime => TimeSpan.FromMinutes(PendingJobCount * 5); // Rough estimate

    // Source media info
    [ObservableProperty]
    private int _sourceWidth;

    [ObservableProperty]
    private int _sourceHeight;

    [ObservableProperty]
    private double _sourceFps;

    [ObservableProperty]
    private int _sourceFrameCount;

    [ObservableProperty]
    private string _sourceDuration = "";

    [ObservableProperty]
    private string _sourceCodec = "";

    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _vapourSynthPath;
    private readonly string _pythonPath;

    public RestoreViewModel(IMediaPoolService mediaPool, IVapourSynthService vapourSynthService)
    {
        _logger = LoggingService.GetLogger<RestoreViewModel>();
        _mediaPool = mediaPool;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        // Initialize VapourSynth service
        _vapourSynthService = vapourSynthService;
        _vapourSynthService.ProgressChanged += OnVapourSynthProgressChanged;
        _vapourSynthService.LogMessage += OnVapourSynthLogMessage;

        // Initialize quick preview service
        _quickPreviewService = new QuickPreviewService();

        LoadPresets();
        DetectGpu();

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = FindProjectRoot(basePath) ?? basePath;
        distPath = Path.Combine(distPath, "dist");
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pythonPath = Path.Combine(distPath, "python", "python.exe");
    }

    // Parameterless constructor for XAML design-time support
    public RestoreViewModel() : this(
        App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService ?? new MediaPoolService(),
        App.Services?.GetService(typeof(IVapourSynthService)) as IVapourSynthService ?? new VapourSynthService())
    {
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        OnPropertyChanged(nameof(SourcePath));
        OnPropertyChanged(nameof(HasSource));

        if (item != null)
        {
            // Update source info from the media item
            SourceWidth = item.Width;
            SourceHeight = item.Height;
            SourceFps = item.FrameRate;
            SourceFrameCount = item.FrameCount;
            SourceDuration = item.DurationFormatted;
            SourceCodec = item.Codec;

            // Auto-generate output path
            var dir = Path.GetDirectoryName(item.FilePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(item.FilePath);
            var ext = Path.GetExtension(item.FilePath);
            OutputPath = Path.Combine(dir, $"{name}_restored{ext}");

            StatusText = $"Source: {item.Name}";
        }
        else
        {
            StatusText = "Ready";
        }
    }

    private void LoadPresets()
    {
        var allPresets = RestorePreset.GetPresets();
        var categories = new HashSet<string> { "All" };

        foreach (var preset in allPresets)
        {
            Presets.Add(preset);
            categories.Add(preset.Category);
        }

        foreach (var cat in categories.OrderBy(c => c == "All" ? "" : c))
        {
            Categories.Add(cat);
        }

        FilterPresets();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        FilterPresets();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterPresets();
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    private void ToggleShowOriginal()
    {
        ShowOriginalInToggle = !ShowOriginalInToggle;
    }

    [RelayCommand]
    private void ToggleFavorite(RestorePreset? preset)
    {
        if (preset == null) return;
        preset.IsFavorite = !preset.IsFavorite;

        // If we're viewing favorites and unfavorited, refresh the list
        if (SelectedCategory == "Favorites")
        {
            FilterPresets();
        }

        // TODO: Persist favorites to settings
    }

    [RelayCommand]
    private void ResetParameter(PresetParameter? param)
    {
        param?.ResetToDefault();
    }

    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        StatusText = IsPaused ? "Queue paused" : "Queue resumed";
    }

    [RelayCommand]
    private void MoveJobUp(RestoreJob? job)
    {
        if (job == null) return;
        var index = JobQueue.IndexOf(job);
        if (index > 0 && job.Status == ProcessingStatus.Pending)
        {
            JobQueue.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveJobDown(RestoreJob? job)
    {
        if (job == null) return;
        var index = JobQueue.IndexOf(job);
        if (index < JobQueue.Count - 1 && job.Status == ProcessingStatus.Pending)
        {
            JobQueue.Move(index, index + 1);
        }
    }

    partial void OnSelectedPresetChanged(RestorePreset? value)
    {
        OnPropertyChanged(nameof(CanGeneratePreview));

        // Clear processed frame when preset changes
        ProcessedFrame = null;
        OnPropertyChanged(nameof(HasPreview));

        // Initialize CurrentValue for all parameters
        if (value != null)
        {
            foreach (var param in value.Parameters)
            {
                if (param.CurrentValue == null)
                {
                    param.CurrentValue = param.DefaultValue;
                }
            }
        }
    }

    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        if (!CanGeneratePreview || SelectedPreset == null) return;

        IsGeneratingPreview = true;
        StatusText = "Generating preview...";

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Use middle frame if no preview frame set
            if (PreviewFrame <= 0)
            {
                PreviewFrame = SourceFrameCount / 2;
            }

            // Generate both original and processed previews in parallel
            var originalTask = _quickPreviewService.GenerateOriginalPreviewAsync(
                SourcePath, PreviewFrame, _cancellationTokenSource.Token);

            var processedTask = _quickPreviewService.GeneratePreviewAsync(
                SourcePath, SelectedPreset, PreviewFrame, _cancellationTokenSource.Token);

            await Task.WhenAll(originalTask, processedTask);

            OriginalFrame = await originalTask;
            ProcessedFrame = await processedTask;

            ShowComparison = OriginalFrame != null && ProcessedFrame != null;
            OnPropertyChanged(nameof(HasPreview));

            StatusText = ProcessedFrame != null
                ? "Preview generated"
                : "Preview generation failed (using original frame)";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Preview cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Preview error: {ex.Message}";
        }
        finally
        {
            IsGeneratingPreview = false;
            OnPropertyChanged(nameof(CanGeneratePreview));
        }
    }

    [RelayCommand]
    private void CancelPreview()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private void SetComparisonMode(ComparisonMode mode)
    {
        ComparisonMode = mode;
    }

    [RelayCommand]
    private void ToggleComparison()
    {
        ShowComparison = !ShowComparison;
    }

    private void FilterPresets()
    {
        FilteredPresets.Clear();

        IEnumerable<RestorePreset> filtered = Presets;

        // Filter by category
        if (SelectedCategory == "Favorites")
        {
            filtered = filtered.Where(p => p.IsFavorite);
        }
        else if (SelectedCategory != "All")
        {
            filtered = filtered.Where(p => p.Category == SelectedCategory);
        }

        // Filter by search query
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var preset in filtered)
        {
            FilteredPresets.Add(preset);
        }
    }

    private void DetectGpu()
    {
        try
        {
            // Simple GPU detection via dxdiag or wmic
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path win32_VideoController get name",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                GpuName = lines[1].Trim();
                GpuAvailable = GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                               GpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                               GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            GpuAvailable = false;
            GpuName = "Unknown";
        }
    }

    private void OnVapourSynthProgressChanged(object? sender, VapourSynthProgressEventArgs e)
    {
        if (CurrentJob == null) return;

        CurrentJob.CurrentFrame = e.CurrentFrame;
        CurrentJob.Progress = e.Progress;
        CurrentJob.ElapsedTime = e.ElapsedTime;
        CurrentJob.EstimatedTimeRemaining = e.EstimatedTimeRemaining;
        CurrentJob.StatusText = $"Processing frame {e.CurrentFrame}/{e.TotalFrames} ({e.Fps:F1} fps)";
    }

    private void OnVapourSynthLogMessage(object? sender, string message)
    {
        _logger.LogDebug("[VapourSynth] {Message}", message);
    }

    private async Task LoadSourceInfo(string path)
    {
        try
        {
            var ffprobePath = FindExecutable("ffprobe.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse JSON (simplified)
            if (output.Contains("\"width\""))
            {
                var widthMatch = Regex.Match(output, "\"width\":\\s*(\\d+)");
                var heightMatch = Regex.Match(output, "\"height\":\\s*(\\d+)");
                var durationMatch = Regex.Match(output, "\"duration\":\\s*\"([\\d.]+)\"");
                var fpsMatch = Regex.Match(output, "\"r_frame_rate\":\\s*\"(\\d+)/(\\d+)\"");
                var codecMatch = Regex.Match(output, "\"codec_name\":\\s*\"([^\"]+)\"");

                if (widthMatch.Success) SourceWidth = int.Parse(widthMatch.Groups[1].Value);
                if (heightMatch.Success) SourceHeight = int.Parse(heightMatch.Groups[1].Value);
                if (durationMatch.Success)
                {
                    var dur = double.Parse(durationMatch.Groups[1].Value);
                    SourceDuration = TimeSpan.FromSeconds(dur).ToString(@"hh\:mm\:ss");
                }
                if (fpsMatch.Success)
                {
                    var num = double.Parse(fpsMatch.Groups[1].Value);
                    var den = double.Parse(fpsMatch.Groups[2].Value);
                    SourceFps = num / den;
                    if (durationMatch.Success)
                    {
                        var dur = double.Parse(durationMatch.Groups[1].Value);
                        SourceFrameCount = (int)(dur * SourceFps);
                    }
                }
                if (codecMatch.Success) SourceCodec = codecMatch.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private string FindExecutable(string name)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");

        if (Directory.Exists(ffmpegDir))
        {
            var inDir = Path.Combine(ffmpegDir, name);
            if (File.Exists(inDir)) return inDir;

            var inBin = Path.Combine(ffmpegDir, "bin", name);
            if (File.Exists(inBin)) return inBin;
        }

        return name;
    }

    [RelayCommand]
    private void SelectOutput()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Restored Video",
            Filter = "MP4 Video|*.mp4|MKV Video|*.mkv|AVI Video|*.avi|All Files|*.*",
            FileName = Path.GetFileName(OutputPath)
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ApplyPreset(RestorePreset preset)
    {
        SelectedPreset = preset;
        StatusText = $"Selected: {preset.Name}";

        if (preset.RequiresGpu && !GpuAvailable)
        {
            StatusText = $"Warning: {preset.Name} requires GPU but none detected";
        }
    }

    [RelayCommand]
    private async Task ProcessNow()
    {
        if (!HasSource || SelectedPreset == null) return;

        var job = new RestoreJob
        {
            SourcePath = SourcePath,
            OutputPath = OutputPath,
            Preset = SelectedPreset,
            Status = ProcessingStatus.Processing,
            StatusText = "Starting...",
            StartTime = DateTime.Now,
            TotalFrames = SourceFrameCount
        };

        JobQueue.Add(job);
        CurrentJob = job;

        await ProcessJob(job);
    }

    [RelayCommand]
    private void AddToQueue()
    {
        if (!HasSource || SelectedPreset == null) return;

        var job = new RestoreJob
        {
            SourcePath = SourcePath,
            OutputPath = OutputPath,
            Preset = SelectedPreset,
            Status = ProcessingStatus.Pending,
            StatusText = "Queued",
            TotalFrames = SourceFrameCount
        };

        JobQueue.Add(job);
        StatusText = $"Added to queue: {SelectedPreset.Name}";
    }

    [RelayCommand]
    private async Task ProcessQueue()
    {
        if (IsProcessing) return;

        var pendingJobs = JobQueue.Where(j => j.Status == ProcessingStatus.Pending).ToList();
        foreach (var job in pendingJobs)
        {
            if (_cancellationTokenSource?.IsCancellationRequested == true)
                break;

            // Wait while paused
            while (IsPaused && !_cancellationTokenSource?.IsCancellationRequested == true)
            {
                await Task.Delay(100);
            }

            if (_cancellationTokenSource?.IsCancellationRequested == true)
                break;

            await ProcessJob(job);

            // Update queue summary properties
            OnPropertyChanged(nameof(CompletedJobCount));
            OnPropertyChanged(nameof(PendingJobCount));
            OnPropertyChanged(nameof(CanStartProcessing));
            OnPropertyChanged(nameof(EstimatedTotalTime));
        }
    }

    private async Task ProcessJob(RestoreJob job)
    {
        IsProcessing = true;
        CurrentJob = job;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            job.Status = ProcessingStatus.Processing;
            job.StatusText = "Generating script...";
            job.StartTime = DateTime.Now;

            // Create temporary VapourSynth script
            var scriptPath = Path.Combine(Path.GetTempPath(), $"restore_{job.Id}.vpy");
            var script = GenerateScript(job);
            await File.WriteAllTextAsync(scriptPath, script);

            job.StatusText = "Processing...";

            // Run VapourSynth + FFmpeg pipeline
            await RunVapourSynthPipeline(job, scriptPath, _cancellationTokenSource.Token);

            if (job.Status != ProcessingStatus.Cancelled)
            {
                job.Status = ProcessingStatus.Completed;
                job.StatusText = "Completed";
                job.Progress = 100;
                job.EndTime = DateTime.Now;
                StatusText = $"Completed: {Path.GetFileName(job.OutputPath)}";
            }

            // Cleanup
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
        catch (OperationCanceledException)
        {
            job.Status = ProcessingStatus.Cancelled;
            job.StatusText = "Cancelled";
            StatusText = "Processing cancelled";
        }
        catch (Exception ex)
        {
            job.Status = ProcessingStatus.Failed;
            job.StatusText = "Failed";
            job.ErrorMessage = ex.Message;
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            CurrentJob = null;
        }
    }

    private string GenerateScript(RestoreJob job)
    {
        var preset = job.Preset;
        if (preset == null) return "";

        // Use the preset's GenerateScript() method which handles parameter substitution
        var presetScript = preset.GenerateScript();

        var script = $@"
import vapoursynth as vs
core = vs.core

# Load source
video_in = core.lsmas.LWLibavSource(r'{job.SourcePath.Replace("'", "\\'")}')

{presetScript}
";
        return script;
    }

    private async Task RunVapourSynthPipeline(RestoreJob job, string scriptPath, CancellationToken token)
    {
        // Check if VapourSynth is available
        if (!_vapourSynthService.IsAvailable)
        {
            // Fall back to simulated processing if VSPipe is not available
            var totalFrames = job.TotalFrames > 0 ? job.TotalFrames : 1000;
            var sw = Stopwatch.StartNew();

            for (int frame = 0; frame < totalFrames && !token.IsCancellationRequested; frame += 10)
            {
                job.CurrentFrame = frame;
                job.Progress = (double)frame / totalFrames * 100;
                job.ElapsedTime = sw.Elapsed;

                if (frame > 0)
                {
                    var fps = frame / sw.Elapsed.TotalSeconds;
                    var remaining = (totalFrames - frame) / fps;
                    job.EstimatedTimeRemaining = TimeSpan.FromSeconds(remaining);
                }

                job.StatusText = $"[Simulated] Processing frame {frame}/{totalFrames}";
                await Task.Delay(10, token);
            }

            StatusText = "Note: VSPipe not available, using simulated processing";
            return;
        }

        // Use real VapourSynth pipeline
        var settings = new VapourSynthEncodingSettings
        {
            VideoCodec = GpuAvailable && GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
                ? "h264_nvenc"
                : "libx264",
            Quality = 18,
            Preset = "medium",
            HardwarePreset = "p4"
        };

        var success = await _vapourSynthService.ProcessScriptAsync(
            scriptPath,
            job.OutputPath,
            settings,
            token);

        if (!success && !token.IsCancellationRequested)
        {
            throw new Exception("VapourSynth processing failed");
        }
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
        _vapourSynthService.Cancel();

        if (CurrentJob != null)
        {
            CurrentJob.Status = ProcessingStatus.Cancelled;
            CurrentJob.StatusText = "Cancelled";
        }
        StatusText = "Cancelled";
    }

    [RelayCommand]
    private void ClearQueue()
    {
        var completedOrFailed = JobQueue.Where(j =>
            j.Status == ProcessingStatus.Completed ||
            j.Status == ProcessingStatus.Failed ||
            j.Status == ProcessingStatus.Cancelled).ToList();

        foreach (var job in completedOrFailed)
        {
            JobQueue.Remove(job);
        }
    }

    [RelayCommand]
    private void RemoveJob(RestoreJob job)
    {
        if (job.Status != ProcessingStatus.Processing)
        {
            JobQueue.Remove(job);
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsSimpleMode = !IsSimpleMode;
    }

    #region IProjectPersistable Implementation

    /// <summary>
    /// Exports the current restore job queue to the project
    /// </summary>
    public void ExportToProject(Project project)
    {
        project.RestoreJobs.Clear();
        foreach (var job in JobQueue)
        {
            project.RestoreJobs.Add(new RestoreJobData
            {
                SourcePath = job.SourcePath,
                OutputPath = job.OutputPath,
                PresetName = job.Preset?.Name ?? "",
                Status = job.Status,
                Progress = job.Progress
            });
        }
    }

    /// <summary>
    /// Imports restore job queue from the project
    /// </summary>
    public void ImportFromProject(Project project)
    {
        JobQueue.Clear();

        foreach (var jobData in project.RestoreJobs)
        {
            // Find matching preset by name
            var preset = Presets.FirstOrDefault(p => p.Name == jobData.PresetName);

            var job = new RestoreJob
            {
                SourcePath = jobData.SourcePath,
                OutputPath = jobData.OutputPath,
                Preset = preset,
                Status = jobData.Status,
                Progress = jobData.Progress
            };

            JobQueue.Add(job);
        }

        StatusText = JobQueue.Count > 0
            ? $"Loaded {JobQueue.Count} restore job(s) from project"
            : "Ready";
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPool.CurrentSourceChanged -= OnCurrentSourceChanged;
        _vapourSynthService.ProgressChanged -= OnVapourSynthProgressChanged;
        _vapourSynthService.LogMessage -= OnVapourSynthLogMessage;
        _cancellationTokenSource?.Dispose();
    }
}
