using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

/// <summary>
/// Represents a complete color grading configuration
/// </summary>
public partial class ColorGrade : ObservableObject
{
    // Lift (Shadows)
    [ObservableProperty] private double _liftX;
    [ObservableProperty] private double _liftY;
    [ObservableProperty] private double _liftMaster;

    // Gamma (Midtones)
    [ObservableProperty] private double _gammaX;
    [ObservableProperty] private double _gammaY;
    [ObservableProperty] private double _gammaMaster;

    // Gain (Highlights)
    [ObservableProperty] private double _gainX;
    [ObservableProperty] private double _gainY;
    [ObservableProperty] private double _gainMaster;

    // Global adjustments
    [ObservableProperty] private double _exposure;
    [ObservableProperty] private double _contrast;
    [ObservableProperty] private double _saturation;
    [ObservableProperty] private double _temperature;
    [ObservableProperty] private double _tint;
    [ObservableProperty] private double _highlights;
    [ObservableProperty] private double _shadows;
    [ObservableProperty] private double _whites;
    [ObservableProperty] private double _blacks;
    [ObservableProperty] private double _vibrance;
    [ObservableProperty] private double _clarity;

    // Curves (simplified - just store control points)
    [ObservableProperty] private double _curveShadows = 0;
    [ObservableProperty] private double _curveMidtones = 0;
    [ObservableProperty] private double _curveHighlights = 0;

    // LUT
    [ObservableProperty] private string _lutPath = "";
    [ObservableProperty] private double _lutIntensity = 1.0;

    public ColorGrade Clone()
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
            CurveShadows = CurveShadows,
            CurveMidtones = CurveMidtones,
            CurveHighlights = CurveHighlights,
            LutPath = LutPath,
            LutIntensity = LutIntensity
        };
    }

    public void Reset()
    {
        LiftX = LiftY = LiftMaster = 0;
        GammaX = GammaY = GammaMaster = 0;
        GainX = GainY = GainMaster = 0;
        Exposure = Contrast = Saturation = 0;
        Temperature = Tint = 0;
        Highlights = Shadows = Whites = Blacks = 0;
        Vibrance = Clarity = 0;
        CurveShadows = CurveMidtones = CurveHighlights = 0;
        LutPath = "";
        LutIntensity = 1.0;
    }

    /// <summary>
    /// Generates VapourSynth script for color grading
    /// </summary>
    public string ToVapourSynthScript(string clipName = "clip")
    {
        var lines = new List<string>();

        // Apply exposure/contrast first
        if (Math.Abs(Exposure) > 0.001 || Math.Abs(Contrast) > 0.001)
        {
            var expMult = Math.Pow(2, Exposure);
            var contrastFactor = (Contrast + 100) / 100.0;
            lines.Add($"{clipName} = core.std.Expr({clipName}, ['x {expMult:F4} * 0.5 - {contrastFactor:F4} * 0.5 +'])");
        }

        // Apply saturation
        if (Math.Abs(Saturation) > 0.001)
        {
            var satFactor = (Saturation + 100) / 100.0;
            lines.Add($"# Saturation adjustment: {satFactor:F2}");
        }

        // Apply lift/gamma/gain (simplified - would need proper color matrix in real implementation)
        if (HasLiftGammaGain())
        {
            lines.Add($"# Lift: ({LiftX:F3}, {LiftY:F3}, {LiftMaster:F3})");
            lines.Add($"# Gamma: ({GammaX:F3}, {GammaY:F3}, {GammaMaster:F3})");
            lines.Add($"# Gain: ({GainX:F3}, {GainY:F3}, {GainMaster:F3})");
        }

        // Apply LUT if specified
        if (!string.IsNullOrEmpty(LutPath))
        {
            lines.Add($"# LUT: {LutPath} @ {LutIntensity:F2}");
        }

        return string.Join("\n", lines);
    }

    private bool HasLiftGammaGain()
    {
        return Math.Abs(LiftX) > 0.001 || Math.Abs(LiftY) > 0.001 || Math.Abs(LiftMaster) > 0.001 ||
               Math.Abs(GammaX) > 0.001 || Math.Abs(GammaY) > 0.001 || Math.Abs(GammaMaster) > 0.001 ||
               Math.Abs(GainX) > 0.001 || Math.Abs(GainY) > 0.001 || Math.Abs(GainMaster) > 0.001;
    }
}

/// <summary>
/// Preset color grades
/// </summary>
public class ColorGradePreset
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public ColorGrade Grade { get; set; } = new();

    public static List<ColorGradePreset> GetPresets()
    {
        return
        [
            // Cinematic
            new() { Name = "Cinematic Teal & Orange", Category = "Cinematic", Grade = new ColorGrade
            {
                Temperature = -15, Tint = 5, Contrast = 10, Saturation = 10,
                LiftX = 0.1, LiftY = 0.15, GainX = 0.1, GainY = -0.05
            }},
            new() { Name = "Cinematic Cold", Category = "Cinematic", Grade = new ColorGrade
            {
                Temperature = -25, Contrast = 15, Saturation = -10,
                LiftX = 0.05, LiftY = 0.1, Shadows = -10
            }},
            new() { Name = "Cinematic Warm", Category = "Cinematic", Grade = new ColorGrade
            {
                Temperature = 20, Contrast = 10, Saturation = 5,
                GainX = 0.05, GainY = -0.05, Highlights = 5
            }},

            // Vintage
            new() { Name = "Vintage Film", Category = "Vintage", Grade = new ColorGrade
            {
                Contrast = -5, Saturation = -20, Temperature = 10,
                LiftMaster = 0.05, Blacks = 10, Highlights = -10
            }},
            new() { Name = "Faded", Category = "Vintage", Grade = new ColorGrade
            {
                Contrast = -15, Saturation = -15,
                LiftMaster = 0.1, Blacks = 15
            }},

            // B&W
            new() { Name = "Black & White", Category = "B&W", Grade = new ColorGrade
            {
                Saturation = -100, Contrast = 15
            }},
            new() { Name = "High Contrast B&W", Category = "B&W", Grade = new ColorGrade
            {
                Saturation = -100, Contrast = 40, Blacks = -15, Whites = 15
            }},

            // Creative
            new() { Name = "Cross Process", Category = "Creative", Grade = new ColorGrade
            {
                LiftX = 0.1, LiftY = 0.15,
                GammaX = -0.05, GammaY = 0.1,
                GainX = 0.1, GainY = -0.1,
                Contrast = 10, Saturation = 15
            }},
            new() { Name = "Bleach Bypass", Category = "Creative", Grade = new ColorGrade
            {
                Saturation = -40, Contrast = 30, Highlights = -10, Shadows = -10
            }},

            // Correction
            new() { Name = "Neutral", Category = "Correction", Grade = new ColorGrade() },
            new() { Name = "Daylight Balance", Category = "Correction", Grade = new ColorGrade
            {
                Temperature = 0, Tint = 0
            }},
            new() { Name = "Tungsten Balance", Category = "Correction", Grade = new ColorGrade
            {
                Temperature = -30, Tint = 5
            }},
        ];
    }
}
