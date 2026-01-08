using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Helpers;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly FrameExtractionService _frameService;
    private readonly FrameCacheService _cacheService;
    private CancellationTokenSource? _loadCts;
    private readonly DispatcherTimer _debounceTimer;
    private int _pendingFrameNumber;

    public PreviewViewModel()
    {
        _frameService = new FrameExtractionService();
        _cacheService = new FrameCacheService(50);

        // Debounce timer for slider changes
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            await LoadFrameInternalAsync(_pendingFrameNumber);
        };
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

    // Computed Properties
    public string FrameInfoText => HasVideo
        ? $"Frame {CurrentFrameNumber + 1} / {TotalFrames}"
        : "No video";

    public string ResolutionText => VideoInfo != null
        ? $"{VideoInfo.Width}x{VideoInfo.Height}"
        : "";

    public string FormatText => VideoInfo?.Format ?? "";

    public bool HasVideo => TotalFrames > 0 && VideoInfo != null;
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

    public void Cancel()
    {
        _loadCts?.Cancel();
        _frameService.Cancel();
    }
}
