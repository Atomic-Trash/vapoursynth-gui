using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class ExportViewModel : ObservableObject, IDisposable
{
    private static readonly ILogger<ExportViewModel> _logger = LoggingService.GetLogger<ExportViewModel>();
    private readonly IMediaPoolService _mediaPool;
    private readonly ISettingsService _settingsService;
    private readonly FFmpegService _ffmpegService = new();
    private readonly VapourSynthService _vapourSynthService = new();
    private readonly EffectService _effectService = EffectService.Instance;
    private readonly ExportPipelineService _pipelineService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;
    private bool _isLoading; // Prevent saving during load

    // Timeline reference for timeline export mode
    [ObservableProperty]
    private Timeline? _timeline;

    // Export mode selection
    [ObservableProperty]
    private ExportMode _exportMode = ExportMode.DirectEncode;

    [ObservableProperty]
    private ObservableCollection<ExportMode> _exportModes = [ExportMode.DirectEncode, ExportMode.TimelineWithEffects];

    /// <summary>
    /// Helper property for radio button binding - Direct Encode mode
    /// </summary>
    public bool IsDirectEncodeMode
    {
        get => ExportMode == ExportMode.DirectEncode;
        set
        {
            if (value && ExportMode != ExportMode.DirectEncode)
            {
                ExportMode = ExportMode.DirectEncode;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimelineWithEffectsMode));
            }
        }
    }

    /// <summary>
    /// Helper property for radio button binding - Timeline with Effects mode
    /// </summary>
    public bool IsTimelineWithEffectsMode
    {
        get => ExportMode == ExportMode.TimelineWithEffects;
        set
        {
            if (value && ExportMode != ExportMode.TimelineWithEffects)
            {
                ExportMode = ExportMode.TimelineWithEffects;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDirectEncodeMode));
            }
        }
    }

    // VapourSynth availability
    public bool IsVapourSynthAvailable => _vapourSynthService.IsAvailable;

    // === AUTO-DETECTION PROPERTIES ===
    // These properties automatically determine what will be exported based on project state

    /// <summary>
    /// Whether the timeline has clips to export
    /// </summary>
    public bool HasTimelineContent => Timeline?.HasClips == true;

    /// <summary>
    /// Count of clips on the timeline
    /// </summary>
    public int TimelineClipCount => Timeline?.Tracks.SelectMany(t => t.Clips).Count() ?? 0;

    /// <summary>
    /// Whether there's a source file available
    /// </summary>
    public bool HasSourceFile => !string.IsNullOrEmpty(InputPath) && System.IO.File.Exists(InputPath);

    /// <summary>
    /// Auto-detected source type description
    /// </summary>
    public string AutoDetectedSourceType
    {
        get
        {
            if (HasTimelineContent)
                return "Timeline";
            if (HasSourceFile)
                return "Source File";
            return "No Source";
        }
    }

    /// <summary>
    /// Auto-detected source name for display
    /// </summary>
    public string AutoDetectedSourceName
    {
        get
        {
            if (HasTimelineContent)
                return $"{TimelineClipCount} clip{(TimelineClipCount != 1 ? "s" : "")} on timeline";
            if (HasSourceFile)
                return System.IO.Path.GetFileName(InputPath);
            return "Select a source";
        }
    }

    /// <summary>
    /// Determines if processing through VapourSynth is needed
    /// </summary>
    public bool RequiresVapourSynth => HasTimelineContent || (HasRestoration && IncludeRestoration);

    /// <summary>
    /// Summary of what will be included in the export
    /// </summary>
    public string ExportPipelineSummary
    {
        get
        {
            var parts = new List<string>();

            if (HasTimelineContent)
            {
                parts.Add($"Timeline ({TimelineClipCount} clips)");
            }
            else if (HasSourceFile)
            {
                parts.Add(System.IO.Path.GetFileName(InputPath));
            }

            if (HasRestoration && IncludeRestoration)
            {
                parts.Add($"+ Restoration ({RestorationPresetName})");
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "No source selected";
        }
    }

    /// <summary>
    /// Whether export is ready (has valid source)
    /// </summary>
    public bool CanExport => HasTimelineContent || HasSourceFile;

    // InputPath now delegates to MediaPoolService, but can be overridden
    [ObservableProperty]
    private string _inputPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    // Convenience property for current source from media pool
    public bool HasCurrentSource => _mediaPool.HasSource;
    public string CurrentSourcePath => _mediaPool.CurrentSource?.FilePath ?? "";

    // Restoration awareness - from MediaItem
    public bool HasRestoration => _mediaPool.CurrentSource?.HasRestoration ?? false;
    public string RestorationPresetName => _mediaPool.CurrentSource?.AppliedRestoration?.PresetName ?? "";
    public bool RestorationEnabled => _mediaPool.CurrentSource?.AppliedRestoration?.IsEnabled ?? false;

    [ObservableProperty]
    private bool _includeRestoration = true;

    [ObservableProperty]
    private string _outputFileName = "output";

    [ObservableProperty]
    private ObservableCollection<ExportPreset> _presets = [];

    [ObservableProperty]
    private ExportPreset? _selectedPreset;

    [ObservableProperty]
    private ObservableCollection<ExportJob> _exportQueue = [];

    [ObservableProperty]
    private ExportJob? _currentJob;

    [ObservableProperty]
    private bool _isEncoding;

    [ObservableProperty]
    private string _statusText = "Ready";

    /// <summary>
    /// Status text shown in the workflow footer
    /// </summary>
    public string FooterStatusText
    {
        get
        {
            if (IsEncoding)
                return StatusText;
            if (ExportQueue.Count > 0)
                return $"{ExportQueue.Count} job(s) in queue • Ready to export";
            return $"Output: {SelectedVideoCodec} • {SelectedAudioCodec}";
        }
    }

    [ObservableProperty]
    private string _logOutput = "";

    // Video settings
    [ObservableProperty]
    private bool _videoEnabled = true;

    [ObservableProperty]
    private ObservableCollection<string> _videoCodecs = ["libx264", "libx265", "h264_nvenc", "hevc_nvenc", "prores_ks", "ffv1"];

    [ObservableProperty]
    private string _selectedVideoCodec = "libx264";

    [ObservableProperty]
    private int _quality = 22;

    [ObservableProperty]
    private ObservableCollection<string> _presetSpeeds = ["ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow"];

    [ObservableProperty]
    private string _selectedPresetSpeed = "medium";

    [ObservableProperty]
    private ObservableCollection<ResolutionOption> _resolutions = [];

    [ObservableProperty]
    private ResolutionOption? _selectedResolution;

    [ObservableProperty]
    private ObservableCollection<string> _frameRates = ["Source", "23.976", "24", "25", "29.97", "30", "50", "59.94", "60"];

    [ObservableProperty]
    private string _selectedFrameRate = "Source";

    // NVENC Hardware Encoder Presets (p1=fastest/lowest quality, p7=slowest/highest quality)
    [ObservableProperty]
    private ObservableCollection<string> _nvencPresets = ["p1 (fastest)", "p2", "p3", "p4 (balanced)", "p5", "p6", "p7 (best quality)"];

    [ObservableProperty]
    private string _selectedNvencPreset = "p4 (balanced)";

    // ProRes Profiles (0=proxy, 1=LT, 2=standard, 3=HQ, 4=4444, 5=4444XQ)
    [ObservableProperty]
    private ObservableCollection<string> _proresProfiles = ["0 - Proxy", "1 - LT", "2 - Standard", "3 - HQ", "4 - 4444", "5 - 4444XQ"];

    [ObservableProperty]
    private string _selectedProresProfile = "2 - Standard";

    // Visibility helpers for codec-specific options
    public bool IsNvencCodec => SelectedVideoCodec?.Contains("nvenc", StringComparison.OrdinalIgnoreCase) ?? false;
    public bool IsProresCodec => SelectedVideoCodec?.Contains("prores", StringComparison.OrdinalIgnoreCase) ?? false;

    // Audio settings
    [ObservableProperty]
    private bool _audioEnabled = true;

    [ObservableProperty]
    private ObservableCollection<string> _audioCodecs = ["aac", "libmp3lame", "flac", "pcm_s16le", "copy"];

    [ObservableProperty]
    private string _selectedAudioCodec = "aac";

    [ObservableProperty]
    private ObservableCollection<int> _audioBitrates = [128, 160, 192, 224, 256, 320];

    [ObservableProperty]
    private int _selectedAudioBitrate = 192;

    // Format
    [ObservableProperty]
    private ObservableCollection<string> _formats = ["mp4", "mkv", "mov", "avi", "webm"];

    [ObservableProperty]
    private string _selectedFormat = "mp4";

    // Estimated info
    [ObservableProperty]
    private string _estimatedSize = "";

    [ObservableProperty]
    private string _inputInfo = "";

    [ObservableProperty]
    private double _inputDuration;

    public ExportViewModel(IMediaPoolService mediaPool, ISettingsService settingsService)
    {
        _mediaPool = mediaPool;
        _settingsService = settingsService;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        // Get Timeline from Edit page via MediaPoolService
        _mediaPool.EditTimelineChanged += OnEditTimelineChanged;
        Timeline = _mediaPool.EditTimeline;

        // Initialize unified export pipeline service
        _pipelineService = new ExportPipelineService(_effectService);

        LoadPresets();
        InitializeResolutions();
        LoadExportSettings();

        _ffmpegService.ProgressChanged += OnProgressChanged;
        _ffmpegService.LogMessage += OnLogMessage;
        _ffmpegService.EncodingStarted += OnEncodingStarted;
        _ffmpegService.EncodingCompleted += OnEncodingCompleted;

        // VapourSynth service events
        _vapourSynthService.ProgressChanged += OnVsProgressChanged;
        _vapourSynthService.LogMessage += OnLogMessage;
        _vapourSynthService.ProcessingStarted += OnVsProcessingStarted;
        _vapourSynthService.ProcessingCompleted += OnVsProcessingCompleted;
    }

    private void OnEditTimelineChanged(object? sender, Timeline? timeline)
    {
        Timeline = timeline;
        NotifyAutoDetectionChanged();
    }

    /// <summary>
    /// Notify all auto-detection properties changed
    /// </summary>
    private void NotifyAutoDetectionChanged()
    {
        OnPropertyChanged(nameof(HasTimelineContent));
        OnPropertyChanged(nameof(TimelineClipCount));
        OnPropertyChanged(nameof(HasSourceFile));
        OnPropertyChanged(nameof(AutoDetectedSourceType));
        OnPropertyChanged(nameof(AutoDetectedSourceName));
        OnPropertyChanged(nameof(RequiresVapourSynth));
        OnPropertyChanged(nameof(ExportPipelineSummary));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(FooterStatusText));
    }

    // Parameterless constructor for XAML design-time support
    public ExportViewModel() : this(
        App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService ?? new MediaPoolService(new PathResolver()),
        App.Services?.GetService(typeof(ISettingsService)) as ISettingsService ?? new SettingsService())
    {
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        OnPropertyChanged(nameof(HasCurrentSource));
        OnPropertyChanged(nameof(CurrentSourcePath));
        OnPropertyChanged(nameof(HasRestoration));
        OnPropertyChanged(nameof(RestorationPresetName));
        OnPropertyChanged(nameof(RestorationEnabled));

        // Auto-populate input from current source if not already set
        if (item != null && string.IsNullOrEmpty(InputPath))
        {
            InputPath = item.FilePath;
        }

        // Default to include restoration if present
        if (item?.HasRestoration == true)
        {
            IncludeRestoration = true;
        }

        NotifyAutoDetectionChanged();
    }

    private void LoadPresets()
    {
        foreach (var preset in FFmpegService.GetPresets())
        {
            Presets.Add(preset);
        }
        SelectedPreset = Presets.FirstOrDefault();
    }

    private void InitializeResolutions()
    {
        Resolutions.Add(new ResolutionOption { Name = "Source", Width = 0, Height = 0 });
        Resolutions.Add(new ResolutionOption { Name = "4K (3840x2160)", Width = 3840, Height = 2160 });
        Resolutions.Add(new ResolutionOption { Name = "1440p (2560x1440)", Width = 2560, Height = 1440 });
        Resolutions.Add(new ResolutionOption { Name = "1080p (1920x1080)", Width = 1920, Height = 1080 });
        Resolutions.Add(new ResolutionOption { Name = "720p (1280x720)", Width = 1280, Height = 720 });
        Resolutions.Add(new ResolutionOption { Name = "480p (854x480)", Width = 854, Height = 480 });
        SelectedResolution = Resolutions.First();
    }

    private void LoadExportSettings()
    {
        try
        {
            _isLoading = true;
            var settings = _settingsService.Load();

            // Load export settings from AppSettings
            SelectedFormat = settings.DefaultExportFormat;
            SelectedVideoCodec = settings.DefaultVideoCodec;
            SelectedAudioCodec = settings.DefaultAudioCodec;
            Quality = settings.DefaultVideoQuality;
            SelectedAudioBitrate = settings.DefaultAudioBitrate;
            SelectedPresetSpeed = settings.DefaultPresetSpeed;
            VideoEnabled = settings.DefaultVideoEnabled;
            AudioEnabled = settings.DefaultAudioEnabled;
            SelectedFrameRate = settings.DefaultFrameRate;
            SelectedNvencPreset = settings.DefaultNvencPreset;
            SelectedProresProfile = settings.DefaultProresProfile;

            // Load export mode
            if (Enum.TryParse<ExportMode>(settings.DefaultExportMode, out var mode))
            {
                ExportMode = mode;
            }

            // Load resolution
            var resolution = Resolutions.FirstOrDefault(r => r.Name == settings.DefaultResolution);
            if (resolution != null)
            {
                SelectedResolution = resolution;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load export settings, using defaults");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SaveExportSettings()
    {
        if (_isLoading) return;

        try
        {
            var settings = _settingsService.Load();

            settings.DefaultExportFormat = SelectedFormat;
            settings.DefaultVideoCodec = SelectedVideoCodec;
            settings.DefaultAudioCodec = SelectedAudioCodec;
            settings.DefaultVideoQuality = Quality;
            settings.DefaultAudioBitrate = SelectedAudioBitrate;
            settings.DefaultPresetSpeed = SelectedPresetSpeed;
            settings.DefaultExportMode = ExportMode.ToString();
            settings.DefaultVideoEnabled = VideoEnabled;
            settings.DefaultAudioEnabled = AudioEnabled;
            settings.DefaultResolution = SelectedResolution?.Name ?? "Source";
            settings.DefaultFrameRate = SelectedFrameRate;
            settings.DefaultNvencPreset = SelectedNvencPreset;
            settings.DefaultProresProfile = SelectedProresProfile;

            _settingsService.Save(settings);
        }
        catch
        {
            // Ignore save errors
        }
    }

    partial void OnSelectedPresetChanged(ExportPreset? value)
    {
        if (value == null) return;

        SelectedVideoCodec = value.VideoCodec;
        SelectedAudioCodec = value.AudioCodec;
        Quality = value.Quality;
        SelectedPresetSpeed = value.Preset;
        SelectedFormat = value.Format;
        SelectedAudioBitrate = value.AudioBitrate > 0 ? value.AudioBitrate : 192;
        VideoEnabled = value.VideoEnabled;

        UpdateEstimatedSize();
    }

    partial void OnInputPathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && File.Exists(value))
        {
            OutputFileName = Path.GetFileNameWithoutExtension(value) + "_export";
            UpdateOutputPath();
            LoadInputInfo();
        }
        NotifyAutoDetectionChanged();
    }

    partial void OnSelectedFormatChanged(string value)
    {
        UpdateOutputPath();
        UpdateEstimatedSize();
        SaveExportSettings();
    }

    partial void OnOutputFileNameChanged(string value)
    {
        UpdateOutputPath();
    }

    partial void OnQualityChanged(int value)
    {
        UpdateEstimatedSize();
        SaveExportSettings();
    }

    partial void OnSelectedVideoCodecChanged(string value)
    {
        OnPropertyChanged(nameof(IsNvencCodec));
        OnPropertyChanged(nameof(IsProresCodec));
        SaveExportSettings();
    }

    partial void OnSelectedAudioCodecChanged(string value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedAudioBitrateChanged(int value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedPresetSpeedChanged(string value)
    {
        SaveExportSettings();
    }

    partial void OnExportModeChanged(ExportMode value)
    {
        SaveExportSettings();
    }

    partial void OnVideoEnabledChanged(bool value)
    {
        SaveExportSettings();
    }

    partial void OnAudioEnabledChanged(bool value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedResolutionChanged(ResolutionOption? value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedFrameRateChanged(string value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedNvencPresetChanged(string value)
    {
        SaveExportSettings();
    }

    partial void OnSelectedProresProfileChanged(string value)
    {
        SaveExportSettings();
    }

    partial void OnIncludeRestorationChanged(bool value)
    {
        NotifyAutoDetectionChanged();
    }

    private void UpdateOutputPath()
    {
        if (string.IsNullOrEmpty(InputPath)) return;

        var dir = Path.GetDirectoryName(InputPath) ?? "";
        OutputPath = Path.Combine(dir, $"{OutputFileName}.{SelectedFormat}");
    }

    private async void LoadInputInfo()
    {
        if (string.IsNullOrEmpty(InputPath) || !File.Exists(InputPath)) return;

        InputDuration = await _ffmpegService.GetDurationAsync(InputPath);
        var fileInfo = new FileInfo(InputPath);

        InputInfo = $"Duration: {FormatTime(InputDuration)} | Size: {FormatFileSize(fileInfo.Length)}";
        UpdateEstimatedSize();
    }

    private void UpdateEstimatedSize()
    {
        if (InputDuration <= 0)
        {
            EstimatedSize = "";
            return;
        }

        // Rough estimation based on codec and quality
        double bitrateMbps = SelectedVideoCodec switch
        {
            "libx264" => (50 - Quality) * 0.8,
            "libx265" => (50 - Quality) * 0.5,
            "h264_nvenc" => (50 - Quality) * 0.9,
            "hevc_nvenc" => (50 - Quality) * 0.6,
            "prores_ks" => 100,
            "ffv1" => 150,
            _ => 20
        };

        var videoBits = bitrateMbps * 1_000_000 * InputDuration;
        var audioBits = SelectedAudioBitrate * 1000 * InputDuration;
        var totalBytes = (videoBits + audioBits) / 8;

        EstimatedSize = $"~{FormatFileSize((long)totalBytes)}";
    }

    [RelayCommand]
    private void BrowseInput()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Input File",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpg;*.mpeg;*.ts|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Output As",
            Filter = GetSaveFilter(),
            FileName = OutputFileName,
            DefaultExt = SelectedFormat
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
            OutputFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private string GetSaveFilter()
    {
        return SelectedFormat switch
        {
            "mp4" => "MP4 Video|*.mp4",
            "mkv" => "Matroska Video|*.mkv",
            "mov" => "QuickTime Movie|*.mov",
            "avi" => "AVI Video|*.avi",
            "webm" => "WebM Video|*.webm",
            _ => "All Files|*.*"
        };
    }

    [RelayCommand]
    private void AddToQueue()
    {
        if (string.IsNullOrEmpty(InputPath) || string.IsNullOrEmpty(OutputPath))
        {
            StatusText = "Please select input and output files";
            return;
        }

        var job = new ExportJob
        {
            Name = Path.GetFileName(OutputPath),
            InputPath = InputPath,
            OutputPath = OutputPath,
            Settings = CreateExportSettings()
        };

        ExportQueue.Add(job);
        StatusText = $"Added to queue: {job.Name}";
        AppendLog($"Added to queue: {job.Name}");
    }

    [RelayCommand]
    private async Task StartExportAsync()
    {
        // === UNIFIED PIPELINE: All exports go through VapourSynth ===

        // Validate VapourSynth availability (required for all exports now)
        if (!IsVapourSynthAvailable)
        {
            StatusText = "VapourSynth not available. Please build the distribution first.";
            AppendLog("Error: VSPipe not found. Run scripts/build/Build-Portable.ps1 to set up VapourSynth.");
            return;
        }

        // Determine source type and validate
        var hasTimeline = Timeline?.HasClips == true;
        var hasSourceFile = !string.IsNullOrEmpty(InputPath) && File.Exists(InputPath);

        if (!hasTimeline && !hasSourceFile)
        {
            StatusText = "Please select an input file or add clips to timeline";
            return;
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            StatusText = "Please select an output file";
            return;
        }

        if (IsEncoding)
        {
            StatusText = "Already encoding";
            return;
        }

        // Check if output file exists and warn user
        if (File.Exists(OutputPath))
        {
            var result = System.Windows.MessageBox.Show(
                $"The file '{Path.GetFileName(OutputPath)}' already exists.\n\nDo you want to replace it?",
                "Confirm Overwrite",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                StatusText = "Export cancelled";
                AppendLog("Export cancelled: output file already exists");
                return;
            }
        }

        // Build unified export settings
        var unifiedSettings = BuildUnifiedExportSettings();

        // Create job for tracking
        var sourceDescription = hasTimeline
            ? "Timeline"
            : (HasRestoration && IncludeRestoration
                ? $"{Path.GetFileName(InputPath)} + {RestorationPresetName}"
                : Path.GetFileName(InputPath));

        var job = new ExportJob
        {
            Name = Path.GetFileName(OutputPath),
            InputPath = sourceDescription,
            OutputPath = OutputPath,
            Settings = CreateExportSettings(),
            Status = ExportJobStatus.Encoding,
            StartTime = DateTime.Now
        };

        CurrentJob = job;
        IsEncoding = true;
        StatusText = "Rendering with VapourSynth pipeline...";

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var success = await ExportWithUnifiedPipelineAsync(unifiedSettings, _cancellationTokenSource.Token);

            job.EndTime = DateTime.Now;
            job.Status = success ? ExportJobStatus.Completed : ExportJobStatus.Failed;

            if (success && File.Exists(OutputPath))
            {
                job.OutputFileSize = new FileInfo(OutputPath).Length;
                StatusText = $"Export completed: {Path.GetFileName(OutputPath)}";
            }
            else if (!success)
            {
                StatusText = "Export failed";
            }
        }
        catch (OperationCanceledException)
        {
            job.Status = ExportJobStatus.Cancelled;
            job.StatusText = "Cancelled by user";
            StatusText = "Export cancelled";
            AppendLog("Export cancelled by user");
        }
        catch (Exception ex)
        {
            job.Status = ExportJobStatus.Failed;
            job.StatusText = ex.Message;
            StatusText = $"Export failed: {ex.Message}";
            AppendLog($"Error: {ex.Message}");
            _logger.LogError(ex, "Export failed");
        }
        finally
        {
            IsEncoding = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Builds unified export settings from current UI state
    /// </summary>
    private UnifiedExportSettings BuildUnifiedExportSettings()
    {
        var hasTimeline = Timeline?.HasClips == true;

        var settings = new UnifiedExportSettings
        {
            // Source configuration
            SourceType = hasTimeline ? ExportSourceType.Timeline : ExportSourceType.SingleFile,
            SingleFilePath = InputPath,
            Timeline = Timeline,

            // Restoration
            IncludeRestoration = IncludeRestoration && HasRestoration,
            Restoration = _mediaPool.CurrentSource?.AppliedRestoration,

            // Output configuration
            OutputPath = OutputPath,
            OutputWidth = SelectedResolution?.Width ?? 0,
            OutputHeight = SelectedResolution?.Height ?? 0,
            OutputFrameRate = SelectedFrameRate != "Source" && double.TryParse(SelectedFrameRate, out var fps) ? fps : 0,

            // Video encoding - use actual UI values
            VideoCodec = SelectedVideoCodec,
            Quality = Quality,
            Preset = SelectedPresetSpeed,
            HardwarePreset = ExtractNvencPreset(),
            ProResProfile = ExtractProResProfile(),
            PixelFormat = "yuv420p",

            // Audio encoding - use actual UI values (NOT hardcoded!)
            AudioEnabled = AudioEnabled,
            AudioCodec = SelectedAudioCodec,
            AudioBitrate = SelectedAudioBitrate
        };

        // Set audio source path
        settings.AudioSourcePath = settings.GetPrimarySourcePath();

        return settings;
    }

    /// <summary>
    /// Extract NVENC preset number from UI selection (e.g., "p4 (balanced)" -> "p4")
    /// </summary>
    private string ExtractNvencPreset()
    {
        if (string.IsNullOrEmpty(SelectedNvencPreset)) return "p4";
        var parts = SelectedNvencPreset.Split(' ');
        return parts.Length > 0 ? parts[0] : "p4";
    }

    /// <summary>
    /// Extract ProRes profile number from UI selection (e.g., "2 - Standard" -> 2)
    /// </summary>
    private int ExtractProResProfile()
    {
        if (string.IsNullOrEmpty(SelectedProresProfile)) return 2;
        var parts = SelectedProresProfile.Split(' ');
        return parts.Length > 0 && int.TryParse(parts[0], out var profile) ? profile : 2;
    }

    /// <summary>
    /// Export using the unified VapourSynth pipeline
    /// </summary>
    private async Task<bool> ExportWithUnifiedPipelineAsync(UnifiedExportSettings settings, CancellationToken ct)
    {
        string? scriptPath = null;

        try
        {
            // Generate unified VapourSynth script
            AppendLog("Generating VapourSynth script...");
            var scriptContent = _pipelineService.GenerateUnifiedScript(settings);

            // Save script to temp file
            scriptPath = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}.vpy");
            await File.WriteAllTextAsync(scriptPath, scriptContent, ct);

            AppendLog($"Script: {scriptPath}");
            AppendLog($"Source: {(settings.SourceType == ExportSourceType.Timeline ? "Timeline" : settings.SingleFilePath)}");
            if (settings.IncludeRestoration)
            {
                AppendLog($"Restoration: {settings.Restoration?.PresetName}");
            }
            AppendLog($"Video: {settings.VideoCodec} (CRF {settings.Quality})");
            AppendLog($"Audio: {settings.AudioCodec} ({settings.AudioBitrate}k)");
            AppendLog("Starting VapourSynth pipeline...");

            // Create VS encoding settings
            var vsSettings = new VapourSynthEncodingSettings
            {
                VideoCodec = settings.VideoCodec,
                Quality = settings.Quality,
                Preset = settings.Preset,
                HardwarePreset = settings.HardwarePreset,
                ProResProfile = settings.ProResProfile,
                PixelFormat = settings.PixelFormat,

                // Audio - using actual user settings
                IncludeAudio = settings.AudioEnabled,
                AudioSourcePath = settings.AudioSourcePath,
                AudioCodec = settings.AudioCodec,
                AudioBitrate = $"{settings.AudioBitrate}k"
            };

            // Process via VapourSynth pipeline
            var success = await _vapourSynthService.ProcessScriptAsync(
                scriptPath,
                settings.OutputPath,
                vsSettings,
                ct);

            return success;
        }
        finally
        {
            // Cleanup temp script
            if (scriptPath != null && File.Exists(scriptPath))
            {
                try { File.Delete(scriptPath); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Export timeline using VapourSynth pipeline for effect processing
    /// </summary>
    private async Task<bool> ExportTimelineWithVapourSynthAsync(CancellationToken ct)
    {
        if (Timeline == null) return false;

        string? scriptPath = null;

        try
        {
            // Generate VapourSynth script from timeline
            AppendLog("Generating VapourSynth script from timeline...");
            var scriptContent = _effectService.GenerateTimelineScript(Timeline, OutputPath);

            // Save script to temp file
            scriptPath = Path.Combine(Path.GetTempPath(), $"timeline_export_{Guid.NewGuid():N}.vpy");
            await File.WriteAllTextAsync(scriptPath, scriptContent, ct);

            AppendLog($"Script saved to: {scriptPath}");
            AppendLog("Starting VapourSynth pipeline...");

            // Get primary audio source from timeline (first clip with audio)
            var primaryClip = Timeline?.Tracks
                .SelectMany(t => t.Clips)
                .FirstOrDefault(c => !string.IsNullOrEmpty(c.SourcePath) && File.Exists(c.SourcePath));

            // Create encoding settings with audio source
            var vsSettings = new VapourSynthEncodingSettings
            {
                VideoCodec = SelectedVideoCodec,
                Quality = Quality,
                Preset = SelectedPresetSpeed,
                PixelFormat = "yuv420p",
                // Include audio from the primary clip (or first source)
                AudioSourcePath = primaryClip?.SourcePath ?? InputPath,
                IncludeAudio = true,
                AudioCodec = "aac",
                AudioBitrate = "192k"
            };

            // Process script with VapourSynth → FFmpeg pipeline
            var success = await _vapourSynthService.ProcessScriptAsync(
                scriptPath,
                OutputPath,
                vsSettings,
                ct);

            return success;
        }
        finally
        {
            // Clean up temp script
            if (scriptPath != null && File.Exists(scriptPath))
            {
                try { File.Delete(scriptPath); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Export source with restoration settings using VapourSynth pipeline
    /// </summary>
    private async Task<bool> ExportWithRestorationAsync(CancellationToken ct)
    {
        var source = _mediaPool.CurrentSource;
        if (source?.AppliedRestoration == null)
        {
            AppendLog("Error: No restoration settings found");
            return false;
        }

        var restoration = source.AppliedRestoration;
        string? scriptPath = null;

        try
        {
            AppendLog($"Generating restoration script ({restoration.PresetName})...");

            // Generate VapourSynth script with restoration
            var scriptContent = GenerateRestorationScript(InputPath, restoration.GeneratedScript);

            // Save script to temp file
            scriptPath = Path.Combine(Path.GetTempPath(), $"restore_export_{Guid.NewGuid():N}.vpy");
            await File.WriteAllTextAsync(scriptPath, scriptContent, ct);

            AppendLog($"Script saved to: {scriptPath}");
            AppendLog("Starting VapourSynth pipeline with restoration...");

            // Create encoding settings with audio source
            var vsSettings = new VapourSynthEncodingSettings
            {
                VideoCodec = SelectedVideoCodec,
                Quality = Quality,
                Preset = SelectedPresetSpeed,
                PixelFormat = "yuv420p",
                // Include audio from the original source file
                AudioSourcePath = InputPath,
                IncludeAudio = true,
                AudioCodec = "aac",
                AudioBitrate = "192k"
            };

            // Add NVENC preset if applicable
            if (IsNvencCodec && !string.IsNullOrEmpty(SelectedNvencPreset))
            {
                vsSettings.HardwarePreset = SelectedNvencPreset.Split(' ')[0];
            }

            // Process script with VapourSynth → FFmpeg pipeline
            var success = await _vapourSynthService.ProcessScriptAsync(
                scriptPath,
                OutputPath,
                vsSettings,
                ct);

            return success;
        }
        finally
        {
            // Clean up temp script
            if (scriptPath != null && File.Exists(scriptPath))
            {
                try { File.Delete(scriptPath); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Generates a complete VapourSynth script with source loading and restoration
    /// </summary>
    private static string GenerateRestorationScript(string sourcePath, string restorationScript)
    {
        var escapedPath = sourcePath.Replace("'", "\\'").Replace("\\", "\\\\");

        return $@"import vapoursynth as vs
core = vs.core

# Load source video
video_in = core.lsmas.LWLibavSource(r'{escapedPath}')

# Apply restoration preset
{restorationScript}
";
    }

    [RelayCommand]
    private async Task StartQueueAsync()
    {
        if (ExportQueue.Count == 0)
        {
            StatusText = "Queue is empty";
            return;
        }

        if (IsEncoding)
        {
            StatusText = "Already encoding";
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        foreach (var job in ExportQueue.Where(j => j.Status == ExportJobStatus.Pending))
        {
            if (_cancellationTokenSource.IsCancellationRequested) break;

            CurrentJob = job;
            job.Status = ExportJobStatus.Encoding;
            job.StartTime = DateTime.Now;
            IsEncoding = true;

            try
            {
                var success = await _ffmpegService.EncodeAsync(job.Settings, _cancellationTokenSource.Token);

                job.EndTime = DateTime.Now;
                job.Status = success ? ExportJobStatus.Completed : ExportJobStatus.Failed;

                if (success && File.Exists(job.OutputPath))
                {
                    job.OutputFileSize = new FileInfo(job.OutputPath).Length;
                }
            }
            catch
            {
                job.Status = ExportJobStatus.Failed;
            }
        }

        IsEncoding = false;
        CurrentJob = null;
        StatusText = "Queue completed";
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    [RelayCommand]
    private void CancelExport()
    {
        _cancellationTokenSource?.Cancel();
        _ffmpegService.Cancel();
        _vapourSynthService.Cancel();

        if (CurrentJob != null)
        {
            CurrentJob.Status = ExportJobStatus.Cancelled;
            CurrentJob.StatusText = "Cancelled";
        }

        StatusText = "Export cancelled";
        IsEncoding = false;
    }

    [RelayCommand]
    private void ClearQueue()
    {
        var completedJobs = ExportQueue.Where(j =>
            j.Status == ExportJobStatus.Completed ||
            j.Status == ExportJobStatus.Failed ||
            j.Status == ExportJobStatus.Cancelled).ToList();

        foreach (var job in completedJobs)
        {
            ExportQueue.Remove(job);
        }
    }

    [RelayCommand]
    private void RemoveJob(ExportJob? job)
    {
        if (job != null && job.Status != ExportJobStatus.Encoding)
        {
            ExportQueue.Remove(job);
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogOutput = "";
    }

    /// <summary>
    /// Clears restoration settings from the current source
    /// </summary>
    [RelayCommand]
    private void ClearRestoration()
    {
        var source = _mediaPool.CurrentSource;
        if (source == null) return;

        source.AppliedRestoration = null;
        OnPropertyChanged(nameof(HasRestoration));
        OnPropertyChanged(nameof(RestorationPresetName));
        OnPropertyChanged(nameof(RestorationEnabled));

        StatusText = $"Cleared restoration from {source.Name}";
        AppendLog($"Cleared restoration settings from {source.Name}");
        ToastService.Instance.ShowInfo($"Cleared restoration from {source.Name}");
    }

    private ExportSettings CreateExportSettings()
    {
        var settings = new ExportSettings
        {
            InputPath = InputPath,
            OutputPath = OutputPath,
            VideoEnabled = VideoEnabled,
            VideoCodec = SelectedVideoCodec,
            Quality = Quality,
            Preset = SelectedPresetSpeed,
            AudioEnabled = AudioEnabled,
            AudioCodec = SelectedAudioCodec,
            AudioBitrate = SelectedAudioBitrate
        };

        // Extract NVENC preset (e.g., "p4 (balanced)" -> "p4")
        if (IsNvencCodec && !string.IsNullOrEmpty(SelectedNvencPreset))
        {
            var preset = SelectedNvencPreset.Split(' ')[0];
            settings.HardwarePreset = preset;
        }

        // Extract ProRes profile (e.g., "2 - Standard" -> 2)
        if (IsProresCodec && !string.IsNullOrEmpty(SelectedProresProfile))
        {
            var profileStr = SelectedProresProfile.Split(' ')[0];
            if (int.TryParse(profileStr, out var profile))
            {
                settings.ProResProfile = profile;
            }
        }

        if (SelectedResolution != null && SelectedResolution.Width > 0)
        {
            settings.Width = SelectedResolution.Width;
            settings.Height = SelectedResolution.Height;
        }

        if (SelectedFrameRate != "Source" && double.TryParse(SelectedFrameRate, out var fps))
        {
            settings.FrameRate = fps;
        }

        return settings;
    }

    private void OnProgressChanged(object? sender, EncodingProgressEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CurrentJob?.UpdateProgress(e);
            StatusText = $"Encoding: {e.Progress:F1}% @ {e.Speed:F2}x";
        });
    }

    private void OnLogMessage(object? sender, string message)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            AppendLog(message);
        });
    }

    private void OnEncodingStarted(object? sender, EventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            StatusText = "Encoding started...";
        });
    }

    private void OnEncodingCompleted(object? sender, EncodingCompletedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (e.Success)
            {
                StatusText = "Export completed!";
                AppendLog($"Completed: {e.OutputPath}");
            }
            else if (e.Cancelled)
            {
                StatusText = "Export cancelled";
            }
            else
            {
                StatusText = "Export failed";
            }
        });
    }

    // VapourSynth event handlers
    private void OnVsProgressChanged(object? sender, VapourSynthProgressEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (CurrentJob != null)
            {
                CurrentJob.Progress = e.Progress;
            }
            var eta = e.EstimatedTimeRemaining;
            var etaStr = eta.TotalSeconds > 0 ? $" ETA: {eta:hh\\:mm\\:ss}" : "";
            StatusText = $"Rendering: {e.Progress:F1}% ({e.CurrentFrame}/{e.TotalFrames} frames) @ {e.Fps:F1} fps{etaStr}";
        });
    }

    private void OnVsProcessingStarted(object? sender, EventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            StatusText = "VapourSynth processing started...";
            AppendLog("VapourSynth pipeline started");
        });
    }

    private void OnVsProcessingCompleted(object? sender, VapourSynthCompletedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (e.Success)
            {
                StatusText = "Timeline export completed!";
                AppendLog($"Completed: {e.OutputPath} ({e.TotalFrames} frames)");
            }
            else if (e.Cancelled)
            {
                StatusText = "Export cancelled";
                AppendLog("VapourSynth processing cancelled");
            }
            else
            {
                StatusText = "Export failed";
                if (!string.IsNullOrEmpty(e.ErrorMessage))
                    AppendLog($"Error: {e.ErrorMessage}");
            }
        });
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogOutput += $"[{timestamp}] {message}\n";
    }

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe from MediaPoolService events
        _mediaPool.CurrentSourceChanged -= OnCurrentSourceChanged;

        // Unsubscribe from FFmpeg service events
        _ffmpegService.ProgressChanged -= OnProgressChanged;
        _ffmpegService.LogMessage -= OnLogMessage;
        _ffmpegService.EncodingStarted -= OnEncodingStarted;
        _ffmpegService.EncodingCompleted -= OnEncodingCompleted;

        // Unsubscribe from VapourSynth service events
        _vapourSynthService.ProgressChanged -= OnVsProgressChanged;
        _vapourSynthService.LogMessage -= OnLogMessage;
        _vapourSynthService.ProcessingStarted -= OnVsProcessingStarted;
        _vapourSynthService.ProcessingCompleted -= OnVsProcessingCompleted;

        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Export mode options
/// </summary>
public enum ExportMode
{
    /// <summary>
    /// Direct FFmpeg encoding (file to file)
    /// </summary>
    DirectEncode,

    /// <summary>
    /// Timeline export using VapourSynth for effect processing
    /// </summary>
    TimelineWithEffects
}

public class ResolutionOption
{
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() => Name;
}
