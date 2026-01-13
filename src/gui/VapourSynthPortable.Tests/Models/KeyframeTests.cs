namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for Keyframe model.
/// Tests cover: construction, properties, interpolation, clone, bezier controls.
/// </summary>
public class KeyframeTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.Id.Should().BeGreaterThan(0);
        keyframe.Frame.Should().Be(0);
        keyframe.Value.Should().BeNull();
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.Linear);
        keyframe.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithFrameAndValue_SetsProperties()
    {
        // Act
        var keyframe = new Keyframe(100, 0.5);

        // Assert
        keyframe.Frame.Should().Be(100);
        keyframe.Value.Should().Be(0.5);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        // Act
        var kf1 = new Keyframe();
        var kf2 = new Keyframe();
        var kf3 = new Keyframe();

        // Assert
        kf1.Id.Should().NotBe(kf2.Id);
        kf2.Id.Should().NotBe(kf3.Id);
    }

    [Fact]
    public void Constructor_GeneratesIncrementingIds()
    {
        // Act
        var kf1 = new Keyframe();
        var kf2 = new Keyframe();

        // Assert
        kf2.Id.Should().BeGreaterThan(kf1.Id);
    }

    #endregion

    #region Frame Property Tests

    [Fact]
    public void Frame_CanBeSetToZero()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Frame = 0;

        // Assert
        keyframe.Frame.Should().Be(0);
    }

    [Fact]
    public void Frame_CanBeSetToPositiveValue()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Frame = 1000;

        // Assert
        keyframe.Frame.Should().Be(1000);
    }

    [Fact]
    public void Frame_CanBeSetToLargeValue()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Frame = long.MaxValue;

        // Assert
        keyframe.Frame.Should().Be(long.MaxValue);
    }

    #endregion

    #region Value Property Tests

    [Fact]
    public void Value_CanBeDouble()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Value = 1.5;

        // Assert
        keyframe.Value.Should().Be(1.5);
    }

    [Fact]
    public void Value_CanBeInteger()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Value = 42;

        // Assert
        keyframe.Value.Should().Be(42);
    }

    [Fact]
    public void Value_CanBeString()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Value = "test";

        // Assert
        keyframe.Value.Should().Be("test");
    }

    [Fact]
    public void Value_CanBeNull()
    {
        // Arrange
        var keyframe = new Keyframe(0, 1.0);

        // Act
        keyframe.Value = null;

        // Assert
        keyframe.Value.Should().BeNull();
    }

    [Fact]
    public void Value_CanBeNegative()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Value = -100.5;

        // Assert
        keyframe.Value.Should().Be(-100.5);
    }

    #endregion

    #region Interpolation Property Tests

    [Fact]
    public void Interpolation_DefaultsToLinear()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.Linear);
    }

    [Fact]
    public void Interpolation_CanBeSetToHold()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = KeyframeInterpolation.Hold;

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.Hold);
    }

    [Fact]
    public void Interpolation_CanBeSetToEaseIn()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = KeyframeInterpolation.EaseIn;

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.EaseIn);
    }

    [Fact]
    public void Interpolation_CanBeSetToEaseOut()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = KeyframeInterpolation.EaseOut;

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.EaseOut);
    }

    [Fact]
    public void Interpolation_CanBeSetToEaseInOut()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = KeyframeInterpolation.EaseInOut;

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.EaseInOut);
    }

    [Fact]
    public void Interpolation_CanBeSetToBezier()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = KeyframeInterpolation.Bezier;

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.Bezier);
    }

    [Theory]
    [InlineData(KeyframeInterpolation.Hold)]
    [InlineData(KeyframeInterpolation.Linear)]
    [InlineData(KeyframeInterpolation.EaseIn)]
    [InlineData(KeyframeInterpolation.EaseOut)]
    [InlineData(KeyframeInterpolation.EaseInOut)]
    [InlineData(KeyframeInterpolation.Bezier)]
    public void Interpolation_AllTypesCanBeSet(KeyframeInterpolation type)
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.Interpolation = type;

        // Assert
        keyframe.Interpolation.Should().Be(type);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void IsSelected_DefaultsToFalse()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void IsSelected_CanBeToggled()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.IsSelected = true;

        // Assert
        keyframe.IsSelected.Should().BeTrue();
    }

    #endregion

    #region Bezier Control Points Tests

    [Fact]
    public void EaseInX_DefaultsToQuarter()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.EaseInX.Should().Be(0.25);
    }

    [Fact]
    public void EaseInY_DefaultsToQuarter()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.EaseInY.Should().Be(0.25);
    }

    [Fact]
    public void EaseOutX_DefaultsToThreeQuarters()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.EaseOutX.Should().Be(0.75);
    }

    [Fact]
    public void EaseOutY_DefaultsToThreeQuarters()
    {
        // Arrange & Act
        var keyframe = new Keyframe();

        // Assert
        keyframe.EaseOutY.Should().Be(0.75);
    }

    [Fact]
    public void EaseInX_CanBeModified()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.EaseInX = 0.1;

        // Assert
        keyframe.EaseInX.Should().Be(0.1);
    }

    [Fact]
    public void EaseInY_CanBeModified()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.EaseInY = 0.9;

        // Assert
        keyframe.EaseInY.Should().Be(0.9);
    }

    [Fact]
    public void EaseOutX_CanBeModified()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.EaseOutX = 0.5;

        // Assert
        keyframe.EaseOutX.Should().Be(0.5);
    }

    [Fact]
    public void EaseOutY_CanBeModified()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act
        keyframe.EaseOutY = 0.0;

        // Assert
        keyframe.EaseOutY.Should().Be(0.0);
    }

    [Fact]
    public void BezierControls_CanExceedNormalRange()
    {
        // Arrange - Bezier controls can overshoot for bounce effects
        var keyframe = new Keyframe();

        // Act
        keyframe.EaseInY = -0.5;
        keyframe.EaseOutY = 1.5;

        // Assert
        keyframe.EaseInY.Should().Be(-0.5);
        keyframe.EaseOutY.Should().Be(1.5);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        // Arrange
        var original = new Keyframe(100, 0.5);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_GeneratesNewId()
    {
        // Arrange
        var original = new Keyframe(100, 0.5);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void Clone_CopiesFrame()
    {
        // Arrange
        var original = new Keyframe(500, 0.5);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Frame.Should().Be(500);
    }

    [Fact]
    public void Clone_CopiesValue()
    {
        // Arrange
        var original = new Keyframe(0, 99.9);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Value.Should().Be(99.9);
    }

    [Fact]
    public void Clone_CopiesInterpolation()
    {
        // Arrange
        var original = new Keyframe { Interpolation = KeyframeInterpolation.EaseInOut };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Interpolation.Should().Be(KeyframeInterpolation.EaseInOut);
    }

    [Fact]
    public void Clone_CopiesBezierControls()
    {
        // Arrange
        var original = new Keyframe
        {
            EaseInX = 0.1,
            EaseInY = 0.2,
            EaseOutX = 0.8,
            EaseOutY = 0.9
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.EaseInX.Should().Be(0.1);
        clone.EaseInY.Should().Be(0.2);
        clone.EaseOutX.Should().Be(0.8);
        clone.EaseOutY.Should().Be(0.9);
    }

    [Fact]
    public void Clone_DoesNotCopyIsSelected()
    {
        // Arrange - IsSelected is UI state, shouldn't be cloned
        var original = new Keyframe { IsSelected = true };

        // Act
        var clone = original.Clone();

        // Assert
        clone.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        // Arrange
        var original = new Keyframe
        {
            Frame = 240,
            Value = 75.5,
            Interpolation = KeyframeInterpolation.Bezier,
            EaseInX = 0.15,
            EaseInY = 0.25,
            EaseOutX = 0.85,
            EaseOutY = 0.95
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Frame.Should().Be(original.Frame);
        clone.Value.Should().Be(original.Value);
        clone.Interpolation.Should().Be(original.Interpolation);
        clone.EaseInX.Should().Be(original.EaseInX);
        clone.EaseInY.Should().Be(original.EaseInY);
        clone.EaseOutX.Should().Be(original.EaseOutX);
        clone.EaseOutY.Should().Be(original.EaseOutY);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new Keyframe(100, 50.0);
        var clone = original.Clone();

        // Act
        clone.Frame = 200;
        clone.Value = 100.0;

        // Assert
        original.Frame.Should().Be(100);
        original.Value.Should().Be(50.0);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_RaisedForFrame()
    {
        // Arrange
        var keyframe = new Keyframe();
        var propertyChanged = false;
        keyframe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Keyframe.Frame))
                propertyChanged = true;
        };

        // Act
        keyframe.Frame = 100;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForValue()
    {
        // Arrange
        var keyframe = new Keyframe();
        var propertyChanged = false;
        keyframe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Keyframe.Value))
                propertyChanged = true;
        };

        // Act
        keyframe.Value = 1.0;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForInterpolation()
    {
        // Arrange
        var keyframe = new Keyframe();
        var propertyChanged = false;
        keyframe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Keyframe.Interpolation))
                propertyChanged = true;
        };

        // Act
        keyframe.Interpolation = KeyframeInterpolation.EaseIn;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsSelected()
    {
        // Arrange
        var keyframe = new Keyframe();
        var propertyChanged = false;
        keyframe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Keyframe.IsSelected))
                propertyChanged = true;
        };

        // Act
        keyframe.IsSelected = true;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Value_WithDifferentTypes_MaintainsType()
    {
        // Arrange
        var keyframe = new Keyframe();

        // Act & Assert - Integer
        keyframe.Value = 42;
        keyframe.Value.Should().BeOfType<int>();

        // Act & Assert - Double
        keyframe.Value = 42.0;
        keyframe.Value.Should().BeOfType<double>();

        // Act & Assert - String
        keyframe.Value = "42";
        keyframe.Value.Should().BeOfType<string>();
    }

    [Fact]
    public void Constructor_WithNullValue_AcceptsNull()
    {
        // Act
        var keyframe = new Keyframe(0, null);

        // Assert
        keyframe.Value.Should().BeNull();
    }

    [Fact]
    public void Frame_WithNegativeValue_Allowed()
    {
        // This might be needed for pre-roll or negative timecodes
        var keyframe = new Keyframe();

        // Act
        keyframe.Frame = -100;

        // Assert
        keyframe.Frame.Should().Be(-100);
    }

    #endregion
}
