using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

/// <summary>
/// Represents an effect applied to a timeline clip
/// </summary>
public partial class TimelineEffect : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _category = "";

    [ObservableProperty]
    private EffectType _effectType;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private ObservableCollection<EffectParameter> _parameters = [];

    /// <summary>
    /// Keyframe tracks for animated parameters
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<KeyframeTrack> _keyframeTracks = [];

    /// <summary>
    /// Whether any parameters have keyframes
    /// </summary>
    public bool HasKeyframes => KeyframeTracks.Any(t => t.HasKeyframes);

    /// <summary>
    /// VapourSynth filter namespace (e.g., "std", "resize", "bm3d")
    /// </summary>
    [ObservableProperty]
    private string _vsNamespace = "";

    /// <summary>
    /// VapourSynth filter function name (e.g., "Crop", "Lanczos", "VAggregate")
    /// </summary>
    [ObservableProperty]
    private string _vsFunction = "";

    public TimelineEffect()
    {
        Id = _nextId++;
    }

    public TimelineEffect(EffectDefinition definition) : this()
    {
        Name = definition.Name;
        Category = definition.Category;
        EffectType = definition.EffectType;
        VsNamespace = definition.VsNamespace;
        VsFunction = definition.VsFunction;

        // Clone parameters from definition
        foreach (var paramDef in definition.ParameterDefinitions)
        {
            Parameters.Add(new EffectParameter
            {
                Name = paramDef.Name,
                DisplayName = paramDef.DisplayName,
                Description = paramDef.Description,
                ParameterType = paramDef.ParameterType,
                Value = paramDef.DefaultValue,
                DefaultValue = paramDef.DefaultValue,
                MinValue = paramDef.MinValue,
                MaxValue = paramDef.MaxValue,
                Options = paramDef.Options != null ? [.. paramDef.Options] : []
            });
        }
    }

    /// <summary>
    /// Generates VapourSynth code for this effect
    /// </summary>
    public string GenerateVsCode(string inputVar, string outputVar)
    {
        var args = new List<string> { inputVar };

        foreach (var param in Parameters)
        {
            if (param.Value != null && !Equals(param.Value, param.DefaultValue))
            {
                var valueStr = param.ParameterType switch
                {
                    EffectParameterType.String => $"\"{param.Value}\"",
                    EffectParameterType.Boolean => param.Value.ToString()?.ToLower() ?? "false",
                    EffectParameterType.Color => FormatColor(param.Value),
                    _ => param.Value.ToString() ?? ""
                };
                args.Add($"{param.Name}={valueStr}");
            }
        }

        return $"{outputVar} = core.{VsNamespace}.{VsFunction}({string.Join(", ", args)})";
    }

    private static string FormatColor(object? value)
    {
        if (value is System.Windows.Media.Color color)
        {
            return $"[{color.R}, {color.G}, {color.B}]";
        }
        return value?.ToString() ?? "[0, 0, 0]";
    }

    /// <summary>
    /// Get or create a keyframe track for a parameter
    /// </summary>
    public KeyframeTrack GetOrCreateKeyframeTrack(EffectParameter parameter)
    {
        var track = KeyframeTracks.FirstOrDefault(t => t.ParameterName == parameter.Name);
        if (track == null)
        {
            track = new KeyframeTrack
            {
                ParameterName = parameter.Name,
                DisplayName = parameter.DisplayName,
                Parameter = parameter,
                Effect = this
            };
            KeyframeTracks.Add(track);
        }
        return track;
    }

    /// <summary>
    /// Get the keyframe track for a parameter (returns null if doesn't exist)
    /// </summary>
    public KeyframeTrack? GetKeyframeTrack(string parameterName)
    {
        return KeyframeTracks.FirstOrDefault(t => t.ParameterName == parameterName);
    }

    /// <summary>
    /// Add a keyframe to a parameter
    /// </summary>
    public Keyframe AddKeyframe(EffectParameter parameter, long frame, object? value, KeyframeInterpolation interpolation = KeyframeInterpolation.Linear)
    {
        var track = GetOrCreateKeyframeTrack(parameter);
        var keyframe = track.AddKeyframe(frame, value, interpolation);
        OnPropertyChanged(nameof(HasKeyframes));
        return keyframe;
    }

    /// <summary>
    /// Remove all keyframes from a parameter
    /// </summary>
    public void ClearKeyframes(EffectParameter parameter)
    {
        var track = KeyframeTracks.FirstOrDefault(t => t.ParameterName == parameter.Name);
        if (track != null)
        {
            KeyframeTracks.Remove(track);
            OnPropertyChanged(nameof(HasKeyframes));
        }
    }

    /// <summary>
    /// Get the interpolated value for a parameter at the specified frame
    /// </summary>
    public object? GetParameterValueAtFrame(EffectParameter parameter, long frame)
    {
        var track = KeyframeTracks.FirstOrDefault(t => t.ParameterName == parameter.Name);
        return track?.GetValueAtFrame(frame) ?? parameter.Value;
    }

    /// <summary>
    /// Apply keyframed values for a specific frame
    /// </summary>
    public void ApplyKeyframedValues(long frame)
    {
        foreach (var track in KeyframeTracks.Where(t => t.IsEnabled && t.HasKeyframes))
        {
            var param = Parameters.FirstOrDefault(p => p.Name == track.ParameterName);
            if (param != null)
            {
                param.Value = track.GetValueAtFrame(frame);
            }
        }
    }

    public TimelineEffect Clone()
    {
        var clone = new TimelineEffect
        {
            Name = Name,
            Category = Category,
            EffectType = EffectType,
            IsEnabled = IsEnabled,
            VsNamespace = VsNamespace,
            VsFunction = VsFunction
        };

        foreach (var param in Parameters)
        {
            clone.Parameters.Add(param.Clone());
        }

        foreach (var track in KeyframeTracks)
        {
            var clonedTrack = track.Clone();
            clonedTrack.Effect = clone;
            clonedTrack.Parameter = clone.Parameters.FirstOrDefault(p => p.Name == track.ParameterName);
            clone.KeyframeTracks.Add(clonedTrack);
        }

        return clone;
    }
}

/// <summary>
/// Defines an available effect that can be applied to clips
/// </summary>
public class EffectDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public EffectType EffectType { get; set; }
    public string VsNamespace { get; set; } = "";
    public string VsFunction { get; set; } = "";
    public List<EffectParameterDefinition> ParameterDefinitions { get; set; } = [];

    /// <summary>
    /// Icon path for the effect (optional)
    /// </summary>
    public string IconPath { get; set; } = "";
}

/// <summary>
/// Defines a parameter for an effect
/// </summary>
public class EffectParameterDefinition
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public EffectParameterType ParameterType { get; set; }
    public object? DefaultValue { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public List<string>? Options { get; set; }
}

/// <summary>
/// Represents a parameter value on an effect instance
/// </summary>
public partial class EffectParameter : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private EffectParameterType _parameterType;

    [ObservableProperty]
    private object? _value;

    [ObservableProperty]
    private object? _defaultValue;

    [ObservableProperty]
    private double? _minValue;

    [ObservableProperty]
    private double? _maxValue;

    [ObservableProperty]
    private ObservableCollection<string> _options = [];

    public bool IsModified => !Equals(Value, DefaultValue);

    public void Reset()
    {
        Value = DefaultValue;
    }

    public EffectParameter Clone()
    {
        return new EffectParameter
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            ParameterType = ParameterType,
            Value = Value,
            DefaultValue = DefaultValue,
            MinValue = MinValue,
            MaxValue = MaxValue,
            Options = [.. Options]
        };
    }
}

/// <summary>
/// Types of effects available
/// </summary>
public enum EffectType
{
    // Video effects
    Resize,
    Crop,
    Rotate,
    Flip,

    // Color effects
    ColorCorrection,
    LUT,
    Curves,

    // Filter effects
    Denoise,
    Sharpen,
    Blur,

    // Restoration effects
    Deinterlace,
    Deblock,
    Deband,

    // Stylize effects
    Grain,
    Vignette,

    // Audio effects
    Volume,
    EQ,
    Compressor,

    // Custom/Other
    Custom
}

/// <summary>
/// Parameter data types
/// </summary>
public enum EffectParameterType
{
    Integer,
    Float,
    Boolean,
    String,
    Enum,
    Color,
    Point,
    Size,
    File
}
