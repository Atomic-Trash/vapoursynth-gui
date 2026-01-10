using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VapourSynthPortable.Helpers;

namespace VapourSynthPortable.Tests.Helpers;

public class ConverterTests
{
    #region BoolToFavoriteIconConverter Tests

    [Fact]
    public void BoolToFavoriteIconConverter_ReturnsFilledStar_WhenTrue()
    {
        // Arrange
        var converter = new BoolToFavoriteIconConverter();

        // Act
        var result = converter.Convert(true, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("\uE735"); // Filled star
    }

    [Fact]
    public void BoolToFavoriteIconConverter_ReturnsOutlineStar_WhenFalse()
    {
        // Arrange
        var converter = new BoolToFavoriteIconConverter();

        // Act
        var result = converter.Convert(false, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("\uE734"); // Outline star
    }

    #endregion

    #region NullOrEmptyToVisibilityConverter Tests

    [Fact]
    public void NullOrEmptyToVisibilityConverter_ReturnsVisible_ForNull()
    {
        // Arrange
        var converter = new NullOrEmptyToVisibilityConverter();

        // Act
        var result = converter.Convert(null!, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void NullOrEmptyToVisibilityConverter_ReturnsVisible_ForEmptyString()
    {
        // Arrange
        var converter = new NullOrEmptyToVisibilityConverter();

        // Act
        var result = converter.Convert("", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void NullOrEmptyToVisibilityConverter_ReturnsCollapsed_ForNonEmptyString()
    {
        // Arrange
        var converter = new NullOrEmptyToVisibilityConverter();

        // Act
        var result = converter.Convert("test", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion

    #region CountToVisibilityInverseConverter Tests

    [Fact]
    public void CountToVisibilityInverseConverter_ReturnsVisible_ForNonZero()
    {
        // Arrange
        var converter = new CountToVisibilityInverseConverter();

        // Act
        var result = converter.Convert(5, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void CountToVisibilityInverseConverter_ReturnsCollapsed_ForZero()
    {
        // Arrange
        var converter = new CountToVisibilityInverseConverter();

        // Act
        var result = converter.Convert(0, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion

    #region TimeSpanToVisibilityConverter Tests

    [Fact]
    public void TimeSpanToVisibilityConverter_ReturnsVisible_ForNonZero()
    {
        // Arrange
        var converter = new TimeSpanToVisibilityConverter();

        // Act
        var result = converter.Convert(TimeSpan.FromMinutes(5), typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void TimeSpanToVisibilityConverter_ReturnsCollapsed_ForZero()
    {
        // Arrange
        var converter = new TimeSpanToVisibilityConverter();

        // Act
        var result = converter.Convert(TimeSpan.Zero, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion

    #region WipeRectConverter Tests

    [Fact]
    public void WipeRectConverter_ReturnsCorrectRect_ForPosition()
    {
        // Arrange
        var converter = new WipeRectConverter();
        var values = new object[] { 100.0, 50.0, 0.5 }; // width, height, position

        // Act
        var result = converter.Convert(values, typeof(Rect), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<Rect>();
        var rect = (Rect)result;
        rect.Width.Should().Be(50); // 100 * 0.5
        rect.Height.Should().Be(50);
        rect.X.Should().Be(0);
        rect.Y.Should().Be(0);
    }

    [Fact]
    public void WipeRectConverter_ReturnsEmptyRect_ForInvalidValues()
    {
        // Arrange
        var converter = new WipeRectConverter();
        var values = new object[] { "invalid", 50.0, 0.5 };

        // Act
        var result = converter.Convert(values, typeof(Rect), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<Rect>();
        var rect = (Rect)result;
        rect.Width.Should().Be(0);
        rect.Height.Should().Be(0);
    }

    [Fact]
    public void WipeRectConverter_ReturnsFullWidth_ForPosition1()
    {
        // Arrange
        var converter = new WipeRectConverter();
        var values = new object[] { 200.0, 100.0, 1.0 }; // width, height, position at 100%

        // Act
        var result = converter.Convert(values, typeof(Rect), null!, CultureInfo.InvariantCulture);

        // Assert
        var rect = (Rect)result;
        rect.Width.Should().Be(200);
    }

    [Fact]
    public void WipeRectConverter_ReturnsZeroWidth_ForPosition0()
    {
        // Arrange
        var converter = new WipeRectConverter();
        var values = new object[] { 200.0, 100.0, 0.0 }; // width, height, position at 0%

        // Act
        var result = converter.Convert(values, typeof(Rect), null!, CultureInfo.InvariantCulture);

        // Assert
        var rect = (Rect)result;
        rect.Width.Should().Be(0);
    }

    #endregion

    #region WipeLineMarginConverter Tests

    [Fact]
    public void WipeLineMarginConverter_ReturnsCorrectMargin()
    {
        // Arrange
        var converter = new WipeLineMarginConverter();

        // Act
        var result = converter.Convert(0.5, typeof(Thickness), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<Thickness>();
        var margin = (Thickness)result;
        margin.Left.Should().Be(250); // 0.5 * 500 (default fallback width)
        margin.Top.Should().Be(0);
        margin.Right.Should().Be(0);
        margin.Bottom.Should().Be(0);
    }

    #endregion

    #region StatusToBackgroundConverter Tests

    [Fact]
    public void StatusToBackgroundConverter_ReturnsCorrectBrush_ForPending()
    {
        // Arrange
        var converter = new StatusToBackgroundConverter();

        // Act
        var result = converter.Convert(ProcessingStatus.Pending, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x33, 0x33, 0x33)); // Gray for pending
    }

    [Fact]
    public void StatusToBackgroundConverter_ReturnsCorrectBrush_ForProcessing()
    {
        // Arrange
        var converter = new StatusToBackgroundConverter();

        // Act
        var result = converter.Convert(ProcessingStatus.Processing, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x1A, 0x3A, 0x5C)); // Blue for processing
    }

    [Fact]
    public void StatusToBackgroundConverter_ReturnsCorrectBrush_ForCompleted()
    {
        // Arrange
        var converter = new StatusToBackgroundConverter();

        // Act
        var result = converter.Convert(ProcessingStatus.Completed, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x1A, 0x3D, 0x2A)); // Green for completed
    }

    [Fact]
    public void StatusToBackgroundConverter_ReturnsCorrectBrush_ForFailed()
    {
        // Arrange
        var converter = new StatusToBackgroundConverter();

        // Act
        var result = converter.Convert(ProcessingStatus.Failed, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x4A, 0x1A, 0x1A)); // Red for failed
    }

    #endregion

    #region StatusToForegroundConverter Tests

    [Fact]
    public void StatusToForegroundConverter_ReturnsCorrectBrush_ForEachStatus()
    {
        // Arrange
        var converter = new StatusToForegroundConverter();

        // Act & Assert
        var pending = converter.Convert(ProcessingStatus.Pending, typeof(Brush), null!, CultureInfo.InvariantCulture) as SolidColorBrush;
        var processing = converter.Convert(ProcessingStatus.Processing, typeof(Brush), null!, CultureInfo.InvariantCulture) as SolidColorBrush;
        var completed = converter.Convert(ProcessingStatus.Completed, typeof(Brush), null!, CultureInfo.InvariantCulture) as SolidColorBrush;
        var failed = converter.Convert(ProcessingStatus.Failed, typeof(Brush), null!, CultureInfo.InvariantCulture) as SolidColorBrush;
        var cancelled = converter.Convert(ProcessingStatus.Cancelled, typeof(Brush), null!, CultureInfo.InvariantCulture) as SolidColorBrush;

        pending!.Color.Should().Be(Color.FromRgb(0xAA, 0xAA, 0xAA));
        processing!.Color.Should().Be(Color.FromRgb(0x6A, 0xB0, 0xFF));
        completed!.Color.Should().Be(Color.FromRgb(0x4A, 0xDE, 0x80));
        failed!.Color.Should().Be(Color.FromRgb(0xE7, 0x48, 0x56));
        cancelled!.Color.Should().Be(Color.FromRgb(0xF5, 0x9E, 0x0B));
    }

    #endregion

    #region BoolToPauseIconConverter Tests

    [Fact]
    public void BoolToPauseIconConverter_ReturnsPlayIcon_WhenPaused()
    {
        // Arrange
        var converter = new BoolToPauseIconConverter();

        // Act
        var result = converter.Convert(true, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("\uE768"); // Play icon when paused
    }

    [Fact]
    public void BoolToPauseIconConverter_ReturnsPauseIcon_WhenNotPaused()
    {
        // Arrange
        var converter = new BoolToPauseIconConverter();

        // Act
        var result = converter.Convert(false, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("\uE769"); // Pause icon when not paused
    }

    #endregion

    #region BoolToPauseTooltipConverter Tests

    [Fact]
    public void BoolToPauseTooltipConverter_ReturnsResumeText_WhenPaused()
    {
        // Arrange
        var converter = new BoolToPauseTooltipConverter();

        // Act
        var result = converter.Convert(true, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("Resume processing");
    }

    [Fact]
    public void BoolToPauseTooltipConverter_ReturnsPauseText_WhenNotPaused()
    {
        // Arrange
        var converter = new BoolToPauseTooltipConverter();

        // Act
        var result = converter.Convert(false, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("Pause processing");
    }

    #endregion

    #region BoolToToggleLabelConverter Tests

    [Fact]
    public void BoolToToggleLabelConverter_ReturnsOriginalLabel_WhenShowingOriginal()
    {
        // Arrange
        var converter = new BoolToToggleLabelConverter();

        // Act
        var result = converter.Convert(true, typeof(string), null!, CultureInfo.InvariantCulture) as string;

        // Assert
        result.Should().Contain("ORIGINAL");
    }

    [Fact]
    public void BoolToToggleLabelConverter_ReturnsProcessedLabel_WhenShowingProcessed()
    {
        // Arrange
        var converter = new BoolToToggleLabelConverter();

        // Act
        var result = converter.Convert(false, typeof(string), null!, CultureInfo.InvariantCulture) as string;

        // Assert
        result.Should().Contain("PROCESSED");
    }

    #endregion

    #region BoolToToggleColorConverter Tests

    [Fact]
    public void BoolToToggleColorConverter_ReturnsGray_WhenShowingOriginal()
    {
        // Arrange
        var converter = new BoolToToggleColorConverter();

        // Act
        var result = converter.Convert(true, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x9E, 0x9E, 0x9E));
    }

    [Fact]
    public void BoolToToggleColorConverter_ReturnsGreen_WhenShowingProcessed()
    {
        // Arrange
        var converter = new BoolToToggleColorConverter();

        // Act
        var result = converter.Convert(false, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.Should().Be(Color.FromRgb(0x4A, 0xDE, 0x80));
    }

    #endregion

    #region InverseBoolConverter Tests

    [Fact]
    public void InverseBoolConverter_ReturnsFalse_WhenTrue()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.Convert(true, typeof(bool), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void InverseBoolConverter_ReturnsTrue_WhenFalse()
    {
        // Arrange
        var converter = new InverseBoolConverter();

        // Act
        var result = converter.Convert(false, typeof(bool), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region BoolToVisibilityConverter Tests

    [Fact]
    public void BoolToVisibilityConverter_ReturnsVisible_WhenTrue()
    {
        // Arrange
        var converter = new BoolToVisibilityConverter();

        // Act
        var result = converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void BoolToVisibilityConverter_ReturnsCollapsed_WhenFalse()
    {
        // Arrange
        var converter = new BoolToVisibilityConverter();

        // Act
        var result = converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion

    #region CountToVisibilityConverter Tests

    [Fact]
    public void CountToVisibilityConverter_ReturnsVisible_WhenZero()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(0, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void CountToVisibilityConverter_ReturnsCollapsed_WhenNonZero()
    {
        // Arrange
        var converter = new CountToVisibilityConverter();

        // Act
        var result = converter.Convert(5, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Visibility.Collapsed);
    }

    #endregion
}
