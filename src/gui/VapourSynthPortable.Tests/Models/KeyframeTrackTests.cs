namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for KeyframeTrack model.
/// Tests cover: construction, keyframe management, interpolation, clone.
/// </summary>
public class KeyframeTrackTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var track = new KeyframeTrack();

        // Assert
        track.Id.Should().BeGreaterThan(0);
        track.ParameterName.Should().BeEmpty();
        track.DisplayName.Should().BeEmpty();
        track.Keyframes.Should().NotBeNull();
        track.Keyframes.Should().BeEmpty();
        track.IsExpanded.Should().BeTrue();
        track.IsEnabled.Should().BeTrue();
        track.Parameter.Should().BeNull();
        track.Effect.Should().BeNull();
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        // Act
        var track1 = new KeyframeTrack();
        var track2 = new KeyframeTrack();

        // Assert
        track1.Id.Should().NotBe(track2.Id);
    }

    [Fact]
    public void Constructor_GeneratesIncrementingIds()
    {
        // Act
        var track1 = new KeyframeTrack();
        var track2 = new KeyframeTrack();

        // Assert
        track2.Id.Should().BeGreaterThan(track1.Id);
    }

    #endregion

    #region HasKeyframes Property Tests

    [Fact]
    public void HasKeyframes_ReturnsFalse_WhenEmpty()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Assert
        track.HasKeyframes.Should().BeFalse();
    }

    [Fact]
    public void HasKeyframes_ReturnsTrue_WhenHasKeyframes()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 1.0);

        // Assert
        track.HasKeyframes.Should().BeTrue();
    }

    #endregion

    #region AddKeyframe Tests

    [Fact]
    public void AddKeyframe_AddsKeyframeToCollection()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.AddKeyframe(0, 1.0);

        // Assert
        track.Keyframes.Should().HaveCount(1);
    }

    [Fact]
    public void AddKeyframe_ReturnsCreatedKeyframe()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        var keyframe = track.AddKeyframe(100, 0.5);

        // Assert
        keyframe.Should().NotBeNull();
        keyframe.Frame.Should().Be(100);
        keyframe.Value.Should().Be(0.5);
    }

    [Fact]
    public void AddKeyframe_WithInterpolation_SetsInterpolationType()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        var keyframe = track.AddKeyframe(0, 1.0, KeyframeInterpolation.EaseIn);

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.EaseIn);
    }

    [Fact]
    public void AddKeyframe_DefaultsToLinear()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        var keyframe = track.AddKeyframe(0, 1.0);

        // Assert
        keyframe.Interpolation.Should().Be(KeyframeInterpolation.Linear);
    }

    [Fact]
    public void AddKeyframe_MaintainsSortedOrder()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act - Add out of order
        track.AddKeyframe(100, 1.0);
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(50, 0.5);

        // Assert - Should be sorted
        track.Keyframes[0].Frame.Should().Be(0);
        track.Keyframes[1].Frame.Should().Be(50);
        track.Keyframes[2].Frame.Should().Be(100);
    }

    [Fact]
    public void AddKeyframe_AtExistingFrame_UpdatesValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Act
        track.AddKeyframe(100, 2.0);

        // Assert - Should update, not add
        track.Keyframes.Should().HaveCount(1);
        track.Keyframes[0].Value.Should().Be(2.0);
    }

    [Fact]
    public void AddKeyframe_AtExistingFrame_UpdatesInterpolation()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0, KeyframeInterpolation.Linear);

        // Act
        track.AddKeyframe(100, 1.0, KeyframeInterpolation.EaseInOut);

        // Assert
        track.Keyframes[0].Interpolation.Should().Be(KeyframeInterpolation.EaseInOut);
    }

    [Fact]
    public void AddKeyframe_Multiple_AllAdded()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(50, 0.5);
        track.AddKeyframe(100, 1.0);

        // Assert
        track.Keyframes.Should().HaveCount(3);
    }

    #endregion

    #region RemoveKeyframe Tests

    [Fact]
    public void RemoveKeyframe_RemovesFromCollection()
    {
        // Arrange
        var track = new KeyframeTrack();
        var keyframe = track.AddKeyframe(0, 1.0);

        // Act
        track.RemoveKeyframe(keyframe);

        // Assert
        track.Keyframes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveKeyframeAt_RemovesKeyframeAtFrame()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Act
        track.RemoveKeyframeAt(100);

        // Assert
        track.Keyframes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveKeyframeAt_NoKeyframeAtFrame_DoesNothing()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Act
        track.RemoveKeyframeAt(50); // No keyframe at frame 50

        // Assert
        track.Keyframes.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveKeyframe_UpdatesHasKeyframes()
    {
        // Arrange
        var track = new KeyframeTrack();
        var keyframe = track.AddKeyframe(0, 1.0);
        track.HasKeyframes.Should().BeTrue();

        // Act
        track.RemoveKeyframe(keyframe);

        // Assert
        track.HasKeyframes.Should().BeFalse();
    }

    #endregion

    #region HasKeyframeAt Tests

    [Fact]
    public void HasKeyframeAt_ReturnsTrue_WhenKeyframeExists()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Assert
        track.HasKeyframeAt(100).Should().BeTrue();
    }

    [Fact]
    public void HasKeyframeAt_ReturnsFalse_WhenNoKeyframe()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Assert
        track.HasKeyframeAt(50).Should().BeFalse();
    }

    [Fact]
    public void HasKeyframeAt_ReturnsFalse_WhenEmpty()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Assert
        track.HasKeyframeAt(0).Should().BeFalse();
    }

    #endregion

    #region GetKeyframeAtOrBefore Tests

    [Fact]
    public void GetKeyframeAtOrBefore_ReturnsExactMatch()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetKeyframeAtOrBefore(100);

        // Assert
        result.Should().NotBeNull();
        result!.Frame.Should().Be(100);
    }

    [Fact]
    public void GetKeyframeAtOrBefore_ReturnsNearestBefore()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetKeyframeAtOrBefore(50);

        // Assert
        result.Should().NotBeNull();
        result!.Frame.Should().Be(0);
    }

    [Fact]
    public void GetKeyframeAtOrBefore_ReturnsNull_WhenBeforeFirstKeyframe()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetKeyframeAtOrBefore(50);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetKeyframeAfter Tests

    [Fact]
    public void GetKeyframeAfter_ReturnsNextKeyframe()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetKeyframeAfter(50);

        // Assert
        result.Should().NotBeNull();
        result!.Frame.Should().Be(100);
    }

    [Fact]
    public void GetKeyframeAfter_ReturnsNull_WhenAfterLastKeyframe()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetKeyframeAfter(100);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetKeyframeAfter_DoesNotReturnCurrentFrame()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 1.0);
        track.AddKeyframe(200, 2.0);

        // Act - At frame 100
        var result = track.GetKeyframeAfter(100);

        // Assert - Should return frame 200, not 100
        result.Should().NotBeNull();
        result!.Frame.Should().Be(200);
    }

    #endregion

    #region GetValueAtFrame Interpolation Tests

    [Fact]
    public void GetValueAtFrame_NoKeyframes_ReturnsParameterValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.Parameter = new EffectParameter { Value = 50.0 };

        // Act
        var result = track.GetValueAtFrame(100);

        // Assert
        result.Should().Be(50.0);
    }

    [Fact]
    public void GetValueAtFrame_SingleKeyframe_ReturnsKeyframeValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 1.0);

        // Act
        var result = track.GetValueAtFrame(100);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void GetValueAtFrame_BeforeFirstKeyframe_ReturnsFirstValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);
        track.AddKeyframe(200, 2.0);

        // Act
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void GetValueAtFrame_AfterLastKeyframe_ReturnsLastValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 1.0);

        // Act
        var result = track.GetValueAtFrame(200);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void GetValueAtFrame_LinearInterpolation_MidPoint()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act - Midpoint
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().Be(50.0);
    }

    [Fact]
    public void GetValueAtFrame_LinearInterpolation_QuarterPoint()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act
        var result = track.GetValueAtFrame(25);

        // Assert
        result.Should().Be(25.0);
    }

    [Fact]
    public void GetValueAtFrame_HoldInterpolation_ReturnsFirstValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.Hold);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act - Hold should return the first value until the next keyframe
        var result = track.GetValueAtFrame(50);

        // Assert - With Hold, should return the FROM value (0.0) not interpolated
        result.Should().Be(0.0);
    }

    [Fact]
    public void GetValueAtFrame_EaseIn_SlowerStart()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.EaseIn);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act - At midpoint with EaseIn
        var result = track.GetValueAtFrame(50);

        // Assert - EaseIn (t*t) at t=0.5 gives 0.25, so value should be 25
        result.Should().Be(25.0);
    }

    [Fact]
    public void GetValueAtFrame_EaseOut_FasterStart()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.EaseOut);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act - At midpoint with EaseOut
        var result = track.GetValueAtFrame(50);

        // Assert - EaseOut at t=0.5 gives 0.75, so value should be 75
        result.Should().Be(75.0);
    }

    [Fact]
    public void GetValueAtFrame_EaseInOut_SlowStartAndEnd()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0, KeyframeInterpolation.EaseInOut);
        track.AddKeyframe(100, 100.0, KeyframeInterpolation.Linear);

        // Act - At midpoint with EaseInOut
        var result = track.GetValueAtFrame(50);

        // Assert - EaseInOut at t=0.5 gives 0.5 (inflection point)
        result.Should().Be(50.0);
    }

    [Fact]
    public void GetValueAtFrame_IntegerInterpolation()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100, KeyframeInterpolation.Linear);

        // Act
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().Be(50);
    }

    [Fact]
    public void GetValueAtFrame_AtExactKeyframe_ReturnsExactValue()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0);
        track.AddKeyframe(100, 100.0);

        // Act
        var result = track.GetValueAtFrame(100);

        // Assert
        result.Should().Be(100.0);
    }

    [Fact]
    public void GetValueAtFrame_NullValues_ReturnsNearestNonNull()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, null);
        track.AddKeyframe(100, 100.0);

        // Act - Before midpoint (t < 0.5)
        var resultBeforeMid = track.GetValueAtFrame(40);
        // At midpoint (t = 0.5) - returns toValue
        var resultAtMid = track.GetValueAtFrame(50);

        // Assert - With null from, returns null when t < 0.5, else returns toValue
        resultBeforeMid.Should().BeNull();
        resultAtMid.Should().Be(100.0); // At t=0.5, returns toValue
    }

    [Fact]
    public void GetValueAtFrame_FloatInterpolation()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0.0f, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100.0f, KeyframeInterpolation.Linear);

        // Act
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().BeOfType<float>();
        ((float)result!).Should().BeApproximately(50.0f, 0.001f);
    }

    [Fact]
    public void GetValueAtFrame_LongInterpolation()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0L, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100L, KeyframeInterpolation.Linear);

        // Act
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().Be(50L);
    }

    [Fact]
    public void GetValueAtFrame_DecimalInterpolation()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, 0m, KeyframeInterpolation.Linear);
        track.AddKeyframe(100, 100m, KeyframeInterpolation.Linear);

        // Act
        var result = track.GetValueAtFrame(50);

        // Assert
        result.Should().Be(50m);
    }

    [Fact]
    public void GetValueAtFrame_NonNumericTypes_StepsAtMidpoint()
    {
        // Arrange
        var track = new KeyframeTrack();
        track.AddKeyframe(0, "start", KeyframeInterpolation.Linear);
        track.AddKeyframe(100, "end", KeyframeInterpolation.Linear);

        // Act - Before midpoint
        var resultBefore = track.GetValueAtFrame(40);
        // After midpoint
        var resultAfter = track.GetValueAtFrame(60);

        // Assert - Non-numeric steps at 0.5
        resultBefore.Should().Be("start");
        resultAfter.Should().Be("end");
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        // Arrange
        var original = new KeyframeTrack { ParameterName = "Test" };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_GeneratesNewId()
    {
        // Arrange
        var original = new KeyframeTrack();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        // Arrange
        var original = new KeyframeTrack
        {
            ParameterName = "Strength",
            DisplayName = "Effect Strength",
            IsExpanded = false,
            IsEnabled = false
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.ParameterName.Should().Be("Strength");
        clone.DisplayName.Should().Be("Effect Strength");
        clone.IsExpanded.Should().BeFalse();
        clone.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Clone_CopiesKeyframes()
    {
        // Arrange
        var original = new KeyframeTrack();
        original.AddKeyframe(0, 0.0);
        original.AddKeyframe(100, 1.0);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Keyframes.Should().HaveCount(2);
    }

    [Fact]
    public void Clone_DeepCopiesKeyframes()
    {
        // Arrange
        var original = new KeyframeTrack();
        original.AddKeyframe(0, 0.0, KeyframeInterpolation.EaseIn);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Keyframes.Should().NotBeSameAs(original.Keyframes);
        clone.Keyframes[0].Should().NotBeSameAs(original.Keyframes[0]);
        clone.Keyframes[0].Frame.Should().Be(0);
        clone.Keyframes[0].Value.Should().Be(0.0);
        clone.Keyframes[0].Interpolation.Should().Be(KeyframeInterpolation.EaseIn);
    }

    [Fact]
    public void Clone_DoesNotCopyParameterReference()
    {
        // Arrange
        var param = new EffectParameter { Name = "Test" };
        var original = new KeyframeTrack { Parameter = param };

        // Act
        var clone = original.Clone();

        // Assert - Parameter reference is not cloned
        clone.Parameter.Should().BeNull();
    }

    [Fact]
    public void Clone_DoesNotCopyEffectReference()
    {
        // Arrange
        var effect = new TimelineEffect { Name = "Test" };
        var original = new KeyframeTrack { Effect = effect };

        // Act
        var clone = original.Clone();

        // Assert - Effect reference is not cloned
        clone.Effect.Should().BeNull();
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new KeyframeTrack { ParameterName = "Original" };
        original.AddKeyframe(0, 0.0);
        var clone = original.Clone();

        // Act
        clone.ParameterName = "Clone";
        clone.AddKeyframe(100, 1.0);

        // Assert
        original.ParameterName.Should().Be("Original");
        original.Keyframes.Should().HaveCount(1);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_RaisedForParameterName()
    {
        // Arrange
        var track = new KeyframeTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(KeyframeTrack.ParameterName))
                propertyChanged = true;
        };

        // Act
        track.ParameterName = "Strength";

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsEnabled()
    {
        // Arrange
        var track = new KeyframeTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(KeyframeTrack.IsEnabled))
                propertyChanged = true;
        };

        // Act
        track.IsEnabled = false;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void AddKeyframe_RaisesHasKeyframesChanged()
    {
        // Arrange
        var track = new KeyframeTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(KeyframeTrack.HasKeyframes))
                propertyChanged = true;
        };

        // Act
        track.AddKeyframe(0, 1.0);

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void RemoveKeyframe_RaisesHasKeyframesChanged()
    {
        // Arrange
        var track = new KeyframeTrack();
        var keyframe = track.AddKeyframe(0, 1.0);
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(KeyframeTrack.HasKeyframes))
                propertyChanged = true;
        };

        // Act
        track.RemoveKeyframe(keyframe);

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddKeyframe_AtFrameZero_Works()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.AddKeyframe(0, 1.0);

        // Assert
        track.Keyframes.Should().HaveCount(1);
        track.Keyframes[0].Frame.Should().Be(0);
    }

    [Fact]
    public void AddKeyframe_AtLargeFrame_Works()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.AddKeyframe(1000000, 1.0);

        // Assert
        track.Keyframes.Should().HaveCount(1);
        track.Keyframes[0].Frame.Should().Be(1000000);
    }

    [Fact]
    public void GetValueAtFrame_ZeroFrameRange_ReturnsFromValue()
    {
        // Arrange - Two keyframes at same frame (shouldn't happen, but test anyway)
        var track = new KeyframeTrack();
        track.AddKeyframe(100, 1.0);
        track.AddKeyframe(100, 2.0); // Updates existing

        // Act
        var result = track.GetValueAtFrame(100);

        // Assert
        result.Should().Be(2.0);
    }

    [Fact]
    public void IsExpanded_DefaultsToTrue()
    {
        // Arrange & Act
        var track = new KeyframeTrack();

        // Assert
        track.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        // Arrange & Act
        var track = new KeyframeTrack();

        // Assert
        track.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisplayName_CanBeSet()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.DisplayName = "Effect Strength";

        // Assert
        track.DisplayName.Should().Be("Effect Strength");
    }

    [Fact]
    public void ParameterName_CanBeSet()
    {
        // Arrange
        var track = new KeyframeTrack();

        // Act
        track.ParameterName = "strength";

        // Assert
        track.ParameterName.Should().Be("strength");
    }

    #endregion
}
