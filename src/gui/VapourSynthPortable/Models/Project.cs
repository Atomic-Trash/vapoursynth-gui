using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

/// <summary>
/// Represents a VapourSynth Studio project file
/// </summary>
public partial class Project : ObservableObject
{
    public const string FileExtension = ".vsproj";
    public const string FileFilter = "VapourSynth Studio Project|*.vsproj|All Files|*.*";

    [ObservableProperty]
    private string _name = "Untitled";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private DateTime _createdDate = DateTime.Now;

    [ObservableProperty]
    private DateTime _modifiedDate = DateTime.Now;

    [ObservableProperty]
    private string _version = "1.0";

    // Project settings
    [ObservableProperty]
    private ProjectSettings _settings = new();

    // Timeline data
    [ObservableProperty]
    private TimelineData _timelineData = new();

    // Media pool references
    [ObservableProperty]
    private List<MediaReference> _mediaReferences = [];

    // Color grading data
    [ObservableProperty]
    private ColorGradeData _colorGradeData = new();

    // Restore/AI processing queue
    [ObservableProperty]
    private List<RestoreJobData> _restoreJobs = [];

    [ObservableProperty]
    private bool _isDirty;

    public void MarkDirty()
    {
        IsDirty = true;
        ModifiedDate = DateTime.Now;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public string DisplayName => string.IsNullOrEmpty(FilePath)
        ? $"{Name}*"
        : IsDirty
            ? $"{System.IO.Path.GetFileNameWithoutExtension(FilePath)}*"
            : System.IO.Path.GetFileNameWithoutExtension(FilePath);
}

/// <summary>
/// Project-level settings
/// </summary>
public class ProjectSettings
{
    public double FrameRate { get; set; } = 24;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string ColorSpace { get; set; } = "sRGB";
    public int AudioSampleRate { get; set; } = 48000;
    public int AudioChannels { get; set; } = 2;
}

/// <summary>
/// Serializable timeline data
/// </summary>
public class TimelineData
{
    public double FrameRate { get; set; } = 24;
    public long PlayheadFrame { get; set; }
    public long InPoint { get; set; } = -1;
    public long OutPoint { get; set; } = -1;
    public double Zoom { get; set; } = 1.0;
    public List<TrackData> Tracks { get; set; } = [];
}

/// <summary>
/// Serializable track data
/// </summary>
public class TrackData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TrackType TrackType { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsMuted { get; set; }
    public bool IsLocked { get; set; }
    public bool IsSolo { get; set; }
    public double Height { get; set; } = 50;
    public double Volume { get; set; } = 1.0;
    public List<ClipData> Clips { get; set; } = [];
    public List<TransitionData> Transitions { get; set; } = [];
}

/// <summary>
/// Serializable clip data
/// </summary>
public class ClipData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public TrackType TrackType { get; set; }
    public long StartFrame { get; set; }
    public long EndFrame { get; set; }
    public long SourceInFrame { get; set; }
    public long SourceOutFrame { get; set; }
    public long SourceDurationFrames { get; set; }
    public double FrameRate { get; set; } = 24;
    public bool IsMuted { get; set; }
    public bool IsLocked { get; set; }
    public string ColorHex { get; set; } = "#2A6A9F";
    public double Volume { get; set; } = 1.0;

    // Per-clip effects
    public List<EffectData> Effects { get; set; } = [];

    // Per-clip color grade
    public ColorGradeData? ColorGrade { get; set; }
}

/// <summary>
/// Serializable effect data
/// </summary>
public class EffectData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public EffectType EffectType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; } = true;
    public string VsNamespace { get; set; } = "";
    public string VsFunction { get; set; } = "";
    public List<EffectParameterData> Parameters { get; set; } = [];
    public List<KeyframeTrackData> KeyframeTracks { get; set; } = [];
}

/// <summary>
/// Serializable effect parameter data
/// </summary>
public class EffectParameterData
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public EffectParameterType ParameterType { get; set; }
    public string? ValueJson { get; set; }
    public string? DefaultValueJson { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public List<string> Options { get; set; } = [];
}

/// <summary>
/// Serializable keyframe track data
/// </summary>
public class KeyframeTrackData
{
    public int Id { get; set; }
    public string ParameterName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsExpanded { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public List<KeyframeData> Keyframes { get; set; } = [];
}

/// <summary>
/// Serializable keyframe data
/// </summary>
public class KeyframeData
{
    public int Id { get; set; }
    public long Frame { get; set; }
    public string? ValueJson { get; set; }
    public KeyframeInterpolation Interpolation { get; set; } = KeyframeInterpolation.Linear;
    public double EaseInX { get; set; } = 0.25;
    public double EaseInY { get; set; } = 0.25;
    public double EaseOutX { get; set; } = 0.75;
    public double EaseOutY { get; set; } = 0.75;
}

/// <summary>
/// Serializable transition data
/// </summary>
public class TransitionData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TransitionType TransitionType { get; set; }
    public long DurationFrames { get; set; }
    public long StartFrame { get; set; }
    public int ClipAId { get; set; }
    public int ClipBId { get; set; }
    public WipeDirection WipeDirection { get; set; }
    public TransitionEasing Easing { get; set; }
}

/// <summary>
/// Reference to a media file in the project
/// </summary>
public class MediaReference
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public MediaType MediaType { get; set; }
    public double Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public string Codec { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime DateImported { get; set; } = DateTime.Now;
}

/// <summary>
/// Serializable color grade data
/// </summary>
public class ColorGradeData
{
    // Lift (Shadows)
    public double LiftX { get; set; }
    public double LiftY { get; set; }
    public double LiftMaster { get; set; }

    // Gamma (Midtones)
    public double GammaX { get; set; }
    public double GammaY { get; set; }
    public double GammaMaster { get; set; }

    // Gain (Highlights)
    public double GainX { get; set; }
    public double GainY { get; set; }
    public double GainMaster { get; set; }

    // Global adjustments
    public double Exposure { get; set; }
    public double Contrast { get; set; }
    public double Saturation { get; set; }
    public double Temperature { get; set; }
    public double Tint { get; set; }
    public double Highlights { get; set; }
    public double Shadows { get; set; }
    public double Whites { get; set; }
    public double Blacks { get; set; }
    public double Vibrance { get; set; }
    public double Clarity { get; set; }

    // LUT
    public string LutPath { get; set; } = "";
    public double LutIntensity { get; set; } = 1.0;

    public static ColorGradeData FromColorGrade(ColorGrade grade)
    {
        return new ColorGradeData
        {
            LiftX = grade.LiftX,
            LiftY = grade.LiftY,
            LiftMaster = grade.LiftMaster,
            GammaX = grade.GammaX,
            GammaY = grade.GammaY,
            GammaMaster = grade.GammaMaster,
            GainX = grade.GainX,
            GainY = grade.GainY,
            GainMaster = grade.GainMaster,
            Exposure = grade.Exposure,
            Contrast = grade.Contrast,
            Saturation = grade.Saturation,
            Temperature = grade.Temperature,
            Tint = grade.Tint,
            Highlights = grade.Highlights,
            Shadows = grade.Shadows,
            Whites = grade.Whites,
            Blacks = grade.Blacks,
            Vibrance = grade.Vibrance,
            Clarity = grade.Clarity,
            LutPath = grade.LutPath,
            LutIntensity = grade.LutIntensity
        };
    }

    public ColorGrade ToColorGrade()
    {
        return new ColorGrade
        {
            LiftX = LiftX,
            LiftY = LiftY,
            LiftMaster = LiftMaster,
            GammaX = GammaX,
            GammaY = GammaY,
            GammaMaster = GammaMaster,
            GainX = GainX,
            GainY = GainY,
            GainMaster = GainMaster,
            Exposure = Exposure,
            Contrast = Contrast,
            Saturation = Saturation,
            Temperature = Temperature,
            Tint = Tint,
            Highlights = Highlights,
            Shadows = Shadows,
            Whites = Whites,
            Blacks = Blacks,
            Vibrance = Vibrance,
            Clarity = Clarity,
            LutPath = LutPath,
            LutIntensity = LutIntensity
        };
    }
}

/// <summary>
/// Serializable restore job data
/// </summary>
public class RestoreJobData
{
    public string SourcePath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string PresetName { get; set; } = "";
    public ProcessingStatus Status { get; set; }
    public double Progress { get; set; }
}
