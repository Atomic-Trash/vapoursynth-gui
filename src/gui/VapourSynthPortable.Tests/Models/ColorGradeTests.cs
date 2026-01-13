namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for ColorGrade model.
/// Tests cover: construction, lift/gamma/gain, global adjustments, curves, LUT, clone, reset, VapourSynth script generation.
/// </summary>
public class ColorGradeTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var grade = new ColorGrade();

        // Assert - All values should default to 0
        grade.LiftX.Should().Be(0);
        grade.LiftY.Should().Be(0);
        grade.LiftMaster.Should().Be(0);
        grade.GammaX.Should().Be(0);
        grade.GammaY.Should().Be(0);
        grade.GammaMaster.Should().Be(0);
        grade.GainX.Should().Be(0);
        grade.GainY.Should().Be(0);
        grade.GainMaster.Should().Be(0);
        grade.Exposure.Should().Be(0);
        grade.Contrast.Should().Be(0);
        grade.Saturation.Should().Be(0);
        grade.Temperature.Should().Be(0);
        grade.Tint.Should().Be(0);
        grade.Highlights.Should().Be(0);
        grade.Shadows.Should().Be(0);
        grade.Whites.Should().Be(0);
        grade.Blacks.Should().Be(0);
        grade.Vibrance.Should().Be(0);
        grade.Clarity.Should().Be(0);
    }

    [Fact]
    public void Constructor_SetsDefaultCurves()
    {
        // Act
        var grade = new ColorGrade();

        // Assert - Default curves are straight lines from (0,0) to (1,1)
        grade.CurvePointsRgb.Should().HaveCount(2);
        grade.CurvePointsRgb[0].X.Should().Be(0);
        grade.CurvePointsRgb[0].Y.Should().Be(0);
        grade.CurvePointsRgb[1].X.Should().Be(1);
        grade.CurvePointsRgb[1].Y.Should().Be(1);
    }

    [Fact]
    public void Constructor_SetsDefaultLutValues()
    {
        // Act
        var grade = new ColorGrade();

        // Assert
        grade.LutPath.Should().BeEmpty();
        grade.LutIntensity.Should().Be(1.0);
    }

    [Fact]
    public void Constructor_CurveLutsAreNull()
    {
        // Act
        var grade = new ColorGrade();

        // Assert
        grade.CurveLutRgb.Should().BeNull();
        grade.CurveLutRed.Should().BeNull();
        grade.CurveLutGreen.Should().BeNull();
        grade.CurveLutBlue.Should().BeNull();
    }

    #endregion

    #region Lift/Gamma/Gain Tests (Color Wheels)

    [Fact]
    public void LiftX_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LiftX = 0.5;

        // Assert
        grade.LiftX.Should().Be(0.5);
    }

    [Fact]
    public void LiftY_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LiftY = -0.3;

        // Assert
        grade.LiftY.Should().Be(-0.3);
    }

    [Fact]
    public void LiftMaster_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LiftMaster = 0.1;

        // Assert
        grade.LiftMaster.Should().Be(0.1);
    }

    [Fact]
    public void GammaX_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GammaX = 0.2;

        // Assert
        grade.GammaX.Should().Be(0.2);
    }

    [Fact]
    public void GammaY_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GammaY = -0.15;

        // Assert
        grade.GammaY.Should().Be(-0.15);
    }

    [Fact]
    public void GammaMaster_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GammaMaster = 0.25;

        // Assert
        grade.GammaMaster.Should().Be(0.25);
    }

    [Fact]
    public void GainX_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GainX = 0.4;

        // Assert
        grade.GainX.Should().Be(0.4);
    }

    [Fact]
    public void GainY_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GainY = -0.2;

        // Assert
        grade.GainY.Should().Be(-0.2);
    }

    [Fact]
    public void GainMaster_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.GainMaster = 0.15;

        // Assert
        grade.GainMaster.Should().Be(0.15);
    }

    #endregion

    #region Global Adjustment Tests

    [Fact]
    public void Exposure_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Exposure = 1.5;

        // Assert
        grade.Exposure.Should().Be(1.5);
    }

    [Fact]
    public void Exposure_CanBeNegative()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Exposure = -2.0;

        // Assert
        grade.Exposure.Should().Be(-2.0);
    }

    [Fact]
    public void Contrast_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Contrast = 25.0;

        // Assert
        grade.Contrast.Should().Be(25.0);
    }

    [Fact]
    public void Saturation_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Saturation = 50.0;

        // Assert
        grade.Saturation.Should().Be(50.0);
    }

    [Fact]
    public void Saturation_CanBeNegative_ForDesaturation()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Saturation = -100.0; // Full desaturation = black & white

        // Assert
        grade.Saturation.Should().Be(-100.0);
    }

    [Fact]
    public void Temperature_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Temperature = 20.0; // Warm

        // Assert
        grade.Temperature.Should().Be(20.0);
    }

    [Fact]
    public void Temperature_CanBeNegative_ForCool()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Temperature = -30.0; // Cool

        // Assert
        grade.Temperature.Should().Be(-30.0);
    }

    [Fact]
    public void Tint_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Tint = 10.0;

        // Assert
        grade.Tint.Should().Be(10.0);
    }

    [Fact]
    public void Highlights_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Highlights = 15.0;

        // Assert
        grade.Highlights.Should().Be(15.0);
    }

    [Fact]
    public void Shadows_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Shadows = -10.0;

        // Assert
        grade.Shadows.Should().Be(-10.0);
    }

    [Fact]
    public void Whites_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Whites = 20.0;

        // Assert
        grade.Whites.Should().Be(20.0);
    }

    [Fact]
    public void Blacks_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Blacks = 5.0;

        // Assert
        grade.Blacks.Should().Be(5.0);
    }

    [Fact]
    public void Vibrance_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Vibrance = 30.0;

        // Assert
        grade.Vibrance.Should().Be(30.0);
    }

    [Fact]
    public void Clarity_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.Clarity = 25.0;

        // Assert
        grade.Clarity.Should().Be(25.0);
    }

    #endregion

    #region Curve Tests

    [Fact]
    public void CurvePointsRgb_DefaultHasTwoPoints()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.CurvePointsRgb.Should().HaveCount(2);
    }

    [Fact]
    public void CurvePointsRed_DefaultHasTwoPoints()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.CurvePointsRed.Should().HaveCount(2);
    }

    [Fact]
    public void CurvePointsGreen_DefaultHasTwoPoints()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.CurvePointsGreen.Should().HaveCount(2);
    }

    [Fact]
    public void CurvePointsBlue_DefaultHasTwoPoints()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.CurvePointsBlue.Should().HaveCount(2);
    }

    [Fact]
    public void CurvePoints_CanBeModified()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act - Add S-curve midpoint
        grade.CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(0.25, 0.15), new CurvePoint(0.75, 0.85), new CurvePoint(1, 1)];

        // Assert
        grade.CurvePointsRgb.Should().HaveCount(4);
        grade.CurvePointsRgb[1].X.Should().Be(0.25);
        grade.CurvePointsRgb[1].Y.Should().Be(0.15);
    }

    [Fact]
    public void HasCurveAdjustments_ReturnsFalse_WhenDefault()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.HasCurveAdjustments.Should().BeFalse();
    }

    [Fact]
    public void HasCurveAdjustments_ReturnsTrue_WhenCurveModified()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act - Add a control point
        grade.CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(0.5, 0.6), new CurvePoint(1, 1)];

        // Assert
        grade.HasCurveAdjustments.Should().BeTrue();
    }

    [Fact]
    public void HasCurveAdjustments_ReturnsTrue_WhenRedCurveModified()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.CurvePointsRed = [new CurvePoint(0, 0), new CurvePoint(0.5, 0.7), new CurvePoint(1, 1)];

        // Assert
        grade.HasCurveAdjustments.Should().BeTrue();
    }

    [Fact]
    public void HasCurveAdjustments_ReturnsTrue_WhenCurveLutSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.CurveLutRgb = new byte[256];

        // Assert
        grade.HasCurveAdjustments.Should().BeTrue();
    }

    #endregion

    #region LUT Tests

    [Fact]
    public void LutPath_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LutPath = @"C:\LUTs\MyLut.cube";

        // Assert
        grade.LutPath.Should().Be(@"C:\LUTs\MyLut.cube");
    }

    [Fact]
    public void LutIntensity_DefaultsToOne()
    {
        // Arrange
        var grade = new ColorGrade();

        // Assert
        grade.LutIntensity.Should().Be(1.0);
    }

    [Fact]
    public void LutIntensity_CanBeSet()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LutIntensity = 0.5;

        // Assert
        grade.LutIntensity.Should().Be(0.5);
    }

    [Fact]
    public void LutIntensity_CanBeZero()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        grade.LutIntensity = 0.0;

        // Assert
        grade.LutIntensity.Should().Be(0.0);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        // Arrange
        var original = new ColorGrade { Exposure = 1.0 };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_CopiesLiftGammaGain()
    {
        // Arrange
        var original = new ColorGrade
        {
            LiftX = 0.1, LiftY = 0.2, LiftMaster = 0.3,
            GammaX = 0.4, GammaY = 0.5, GammaMaster = 0.6,
            GainX = 0.7, GainY = 0.8, GainMaster = 0.9
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.LiftX.Should().Be(0.1);
        clone.LiftY.Should().Be(0.2);
        clone.LiftMaster.Should().Be(0.3);
        clone.GammaX.Should().Be(0.4);
        clone.GammaY.Should().Be(0.5);
        clone.GammaMaster.Should().Be(0.6);
        clone.GainX.Should().Be(0.7);
        clone.GainY.Should().Be(0.8);
        clone.GainMaster.Should().Be(0.9);
    }

    [Fact]
    public void Clone_CopiesGlobalAdjustments()
    {
        // Arrange
        var original = new ColorGrade
        {
            Exposure = 1.5,
            Contrast = 20,
            Saturation = 30,
            Temperature = -15,
            Tint = 5,
            Highlights = 10,
            Shadows = -10,
            Whites = 15,
            Blacks = 5,
            Vibrance = 25,
            Clarity = 20
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Exposure.Should().Be(1.5);
        clone.Contrast.Should().Be(20);
        clone.Saturation.Should().Be(30);
        clone.Temperature.Should().Be(-15);
        clone.Tint.Should().Be(5);
        clone.Highlights.Should().Be(10);
        clone.Shadows.Should().Be(-10);
        clone.Whites.Should().Be(15);
        clone.Blacks.Should().Be(5);
        clone.Vibrance.Should().Be(25);
        clone.Clarity.Should().Be(20);
    }

    [Fact]
    public void Clone_CopiesCurvePoints()
    {
        // Arrange
        var original = new ColorGrade
        {
            CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(0.5, 0.6), new CurvePoint(1, 1)]
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.CurvePointsRgb.Should().HaveCount(3);
        clone.CurvePointsRgb[1].X.Should().Be(0.5);
        clone.CurvePointsRgb[1].Y.Should().Be(0.6);
    }

    [Fact]
    public void Clone_DeepCopiesCurvePoints()
    {
        // Arrange
        var original = new ColorGrade
        {
            CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(1, 1)]
        };

        // Act
        var clone = original.Clone();

        // Assert - Modifying clone should not affect original
        clone.CurvePointsRgb[0].X = 0.1;
        original.CurvePointsRgb[0].X.Should().Be(0);
    }

    [Fact]
    public void Clone_CopiesCurveLuts()
    {
        // Arrange
        var original = new ColorGrade();
        original.CurveLutRgb = new byte[] { 0, 1, 2, 3 };

        // Act
        var clone = original.Clone();

        // Assert
        clone.CurveLutRgb.Should().NotBeNull();
        clone.CurveLutRgb.Should().HaveCount(4);
        clone.CurveLutRgb![0].Should().Be(0);
    }

    [Fact]
    public void Clone_DeepCopiesCurveLuts()
    {
        // Arrange
        var original = new ColorGrade();
        original.CurveLutRgb = new byte[] { 0, 1, 2, 3 };

        // Act
        var clone = original.Clone();
        clone.CurveLutRgb![0] = 100;

        // Assert - Original should not be affected
        original.CurveLutRgb[0].Should().Be(0);
    }

    [Fact]
    public void Clone_CopiesLutPath()
    {
        // Arrange
        var original = new ColorGrade { LutPath = @"C:\LUTs\test.cube" };

        // Act
        var clone = original.Clone();

        // Assert
        clone.LutPath.Should().Be(@"C:\LUTs\test.cube");
    }

    [Fact]
    public void Clone_CopiesLutIntensity()
    {
        // Arrange
        var original = new ColorGrade { LutIntensity = 0.75 };

        // Act
        var clone = original.Clone();

        // Assert
        clone.LutIntensity.Should().Be(0.75);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new ColorGrade { Exposure = 1.0 };
        var clone = original.Clone();

        // Act
        clone.Exposure = 2.0;

        // Assert
        original.Exposure.Should().Be(1.0);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_SetsLiftToZero()
    {
        // Arrange
        var grade = new ColorGrade { LiftX = 0.5, LiftY = 0.5, LiftMaster = 0.5 };

        // Act
        grade.Reset();

        // Assert
        grade.LiftX.Should().Be(0);
        grade.LiftY.Should().Be(0);
        grade.LiftMaster.Should().Be(0);
    }

    [Fact]
    public void Reset_SetsGammaToZero()
    {
        // Arrange
        var grade = new ColorGrade { GammaX = 0.5, GammaY = 0.5, GammaMaster = 0.5 };

        // Act
        grade.Reset();

        // Assert
        grade.GammaX.Should().Be(0);
        grade.GammaY.Should().Be(0);
        grade.GammaMaster.Should().Be(0);
    }

    [Fact]
    public void Reset_SetsGainToZero()
    {
        // Arrange
        var grade = new ColorGrade { GainX = 0.5, GainY = 0.5, GainMaster = 0.5 };

        // Act
        grade.Reset();

        // Assert
        grade.GainX.Should().Be(0);
        grade.GainY.Should().Be(0);
        grade.GainMaster.Should().Be(0);
    }

    [Fact]
    public void Reset_SetsGlobalAdjustmentsToZero()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.5,
            Contrast = 20,
            Saturation = 30,
            Temperature = -15,
            Tint = 5,
            Highlights = 10,
            Shadows = -10,
            Whites = 15,
            Blacks = 5,
            Vibrance = 25,
            Clarity = 20
        };

        // Act
        grade.Reset();

        // Assert
        grade.Exposure.Should().Be(0);
        grade.Contrast.Should().Be(0);
        grade.Saturation.Should().Be(0);
        grade.Temperature.Should().Be(0);
        grade.Tint.Should().Be(0);
        grade.Highlights.Should().Be(0);
        grade.Shadows.Should().Be(0);
        grade.Whites.Should().Be(0);
        grade.Blacks.Should().Be(0);
        grade.Vibrance.Should().Be(0);
        grade.Clarity.Should().Be(0);
    }

    [Fact]
    public void Reset_ResetsCurvesToDefault()
    {
        // Arrange
        var grade = new ColorGrade
        {
            CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(0.5, 0.6), new CurvePoint(1, 1)]
        };

        // Act
        grade.Reset();

        // Assert
        grade.CurvePointsRgb.Should().HaveCount(2);
        grade.CurvePointsRgb[0].X.Should().Be(0);
        grade.CurvePointsRgb[0].Y.Should().Be(0);
        grade.CurvePointsRgb[1].X.Should().Be(1);
        grade.CurvePointsRgb[1].Y.Should().Be(1);
    }

    [Fact]
    public void Reset_ClearsCurveLuts()
    {
        // Arrange
        var grade = new ColorGrade
        {
            CurveLutRgb = new byte[256],
            CurveLutRed = new byte[256],
            CurveLutGreen = new byte[256],
            CurveLutBlue = new byte[256]
        };

        // Act
        grade.Reset();

        // Assert
        grade.CurveLutRgb.Should().BeNull();
        grade.CurveLutRed.Should().BeNull();
        grade.CurveLutGreen.Should().BeNull();
        grade.CurveLutBlue.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsLutPath()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = @"C:\LUTs\test.cube" };

        // Act
        grade.Reset();

        // Assert
        grade.LutPath.Should().BeEmpty();
    }

    [Fact]
    public void Reset_SetsLutIntensityToOne()
    {
        // Arrange
        var grade = new ColorGrade { LutIntensity = 0.5 };

        // Act
        grade.Reset();

        // Assert
        grade.LutIntensity.Should().Be(1.0);
    }

    [Fact]
    public void ResetCurves_OnlyResetsCurves()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.5,
            CurvePointsRgb = [new CurvePoint(0, 0), new CurvePoint(0.5, 0.6), new CurvePoint(1, 1)],
            CurveLutRgb = new byte[256]
        };

        // Act
        grade.ResetCurves();

        // Assert - Exposure should remain
        grade.Exposure.Should().Be(1.5);
        // Curves should be reset
        grade.CurvePointsRgb.Should().HaveCount(2);
        grade.CurveLutRgb.Should().BeNull();
    }

    #endregion

    #region VapourSynth Script Generation Tests

    [Fact]
    public void ToVapourSynthScript_DefaultGrade_ReturnsEmptyLines()
    {
        // Arrange
        var grade = new ColorGrade();

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().BeEmpty();
    }

    [Fact]
    public void ToVapourSynthScript_WithExposure_GeneratesExpression()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.0 };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().Contain("clip = core.std.Expr");
    }

    [Fact]
    public void ToVapourSynthScript_WithContrast_GeneratesExpression()
    {
        // Arrange
        var grade = new ColorGrade { Contrast = 20 };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().Contain("core.std.Expr");
    }

    [Fact]
    public void ToVapourSynthScript_WithSaturation_GeneratesComment()
    {
        // Arrange
        var grade = new ColorGrade { Saturation = 50 };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().Contain("Saturation adjustment");
    }

    [Fact]
    public void ToVapourSynthScript_WithLiftGammaGain_GeneratesComments()
    {
        // Arrange
        var grade = new ColorGrade
        {
            LiftX = 0.1, LiftY = 0.2, LiftMaster = 0.05,
            GammaX = 0.1, GammaY = 0.1, GammaMaster = 0.0,
            GainX = 0.05, GainY = 0.0, GainMaster = 0.0
        };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().Contain("Lift:");
        script.Should().Contain("Gamma:");
        script.Should().Contain("Gain:");
    }

    [Fact]
    public void ToVapourSynthScript_WithLut_GeneratesLutComment()
    {
        // Arrange
        var grade = new ColorGrade
        {
            LutPath = @"C:\LUTs\cinematic.cube",
            LutIntensity = 0.75
        };

        // Act
        var script = grade.ToVapourSynthScript();

        // Assert
        script.Should().Contain("LUT:");
        script.Should().Contain("cinematic.cube");
        script.Should().Contain("0.75");
    }

    [Fact]
    public void ToVapourSynthScript_CustomClipName_UsesProvidedName()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.0 };

        // Act
        var script = grade.ToVapourSynthScript("myClip");

        // Assert
        script.Should().Contain("myClip = core.std.Expr");
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_RaisedForExposure()
    {
        // Arrange
        var grade = new ColorGrade();
        var propertyChanged = false;
        grade.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ColorGrade.Exposure))
                propertyChanged = true;
        };

        // Act
        grade.Exposure = 1.5;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForSaturation()
    {
        // Arrange
        var grade = new ColorGrade();
        var propertyChanged = false;
        grade.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ColorGrade.Saturation))
                propertyChanged = true;
        };

        // Act
        grade.Saturation = 50;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForLiftX()
    {
        // Arrange
        var grade = new ColorGrade();
        var propertyChanged = false;
        grade.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ColorGrade.LiftX))
                propertyChanged = true;
        };

        // Act
        grade.LiftX = 0.1;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion

    #region ColorGradePreset Tests

    [Fact]
    public void ColorGradePreset_GetPresets_ReturnsPresets()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.Should().NotBeEmpty();
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsCinematicCategory()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.Should().Contain(p => p.Category == "Cinematic");
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsVintageCategory()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.Should().Contain(p => p.Category == "Vintage");
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsBWCategory()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.Should().Contain(p => p.Category == "B&W");
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsBlackAndWhitePreset()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        var bwPreset = presets.FirstOrDefault(p => p.Name == "Black & White");
        bwPreset.Should().NotBeNull();
        bwPreset!.Grade.Saturation.Should().Be(-100);
    }

    [Fact]
    public void ColorGradePreset_GetPresets_ContainsNeutralPreset()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        var neutralPreset = presets.FirstOrDefault(p => p.Name == "Neutral");
        neutralPreset.Should().NotBeNull();
        neutralPreset!.Grade.Exposure.Should().Be(0);
        neutralPreset.Grade.Saturation.Should().Be(0);
    }

    [Fact]
    public void ColorGradePreset_TealAndOrange_HasCorrectSettings()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();
        var preset = presets.FirstOrDefault(p => p.Name == "Cinematic Teal & Orange");

        // Assert
        preset.Should().NotBeNull();
        preset!.Grade.Temperature.Should().Be(-15);
        preset.Grade.LiftX.Should().Be(0.1);
    }

    [Fact]
    public void ColorGradePreset_AllPresetsHaveNames()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.All(p => !string.IsNullOrEmpty(p.Name)).Should().BeTrue();
    }

    [Fact]
    public void ColorGradePreset_AllPresetsHaveCategories()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.All(p => !string.IsNullOrEmpty(p.Category)).Should().BeTrue();
    }

    [Fact]
    public void ColorGradePreset_AllPresetsHaveGrades()
    {
        // Act
        var presets = ColorGradePreset.GetPresets();

        // Assert
        presets.All(p => p.Grade != null).Should().BeTrue();
    }

    #endregion

    #region CurvePoint Tests

    [Fact]
    public void CurvePoint_DefaultConstructor_SetsZeroValues()
    {
        // Act
        var point = new CurvePoint();

        // Assert
        point.X.Should().Be(0);
        point.Y.Should().Be(0);
    }

    [Fact]
    public void CurvePoint_ParameterizedConstructor_SetsValues()
    {
        // Act
        var point = new CurvePoint(0.5, 0.7);

        // Assert
        point.X.Should().Be(0.5);
        point.Y.Should().Be(0.7);
    }

    [Fact]
    public void CurvePoint_XCanBeModified()
    {
        // Arrange
        var point = new CurvePoint();

        // Act
        point.X = 0.75;

        // Assert
        point.X.Should().Be(0.75);
    }

    [Fact]
    public void CurvePoint_YCanBeModified()
    {
        // Arrange
        var point = new CurvePoint();

        // Act
        point.Y = 0.25;

        // Assert
        point.Y.Should().Be(0.25);
    }

    #endregion
}
