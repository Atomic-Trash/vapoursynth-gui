using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class ExportViewModel : ObservableObject, IDisposable
{
    private readonly IMediaPoolService _mediaPool;
    private readonly FFmpegService _ffmpegService = new();
    private readonly VapourSynthService _vapourSynthService = new();
    private readonly EffectService _effectService = EffectService.Instance;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    // Timeline reference for timeline export mode
    [ObservableProperty]
    private Timeline? _timeline;

    // Export mode selection
    [ObservableProperty]
    private ExportMode _exportMode = ExportMode.DirectEncode;

    [ObservableProperty]
    private ObservableCollection<ExportMode> _exportModes = [ExportMode.DirectEncode, ExportMode.TimelineWithEffects];

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

    public ExportViewModel(IMediaPoolService mediaPool)
    {
        _mediaPool = mediaPool;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        LoadPresets();
        InitializeResolutions();

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

    // Parameterless constructor for XAML design-time support
    public ExportViewModel() : this(App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService
        ?? new MediaPoolService())
    {
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        OnPropertyChanged(nameof(HasCurrentSource));
        OnPropertyChanged(nameof(CurrentSourcePath));

        // Auto-populate input from current source if not already set
        if (item != null && string.IsNullOrEmpty(InputPath))
        {
            InputPath = item.FilePath;
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
    }

    partial void OnOutputFileNameChanged(string value)
    {
        UpdateOutputPath();
    }

    partial void OnQualityChanged(int value)
    {
        UpdateEstimatedSize();
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
                AppendLog("Error: VSPipe not found. Run Build-Portable.ps1 to set up VapourSynth.");
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
        var job = new ExportJob
        {
            Name = Path.GetFileName(OutputPath),
            InputPath = ExportMode == ExportMode.TimelineWithEffects ? "Timeline" : InputPath,
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
