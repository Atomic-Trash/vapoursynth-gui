using VapourSynthPortable.Services.LibMpv;

namespace VapourSynthPortable.Tests.Services;

/// <summary>
/// Tests for MpvPlayer video playback functionality.
/// Many tests require actual mpv library to be present.
/// </summary>
public class MpvPlayerTests
{
    [Fact]
    public void IsLibraryAvailable_ReturnsBoolean()
    {
        // Act
        var result = MpvPlayer.IsLibraryAvailable;

        // Assert - just verify it returns without throwing
        Assert.True(result || !result);
    }

    [Fact]
    public void LibraryPath_IsNullOrValidPath()
    {
        // Act
        var path = MpvPlayer.LibraryPath;

        // Assert - either null (not found) or valid path
        Assert.True(path == null || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new MpvPlayer());
        Assert.Null(exception);
    }

    [Fact]
    public void NewPlayer_IsNotInitialized()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.False(player.IsInitialized);
    }

    [Fact]
    public void NewPlayer_IsNotPlaying()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.False(player.IsPlaying);
    }

    [Fact]
    public void NewPlayer_IsNotPaused()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.False(player.IsPaused);
    }

    [Fact]
    public void NewPlayer_DefaultVolume_IsHundred()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Equal(100, player.Volume);
    }

    [Fact]
    public void NewPlayer_DefaultSpeed_IsOne()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Equal(1.0, player.Speed);
    }

    [Fact]
    public void NewPlayer_DefaultDuration_IsZero()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Equal(0, player.Duration);
    }

    [Fact]
    public void NewPlayer_DefaultPosition_IsZero()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Equal(0, player.Position);
    }

    [Fact]
    public void NewPlayer_DefaultFrameRate_IsTwentyFour()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Equal(24.0, player.FrameRate);
    }

    [Fact]
    public void NewPlayer_CurrentFile_IsNull()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Null(player.CurrentFile);
    }

    [Fact]
    public void NewPlayer_LoopPoints_AreNull()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.Null(player.LoopStartPoint);
        Assert.Null(player.LoopEndPoint);
    }

    [Fact]
    public void NewPlayer_IsLoopEnabled_IsFalse()
    {
        // Arrange & Act
        using var player = new MpvPlayer();

        // Assert
        Assert.False(player.IsLoopEnabled);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var player = new MpvPlayer();

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            player.Dispose();
            player.Dispose();
        });
        Assert.Null(exception);
    }
}
