using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Controls;

/// <summary>
/// Control that displays a strip of thumbnails for video clips on the timeline
/// </summary>
public partial class ClipThumbnailStrip : UserControl
{
    private readonly FrameCacheService _frameCache;
    private CancellationTokenSource? _loadCts;
    private bool _isLoading;
    private double _lastWidth;

    // Thumbnail dimensions (maintain 16:9 aspect ratio)
    private const int ThumbnailWidth = 64;
    private const int ThumbnailHeight = 36;
    private const int ThumbnailSpacing = 2;

    public static readonly DependencyProperty SourcePathProperty =
        DependencyProperty.Register(nameof(SourcePath), typeof(string), typeof(ClipThumbnailStrip),
            new PropertyMetadata(null, OnSourcePropertyChanged));

    public static readonly DependencyProperty SourceInFrameProperty =
        DependencyProperty.Register(nameof(SourceInFrame), typeof(long), typeof(ClipThumbnailStrip),
            new PropertyMetadata(0L, OnSourcePropertyChanged));

    public static readonly DependencyProperty SourceOutFrameProperty =
        DependencyProperty.Register(nameof(SourceOutFrame), typeof(long), typeof(ClipThumbnailStrip),
            new PropertyMetadata(0L, OnSourcePropertyChanged));

    public static readonly DependencyProperty FrameRateProperty =
        DependencyProperty.Register(nameof(FrameRate), typeof(double), typeof(ClipThumbnailStrip),
            new PropertyMetadata(24.0, OnSourcePropertyChanged));

    public static readonly DependencyProperty ClipDurationFramesProperty =
        DependencyProperty.Register(nameof(ClipDurationFrames), typeof(long), typeof(ClipThumbnailStrip),
            new PropertyMetadata(0L, OnSourcePropertyChanged));

    /// <summary>
    /// Path to the source video file
    /// </summary>
    public string? SourcePath
    {
        get => (string?)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    /// <summary>
    /// First frame of the clip in source file
    /// </summary>
    public long SourceInFrame
    {
        get => (long)GetValue(SourceInFrameProperty);
        set => SetValue(SourceInFrameProperty, value);
    }

    /// <summary>
    /// Last frame of the clip in source file
    /// </summary>
    public long SourceOutFrame
    {
        get => (long)GetValue(SourceOutFrameProperty);
        set => SetValue(SourceOutFrameProperty, value);
    }

    /// <summary>
    /// Frame rate of the video
    /// </summary>
    public double FrameRate
    {
        get => (double)GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    /// <summary>
    /// Duration of the clip in frames (for calculating thumbnail distribution)
    /// </summary>
    public long ClipDurationFrames
    {
        get => (long)GetValue(ClipDurationFramesProperty);
        set => SetValue(ClipDurationFramesProperty, value);
    }

    public ClipThumbnailStrip()
    {
        InitializeComponent();
        var pathResolver = App.Services?.GetService<IPathResolver>() ?? new PathResolver();
        _frameCache = new FrameCacheService(pathResolver, maxCacheSize: 200, maxConcurrentExtractions: 2);
        Loaded += (s, e) => LoadThumbnails();
        Unloaded += (s, e) => CancelLoading();
    }

    private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClipThumbnailStrip strip)
        {
            strip.LoadThumbnails();
        }
    }

    private void ThumbnailCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Only reload if width changed significantly (more than one thumbnail width)
        if (Math.Abs(e.NewSize.Width - _lastWidth) > ThumbnailWidth)
        {
            _lastWidth = e.NewSize.Width;
            LoadThumbnails();
        }
    }

    private void CancelLoading()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }

    private async void LoadThumbnails()
    {
        if (string.IsNullOrEmpty(SourcePath) || !System.IO.File.Exists(SourcePath))
        {
            ThumbnailCanvas.Children.Clear();
            DrawPlaceholder();
            return;
        }

        if (_isLoading) return;

        double width = ThumbnailCanvas.ActualWidth;
        double height = ThumbnailCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        CancelLoading();
        _loadCts = new CancellationTokenSource();
        _isLoading = true;

        try
        {
            await LoadThumbnailsInternalAsync(width, height, _loadCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ignore - loading was cancelled
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadThumbnailsInternalAsync(double width, double height, CancellationToken ct)
    {
        ThumbnailCanvas.Children.Clear();

        // Calculate how many thumbnails can fit
        int thumbnailCount = Math.Max(1, (int)(width / (ThumbnailWidth + ThumbnailSpacing)));
        thumbnailCount = Math.Min(thumbnailCount, 20); // Cap at 20 thumbnails

        // Calculate thumbnail height based on available space
        int thumbHeight = Math.Min(ThumbnailHeight, (int)height - 4);
        int thumbWidth = (int)(thumbHeight * 16.0 / 9.0); // Maintain aspect ratio

        // Calculate frame positions for thumbnails
        long sourceDuration = SourceOutFrame > SourceInFrame
            ? SourceOutFrame - SourceInFrame
            : ClipDurationFrames;

        if (sourceDuration <= 0) sourceDuration = 1;

        var framePositions = new List<long>();
        for (int i = 0; i < thumbnailCount; i++)
        {
            // Distribute thumbnails evenly across the clip
            double position = (double)i / Math.Max(1, thumbnailCount - 1);
            long frame = SourceInFrame + (long)(position * sourceDuration);
            framePositions.Add(frame);
        }

        // Calculate spacing
        double spacing = thumbnailCount > 1
            ? (width - thumbnailCount * thumbWidth) / (thumbnailCount - 1)
            : 0;
        spacing = Math.Max(ThumbnailSpacing, spacing);

        // Load thumbnails
        var loadTasks = new List<Task<(int index, BitmapSource? frame)>>();

        for (int i = 0; i < framePositions.Count; i++)
        {
            int index = i;
            long frame = framePositions[i];

            // Try cache first
            var cached = _frameCache.TryGetFrame(SourcePath!, frame, thumbWidth, thumbHeight);
            if (cached != null)
            {
                AddThumbnailToCanvas(cached, index, thumbWidth, thumbHeight, spacing);
            }
            else
            {
                // Queue async load
                loadTasks.Add(LoadSingleThumbnailAsync(index, frame, thumbWidth, thumbHeight, ct));
            }
        }

        // Wait for async loads
        foreach (var task in loadTasks)
        {
            ct.ThrowIfCancellationRequested();

            var (index, frame) = await task;
            if (frame != null)
            {
                AddThumbnailToCanvas(frame, index, thumbWidth, thumbHeight, spacing);
            }
        }
    }

    private async Task<(int index, BitmapSource? frame)> LoadSingleThumbnailAsync(
        int index, long frameNumber, int width, int height, CancellationToken ct)
    {
        try
        {
            var frame = await _frameCache.GetFrameAsync(
                SourcePath!,
                frameNumber,
                FrameRate,
                width,
                height,
                ct);
            return (index, frame);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Thumbnail extraction failed for frame {index}: {ex.Message}");
            return (index, null);
        }
    }

    private void AddThumbnailToCanvas(BitmapSource frame, int index, int thumbWidth, int thumbHeight, double spacing)
    {
        var image = new Image
        {
            Source = frame,
            Width = thumbWidth,
            Height = thumbHeight,
            Stretch = Stretch.UniformToFill
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);

        // Add subtle border
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            BorderThickness = new Thickness(1),
            Child = image
        };

        double x = index * (thumbWidth + spacing);
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, 2);

        // Insert in order (may arrive out of order due to async)
        int insertIndex = 0;
        foreach (UIElement child in ThumbnailCanvas.Children)
        {
            if (Canvas.GetLeft(child) < x)
                insertIndex++;
            else
                break;
        }

        if (insertIndex >= ThumbnailCanvas.Children.Count)
            ThumbnailCanvas.Children.Add(border);
        else
            ThumbnailCanvas.Children.Insert(insertIndex, border);
    }

    private void DrawPlaceholder()
    {
        double width = ThumbnailCanvas.ActualWidth;
        double height = ThumbnailCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        // Draw a subtle placeholder pattern
        var placeholder = new Border
        {
            Width = width,
            Height = height - 4,
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            CornerRadius = new CornerRadius(2)
        };

        Canvas.SetTop(placeholder, 2);
        ThumbnailCanvas.Children.Add(placeholder);
    }

    /// <summary>
    /// Force refresh of thumbnails
    /// </summary>
    public void Refresh()
    {
        _frameCache.ClearCacheForFile(SourcePath ?? "");
        LoadThumbnails();
    }
}
