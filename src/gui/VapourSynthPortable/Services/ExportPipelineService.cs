using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Unified export pipeline service that generates VapourSynth scripts for all export scenarios.
/// All exports flow through this single pipeline regardless of complexity.
/// </summary>
public class ExportPipelineService
{
    private static readonly ILogger<ExportPipelineService> _logger = LoggingService.GetLogger<ExportPipelineService>();
    private readonly EffectService _effectService;

    public ExportPipelineService(EffectService effectService)
    {
        _effectService = effectService;
    }

    /// <summary>
    /// Generates a complete VapourSynth script for the unified export pipeline.
    /// Handles all export scenarios: single file, timeline, effects, color grading, restoration.
    /// </summary>
    public string GenerateUnifiedScript(UnifiedExportSettings settings)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("import vapoursynth as vs");
        sb.AppendLine("core = vs.core");
        sb.AppendLine();
        sb.AppendLine($"# VapourSynth Studio - Unified Export Pipeline");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# Output: {settings.OutputPath}");
        sb.AppendLine();

        // Track the current output variable
        string outputVar;

        // === SECTION 1: SOURCE LOADING ===
        if (settings.SourceType == ExportSourceType.Timeline && settings.Timeline?.HasClips == true)
        {
            outputVar = GenerateTimelineSection(sb, settings);
        }
        else
        {
            outputVar = GenerateSingleFileSection(sb, settings);
        }

        // === SECTION 2: GLOBAL COLOR GRADE (optional) ===
        if (settings.GlobalColorGrade != null)
        {
            outputVar = GenerateGlobalColorSection(sb, settings, outputVar);
        }

        // === SECTION 3: RESTORATION (optional) ===
        if (settings.IncludeRestoration && settings.Restoration != null)
        {
            outputVar = GenerateRestorationSection(sb, settings, outputVar);
        }

        // === SECTION 4: OUTPUT TRANSFORM (resolution/fps) ===
        outputVar = GenerateOutputTransformSection(sb, settings, outputVar);

        // === SECTION 5: SET OUTPUT ===
        sb.AppendLine();
        sb.AppendLine("# === OUTPUT ===");
        if (outputVar != "output")
        {
            sb.AppendLine($"output = {outputVar}");
        }
        sb.AppendLine("output.set_output()");

        var script = sb.ToString();
        _logger.LogDebug("Generated unified script ({Lines} lines)", script.Split('\n').Length);

        return script;
    }

    /// <summary>
    /// Generates the source loading section for a single file
    /// </summary>
    private string GenerateSingleFileSection(StringBuilder sb, UnifiedExportSettings settings)
    {
        sb.AppendLine("# === SOURCE ===");

        var escapedPath = EscapePath(settings.SingleFilePath ?? "");
        sb.AppendLine($"output = core.lsmas.LWLibavSource(r'{escapedPath}')");
        sb.AppendLine();

        return "output";
    }

    /// <summary>
    /// Generates the timeline section with all clips, effects, and per-clip color grading
    /// </summary>
    private string GenerateTimelineSection(StringBuilder sb, UnifiedExportSettings settings)
    {
        sb.AppendLine("# === TIMELINE ===");

        var clipVars = new List<string>();
        var clipIndex = 0;

        foreach (var track in settings.Timeline!.Tracks
            .Where(t => t.TrackType == TrackType.Video && t.IsVisible))
        {
            foreach (var clip in track.Clips.OrderBy(c => c.StartFrame))
            {
                var sourceVar = $"src_{clipIndex}";
                var currentVar = $"clip_{clipIndex}";

                // Load source
                sb.AppendLine($"# Clip {clipIndex}: {clip.Name}");
                var escapedClipPath = EscapePath(clip.SourcePath);
                sb.AppendLine($"{sourceVar} = core.lsmas.LWLibavSource(r'{escapedClipPath}')");

                // Trim to in/out points
                if (clip.SourceInFrame > 0 || clip.SourceOutFrame < clip.SourceDurationFrames)
                {
                    sb.AppendLine($"{currentVar} = core.std.Trim({sourceVar}, first={clip.SourceInFrame}, last={clip.SourceOutFrame - 1})");
                }
                else
                {
                    sb.AppendLine($"{currentVar} = {sourceVar}");
                }

                // Apply effects chain
                if (clip.HasEffects)
                {
                    var effectCode = _effectService.GenerateEffectChainCode(clip, currentVar);
                    if (!string.IsNullOrEmpty(effectCode))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"# Effects for {clip.Name}");
                        sb.AppendLine(effectCode);
                        currentVar = _effectService.GetFinalOutputVar(clip, currentVar);
                    }
                }

                // Apply per-clip color grading
                if (clip.HasColorGrade && clip.ColorGrade != null)
                {
                    var colorVar = $"color_{clipIndex}";
                    sb.AppendLine();
                    sb.AppendLine($"# Color grading for {clip.Name}");
                    var colorCode = GenerateColorGradeCode(clip.ColorGrade, currentVar, colorVar);
                    sb.AppendLine(colorCode);
                    currentVar = colorVar;
                }

                clipVars.Add(currentVar);
                sb.AppendLine();
                clipIndex++;
            }
        }

        // Combine clips
        string outputVar;
        if (clipVars.Count > 1)
        {
            sb.AppendLine("# Combine all clips");
            sb.AppendLine($"output = core.std.Splice([{string.Join(", ", clipVars)}])");
            outputVar = "output";
        }
        else if (clipVars.Count == 1)
        {
            outputVar = clipVars[0];
        }
        else
        {
            // Empty timeline - create black frame placeholder
            sb.AppendLine("# No clips - creating placeholder");
            sb.AppendLine("output = core.std.BlankClip(width=1920, height=1080, length=1)");
            outputVar = "output";
        }

        sb.AppendLine();
        return outputVar;
    }

    /// <summary>
    /// Generates the global color grading section
    /// </summary>
    private string GenerateGlobalColorSection(StringBuilder sb, UnifiedExportSettings settings, string inputVar)
    {
        sb.AppendLine("# === GLOBAL COLOR GRADE ===");

        var outputVar = "global_color";
        var colorCode = GenerateColorGradeCode(settings.GlobalColorGrade!, inputVar, outputVar);
        sb.AppendLine(colorCode);
        sb.AppendLine();

        return outputVar;
    }

    /// <summary>
    /// Generates the restoration section
    /// </summary>
    private string GenerateRestorationSection(StringBuilder sb, UnifiedExportSettings settings, string inputVar)
    {
        sb.AppendLine("# === RESTORATION ===");
        sb.AppendLine($"# Preset: {settings.Restoration!.PresetName}");
        sb.AppendLine();

        // The restoration script expects 'video_in' as input and sets 'output'
        // We need to adapt it to our pipeline
        sb.AppendLine($"video_in = {inputVar}");
        sb.AppendLine();

        // Insert the restoration script (which should produce 'output')
        var restorationScript = settings.Restoration.GeneratedScript;
        if (!string.IsNullOrEmpty(restorationScript))
        {
            sb.AppendLine(restorationScript);
        }

        // The restoration script should have set 'output', but if not, fallback
        sb.AppendLine();
        sb.AppendLine("# Ensure restoration output is captured");
        sb.AppendLine("try:");
        sb.AppendLine("    restored = output");
        sb.AppendLine("except NameError:");
        sb.AppendLine("    restored = video_in  # Fallback if restoration didn't set output");
        sb.AppendLine();

        return "restored";
    }

    /// <summary>
    /// Generates the output transform section (resolution, frame rate)
    /// </summary>
    private string GenerateOutputTransformSection(StringBuilder sb, UnifiedExportSettings settings, string inputVar)
    {
        var hasTransform = settings.OutputWidth > 0 || settings.OutputHeight > 0 || settings.OutputFrameRate > 0;

        if (!hasTransform)
        {
            return inputVar;
        }

        sb.AppendLine("# === OUTPUT TRANSFORM ===");
        var currentVar = inputVar;

        // Resolution scaling
        if (settings.OutputWidth > 0 && settings.OutputHeight > 0)
        {
            sb.AppendLine($"# Scale to {settings.OutputWidth}x{settings.OutputHeight}");
            sb.AppendLine($"scaled = core.resize.Lanczos({currentVar}, width={settings.OutputWidth}, height={settings.OutputHeight})");
            currentVar = "scaled";
        }

        // Frame rate conversion
        if (settings.OutputFrameRate > 0)
        {
            var (fpsNum, fpsDen) = GetFpsFraction(settings.OutputFrameRate);
            sb.AppendLine($"# Convert to {settings.OutputFrameRate} fps");
            sb.AppendLine($"output_fps = core.std.AssumeFPS({currentVar}, fpsnum={fpsNum}, fpsden={fpsDen})");
            currentVar = "output_fps";
        }

        sb.AppendLine();
        return currentVar;
    }

    /// <summary>
    /// Generates VapourSynth code for color grading with LUT support
    /// </summary>
    private string GenerateColorGradeCode(ColorGrade grade, string inputVar, string outputVar)
    {
        var lines = new List<string>();
        var currentVar = inputVar;
        var stepIndex = 0;

        // Apply exposure and contrast
        if (Math.Abs(grade.Exposure) > 0.001 || Math.Abs(grade.Contrast) > 0.001)
        {
            var stepVar = $"{outputVar}_exp";
            var expMult = Math.Pow(2, grade.Exposure);
            var contrastFactor = (grade.Contrast + 100) / 100.0;

            lines.Add($"{stepVar} = core.std.Expr({currentVar}, ['x {expMult:F4} * 128 - {contrastFactor:F4} * 128 +'])");
            currentVar = stepVar;
            stepIndex++;
        }

        // Apply saturation adjustment
        if (Math.Abs(grade.Saturation) > 0.001)
        {
            var stepVar = $"{outputVar}_sat";
            var satFactor = (grade.Saturation + 100) / 100.0;

            lines.Add($"# Saturation: {satFactor:F2}x");
            lines.Add($"{stepVar} = core.std.Expr({currentVar}, ['', 'x 128 - {satFactor:F4} * 128 +', 'x 128 - {satFactor:F4} * 128 +'])");
            currentVar = stepVar;
            stepIndex++;
        }

        // Apply temperature
        if (Math.Abs(grade.Temperature) > 0.001)
        {
            var stepVar = $"{outputVar}_temp";
            var tempShift = grade.Temperature * 0.5;

            lines.Add($"# Temperature: {grade.Temperature:F1}");
            if (grade.Temperature > 0)
            {
                lines.Add($"{stepVar} = core.std.Expr({currentVar}, ['x {Math.Abs(tempShift):F1} +', '', 'x {Math.Abs(tempShift):F1} -'])");
            }
            else
            {
                lines.Add($"{stepVar} = core.std.Expr({currentVar}, ['x {Math.Abs(tempShift):F1} -', '', 'x {Math.Abs(tempShift):F1} +'])");
            }
            currentVar = stepVar;
            stepIndex++;
        }

        // Apply lift/gamma/gain
        if (HasLiftGammaGain(grade))
        {
            var stepVar = $"{outputVar}_lgg";
            var gammaVal = 1.0 + grade.GammaMaster * 0.5;

            lines.Add($"# Lift/Gamma/Gain");
            lines.Add($"{stepVar} = core.std.Levels({currentVar}, gamma={gammaVal:F3})");
            currentVar = stepVar;
            stepIndex++;
        }

        // Apply LUT if specified (ENABLED - with error handling)
        if (!string.IsNullOrEmpty(grade.LutPath) && File.Exists(grade.LutPath))
        {
            var stepVar = $"{outputVar}_lut";
            var lutPath = EscapePath(grade.LutPath);

            lines.Add($"# LUT: {Path.GetFileName(grade.LutPath)}");
            lines.Add($"try:");
            lines.Add($"    {stepVar}_full = core.timecube.Cube({currentVar}, cube=r'{lutPath}')");

            // Apply LUT intensity via blending if not 100%
            if (Math.Abs(grade.LutIntensity - 1.0) > 0.001)
            {
                lines.Add($"    {stepVar} = core.std.Merge({currentVar}, {stepVar}_full, weight=[{grade.LutIntensity:F3}])");
            }
            else
            {
                lines.Add($"    {stepVar} = {stepVar}_full");
            }

            lines.Add($"except AttributeError:");
            lines.Add($"    {stepVar} = {currentVar}  # timecube plugin not available");

            currentVar = stepVar;
            stepIndex++;
        }

        // Final assignment
        if (currentVar != inputVar)
        {
            lines.Add($"{outputVar} = {currentVar}");
        }
        else
        {
            lines.Add($"{outputVar} = {inputVar}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool HasLiftGammaGain(ColorGrade grade)
    {
        return Math.Abs(grade.LiftX) > 0.001 || Math.Abs(grade.LiftY) > 0.001 || Math.Abs(grade.LiftMaster) > 0.001 ||
               Math.Abs(grade.GammaX) > 0.001 || Math.Abs(grade.GammaY) > 0.001 || Math.Abs(grade.GammaMaster) > 0.001 ||
               Math.Abs(grade.GainX) > 0.001 || Math.Abs(grade.GainY) > 0.001 || Math.Abs(grade.GainMaster) > 0.001;
    }

    /// <summary>
    /// Escapes a file path for use in Python raw strings
    /// </summary>
    private static string EscapePath(string path)
    {
        // For raw strings (r'...'), we only need to escape single quotes
        // Backslashes are preserved as-is in raw strings
        return path.Replace("'", "\\'");
    }

    /// <summary>
    /// Converts a frame rate to a fraction (numerator/denominator)
    /// </summary>
    private static (int num, int den) GetFpsFraction(double fps)
    {
        // Common frame rates with their exact fractions
        return fps switch
        {
            23.976 or 23.98 => (24000, 1001),
            29.97 or 29.970 => (30000, 1001),
            59.94 or 59.940 => (60000, 1001),
            _ => ((int)(fps * 1000), 1000)
        };
    }
}
