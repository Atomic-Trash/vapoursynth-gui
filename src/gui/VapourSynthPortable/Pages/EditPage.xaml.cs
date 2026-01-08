using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class EditPage : UserControl
{
    private bool _syncingFromPlayer;
    private bool _syncingFromTimeline;
    private string? _currentProgramSource;
    private string? _currentSourceMonitor;

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

            // Wire up program player position sync
            if (ProgramPlayer.Player != null)
            {
                ProgramPlayer.Player.PositionChanged += ProgramPlayer_PositionChanged;
            }
        }
    }

    private void EditPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.Timeline.PropertyChanged -= Timeline_PropertyChanged;
        }

        if (ProgramPlayer.Player != null)
        {
            ProgramPlayer.Player.PositionChanged -= ProgramPlayer_PositionChanged;
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

    private void HandlePlaybackStateChange()
    {
        if (DataContext is not EditViewModel viewModel) return;

        if (viewModel.IsPlaying)
        {
            // Load first clip on timeline if not already loaded
            LoadProgramMonitorVideo();
            ProgramPlayer.Play();
        }
        else
        {
            ProgramPlayer.Pause();
        }
    }

    private void LoadProgramMonitorVideo()
    {
        if (DataContext is not EditViewModel viewModel) return;

        // Find the clip at the current playhead position
        var clip = viewModel.Timeline.GetClipAtFrame(viewModel.Timeline.PlayheadFrame);
        if (clip != null && clip.SourcePath != _currentProgramSource)
        {
            _currentProgramSource = clip.SourcePath;
            ProgramPlayer.LoadFile(clip.SourcePath);

            // Seek to the correct position within the clip
            var frameInClip = viewModel.Timeline.PlayheadFrame - clip.StartFrame + clip.SourceInFrame;
            var seconds = frameInClip / viewModel.Timeline.FrameRate;
            ProgramPlayer.Seek(seconds);
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

                // Calculate position within the clip
                var frameInClip = viewModel.Timeline.PlayheadFrame - clip.StartFrame + clip.SourceInFrame;
                var seconds = frameInClip / viewModel.Timeline.FrameRate;
                ProgramPlayer.Seek(seconds);
            }
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
}
