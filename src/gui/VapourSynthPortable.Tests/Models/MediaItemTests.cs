namespace VapourSynthPortable.Tests.Models;

public class MediaItemTests
{
    #region Default Values Tests

    [Fact]
    public void MediaItem_DefaultValues_AreCorrect()
    {
        // Act
        var item = new MediaItem();

        // Assert
        item.Name.Should().BeEmpty();
        item.FilePath.Should().BeEmpty();
        item.MediaType.Should().Be(MediaType.Unknown);
        item.Width.Should().Be(0);
        item.Height.Should().Be(0);
        item.FrameRate.Should().Be(0);
        item.FrameCount.Should().Be(0);
        item.Duration.Should().Be(0);
        item.FileSize.Should().Be(0);
        item.Codec.Should().BeEmpty();
        item.IsSelected.Should().BeFalse();
        item.IsLoadingThumbnail.Should().BeFalse();
        item.Thumbnail.Should().BeNull();
    }

    #endregion

    #region Resolution Tests

    [Fact]
    public void Resolution_ReturnsCorrectFormat()
    {
        // Arrange
        var item = new MediaItem { Width = 1920, Height = 1080 };

        // Assert
        item.Resolution.Should().Be("1920x1080");
    }

    [Fact]
    public void Resolution_ReturnsEmpty_WhenWidthIsZero()
    {
        // Arrange
        var item = new MediaItem { Width = 0, Height = 1080 };

        // Assert
        item.Resolution.Should().BeEmpty();
    }

    [Fact]
    public void Resolution_ReturnsEmpty_WhenHeightIsZero()
    {
        // Arrange
        var item = new MediaItem { Width = 1920, Height = 0 };

        // Assert
        item.Resolution.Should().BeEmpty();
    }

    #endregion

    #region DurationFormatted Tests

    [Fact]
    public void DurationFormatted_ReturnsCorrectFormat_ForSecondsOnly()
    {
        // Arrange
        var item = new MediaItem { Duration = 45 };

        // Assert
        item.DurationFormatted.Should().Be("00:45");
    }

    [Fact]
    public void DurationFormatted_ReturnsCorrectFormat_ForMinutesAndSeconds()
    {
        // Arrange
        var item = new MediaItem { Duration = 125 }; // 2:05

        // Assert
        item.DurationFormatted.Should().Be("02:05");
    }

    [Fact]
    public void DurationFormatted_ReturnsCorrectFormat_WithHours()
    {
        // Arrange
        var item = new MediaItem { Duration = 3725 }; // 1:02:05

        // Assert
        item.DurationFormatted.Should().Be("01:02:05");
    }

    [Fact]
    public void DurationFormatted_ReturnsEmpty_WhenDurationIsZero()
    {
        // Arrange
        var item = new MediaItem { Duration = 0 };

        // Assert
        item.DurationFormatted.Should().BeEmpty();
    }

    [Fact]
    public void DurationFormatted_ReturnsEmpty_WhenDurationIsNegative()
    {
        // Arrange
        var item = new MediaItem { Duration = -10 };

        // Assert
        item.DurationFormatted.Should().BeEmpty();
    }

    #endregion

    #region FileSizeFormatted Tests

    [Fact]
    public void FileSizeFormatted_ReturnsBytes_ForSmallSizes()
    {
        // Arrange
        var item = new MediaItem { FileSize = 500 };

        // Assert
        item.FileSizeFormatted.Should().Be("500 B");
    }

    [Fact]
    public void FileSizeFormatted_ReturnsKB()
    {
        // Arrange
        var item = new MediaItem { FileSize = 1536 }; // 1.5 KB

        // Assert
        item.FileSizeFormatted.Should().Be("1.5 KB");
    }

    [Fact]
    public void FileSizeFormatted_ReturnsMB()
    {
        // Arrange
        var item = new MediaItem { FileSize = 10 * 1024 * 1024 }; // 10 MB

        // Assert
        item.FileSizeFormatted.Should().Be("10 MB");
    }

    [Fact]
    public void FileSizeFormatted_ReturnsGB()
    {
        // Arrange
        var item = new MediaItem { FileSize = 2L * 1024 * 1024 * 1024 }; // 2 GB

        // Assert
        item.FileSizeFormatted.Should().Be("2 GB");
    }

    [Fact]
    public void FileSizeFormatted_ReturnsEmpty_WhenSizeIsZero()
    {
        // Arrange
        var item = new MediaItem { FileSize = 0 };

        // Assert
        item.FileSizeFormatted.Should().BeEmpty();
    }

    #endregion

    #region MediaTypeIcon Tests

    [Fact]
    public void MediaTypeIcon_ReturnsVideoIcon_ForVideo()
    {
        // Arrange
        var item = new MediaItem { MediaType = MediaType.Video };

        // Assert
        item.MediaTypeIcon.Should().Be("\uE714");
    }

    [Fact]
    public void MediaTypeIcon_ReturnsAudioIcon_ForAudio()
    {
        // Arrange
        var item = new MediaItem { MediaType = MediaType.Audio };

        // Assert
        item.MediaTypeIcon.Should().Be("\uE8D6");
    }

    [Fact]
    public void MediaTypeIcon_ReturnsImageIcon_ForImage()
    {
        // Arrange
        var item = new MediaItem { MediaType = MediaType.Image };

        // Assert
        item.MediaTypeIcon.Should().Be("\uEB9F");
    }

    [Fact]
    public void MediaTypeIcon_ReturnsFileIcon_ForUnknown()
    {
        // Arrange
        var item = new MediaItem { MediaType = MediaType.Unknown };

        // Assert
        item.MediaTypeIcon.Should().Be("\uE8A5");
    }

    #endregion

    #region MediaType Enum Tests

    [Fact]
    public void MediaType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<MediaType>().Should().HaveCount(4);
        Enum.IsDefined(MediaType.Video).Should().BeTrue();
        Enum.IsDefined(MediaType.Audio).Should().BeTrue();
        Enum.IsDefined(MediaType.Image).Should().BeTrue();
        Enum.IsDefined(MediaType.Unknown).Should().BeTrue();
    }

    #endregion
}
