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
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=2)
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
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=0)
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
                VapourSynthScript = @"
import vapoursynth as vs
from vsrealesrgan import realesrgan
core = vs.core
clip = video_in
clip = realesrgan(clip, model=1, scale=2)
clip.set_output()"
            },

            // Denoising
            new RestorePreset
            {
                Name = "Light Denoise",
                Description = "Subtle noise reduction preserving detail",
                Icon = "\uE9F5",
                TaskType = RestoreTaskType.Denoise,
                Category = "Denoise",
                RequiresGpu = false,
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma=3)
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
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma=6)
clip = core.bm3d.VFinal(clip, clip, sigma=6)
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
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.bm3d.VBasic(clip, sigma=12)
clip = core.bm3d.VFinal(clip, clip, sigma=12)
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
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
clip = core.dfttest.DFTTest(clip, sigma=8, tbsize=3)
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
                Name = "Interpolate to 60fps",
                Description = "AI frame interpolation using RIFE",
                Icon = "\uE916",
                TaskType = RestoreTaskType.Interpolate,
                AiModel = "rife",
                Category = "Interpolate",
                RequiresGpu = true,
                VapourSynthScript = @"
import vapoursynth as vs
from vsrife import rife
core = vs.core
clip = video_in
clip = rife(clip, multi=2)
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
                VapourSynthScript = @"
import vapoursynth as vs
from vsrife import rife
core = vs.core
clip = video_in
clip = rife(clip, multi=4)
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
                VapourSynthScript = @"
import vapoursynth as vs
core = vs.core
clip = video_in
sup = core.mv.Super(clip)
bv = core.mv.Analyse(sup, isb=True)
fv = core.mv.Analyse(sup, isb=False)
clip = core.mv.FlowFPS(clip, sup, bv, fv, num=60, den=1)
clip.set_output()"
            },

            // Face Restoration
            new RestorePreset
            {
                Name = "Face Restore (GFPGAN)",
                Description = "Restore and enhance faces in video",
                Icon = "\uE77B",
                TaskType = RestoreTaskType.FaceRestore,
                AiModel = "gfpgan",
                Category = "Face",
                RequiresGpu = true,
                VapourSynthScript = @"
import vapoursynth as vs
# GFPGAN integration placeholder
core = vs.core
clip = video_in
# clip = gfpgan_restore(clip)
clip.set_output()"
            },
            new RestorePreset
            {
                Name = "Face Restore (CodeFormer)",
                Description = "Advanced face restoration with CodeFormer",
                Icon = "\uE77B",
                TaskType = RestoreTaskType.FaceRestore,
                AiModel = "codeformer",
                Category = "Face",
                RequiresGpu = true,
                VapourSynthScript = @"
import vapoursynth as vs
# CodeFormer integration placeholder
core = vs.core
clip = video_in
# clip = codeformer_restore(clip)
clip.set_output()"
            },

            // Colorization
            new RestorePreset
            {
                Name = "Colorize B&W",
                Description = "AI colorization of black & white footage",
                Icon = "\uE790",
                TaskType = RestoreTaskType.Colorize,
                AiModel = "deoldify",
                Category = "Color",
                RequiresGpu = true,
                VapourSynthScript = @"
import vapoursynth as vs
# DeOldify integration placeholder
core = vs.core
clip = video_in
# clip = deoldify_colorize(clip)
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

    public RestoreJob()
    {
        Id = _nextId++;
    }

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
