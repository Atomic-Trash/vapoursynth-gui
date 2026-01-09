using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Helpers;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class PreviewViewModel : ObservableObject, IDisposable
{
    private readonly FrameExtractionService _frameService;
    private readonly FrameCacheService _cacheService;
    private CancellationTokenSource? _loadCts;
    private readonly DispatcherTimer _debounceTimer;
    private int _pendingFrameNumber;
    private EventHandler? _debounceTickHandler;
    private bool _disposed;

    public PreviewViewModel()
    {
        _frameService = new FrameExtractionService();
        _cacheService = new FrameCacheService(50);

        // Debounce timer for slider changes (store handler for cleanup)
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _debounceTickHandler = async (s, e) =>
        {
            _debounceTimer.Stop();
            await LoadFrameInternalAsync(_pendingFrameNumber);
        };
        _debounceTimer.Tick += _debounceTickHandler;
    }

    // State Properties
    [ObservableProperty]
    private WriteableBitmap? _currentFrame;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrameInfoText))]
    private int _currentFrameNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrameInfoText))]
    [NotifyPropertyChangedFor(nameof(HasVideo))]
    private int _totalFrames;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNavigate))]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "No video loaded";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolutionText))]
    [NotifyPropertyChangedFor(nameof(FormatText))]
    [NotifyPropertyChangedFor(nameof(HasVideo))]
    private VideoInfo? _videoInfo;

    [ObservableProperty]
    private string? _currentScriptPath;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _fitToWindow = true;

    // A/B Comparison Mode
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComparisonEnabled))]
    private string? _comparisonScriptPath;

    [ObservableProperty]
    private WriteableBitmap? _comparisonFrame;

    [ObservableProperty]
    private bool _showComparison;

    [ObservableProperty]
    private ComparisonMode _comparisonMode = ComparisonMode.SideBySide;

    [ObservableProperty]
    private double _wipePosition = 0.5; // 0.0 to 1.0

    // Metadata overlay
    [ObservableProperty]
    private bool _showMetadataOverlay;

    public bool IsComparisonEnabled => !string.IsNullOrEmpty(ComparisonScriptPath);

    // Computed Properties
    public string FrameInfoText => HasVideo
        ? $"Frame {CurrentFrameNumber + 1} / {TotalFrames}"
        : "No video";

    public string ResolutionText => VideoInfo != null
        ? $"{VideoInfo.Width}x{VideoInfo.Height}"
        : "";

    public string FormatText => VideoInfo?.Format ?? "";

    public bool HasVideo => TotalFrames > 0 && VideoInfo != null;

    // Extended metadata properties for overlay
    public string FrameRateText => VideoInfo != null
        ? $"{VideoInfo.Fps:F3} fps"
        : "";

    public string AspectRatioText => VideoInfo != null && VideoInfo.Width > 0 && VideoInfo.Height > 0
        ? CalculateAspectRatio(VideoInfo.Width, VideoInfo.Height)
        : "";

    public string BitDepthText => VideoInfo?.BitsPerSample > 0
        ? $"{VideoInfo.BitsPerSample}-bit"
        : "";

    public string DurationText => VideoInfo != null && VideoInfo.Fps > 0
        ? FormatDuration(TotalFrames / VideoInfo.Fps)
        : "";

    public string ColorSpaceText => VideoInfo?.ColorFamily ?? "";

    private static string CalculateAspectRatio(int width, int height)
    {
        var gcd = GCD(width, height);
        var ratioW = width / gcd;
        var ratioH = height / gcd;

        // Map to common aspect ratios
        var ratio = (double)width / height;
        return ratio switch
        {
            >= 2.35 and <= 2.40 => "2.39:1 (Scope)",
            >= 1.77 and <= 1.78 => "16:9",
            >= 1.85 and <= 1.86 => "1.85:1",
            >= 1.33 and <= 1.34 => "4:3",
            >= 2.0 and <= 2.01 => "2:1",
            _ => $"{ratioW}:{ratioH}"
        };
    }

    private static int GCD(int a, int b)
    {
        while (b != 0)
        {
            var t = b;
            b = a % b;
            a = t;
        }
        return a;
    }

    private static string FormatDuration(double totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(totalSeconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
    public bool CanNavigate => HasVideo && !IsLoading;
    public bool IsVSPipeAvailable => _frameService.IsAvailable;

    partial void OnCurrentFrameNumberChanged(int value)
    {
        // Debounce frame loading when slider is dragged
        _pendingFrameNumber = value;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task LoadScriptAsync(string? scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
        {
            HasError = true;
            ErrorMessage = "Script file not found";
            return;
        }

        // Cancel any previous loading operation
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        IsLoading = true;
        HasError = false;
        ErrorMessage = "";
        CurrentScriptPath = scriptPath;
        _cacheService.Clear();

        try
        {
            StatusMessage = "Loading video info...";

            // Get video information
            VideoInfo = await _frameService.GetVideoInfoAsync(scriptPath, _loadCts.Token);
            TotalFrames = VideoInfo.FrameCount;
            CurrentFrameNumber = 0;

            StatusMessage = $"Loaded: {VideoInfo.Resolution} - {TotalFrames} frames";

            // Load first frame
            await LoadFrameInternalAsync(0);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Loading cancelled";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusMessage = $"Error: {ex.Message}";
            VideoInfo = null;
            TotalFrames = 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFrameInternalAsync(int frameNumber)
    {
        if (!HasVideo || VideoInfo == null || string.IsNullOrEmpty(CurrentScriptPath))
            return;

        // Check cache first
        var cached = _cacheService.TryGetFrame(frameNumber);
        if (cached != null)
        {
            CurrentFrame = cached;
            return;
        }

        // Cancel previous load
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        IsLoading = true;

        try
        {
            StatusMessage = $"Loading frame {frameNumber + 1}...";

            var frameData = await _frameService.ExtractFrameAsync(
                CurrentScriptPath,
                frameNumber,
                VideoInfo,
                _loadCts.Token);

            // Convert Y4M to bitmap
            var bitmap = FrameConverter.FromY4M(frameData);

            // Cache and display
            _cacheService.AddFrame(frameNumber, bitmap);
            CurrentFrame = bitmap;

            // Load comparison frame if enabled
            if (ShowComparison && IsComparisonEnabled)
            {
                await LoadComparisonFrameAsync(frameNumber);
            }

            StatusMessage = $"Frame {frameNumber + 1} / {TotalFrames}";
        }
        catch (OperationCanceledException)
        {
            // Ignore - probably navigating quickly
        }
        catch (Exception ex)
        {
            StatusMessage = $"Frame error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task GoToFrameAsync(int frame)
    {
        if (frame < 0) frame = 0;
        if (frame >= TotalFrames) frame = TotalFrames - 1;

        CurrentFrameNumber = frame;
        await LoadFrameInternalAsync(frame);
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task NextFrameAsync()
    {
        if (CurrentFrameNumber < TotalFrames - 1)
        {
            CurrentFrameNumber++;
            await LoadFrameInternalAsync(CurrentFrameNumber);
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task PreviousFrameAsync()
    {
        if (CurrentFrameNumber > 0)
        {
            CurrentFrameNumber--;
            await LoadFrameInternalAsync(CurrentFrameNumber);
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task FirstFrameAsync()
    {
        CurrentFrameNumber = 0;
        await LoadFrameInternalAsync(0);
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task LastFrameAsync()
    {
        CurrentFrameNumber = TotalFrames - 1;
        await LoadFrameInternalAsync(CurrentFrameNumber);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        FitToWindow = false;
        ZoomLevel = Math.Min(ZoomLevel * 1.25, 8.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        FitToWindow = false;
        ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        FitToWindow = false;
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private void ToggleFitToWindow()
    {
        FitToWindow = !FitToWindow;
        if (FitToWindow)
            ZoomLevel = 1.0;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(CurrentScriptPath))
        {
            _cacheService.Clear();
            await LoadScriptAsync(CurrentScriptPath);
        }
    }

    [RelayCommand]
    private async Task SetComparisonScriptAsync(string? scriptPath)
    {
        ComparisonScriptPath = scriptPath;
        if (!string.IsNullOrEmpty(scriptPath) && HasVideo)
        {
            await LoadComparisonFrameAsync(CurrentFrameNumber);
        }
    }

    [RelayCommand]
    private void ToggleComparison()
    {
        ShowComparison = !ShowComparison && IsComparisonEnabled;
    }

    [RelayCommand]
    private void SetComparisonMode(ComparisonMode mode)
    {
        ComparisonMode = mode;
    }

    [RelayCommand]
    private void CycleComparisonMode()
    {
        ComparisonMode = ComparisonMode switch
        {
            ComparisonMode.SideBySide => ComparisonMode.Wipe,
            ComparisonMode.Wipe => ComparisonMode.Toggle,
            ComparisonMode.Toggle => ComparisonMode.SideBySide,
            _ => ComparisonMode.SideBySide
        };
    }

    [RelayCommand]
    private void ToggleMetadataOverlay()
    {
        ShowMetadataOverlay = !ShowMetadataOverlay;
    }

    private async Task LoadComparisonFrameAsync(int frameNumber)
    {
        if (string.IsNullOrEmpty(ComparisonScriptPath) || VideoInfo == null)
            return;

        try
        {
            var frameData = await _frameService.ExtractFrameAsync(
                ComparisonScriptPath,
                frameNumber,
                VideoInfo,
                _loadCts?.Token ?? CancellationToken.None);

            ComparisonFrame = FrameConverter.FromY4M(frameData);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Comparison error: {ex.Message}";
        }
    }

    partial void OnShowComparisonChanged(bool value)
    {
        if (value && IsComparisonEnabled && ComparisonFrame == null)
        {
            _ = LoadComparisonFrameAsync(CurrentFrameNumber);
        }
    }

    public void Cancel()
    {
        _loadCts?.Cancel();
        _frameService.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Stop and unsubscribe from debounce timer
        _debounceTimer.Stop();
        if (_debounceTickHandler != null)
        {
            _debounceTimer.Tick -= _debounceTickHandler;
        }

        _loadCts?.Dispose();
        _cacheService.Clear();
    }
}

/// <summary>
/// Comparison display modes
/// </summary>
public enum ComparisonMode
{
    /// <summary>Side by side view</summary>
    SideBySide,
    /// <summary>Wipe/split view with adjustable position</summary>
    Wipe,
    /// <summary>Toggle between A and B</summary>
    Toggle
}
