namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for TimelineTrack model.
/// Tests cover: construction, properties, clip management, audio/video track behavior.
/// </summary>
public class TimelineTrackTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var track = new TimelineTrack();

        // Assert
        track.Id.Should().BeGreaterThan(0);
        track.Name.Should().BeEmpty();
        track.TrackType.Should().Be(TrackType.Video);
        track.Clips.Should().NotBeNull();
        track.Clips.Should().BeEmpty();
        track.Transitions.Should().NotBeNull();
        track.IsVisible.Should().BeTrue();
        track.IsMuted.Should().BeFalse();
        track.IsLocked.Should().BeFalse();
        track.IsSolo.Should().BeFalse();
        track.Height.Should().Be(50);
        track.Volume.Should().Be(1.0);
        track.Pan.Should().Be(0.0);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Arrange & Act
        var track1 = new TimelineTrack();
        var track2 = new TimelineTrack();

        // Assert
        track1.Id.Should().NotBe(track2.Id);
    }

    [Fact]
    public void Constructor_WithNameAndType_SetsProperties()
    {
        // Act
        var videoTrack = new TimelineTrack("V1", TrackType.Video);
        var audioTrack = new TimelineTrack("A1", TrackType.Audio);

        // Assert
        videoTrack.Name.Should().Be("V1");
        videoTrack.TrackType.Should().Be(TrackType.Video);
        videoTrack.Height.Should().Be(60); // Video track default height

        audioTrack.Name.Should().Be("A1");
        audioTrack.TrackType.Should().Be(TrackType.Audio);
        audioTrack.Height.Should().Be(40); // Audio track default height
    }

    #endregion

    #region Clip Collection Tests

    [Fact]
    public void Clips_CanAddClips()
    {
        // Arrange
        var track = new TimelineTrack();
        var clip = new TimelineClip { Name = "Test Clip" };

        // Act
        track.Clips.Add(clip);

        // Assert
        track.Clips.Should().HaveCount(1);
        track.Clips.Should().Contain(clip);
    }

    [Fact]
    public void Clips_CanRemoveClips()
    {
        // Arrange
        var track = new TimelineTrack();
        var clip = new TimelineClip { Name = "Test Clip" };
        track.Clips.Add(clip);

        // Act
        track.Clips.Remove(clip);

        // Assert
        track.Clips.Should().BeEmpty();
    }

    [Fact]
    public void Clips_CanHaveMultipleClips()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Clips.Add(new TimelineClip { Name = "Clip 1" });
        track.Clips.Add(new TimelineClip { Name = "Clip 2" });
        track.Clips.Add(new TimelineClip { Name = "Clip 3" });

        // Assert
        track.Clips.Should().HaveCount(3);
    }

    [Fact]
    public void Clips_CanClearAll()
    {
        // Arrange
        var track = new TimelineTrack();
        track.Clips.Add(new TimelineClip());
        track.Clips.Add(new TimelineClip());

        // Act
        track.Clips.Clear();

        // Assert
        track.Clips.Should().BeEmpty();
    }

    #endregion

    #region Transition Collection Tests

    [Fact]
    public void Transitions_InitiallyEmpty()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void Transitions_CanAddTransitions()
    {
        // Arrange
        var track = new TimelineTrack();
        var transition = new TimelineTransition { Name = "Cross Dissolve" };

        // Act
        track.Transitions.Add(transition);

        // Assert
        track.Transitions.Should().HaveCount(1);
        track.Transitions.Should().Contain(transition);
    }

    #endregion

    #region Audio Properties Tests

    [Fact]
    public void Volume_DefaultsToOne()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.Volume.Should().Be(1.0);
    }

    [Fact]
    public void Volume_CanBeSetToZero()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Volume = 0.0;

        // Assert
        track.Volume.Should().Be(0.0);
    }

    [Fact]
    public void Volume_CanBeAboveOne()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Volume = 2.0;

        // Assert
        track.Volume.Should().Be(2.0);
    }

    [Fact]
    public void VolumeDb_CalculatesCorrectly()
    {
        // Arrange - Volume 1.0 = 0 dB
        var track = new TimelineTrack { Volume = 1.0 };

        // Assert
        track.VolumeDb.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void VolumeDb_HandlesZeroVolume()
    {
        // Arrange
        var track = new TimelineTrack { Volume = 0 };

        // Assert - Should return -96 (or similar floor value)
        track.VolumeDb.Should().BeLessThan(-90);
    }

    [Fact]
    public void VolumeDb_CalculatesHalfVolume()
    {
        // Arrange - 0.5 volume ≈ -6dB
        var track = new TimelineTrack { Volume = 0.5 };

        // Assert
        track.VolumeDb.Should().BeApproximately(-6.02, 0.1);
    }

    [Fact]
    public void Pan_DefaultsToCenter()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.Pan.Should().Be(0.0);
    }

    [Fact]
    public void Pan_CanBeSetToFullLeft()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Pan = -1.0;

        // Assert
        track.Pan.Should().Be(-1.0);
    }

    [Fact]
    public void Pan_CanBeSetToFullRight()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Pan = 1.0;

        // Assert
        track.Pan.Should().Be(1.0);
    }

    [Fact]
    public void PanDisplay_ShowsCenter()
    {
        // Arrange
        var track = new TimelineTrack { Pan = 0 };

        // Assert
        track.PanDisplay.Should().Be("C");
    }

    [Fact]
    public void PanDisplay_ShowsLeft()
    {
        // Arrange
        var track = new TimelineTrack { Pan = -0.5 };

        // Assert
        track.PanDisplay.Should().StartWith("L");
        track.PanDisplay.Should().Contain("50");
    }

    [Fact]
    public void PanDisplay_ShowsRight()
    {
        // Arrange
        var track = new TimelineTrack { Pan = 0.75 };

        // Assert
        track.PanDisplay.Should().StartWith("R");
        track.PanDisplay.Should().Contain("75");
    }

    #endregion

    #region Visibility & State Tests

    [Fact]
    public void IsVisible_DefaultsToTrue()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void IsVisible_CanBeToggled()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.IsVisible = false;

        // Assert
        track.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void IsMuted_DefaultsToFalse()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.IsMuted.Should().BeFalse();
    }

    [Fact]
    public void IsMuted_CanBeToggled()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.IsMuted = true;

        // Assert
        track.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void IsLocked_DefaultsToFalse()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_CanBeToggled()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.IsLocked = true;

        // Assert
        track.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void IsSolo_DefaultsToFalse()
    {
        // Arrange & Act
        var track = new TimelineTrack();

        // Assert
        track.IsSolo.Should().BeFalse();
    }

    [Fact]
    public void IsSolo_CanBeToggled()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.IsSolo = true;

        // Assert
        track.IsSolo.Should().BeTrue();
    }

    #endregion

    #region Height Tests

    [Fact]
    public void Height_VideoTrack_DefaultsTo60()
    {
        // Arrange & Act
        var track = new TimelineTrack("V1", TrackType.Video);

        // Assert
        track.Height.Should().Be(60);
    }

    [Fact]
    public void Height_AudioTrack_DefaultsTo40()
    {
        // Arrange & Act
        var track = new TimelineTrack("A1", TrackType.Audio);

        // Assert
        track.Height.Should().Be(40);
    }

    [Fact]
    public void Height_CanBeCustomized()
    {
        // Arrange
        var track = new TimelineTrack();

        // Act
        track.Height = 100;

        // Assert
        track.Height.Should().Be(100);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_RaisedForName()
    {
        // Arrange
        var track = new TimelineTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineTrack.Name))
                propertyChanged = true;
        };

        // Act
        track.Name = "New Track";

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsMuted()
    {
        // Arrange
        var track = new TimelineTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineTrack.IsMuted))
                propertyChanged = true;
        };

        // Act
        track.IsMuted = true;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForVolume()
    {
        // Arrange
        var track = new TimelineTrack();
        var propertyChanged = false;
        track.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineTrack.Volume))
                propertyChanged = true;
        };

        // Act
        track.Volume = 0.5;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion
}
