using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for applying color grading adjustments to images
/// </summary>
public class ColorGradingService
{
    private static readonly ILogger<ColorGradingService> _logger = LoggingService.GetLogger<ColorGradingService>();

    private readonly LutService _lutService;

    public ColorGradingService()
    {
        _lutService = new LutService();
    }

    public ColorGradingService(LutService lutService)
    {
        _lutService = lutService;
    }

    /// <summary>
    /// Apply full color grading pipeline to an image
    /// </summary>
    public BitmapSource? ApplyGrade(BitmapSource source, ColorGrade grade)
    {
        if (source == null || grade == null)
            return source;

        try
        {
            // Convert to 32-bit BGRA for processing
            var formatConvertedBitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            var width = formatConvertedBitmap.PixelWidth;
            var height = formatConvertedBitmap.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];
            formatConvertedBitmap.CopyPixels(pixels, stride, 0);

            // Build processing pipeline
            var hasAdjustments = HasColorAdjustments(grade);
            var hasLiftGammaGain = HasLiftGammaGain(grade);

            // Process each pixel
            for (int i = 0; i < pixels.Length; i += 4)
            {
                // Read BGRA
                float b = pixels[i] / 255f;
                float g = pixels[i + 1] / 255f;
                float r = pixels[i + 2] / 255f;
                // Alpha preserved

                // 1. Apply exposure
                if (Math.Abs(grade.Exposure) > 0.001)
                {
                    var expMult = (float)Math.Pow(2, grade.Exposure);
                    r *= expMult;
                    g *= expMult;
                    b *= expMult;
                }

                // 2. Apply temperature and tint (simplified white balance)
                if (Math.Abs(grade.Temperature) > 0.001 || Math.Abs(grade.Tint) > 0.001)
                {
                    ApplyTemperatureTint(ref r, ref g, ref b, (float)grade.Temperature, (float)grade.Tint);
                }

                // 3. Apply lift/gamma/gain (color wheels)
                if (hasLiftGammaGain)
                {
                    ApplyLiftGammaGain(ref r, ref g, ref b, grade);
                }

                // 4. Apply contrast
                if (Math.Abs(grade.Contrast) > 0.001)
                {
                    var contrastFactor = (float)((grade.Contrast + 100) / 100.0);
                    r = ApplyContrast(r, contrastFactor);
                    g = ApplyContrast(g, contrastFactor);
                    b = ApplyContrast(b, contrastFactor);
                }

                // 5. Apply highlights/shadows/whites/blacks
                if (Math.Abs(grade.Highlights) > 0.001 || Math.Abs(grade.Shadows) > 0.001 ||
                    Math.Abs(grade.Whites) > 0.001 || Math.Abs(grade.Blacks) > 0.001)
                {
                    ApplyTonalAdjustments(ref r, ref g, ref b, grade);
                }

                // 6. Apply saturation and vibrance
                if (Math.Abs(grade.Saturation) > 0.001 || Math.Abs(grade.Vibrance) > 0.001)
                {
                    ApplySaturationVibrance(ref r, ref g, ref b, (float)grade.Saturation, (float)grade.Vibrance);
                }

                // 7. Apply clarity (midtone contrast enhancement)
                if (Math.Abs(grade.Clarity) > 0.001)
                {
                    ApplyClarity(ref r, ref g, ref b, (float)grade.Clarity);
                }

                // 8. Apply curves (before final clamp)
                if (grade.HasCurveAdjustments)
                {
                    ApplyCurves(ref r, ref g, ref b, grade);
                }

                // Clamp and write back
                pixels[i] = (byte)Math.Clamp(b * 255, 0, 255);
                pixels[i + 1] = (byte)Math.Clamp(g * 255, 0, 255);
                pixels[i + 2] = (byte)Math.Clamp(r * 255, 0, 255);
            }

            // Create intermediate result
            var result = BitmapSource.Create(width, height, source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);

            // 7. Apply LUT if specified
            if (!string.IsNullOrEmpty(grade.LutPath))
            {
                var lutResult = _lutService.LoadLut(grade.LutPath);
                if (lutResult.IsSuccess && lutResult.Value != null)
                {
                    result = _lutService.ApplyLut(result, lutResult.Value, grade.LutIntensity) ?? result;
                }
                else if (lutResult.IsFailure)
                {
                    _logger.LogWarning("Could not apply LUT: {Error}", lutResult.Error);
                }
            }

            result.Freeze();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply color grade");
            return source;
        }
    }

    /// <summary>
    /// Apply only LUT to an image (faster for LUT-only preview)
    /// </summary>
    public BitmapSource? ApplyLutOnly(BitmapSource source, string lutPath, double intensity = 1.0)
    {
        if (source == null || string.IsNullOrEmpty(lutPath))
            return source;

        var lutResult = _lutService.LoadLut(lutPath);
        if (lutResult.IsFailure || lutResult.Value == null)
        {
            _logger.LogWarning("Could not load LUT for preview: {Error}", lutResult.Error);
            return source;
        }

        return _lutService.ApplyLut(source, lutResult.Value, intensity);
    }

    private static bool HasColorAdjustments(ColorGrade grade)
    {
        return Math.Abs(grade.Exposure) > 0.001 ||
               Math.Abs(grade.Contrast) > 0.001 ||
               Math.Abs(grade.Saturation) > 0.001 ||
               Math.Abs(grade.Temperature) > 0.001 ||
               Math.Abs(grade.Tint) > 0.001 ||
               Math.Abs(grade.Highlights) > 0.001 ||
               Math.Abs(grade.Shadows) > 0.001 ||
               Math.Abs(grade.Whites) > 0.001 ||
               Math.Abs(grade.Blacks) > 0.001 ||
               Math.Abs(grade.Vibrance) > 0.001 ||
               Math.Abs(grade.Clarity) > 0.001;
    }

    private static bool HasLiftGammaGain(ColorGrade grade)
    {
        return Math.Abs(grade.LiftX) > 0.001 || Math.Abs(grade.LiftY) > 0.001 || Math.Abs(grade.LiftMaster) > 0.001 ||
               Math.Abs(grade.GammaX) > 0.001 || Math.Abs(grade.GammaY) > 0.001 || Math.Abs(grade.GammaMaster) > 0.001 ||
               Math.Abs(grade.GainX) > 0.001 || Math.Abs(grade.GainY) > 0.001 || Math.Abs(grade.GainMaster) > 0.001;
    }

    /// <summary>
    /// Apply temperature (blue-yellow) and tint (green-magenta) adjustments
    /// </summary>
    private static void ApplyTemperatureTint(ref float r, ref float g, ref float b, float temperature, float tint)
    {
        // Temperature: negative = cooler (blue), positive = warmer (yellow/orange)
        // Range: -100 to 100
        var tempFactor = temperature / 100f;
        var tintFactor = tint / 100f;

        // Simplified color temperature adjustment
        if (tempFactor > 0)
        {
            // Warmer: add red/yellow, reduce blue
            r += tempFactor * 0.1f;
            g += tempFactor * 0.05f;
            b -= tempFactor * 0.1f;
        }
        else
        {
            // Cooler: add blue, reduce red
            r += tempFactor * 0.1f;
            b -= tempFactor * 0.1f;
        }

        // Tint: negative = green, positive = magenta
        if (tintFactor > 0)
        {
            // Magenta: add red and blue, reduce green
            r += tintFactor * 0.05f;
            g -= tintFactor * 0.1f;
            b += tintFactor * 0.05f;
        }
        else
        {
            // Green: add green, reduce magenta (red + blue)
            r += tintFactor * 0.05f;
            g -= tintFactor * 0.1f;
            b += tintFactor * 0.05f;
        }
    }

    /// <summary>
    /// Apply lift (shadows), gamma (midtones), and gain (highlights) color wheels
    /// </summary>
    private static void ApplyLiftGammaGain(ref float r, ref float g, ref float b, ColorGrade grade)
    {
        // Lift (shadows) - affects dark values more
        // X controls red-cyan axis, Y controls blue-yellow axis
        var liftR = (float)(grade.LiftMaster + grade.LiftX * 0.5);
        var liftG = (float)(grade.LiftMaster - grade.LiftX * 0.25 - grade.LiftY * 0.25);
        var liftB = (float)(grade.LiftMaster + grade.LiftY * 0.5);

        // Gamma (midtones) - affects middle values
        var gammaR = 1f + (float)(grade.GammaMaster + grade.GammaX * 0.5) * 0.5f;
        var gammaG = 1f + (float)(grade.GammaMaster - grade.GammaX * 0.25 - grade.GammaY * 0.25) * 0.5f;
        var gammaB = 1f + (float)(grade.GammaMaster + grade.GammaY * 0.5) * 0.5f;

        // Gain (highlights) - multiplier for bright values
        var gainR = 1f + (float)(grade.GainMaster + grade.GainX * 0.5) * 0.5f;
        var gainG = 1f + (float)(grade.GainMaster - grade.GainX * 0.25 - grade.GainY * 0.25) * 0.5f;
        var gainB = 1f + (float)(grade.GainMaster + grade.GainY * 0.5) * 0.5f;

        // Apply: output = (input * gain + lift) ^ (1/gamma)
        r = ApplyLGG(r, liftR, gammaR, gainR);
        g = ApplyLGG(g, liftG, gammaG, gainG);
        b = ApplyLGG(b, liftB, gammaB, gainB);
    }

    private static float ApplyLGG(float value, float lift, float gamma, float gain)
    {
        // Apply gain (multiply)
        value *= gain;

        // Apply lift (add to shadows)
        value += lift * (1 - value);

        // Apply gamma (power curve)
        if (value > 0 && Math.Abs(gamma - 1) > 0.001)
        {
            value = (float)Math.Pow(value, 1.0 / gamma);
        }

        return value;
    }

    /// <summary>
    /// Apply contrast adjustment
    /// </summary>
    private static float ApplyContrast(float value, float contrastFactor)
    {
        // Contrast around midpoint (0.5)
        return (value - 0.5f) * contrastFactor + 0.5f;
    }

    /// <summary>
    /// Apply highlights, shadows, whites, blacks adjustments
    /// </summary>
    private static void ApplyTonalAdjustments(ref float r, ref float g, ref float b, ColorGrade grade)
    {
        // Calculate luminance for masking
        var lum = r * 0.2126f + g * 0.7152f + b * 0.0722f;

        // Shadows (affects dark values) - range: 0-0.3
        if (Math.Abs(grade.Shadows) > 0.001)
        {
            var shadowMask = 1f - SmoothStep(0f, 0.3f, lum);
            var shadowAdj = (float)(grade.Shadows / 100.0) * shadowMask;
            r += shadowAdj;
            g += shadowAdj;
            b += shadowAdj;
        }

        // Highlights (affects bright values) - range: 0.7-1.0
        if (Math.Abs(grade.Highlights) > 0.001)
        {
            var highlightMask = SmoothStep(0.5f, 1f, lum);
            var highlightAdj = (float)(grade.Highlights / 100.0) * highlightMask;
            r += highlightAdj;
            g += highlightAdj;
            b += highlightAdj;
        }

        // Blacks (lift black point)
        if (Math.Abs(grade.Blacks) > 0.001)
        {
            var blackAdj = (float)(grade.Blacks / 100.0) * 0.1f;
            r = Math.Max(r, blackAdj);
            g = Math.Max(g, blackAdj);
            b = Math.Max(b, blackAdj);
        }

        // Whites (compress white point)
        if (Math.Abs(grade.Whites) > 0.001)
        {
            var whiteAdj = 1f + (float)(grade.Whites / 100.0) * 0.2f;
            r *= whiteAdj;
            g *= whiteAdj;
            b *= whiteAdj;
        }
    }

    /// <summary>
    /// Apply saturation and vibrance adjustments
    /// </summary>
    private static void ApplySaturationVibrance(ref float r, ref float g, ref float b, float saturation, float vibrance)
    {
        // Calculate luminance
        var lum = r * 0.2126f + g * 0.7152f + b * 0.0722f;

        // Calculate current saturation (simplified)
        var maxC = Math.Max(r, Math.Max(g, b));
        var minC = Math.Min(r, Math.Min(g, b));
        var currentSat = maxC > 0 ? (maxC - minC) / maxC : 0;

        // Vibrance: affects less saturated colors more
        var vibranceFactor = 1f + vibrance / 100f;
        if (Math.Abs(vibrance) > 0.001)
        {
            // Less saturated colors get more boost
            var vibranceMask = 1f - currentSat;
            vibranceFactor = 1f + (vibrance / 100f) * vibranceMask;
        }

        // Saturation factor
        var satFactor = (saturation + 100f) / 100f * vibranceFactor;

        // Apply saturation (blend between gray and color)
        r = lum + (r - lum) * satFactor;
        g = lum + (g - lum) * satFactor;
        b = lum + (b - lum) * satFactor;
    }

    /// <summary>
    /// Smooth step function for gradual transitions
    /// </summary>
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        x = Math.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
        return x * x * (3 - 2 * x);
    }

    /// <summary>
    /// Apply clarity adjustment (local contrast enhancement in midtones)
    /// Clarity enhances texture and detail without affecting shadows or highlights
    /// </summary>
    private static void ApplyClarity(ref float r, ref float g, ref float b, float clarity)
    {
        // Calculate luminance
        var lum = r * 0.2126f + g * 0.7152f + b * 0.0722f;

        // Clarity affects midtones most (bell curve centered at 0.5)
        // This creates a mask that peaks at mid-gray and falls off at shadows/highlights
        var midtoneMask = 1f - Math.Abs(lum - 0.5f) * 2f;
        midtoneMask = Math.Max(0, midtoneMask);
        midtoneMask = midtoneMask * midtoneMask; // Smooth falloff

        // Clarity is essentially localized contrast enhancement
        // We apply a contrast-like adjustment weighted by the midtone mask
        var clarityFactor = clarity / 100f * midtoneMask;

        // Apply clarity as contrast around the pixel's own luminance
        // This enhances local detail without shifting overall brightness
        var adjustedR = lum + (r - lum) * (1f + clarityFactor * 0.5f);
        var adjustedG = lum + (g - lum) * (1f + clarityFactor * 0.5f);
        var adjustedB = lum + (b - lum) * (1f + clarityFactor * 0.5f);

        // Also add subtle contrast around midpoint for punch
        var contrastAmount = clarityFactor * 0.3f;
        r = adjustedR + (adjustedR - 0.5f) * contrastAmount;
        g = adjustedG + (adjustedG - 0.5f) * contrastAmount;
        b = adjustedB + (adjustedB - 0.5f) * contrastAmount;
    }

    /// <summary>
    /// Apply curve lookup tables to RGB values
    /// </summary>
    private static void ApplyCurves(ref float r, ref float g, ref float b, ColorGrade grade)
    {
        // Convert to 0-255 range for LUT lookup
        int ri = (int)Math.Clamp(r * 255, 0, 255);
        int gi = (int)Math.Clamp(g * 255, 0, 255);
        int bi = (int)Math.Clamp(b * 255, 0, 255);

        // Apply RGB curve (affects all channels equally)
        if (grade.CurveLutRgb != null)
        {
            ri = grade.CurveLutRgb[ri];
            gi = grade.CurveLutRgb[gi];
            bi = grade.CurveLutRgb[bi];
        }

        // Apply individual channel curves
        if (grade.CurveLutRed != null)
        {
            ri = grade.CurveLutRed[ri];
        }
        if (grade.CurveLutGreen != null)
        {
            gi = grade.CurveLutGreen[gi];
        }
        if (grade.CurveLutBlue != null)
        {
            bi = grade.CurveLutBlue[bi];
        }

        // Convert back to 0-1 range
        r = ri / 255f;
        g = gi / 255f;
        b = bi / 255f;
    }

    /// <summary>
    /// Generate a real VapourSynth script for the color grade
    /// </summary>
    public static string GenerateVapourSynthScript(ColorGrade grade, string inputClip = "clip")
    {
        var lines = new List<string>
        {
            "import vapoursynth as vs",
            "core = vs.core",
            "",
            $"# Color grading script generated by VapourSynth Portable",
            $"# Input clip: {inputClip}",
            ""
        };

        // Exposure adjustment
        if (Math.Abs(grade.Exposure) > 0.001)
        {
            var expMult = Math.Pow(2, grade.Exposure);
            lines.Add($"# Exposure: {grade.Exposure:F2} stops");
            lines.Add($"{inputClip} = core.std.Expr({inputClip}, expr=['x {expMult:F4} *'])");
            lines.Add("");
        }

        // Contrast adjustment
        if (Math.Abs(grade.Contrast) > 0.001)
        {
            var contrastFactor = (grade.Contrast + 100) / 100.0;
            lines.Add($"# Contrast: {grade.Contrast:F0}%");
            lines.Add($"{inputClip} = core.std.Expr({inputClip}, expr=['x 0.5 - {contrastFactor:F4} * 0.5 +'])");
            lines.Add("");
        }

        // Saturation (requires YUV conversion)
        if (Math.Abs(grade.Saturation) > 0.001)
        {
            var satFactor = (grade.Saturation + 100) / 100.0;
            lines.Add($"# Saturation: {grade.Saturation:F0}%");
            lines.Add($"# Convert to YUV, adjust saturation, convert back");
            lines.Add($"{inputClip}_yuv = core.resize.Bicubic({inputClip}, format=vs.YUV444P16, matrix_s='709')");
            lines.Add($"{inputClip}_y = core.std.ShufflePlanes({inputClip}_yuv, 0, vs.GRAY)");
            lines.Add($"{inputClip}_u = core.std.ShufflePlanes({inputClip}_yuv, 1, vs.GRAY)");
            lines.Add($"{inputClip}_v = core.std.ShufflePlanes({inputClip}_yuv, 2, vs.GRAY)");
            lines.Add($"{inputClip}_u = core.std.Expr({inputClip}_u, expr=['x 32768 - {satFactor:F4} * 32768 +'])");
            lines.Add($"{inputClip}_v = core.std.Expr({inputClip}_v, expr=['x 32768 - {satFactor:F4} * 32768 +'])");
            lines.Add($"{inputClip} = core.std.ShufflePlanes([{inputClip}_y, {inputClip}_u, {inputClip}_v], [0, 0, 0], vs.YUV)");
            lines.Add($"{inputClip} = core.resize.Bicubic({inputClip}, format=vs.RGB24, matrix_in_s='709')");
            lines.Add("");
        }

        // LUT application
        if (!string.IsNullOrEmpty(grade.LutPath))
        {
            var escapedPath = grade.LutPath.Replace("\\", "\\\\");
            lines.Add($"# LUT: {grade.LutPath}");
            lines.Add($"# Intensity: {grade.LutIntensity:F2}");
            lines.Add($"# Note: Requires vs-lut plugin for .cube LUT support");
            lines.Add($"# {inputClip} = core.lut.Cube({inputClip}, cube=r'{escapedPath}')");
            lines.Add("");
        }

        lines.Add($"{inputClip}.set_output()");

        return string.Join("\n", lines);
    }
}
