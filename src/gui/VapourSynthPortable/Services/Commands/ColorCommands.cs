using System.IO;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services.Commands;

/// <summary>
/// Command to change a color grade property
/// </summary>
public class ColorGradeChangeCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly ColorGrade _beforeState;
    private readonly ColorGrade _afterState;

    public string Description { get; }

    public ColorGradeChangeCommand(ColorGrade grade, ColorGrade beforeState, string description)
    {
        _grade = grade;
        _beforeState = beforeState.Clone();
        _afterState = grade.Clone();
        Description = description;
    }

    /// <summary>
    /// Creates a command by capturing the before state, executing an action, and capturing after state
    /// </summary>
    public static ColorGradeChangeCommand Create(ColorGrade grade, Action action, string description)
    {
        var beforeState = grade.Clone();
        action();
        return new ColorGradeChangeCommand(grade, beforeState, description);
    }

    public void Undo()
    {
        RestoreState(_beforeState);
    }

    public void Redo()
    {
        RestoreState(_afterState);
    }

    private void RestoreState(ColorGrade state)
    {
        _grade.Exposure = state.Exposure;
        _grade.Contrast = state.Contrast;
        _grade.Saturation = state.Saturation;
        _grade.Temperature = state.Temperature;
        _grade.Tint = state.Tint;
        _grade.Highlights = state.Highlights;
        _grade.Shadows = state.Shadows;
        _grade.Whites = state.Whites;
        _grade.Blacks = state.Blacks;
        _grade.Vibrance = state.Vibrance;
        _grade.Clarity = state.Clarity;
        _grade.LiftX = state.LiftX;
        _grade.LiftY = state.LiftY;
        _grade.LiftMaster = state.LiftMaster;
        _grade.GammaX = state.GammaX;
        _grade.GammaY = state.GammaY;
        _grade.GammaMaster = state.GammaMaster;
        _grade.GainX = state.GainX;
        _grade.GainY = state.GainY;
        _grade.GainMaster = state.GainMaster;
        _grade.LutPath = state.LutPath;
        _grade.LutIntensity = state.LutIntensity;
    }
}

/// <summary>
/// Command to adjust a color wheel (Lift, Gamma, or Gain)
/// </summary>
public class AdjustColorWheelCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly ColorWheelType _wheelType;
    private readonly double _oldX;
    private readonly double _oldY;
    private readonly double _oldMaster;
    private readonly double _newX;
    private readonly double _newY;
    private readonly double _newMaster;

    public string Description { get; }

    public AdjustColorWheelCommand(
        ColorGrade grade,
        ColorWheelType wheelType,
        double newX,
        double newY,
        double newMaster)
    {
        _grade = grade;
        _wheelType = wheelType;

        // Capture current state
        (_oldX, _oldY, _oldMaster) = GetCurrentValues();

        _newX = newX;
        _newY = newY;
        _newMaster = newMaster;

        Description = $"Adjust {wheelType} wheel";
    }

    private (double x, double y, double master) GetCurrentValues()
    {
        return _wheelType switch
        {
            ColorWheelType.Lift => (_grade.LiftX, _grade.LiftY, _grade.LiftMaster),
            ColorWheelType.Gamma => (_grade.GammaX, _grade.GammaY, _grade.GammaMaster),
            ColorWheelType.Gain => (_grade.GainX, _grade.GainY, _grade.GainMaster),
            _ => (0, 0, 0)
        };
    }

    private void SetValues(double x, double y, double master)
    {
        switch (_wheelType)
        {
            case ColorWheelType.Lift:
                _grade.LiftX = x;
                _grade.LiftY = y;
                _grade.LiftMaster = master;
                break;
            case ColorWheelType.Gamma:
                _grade.GammaX = x;
                _grade.GammaY = y;
                _grade.GammaMaster = master;
                break;
            case ColorWheelType.Gain:
                _grade.GainX = x;
                _grade.GainY = y;
                _grade.GainMaster = master;
                break;
        }
    }

    public void Undo()
    {
        SetValues(_oldX, _oldY, _oldMaster);
    }

    public void Redo()
    {
        SetValues(_newX, _newY, _newMaster);
    }
}

/// <summary>
/// Type of color wheel
/// </summary>
public enum ColorWheelType
{
    Lift,
    Gamma,
    Gain
}

/// <summary>
/// Command to apply a LUT
/// </summary>
public class ApplyLutCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly string _oldLutPath;
    private readonly double _oldLutIntensity;
    private readonly string _newLutPath;
    private readonly double _newLutIntensity;

    public string Description { get; }

    public ApplyLutCommand(ColorGrade grade, string? lutPath, double intensity = 1.0)
    {
        _grade = grade;
        _oldLutPath = grade.LutPath;
        _oldLutIntensity = grade.LutIntensity;
        _newLutPath = lutPath ?? "";
        _newLutIntensity = intensity;

        var lutName = string.IsNullOrEmpty(lutPath) ? "None" : Path.GetFileNameWithoutExtension(lutPath);
        Description = $"Apply LUT: {lutName}";
    }

    public void Undo()
    {
        _grade.LutPath = _oldLutPath;
        _grade.LutIntensity = _oldLutIntensity;
    }

    public void Redo()
    {
        _grade.LutPath = _newLutPath;
        _grade.LutIntensity = _newLutIntensity;
    }
}

/// <summary>
/// Command to clear a LUT
/// </summary>
public class ClearLutCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly string _oldLutPath;
    private readonly double _oldLutIntensity;

    public string Description => "Clear LUT";

    public ClearLutCommand(ColorGrade grade)
    {
        _grade = grade;
        _oldLutPath = grade.LutPath;
        _oldLutIntensity = grade.LutIntensity;
    }

    public void Undo()
    {
        _grade.LutPath = _oldLutPath;
        _grade.LutIntensity = _oldLutIntensity;
    }

    public void Redo()
    {
        _grade.LutPath = "";
        _grade.LutIntensity = 1.0;
    }
}

/// <summary>
/// Command to apply a color grade preset
/// </summary>
public class ApplyPresetCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly ColorGrade _beforeState;
    private readonly ColorGrade _presetValues;
    private readonly string _presetName;

    public string Description { get; }

    public ApplyPresetCommand(ColorGrade grade, ColorGrade preset, string presetName)
    {
        _grade = grade;
        _beforeState = grade.Clone();
        _presetValues = preset.Clone();
        _presetName = presetName;
        Description = $"Apply preset: {presetName}";
    }

    public void Undo()
    {
        RestoreFrom(_beforeState);
    }

    public void Redo()
    {
        RestoreFrom(_presetValues);
    }

    private void RestoreFrom(ColorGrade source)
    {
        _grade.Exposure = source.Exposure;
        _grade.Contrast = source.Contrast;
        _grade.Saturation = source.Saturation;
        _grade.Temperature = source.Temperature;
        _grade.Tint = source.Tint;
        _grade.Highlights = source.Highlights;
        _grade.Shadows = source.Shadows;
        _grade.Whites = source.Whites;
        _grade.Blacks = source.Blacks;
        _grade.Vibrance = source.Vibrance;
        _grade.Clarity = source.Clarity;
        _grade.LiftX = source.LiftX;
        _grade.LiftY = source.LiftY;
        _grade.LiftMaster = source.LiftMaster;
        _grade.GammaX = source.GammaX;
        _grade.GammaY = source.GammaY;
        _grade.GammaMaster = source.GammaMaster;
        _grade.GainX = source.GainX;
        _grade.GainY = source.GainY;
        _grade.GainMaster = source.GainMaster;
        // Note: LUT is not applied from presets by default
    }
}

/// <summary>
/// Command to adjust a single color grade property
/// </summary>
public class AdjustPropertyCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly string _propertyName;
    private readonly double _oldValue;
    private readonly double _newValue;

    public string Description { get; }

    public AdjustPropertyCommand(ColorGrade grade, string propertyName, double oldValue, double newValue)
    {
        _grade = grade;
        _propertyName = propertyName;
        _oldValue = oldValue;
        _newValue = newValue;
        Description = $"Adjust {propertyName}";
    }

    public void Undo()
    {
        SetProperty(_oldValue);
    }

    public void Redo()
    {
        SetProperty(_newValue);
    }

    private void SetProperty(double value)
    {
        var property = typeof(ColorGrade).GetProperty(_propertyName);
        property?.SetValue(_grade, value);
    }
}

/// <summary>
/// Command to reset color grade to defaults
/// </summary>
public class ResetColorGradeCommand : IUndoAction
{
    private readonly ColorGrade _grade;
    private readonly ColorGrade _beforeState;
    private readonly ResetType _resetType;

    public string Description { get; }

    public ResetColorGradeCommand(ColorGrade grade, ResetType resetType)
    {
        _grade = grade;
        _beforeState = grade.Clone();
        _resetType = resetType;
        Description = resetType switch
        {
            ResetType.All => "Reset all adjustments",
            ResetType.Wheels => "Reset color wheels",
            ResetType.Adjustments => "Reset adjustments",
            ResetType.Curves => "Reset curves",
            _ => "Reset"
        };
    }

    public void Undo()
    {
        RestoreFrom(_beforeState);
    }

    public void Redo()
    {
        switch (_resetType)
        {
            case ResetType.All:
                ResetAll();
                break;
            case ResetType.Wheels:
                ResetWheels();
                break;
            case ResetType.Adjustments:
                ResetAdjustments();
                break;
            case ResetType.Curves:
                ResetCurves();
                break;
        }
    }

    private void RestoreFrom(ColorGrade source)
    {
        _grade.Exposure = source.Exposure;
        _grade.Contrast = source.Contrast;
        _grade.Saturation = source.Saturation;
        _grade.Temperature = source.Temperature;
        _grade.Tint = source.Tint;
        _grade.Highlights = source.Highlights;
        _grade.Shadows = source.Shadows;
        _grade.Whites = source.Whites;
        _grade.Blacks = source.Blacks;
        _grade.Vibrance = source.Vibrance;
        _grade.Clarity = source.Clarity;
        _grade.LiftX = source.LiftX;
        _grade.LiftY = source.LiftY;
        _grade.LiftMaster = source.LiftMaster;
        _grade.GammaX = source.GammaX;
        _grade.GammaY = source.GammaY;
        _grade.GammaMaster = source.GammaMaster;
        _grade.GainX = source.GainX;
        _grade.GainY = source.GainY;
        _grade.GainMaster = source.GainMaster;
        _grade.LutPath = source.LutPath;
        _grade.LutIntensity = source.LutIntensity;
    }

    private void ResetAll()
    {
        ResetWheels();
        ResetAdjustments();
        _grade.LutPath = "";
        _grade.LutIntensity = 1.0;
    }

    private void ResetWheels()
    {
        _grade.LiftX = 0;
        _grade.LiftY = 0;
        _grade.LiftMaster = 0;
        _grade.GammaX = 0;
        _grade.GammaY = 0;
        _grade.GammaMaster = 0;
        _grade.GainX = 0;
        _grade.GainY = 0;
        _grade.GainMaster = 0;
    }

    private void ResetAdjustments()
    {
        _grade.Exposure = 0;
        _grade.Contrast = 0;
        _grade.Saturation = 0;
        _grade.Temperature = 0;
        _grade.Tint = 0;
        _grade.Highlights = 0;
        _grade.Shadows = 0;
        _grade.Whites = 0;
        _grade.Blacks = 0;
        _grade.Vibrance = 0;
        _grade.Clarity = 0;
    }

    private void ResetCurves()
    {
        // Curves are stored separately, would need reference to curves data
        // This is a placeholder for curves reset functionality
    }
}

/// <summary>
/// Type of reset operation
/// </summary>
public enum ResetType
{
    All,
    Wheels,
    Adjustments,
    Curves
}
