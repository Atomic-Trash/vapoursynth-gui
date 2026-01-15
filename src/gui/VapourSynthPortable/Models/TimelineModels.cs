using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Models;

public enum TrackType
{
    Video,
    Audio
}

/// <summary>
/// Represents a clip on the timeline
/// </summary>
public partial class TimelineClip : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _sourcePath = "";

    [ObservableProperty]
    private TrackType _trackType = TrackType.Video;

    // Position on timeline (in frames)
    [ObservableProperty]
    private long _startFrame;

    [ObservableProperty]
    private long _endFrame;

    // Source in/out points (for trimming)
    [ObservableProperty]
    private long _sourceInFrame;

    [ObservableProperty]
    private long _sourceOutFrame;

    // Original source duration
    [ObservableProperty]
    private long _sourceDurationFrames;

    [ObservableProperty]
    private double _frameRate = 24;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private Color _color = Color.FromRgb(0x4A, 0x9E, 0xCF);

    // Audio properties
    [ObservableProperty]
    private double _volume = 1.0;

    // Linked audio/video clip
    [ObservableProperty]
    private TimelineClip? _linkedClip;

    // Effects applied to this clip
    [ObservableProperty]
    private ObservableCollection<TimelineEffect> _effects = [];

    // Color grade applied to this clip
    [ObservableProperty]
    private ColorGrade? _colorGrade;

    // Restoration applied to the source media
    [ObservableProperty]
    private bool _hasRestoration;

    /// <summary>
    /// Indicates if the clip has any enabled effects
    /// </summary>
    public bool HasEffects => Effects.Any(e => e.IsEnabled);

    /// <summary>
    /// Indicates if the clip has a color grade applied
    /// </summary>
    public bool HasColorGrade => ColorGrade != null && !IsColorGradeDefault(ColorGrade);

    private static bool IsColorGradeDefault(ColorGrade grade)
    {
        return Math.Abs(grade.Exposure) < 0.001 &&
               Math.Abs(grade.Contrast) < 0.001 &&
               Math.Abs(grade.Saturation) < 0.001 &&
               Math.Abs(grade.Temperature) < 0.001 &&
               Math.Abs(grade.Tint) < 0.001 &&
               Math.Abs(grade.LiftX) < 0.001 &&
               Math.Abs(grade.LiftY) < 0.001 &&
               Math.Abs(grade.LiftMaster) < 0.001 &&
               Math.Abs(grade.GammaX) < 0.001 &&
               Math.Abs(grade.GammaY) < 0.001 &&
               Math.Abs(grade.GammaMaster) < 0.001 &&
               Math.Abs(grade.GainX) < 0.001 &&
               Math.Abs(grade.GainY) < 0.001 &&
               Math.Abs(grade.GainMaster) < 0.001 &&
               string.IsNullOrEmpty(grade.LutPath);
    }

    public TimelineClip()
    {
        Id = _nextId++;
    }

    public long DurationFrames => EndFrame - StartFrame;

    public double DurationSeconds => DurationFrames / FrameRate;

    public bool IsAudio => TrackType == TrackType.Audio;

    public bool IsVideo => TrackType == TrackType.Video;

    /// <summary>
    /// Source in-point as a percentage (0.0-1.0) for waveform display
    /// </summary>
    public double SourceInPoint => SourceDurationFrames > 0 ? SourceInFrame / (double)SourceDurationFrames : 0;

    /// <summary>
    /// Source out-point as a percentage (0.0-1.0) for waveform display
    /// </summary>
    public double SourceOutPoint => SourceDurationFrames > 0 ? SourceOutFrame / (double)SourceDurationFrames : 1;

    public double StartSeconds => StartFrame / FrameRate;

    public double EndSeconds => EndFrame / FrameRate;

    public string DurationFormatted
    {
        get
        {
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}:{(int)(ts.Milliseconds / 1000.0 * FrameRate):D2}";
        }
    }

    public string StartTimecode => FramesToTimecode(StartFrame);

    public string EndTimecode => FramesToTimecode(EndFrame);

    private string FramesToTimecode(long frames)
    {
        var totalSeconds = frames / FrameRate;
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = (int)(totalSeconds % 60);
        var frameInSecond = (int)(frames % FrameRate);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frameInSecond:D2}";
    }

    public TimelineClip Clone()
    {
        var clone = new TimelineClip
        {
            Name = Name,
            SourcePath = SourcePath,
            TrackType = TrackType,
            StartFrame = StartFrame,
            EndFrame = EndFrame,
            SourceInFrame = SourceInFrame,
            SourceOutFrame = SourceOutFrame,
            SourceDurationFrames = SourceDurationFrames,
            FrameRate = FrameRate,
            Color = Color,
            Volume = Volume,
            IsMuted = IsMuted,
            HasRestoration = HasRestoration,
            ColorGrade = ColorGrade?.Clone()
        };

        // Clone effects
        foreach (var effect in Effects)
        {
            clone.Effects.Add(effect.Clone());
        }

        return clone;
    }

    /// <summary>
    /// Adds an effect to this clip
    /// </summary>
    public void AddEffect(TimelineEffect effect)
    {
        Effects.Add(effect);
        OnPropertyChanged(nameof(HasEffects));
    }

    /// <summary>
    /// Removes an effect from this clip
    /// </summary>
    public void RemoveEffect(TimelineEffect effect)
    {
        Effects.Remove(effect);
        OnPropertyChanged(nameof(HasEffects));
    }

    /// <summary>
    /// Moves an effect to a new position in the stack
    /// </summary>
    public void MoveEffect(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Effects.Count) return;
        if (newIndex < 0 || newIndex >= Effects.Count) return;

        Effects.Move(oldIndex, newIndex);
    }
}

/// <summary>
/// Represents a track in the timeline
/// </summary>
public partial class TimelineTrack : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private TrackType _trackType = TrackType.Video;

    [ObservableProperty]
    private ObservableCollection<TimelineClip> _clips = [];

    [ObservableProperty]
    private ObservableCollection<TimelineTransition> _transitions = [];

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _isSolo;

    [ObservableProperty]
    private double _height = 50;

    [ObservableProperty]
    private double _volume = 1.0;

    /// <summary>
    /// Pan position (-1.0 = full left, 0 = center, 1.0 = full right)
    /// </summary>
    [ObservableProperty]
    private double _pan = 0.0;

    /// <summary>
    /// Volume in decibels for display purposes
    /// </summary>
    public double VolumeDb => Volume > 0 ? 20.0 * Math.Log10(Volume) : -96.0;

    /// <summary>
    /// Pan display string
    /// </summary>
    public string PanDisplay => Pan switch
    {
        0 => "C",
        < 0 => $"L{(int)(-Pan * 100)}",
        > 0 => $"R{(int)(Pan * 100)}",
        _ => "C"
    };

    public TimelineTrack()
    {
        Id = _nextId++;
    }

    public TimelineTrack(string name, TrackType type) : this()
    {
        Name = name;
        TrackType = type;
        Height = type == TrackType.Video ? 60 : 40;
    }
}

/// <summary>
/// Represents the entire timeline
/// </summary>
public partial class Timeline : ObservableObject
{
    private static readonly ILogger<Timeline> _logger = LoggingService.GetLogger<Timeline>();

    [ObservableProperty]
    private ObservableCollection<TimelineTrack> _tracks = [];

    [ObservableProperty]
    private ObservableCollection<TimelineTextOverlay> _textOverlays = [];

    [ObservableProperty]
    private ObservableCollection<TimelineMarker> _markers = [];

    [ObservableProperty]
    private TimelineMarker? _selectedMarker;

    [ObservableProperty]
    private double _frameRate = 24;

    [ObservableProperty]
    private long _playheadFrame;

    [ObservableProperty]
    private long _inPoint = -1;

    [ObservableProperty]
    private long _outPoint = -1;

    [ObservableProperty]
    private double _zoom = 1.0;

    [ObservableProperty]
    private double _scrollPosition;

    [ObservableProperty]
    private TimelineClip? _selectedClip;

    [ObservableProperty]
    private TimelineTrack? _selectedTrack;

    [ObservableProperty]
    private TimelineTextOverlay? _selectedTextOverlay;

    // Track the previous HasClips state to detect changes
    private bool _lastHasClips;

    public Timeline()
    {
        // Subscribe to track collection changes to monitor clip additions
        Tracks.CollectionChanged += Tracks_CollectionChanged;
    }

    private void Tracks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Subscribe to new tracks' clip collections
        if (e.NewItems != null)
        {
            foreach (TimelineTrack track in e.NewItems)
            {
                track.Clips.CollectionChanged += Clips_CollectionChanged;
            }
        }

        // Unsubscribe from removed tracks
        if (e.OldItems != null)
        {
            foreach (TimelineTrack track in e.OldItems)
            {
                track.Clips.CollectionChanged -= Clips_CollectionChanged;
            }
        }

        NotifyComputedPropertiesChanged();
    }

    private void Clips_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        NotifyComputedPropertiesChanged();
    }

    /// <summary>
    /// Notifies that computed properties (HasClips, DurationFrames, etc.) may have changed.
    /// Call this after adding/removing clips programmatically.
    /// </summary>
    public void NotifyComputedPropertiesChanged()
    {
        var currentHasClips = HasClips;
        _logger.LogDebug("NotifyComputedPropertiesChanged: currentHasClips={Current}, lastHasClips={Last}",
            currentHasClips, _lastHasClips);

        if (currentHasClips != _lastHasClips)
        {
            _logger.LogInformation("HasClips changed from {Old} to {New}, firing PropertyChanged",
                _lastHasClips, currentHasClips);
            _lastHasClips = currentHasClips;
            OnPropertyChanged(nameof(HasClips));
        }
        OnPropertyChanged(nameof(DurationFrames));
        OnPropertyChanged(nameof(DurationSeconds));
        OnPropertyChanged(nameof(DurationFormatted));
    }

    /// <summary>
    /// Subscribes to clip changes for an existing track. Call this for tracks created before Timeline was constructed.
    /// </summary>
    public void SubscribeToTrackClips(TimelineTrack track)
    {
        track.Clips.CollectionChanged -= Clips_CollectionChanged; // Avoid double subscription
        track.Clips.CollectionChanged += Clips_CollectionChanged;
    }

    public long DurationFrames
    {
        get
        {
            long maxFrame = 0;
            foreach (var track in Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    if (clip.EndFrame > maxFrame)
                        maxFrame = clip.EndFrame;
                }
            }
            return maxFrame;
        }
    }

    public double DurationSeconds => DurationFrames / FrameRate;

    public bool HasClips => Tracks.Any(t => t.Clips.Count > 0);

    public string DurationFormatted
    {
        get
        {
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}:{(int)(ts.Milliseconds / 1000.0 * FrameRate):D2}";
        }
    }

    public string PlayheadTimecode
    {
        get
        {
            var totalSeconds = PlayheadFrame / FrameRate;
            var hours = (int)(totalSeconds / 3600);
            var minutes = (int)((totalSeconds % 3600) / 60);
            var seconds = (int)(totalSeconds % 60);
            var frameInSecond = (int)(PlayheadFrame % FrameRate);
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frameInSecond:D2}";
        }
    }

    public void AddTrack(TrackType type)
    {
        var videoCount = Tracks.Count(t => t.TrackType == TrackType.Video);
        var audioCount = Tracks.Count(t => t.TrackType == TrackType.Audio);

        var name = type == TrackType.Video ? $"V{videoCount + 1}" : $"A{audioCount + 1}";
        var track = new TimelineTrack(name, type);

        // Insert video tracks at top, audio at bottom
        if (type == TrackType.Video)
        {
            var insertIndex = Tracks.TakeWhile(t => t.TrackType == TrackType.Video).Count();
            Tracks.Insert(insertIndex, track);
        }
        else
        {
            Tracks.Add(track);
        }
    }

    public void RemoveTrack(TimelineTrack track)
    {
        Tracks.Remove(track);
    }

    public TimelineClip? GetClipAtFrame(long frame, TimelineTrack? track = null)
    {
        var tracksToSearch = track != null ? [track] : Tracks;

        foreach (var t in tracksToSearch)
        {
            foreach (var clip in t.Clips)
            {
                if (frame >= clip.StartFrame && frame < clip.EndFrame)
                    return clip;
            }
        }
        return null;
    }

    public void DeleteSelectedClip()
    {
        if (SelectedClip == null) return;

        foreach (var track in Tracks)
        {
            if (track.Clips.Contains(SelectedClip))
            {
                track.Clips.Remove(SelectedClip);
                break;
            }
        }
        SelectedClip = null;
    }

    public void AddTextOverlay(TimelineTextOverlay overlay)
    {
        TextOverlays.Add(overlay);
    }

    public void RemoveTextOverlay(TimelineTextOverlay overlay)
    {
        TextOverlays.Remove(overlay);
        if (SelectedTextOverlay == overlay)
            SelectedTextOverlay = null;
    }

    public void DeleteSelectedTextOverlay()
    {
        if (SelectedTextOverlay == null) return;
        TextOverlays.Remove(SelectedTextOverlay);
        SelectedTextOverlay = null;
    }

    public TimelineTextOverlay? GetTextOverlayAtFrame(long frame)
    {
        return TextOverlays.FirstOrDefault(t => frame >= t.StartFrame && frame < t.EndFrame);
    }

    public IEnumerable<TimelineTextOverlay> GetTextOverlaysAtFrame(long frame)
    {
        return TextOverlays.Where(t => frame >= t.StartFrame && frame < t.EndFrame);
    }

    /// <summary>
    /// Adds a marker at the specified frame
    /// </summary>
    public TimelineMarker AddMarker(string name, long frame)
    {
        var marker = new TimelineMarker(name, frame);
        Markers.Add(marker);
        SortMarkers();
        return marker;
    }

    /// <summary>
    /// Adds a marker at the specified frame with a color
    /// </summary>
    public TimelineMarker AddMarker(string name, long frame, Color color)
    {
        var marker = new TimelineMarker(name, frame, color);
        Markers.Add(marker);
        SortMarkers();
        return marker;
    }

    /// <summary>
    /// Adds a marker at the current playhead position
    /// </summary>
    public TimelineMarker AddMarkerAtPlayhead(string name = "")
    {
        var markerName = string.IsNullOrEmpty(name) ? $"Marker {Markers.Count + 1}" : name;
        return AddMarker(markerName, PlayheadFrame);
    }

    /// <summary>
    /// Removes a marker from the timeline
    /// </summary>
    public void RemoveMarker(TimelineMarker marker)
    {
        Markers.Remove(marker);
        if (SelectedMarker == marker)
            SelectedMarker = null;
    }

    /// <summary>
    /// Removes the selected marker
    /// </summary>
    public void DeleteSelectedMarker()
    {
        if (SelectedMarker != null)
            RemoveMarker(SelectedMarker);
    }

    /// <summary>
    /// Clears all markers from the timeline
    /// </summary>
    public void ClearMarkers()
    {
        Markers.Clear();
        SelectedMarker = null;
    }

    /// <summary>
    /// Navigates to the next marker after the current playhead
    /// </summary>
    public TimelineMarker? GoToNextMarker()
    {
        var nextMarker = Markers
            .Where(m => m.Frame > PlayheadFrame)
            .OrderBy(m => m.Frame)
            .FirstOrDefault();

        if (nextMarker != null)
        {
            PlayheadFrame = nextMarker.Frame;
            SelectedMarker = nextMarker;
        }
        return nextMarker;
    }

    /// <summary>
    /// Navigates to the previous marker before the current playhead
    /// </summary>
    public TimelineMarker? GoToPreviousMarker()
    {
        var prevMarker = Markers
            .Where(m => m.Frame < PlayheadFrame)
            .OrderByDescending(m => m.Frame)
            .FirstOrDefault();

        if (prevMarker != null)
        {
            PlayheadFrame = prevMarker.Frame;
            SelectedMarker = prevMarker;
        }
        return prevMarker;
    }

    /// <summary>
    /// Navigates to a specific marker
    /// </summary>
    public void GoToMarker(TimelineMarker marker)
    {
        if (Markers.Contains(marker))
        {
            PlayheadFrame = marker.Frame;
            SelectedMarker = marker;
        }
    }

    /// <summary>
    /// Gets the marker at or near the specified frame (within tolerance)
    /// </summary>
    public TimelineMarker? GetMarkerAtFrame(long frame, int tolerance = 0)
    {
        return Markers.FirstOrDefault(m => Math.Abs(m.Frame - frame) <= tolerance);
    }

    /// <summary>
    /// Sorts markers by frame position
    /// </summary>
    private void SortMarkers()
    {
        var sorted = Markers.OrderBy(m => m.Frame).ToList();
        Markers.Clear();
        foreach (var marker in sorted)
            Markers.Add(marker);
    }
}

/// <summary>
/// Represents a transition between clips
/// </summary>
public partial class TimelineTransition : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "Cross Dissolve";

    [ObservableProperty]
    private TransitionType _transitionType = TransitionType.CrossDissolve;

    [ObservableProperty]
    private long _durationFrames = 24;

    [ObservableProperty]
    private long _startFrame;

    [ObservableProperty]
    private TimelineClip? _clipA;

    [ObservableProperty]
    private TimelineClip? _clipB;

    [ObservableProperty]
    private bool _isSelected;

    // Wipe direction for wipe transitions
    [ObservableProperty]
    private WipeDirection _wipeDirection = WipeDirection.Left;

    // Easing for the transition
    [ObservableProperty]
    private TransitionEasing _easing = TransitionEasing.Linear;

    public TimelineTransition()
    {
        Id = _nextId++;
    }

    public string DisplayName => GetDisplayName();

    public string IconPath => GetIconPath();

    private string GetDisplayName()
    {
        return TransitionType switch
        {
            TransitionType.Cut => "Cut",
            TransitionType.CrossDissolve => "Cross Dissolve",
            TransitionType.FadeIn => "Fade In",
            TransitionType.FadeOut => "Fade Out",
            TransitionType.DipToBlack => "Dip to Black",
            TransitionType.DipToWhite => "Dip to White",
            TransitionType.Wipe => $"Wipe {WipeDirection}",
            TransitionType.Slide => $"Slide {WipeDirection}",
            TransitionType.Push => $"Push {WipeDirection}",
            _ => Name
        };
    }

    private string GetIconPath()
    {
        return TransitionType switch
        {
            TransitionType.CrossDissolve => "M0,0 L20,10 L0,20 Z M20,0 L0,10 L20,20 Z",
            TransitionType.FadeIn => "M0,20 L20,20 L20,0 Z",
            TransitionType.FadeOut => "M0,0 L20,0 L0,20 Z",
            TransitionType.Wipe => "M0,0 L10,0 L10,20 L0,20 Z",
            _ => "M0,10 L10,0 L20,10 L10,20 Z"
        };
    }

    public static List<TransitionPreset> GetPresets()
    {
        return
        [
            new() { Name = "Cut", Type = TransitionType.Cut, DefaultDuration = 0 },
            new() { Name = "Cross Dissolve", Type = TransitionType.CrossDissolve, DefaultDuration = 24 },
            new() { Name = "Fade In", Type = TransitionType.FadeIn, DefaultDuration = 24 },
            new() { Name = "Fade Out", Type = TransitionType.FadeOut, DefaultDuration = 24 },
            new() { Name = "Dip to Black", Type = TransitionType.DipToBlack, DefaultDuration = 24 },
            new() { Name = "Dip to White", Type = TransitionType.DipToWhite, DefaultDuration = 24 },
            new() { Name = "Wipe Left", Type = TransitionType.Wipe, Direction = WipeDirection.Left, DefaultDuration = 24 },
            new() { Name = "Wipe Right", Type = TransitionType.Wipe, Direction = WipeDirection.Right, DefaultDuration = 24 },
            new() { Name = "Wipe Up", Type = TransitionType.Wipe, Direction = WipeDirection.Up, DefaultDuration = 24 },
            new() { Name = "Wipe Down", Type = TransitionType.Wipe, Direction = WipeDirection.Down, DefaultDuration = 24 },
            new() { Name = "Slide Left", Type = TransitionType.Slide, Direction = WipeDirection.Left, DefaultDuration = 24 },
            new() { Name = "Push Right", Type = TransitionType.Push, Direction = WipeDirection.Right, DefaultDuration = 24 },
        ];
    }
}

public class TransitionPreset
{
    public string Name { get; set; } = "";
    public TransitionType Type { get; set; }
    public WipeDirection Direction { get; set; }
    public int DefaultDuration { get; set; } = 24;
}

public enum TransitionType
{
    Cut,
    CrossDissolve,
    FadeIn,
    FadeOut,
    DipToBlack,
    DipToWhite,
    Wipe,
    Slide,
    Push
}

public enum WipeDirection
{
    Left,
    Right,
    Up,
    Down
}

public enum TransitionEasing
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut
}

/// <summary>
/// Represents a marker on the timeline for navigation and annotation
/// </summary>
public partial class TimelineMarker : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private long _frame;

    [ObservableProperty]
    private Color _color = Color.FromRgb(0x4A, 0xCF, 0x6A);

    [ObservableProperty]
    private string _comment = "";

    [ObservableProperty]
    private MarkerType _markerType = MarkerType.Standard;

    [ObservableProperty]
    private bool _isSelected;

    public TimelineMarker()
    {
        Id = _nextId++;
    }

    public TimelineMarker(string name, long frame) : this()
    {
        Name = name;
        Frame = frame;
    }

    public TimelineMarker(string name, long frame, Color color) : this(name, frame)
    {
        Color = color;
    }

    /// <summary>
    /// Gets the timecode representation of this marker's position
    /// </summary>
    public string GetTimecode(double frameRate)
    {
        var totalSeconds = Frame / frameRate;
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = (int)(totalSeconds % 60);
        var frameInSecond = (int)(Frame % frameRate);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frameInSecond:D2}";
    }

    public TimelineMarker Clone()
    {
        return new TimelineMarker
        {
            Name = Name,
            Frame = Frame,
            Color = Color,
            Comment = Comment,
            MarkerType = MarkerType
        };
    }
}

public enum MarkerType
{
    Standard,
    Chapter,
    ToDo,
    Note
}
