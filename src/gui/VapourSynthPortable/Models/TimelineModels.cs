using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public TimelineClip()
    {
        Id = _nextId++;
    }

    public long DurationFrames => EndFrame - StartFrame;

    public double DurationSeconds => DurationFrames / FrameRate;

    public bool IsAudio => TrackType == TrackType.Audio;

    public bool IsVideo => TrackType == TrackType.Video;

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
        return new TimelineClip
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
            IsMuted = IsMuted
        };
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
    [ObservableProperty]
    private ObservableCollection<TimelineTrack> _tracks = [];

    [ObservableProperty]
    private ObservableCollection<TimelineTextOverlay> _textOverlays = [];

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
