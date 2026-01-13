namespace VapourSynthPortable.Tests.Models;

/// <summary>
/// Comprehensive tests for TimelineEffect model.
/// Tests cover: construction, parameters, keyframes, cloning, VapourSynth integration.
/// </summary>
public class TimelineEffectTests
{
    #region Constructor & Default Values

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var effect = new TimelineEffect();

        // Assert
        effect.Id.Should().BeGreaterThan(0);
        effect.Name.Should().BeEmpty();
        effect.Category.Should().BeEmpty();
        effect.IsEnabled.Should().BeTrue();
        effect.IsExpanded.Should().BeTrue();
        effect.Parameters.Should().NotBeNull();
        effect.KeyframeTracks.Should().NotBeNull();
        effect.VsNamespace.Should().BeEmpty();
        effect.VsFunction.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Arrange & Act
        var effect1 = new TimelineEffect();
        var effect2 = new TimelineEffect();

        // Assert
        effect1.Id.Should().NotBe(effect2.Id);
    }

    [Fact]
    public void Constructor_GeneratesIncrementingIds()
    {
        // Arrange & Act
        var effect1 = new TimelineEffect();
        var effect2 = new TimelineEffect();
        var effect3 = new TimelineEffect();

        // Assert
        effect2.Id.Should().BeGreaterThan(effect1.Id);
        effect3.Id.Should().BeGreaterThan(effect2.Id);
    }

    #endregion

    #region Enabled State Tests

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        // Arrange & Act
        var effect = new TimelineEffect();

        // Assert
        effect.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_CanBeDisabled()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.IsEnabled = false;

        // Assert
        effect.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Parameter Tests

    [Fact]
    public void Parameters_InitiallyEmpty()
    {
        // Arrange & Act
        var effect = new TimelineEffect();

        // Assert
        effect.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Parameters_CanAddParameters()
    {
        // Arrange
        var effect = new TimelineEffect();
        var param = new EffectParameter { Name = "Strength", DefaultValue = 1.0 };

        // Act
        effect.Parameters.Add(param);

        // Assert
        effect.Parameters.Should().HaveCount(1);
        effect.Parameters.Should().Contain(param);
    }

    [Fact]
    public void Parameters_CanHaveMultiple()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.Parameters.Add(new EffectParameter { Name = "Strength" });
        effect.Parameters.Add(new EffectParameter { Name = "Radius" });
        effect.Parameters.Add(new EffectParameter { Name = "Threshold" });

        // Assert
        effect.Parameters.Should().HaveCount(3);
    }

    #endregion

    #region Keyframe Tests

    [Fact]
    public void KeyframeTracks_InitiallyEmpty()
    {
        // Arrange & Act
        var effect = new TimelineEffect();

        // Assert
        effect.KeyframeTracks.Should().BeEmpty();
    }

    [Fact]
    public void KeyframeTracks_CanAddTracks()
    {
        // Arrange
        var effect = new TimelineEffect();
        var track = new KeyframeTrack { ParameterName = "Strength" };

        // Act
        effect.KeyframeTracks.Add(track);

        // Assert
        effect.KeyframeTracks.Should().HaveCount(1);
        effect.KeyframeTracks.Should().Contain(track);
    }

    [Fact]
    public void HasKeyframes_ReturnsFalseWhenNoKeyframeTracks()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Assert
        effect.HasKeyframes.Should().BeFalse();
    }

    [Fact]
    public void HasKeyframes_ReturnsFalseWhenTracksHaveNoKeyframes()
    {
        // Arrange
        var effect = new TimelineEffect();
        effect.KeyframeTracks.Add(new KeyframeTrack { ParameterName = "Strength" });

        // Assert - Empty keyframe track
        effect.HasKeyframes.Should().BeFalse();
    }

    [Fact]
    public void HasKeyframes_ReturnsTrueWhenTracksHaveKeyframes()
    {
        // Arrange
        var effect = new TimelineEffect();
        var track = new KeyframeTrack { ParameterName = "Strength" };
        track.Keyframes.Add(new Keyframe { Frame = 0, Value = 1.0 });
        effect.KeyframeTracks.Add(track);

        // Assert
        effect.HasKeyframes.Should().BeTrue();
    }

    #endregion

    #region VapourSynth Integration Tests

    [Fact]
    public void VsNamespace_CanBeSet()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.VsNamespace = "std";

        // Assert
        effect.VsNamespace.Should().Be("std");
    }

    [Fact]
    public void VsFunction_CanBeSet()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.VsFunction = "Crop";

        // Assert
        effect.VsFunction.Should().Be("Crop");
    }

    [Fact]
    public void CommonVsNamespaces_CanBeStored()
    {
        var namespaces = new[] { "std", "resize", "bm3d", "knlm", "nnedi3", "eedi3" };

        foreach (var ns in namespaces)
        {
            var effect = new TimelineEffect { VsNamespace = ns };
            effect.VsNamespace.Should().Be(ns);
        }
    }

    [Fact]
    public void CommonVsFunctions_CanBeStored()
    {
        var functions = new[]
        {
            "Crop", "Lanczos", "Bicubic", "VAggregate", "KNLMeansCL", "nnedi3", "eedi3m"
        };

        foreach (var func in functions)
        {
            var effect = new TimelineEffect { VsFunction = func };
            effect.VsFunction.Should().Be(func);
        }
    }

    #endregion

    #region Category Tests

    [Fact]
    public void Category_DefaultsToEmpty()
    {
        // Arrange & Act
        var effect = new TimelineEffect();

        // Assert
        effect.Category.Should().BeEmpty();
    }

    [Fact]
    public void Category_CanBeSet()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.Category = "Denoise";

        // Assert
        effect.Category.Should().Be("Denoise");
    }

    [Fact]
    public void CommonCategories_CanBeStored()
    {
        var categories = new[]
        {
            "Resize", "Denoise", "Sharpen", "Color", "Deinterlace", "Upscale", "Restoration"
        };

        foreach (var category in categories)
        {
            var effect = new TimelineEffect { Category = category };
            effect.Category.Should().Be(category);
        }
    }

    #endregion

    #region IsExpanded Tests

    [Fact]
    public void IsExpanded_DefaultsToTrue()
    {
        // Arrange & Act
        var effect = new TimelineEffect();

        // Assert
        effect.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void IsExpanded_CanBeToggled()
    {
        // Arrange
        var effect = new TimelineEffect();

        // Act
        effect.IsExpanded = false;

        // Assert
        effect.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        // Arrange
        var original = new TimelineEffect { Name = "Test Effect" };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_GeneratesNewId()
    {
        // Arrange
        var original = new TimelineEffect();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        // Arrange
        var original = new TimelineEffect
        {
            Name = "Test Effect",
            Category = "Denoise",
            IsEnabled = true,
            IsExpanded = false,
            VsNamespace = "bm3d",
            VsFunction = "VAggregate"
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Name.Should().Be(original.Name);
        clone.Category.Should().Be(original.Category);
        clone.IsEnabled.Should().Be(original.IsEnabled);
        // Note: IsExpanded is intentionally NOT cloned (UI state)
        clone.IsExpanded.Should().BeTrue(); // Always defaults to true
        clone.VsNamespace.Should().Be(original.VsNamespace);
        clone.VsFunction.Should().Be(original.VsFunction);
    }

    [Fact]
    public void Clone_DeepCopiesParameters()
    {
        // Arrange
        var original = new TimelineEffect();
        original.Parameters.Add(new EffectParameter { Name = "Strength", Value = 0.5 });

        // Act
        var clone = original.Clone();

        // Assert
        clone.Parameters.Should().HaveCount(1);
        clone.Parameters.Should().NotBeSameAs(original.Parameters);
        clone.Parameters[0].Should().NotBeSameAs(original.Parameters[0]);
        clone.Parameters[0].Value.Should().Be(0.5);
    }

    [Fact]
    public void Clone_DeepCopiesKeyframeTracks()
    {
        // Arrange
        var original = new TimelineEffect();
        var track = new KeyframeTrack { ParameterName = "Strength" };
        track.Keyframes.Add(new Keyframe { Frame = 0, Value = 1.0 });
        original.KeyframeTracks.Add(track);

        // Act
        var clone = original.Clone();

        // Assert
        clone.KeyframeTracks.Should().HaveCount(1);
        clone.KeyframeTracks.Should().NotBeSameAs(original.KeyframeTracks);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_RaisedForName()
    {
        // Arrange
        var effect = new TimelineEffect();
        var propertyChanged = false;
        effect.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineEffect.Name))
                propertyChanged = true;
        };

        // Act
        effect.Name = "New Effect";

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsEnabled()
    {
        // Arrange
        var effect = new TimelineEffect();
        var propertyChanged = false;
        effect.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineEffect.IsEnabled))
                propertyChanged = true;
        };

        // Act
        effect.IsEnabled = false;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_RaisedForIsExpanded()
    {
        // Arrange
        var effect = new TimelineEffect();
        var propertyChanged = false;
        effect.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineEffect.IsExpanded))
                propertyChanged = true;
        };

        // Act
        effect.IsExpanded = false;

        // Assert
        propertyChanged.Should().BeTrue();
    }

    #endregion
}
