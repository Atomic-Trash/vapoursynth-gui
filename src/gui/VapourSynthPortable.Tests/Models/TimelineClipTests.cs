namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for TimelineClip model - critical for timeline editing functionality.
/// Tests cover: construction, property defaults, computed properties, cloning, effects, and edge cases.
/// </summary>
public class TimelineClipTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var clip = new TimelineClip();

        // Assert
        clip.Id.Should().BeGreaterThan(0);
        clip.Name.Should().BeEmpty();
        clip.SourcePath.Should().BeEmpty();
        clip.StartFrame.Should().Be(0);
        clip.EndFrame.Should().Be(0);
        clip.SourceInFrame.Should().Be(0);
        clip.SourceOutFrame.Should().Be(0);
        clip.SourceDurationFrames.Should().Be(0);
        clip.FrameRate.Should().Be(24.0);
        clip.IsSelected.Should().BeFalse();
        clip.IsMuted.Should().BeFalse();
        clip.IsLocked.Should().BeFalse();
        clip.Volume.Should().Be(1.0);
        clip.TrackType.Should().Be(TrackType.Video);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Arrange & Act
        var clip1 = new TimelineClip();
        var clip2 = new TimelineClip();

        // Assert
        clip1.Id.Should().NotBe(clip2.Id);
    }

    [Fact]
    public void Constructor_GeneratesIncrementingIds()
    {
        // Arrange & Act
        var clip1 = new TimelineClip();
        var clip2 = new TimelineClip();
        var clip3 = new TimelineClip();

        // Assert - IDs should be sequential
        clip2.Id.Should().BeGreaterThan(clip1.Id);
        clip3.Id.Should().BeGreaterThan(clip2.Id);
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void DurationFrames_ReturnsCorrectValue()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 100
        };

        // Act
        var duration = clip.DurationFrames;

        // Assert
        duration.Should().Be(100);
    }

    [Fact]
    public void DurationFrames_HandlesNonZeroStartFrame()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 50,
            EndFrame = 150
        };

        // Act
        var duration = clip.DurationFrames;

        // Assert
        duration.Should().Be(100);
    }

    [Fact]
    public void DurationSeconds_ReturnsCorrectValue_At24Fps()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 48,
            FrameRate = 24.0
        };

        // Act
        var duration = clip.DurationSeconds;

        // Assert
        duration.Should().Be(2.0);
    }

    [Fact]
    public void DurationSeconds_ReturnsCorrectValue_At30Fps()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 60,
            FrameRate = 30.0
        };

        // Act
        var duration = clip.DurationSeconds;

        // Assert
        duration.Should().Be(2.0);
    }

    [Fact]
    public void DurationSeconds_ReturnsCorrectValue_At60Fps()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 120,
            FrameRate = 60.0
        };

        // Act
        var duration = clip.DurationSeconds;

        // Assert
        duration.Should().Be(2.0);
    }

    [Fact]
    public void DurationSeconds_ReturnsCorrectValue_AtNTSCFrameRates()
    {
        // Arrange - 23.976 fps (NTSC film)
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 23976,
            FrameRate = 23.976
        };

        // Act
        var duration = clip.DurationSeconds;

        // Assert
        duration.Should().BeApproximately(1000.0, 0.01);
    }

    [Fact]
    public void IsVideo_ReturnsTrueForVideoTrackType()
    {
        // Arrange
        var clip = new TimelineClip { TrackType = TrackType.Video };

        // Assert
        clip.IsVideo.Should().BeTrue();
        clip.IsAudio.Should().BeFalse();
    }

    [Fact]
    public void IsAudio_ReturnsTrueForAudioTrackType()
    {
        // Arrange
        var clip = new TimelineClip { TrackType = TrackType.Audio };

        // Assert
        clip.IsAudio.Should().BeTrue();
        clip.IsVideo.Should().BeFalse();
    }

    [Fact]
    public void HasEffects_ReturnsFalseWhenEmpty()
    {
        // Arrange
        var clip = new TimelineClip();

        // Assert
        clip.HasEffects.Should().BeFalse();
    }

    [Fact]
    public void HasEffects_ReturnsTrueWhenEnabledEffectsExist()
    {
        // Arrange - TimelineEffect.IsEnabled defaults to true
        var clip = new TimelineClip();
        clip.Effects.Add(new TimelineEffect { Name = "Test Effect" });

        // Assert
        clip.HasEffects.Should().BeTrue();
    }

    [Fact]
    public void HasEffects_ReturnsFalseWhenAllEffectsDisabled()
    {
        // Arrange
        var clip = new TimelineClip();
        clip.Effects.Add(new TimelineEffect { Name = "Test Effect", IsEnabled = false });

        // Assert
        clip.HasEffects.Should().BeFalse();
    }

    [Fact]
    public void HasColorGrade_ReturnsFalseWhenNull()
    {
        // Arrange
        var clip = new TimelineClip { ColorGrade = null };

        // Assert
        clip.HasColorGrade.Should().BeFalse();
    }

    [Fact]
    public void HasColorGrade_ReturnsFalseWhenDefault()
    {
        // Arrange - A default ColorGrade (all zeros) is treated as "no grade"
        var clip = new TimelineClip { ColorGrade = new ColorGrade() };

        // Assert
        clip.HasColorGrade.Should().BeFalse();
    }

    [Fact]
    public void HasColorGrade_ReturnsTrueWhenModified()
    {
        // Arrange
        var clip = new TimelineClip { ColorGrade = new ColorGrade { Exposure = 0.5 } };

        // Assert
        clip.HasColorGrade.Should().BeTrue();
    }

    [Fact]
    public void SourceInPoint_CalculatesCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourceInFrame = 50,
            SourceDurationFrames = 200
        };

        // Assert
        clip.SourceInPoint.Should().Be(0.25);
    }

    [Fact]
    public void SourceOutPoint_CalculatesCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourceOutFrame = 150,
            SourceDurationFrames = 200
        };

        // Assert
        clip.SourceOutPoint.Should().Be(0.75);
    }

    [Fact]
    public void SourceInPoint_ReturnsZeroWhenDurationIsZero()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourceInFrame = 50,
            SourceDurationFrames = 0
        };

        // Assert
        clip.SourceInPoint.Should().Be(0);
    }

    [Fact]
    public void SourceOutPoint_ReturnsOneWhenDurationIsZero()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourceOutFrame = 50,
            SourceDurationFrames = 0
        };

        // Assert
        clip.SourceOutPoint.Should().Be(1);
    }

    [Fact]
    public void StartSeconds_CalculatesCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 48,
            FrameRate = 24.0
        };

        // Assert
        clip.StartSeconds.Should().Be(2.0);
    }

    [Fact]
    public void EndSeconds_CalculatesCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            EndFrame = 72,
            FrameRate = 24.0
        };

        // Assert
        clip.EndSeconds.Should().Be(3.0);
    }

    [Fact]
    public void DurationFormatted_FormatsCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 1440, // 1 minute at 24fps
            FrameRate = 24.0
        };

        // Act
        var formatted = clip.DurationFormatted;

        // Assert
        formatted.Should().Be("01:00:00"); // MM:SS:FF
    }

    [Fact]
    public void StartTimecode_FormatsCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 86424, // 1 hour at 24fps + 24 frames
            FrameRate = 24.0
        };

        // Act
        var timecode = clip.StartTimecode;

        // Assert
        timecode.Should().StartWith("01:"); // Starts with 1 hour
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        // Arrange
        var original = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = "/path/to/source.mp4",
            StartFrame = 100,
            EndFrame = 200
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_GeneratesNewId()
    {
        // Arrange
        var original = new TimelineClip();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        // Arrange
        var original = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = "/path/to/source.mp4",
            StartFrame = 100,
            EndFrame = 200,
            SourceInFrame = 50,
            SourceOutFrame = 150,
            SourceDurationFrames = 500,
            FrameRate = 30.0,
            IsMuted = true,
            Volume = 0.5,
            TrackType = TrackType.Audio
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Name.Should().Be(original.Name);
        clone.SourcePath.Should().Be(original.SourcePath);
        clone.StartFrame.Should().Be(original.StartFrame);
        clone.EndFrame.Should().Be(original.EndFrame);
        clone.SourceInFrame.Should().Be(original.SourceInFrame);
        clone.SourceOutFrame.Should().Be(original.SourceOutFrame);
        clone.SourceDurationFrames.Should().Be(original.SourceDurationFrames);
        clone.FrameRate.Should().Be(original.FrameRate);
        clone.IsMuted.Should().Be(original.IsMuted);
        clone.Volume.Should().Be(original.Volume);
        clone.TrackType.Should().Be(original.TrackType);
    }

    [Fact]
    public void Clone_DoesNotCopyIsSelected()
    {
        // Arrange - IsSelected is deliberately not cloned
        var original = new TimelineClip { IsSelected = true };

        // Act
        var clone = original.Clone();

        // Assert
        clone.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Clone_DoesNotCopyIsLocked()
    {
        // Arrange - IsLocked is deliberately not cloned
        var original = new TimelineClip { IsLocked = true };

        // Act
        var clone = original.Clone();

        // Assert
        clone.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Clone_DeepCopiesEffects()
    {
        // Arrange
        var original = new TimelineClip();
        original.Effects.Add(new TimelineEffect { Name = "Effect 1" });
        original.Effects.Add(new TimelineEffect { Name = "Effect 2" });

        // Act
        var clone = original.Clone();

        // Assert
        clone.Effects.Should().HaveCount(2);
        clone.Effects.Should().NotBeSameAs(original.Effects);
        clone.Effects[0].Should().NotBeSameAs(original.Effects[0]);
        clone.Effects[0].Name.Should().Be("Effect 1");
    }

    [Fact]
    public void Clone_DeepCopiesColorGrade()
    {
        // Arrange
        var original = new TimelineClip
        {
            ColorGrade = new ColorGrade { Exposure = 1.5 }
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.ColorGrade.Should().NotBeNull();
        clone.ColorGrade.Should().NotBeSameAs(original.ColorGrade);
        clone.ColorGrade!.Exposure.Should().Be(1.5);
    }

    [Fact]
    public void Clone_HandlesNullColorGrade()
    {
        // Arrange
        var original = new TimelineClip { ColorGrade = null };

        // Act
        var clone = original.Clone();

        // Assert
        clone.ColorGrade.Should().BeNull();
    }

    [Fact]
    public void Clone_CopiesColorProperty()
    {
        // Arrange
        var original = new TimelineClip
        {
            Color = System.Windows.Media.Colors.Red
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Color.Should().Be(original.Color);
    }

    #endregion

    #region Effect Management

    [Fact]
    public void AddEffect_AddsToEffectsCollection()
    {
        // Arrange
        var clip = new TimelineClip();
        var effect = new TimelineEffect { Name = "Brightness" };

        // Act
        clip.AddEffect(effect);

        // Assert
        clip.Effects.Should().Contain(effect);
        clip.HasEffects.Should().BeTrue();
    }

    [Fact]
    public void AddEffect_AllowsMultipleEffects()
    {
        // Arrange
        var clip = new TimelineClip();

        // Act
        clip.AddEffect(new TimelineEffect { Name = "Brightness" });
        clip.AddEffect(new TimelineEffect { Name = "Contrast" });
        clip.AddEffect(new TimelineEffect { Name = "Saturation" });

        // Assert
        clip.Effects.Should().HaveCount(3);
    }

    [Fact]
    public void RemoveEffect_RemovesFromCollection()
    {
        // Arrange
        var clip = new TimelineClip();
        var effect = new TimelineEffect { Name = "Brightness" };
        clip.AddEffect(effect);

        // Act
        clip.RemoveEffect(effect);

        // Assert
        clip.Effects.Should().NotContain(effect);
        clip.HasEffects.Should().BeFalse();
    }

    [Fact]
    public void RemoveEffect_HandlesNonExistentEffect()
    {
        // Arrange
        var clip = new TimelineClip();
        var effect = new TimelineEffect { Name = "Non-existent" };

        // Act & Assert - Should not throw
        var action = () => clip.RemoveEffect(effect);
        action.Should().NotThrow();
    }

    [Fact]
    public void MoveEffect_ChangesOrder()
    {
        // Arrange
        var clip = new TimelineClip();
        var effect1 = new TimelineEffect { Name = "Effect 1" };
        var effect2 = new TimelineEffect { Name = "Effect 2" };
        var effect3 = new TimelineEffect { Name = "Effect 3" };
        clip.AddEffect(effect1);
        clip.AddEffect(effect2);
        clip.AddEffect(effect3);

        // Act - Move effect at index 2 (effect3) to position 0
        clip.MoveEffect(2, 0);

        // Assert
        clip.Effects[0].Should().BeSameAs(effect3);
        clip.Effects[1].Should().BeSameAs(effect1);
        clip.Effects[2].Should().BeSameAs(effect2);
    }

    [Fact]
    public void MoveEffect_HandlesInvalidOldIndex()
    {
        // Arrange
        var clip = new TimelineClip();
        clip.AddEffect(new TimelineEffect { Name = "Effect 1" });

        // Act & Assert - Should not throw for invalid index
        var action = () => clip.MoveEffect(-1, 0);
        action.Should().NotThrow();

        action = () => clip.MoveEffect(10, 0);
        action.Should().NotThrow();
    }

    [Fact]
    public void MoveEffect_HandlesInvalidNewIndex()
    {
        // Arrange
        var clip = new TimelineClip();
        clip.AddEffect(new TimelineEffect { Name = "Effect 1" });

        // Act & Assert - Should not throw for invalid index
        var action = () => clip.MoveEffect(0, -1);
        action.Should().NotThrow();

        action = () => clip.MoveEffect(0, 10);
        action.Should().NotThrow();
    }

    #endregion

    #region Volume Tests

    [Fact]
    public void Volume_DefaultsToOne()
    {
        // Arrange & Act
        var clip = new TimelineClip();

        // Assert
        clip.Volume.Should().Be(1.0);
    }

    [Fact]
    public void Volume_CanBeSetToZero()
    {
        // Arrange
        var clip = new TimelineClip();

        // Act
        clip.Volume = 0.0;

        // Assert
        clip.Volume.Should().Be(0.0);
    }

    [Fact]
    public void Volume_AcceptsValuesAboveOne()
    {
        // Arrange - Some systems allow volume boost
        var clip = new TimelineClip();

        // Act
        clip.Volume = 2.0;

        // Assert
        clip.Volume.Should().Be(2.0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void NegativeFrameValues_AreAccepted()
    {
        // Note: Negative frames might be used for pre-roll or special cases
        // The model should accept them without throwing
        var clip = new TimelineClip
        {
            StartFrame = -10,
            EndFrame = 100
        };

        clip.DurationFrames.Should().Be(110);
    }

    [Fact]
    public void ZeroDuration_HandledCorrectly()
    {
        // Arrange
        var clip = new TimelineClip
        {
            StartFrame = 50,
            EndFrame = 50
        };

        // Assert
        clip.DurationFrames.Should().Be(0);
    }

    [Fact]
    public void VeryLongDuration_HandledCorrectly()
    {
        // Arrange - Test with large frame count (e.g., 10 hours at 24fps)
        var clip = new TimelineClip
        {
            StartFrame = 0,
            EndFrame = 864000, // 10 hours at 24fps
            FrameRate = 24.0
        };

        // Assert
        clip.DurationFrames.Should().Be(864000);
        clip.DurationSeconds.Should().Be(36000); // 10 hours in seconds
    }

    [Fact]
    public void SourcePath_CanContainSpecialCharacters()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourcePath = @"C:\Users\Test User\Videos\My Video (2024) [HD].mp4"
        };

        // Assert
        clip.SourcePath.Should().Contain("Test User");
        clip.SourcePath.Should().Contain("(2024)");
        clip.SourcePath.Should().Contain("[HD]");
    }

    [Fact]
    public void SourcePath_CanContainUnicodeCharacters()
    {
        // Arrange
        var clip = new TimelineClip
        {
            SourcePath = @"C:\Users\Test\Videos\日本語ファイル.mp4"
        };

        // Assert
        clip.SourcePath.Should().Contain("日本語");
    }

    [Fact]
    public void LongSourcePath_Accepted()
    {
        // Arrange - Very long path
        var longPath = @"C:\Users\Test\" + new string('a', 200) + ".mp4";
        var clip = new TimelineClip { SourcePath = longPath };

        // Assert
        clip.SourcePath.Should().Be(longPath);
    }

    #endregion

    #region Observable Property Change Notifications

    [Fact]
    public void PropertyChanged_RaisedForName()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.Name))
                propertyChanged = true;
        };

        // Act
        clip.Name = "New Name";

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForStartFrame()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.StartFrame))
                propertyChanged = true;
        };

        // Act
        clip.StartFrame = 100;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsSelected()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.IsSelected))
                propertyChanged = true;
        };

        // Act
        clip.IsSelected = true;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForVolume()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.Volume))
                propertyChanged = true;
        };

        // Act
        clip.Volume = 0.5;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForTrackType()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.TrackType))
                propertyChanged = true;
        };

        // Act
        clip.TrackType = TrackType.Audio;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_NotRaisedWhenValueUnchanged()
    {
        // Arrange
        var clip = new TimelineClip { Name = "Test" };
        var changeCount = 0;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.Name))
                changeCount++;
        };

        // Act - Set same value
        clip.Name = "Test";

        // Assert
        changeCount.Should().Be(0);
    }

    #endregion

    #region Linked Clip Tests

    [Fact]
    public void LinkedClip_DefaultsToNull()
    {
        // Arrange & Act
        var clip = new TimelineClip();

        // Assert
        clip.LinkedClip.Should().BeNull();
    }

    [Fact]
    public void LinkedClip_CanBeSet()
    {
        // Arrange
        var videoClip = new TimelineClip { TrackType = TrackType.Video };
        var audioClip = new TimelineClip { TrackType = TrackType.Audio };

        // Act
        videoClip.LinkedClip = audioClip;

        // Assert
        videoClip.LinkedClip.Should().BeSameAs(audioClip);
    }

    [Fact]
    public void LinkedClip_Clone_DoesNotCloneLink()
    {
        // Arrange - Linked clips should not be cloned together to avoid circular references
        var videoClip = new TimelineClip { TrackType = TrackType.Video };
        var audioClip = new TimelineClip { TrackType = TrackType.Audio };
        videoClip.LinkedClip = audioClip;

        // Act
        var clone = videoClip.Clone();

        // Assert
        clone.LinkedClip.Should().BeNull();
    }

    [Fact]
    public void LinkedClip_CanBeCleared()
    {
        // Arrange
        var videoClip = new TimelineClip { TrackType = TrackType.Video };
        var audioClip = new TimelineClip { TrackType = TrackType.Audio };
        videoClip.LinkedClip = audioClip;

        // Act
        videoClip.LinkedClip = null;

        // Assert
        videoClip.LinkedClip.Should().BeNull();
    }

    #endregion

    #region Color Property Tests

    [Fact]
    public void Color_HasDefaultValue()
    {
        // Arrange & Act
        var clip = new TimelineClip();

        // Assert - Default color is set
        clip.Color.Should().NotBe(default(System.Windows.Media.Color));
    }

    [Fact]
    public void Color_CanBeChanged()
    {
        // Arrange
        var clip = new TimelineClip();
        var newColor = System.Windows.Media.Colors.Red;

        // Act
        clip.Color = newColor;

        // Assert
        clip.Color.Should().Be(newColor);
    }

    [Fact]
    public void Color_PropertyChangedRaised()
    {
        // Arrange
        var clip = new TimelineClip();
        var propertyChanged = false;
        clip.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineClip.Color))
                propertyChanged = true;
        };

        // Act
        clip.Color = System.Windows.Media.Colors.Blue;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion
}
