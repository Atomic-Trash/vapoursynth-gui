using VapourSynthPortable.Models;
using VapourSynthPortable.Services.Commands;

namespace VapourSynthPortable.Tests.Commands;

public class ColorCommandsTests
{
    #region ColorGradeChangeCommand Tests

    [Fact]
    public void ColorGradeChangeCommand_Undo_RestoresPreviousState()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 0, Contrast = 0 };
        var beforeState = grade.Clone();
        grade.Exposure = 1.5;
        grade.Contrast = 25;
        var command = new ColorGradeChangeCommand(grade, beforeState, "Change exposure and contrast");

        // Act
        command.Undo();

        // Assert
        Assert.Equal(0, grade.Exposure);
        Assert.Equal(0, grade.Contrast);
    }

    [Fact]
    public void ColorGradeChangeCommand_Redo_RestoresChangedState()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 0, Contrast = 0 };
        var beforeState = grade.Clone();
        grade.Exposure = 1.5;
        grade.Contrast = 25;
        var command = new ColorGradeChangeCommand(grade, beforeState, "Change exposure and contrast");
        command.Undo();

        // Act
        command.Redo();

        // Assert
        Assert.Equal(1.5, grade.Exposure);
        Assert.Equal(25, grade.Contrast);
    }

    [Fact]
    public void ColorGradeChangeCommand_Create_CapturesBeforeAndAfterState()
    {
        // Arrange
        var grade = new ColorGrade { Saturation = 0 };

        // Act
        var command = ColorGradeChangeCommand.Create(grade, () => grade.Saturation = 50, "Increase saturation");
        command.Undo();

        // Assert
        Assert.Equal(0, grade.Saturation);
        command.Redo();
        Assert.Equal(50, grade.Saturation);
    }

    [Fact]
    public void ColorGradeChangeCommand_Description_IsSet()
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new ColorGradeChangeCommand(grade, grade.Clone(), "Test description");

        // Assert
        Assert.Equal("Test description", command.Description);
    }

    #endregion

    #region AdjustColorWheelCommand Tests

    [Fact]
    public void AdjustColorWheelCommand_Lift_UndoRestoresOldValues()
    {
        // Arrange
        var grade = new ColorGrade { LiftX = 0.1, LiftY = 0.2, LiftMaster = 0.3 };
        var command = new AdjustColorWheelCommand(grade, ColorWheelType.Lift, 0.5, 0.6, 0.7);
        command.Redo(); // Apply the change

        // Act
        command.Undo();

        // Assert
        Assert.Equal(0.1, grade.LiftX);
        Assert.Equal(0.2, grade.LiftY);
        Assert.Equal(0.3, grade.LiftMaster);
    }

    [Fact]
    public void AdjustColorWheelCommand_Gamma_RedoAppliesNewValues()
    {
        // Arrange
        var grade = new ColorGrade { GammaX = 0, GammaY = 0, GammaMaster = 0 };
        var command = new AdjustColorWheelCommand(grade, ColorWheelType.Gamma, 0.2, 0.3, 0.4);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(0.2, grade.GammaX);
        Assert.Equal(0.3, grade.GammaY);
        Assert.Equal(0.4, grade.GammaMaster);
    }

    [Fact]
    public void AdjustColorWheelCommand_Gain_RoundTrip()
    {
        // Arrange
        var grade = new ColorGrade { GainX = 0.1, GainY = 0.1, GainMaster = 0.1 };
        var command = new AdjustColorWheelCommand(grade, ColorWheelType.Gain, 0.5, 0.5, 0.5);

        // Act & Assert
        command.Redo();
        Assert.Equal(0.5, grade.GainX);
        Assert.Equal(0.5, grade.GainY);
        Assert.Equal(0.5, grade.GainMaster);

        command.Undo();
        Assert.Equal(0.1, grade.GainX);
        Assert.Equal(0.1, grade.GainY);
        Assert.Equal(0.1, grade.GainMaster);
    }

    [Fact]
    public void AdjustColorWheelCommand_Description_IncludesWheelType()
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new AdjustColorWheelCommand(grade, ColorWheelType.Gamma, 0, 0, 0);

        // Assert
        Assert.Contains("Gamma", command.Description);
    }

    #endregion

    #region ApplyLutCommand Tests

    [Fact]
    public void ApplyLutCommand_Undo_RestoresOriginalLut()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = "original.cube", LutIntensity = 0.5 };
        var command = new ApplyLutCommand(grade, "new.cube", 1.0);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal("original.cube", grade.LutPath);
        Assert.Equal(0.5, grade.LutIntensity);
    }

    [Fact]
    public void ApplyLutCommand_Redo_AppliesNewLut()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = "", LutIntensity = 1.0 };
        var command = new ApplyLutCommand(grade, "cinematic.cube", 0.75);

        // Act
        command.Redo();

        // Assert
        Assert.Equal("cinematic.cube", grade.LutPath);
        Assert.Equal(0.75, grade.LutIntensity);
    }

    [Fact]
    public void ApplyLutCommand_WithNullPath_SetsEmptyPath()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = "existing.cube" };
        var command = new ApplyLutCommand(grade, null);

        // Act
        command.Redo();

        // Assert
        Assert.Equal("", grade.LutPath);
    }

    [Fact]
    public void ApplyLutCommand_Description_IncludesLutName()
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new ApplyLutCommand(grade, @"C:\LUTs\FilmLook.cube");

        // Assert
        Assert.Contains("FilmLook", command.Description);
    }

    #endregion

    #region ClearLutCommand Tests

    [Fact]
    public void ClearLutCommand_Redo_ClearsLut()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = "some.cube", LutIntensity = 0.8 };
        var command = new ClearLutCommand(grade);

        // Act
        command.Redo();

        // Assert
        Assert.Equal("", grade.LutPath);
        Assert.Equal(1.0, grade.LutIntensity);
    }

    [Fact]
    public void ClearLutCommand_Undo_RestoresLut()
    {
        // Arrange
        var grade = new ColorGrade { LutPath = "some.cube", LutIntensity = 0.8 };
        var command = new ClearLutCommand(grade);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal("some.cube", grade.LutPath);
        Assert.Equal(0.8, grade.LutIntensity);
    }

    [Fact]
    public void ClearLutCommand_Description_IsClearLut()
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new ClearLutCommand(grade);

        // Assert
        Assert.Equal("Clear LUT", command.Description);
    }

    #endregion

    #region ApplyPresetCommand Tests

    [Fact]
    public void ApplyPresetCommand_Redo_AppliesPresetValues()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 0, Contrast = 0, Saturation = 0 };
        var preset = new ColorGrade { Exposure = 0.5, Contrast = 20, Saturation = 10 };
        var command = new ApplyPresetCommand(grade, preset, "Warm Look");

        // Act
        command.Redo();

        // Assert
        Assert.Equal(0.5, grade.Exposure);
        Assert.Equal(20, grade.Contrast);
        Assert.Equal(10, grade.Saturation);
    }

    [Fact]
    public void ApplyPresetCommand_Undo_RestoresOriginalValues()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.0, Contrast = 50 };
        var preset = new ColorGrade { Exposure = 0, Contrast = 0 };
        var command = new ApplyPresetCommand(grade, preset, "Reset Look");
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(1.0, grade.Exposure);
        Assert.Equal(50, grade.Contrast);
    }

    [Fact]
    public void ApplyPresetCommand_Description_IncludesPresetName()
    {
        // Arrange
        var grade = new ColorGrade();
        var preset = new ColorGrade();
        var command = new ApplyPresetCommand(grade, preset, "Cinematic Teal");

        // Assert
        Assert.Contains("Cinematic Teal", command.Description);
    }

    #endregion

    #region AdjustPropertyCommand Tests

    [Fact]
    public void AdjustPropertyCommand_Undo_RestoresOldValue()
    {
        // Arrange
        var grade = new ColorGrade { Exposure = 1.0 };
        var command = new AdjustPropertyCommand(grade, "Exposure", 1.0, 2.0);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(1.0, grade.Exposure);
    }

    [Fact]
    public void AdjustPropertyCommand_Redo_AppliesNewValue()
    {
        // Arrange
        var grade = new ColorGrade { Contrast = 0 };
        var command = new AdjustPropertyCommand(grade, "Contrast", 0, 50);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(50, grade.Contrast);
    }

    [Fact]
    public void AdjustPropertyCommand_Description_IncludesPropertyName()
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new AdjustPropertyCommand(grade, "Vibrance", 0, 25);

        // Assert
        Assert.Contains("Vibrance", command.Description);
    }

    [Theory]
    [InlineData("Temperature", 50)]
    [InlineData("Tint", -20)]
    [InlineData("Highlights", 30)]
    [InlineData("Shadows", -15)]
    public void AdjustPropertyCommand_WorksWithVariousProperties(string propertyName, double newValue)
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new AdjustPropertyCommand(grade, propertyName, 0, newValue);

        // Act
        command.Redo();

        // Assert
        var property = typeof(ColorGrade).GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(newValue, property.GetValue(grade));
    }

    #endregion

    #region ResetColorGradeCommand Tests

    [Fact]
    public void ResetColorGradeCommand_ResetAll_ClearsAllValues()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.0,
            Contrast = 50,
            LiftX = 0.2,
            GammaY = 0.3,
            GainMaster = 0.4,
            LutPath = "test.cube"
        };
        var command = new ResetColorGradeCommand(grade, ResetType.All);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(0, grade.Exposure);
        Assert.Equal(0, grade.Contrast);
        Assert.Equal(0, grade.LiftX);
        Assert.Equal(0, grade.GammaY);
        Assert.Equal(0, grade.GainMaster);
        Assert.Equal("", grade.LutPath);
    }

    [Fact]
    public void ResetColorGradeCommand_ResetWheels_OnlyClearsWheels()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.0,
            LiftX = 0.5,
            GammaX = 0.5,
            GainX = 0.5
        };
        var command = new ResetColorGradeCommand(grade, ResetType.Wheels);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(1.0, grade.Exposure); // Preserved
        Assert.Equal(0, grade.LiftX);
        Assert.Equal(0, grade.GammaX);
        Assert.Equal(0, grade.GainX);
    }

    [Fact]
    public void ResetColorGradeCommand_ResetAdjustments_OnlyClearsAdjustments()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.0,
            Contrast = 50,
            LiftX = 0.5
        };
        var command = new ResetColorGradeCommand(grade, ResetType.Adjustments);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(0, grade.Exposure);
        Assert.Equal(0, grade.Contrast);
        Assert.Equal(0.5, grade.LiftX); // Preserved
    }

    [Fact]
    public void ResetColorGradeCommand_Undo_RestoresAllValues()
    {
        // Arrange
        var grade = new ColorGrade
        {
            Exposure = 1.5,
            Contrast = 25,
            Saturation = 10
        };
        var command = new ResetColorGradeCommand(grade, ResetType.All);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(1.5, grade.Exposure);
        Assert.Equal(25, grade.Contrast);
        Assert.Equal(10, grade.Saturation);
    }

    [Theory]
    [InlineData(ResetType.All, "Reset all adjustments")]
    [InlineData(ResetType.Wheels, "Reset color wheels")]
    [InlineData(ResetType.Adjustments, "Reset adjustments")]
    [InlineData(ResetType.Curves, "Reset curves")]
    public void ResetColorGradeCommand_Description_MatchesResetType(ResetType resetType, string expectedDescription)
    {
        // Arrange
        var grade = new ColorGrade();
        var command = new ResetColorGradeCommand(grade, resetType);

        // Assert
        Assert.Equal(expectedDescription, command.Description);
    }

    #endregion
}
