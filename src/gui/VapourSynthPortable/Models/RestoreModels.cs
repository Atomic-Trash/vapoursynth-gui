using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

public enum RestoreTaskType
{
    Upscale,
    Denoise,
    Deinterlace,
    Interpolate,
    FaceRestore,
    Colorize,
    Stabilize,
    Custom
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum ParameterType
{
    Float,
    Int,
    Bool,
    Enum,
    String
}

/// <summary>
/// Represents a configurable parameter for a preset
/// </summary>
public partial class PresetParameter : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private ParameterType _type;

    [ObservableProperty]
    private object? _defaultValue;

    [ObservableProperty]
    private object? _minValue;

    [ObservableProperty]
    private object? _maxValue;

    [ObservableProperty]
    private object? _currentValue;

    [ObservableProperty]
    private string[]? _enumValues;

    [ObservableProperty]
    private double _step = 1;

    public PresetParameter Clone()
    {
        return new PresetParameter
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            Type = Type,
            DefaultValue = DefaultValue,
            MinValue = MinValue,
            MaxValue = MaxValue,
            CurrentValue = CurrentValue ?? DefaultValue,
            EnumValues = EnumValues,
            Step = Step
        };
    }

    public void ResetToDefault()
    {
        CurrentValue = DefaultValue;
    }
}

/// <summary>
/// Represents a restoration preset/quick action
/// </summary>
public partial class RestorePreset : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _icon = "";

    [ObservableProperty]
    private RestoreTaskType _taskType;

    [ObservableProperty]
    private string _vapourSynthScript = "";

    [ObservableProperty]
    private string _aiModel = "";

    [ObservableProperty]
    private bool _requiresGpu;

    [ObservableProperty]
    private string _category = "";

    [ObservableProperty]
    private List<PresetParameter> _parameters = [];

    [ObservableProperty]
    private bool _isFavorite;

    public bool HasParameters => Parameters.Count > 0;

    public string GenerateScript()
    {
        if (Parameters.Count == 0)
            return VapourSynthScript;

        var script = VapourSynthScript;
        foreach (var param in Parameters)
        {
            var value = param.CurrentValue ?? param.DefaultValue;
            var replacement = param.Type switch
            {
                ParameterType.Bool => value is true ? "True" : "False",
                ParameterType.String => $"\"{value}\"",
                _ => value?.ToString() ?? ""
            };
            script = script.Replace($"{{{param.Name}}}", replacement);
        }
        return script;
    }

    public RestorePreset CloneWithParameters()
    {
        return new RestorePreset
        {
            Name = Name,
            Description = Description,
            Icon = Icon,
            TaskType = TaskType,
            VapourSynthScript = VapourSynthScript,
            AiModel = AiModel,
            RequiresGpu = RequiresGpu,
            Category = Category,
            Parameters = Parameters.Select(p => p.Clone()).ToList()
        };
    }

    public static List<RestorePreset> GetPresets()
    {
        return
        [
            // Upscaling
            new RestorePreset
            {
                Name = "Upscale 2x",
                Description = "AI upscale to 2x resolution using Real-ESRGAN",
                Icon = "\uE8B3",
                TaskType = RestoreTaskType.Upscale,
                AiModel = "realesrgan-x2plus",
                Category = "Upscale",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "tile_size",
                        DisplayName = "Tile Size",
                        Description = "Processing tile size (larger = faster, more VRAM)",
                        Type = ParameterType.Int,
                        DefaultValue = 512,
                        MinValue = 128,
                        MaxValue = 1024,
                        Step = 64
                    },
                    new PresetParameter
                    {
                        Name = "denoise",
                        DisplayName = "Denoise Strength",
                        Description = "Apply denoising during upscale",
                        Type = ParameterType.Float,
                        DefaultValue = 0.0,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.1
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=2, tile={tile_size}, denoise={denoise})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Upscale 4x",
                Description = "AI upscale to 4x resolution using Real-ESRGAN",
                Icon = "\uE740",
                TaskType = RestoreTaskType.Upscale,
                AiModel = "realesrgan-x4plus",
                Category = "Upscale",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "tile_size",
                        DisplayName = "Tile Size",
                        Description = "Processing tile size (larger = faster, more VRAM)",
                        Type = ParameterType.Int,
                        DefaultValue = 512,
                        MinValue = 128,
                        MaxValue = 1024,
                        Step = 64
                    },
                    new PresetParameter
                    {
                        Name = "denoise",
                        DisplayName = "Denoise Strength",
                        Description = "Apply denoising during upscale",
                        Type = ParameterType.Float,
                        DefaultValue = 0.0,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.1
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=0, tile={tile_size}, denoise={denoise})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Upscale Anime 2x",
                Description = "Optimized for anime/cartoon content",
                Icon = "\uE8B3",
                TaskType = RestoreTaskType.Upscale,
                AiModel = "realesrgan-x4plus-anime",
                Category = "Upscale",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "scale",
                        DisplayName = "Scale Factor",
                        Description = "Output scale multiplier",
                        Type = ParameterType.Int,
                        DefaultValue = 2,
                        MinValue = 2,
                        MaxValue = 4,
                        Step = 1
                    },
                    new PresetParameter
                    {
                        Name = "tile_size",
                        DisplayName = "Tile Size",
                        Description = "Processing tile size (larger = faster, more VRAM)",
                        Type = ParameterType.Int,
                        DefaultValue = 512,
                        MinValue = 128,
                        MaxValue = 1024,
                        Step = 64
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=1, scale={scale}, tile={tile_size})
clip.set_output()"
            },

            // Denoising
            new RestorePreset
            {
                Name = "BM3D Denoise",
                Description = "High-quality block-matching denoise with adjustable strength",
                Icon = "\uE9F5",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "sigma",
                        DisplayName = "Denoise Strength",
                        Description = "Noise reduction strength (higher = more smoothing)",
                        Type = ParameterType.Float,
                        DefaultValue = 6.0,
                        MinValue = 1.0,
                        MaxValue = 20.0,
                        Step = 0.5
                    },
                    new PresetParameter
                    {
                        Name = "two_pass",
                        DisplayName = "Two-Pass Mode",
                        Description = "Enable VFinal for higher quality (slower)",
                        Type = ParameterType.Bool,
                        DefaultValue = true
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma={sigma})
if {two_pass}:
    clip = core.bm3d.VFinal(clip, clip, sigma={sigma})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Light Denoise",
                Description = "Subtle noise reduction preserving detail",
                Icon = "\uE9F5",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "sigma",
                        DisplayName = "Strength",
                        Description = "Noise reduction strength",
                        Type = ParameterType.Float,
                        DefaultValue = 3.0,
                        MinValue = 1.0,
                        MaxValue = 6.0,
                        Step = 0.5
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma={sigma})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Medium Denoise",
                Description = "Balanced noise reduction",
                Icon = "\uE9F5",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "sigma",
                        DisplayName = "Strength",
                        Description = "Noise reduction strength",
                        Type = ParameterType.Float,
                        DefaultValue = 6.0,
                        MinValue = 4.0,
                        MaxValue = 10.0,
                        Step = 0.5
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma={sigma})
clip = core.bm3d.VFinal(clip, clip, sigma={sigma})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Heavy Denoise",
                Description = "Strong noise reduction for very noisy footage",
                Icon = "\uE9F5",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "sigma",
                        DisplayName = "Strength",
                        Description = "Noise reduction strength",
                        Type = ParameterType.Float,
                        DefaultValue = 12.0,
                        MinValue = 8.0,
                        MaxValue = 25.0,
                        Step = 1.0
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma={sigma})
clip = core.bm3d.VFinal(clip, clip, sigma={sigma})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Film Grain Removal",
                Description = "Remove film grain while preserving detail",
                Icon = "\uE91B",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "sigma",
                        DisplayName = "Grain Strength",
                        Description = "How much grain to remove",
                        Type = ParameterType.Float,
                        DefaultValue = 8.0,
                        MinValue = 4.0,
                        MaxValue = 16.0,
                        Step = 1.0
                    },
                    new PresetParameter
                    {
                        Name = "tbsize",
                        DisplayName = "Temporal Block Size",
                        Description = "Temporal analysis window (higher = more temporal smoothing)",
                        Type = ParameterType.Int,
                        DefaultValue = 3,
                        MinValue = 1,
                        MaxValue = 5,
                        Step = 2
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.dfttest.DFTTest(clip, sigma={sigma}, tbsize={tbsize})
clip.set_output()"
            },

            // Deinterlacing
            new RestorePreset
            {
                Name = "Deinterlace (NNEDI3)",
                Description = "High quality deinterlacing with neural network",
                Icon = "\uE8A9",
                TaskType = RestoreTaskType.Deinterlace,
                Category = "Deinterlace",
                RequiresGpu = false,
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.nnedi3.nnedi3(clip, field=1, dh=True)
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Deinterlace (EEDI3)",
                Description = "Edge-directed interpolation deinterlacing",
                Icon = "\uE8A9",
                TaskType = RestoreTaskType.Deinterlace,
                Category = "Deinterlace",
                RequiresGpu = false,
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.eedi3.eedi3(clip, field=1, dh=True)
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "IVTC (Inverse Telecine)",
                Description = "Remove 3:2 pulldown from film content",
                Icon = "\uE8A9",
                TaskType = RestoreTaskType.Deinterlace,
                Category = "Deinterlace",
                RequiresGpu = false,
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.vivtc.VFM(clip, order=1)
clip = core.vivtc.VDecimate(clip)
clip.set_output()"
            },

            // Frame Interpolation
            new RestorePreset
            {
                Name = "RIFE Interpolation",
                Description = "AI frame interpolation using RIFE",
                Icon = "\uE916",
                TaskType = RestoreTaskType.Interpolate,
                AiModel = "rife",
                Category = "Interpolate",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "multi",
                        DisplayName = "Frame Multiplier",
                        Description = "How many times to multiply frame rate",
                        Type = ParameterType.Int,
                        DefaultValue = 2,
                        MinValue = 2,
                        MaxValue = 8,
                        Step = 1
                    },
                    new PresetParameter
                    {
                        Name = "scene_detect",
                        DisplayName = "Scene Detection",
                        Description = "Threshold for scene change detection (0 = disabled)",
                        Type = ParameterType.Float,
                        DefaultValue = 0.15,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.05
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrife import rife
core = vs.core
clip = video_in
clip = rife(clip, multi={multi}, sc_threshold={scene_detect})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Interpolate to 60fps",
                Description = "AI frame interpolation to 60fps",
                Icon = "\uE916",
                TaskType = RestoreTaskType.Interpolate,
                AiModel = "rife",
                Category = "Interpolate",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "scene_detect",
                        DisplayName = "Scene Detection",
                        Description = "Threshold for scene change detection",
                        Type = ParameterType.Float,
                        DefaultValue = 0.15,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.05
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrife import rife
core = vs.core
clip = video_in
clip = rife(clip, multi=2, sc_threshold={scene_detect})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Interpolate to 120fps",
                Description = "AI frame interpolation to 120fps",
                Icon = "\uE916",
                TaskType = RestoreTaskType.Interpolate,
                AiModel = "rife",
                Category = "Interpolate",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "scene_detect",
                        DisplayName = "Scene Detection",
                        Description = "Threshold for scene change detection",
                        Type = ParameterType.Float,
                        DefaultValue = 0.15,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.05
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
from vsrife import rife
core = vs.core
clip = video_in
clip = rife(clip, multi=4, sc_threshold={scene_detect})
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Smooth Motion (MVTools)",
                Description = "Motion-compensated frame interpolation",
                Icon = "\uE916",
                TaskType = RestoreTaskType.Interpolate,
                Category = "Interpolate",
                RequiresGpu = false,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "target_fps",
                        DisplayName = "Target FPS",
                        Description = "Output frame rate",
                        Type = ParameterType.Int,
                        DefaultValue = 60,
                        MinValue = 24,
                        MaxValue = 120,
                        Step = 1
                    },
                    new PresetParameter
                    {
                        Name = "block_size",
                        DisplayName = "Block Size",
                        Description = "Motion analysis block size",
                        Type = ParameterType.Int,
                        DefaultValue = 16,
                        MinValue = 8,
                        MaxValue = 32,
                        Step = 8
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
sup = core.mv.Super(clip)
bv = core.mv.Analyse(sup, isb=True, blksize={block_size})
fv = core.mv.Analyse(sup, isb=False, blksize={block_size})
clip = core.mv.FlowFPS(clip, sup, bv, fv, num={target_fps}, den=1)
clip.set_output()"
            },

            // Face Restoration
            new RestorePreset
            {
                Name = "Face Restore (GFPGAN)",
                Description = "Restore and enhance faces in video using GFPGAN",
                Icon = "\uE77B",
                TaskType = RestoreTaskType.FaceRestore,
                AiModel = "gfpgan",
                Category = "Face",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "fidelity_weight",
                        DisplayName = "Fidelity Weight",
                        Description = "Balance between quality and fidelity (0=quality, 1=fidelity)",
                        DefaultValue = 0.5,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.1
                    },
                    new PresetParameter
                    {
                        Name = "upscale",
                        DisplayName = "Upscale Factor",
                        Description = "Output upscale factor",
                        DefaultValue = 2,
                        MinValue = 1,
                        MaxValue = 4,
                        Step = 1
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in

# GFPGAN face restoration via vsmlrt ONNX backend
try:
    from vsmlrt import GFPGAN
    clip = GFPGAN(clip, weight={fidelity_weight}, scale={upscale})
except ImportError:
    # Fallback: apply mild enhancement if GFPGAN not available
    clip = core.resize.Lanczos(clip, width=clip.width*{upscale}, height=clip.height*{upscale})
    # Apply slight sharpening as minimal enhancement
    clip = core.std.Convolution(clip, [0, -1, 0, -1, 5, -1, 0, -1, 0])

clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Face Restore (CodeFormer)",
                Description = "Advanced face restoration with CodeFormer neural network",
                Icon = "\uE77B",
                TaskType = RestoreTaskType.FaceRestore,
                AiModel = "codeformer",
                Category = "Face",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "fidelity_weight",
                        DisplayName = "Fidelity Weight",
                        Description = "Balance between quality and fidelity (0=quality, 1=fidelity)",
                        DefaultValue = 0.7,
                        MinValue = 0.0,
                        MaxValue = 1.0,
                        Step = 0.1
                    },
                    new PresetParameter
                    {
                        Name = "upscale",
                        DisplayName = "Upscale Factor",
                        Description = "Output upscale factor",
                        DefaultValue = 2,
                        MinValue = 1,
                        MaxValue = 4,
                        Step = 1
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in

# CodeFormer face restoration via vsmlrt ONNX backend
try:
    from vsmlrt import CodeFormer
    clip = CodeFormer(clip, weight={fidelity_weight}, scale={upscale})
except ImportError:
    # Fallback: apply basic enhancement if CodeFormer not available
    clip = core.resize.Lanczos(clip, width=clip.width*{upscale}, height=clip.height*{upscale})
    # Apply adaptive sharpening for facial detail
    clip = core.std.Convolution(clip, [0, -1, 0, -1, 5, -1, 0, -1, 0])

clip.set_output()"
            },

            // Colorization
            new RestorePreset
            {
                Name = "Colorize B&W",
                Description = "AI colorization of black & white footage using DeOldify",
                Icon = "\uE790",
                TaskType = RestoreTaskType.Colorize,
                AiModel = "deoldify",
                Category = "Color",
                RequiresGpu = true,
                Parameters =
                [
                    new PresetParameter
                    {
                        Name = "render_factor",
                        DisplayName = "Render Factor",
                        Description = "Quality vs speed (higher = better quality but slower)",
                        DefaultValue = 21,
                        MinValue = 7,
                        MaxValue = 45,
                        Step = 2
                    },
                    new PresetParameter
                    {
                        Name = "saturation",
                        DisplayName = "Saturation",
                        Description = "Color saturation boost",
                        DefaultValue = 1.0,
                        MinValue = 0.5,
                        MaxValue = 2.0,
                        Step = 0.1
                    }
                ],
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in

# DeOldify colorization via vsmlrt ONNX backend
try:
    from vsmlrt import DeOldify
    clip = DeOldify(clip, render_factor={render_factor})
    # Apply saturation adjustment
    if {saturation} != 1.0:
        clip = core.std.Expr(clip, ['', 'x 128 - {saturation} * 128 +', 'x 128 - {saturation} * 128 +'])
except ImportError:
    # Fallback: convert grayscale to color space but keep grayscale values
    if clip.format.color_family == vs.GRAY:
        clip = core.resize.Bicubic(clip, format=vs.YUV444P8)

clip.set_output()"
            },

            // Stabilization
            new RestorePreset
            {
                Name = "Stabilize Video",
                Description = "Remove camera shake and stabilize footage",
                Icon = "\uE809",
                TaskType = RestoreTaskType.Stabilize,
                Category = "Stabilize",
                RequiresGpu = false,
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.stab.Stabilize(clip)
clip.set_output()"
            }
        ];
    }
}

/// <summary>
/// Represents a restoration job in the queue
/// </summary>
public partial class RestoreJob : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _sourcePath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private RestorePreset? _preset;

    [ObservableProperty]
    private string _customScript = "";

    [ObservableProperty]
    private ProcessingStatus _status = ProcessingStatus.Pending;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Pending";

    [ObservableProperty]
    private int _currentFrame;

    [ObservableProperty]
    private int _totalFrames;

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    [ObservableProperty]
    private TimeSpan _estimatedTimeRemaining;

    [ObservableProperty]
    private DateTime _startTime;

    [ObservableProperty]
    private DateTime? _endTime;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private BitmapSource? _thumbnail;

    public RestoreJob()
    {
        Id = _nextId++;
    }

    public string SourceFileName => Path.GetFileName(SourcePath);
    public string ProgressText => $"{Progress:F1}% ({CurrentFrame}/{TotalFrames} frames)";
}

/// <summary>
/// Settings for AI model execution
/// </summary>
public partial class AIModelSettings : ObservableObject
{
    [ObservableProperty]
    private bool _useGpu = true;

    [ObservableProperty]
    private int _gpuId;

    [ObservableProperty]
    private int _tileSize = 512;

    [ObservableProperty]
    private int _threads = 4;

    [ObservableProperty]
    private string _modelPath = "";

    [ObservableProperty]
    private bool _fp16 = true;
}

/// <summary>
/// Stores restoration settings applied to a media source.
/// Used for cross-page communication between Restore and Export pages.
/// </summary>
public partial class RestorationSettings : ObservableObject
{
    /// <summary>
    /// Name of the applied preset
    /// </summary>
    [ObservableProperty]
    private string _presetName = "";

    /// <summary>
    /// Description of the preset
    /// </summary>
    [ObservableProperty]
    private string _presetDescription = "";

    /// <summary>
    /// The task type (Upscale, Denoise, etc.)
    /// </summary>
    [ObservableProperty]
    private RestoreTaskType _taskType;

    /// <summary>
    /// Generated VapourSynth script code for the restoration
    /// </summary>
    [ObservableProperty]
    private string _generatedScript = "";

    /// <summary>
    /// Snapshot of parameter values at time of application
    /// </summary>
    [ObservableProperty]
    private List<ParameterSnapshot> _parameters = [];

    /// <summary>
    /// Whether restoration is enabled for export
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// When the restoration was applied
    /// </summary>
    [ObservableProperty]
    private DateTime _appliedAt = DateTime.Now;

    /// <summary>
    /// Whether the preset requires GPU
    /// </summary>
    [ObservableProperty]
    private bool _requiresGpu;

    /// <summary>
    /// AI model name if applicable
    /// </summary>
    [ObservableProperty]
    private string _aiModel = "";
}

/// <summary>
/// Snapshot of a parameter value for serialization
/// </summary>
public class ParameterSnapshot
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ParameterType Type { get; set; }
    public object? Value { get; set; }
}
