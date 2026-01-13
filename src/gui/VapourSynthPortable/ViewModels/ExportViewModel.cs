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
    }

    // Parameterless constructor for XAML design-time support
    public ExportViewModel() : this(
        App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService ?? new MediaPoolService(),
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
        // Check if we need VapourSynth (for restoration or timeline effects)
        var needsVapourSynth = ExportMode == ExportMode.TimelineWithEffects ||
                               (HasRestoration && IncludeRestoration);

        // Validate based on export mode
        if (ExportMode == ExportMode.TimelineWithEffects)
        {
            if (Timeline == null || !Timeline.HasClips)
            {
                StatusText = "No timeline clips to export";
                return;
            }

            if (!IsVapourSynthAvailable)
            {
                StatusText = "VapourSynth not available. Please build the distribution first.";
                AppendLog("Error: VSPipe not found. Run scripts/build/Build-Portable.ps1 to set up VapourSynth.");
                return;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(InputPath) || string.IsNullOrEmpty(OutputPath))
            {
                StatusText = "Please select input and output files";
                return;
            }

            // Warn if restoration requires VapourSynth but it's not available
            if (needsVapourSynth && !IsVapourSynthAvailable)
            {
                StatusText = "VapourSynth required for restoration. Please build the distribution first.";
                AppendLog("Error: Restoration requires VapourSynth. Run scripts/build/Build-Portable.ps1.");
                return;
            }
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

        var settings = CreateExportSettings();
        var sourceDescription = ExportMode == ExportMode.TimelineWithEffects
            ? "Timeline"
            : (HasRestoration && IncludeRestoration ? $"{Path.GetFileName(InputPath)} + {RestorationPresetName}" : InputPath);

        var job = new ExportJob
        {
            Name = Path.GetFileName(OutputPath),
            InputPath = sourceDescription,
            OutputPath = OutputPath,
            Settings = settings,
            Status = ExportJobStatus.Encoding,
            StartTime = DateTime.Now
        };

        CurrentJob = job;
        IsEncoding = true;

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            bool success;

            if (ExportMode == ExportMode.TimelineWithEffects)
            {
                StatusText = "Rendering timeline with VapourSynth...";
                success = await ExportTimelineWithVapourSynthAsync(_cancellationTokenSource.Token);
            }
            else if (HasRestoration && IncludeRestoration)
            {
                StatusText = $"Rendering with restoration ({RestorationPresetName})...";
                AppendLog($"Exporting with restoration: {RestorationPresetName}");
                success = await ExportWithRestorationAsync(_cancellationTokenSource.Token);
            }
            else
            {
                StatusText = "Encoding...";
                success = await _ffmpegService.EncodeAsync(settings, _cancellationTokenSource.Token);
            }

            job.EndTime = DateTime.Now;
            job.Status = success ? ExportJobStatus.Completed : ExportJobStatus.Failed;

            if (success && File.Exists(OutputPath))
            {
                job.OutputFileSize = new FileInfo(OutputPath).Length;
            }
        }
        catch (Exception ex)
        {
            job.Status = ExportJobStatus.Failed;
            job.StatusText = ex.Message;
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            IsEncoding = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
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

            // Create encoding settings for VSPipe output
            var vsSettings = new VapourSynthEncodingSettings
            {
                VideoCodec = SelectedVideoCodec,
                Quality = Quality,
                Preset = SelectedPresetSpeed,
                PixelFormat = "yuv420p"
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

            // Create encoding settings
            var vsSettings = new VapourSynthEncodingSettings
            {
                VideoCodec = SelectedVideoCodec,
                Quality = Quality,
                Preset = SelectedPresetSpeed,
                PixelFormat = "yuv420p"
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
