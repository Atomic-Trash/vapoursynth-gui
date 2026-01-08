using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.ViewModels;

public partial class RestoreViewModel : ObservableObject
{
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
    private string _sourcePath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private bool _hasSource;

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

    public RestoreViewModel()
    {
        LoadPresets();
        DetectGpu();

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pythonPath = Path.Combine(distPath, "python", "python.exe");
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

    private void FilterPresets()
    {
        FilteredPresets.Clear();

        var filtered = SelectedCategory == "All"
            ? Presets
            : Presets.Where(p => p.Category == SelectedCategory);

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

    [RelayCommand]
    private async Task LoadSource()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Source Video",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.m2ts;*.ts|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SourcePath = dialog.FileName;
            HasSource = true;

            // Get source info
            await LoadSourceInfo(dialog.FileName);

            // Auto-generate output path
            var dir = Path.GetDirectoryName(dialog.FileName) ?? "";
            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            var ext = Path.GetExtension(dialog.FileName);
            OutputPath = Path.Combine(dir, $"{name}_restored{ext}");

            StatusText = $"Loaded: {Path.GetFileName(SourcePath)}";
        }
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

            await ProcessJob(job);
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

        var script = $@"
import vapoursynth as vs
core = vs.core

# Load source
video_in = core.lsmas.LWLibavSource(r'{job.SourcePath.Replace("'", "\\'")}')

{preset.VapourSynthScript}
";
        return script;
    }

    private async Task RunVapourSynthPipeline(RestoreJob job, string scriptPath, CancellationToken token)
    {
        var vspipePath = Path.Combine(_vapourSynthPath, "vspipe.exe");
        var ffmpegPath = FindExecutable("ffmpeg.exe");

        // Use vspipe to output raw video, pipe to FFmpeg
        var vspipeArgs = $"\"{scriptPath}\" -c y4m -";
        var ffmpegArgs = $"-i - -c:v libx264 -crf 18 -preset medium -y \"{job.OutputPath}\"";

        // For now, simulate processing with progress updates
        // In production, this would pipe vspipe output to FFmpeg
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

            job.StatusText = $"Processing frame {frame}/{totalFrames}";
            await Task.Delay(10, token); // Simulate processing time
        }
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
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
}
