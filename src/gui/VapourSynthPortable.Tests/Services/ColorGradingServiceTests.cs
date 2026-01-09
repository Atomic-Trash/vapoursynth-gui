using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class ColorGradingServiceTests
{
    [Fact]
    public void GenerateVapourSynthScript_DefaultGrade_ProducesMinimalScript()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade);

        // Assert
        Assert.Contains("import vapoursynth as vs", script);
        Assert.Contains("core = vs.core", script);
        Assert.Contains("clip.set_output()", script);
    }

    [Fact]
    public void GenerateVapourSynthScript_WithExposure_IncludesExposureAdjustment()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.5 };

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade);

        // Assert
        Assert.Contains("Exposure:", script);
        Assert.Contains("core.std.Expr", script);
    }

    [Fact]
    public void GenerateVapourSynthScript_WithContrast_IncludesContrastAdjustment()
    {
        // Arrange
        var grade = new ColorGrade { Contrast = 25 };

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade);

        // Assert
        Assert.Contains("Contrast:", script);
        Assert.Contains("core.std.Expr", script);
    }

    [Fact]
    public void GenerateVapourSynthScript_WithSaturation_IncludesYUVConversion()
    {
        // Arrange
        var grade = new ColorGrade { Saturation = 30 };

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade);

        // Assert
        Assert.Contains("Saturation:", script);
        Assert.Contains("YUV444P16", script);
        Assert.Contains("core.resize.Bicubic", script);
    }

    [Fact]
    public void GenerateVapourSynthScript_WithLut_IncludesLutReference()
    {
        // Arrange
        var grade = new ColorGrade
        {
            LutPath = @"C:\path\to\lut.cube",
            LutIntensity = 0.75
        };

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade);

        // Assert
        Assert.Contains("LUT:", script);
        Assert.Contains("lut.cube", script);
        Assert.Contains("Intensity:", script);
    }

    [Fact]
    public void GenerateVapourSynthScript_CustomClipName_UsesProvidedName()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.0 };

        // Act
        var script = ColorGradingService.GenerateVapourSynthScript(grade, "myClip");

        // Assert
        Assert.Contains("myClip", script);
        Assert.Contains("myClip.set_output()", script);
    }

    [Fact]
    public void ColorGrade_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new ColorGrade
        {
            Exposure = 1.5,
            Contrast = 25,
            Saturation = 10,
            Temperature = -15,
            LutPath = "test.cube"
        };

        // Act
        var clone = original.Clone();
        original.Exposure = 0;

        // Assert
        Assert.Equal(1.5, clone.Exposure);
        Assert.Equal(25, clone.Contrast);
        Assert.Equal(10, clone.Saturation);
        Assert.Equal(-15, clone.Temperature);
        Assert.Equal("test.cube", clone.LutPath);
    }

    [Fact]
    public void ColorGrade_Reset_ClearsAllValues()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.5,
            Contrast = 25,
            Saturation = 10,
            LiftX = 0.1,
            GammaY = 0.2,
            GainMaster = 0.3,
            LutPath = "test.cube",
            LutIntensity = 0.5
        };

        // Act
        grade.Reset();

        // Assert
        Assert.Equal(0, grade.Exposure);
        Assert.Equal(0, grade.Contrast);
        Assert.Equal(0, grade.Saturation);
        Assert.Equal(0, grade.LiftX);
        Assert.Equal(0, grade.GammaY);
        Assert.Equal(0, grade.GainMaster);
        Assert.Equal("", grade.LutPath);
        Assert.Equal(1.0, grade.LutIntensity);
    }

    [Fact]
    public void ColorGrade_ToVapourSynthScript_WithExposureAndContrast()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.0,
            Contrast = 20
        };

        // Act
        var script = grade.ToVapourSynthScript("clip");

        // Assert
        Assert.Contains("core.std.Expr", script);
    }

    [Fact]
    public void ColorGrade_ToVapourSynthScript_WithLiftGammaGain()
    {
        // Arrange
        var grade = new ColorGrade
        {
            LiftX = 0.1,
            GammaY = 0.2,
            GainMaster = 0.15
        };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        Assert.Contains("Lift:", script);
        Assert.Contains("Gamma:", script);
        Assert.Contains("Gain:", script);
    }

    [Fact]
    public void ColorGrade_ToVapourSynthScript_WithLut()
    {
        // Arrange
        var grade = new ColorGrade
        {
            LutPath = "cinematic.cube",
            LutIntensity = 0.8
        };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        Assert.Contains("LUT: cinematic.cube", script);
        Assert.Contains("@ 0.80", script);
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ReturnsNonEmptyList()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        Assert.NotEmpty(presets);
        Assert.True(presets.Count >= 10);
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsExpectedCategories()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();
        var categories = presets.Select(p => p.Category).Distinct().ToList();

        // Assert
        Assert.Contains("Cinematic", categories);
        Assert.Contains("Vintage", categories);
        Assert.Contains("B&W", categories);
        Assert.Contains("Creative", categories);
        Assert.Contains("Correction", categories);
    }

    [Fact]
    public void ColorGradePreset_CinematicTealOrange_HasExpectedValues()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();
        var tealOrange = presets.FirstOrDefault(p => p.Name == "Cinematic Teal & Orange");

        // Assert
        Assert.NotNull(tealOrange);
        Assert.Equal("Cinematic", tealOrange.Category);
        Assert.True(tealOrange.Grade.Temperature < 0); // Cooler temperature for teal
    }

    [Fact]
    public void ColorGradePreset_BlackWhite_HasFullDesaturation()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();
        var bw = presets.FirstOrDefault(p => p.Name == "Black & White");

        // Assert
        Assert.NotNull(bw);
        Assert.Equal(-100, bw.Grade.Saturation);
    }

    [Fact]
    public void ColorGradePreset_Neutral_HasDefaultValues()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();
        var neutral = presets.FirstOrDefault(p => p.Name == "Neutral");

        // Assert
        Assert.NotNull(neutral);
        Assert.Equal(0, neutral.Grade.Exposure);
        Assert.Equal(0, neutral.Grade.Contrast);
        Assert.Equal(0, neutral.Grade.Saturation);
    }
}
