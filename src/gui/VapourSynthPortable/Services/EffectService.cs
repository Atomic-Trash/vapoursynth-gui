using System.Collections.ObjectModel;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for managing video effects and generating VapourSynth effect code
/// </summary>
public class EffectService
{
    private static EffectService? _instance;
    public static EffectService Instance => _instance ??= new EffectService();

    private readonly List<EffectDefinition> _effectDefinitions;
    private readonly Dictionary<string, EffectPreset> _presets;

    public EffectService()
    {
        _effectDefinitions = InitializeEffectDefinitions();
        _presets = new Dictionary<string, EffectPreset>();
        LoadBuiltInPresets();
    }

    /// <summary>
    /// Gets all available effect definitions
    /// </summary>
    public IReadOnlyList<EffectDefinition> AvailableEffects => _effectDefinitions;

    /// <summary>
    /// Gets effects grouped by category
    /// </summary>
    public IEnumerable<IGrouping<string, EffectDefinition>> EffectsByCategory =>
        _effectDefinitions.GroupBy(e => e.Category);

    /// <summary>
    /// Gets an effect definition by name
    /// </summary>
    public EffectDefinition? GetEffectDefinition(string name) =>
        _effectDefinitions.FirstOrDefault(e => e.Name == name);

    /// <summary>
    /// Creates a new effect instance from a definition
    /// </summary>
    public TimelineEffect CreateEffect(string effectName)
    {
        var definition = GetEffectDefinition(effectName);
        if (definition == null)
            throw new ArgumentException($"Effect '{effectName}' not found", nameof(effectName));

        return new TimelineEffect(definition);
    }

    /// <summary>
    /// Creates a new effect instance from a definition
    /// </summary>
    public TimelineEffect CreateEffect(EffectDefinition definition)
    {
        return new TimelineEffect(definition);
    }

    /// <summary>
    /// Generates VapourSynth script code for a clip's effect chain
    /// </summary>
    public string GenerateEffectChainCode(TimelineClip clip, string inputVar)
    {
        var lines = new List<string>();
        var currentVar = inputVar;
        var effectIndex = 0;

        foreach (var effect in clip.Effects.Where(e => e.IsEnabled))
        {
            var outputVar = $"fx_{clip.Id}_{effectIndex}";
            lines.Add(effect.GenerateVsCode(currentVar, outputVar));
            currentVar = outputVar;
            effectIndex++;
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Gets the final output variable name after applying all effects
    /// </summary>
    public string GetFinalOutputVar(TimelineClip clip, string inputVar)
    {
        var enabledEffects = clip.Effects.Where(e => e.IsEnabled).ToList();
        if (enabledEffects.Count == 0)
            return inputVar;

        return $"fx_{clip.Id}_{enabledEffects.Count - 1}";
    }

    /// <summary>
    /// Generates a complete VapourSynth script for a timeline
    /// </summary>
    public string GenerateTimelineScript(Timeline timeline, string outputPath)
    {
        var lines = new List<string>
        {
            "import vapoursynth as vs",
            "core = vs.core",
            "",
            "# Load plugins (as needed)",
            "# core.std.LoadPlugin(...)",
            ""
        };

        var clipVars = new Dictionary<int, string>();
        var clipIndex = 0;

        // Process each video track
        foreach (var track in timeline.Tracks.Where(t => t.TrackType == TrackType.Video && t.IsVisible))
        {
            foreach (var clip in track.Clips.OrderBy(c => c.StartFrame))
            {
                var sourceVar = $"src_{clipIndex}";
                var trimVar = $"clip_{clipIndex}";

                // Load source
                lines.Add($"# Clip: {clip.Name}");
                lines.Add($"{sourceVar} = core.lsmas.LWLibavSource(\"{clip.SourcePath.Replace("\\", "/")}\")");

                // Trim to in/out points
                if (clip.SourceInFrame > 0 || clip.SourceOutFrame < clip.SourceDurationFrames)
                {
                    lines.Add($"{trimVar} = core.std.Trim({sourceVar}, first={clip.SourceInFrame}, last={clip.SourceOutFrame - 1})");
                }
                else
                {
                    lines.Add($"{trimVar} = {sourceVar}");
                }

                // Apply effects
                if (clip.HasEffects)
                {
                    var effectCode = GenerateEffectChainCode(clip, trimVar);
                    if (!string.IsNullOrEmpty(effectCode))
                    {
                        lines.Add("");
                        lines.Add($"# Effects for {clip.Name}");
                        lines.Add(effectCode);
                        trimVar = GetFinalOutputVar(clip, trimVar);
                    }
                }

                clipVars[clip.Id] = trimVar;
                lines.Add("");
                clipIndex++;
            }
        }

        // Combine clips (simplified - just concatenate for now)
        if (clipVars.Count > 0)
        {
            var allClips = string.Join(", ", clipVars.Values);
            if (clipVars.Count > 1)
            {
                lines.Add("# Combine all clips");
                lines.Add($"output = core.std.Splice([{allClips}])");
            }
            else
            {
                lines.Add($"output = {clipVars.Values.First()}");
            }

            lines.Add("output.set_output()");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Saves an effect configuration as a preset
    /// </summary>
    public void SavePreset(string name, TimelineEffect effect)
    {
        _presets[name] = new EffectPreset
        {
            Name = name,
            EffectName = effect.Name,
            Parameters = effect.Parameters.ToDictionary(p => p.Name, p => p.Value)
        };
    }

    /// <summary>
    /// Loads a preset onto an effect
    /// </summary>
    public void ApplyPreset(string presetName, TimelineEffect effect)
    {
        if (!_presets.TryGetValue(presetName, out var preset))
            return;

        foreach (var param in effect.Parameters)
        {
            if (preset.Parameters.TryGetValue(param.Name, out var value))
            {
                param.Value = value;
            }
        }
    }

    /// <summary>
    /// Gets available presets for an effect type
    /// </summary>
    public IEnumerable<EffectPreset> GetPresetsForEffect(string effectName) =>
        _presets.Values.Where(p => p.EffectName == effectName);

    private List<EffectDefinition> InitializeEffectDefinitions()
    {
        return
        [
            // Resize effects
            new EffectDefinition
            {
                Name = "Resize (Lanczos)",
                Description = "High-quality resize using Lanczos algorithm",
                Category = "Resize",
                EffectType = EffectType.Resize,
                VsNamespace = "resize",
                VsFunction = "Lanczos",
                ParameterDefinitions =
                [
                    new() { Name = "width", DisplayName = "Width", ParameterType = EffectParameterType.Integer, DefaultValue = 1920, MinValue = 1, MaxValue = 8192 },
                    new() { Name = "height", DisplayName = "Height", ParameterType = EffectParameterType.Integer, DefaultValue = 1080, MinValue = 1, MaxValue = 8192 }
                ]
            },
            new EffectDefinition
            {
                Name = "Resize (Bicubic)",
                Description = "Bicubic resize with adjustable sharpness",
                Category = "Resize",
                EffectType = EffectType.Resize,
                VsNamespace = "resize",
                VsFunction = "Bicubic",
                ParameterDefinitions =
                [
                    new() { Name = "width", DisplayName = "Width", ParameterType = EffectParameterType.Integer, DefaultValue = 1920, MinValue = 1, MaxValue = 8192 },
                    new() { Name = "height", DisplayName = "Height", ParameterType = EffectParameterType.Integer, DefaultValue = 1080, MinValue = 1, MaxValue = 8192 },
                    new() { Name = "filter_param_a", DisplayName = "B (blur)", ParameterType = EffectParameterType.Float, DefaultValue = 0.0, MinValue = -1.0, MaxValue = 1.0 },
                    new() { Name = "filter_param_b", DisplayName = "C (sharpness)", ParameterType = EffectParameterType.Float, DefaultValue = 0.5, MinValue = 0.0, MaxValue = 1.0 }
                ]
            },

            // Crop effect
            new EffectDefinition
            {
                Name = "Crop",
                Description = "Crop video edges",
                Category = "Transform",
                EffectType = EffectType.Crop,
                VsNamespace = "std",
                VsFunction = "Crop",
                ParameterDefinitions =
                [
                    new() { Name = "left", DisplayName = "Left", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 4096 },
                    new() { Name = "right", DisplayName = "Right", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 4096 },
                    new() { Name = "top", DisplayName = "Top", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 4096 },
                    new() { Name = "bottom", DisplayName = "Bottom", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 4096 }
                ]
            },

            // Flip/Rotate
            new EffectDefinition
            {
                Name = "Flip Horizontal",
                Description = "Mirror video horizontally",
                Category = "Transform",
                EffectType = EffectType.Flip,
                VsNamespace = "std",
                VsFunction = "FlipHorizontal",
                ParameterDefinitions = []
            },
            new EffectDefinition
            {
                Name = "Flip Vertical",
                Description = "Mirror video vertically",
                Category = "Transform",
                EffectType = EffectType.Flip,
                VsNamespace = "std",
                VsFunction = "FlipVertical",
                ParameterDefinitions = []
            },
            new EffectDefinition
            {
                Name = "Rotate 90 CW",
                Description = "Rotate video 90 degrees clockwise",
                Category = "Transform",
                EffectType = EffectType.Rotate,
                VsNamespace = "std",
                VsFunction = "Turn90",
                ParameterDefinitions = []
            },
            new EffectDefinition
            {
                Name = "Rotate 180",
                Description = "Rotate video 180 degrees",
                Category = "Transform",
                EffectType = EffectType.Rotate,
                VsNamespace = "std",
                VsFunction = "Turn180",
                ParameterDefinitions = []
            },

            // Denoise effects
            new EffectDefinition
            {
                Name = "BM3D Denoise",
                Description = "Block-matching 3D denoising (CPU intensive)",
                Category = "Denoise",
                EffectType = EffectType.Denoise,
                VsNamespace = "bm3d",
                VsFunction = "VAggregate",
                ParameterDefinitions =
                [
                    new() { Name = "sigma", DisplayName = "Strength", Description = "Denoising strength", ParameterType = EffectParameterType.Float, DefaultValue = 3.0, MinValue = 0.0, MaxValue = 25.0 },
                    new() { Name = "radius", DisplayName = "Temporal Radius", Description = "Frames to use for temporal denoising", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 16 }
                ]
            },
            new EffectDefinition
            {
                Name = "KNLMeans Denoise",
                Description = "GPU-accelerated non-local means denoising",
                Category = "Denoise",
                EffectType = EffectType.Denoise,
                VsNamespace = "knlm",
                VsFunction = "KNLMeansCL",
                ParameterDefinitions =
                [
                    new() { Name = "h", DisplayName = "Strength", Description = "Denoising strength", ParameterType = EffectParameterType.Float, DefaultValue = 1.2, MinValue = 0.0, MaxValue = 10.0 },
                    new() { Name = "d", DisplayName = "Temporal Radius", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 16 },
                    new() { Name = "a", DisplayName = "Spatial Radius", ParameterType = EffectParameterType.Integer, DefaultValue = 2, MinValue = 1, MaxValue = 16 }
                ]
            },
            new EffectDefinition
            {
                Name = "DFTTest Denoise",
                Description = "Frequency domain denoising",
                Category = "Denoise",
                EffectType = EffectType.Denoise,
                VsNamespace = "dfttest",
                VsFunction = "DFTTest",
                ParameterDefinitions =
                [
                    new() { Name = "sigma", DisplayName = "Sigma", Description = "Noise threshold", ParameterType = EffectParameterType.Float, DefaultValue = 8.0, MinValue = 0.0, MaxValue = 100.0 },
                    new() { Name = "tbsize", DisplayName = "Temporal Block Size", ParameterType = EffectParameterType.Integer, DefaultValue = 5, MinValue = 1, MaxValue = 15 }
                ]
            },

            // Sharpen effects
            new EffectDefinition
            {
                Name = "CAS Sharpen",
                Description = "AMD FidelityFX Contrast Adaptive Sharpening",
                Category = "Sharpen",
                EffectType = EffectType.Sharpen,
                VsNamespace = "cas",
                VsFunction = "CAS",
                ParameterDefinitions =
                [
                    new() { Name = "sharpness", DisplayName = "Sharpness", Description = "Sharpening strength", ParameterType = EffectParameterType.Float, DefaultValue = 0.5, MinValue = 0.0, MaxValue = 1.0 }
                ]
            },
            new EffectDefinition
            {
                Name = "Unsharp Mask",
                Description = "Classic unsharp masking",
                Category = "Sharpen",
                EffectType = EffectType.Sharpen,
                VsNamespace = "std",
                VsFunction = "Convolution",
                ParameterDefinitions =
                [
                    new() { Name = "matrix", DisplayName = "Kernel", ParameterType = EffectParameterType.String, DefaultValue = "[-1, -1, -1, -1, 9, -1, -1, -1, -1]" }
                ]
            },

            // Deinterlace effects
            new EffectDefinition
            {
                Name = "NNEDI3",
                Description = "Neural network edge directed interpolation",
                Category = "Deinterlace",
                EffectType = EffectType.Deinterlace,
                VsNamespace = "nnedi3",
                VsFunction = "nnedi3",
                ParameterDefinitions =
                [
                    new() { Name = "field", DisplayName = "Field Order", Description = "0=BFF, 1=TFF, 2=same as input", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 3 },
                    new() { Name = "nsize", DisplayName = "Neural Network Size", ParameterType = EffectParameterType.Integer, DefaultValue = 4, MinValue = 0, MaxValue = 6 },
                    new() { Name = "nns", DisplayName = "Neurons", ParameterType = EffectParameterType.Integer, DefaultValue = 3, MinValue = 0, MaxValue = 4 }
                ]
            },
            new EffectDefinition
            {
                Name = "EEDI3",
                Description = "Enhanced edge directed interpolation",
                Category = "Deinterlace",
                EffectType = EffectType.Deinterlace,
                VsNamespace = "eedi3",
                VsFunction = "eedi3",
                ParameterDefinitions =
                [
                    new() { Name = "field", DisplayName = "Field Order", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 3 },
                    new() { Name = "alpha", DisplayName = "Alpha", ParameterType = EffectParameterType.Float, DefaultValue = 0.2, MinValue = 0.0, MaxValue = 1.0 },
                    new() { Name = "beta", DisplayName = "Beta", ParameterType = EffectParameterType.Float, DefaultValue = 0.25, MinValue = 0.0, MaxValue = 1.0 }
                ]
            },

            // Deband effect
            new EffectDefinition
            {
                Name = "Deband (f3kdb)",
                Description = "Remove color banding artifacts",
                Category = "Restoration",
                EffectType = EffectType.Deband,
                VsNamespace = "f3kdb",
                VsFunction = "Deband",
                ParameterDefinitions =
                [
                    new() { Name = "y", DisplayName = "Luma Threshold", ParameterType = EffectParameterType.Integer, DefaultValue = 64, MinValue = 0, MaxValue = 511 },
                    new() { Name = "cb", DisplayName = "Chroma Blue Threshold", ParameterType = EffectParameterType.Integer, DefaultValue = 64, MinValue = 0, MaxValue = 511 },
                    new() { Name = "cr", DisplayName = "Chroma Red Threshold", ParameterType = EffectParameterType.Integer, DefaultValue = 64, MinValue = 0, MaxValue = 511 },
                    new() { Name = "grainy", DisplayName = "Grain (Luma)", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 64 },
                    new() { Name = "grainc", DisplayName = "Grain (Chroma)", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 64 }
                ]
            },

            // Grain effect
            new EffectDefinition
            {
                Name = "Add Grain",
                Description = "Add film grain texture",
                Category = "Stylize",
                EffectType = EffectType.Grain,
                VsNamespace = "grain",
                VsFunction = "Add",
                ParameterDefinitions =
                [
                    new() { Name = "var", DisplayName = "Variance", Description = "Grain intensity", ParameterType = EffectParameterType.Float, DefaultValue = 1.0, MinValue = 0.0, MaxValue = 10.0 },
                    new() { Name = "constant", DisplayName = "Constant", Description = "Keep grain constant across frames", ParameterType = EffectParameterType.Boolean, DefaultValue = false }
                ]
            },

            // Levels adjustment
            new EffectDefinition
            {
                Name = "Levels",
                Description = "Adjust input/output levels",
                Category = "Color",
                EffectType = EffectType.ColorCorrection,
                VsNamespace = "std",
                VsFunction = "Levels",
                ParameterDefinitions =
                [
                    new() { Name = "min_in", DisplayName = "Input Black", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 255 },
                    new() { Name = "max_in", DisplayName = "Input White", ParameterType = EffectParameterType.Integer, DefaultValue = 255, MinValue = 0, MaxValue = 255 },
                    new() { Name = "min_out", DisplayName = "Output Black", ParameterType = EffectParameterType.Integer, DefaultValue = 0, MinValue = 0, MaxValue = 255 },
                    new() { Name = "max_out", DisplayName = "Output White", ParameterType = EffectParameterType.Integer, DefaultValue = 255, MinValue = 0, MaxValue = 255 },
                    new() { Name = "gamma", DisplayName = "Gamma", ParameterType = EffectParameterType.Float, DefaultValue = 1.0, MinValue = 0.1, MaxValue = 10.0 }
                ]
            },

            // Blur effect
            new EffectDefinition
            {
                Name = "Box Blur",
                Description = "Simple box blur",
                Category = "Blur",
                EffectType = EffectType.Blur,
                VsNamespace = "std",
                VsFunction = "BoxBlur",
                ParameterDefinitions =
                [
                    new() { Name = "hradius", DisplayName = "Horizontal Radius", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 100 },
                    new() { Name = "vradius", DisplayName = "Vertical Radius", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 0, MaxValue = 100 },
                    new() { Name = "hpasses", DisplayName = "Horizontal Passes", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 1, MaxValue = 10 },
                    new() { Name = "vpasses", DisplayName = "Vertical Passes", ParameterType = EffectParameterType.Integer, DefaultValue = 1, MinValue = 1, MaxValue = 10 }
                ]
            }
        ];
    }

    private void LoadBuiltInPresets()
    {
        // BM3D presets
        _presets["BM3D - Light"] = new EffectPreset
        {
            Name = "BM3D - Light",
            EffectName = "BM3D Denoise",
            Parameters = new() { ["sigma"] = 2.0, ["radius"] = 1 }
        };
        _presets["BM3D - Medium"] = new EffectPreset
        {
            Name = "BM3D - Medium",
            EffectName = "BM3D Denoise",
            Parameters = new() { ["sigma"] = 5.0, ["radius"] = 1 }
        };
        _presets["BM3D - Strong"] = new EffectPreset
        {
            Name = "BM3D - Strong",
            EffectName = "BM3D Denoise",
            Parameters = new() { ["sigma"] = 10.0, ["radius"] = 2 }
        };

        // Resize presets
        _presets["1080p"] = new EffectPreset
        {
            Name = "1080p",
            EffectName = "Resize (Lanczos)",
            Parameters = new() { ["width"] = 1920, ["height"] = 1080 }
        };
        _presets["720p"] = new EffectPreset
        {
            Name = "720p",
            EffectName = "Resize (Lanczos)",
            Parameters = new() { ["width"] = 1280, ["height"] = 720 }
        };
        _presets["4K"] = new EffectPreset
        {
            Name = "4K",
            EffectName = "Resize (Lanczos)",
            Parameters = new() { ["width"] = 3840, ["height"] = 2160 }
        };
    }
}

/// <summary>
/// Represents a saved effect configuration
/// </summary>
public class EffectPreset
{
    public string Name { get; set; } = "";
    public string EffectName { get; set; } = "";
    public Dictionary<string, object?> Parameters { get; set; } = [];
}
