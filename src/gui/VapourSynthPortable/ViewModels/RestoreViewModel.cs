using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class RestoreViewModel : ObservableObject, IDisposable, IProjectPersistable
{
    private static readonly ILogger<RestoreViewModel> _logger = LoggingService.GetLogger<RestoreViewModel>();
    private readonly IMediaPoolService _mediaPool;
    private readonly IVapourSynthService _vapourSynthService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly QuickPreviewService _quickPreviewService;
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

    public RestoreViewModel(
        IMediaPoolService mediaPool,
        IVapourSynthService vapourSynthService,
        ISettingsService settingsService,
        INavigationService navigationService)
    {
        _mediaPool = mediaPool;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        // Initialize VapourSynth service (for preview generation)
        _vapourSynthService = vapourSynthService;

        // Initialize settings service
        _settingsService = settingsService;

        // Initialize navigation service
        _navigationService = navigationService;

        // Initialize quick preview service
        _quickPreviewService = new QuickPreviewService();

        LoadPresets();
        LoadFavorites();
        DetectGpu();

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = FindProjectRoot(basePath) ?? basePath;
        distPath = Path.Combine(distPath, "dist");
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pythonPath = Path.Combine(distPath, "python", "python.exe");
    }

    // Parameterless constructor for XAML design-time support
    public RestoreViewModel() : this(
        GetServiceWithFallback<IMediaPoolService>(() => new MediaPoolService(new PathResolver())),
        GetServiceWithFallback<IVapourSynthService>(() => new VapourSynthService()),
        GetServiceWithFallback<ISettingsService>(() => new SettingsService()),
        GetServiceWithFallback<INavigationService>(() => new NavigationService()))
    {
    }

    private static T GetServiceWithFallback<T>(Func<T> fallbackFactory) where T : class
    {
        var service = App.Services?.GetService(typeof(T)) as T;
        if (service == null)
        {
            _logger.LogWarning("{ServiceType} not available from DI, using fallback instance", typeof(T).Name);
            return fallbackFactory();
        }
        return service;
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

            // Show restoration status if applied
            if (item.HasRestoration)
            {
                StatusText = $"Source: {item.Name} (Restoration: {item.AppliedRestoration!.PresetName})";
            }
            else
            {
                StatusText = $"Source: {item.Name}";
            }
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

    private void LoadFavorites()
    {
        try
        {
            var settings = _settingsService.Load();
            var favoriteNames = new HashSet<string>(settings.FavoritePresets);

            foreach (var preset in Presets)
            {
                preset.IsFavorite = favoriteNames.Contains(preset.Name);
            }

            _logger.LogDebug("Loaded {Count} favorite presets", favoriteNames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load favorite presets");
        }
    }

    private void SaveFavorites()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.FavoritePresets = Presets
                .Where(p => p.IsFavorite)
                .Select(p => p.Name)
                .ToList();

            _settingsService.Save(settings);
            _logger.LogDebug("Saved {Count} favorite presets", settings.FavoritePresets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save favorite presets");
        }
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

        // Persist favorites to settings
        SaveFavorites();
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GPU detection failed, falling back to software rendering");
            GpuAvailable = false;
            GpuName = "Unknown";
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load source info for {Path}", path);
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
    private void ApplyPreset(RestorePreset preset)
    {
        SelectedPreset = preset;
        StatusText = $"Selected: {preset.Name}";

        if (preset.RequiresGpu && !GpuAvailable)
        {
            StatusText = $"Warning: {preset.Name} requires GPU but none detected";
        }
    }

    /// <summary>
    /// Applies the current restoration preset to the source media item.
    /// The restoration settings are stored on the MediaItem for use by Export page.
    /// </summary>
    [RelayCommand]
    private void ApplyToSource()
    {
        if (!HasSource || SelectedPreset == null) return;

        var source = _mediaPool.CurrentSource;
        if (source == null) return;

        // Capture current parameter values
        var parameters = SelectedPreset.Parameters
            .Select(p => new ParameterSnapshot
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Type = p.Type,
                Value = p.CurrentValue ?? p.DefaultValue
            })
            .ToList();

        // Create restoration settings
        source.AppliedRestoration = new RestorationSettings
        {
            PresetName = SelectedPreset.Name,
            PresetDescription = SelectedPreset.Description,
            TaskType = SelectedPreset.TaskType,
            GeneratedScript = SelectedPreset.GenerateScript(),
            Parameters = parameters,
            IsEnabled = true,
            AppliedAt = DateTime.Now,
            RequiresGpu = SelectedPreset.RequiresGpu,
            AiModel = SelectedPreset.AiModel
        };

        StatusText = $"Applied: {SelectedPreset.Name}";
        ToastService.Instance.ShowSuccess($"Applied {SelectedPreset.Name} to {source.Name}");
    }

    /// <summary>
    /// Applies the current restoration preset and navigates to Export page.
    /// </summary>
    [RelayCommand]
    private void SendToExport()
    {
        if (!HasSource || SelectedPreset == null) return;

        // Apply restoration first
        ApplyToSource();

        // Navigate to Export page
        _navigationService.NavigateTo(PageType.Export);
    }

    /// <summary>
    /// Clears the restoration settings from the current source.
    /// </summary>
    [RelayCommand]
    private void ClearRestoration()
    {
        var source = _mediaPool.CurrentSource;
        if (source == null) return;

        source.AppliedRestoration = null;
        StatusText = $"Cleared restoration from {source.Name}";
        ToastService.Instance.ShowInfo($"Cleared restoration from {source.Name}");
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
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
        _cancellationTokenSource?.Dispose();
    }
}
