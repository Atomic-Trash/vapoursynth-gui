using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class EditPage : UserControl
{
    private static readonly ILogger<EditPage> _logger = LoggingService.GetLogger<EditPage>();
    private bool _syncingFromPlayer;
    private bool _syncingFromTimeline;
    private string? _currentProgramSource;
    private string? _currentSourceMonitor;

    // Playback timer for smooth playhead movement
    private DispatcherTimer? _playbackTimer;
    private DateTime _playbackStartTime;
    private long _playbackStartFrame;

    public EditPage()
    {
        InitializeComponent();

        // Get ViewModel from DI to ensure shared MediaPoolService singleton
        DataContext = App.Services?.GetService(typeof(EditViewModel))
            ?? new EditViewModel();

        Loaded += EditPage_Loaded;
        Unloaded += EditPage_Unloaded;
        IsVisibleChanged += EditPage_IsVisibleChanged;
    }

    private void EditPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            // Subscribe to ViewModel property changes
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Subscribe to timeline property changes for playhead sync
            viewModel.Timeline.PropertyChanged += Timeline_PropertyChanged;

            // Subscribe to clip collection changes on all tracks
            foreach (var track in viewModel.Timeline.Tracks)
            {
                track.Clips.CollectionChanged += (s, args) =>
                {
                    _logger.LogInformation("Clips collection changed on track. HasClips={HasClips}, ProgramPlayer.Visibility={Visibility}, ProgramPlayer.IsVisible={IsVisible}",
                        viewModel.Timeline.HasClips, ProgramPlayer.Visibility, ProgramPlayer.IsVisible);
                    LoadProgramMonitorVideo();
                };
            }

            // Also subscribe to track collection changes
            viewModel.Timeline.Tracks.CollectionChanged += (s, args) =>
            {
                _logger.LogDebug("Tracks collection changed");
                if (args.NewItems != null)
                {
                    foreach (TimelineTrack track in args.NewItems)
                    {
                        track.Clips.CollectionChanged += (s2, args2) =>
                        {
                            _logger.LogInformation("Clips collection changed (new track). HasClips={HasClips}, ProgramPlayer.Visibility={Visibility}",
                                viewModel.Timeline.HasClips, ProgramPlayer.Visibility);
                            LoadProgramMonitorVideo();
                        };
                    }
                }
                LoadProgramMonitorVideo();
            };

            // Wire up program player position sync
            if (ProgramPlayer.Player != null)
            {
                ProgramPlayer.Player.PositionChanged += ProgramPlayer_PositionChanged;
            }

            // Wire up timeline context menu events
            TimelineControl.ClipCutRequested += TimelineControl_ClipCutRequested;
            TimelineControl.ClipCopyRequested += TimelineControl_ClipCopyRequested;

            // Wire up scrub events for frame preview
            TimelineControl.ScrubStarted += TimelineControl_ScrubStarted;
            TimelineControl.ScrubEnded += TimelineControl_ScrubEnded;

            // Load program monitor if there are clips
            LoadProgramMonitorVideo();
        }
    }

    private void EditPage_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Stop playback timer
            StopPlaybackTimer();

            if (DataContext is EditViewModel viewModel)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.Timeline.PropertyChanged -= Timeline_PropertyChanged;
            }

            if (ProgramPlayer?.Player != null)
            {
                ProgramPlayer.Player.PositionChanged -= ProgramPlayer_PositionChanged;
            }

            // Unsubscribe from timeline context menu events
            if (TimelineControl != null)
            {
                TimelineControl.ClipCutRequested -= TimelineControl_ClipCutRequested;
                TimelineControl.ClipCopyRequested -= TimelineControl_ClipCopyRequested;
                TimelineControl.ScrubStarted -= TimelineControl_ScrubStarted;
                TimelineControl.ScrubEnded -= TimelineControl_ScrubEnded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during EditPage unload cleanup");
        }
    }

    private void EditPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // When page becomes visible, check if there's a source to load
        if ((bool)e.NewValue && DataContext is EditViewModel viewModel)
        {
            if (viewModel.SourceMonitorItem != null)
            {
                var filePath = viewModel.SourceMonitorItem.FilePath;
                if (!string.IsNullOrEmpty(filePath) &&
                    viewModel.SourceMonitorItem.MediaType == MediaType.Video &&
                    _currentSourceMonitor != filePath)
                {
                    _currentSourceMonitor = filePath;
                    SourcePlayer.LoadFile(filePath);
                }
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditViewModel.SourceMonitorItem))
        {
            LoadSourceMonitorVideo();
        }
        else if (e.PropertyName == nameof(EditViewModel.IsPlaying))
        {
            HandlePlaybackStateChange();
        }
    }

    private void LoadSourceMonitorVideo()
    {
        try
        {
            if (DataContext is EditViewModel viewModel && viewModel.SourceMonitorItem != null)
            {
                var filePath = viewModel.SourceMonitorItem.FilePath;
                if (!string.IsNullOrEmpty(filePath) &&
                    viewModel.SourceMonitorItem.MediaType == MediaType.Video &&
                    _currentSourceMonitor != filePath)
                {
                    _currentSourceMonitor = filePath;
                    SourcePlayer.LoadFile(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load source monitor video");
            ToastService.Instance.ShowError("Failed to load source", ex.Message);
        }
    }

    private async void HandlePlaybackStateChange()
    {
        try
        {
            if (DataContext is not EditViewModel viewModel) return;

            if (viewModel.IsPlaying)
            {
                // Load the clip at current playhead position
                var clip = viewModel.Timeline.GetClipAtFrame(viewModel.Timeline.PlayheadFrame);
                if (clip != null)
                {
                    // Ensure the clip is loaded
                    if (clip.SourcePath != _currentProgramSource)
                    {
                        _currentProgramSource = clip.SourcePath;
                        ProgramPlayer.LoadFile(clip.SourcePath);
                    }

                    // Wait for player to be ready before seeking/playing
                    if (!ProgramPlayer.IsPlayerReady)
                    {
                        await ProgramPlayer.WaitForInitializationAsync();
                    }

                    // Seek to correct position in clip before playing
                    var frameInClip = CalculateFrameInClip(clip, viewModel.Timeline.PlayheadFrame);
                    var seconds = Math.Max(0, frameInClip / viewModel.Timeline.FrameRate);
                    ProgramPlayer.Seek(seconds);
                }

                // Start video playback and timeline playhead timer
                ProgramPlayer.Play();
                StartPlaybackTimer(viewModel);
            }
            else
            {
                ProgramPlayer.Pause();
                StopPlaybackTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change playback state");
        }
    }

    private void StartPlaybackTimer(EditViewModel viewModel)
    {
        StopPlaybackTimer(); // Ensure no duplicate timers

        _playbackStartTime = DateTime.Now;
        _playbackStartFrame = viewModel.Timeline.PlayheadFrame;

        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps update rate
        };
        _playbackTimer.Tick += PlaybackTimer_Tick;
        _playbackTimer.Start();
    }

    private void StopPlaybackTimer()
    {
        if (_playbackTimer != null)
        {
            _playbackTimer.Stop();
            _playbackTimer.Tick -= PlaybackTimer_Tick;
            _playbackTimer = null;
        }
    }

    /// <summary>
    /// Safely calculates the frame position within a clip, clamping to valid bounds.
    /// </summary>
    private static long CalculateFrameInClip(TimelineClip clip, long playheadFrame)
    {
        var frameInClip = playheadFrame - clip.StartFrame + clip.SourceInFrame;
        var maxFrame = clip.SourceOutFrame > 0
            ? clip.SourceOutFrame - 1
            : clip.SourceDurationFrames - 1;
        return Math.Max(clip.SourceInFrame, Math.Min(frameInClip, Math.Max(0, maxFrame)));
    }

    private void PlaybackTimer_Tick(object? sender, EventArgs e)
    {
        if (DataContext is not EditViewModel viewModel) return;
        if (!viewModel.IsPlaying)
        {
            StopPlaybackTimer();
            return;
        }

        var elapsed = (DateTime.Now - _playbackStartTime).TotalSeconds;
        var newFrame = _playbackStartFrame + (long)(elapsed * viewModel.Timeline.FrameRate);

        // Clamp to timeline duration
        var maxFrame = viewModel.Timeline.DurationFrames;
        if (maxFrame > 0 && newFrame >= maxFrame)
        {
            newFrame = maxFrame - 1;
            viewModel.IsPlaying = false; // Stop at end
        }

        // Update playhead without triggering player sync (we're already playing)
        _syncingFromPlayer = true;
        try
        {
            viewModel.Timeline.PlayheadFrame = newFrame;
        }
        finally
        {
            _syncingFromPlayer = false;
        }
    }

    private void LoadProgramMonitorVideo()
    {
        try
        {
            if (DataContext is not EditViewModel viewModel) return;

            // Find the clip at the current playhead position
            var clip = viewModel.Timeline.GetClipAtFrame(viewModel.Timeline.PlayheadFrame);
            if (clip != null)
            {
                // Load new clip if source changed (LoadFile queues if player not ready)
                if (clip.SourcePath != _currentProgramSource)
                {
                    _currentProgramSource = clip.SourcePath;
                    ProgramPlayer.LoadFile(clip.SourcePath);
                }

                // Only seek when player is ready and not currently playing
                if (!viewModel.IsPlaying && ProgramPlayer.IsPlayerReady)
                {
                    var frameInClip = CalculateFrameInClip(clip, viewModel.Timeline.PlayheadFrame);
                    var seconds = Math.Max(0, frameInClip / viewModel.Timeline.FrameRate);
                    ProgramPlayer.Seek(seconds);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load program monitor video");
            ToastService.Instance.ShowError("Failed to load timeline video", ex.Message);
        }
    }

    private void ProgramPlayer_PositionChanged(object? sender, double seconds)
    {
        if (_syncingFromTimeline || DataContext is not EditViewModel viewModel) return;

        _syncingFromPlayer = true;
        try
        {
            // Convert seconds to frame
            var frame = (long)(seconds * viewModel.Timeline.FrameRate);
            viewModel.Timeline.PlayheadFrame = frame;
        }
        finally
        {
            _syncingFromPlayer = false;
        }
    }

    private void Timeline_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Timeline.PlayheadFrame) && !_syncingFromPlayer)
        {
            SyncPlayerToTimeline();
            LoadProgramMonitorVideo();
        }
    }

    private void SyncPlayerToTimeline()
    {
        if (DataContext is not EditViewModel viewModel) return;

        _syncingFromTimeline = true;
        try
        {
            // Check if we need to load a different clip
            var clip = viewModel.Timeline.GetClipAtFrame(viewModel.Timeline.PlayheadFrame);

            if (clip != null)
            {
                if (clip.SourcePath != _currentProgramSource)
                {
                    _currentProgramSource = clip.SourcePath;
                    ProgramPlayer.LoadFile(clip.SourcePath);
                }

                // Only seek if player is ready (file load is queued otherwise)
                if (ProgramPlayer.IsPlayerReady)
                {
                    // Calculate position within the clip (clamped to valid bounds)
                    var frameInClip = CalculateFrameInClip(clip, viewModel.Timeline.PlayheadFrame);
                    var seconds = Math.Max(0, frameInClip / viewModel.Timeline.FrameRate);
                    ProgramPlayer.Seek(seconds);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync player to timeline position");
        }
        finally
        {
            _syncingFromTimeline = false;
        }
    }

    private void MediaItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (sender is FrameworkElement element && element.DataContext is MediaItem mediaItem)
            {
                // Start drag operation
                var data = new DataObject(typeof(MediaItem), mediaItem);
                DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
            }
        }
    }

    private void TimelineControl_ClipSelected(object? sender, TimelineClip? clip)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.Timeline.SelectedClip = clip;
        }
    }

    private void TimelineControl_ClipCutRequested(object? sender, EventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.CutClipCommand.Execute(null);
        }
    }

    private void TimelineControl_ClipCopyRequested(object? sender, EventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.CopyClipCommand.Execute(null);
        }
    }

    private void TimelineControl_ScrubStarted(object? sender, EventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.BeginScrub();
        }
    }

    private void TimelineControl_ScrubEnded(object? sender, EventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.EndScrub();
        }
    }
}
