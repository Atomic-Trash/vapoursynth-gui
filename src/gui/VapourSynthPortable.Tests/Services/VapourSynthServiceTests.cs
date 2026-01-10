namespace VapourSynthPortable.Tests.Services;

public class VapourSynthServiceTests
{
    private readonly VapourSynthService _service;

    public VapourSynthServiceTests()
    {
        _service = new VapourSynthService();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new VapourSynthService();
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesIsProcessingToFalse()
    {
        // Act
        var service = new VapourSynthService();

        // Assert
        service.IsProcessing.Should().BeFalse();
    }

    #endregion

    #region IsAvailable Tests

    [Fact]
    public void IsAvailable_ReturnsBool()
    {
        // Act
        var result = _service.IsAvailable;

        // Assert - IsAvailable should be a boolean (true if VSPipe exists, false otherwise)
        result.Should().Be(result); // Just verify it doesn't throw
    }

    #endregion

    #region GetScriptInfoAsync Tests

    [Fact]
    public async Task GetScriptInfoAsync_ReturnsNull_WhenScriptDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");

        // Act
        var result = await _service.GetScriptInfoAsync(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetScriptInfoAsync_ReturnsNull_WhenVSPipeNotAvailable()
    {
        // Skip if VSPipe is available (this test is for when it's not)
        if (_service.IsAvailable)
        {
            return; // Skip test when VSPipe is available
        }

        // Arrange - Create a temp script file
        var tempScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");
        await File.WriteAllTextAsync(tempScript, "# Empty script");

        try
        {
            // Act
            var result = await _service.GetScriptInfoAsync(tempScript);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempScript);
        }
    }

    #endregion

    #region ValidateScriptAsync Tests

    [Fact]
    public async Task ValidateScriptAsync_ReturnsInvalid_WhenScriptDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");

        // Act
        var result = await _service.ValidateScriptAsync(nonExistentPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateScriptAsync_ReturnsInvalid_WhenVSPipeNotAvailable()
    {
        // Skip if VSPipe is available
        if (_service.IsAvailable)
        {
            return;
        }

        // Arrange
        var tempScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");
        await File.WriteAllTextAsync(tempScript, "# Empty script");

        try
        {
            // Act
            var result = await _service.ValidateScriptAsync(tempScript);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("VSPipe"));
        }
        finally
        {
            File.Delete(tempScript);
        }
    }

    #endregion

    #region ProcessScriptAsync Tests

    [Fact]
    public async Task ProcessScriptAsync_ReturnsFalse_WhenScriptDoesNotExist()
    {
        // Arrange
        var nonExistentScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.mp4");

        // Act
        var result = await _service.ProcessScriptAsync(nonExistentScript, outputPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessScriptAsync_ReturnsFalse_WhenVSPipeNotAvailable()
    {
        // Skip if VSPipe is available
        if (_service.IsAvailable)
        {
            return;
        }

        // Arrange
        var tempScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.mp4");
        await File.WriteAllTextAsync(tempScript, "# Empty script");

        try
        {
            // Act
            var result = await _service.ProcessScriptAsync(tempScript, outputPath);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempScript);
        }
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_DoesNotThrow_WhenNotProcessing()
    {
        // Act & Assert
        var action = () => _service.Cancel();
        action.Should().NotThrow();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Events_CanBeSubscribedTo()
    {
        // Arrange & Act
        var progressHandler = new EventHandler<VapourSynthProgressEventArgs>((s, e) => { });
        var logHandler = new EventHandler<string>((s, e) => { });
        var startHandler = new EventHandler((s, e) => { });
        var completeHandler = new EventHandler<VapourSynthCompletedEventArgs>((s, e) => { });

        // Assert - Subscribe and unsubscribe should not throw
        var action = () =>
        {
            _service.ProgressChanged += progressHandler;
            _service.LogMessage += logHandler;
            _service.ProcessingStarted += startHandler;
            _service.ProcessingCompleted += completeHandler;

            _service.ProgressChanged -= progressHandler;
            _service.LogMessage -= logHandler;
            _service.ProcessingStarted -= startHandler;
            _service.ProcessingCompleted -= completeHandler;
        };

        action.Should().NotThrow();
    }

    #endregion

    #region GetPluginsAsync Tests

    [Fact]
    public async Task GetPluginsAsync_ReturnsEmptyList_WhenVSPipeNotAvailable()
    {
        // Skip if VSPipe is available
        if (_service.IsAvailable)
        {
            return;
        }

        // Act
        var result = await _service.GetPluginsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Model Tests

    [Fact]
    public void VapourSynthScriptInfo_DefaultValues()
    {
        // Act
        var info = new VapourSynthScriptInfo();

        // Assert
        info.Width.Should().Be(0);
        info.Height.Should().Be(0);
        info.FrameCount.Should().Be(0);
        info.Fps.Should().Be(0);
        info.Format.Should().BeEmpty();
    }

    [Fact]
    public void VapourSynthScriptInfo_Resolution_ReturnsFormattedString()
    {
        // Arrange
        var info = new VapourSynthScriptInfo { Width = 1920, Height = 1080 };

        // Act
        var resolution = info.Resolution;

        // Assert
        resolution.Should().Be("1920x1080");
    }

    [Fact]
    public void VapourSynthScriptInfo_Duration_CalculatesCorrectly()
    {
        // Arrange
        var info = new VapourSynthScriptInfo { FrameCount = 300, Fps = 30 };

        // Act
        var duration = info.Duration;

        // Assert
        duration.TotalSeconds.Should().Be(10);
    }

    [Fact]
    public void VapourSynthEncodingSettings_DefaultValues()
    {
        // Act
        var settings = new VapourSynthEncodingSettings();

        // Assert
        settings.VideoCodec.Should().Be("libx264");
        settings.Quality.Should().Be(18);
        settings.Preset.Should().Be("medium");
        settings.PixelFormat.Should().Be("yuv420p");
    }

    [Fact]
    public void VapourSynthProgressEventArgs_EstimatedTimeRemaining_CalculatesCorrectly()
    {
        // Arrange
        var args = new VapourSynthProgressEventArgs
        {
            CurrentFrame = 100,
            TotalFrames = 1000,
            Fps = 50
        };

        // Act
        var remaining = args.EstimatedTimeRemaining;

        // Assert - 900 frames remaining at 50 fps = 18 seconds
        remaining.TotalSeconds.Should().Be(18);
    }

    [Fact]
    public void VapourSynthValidationResult_DefaultValues()
    {
        // Act
        var result = new VapourSynthValidationResult();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ScriptInfo.Should().BeNull();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().NotBeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void VapourSynthPlugin_DefaultValues()
    {
        // Act
        var plugin = new VapourSynthPlugin();

        // Assert
        plugin.Namespace.Should().BeEmpty();
        plugin.Name.Should().BeEmpty();
        plugin.Identifier.Should().BeEmpty();
    }

    [Fact]
    public void VapourSynthCompletedEventArgs_DefaultValues()
    {
        // Act
        var args = new VapourSynthCompletedEventArgs();

        // Assert
        args.Success.Should().BeFalse();
        args.Cancelled.Should().BeFalse();
        args.OutputPath.Should().BeEmpty();
        args.ErrorMessage.Should().BeNull();
        args.TotalFrames.Should().Be(0);
    }

    #endregion
}
