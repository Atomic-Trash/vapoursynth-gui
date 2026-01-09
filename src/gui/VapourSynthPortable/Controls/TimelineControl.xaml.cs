using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Controls;

public partial class TimelineControl : UserControl
{
    private const double PixelsPerFrame = 5.0;
    private const double RulerTickHeight = 8;
    private const double RulerMajorTickHeight = 15;

    private bool _isDraggingPlayhead;
    private bool _isDraggingClip;
    private bool _isDraggingTextOverlay;
    private bool _isTrimmingLeft;
    private bool _isTrimmingRight;
    private TimelineClip? _draggingClip;
    private TimelineTextOverlay? _draggingOverlay;
    private Point _dragStartPoint;
    private long _dragStartFrame;

    public static readonly DependencyProperty TimelineProperty =
        DependencyProperty.Register(nameof(Timeline), typeof(Timeline), typeof(TimelineControl),
            new PropertyMetadata(null, OnTimelineChanged));

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(TimelineControl),
            new PropertyMetadata(1.0, OnZoomChanged));

    public Timeline? Timeline
    {
        get => (Timeline?)GetValue(TimelineProperty);
        set => SetValue(TimelineProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public event EventHandler<long>? PlayheadChanged;
    public event EventHandler<TimelineClip?>? ClipSelected;
    public event EventHandler<TimelineTextOverlay?>? TextOverlaySelected;
    public event EventHandler? TimelineModified;
    public event EventHandler? ScrubStarted;
    public event EventHandler? ScrubEnded;

    public TimelineControl()
    {
        InitializeComponent();
        DataContextChanged += TimelineControl_DataContextChanged;
        Loaded += TimelineControl_Loaded;
        SizeChanged += TimelineControl_SizeChanged;
    }

    private void TimelineControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is Timeline timeline)
        {
            Timeline = timeline;
        }
    }

    private void TimelineControl_Loaded(object sender, RoutedEventArgs e)
    {
        DrawRuler();
        UpdatePlayhead();
    }

    private void TimelineControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawRuler();
        UpdatePlayhead();
    }

    private static void OnTimelineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.DataContext = e.NewValue;
            control.DrawRuler();
            control.UpdatePlayhead();
            control.UpdateDurationDisplay();

            if (e.NewValue is Timeline timeline)
            {
                timeline.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(Timeline.PlayheadFrame))
                    {
                        control.UpdatePlayhead();
                    }
                    else if (args.PropertyName == nameof(Timeline.Zoom))
                    {
                        control.Zoom = timeline.Zoom;
                    }
                };
            }
        }
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.ZoomSlider.Value = (double)e.NewValue;
            control.DrawRuler();
            control.UpdatePlayhead();
            if (control.Timeline != null)
            {
                control.Timeline.Zoom = (double)e.NewValue;
            }
        }
    }

    private double FrameToPixel(long frame)
    {
        return frame * PixelsPerFrame * Zoom;
    }

    private long PixelToFrame(double pixel)
    {
        return (long)(pixel / (PixelsPerFrame * Zoom));
    }

    private void DrawRuler()
    {
        RulerCanvas.Children.Clear();

        if (Timeline == null || RulerCanvas.ActualWidth <= 0) return;

        var frameRate = Timeline.FrameRate;
        var visibleWidth = TimelineScrollViewer.ActualWidth;
        var scrollOffset = TimelineScrollViewer.HorizontalOffset;

        var startFrame = PixelToFrame(scrollOffset);
        var endFrame = PixelToFrame(scrollOffset + visibleWidth);

        // Determine tick interval based on zoom
        int framesPerTick;
        int majorTickInterval;

        if (Zoom < 0.3)
        {
            framesPerTick = (int)(frameRate * 10); // 10 seconds
            majorTickInterval = 6;
        }
        else if (Zoom < 0.6)
        {
            framesPerTick = (int)(frameRate * 5); // 5 seconds
            majorTickInterval = 6;
        }
        else if (Zoom < 1.5)
        {
            framesPerTick = (int)frameRate; // 1 second
            majorTickInterval = 5;
        }
        else if (Zoom < 3)
        {
            framesPerTick = (int)(frameRate / 2); // 0.5 seconds
            majorTickInterval = 4;
        }
        else
        {
            framesPerTick = (int)(frameRate / 4); // quarter second
            majorTickInterval = 4;
        }

        framesPerTick = Math.Max(1, framesPerTick);

        var firstTick = (startFrame / framesPerTick) * framesPerTick;

        for (long frame = firstTick; frame <= endFrame + framesPerTick; frame += framesPerTick)
        {
            var x = FrameToPixel(frame) - scrollOffset;
            var tickIndex = (int)(frame / framesPerTick);
            var isMajor = tickIndex % majorTickInterval == 0;

            var line = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = RulerCanvas.ActualHeight,
                Y2 = RulerCanvas.ActualHeight - (isMajor ? RulerMajorTickHeight : RulerTickHeight),
                Stroke = new SolidColorBrush(isMajor ? Color.FromRgb(0x88, 0x88, 0x88) : Color.FromRgb(0x55, 0x55, 0x55)),
                StrokeThickness = 1
            };
            RulerCanvas.Children.Add(line);

            if (isMajor)
            {
                var timecode = FormatTimecode(frame, frameRate);
                var text = new TextBlock
                {
                    Text = timecode,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas")
                };
                Canvas.SetLeft(text, x + 3);
                Canvas.SetTop(text, 2);
                RulerCanvas.Children.Add(text);
            }
        }
    }

    private string FormatTimecode(long frame, double frameRate)
    {
        var totalSeconds = frame / frameRate;
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = (int)(totalSeconds % 60);
        var frameInSecond = (int)(frame % frameRate);

        if (hours > 0)
            return $"{hours}:{minutes:D2}:{seconds:D2}";
        return $"{minutes}:{seconds:D2}:{frameInSecond:D2}";
    }

    private void UpdatePlayhead()
    {
        if (Timeline == null) return;

        var x = FrameToPixel(Timeline.PlayheadFrame);
        Canvas.SetLeft(PlayheadLine, x);
        Canvas.SetLeft(PlayheadHead, x);

        PlayheadLine.Y2 = TracksContainer.ActualHeight > 0 ? TracksContainer.ActualHeight : 500;
    }

    private void UpdateDurationDisplay()
    {
        if (Timeline != null)
        {
            DurationText.Text = Timeline.DurationFormatted;
        }
    }

    private void Ruler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(RulerCanvas);
        var frame = PixelToFrame(pos.X + TimelineScrollViewer.HorizontalOffset);

        if (Timeline != null)
        {
            Timeline.PlayheadFrame = Math.Max(0, frame);
            PlayheadChanged?.Invoke(this, Timeline.PlayheadFrame);
        }

        _isDraggingPlayhead = true;
        ScrubStarted?.Invoke(this, EventArgs.Empty);
        RulerCanvas.CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDraggingPlayhead && Timeline != null)
        {
            var pos = e.GetPosition(RulerCanvas);
            var frame = PixelToFrame(pos.X + TimelineScrollViewer.HorizontalOffset);
            Timeline.PlayheadFrame = Math.Max(0, frame);
            PlayheadChanged?.Invoke(this, Timeline.PlayheadFrame);
        }
        else if (_isDraggingClip && _draggingClip != null && Timeline != null)
        {
            var pos = e.GetPosition(TimelineGrid);
            var deltaX = pos.X - _dragStartPoint.X;
            var deltaFrames = PixelToFrame(deltaX);

            var newStart = Math.Max(0, _dragStartFrame + deltaFrames);
            var duration = _draggingClip.DurationFrames;

            _draggingClip.StartFrame = newStart;
            _draggingClip.EndFrame = newStart + duration;

            TimelineModified?.Invoke(this, EventArgs.Empty);
        }
        else if (_isTrimmingLeft && _draggingClip != null)
        {
            var pos = e.GetPosition(TimelineGrid);
            var frame = PixelToFrame(pos.X + TimelineScrollViewer.HorizontalOffset);

            var maxStart = _draggingClip.EndFrame - 1;
            _draggingClip.StartFrame = Math.Max(0, Math.Min(maxStart, frame));
            _draggingClip.SourceInFrame += frame - _dragStartFrame;

            TimelineModified?.Invoke(this, EventArgs.Empty);
        }
        else if (_isTrimmingRight && _draggingClip != null)
        {
            var pos = e.GetPosition(TimelineGrid);
            var frame = PixelToFrame(pos.X + TimelineScrollViewer.HorizontalOffset);

            var minEnd = _draggingClip.StartFrame + 1;
            _draggingClip.EndFrame = Math.Max(minEnd, frame);

            TimelineModified?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_isDraggingPlayhead)
        {
            _isDraggingPlayhead = false;
            RulerCanvas.ReleaseMouseCapture();
            ScrubEnded?.Invoke(this, EventArgs.Empty);
        }

        if (_isDraggingClip || _isTrimmingLeft || _isTrimmingRight)
        {
            _isDraggingClip = false;
            _isTrimmingLeft = false;
            _isTrimmingRight = false;
            _draggingClip = null;
            ReleaseMouseCapture();
            UpdateDurationDisplay();
        }
    }

    private void TrimHandle_LeftMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimelineClip clip)
        {
            _isTrimmingLeft = true;
            _draggingClip = clip;
            _dragStartPoint = e.GetPosition(TimelineGrid);
            _dragStartFrame = clip.StartFrame;

            SelectClip(clip);
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void TrimHandle_RightMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimelineClip clip)
        {
            _isTrimmingRight = true;
            _draggingClip = clip;
            _dragStartPoint = e.GetPosition(TimelineGrid);
            _dragStartFrame = clip.EndFrame;

            SelectClip(clip);
            CaptureMouse();
            e.Handled = true;
        }
    }

    public void SelectClip(TimelineClip? clip)
    {
        if (Timeline == null) return;

        // Deselect all clips
        foreach (var track in Timeline.Tracks)
        {
            foreach (var c in track.Clips)
            {
                c.IsSelected = false;
            }
        }

        // Select new clip
        if (clip != null)
        {
            clip.IsSelected = true;
        }

        Timeline.SelectedClip = clip;
        ClipSelected?.Invoke(this, clip);
    }

    private void TimelineScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Sync track header scroll with timeline scroll
        TrackHeaderScrollViewer.ScrollToVerticalOffset(TimelineScrollViewer.VerticalOffset);
        DrawRuler();
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Zoom = e.NewValue;
    }

    private void AddVideoTrack_Click(object sender, RoutedEventArgs e)
    {
        Timeline?.AddTrack(TrackType.Video);
    }

    private void AddAudioTrack_Click(object sender, RoutedEventArgs e)
    {
        Timeline?.AddTrack(TrackType.Audio);
    }

    private void Track_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimelineTrack track)
        {
            if (e.Data.GetDataPresent(typeof(MediaItem)))
            {
                var mediaItem = e.Data.GetData(typeof(MediaItem)) as MediaItem;
                if (mediaItem != null)
                {
                    AddClipToTrack(track, mediaItem, e.GetPosition(element));
                }
            }
        }
    }

    private void Track_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(MediaItem)))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    public void AddClipToTrack(TimelineTrack track, MediaItem mediaItem, Point dropPosition)
    {
        if (Timeline == null) return;

        var startFrame = PixelToFrame(dropPosition.X + TimelineScrollViewer.HorizontalOffset);
        var durationFrames = (long)(mediaItem.Duration * Timeline.FrameRate);

        if (durationFrames <= 0)
        {
            durationFrames = (long)(5 * Timeline.FrameRate); // Default 5 seconds
        }

        var clip = new TimelineClip
        {
            Name = mediaItem.Name,
            SourcePath = mediaItem.FilePath,
            TrackType = track.TrackType,
            StartFrame = startFrame,
            EndFrame = startFrame + durationFrames,
            SourceInFrame = 0,
            SourceOutFrame = durationFrames,
            SourceDurationFrames = durationFrames,
            FrameRate = Timeline.FrameRate,
            Color = track.TrackType == TrackType.Video
                ? Color.FromRgb(0x2A, 0x6A, 0x9F)  // Darker blue for video (better contrast)
                : Color.FromRgb(0x4A, 0xCF, 0x6A)  // Green for audio
        };

        track.Clips.Add(clip);
        SelectClip(clip);
        UpdateDurationDisplay();
        TimelineModified?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteSelectedClip()
    {
        Timeline?.DeleteSelectedClip();
        UpdateDurationDisplay();
        TimelineModified?.Invoke(this, EventArgs.Empty);
    }

    public void SplitClipAtPlayhead()
    {
        if (Timeline?.SelectedClip == null) return;

        var clip = Timeline.SelectedClip;
        var playhead = Timeline.PlayheadFrame;

        if (playhead <= clip.StartFrame || playhead >= clip.EndFrame) return;

        // Find the track containing this clip
        TimelineTrack? containingTrack = null;
        foreach (var track in Timeline.Tracks)
        {
            if (track.Clips.Contains(clip))
            {
                containingTrack = track;
                break;
            }
        }

        if (containingTrack == null) return;

        // Create the second clip
        var secondClip = clip.Clone();
        secondClip.StartFrame = playhead;
        secondClip.SourceInFrame = clip.SourceInFrame + (playhead - clip.StartFrame);

        // Trim the first clip
        clip.EndFrame = playhead;

        // Add the second clip
        containingTrack.Clips.Add(secondClip);

        TimelineModified?.Invoke(this, EventArgs.Empty);
    }

    // Context menu handlers

    private void ContextMenu_Cut(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimelineClip clip)
        {
            SelectClip(clip);
            ClipCutRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ContextMenu_Copy(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimelineClip clip)
        {
            SelectClip(clip);
            ClipCopyRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ContextMenu_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimelineClip clip)
        {
            SelectClip(clip);
            DeleteSelectedClip();
        }
    }

    private void ContextMenu_Split(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimelineClip clip)
        {
            SelectClip(clip);
            SplitClipAtPlayhead();
        }
    }

    private void ContextMenu_ToggleMute(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimelineClip clip)
        {
            clip.IsMuted = !clip.IsMuted;
            TimelineModified?.Invoke(this, EventArgs.Empty);
        }
    }

    // Events for parent to handle cut/copy
    public event EventHandler? ClipCutRequested;
    public event EventHandler? ClipCopyRequested;

    // Text Overlay handlers

    private void TextOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Timeline == null) return;

        var pos = e.GetPosition(TextOverlayCanvas);
        var frame = PixelToFrame(pos.X);

        // Check if clicking on an existing text overlay
        var clickedOverlay = Timeline.GetTextOverlayAtFrame(frame);
        if (clickedOverlay != null)
        {
            SelectTextOverlay(clickedOverlay);
            _isDraggingTextOverlay = true;
            _draggingOverlay = clickedOverlay;
            _dragStartPoint = pos;
            _dragStartFrame = clickedOverlay.StartFrame;
            TextOverlayCanvas.CaptureMouse();
            e.Handled = true;
        }
        else
        {
            // Deselect text overlays when clicking empty area
            SelectTextOverlay(null);
        }
    }

    public void SelectTextOverlay(TimelineTextOverlay? overlay)
    {
        if (Timeline == null) return;

        // Deselect all text overlays
        foreach (var o in Timeline.TextOverlays)
        {
            o.IsSelected = false;
        }

        // Also deselect clips when selecting text overlay
        if (overlay != null)
        {
            SelectClip(null);
        }

        // Select new overlay
        if (overlay != null)
        {
            overlay.IsSelected = true;
        }

        Timeline.SelectedTextOverlay = overlay;
        TextOverlaySelected?.Invoke(this, overlay);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (_isDraggingTextOverlay && _draggingOverlay != null && Timeline != null)
        {
            var pos = e.GetPosition(TextOverlayCanvas);
            var deltaX = pos.X - _dragStartPoint.X;
            var deltaFrames = PixelToFrame(deltaX);

            var newStart = Math.Max(0, _dragStartFrame + deltaFrames);
            _draggingOverlay.StartFrame = newStart;

            TimelineModified?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);

        if (_isDraggingTextOverlay)
        {
            _isDraggingTextOverlay = false;
            _draggingOverlay = null;
            TextOverlayCanvas.ReleaseMouseCapture();
        }
    }

    public void DeleteSelectedTextOverlay()
    {
        Timeline?.DeleteSelectedTextOverlay();
        UpdateDurationDisplay();
        TimelineModified?.Invoke(this, EventArgs.Empty);
    }
}
