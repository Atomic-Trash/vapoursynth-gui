using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class EditViewModel : ObservableObject, IDisposable
{
    private readonly IMediaPoolService _mediaPoolService;
    private readonly Stack<TimelineAction> _undoStack = new();
    private readonly Stack<TimelineAction> _redoStack = new();
    private bool _disposed;

    [ObservableProperty]
    private Timeline _timeline = new();

    // Media pool is now shared across all pages via IMediaPoolService
    public ObservableCollection<MediaItem> MediaPool => _mediaPoolService.MediaPool;

    [ObservableProperty]
    private MediaItem? _selectedMediaItem;

    [ObservableProperty]
    private MediaItem? _sourceMonitorItem;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _sourceInPoint;

    [ObservableProperty]
    private double _sourceOutPoint;

    [ObservableProperty]
    private string _currentTimecode = "00:00:00:00";

    [ObservableProperty]
    private bool _snapToClips = true;

    [ObservableProperty]
    private bool _rippleEdit;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    [ObservableProperty]
    private ObservableCollection<TransitionPreset> _transitionPresets = [];

    [ObservableProperty]
    private TransitionPreset? _selectedTransitionPreset;

    public EditViewModel(IMediaPoolService mediaPoolService)
    {
        _mediaPoolService = mediaPoolService;
        _mediaPoolService.CurrentSourceChanged += OnCurrentSourceChanged;
        _mediaPoolService.MediaPoolChanged += OnMediaPoolChanged;

        InitializeTimeline();
        LoadTransitionPresets();
    }

    // Parameterless constructor for XAML design-time support
    public EditViewModel() : this(App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService
        ?? new MediaPoolService())
    {
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        // When current source changes (from any page), update the source monitor
        if (item != null)
        {
            SourceMonitorItem = item;
            SourceInPoint = 0;
            SourceOutPoint = item.Duration;
            SelectedMediaItem = item;
            StatusText = $"Source: {item.Name}";
        }
    }

    private void OnMediaPoolChanged(object? sender, EventArgs e)
    {
        // Notify UI that the media pool has changed (media imported from another page)
        OnPropertyChanged(nameof(MediaPool));
    }

    private void LoadTransitionPresets()
    {
        var presets = TimelineTransition.GetPresets();
        foreach (var preset in presets)
        {
            TransitionPresets.Add(preset);
        }
        SelectedTransitionPreset = TransitionPresets.FirstOrDefault(p => p.Type == TransitionType.CrossDissolve);
    }

    private void InitializeTimeline()
    {
        // Create default tracks
        Timeline.AddTrack(TrackType.Video);
        Timeline.AddTrack(TrackType.Video);
        Timeline.AddTrack(TrackType.Audio);
        Timeline.AddTrack(TrackType.Audio);

        Timeline.FrameRate = 24;
    }

    [RelayCommand]
    private async Task ImportMedia()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Media",
            Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.mxf;*.m2ts|" +
                     "Audio Files|*.mp3;*.wav;*.aac;*.flac;*.ogg|" +
                     "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|" +
                     "All Files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            // Import via the shared service
            await _mediaPoolService.ImportMediaAsync(dialog.FileNames);
            StatusText = $"Imported {dialog.FileNames.Length} file(s)";
        }
    }

    partial void OnSelectedMediaItemChanged(MediaItem? value)
    {
        if (value != null)
        {
            // Set as current source in the shared service (updates all pages)
            _mediaPoolService.SetCurrentSource(value);
            SourceMonitorItem = value;
            SourceInPoint = 0;
            SourceOutPoint = value.Duration;
            StatusText = $"Selected: {value.Name}";
        }
    }

    [RelayCommand]
    private void AddToTimeline()
    {
        if (SelectedMediaItem == null) return;

        // Find first appropriate track
        TimelineTrack? targetTrack = null;
        var trackType = SelectedMediaItem.MediaType == MediaType.Audio ? TrackType.Audio : TrackType.Video;

        foreach (var track in Timeline.Tracks)
        {
            if (track.TrackType == trackType && !track.IsLocked)
            {
                targetTrack = track;
                break;
            }
        }

        if (targetTrack == null)
        {
            StatusText = "No available track for this media type";
            return;
        }

        // Calculate insert position (at playhead or end of track)
        long startFrame = Timeline.PlayheadFrame;
        if (startFrame == 0 && targetTrack.Clips.Count > 0)
        {
            startFrame = targetTrack.Clips.Max(c => c.EndFrame);
        }

        var durationFrames = (long)((SourceOutPoint - SourceInPoint) * Timeline.FrameRate);
        if (durationFrames <= 0)
        {
            durationFrames = (long)(SelectedMediaItem.Duration * Timeline.FrameRate);
        }
        if (durationFrames <= 0)
        {
            durationFrames = (long)(5 * Timeline.FrameRate); // Default 5 seconds
        }

        var clip = new TimelineClip
        {
            Name = SelectedMediaItem.Name,
            SourcePath = SelectedMediaItem.FilePath,
            TrackType = trackType,
            StartFrame = startFrame,
            EndFrame = startFrame + durationFrames,
            SourceInFrame = (long)(SourceInPoint * Timeline.FrameRate),
            SourceOutFrame = (long)(SourceOutPoint * Timeline.FrameRate),
            SourceDurationFrames = (long)(SelectedMediaItem.Duration * Timeline.FrameRate),
            FrameRate = Timeline.FrameRate,
            Color = trackType == TrackType.Video
                ? System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xCF)
                : System.Windows.Media.Color.FromRgb(0x4A, 0xCF, 0x6A)
        };

        SaveUndoState("Add clip");
        targetTrack.Clips.Add(clip);
        Timeline.SelectedClip = clip;
        StatusText = $"Added {clip.Name} to {targetTrack.Name}";
    }

    [RelayCommand]
    private void InsertClip()
    {
        // Similar to AddToTimeline but at playhead position, shifting other clips
        if (SelectedMediaItem == null) return;

        var trackType = SelectedMediaItem.MediaType == MediaType.Audio ? TrackType.Audio : TrackType.Video;
        TimelineTrack? targetTrack = null;

        foreach (var track in Timeline.Tracks)
        {
            if (track.TrackType == trackType && !track.IsLocked)
            {
                targetTrack = track;
                break;
            }
        }

        if (targetTrack == null) return;

        var durationFrames = (long)(SelectedMediaItem.Duration * Timeline.FrameRate);
        if (durationFrames <= 0) durationFrames = (long)(5 * Timeline.FrameRate);

        SaveUndoState("Insert clip");

        // Shift clips after playhead
        foreach (var clip in targetTrack.Clips.Where(c => c.StartFrame >= Timeline.PlayheadFrame))
        {
            clip.StartFrame += durationFrames;
            clip.EndFrame += durationFrames;
        }

        var newClip = new TimelineClip
        {
            Name = SelectedMediaItem.Name,
            SourcePath = SelectedMediaItem.FilePath,
            TrackType = trackType,
            StartFrame = Timeline.PlayheadFrame,
            EndFrame = Timeline.PlayheadFrame + durationFrames,
            SourceDurationFrames = durationFrames,
            FrameRate = Timeline.FrameRate,
            Color = trackType == TrackType.Video
                ? System.Windows.Media.Color.FromRgb(0x4A, 0x9E, 0xCF)
                : System.Windows.Media.Color.FromRgb(0x4A, 0xCF, 0x6A)
        };

        targetTrack.Clips.Add(newClip);
        Timeline.SelectedClip = newClip;
        StatusText = "Clip inserted";
    }

    [RelayCommand]
    private void DeleteClip()
    {
        if (Timeline.SelectedClip == null) return;

        SaveUndoState("Delete clip");
        var clipName = Timeline.SelectedClip.Name;
        Timeline.DeleteSelectedClip();
        StatusText = $"Deleted {clipName}";
    }

    [RelayCommand]
    private void SplitClip()
    {
        if (Timeline.SelectedClip == null) return;

        var clip = Timeline.SelectedClip;
        if (Timeline.PlayheadFrame <= clip.StartFrame || Timeline.PlayheadFrame >= clip.EndFrame)
        {
            StatusText = "Playhead must be within the selected clip to split";
            return;
        }

        SaveUndoState("Split clip");

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

        var secondClip = clip.Clone();
        secondClip.StartFrame = Timeline.PlayheadFrame;
        secondClip.SourceInFrame = clip.SourceInFrame + (Timeline.PlayheadFrame - clip.StartFrame);

        clip.EndFrame = Timeline.PlayheadFrame;

        containingTrack.Clips.Add(secondClip);
        StatusText = "Clip split at playhead";
        ToastService.Instance.ShowSuccess("Split", clip.Name);
    }

    [RelayCommand]
    private void CutClip()
    {
        if (Timeline.SelectedClip == null) return;

        // Copy to clipboard (simplified - just store reference)
        _clipboardClip = Timeline.SelectedClip.Clone();

        SaveUndoState("Cut clip");
        var clipName = Timeline.SelectedClip.Name;
        Timeline.DeleteSelectedClip();
        StatusText = $"Cut {clipName}";
        ToastService.Instance.ShowInfo("Cut", clipName);
    }

    [RelayCommand]
    private void CopyClip()
    {
        if (Timeline.SelectedClip == null) return;

        _clipboardClip = Timeline.SelectedClip.Clone();
        StatusText = $"Copied {Timeline.SelectedClip.Name}";
        ToastService.Instance.ShowInfo("Copied", Timeline.SelectedClip.Name);
    }

    private TimelineClip? _clipboardClip;

    [RelayCommand]
    private void PasteClip()
    {
        if (_clipboardClip == null) return;

        TimelineTrack? targetTrack = null;
        foreach (var track in Timeline.Tracks)
        {
            if (track.TrackType == _clipboardClip.TrackType && !track.IsLocked)
            {
                targetTrack = track;
                break;
            }
        }

        if (targetTrack == null) return;

        SaveUndoState("Paste clip");

        var newClip = _clipboardClip.Clone();
        var duration = newClip.DurationFrames;
        newClip.StartFrame = Timeline.PlayheadFrame;
        newClip.EndFrame = Timeline.PlayheadFrame + duration;

        targetTrack.Clips.Add(newClip);
        Timeline.SelectedClip = newClip;
        StatusText = "Pasted clip";
        ToastService.Instance.ShowSuccess("Pasted", newClip.Name);
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var action = _undoStack.Pop();
        _redoStack.Push(CreateCurrentState("Redo"));

        RestoreState(action);
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        StatusText = $"Undo: {action.Description}";
        ToastService.Instance.ShowInfo("Undo", action.Description);
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        var action = _redoStack.Pop();
        _undoStack.Push(CreateCurrentState("Undo"));

        RestoreState(action);
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        StatusText = $"Redo: {action.Description}";
        ToastService.Instance.ShowInfo("Redo", action.Description);
    }

    private void SaveUndoState(string description)
    {
        _undoStack.Push(CreateCurrentState(description));
        _redoStack.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private TimelineAction CreateCurrentState(string description)
    {
        var clips = new List<(int TrackId, TimelineClip Clip)>();
        foreach (var track in Timeline.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                clips.Add((track.Id, clip.Clone()));
            }
        }

        var textOverlays = new List<TimelineTextOverlay>();
        foreach (var overlay in Timeline.TextOverlays)
        {
            textOverlays.Add(overlay.Clone());
        }

        return new TimelineAction
        {
            Description = description,
            Clips = clips,
            TextOverlays = textOverlays
        };
    }

    private void RestoreState(TimelineAction action)
    {
        // Clear all clips
        foreach (var track in Timeline.Tracks)
        {
            track.Clips.Clear();
        }

        // Restore clips
        foreach (var (trackId, clip) in action.Clips)
        {
            var track = Timeline.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null)
            {
                track.Clips.Add(clip);
            }
        }

        // Clear and restore text overlays
        Timeline.TextOverlays.Clear();
        foreach (var overlay in action.TextOverlays)
        {
            Timeline.TextOverlays.Add(overlay);
        }

        Timeline.SelectedClip = null;
        Timeline.SelectedTextOverlay = null;
    }

    [RelayCommand]
    private void Play()
    {
        IsPlaying = !IsPlaying;
        StatusText = IsPlaying ? "Playing" : "Paused";
    }

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        Timeline.PlayheadFrame = 0;
        StatusText = "Stopped";
    }

    [RelayCommand]
    private void GoToStart()
    {
        Timeline.PlayheadFrame = 0;
        UpdateCurrentTimecode();
    }

    [RelayCommand]
    private void GoToEnd()
    {
        Timeline.PlayheadFrame = Timeline.DurationFrames;
        UpdateCurrentTimecode();
    }

    [RelayCommand]
    private void StepForward()
    {
        Timeline.PlayheadFrame++;
        UpdateCurrentTimecode();
    }

    [RelayCommand]
    private void StepBackward()
    {
        if (Timeline.PlayheadFrame > 0)
            Timeline.PlayheadFrame--;
        UpdateCurrentTimecode();
    }

    [RelayCommand]
    private void SetInPoint()
    {
        Timeline.InPoint = Timeline.PlayheadFrame;
        StatusText = $"In point set at {Timeline.PlayheadTimecode}";
    }

    [RelayCommand]
    private void SetOutPoint()
    {
        Timeline.OutPoint = Timeline.PlayheadFrame;
        StatusText = $"Out point set at {Timeline.PlayheadTimecode}";
    }

    [RelayCommand]
    private void ClearInOutPoints()
    {
        Timeline.InPoint = -1;
        Timeline.OutPoint = -1;
        StatusText = "In/Out points cleared";
    }

    private void UpdateCurrentTimecode()
    {
        CurrentTimecode = Timeline.PlayheadTimecode;
    }

    [RelayCommand]
    private void AddVideoTrack()
    {
        Timeline.AddTrack(TrackType.Video);
        StatusText = "Added video track";
    }

    [RelayCommand]
    private void AddAudioTrack()
    {
        Timeline.AddTrack(TrackType.Audio);
        StatusText = "Added audio track";
    }

    [RelayCommand]
    private void DeleteTrack()
    {
        if (Timeline.SelectedTrack != null && Timeline.Tracks.Count > 1)
        {
            SaveUndoState("Delete track");
            Timeline.RemoveTrack(Timeline.SelectedTrack);
            StatusText = "Track deleted";
        }
    }

    [RelayCommand]
    private void AddTransition()
    {
        if (Timeline.SelectedClip == null || SelectedTransitionPreset == null) return;

        var clip = Timeline.SelectedClip;

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

        // Find adjacent clip to transition to
        var sortedClips = containingTrack.Clips.OrderBy(c => c.StartFrame).ToList();
        var clipIndex = sortedClips.IndexOf(clip);

        TimelineClip? adjacentClip = null;
        bool transitionAtStart = false;

        // Check if there's a clip before or after
        if (clipIndex > 0)
        {
            var prevClip = sortedClips[clipIndex - 1];
            if (prevClip.EndFrame == clip.StartFrame || Math.Abs(prevClip.EndFrame - clip.StartFrame) <= SelectedTransitionPreset.DefaultDuration)
            {
                adjacentClip = prevClip;
                transitionAtStart = true;
            }
        }

        if (adjacentClip == null && clipIndex < sortedClips.Count - 1)
        {
            var nextClip = sortedClips[clipIndex + 1];
            if (nextClip.StartFrame == clip.EndFrame || Math.Abs(nextClip.StartFrame - clip.EndFrame) <= SelectedTransitionPreset.DefaultDuration)
            {
                adjacentClip = nextClip;
                transitionAtStart = false;
            }
        }

        if (adjacentClip == null)
        {
            StatusText = "No adjacent clip found for transition";
            return;
        }

        SaveUndoState("Add transition");

        var transition = new TimelineTransition
        {
            Name = SelectedTransitionPreset.Name,
            TransitionType = SelectedTransitionPreset.Type,
            WipeDirection = SelectedTransitionPreset.Direction,
            DurationFrames = SelectedTransitionPreset.DefaultDuration,
            ClipA = transitionAtStart ? adjacentClip : clip,
            ClipB = transitionAtStart ? clip : adjacentClip,
            StartFrame = transitionAtStart
                ? clip.StartFrame - SelectedTransitionPreset.DefaultDuration / 2
                : clip.EndFrame - SelectedTransitionPreset.DefaultDuration / 2
        };

        containingTrack.Transitions.Add(transition);
        StatusText = $"Added {transition.DisplayName} transition";
    }

    [RelayCommand]
    private void RemoveTransition(TimelineTransition? transition)
    {
        if (transition == null) return;

        foreach (var track in Timeline.Tracks)
        {
            if (track.Transitions.Contains(transition))
            {
                SaveUndoState("Remove transition");
                track.Transitions.Remove(transition);
                StatusText = "Transition removed";
                return;
            }
        }
    }

    [RelayCommand]
    private void ApplyDefaultTransition()
    {
        // Apply cross dissolve at playhead between two clips
        foreach (var track in Timeline.Tracks.Where(t => t.TrackType == TrackType.Video))
        {
            var sortedClips = track.Clips.OrderBy(c => c.StartFrame).ToList();

            for (int i = 0; i < sortedClips.Count - 1; i++)
            {
                var clipA = sortedClips[i];
                var clipB = sortedClips[i + 1];

                // Check if playhead is at the cut point
                if (Math.Abs(Timeline.PlayheadFrame - clipA.EndFrame) < 5 ||
                    Math.Abs(Timeline.PlayheadFrame - clipB.StartFrame) < 5)
                {
                    // Check if transition already exists
                    var existingTransition = track.Transitions.FirstOrDefault(t =>
                        t.ClipA == clipA && t.ClipB == clipB);

                    if (existingTransition != null)
                    {
                        StatusText = "Transition already exists at this point";
                        return;
                    }

                    SaveUndoState("Add default transition");

                    var transition = new TimelineTransition
                    {
                        Name = "Cross Dissolve",
                        TransitionType = TransitionType.CrossDissolve,
                        DurationFrames = 24,
                        ClipA = clipA,
                        ClipB = clipB,
                        StartFrame = clipA.EndFrame - 12
                    };

                    track.Transitions.Add(transition);
                    StatusText = "Added Cross Dissolve";
                    return;
                }
            }
        }

        StatusText = "Position playhead at a cut point to add transition";
    }

    // Text Overlay Commands

    [RelayCommand]
    private void AddTextOverlay()
    {
        SaveUndoState("Add text overlay");

        var overlay = new TimelineTextOverlay
        {
            Text = "New Text",
            StartFrame = Timeline.PlayheadFrame,
            DurationFrames = (long)(3 * Timeline.FrameRate), // 3 seconds default
            FontFamily = "Segoe UI",
            FontSize = 48,
            X = 0.5,
            Y = 0.85
        };

        Timeline.AddTextOverlay(overlay);
        Timeline.SelectedTextOverlay = overlay;
        StatusText = "Added text overlay at playhead";
    }

    [RelayCommand]
    private void DeleteTextOverlay()
    {
        if (Timeline.SelectedTextOverlay == null) return;

        SaveUndoState("Delete text overlay");
        var overlayText = Timeline.SelectedTextOverlay.Text;
        Timeline.DeleteSelectedTextOverlay();
        StatusText = $"Deleted text overlay: {overlayText}";
    }

    [RelayCommand]
    private void DuplicateTextOverlay()
    {
        if (Timeline.SelectedTextOverlay == null) return;

        SaveUndoState("Duplicate text overlay");

        var clone = Timeline.SelectedTextOverlay.Clone();
        clone.StartFrame = Timeline.PlayheadFrame;

        Timeline.AddTextOverlay(clone);
        Timeline.SelectedTextOverlay = clone;
        StatusText = "Duplicated text overlay";
    }

    public void UpdateTextOverlay(TimelineTextOverlay overlay, string newText)
    {
        if (overlay == null) return;

        SaveUndoState("Edit text overlay");
        overlay.Text = newText;
        StatusText = $"Updated text overlay";
    }

    public void MoveTextOverlay(TimelineTextOverlay overlay, long newStartFrame)
    {
        if (overlay == null) return;

        SaveUndoState("Move text overlay");
        overlay.StartFrame = newStartFrame;
        StatusText = "Moved text overlay";
    }

    public void ResizeTextOverlay(TimelineTextOverlay overlay, long newDuration)
    {
        if (overlay == null) return;

        SaveUndoState("Resize text overlay");
        overlay.DurationFrames = Math.Max(1, newDuration);
        StatusText = "Resized text overlay";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPoolService.CurrentSourceChanged -= OnCurrentSourceChanged;
        _mediaPoolService.MediaPoolChanged -= OnMediaPoolChanged;
    }
}

internal class TimelineAction
{
    public string Description { get; set; } = "";
    public List<(int TrackId, TimelineClip Clip)> Clips { get; set; } = [];
    public List<TimelineTextOverlay> TextOverlays { get; set; } = [];
}
