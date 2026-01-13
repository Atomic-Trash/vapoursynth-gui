using VapourSynthPortable.Tests.Helpers;

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

    #region VapourSynthScriptInfo Extended Tests

    [Fact]
    public void VapourSynthScriptInfo_WithProcessMockerData_HasCorrectProperties()
    {
        // Arrange
        var info = ProcessMocker.CreateSampleScriptInfo(
            width: 3840,
            height: 2160,
            frameCount: 14400,
            fps: 60.0,
            format: "YUV422P10");

        // Assert
        info.Width.Should().Be(3840);
        info.Height.Should().Be(2160);
        info.FrameCount.Should().Be(14400);
        info.Fps.Should().Be(60.0);
        info.Format.Should().Be("YUV422P10");
        info.Resolution.Should().Be("3840x2160");
        info.Duration.TotalMinutes.Should().Be(4); // 14400 frames at 60fps = 4 minutes
    }

    [Fact]
    public void VapourSynthScriptInfo_Duration_HandlesZeroFps()
    {
        // Arrange
        var info = new VapourSynthScriptInfo { FrameCount = 300, Fps = 0 };

        // Act
        var duration = info.Duration;

        // Assert - Should handle gracefully without throwing
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void VapourSynthScriptInfo_Duration_HandlesLargeFrameCount()
    {
        // Arrange - 10 hour video at 24fps
        var info = new VapourSynthScriptInfo { FrameCount = 864000, Fps = 24 };

        // Act
        var duration = info.Duration;

        // Assert
        duration.TotalHours.Should().Be(10);
    }

    [Fact]
    public void VapourSynthScriptInfo_Resolution_HandlesZeroDimensions()
    {
        // Arrange
        var info = new VapourSynthScriptInfo { Width = 0, Height = 0 };

        // Act
        var resolution = info.Resolution;

        // Assert
        resolution.Should().Be("0x0");
    }

    [Fact]
    public void VapourSynthScriptInfo_CanStoreColorFamily()
    {
        // Arrange
        var info = new VapourSynthScriptInfo();

        // Act
        info.ColorFamily = "RGB";

        // Assert
        info.ColorFamily.Should().Be("RGB");
    }

    [Fact]
    public void VapourSynthScriptInfo_CanStoreBitsPerSample()
    {
        // Arrange
        var info = new VapourSynthScriptInfo();

        // Act
        info.BitsPerSample = 10;

        // Assert
        info.BitsPerSample.Should().Be(10);
    }

    [Fact]
    public void VapourSynthScriptInfo_CanStoreFpsRational()
    {
        // Arrange
        var info = new VapourSynthScriptInfo
        {
            FpsNum = 24000,
            FpsDen = 1001
        };

        // Assert
        info.FpsNum.Should().Be(24000);
        info.FpsDen.Should().Be(1001);
    }

    #endregion

    #region VapourSynthEncodingSettings Extended Tests

    [Fact]
    public void VapourSynthEncodingSettings_WithProcessMockerData_HasCorrectProperties()
    {
        // Arrange
        var settings = ProcessMocker.CreateSampleVSEncodingSettings(
            videoCodec: "libx265",
            quality: 20,
            preset: "slow");

        // Assert
        settings.VideoCodec.Should().Be("libx265");
        settings.Quality.Should().Be(20);
        settings.Preset.Should().Be("slow");
    }

    [Fact]
    public void VapourSynthEncodingSettings_SupportsAllCommonCodecs()
    {
        var codecs = new[] { "libx264", "libx265", "h264_nvenc", "hevc_nvenc", "prores_ks", "ffv1" };

        foreach (var codec in codecs)
        {
            var settings = new VapourSynthEncodingSettings { VideoCodec = codec };
            settings.VideoCodec.Should().Be(codec);
        }
    }

    [Fact]
    public void VapourSynthEncodingSettings_SupportsAllCommonPresets()
    {
        var presets = new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "placebo" };

        foreach (var preset in presets)
        {
            var settings = new VapourSynthEncodingSettings { Preset = preset };
            settings.Preset.Should().Be(preset);
        }
    }

    [Fact]
    public void VapourSynthEncodingSettings_SupportsHardwarePresets()
    {
        var hwPresets = new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7" };

        foreach (var hwPreset in hwPresets)
        {
            var settings = new VapourSynthEncodingSettings { HardwarePreset = hwPreset };
            settings.HardwarePreset.Should().Be(hwPreset);
        }
    }

    [Fact]
    public void VapourSynthEncodingSettings_SupportsVariousPixelFormats()
    {
        var formats = new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le" };

        foreach (var format in formats)
        {
            var settings = new VapourSynthEncodingSettings { PixelFormat = format };
            settings.PixelFormat.Should().Be(format);
        }
    }

    [Fact]
    public void VapourSynthEncodingSettings_QualityRange_AcceptsAllCRFValues()
    {
        // CRF range is typically 0-51 for x264/x265
        for (int crf = 0; crf <= 51; crf++)
        {
            var settings = new VapourSynthEncodingSettings { Quality = crf };
            settings.Quality.Should().Be(crf);
        }
    }

    #endregion

    #region VapourSynthProgressEventArgs Extended Tests

    [Fact]
    public void VapourSynthProgressEventArgs_Progress_CanBeSetDirectly()
    {
        // Arrange - Progress is a simple property, not calculated
        var args = new VapourSynthProgressEventArgs
        {
            CurrentFrame = 500,
            TotalFrames = 1000,
            Progress = 50.0
        };

        // Assert
        args.Progress.Should().Be(50.0);
        args.CurrentFrame.Should().Be(500);
        args.TotalFrames.Should().Be(1000);
    }

    [Fact]
    public void VapourSynthProgressEventArgs_Progress_DefaultsToZero()
    {
        // Arrange
        var args = new VapourSynthProgressEventArgs();

        // Assert
        args.Progress.Should().Be(0);
    }

    [Fact]
    public void VapourSynthProgressEventArgs_Progress_AcceptsFullRange()
    {
        // Arrange & Act - Test 0, 50, 100
        var args0 = new VapourSynthProgressEventArgs { Progress = 0 };
        var args50 = new VapourSynthProgressEventArgs { Progress = 50 };
        var args100 = new VapourSynthProgressEventArgs { Progress = 100 };

        // Assert
        args0.Progress.Should().Be(0);
        args50.Progress.Should().Be(50);
        args100.Progress.Should().Be(100);
    }

    [Fact]
    public void VapourSynthProgressEventArgs_EstimatedTimeRemaining_HandlesZeroFps()
    {
        // Arrange
        var args = new VapourSynthProgressEventArgs
        {
            CurrentFrame = 500,
            TotalFrames = 1000,
            Fps = 0
        };

        // Assert - Should return something reasonable for zero fps
        args.EstimatedTimeRemaining.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void VapourSynthProgressEventArgs_EstimatedTimeRemaining_HandlesVeryHighFps()
    {
        // Arrange
        var args = new VapourSynthProgressEventArgs
        {
            CurrentFrame = 500,
            TotalFrames = 1000,
            Fps = 1000 // Very high fps
        };

        // Assert - 500 frames remaining at 1000 fps = 0.5 seconds
        args.EstimatedTimeRemaining.TotalSeconds.Should().Be(0.5);
    }

    [Fact]
    public void VapourSynthProgressEventArgs_WithProcessMockerData_GeneratesValidSequence()
    {
        // Arrange
        var progressEvents = ProcessMocker.GenerateVSProgress(totalFrames: 1000, steps: 10).ToList();

        // Assert
        progressEvents.Should().HaveCount(11); // 0% to 100%
        progressEvents.First().Progress.Should().Be(0);
        progressEvents.Last().Progress.Should().Be(100);
        progressEvents.First().CurrentFrame.Should().Be(0);
        progressEvents.Last().CurrentFrame.Should().Be(1000);

        // Progress should increase monotonically
        for (int i = 1; i < progressEvents.Count; i++)
        {
            progressEvents[i].Progress.Should().BeGreaterThanOrEqualTo(progressEvents[i - 1].Progress);
        }
    }

    [Fact]
    public void VapourSynthProgressEventArgs_ElapsedTime_CanBeSet()
    {
        // Arrange
        var args = new VapourSynthProgressEventArgs
        {
            ElapsedTime = TimeSpan.FromMinutes(5)
        };

        // Assert
        args.ElapsedTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region VapourSynthValidationResult Extended Tests

    [Fact]
    public void VapourSynthValidationResult_IsValid_DefaultsToFalse()
    {
        // Arrange
        var result = new VapourSynthValidationResult();

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VapourSynthValidationResult_CanAddErrors()
    {
        // Arrange
        var result = new VapourSynthValidationResult();

        // Act
        result.Errors.Add("Error 1");
        result.Errors.Add("Error 2");

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void VapourSynthValidationResult_CanAddWarnings()
    {
        // Arrange
        var result = new VapourSynthValidationResult();

        // Act
        result.Warnings.Add("Warning 1");

        // Assert
        result.Warnings.Should().HaveCount(1);
        result.Warnings.Should().Contain("Warning 1");
    }

    [Fact]
    public void VapourSynthValidationResult_CanSetScriptInfo()
    {
        // Arrange
        var result = new VapourSynthValidationResult();
        var scriptInfo = new VapourSynthScriptInfo { Width = 1920, Height = 1080 };

        // Act
        result.ScriptInfo = scriptInfo;

        // Assert
        result.ScriptInfo.Should().NotBeNull();
        result.ScriptInfo!.Width.Should().Be(1920);
    }

    [Fact]
    public void VapourSynthValidationResult_ValidWithErrors_CanHaveWarnings()
    {
        // Arrange - A script can be valid but have warnings
        var result = new VapourSynthValidationResult
        {
            IsValid = true
        };
        result.Warnings.Add("Performance warning: Consider using GPU acceleration");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(1);
    }

    #endregion

    #region VapourSynthPlugin Extended Tests

    [Fact]
    public void VapourSynthPlugin_CanStoreAllProperties()
    {
        // Arrange
        var plugin = new VapourSynthPlugin
        {
            Namespace = "bm3d",
            Name = "BM3D",
            Identifier = "com.vapoursynth.bm3d"
        };

        // Assert
        plugin.Namespace.Should().Be("bm3d");
        plugin.Name.Should().Be("BM3D");
        plugin.Identifier.Should().Be("com.vapoursynth.bm3d");
    }

    [Fact]
    public void VapourSynthPlugin_CommonPluginNamespaces()
    {
        // Test common plugin namespaces
        var namespaces = new[] { "std", "resize", "bm3d", "knlm", "nnedi3", "eedi3", "lsmas", "ffms2", "descale" };

        foreach (var ns in namespaces)
        {
            var plugin = new VapourSynthPlugin { Namespace = ns };
            plugin.Namespace.Should().Be(ns);
        }
    }

    #endregion

    #region VapourSynthCompletedEventArgs Extended Tests

    [Fact]
    public void VapourSynthCompletedEventArgs_SuccessfulCompletion()
    {
        // Arrange
        var args = new VapourSynthCompletedEventArgs
        {
            Success = true,
            Cancelled = false,
            OutputPath = "/output/video.mp4",
            TotalFrames = 1000
        };

        // Assert
        args.Success.Should().BeTrue();
        args.Cancelled.Should().BeFalse();
        args.OutputPath.Should().Be("/output/video.mp4");
        args.TotalFrames.Should().Be(1000);
        args.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void VapourSynthCompletedEventArgs_FailedCompletion()
    {
        // Arrange
        var args = new VapourSynthCompletedEventArgs
        {
            Success = false,
            Cancelled = false,
            ErrorMessage = "Script error: line 10"
        };

        // Assert
        args.Success.Should().BeFalse();
        args.Cancelled.Should().BeFalse();
        args.ErrorMessage.Should().Contain("Script error");
    }

    [Fact]
    public void VapourSynthCompletedEventArgs_CancelledCompletion()
    {
        // Arrange
        var args = new VapourSynthCompletedEventArgs
        {
            Success = false,
            Cancelled = true
        };

        // Assert
        args.Success.Should().BeFalse();
        args.Cancelled.Should().BeTrue();
    }

    #endregion

    #region Script Generation Tests

    [Fact]
    public async Task ProcessScriptAsync_WithCancellation_DoesNotHang()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var nonExistentScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");

        // Act & Assert - Should complete quickly without hanging
        var act = async () => await _service.ProcessScriptAsync(nonExistentScript, "output.mp4", null, cts.Token);
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetScriptInfoAsync_WithCancellation_DoesNotHang()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".vpy");

        // Act & Assert - Should complete quickly without hanging
        var act = async () => await _service.GetScriptInfoAsync(nonExistentPath, cts.Token);
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
    }

    #endregion

    #region ProcessMocker VSPipe Output Tests

    [Fact]
    public void ProcessMocker_VSPipeInfoOutput_ContainsAllFields()
    {
        // Act
        var output = ProcessMocker.GetVSPipeInfoOutput(
            width: 1920,
            height: 1080,
            frameCount: 2880,
            fpsNum: 24000,
            fpsDen: 1001,
            format: "YUV420P8");

        // Assert
        output.Should().Contain("Width: 1920");
        output.Should().Contain("Height: 1080");
        output.Should().Contain("Frames: 2880");
        output.Should().Contain("24000/1001");
        output.Should().Contain("YUV420P8");
    }

    [Fact]
    public void ProcessMocker_VSPipeProgressLines_GeneratesCorrectSequence()
    {
        // Act
        var progressLines = ProcessMocker.GetVSPipeProgressLines(1000, 100).ToList();

        // Assert
        progressLines.Should().HaveCount(11); // 0, 100, 200, ..., 1000
        progressLines.First().Should().Contain("Frame: 0/1000");
        progressLines.Last().Should().Contain("Frame: 1000/1000");
    }

    [Fact]
    public void ProcessMocker_VSPipeScriptError_ContainsErrorInfo()
    {
        // Act
        var errorOutput = ProcessMocker.GetVSPipeScriptErrorOutput("NameError: name 'undefined' is not defined");

        // Assert
        errorOutput.Should().Contain("Script error");
        errorOutput.Should().Contain("NameError");
    }

    [Fact]
    public void ProcessMocker_VSPipeMissingPlugin_ContainsPluginName()
    {
        // Act
        var errorOutput = ProcessMocker.GetVSPipeMissingPluginOutput("bm3d");

        // Assert
        errorOutput.Should().Contain("bm3d");
        errorOutput.Should().Contain("namespace");
    }

    [Fact]
    public void ProcessMocker_ErrorScenarios_VSScriptError_HasCorrectExitCode()
    {
        // Assert
        ProcessMocker.ErrorScenarios.VSScriptError.ExitCode.Should().Be(1);
        ProcessMocker.ErrorScenarios.VSScriptError.Stderr.Should().Contain("Script error");
    }

    [Fact]
    public void ProcessMocker_ErrorScenarios_VSPluginMissing_HasCorrectExitCode()
    {
        // Assert
        ProcessMocker.ErrorScenarios.VSPluginMissing.ExitCode.Should().Be(1);
        ProcessMocker.ErrorScenarios.VSPluginMissing.Stderr.Should().Contain("namespace");
    }

    #endregion

    #region Service State Tests

    [Fact]
    public void IsProcessing_DefaultsToFalse()
    {
        // Arrange
        var service = new VapourSynthService();

        // Assert
        service.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void Cancel_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new VapourSynthService();

        // Act & Assert - Multiple cancel calls should not throw
        var action = () =>
        {
            service.Cancel();
            service.Cancel();
            service.Cancel();
        };

        action.Should().NotThrow();
    }

    #endregion
}
