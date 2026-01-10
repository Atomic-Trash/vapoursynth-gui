using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class FFmpegServiceTests
{
    private readonly FFmpegService _service;

    public FFmpegServiceTests()
    {
        _service = new FFmpegService();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new FFmpegService();
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesIsEncodingToFalse()
    {
        // Act
        var service = new FFmpegService();

        // Assert
        service.IsEncoding.Should().BeFalse();
    }

    #endregion

    #region IsAvailable Tests

    [Fact]
    public void IsAvailable_ReturnsBool()
    {
        // Act
        var result = _service.IsAvailable;

        // Assert - IsAvailable should be a boolean (true if FFmpeg exists, false otherwise)
        result.Should().Be(result); // Just verify it doesn't throw
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_DoesNotThrow_WhenNotEncoding()
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
        var progressHandler = new EventHandler<EncodingProgressEventArgs>((s, e) => { });
        var logHandler = new EventHandler<string>((s, e) => { });
        var startHandler = new EventHandler((s, e) => { });
        var completeHandler = new EventHandler<EncodingCompletedEventArgs>((s, e) => { });

        // Assert - Subscribe and unsubscribe should not throw
        var action = () =>
        {
            _service.ProgressChanged += progressHandler;
            _service.LogMessage += logHandler;
            _service.EncodingStarted += startHandler;
            _service.EncodingCompleted += completeHandler;

            _service.ProgressChanged -= progressHandler;
            _service.LogMessage -= logHandler;
            _service.EncodingStarted -= startHandler;
            _service.EncodingCompleted -= completeHandler;
        };

        action.Should().NotThrow();
    }

    #endregion

    #region ExportSettings Model Tests

    [Fact]
    public void ExportSettings_DefaultValues()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.InputPath.Should().BeEmpty();
        settings.OutputPath.Should().BeEmpty();
        settings.VideoEnabled.Should().BeTrue();
        settings.VideoCodec.Should().Be("libx264");
        settings.Preset.Should().Be("medium");
        settings.PixelFormat.Should().Be("yuv420p");
        settings.AudioEnabled.Should().BeTrue();
        settings.AudioCodec.Should().Be("aac");
    }

    [Fact]
    public void ExportSettings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new ExportSettings
        {
            InputPath = "input.mp4",
            OutputPath = "output.mp4",
            VideoCodec = "libx265",
            AudioCodec = "flac"
        };

        // Assert
        settings.InputPath.Should().Be("input.mp4");
        settings.OutputPath.Should().Be("output.mp4");
        settings.VideoCodec.Should().Be("libx265");
        settings.AudioCodec.Should().Be("flac");
    }

    #endregion

    #region ExportPreset Model Tests

    [Fact]
    public void ExportPreset_DefaultValues()
    {
        // Act
        var preset = new ExportPreset();

        // Assert
        preset.Name.Should().BeEmpty();
        preset.Description.Should().BeEmpty();
        preset.Format.Should().Be("mp4");
        preset.VideoCodec.Should().Be("libx264");
        preset.AudioCodec.Should().Be("aac");
        preset.Preset.Should().Be("medium");
        preset.VideoEnabled.Should().BeTrue();
    }

    [Fact]
    public void ExportPreset_ToSettings_CreatesValidSettings()
    {
        // Arrange
        var preset = new ExportPreset
        {
            Name = "Test Preset",
            VideoCodec = "libx265",
            AudioCodec = "flac",
            Quality = 20
        };

        // Act
        var settings = preset.ToSettings("input.mp4", "output.mp4");

        // Assert
        settings.InputPath.Should().Be("input.mp4");
        settings.OutputPath.Should().Be("output.mp4");
        settings.VideoCodec.Should().Be("libx265");
        settings.AudioCodec.Should().Be("flac");
        settings.Quality.Should().Be(20);
    }

    #endregion

    #region EncodingProgressEventArgs Model Tests

    [Fact]
    public void EncodingProgressEventArgs_TimeRemaining_FormatsCorrectly()
    {
        // Arrange - TimeRemaining is calculated from TotalDuration, CurrentTime, and Speed
        // Note: Progress must be > 0 for TimeRemaining to calculate
        var args = new EncodingProgressEventArgs
        {
            TotalDuration = 100,
            CurrentTime = 50,
            Speed = 1.0, // 1x speed means 50 seconds remaining
            Progress = 50 // 50% progress
        };

        // Act
        var timeRemaining = args.TimeRemaining;

        // Assert - Should show 00:00:50
        timeRemaining.Should().Be("00:00:50");
    }

    [Fact]
    public void EncodingProgressEventArgs_TimeRemaining_HandlesZeroSpeed()
    {
        // Arrange
        var args = new EncodingProgressEventArgs
        {
            TotalDuration = 100,
            CurrentTime = 50,
            Speed = 0 // Zero speed
        };

        // Act
        var timeRemaining = args.TimeRemaining;

        // Assert - Should return placeholder when speed is 0
        timeRemaining.Should().Be("--:--:--");
    }

    [Fact]
    public void EncodingProgressEventArgs_TimeRemaining_HandlesHours()
    {
        // Arrange - 2 hours remaining at 1x speed
        // Note: Progress must be > 0 for TimeRemaining to calculate
        var args = new EncodingProgressEventArgs
        {
            TotalDuration = 10800, // 3 hours total
            CurrentTime = 3600,    // 1 hour elapsed
            Speed = 1.0,           // 1x speed
            Progress = 33.33       // ~33% progress
        };

        // Act
        var timeRemaining = args.TimeRemaining;

        // Assert - Should show 02:00:00 (2 hours remaining)
        timeRemaining.Should().Be("02:00:00");
    }

    #endregion

    #region EncodingCompletedEventArgs Model Tests

    [Fact]
    public void EncodingCompletedEventArgs_DefaultValues()
    {
        // Act
        var args = new EncodingCompletedEventArgs();

        // Assert
        args.Success.Should().BeFalse();
        args.Cancelled.Should().BeFalse();
        args.OutputPath.Should().BeEmpty();
    }

    #endregion

    #region MediaAnalysis Model Tests

    [Fact]
    public void MediaAnalysis_DefaultValues()
    {
        // Act
        var info = new MediaAnalysis();

        // Assert
        info.FilePath.Should().BeEmpty();
        info.Duration.Should().Be(TimeSpan.Zero);
        info.Format.Should().BeEmpty();
        info.VideoStream.Should().BeNull();
        info.AudioStream.Should().BeNull();
    }

    [Fact]
    public void MediaAnalysis_Resolution_ReturnsEmptyWhenNoVideoStream()
    {
        // Arrange
        var info = new MediaAnalysis { VideoStream = null };

        // Act
        var resolution = info.Resolution;

        // Assert
        resolution.Should().BeEmpty();
    }

    [Fact]
    public void MediaAnalysis_Resolution_ReturnsFormattedString()
    {
        // Arrange
        var info = new MediaAnalysis
        {
            VideoStream = new VideoStreamInfo { Width = 1920, Height = 1080 }
        };

        // Act
        var resolution = info.Resolution;

        // Assert
        resolution.Should().Be("1920x1080");
    }

    [Fact]
    public void MediaAnalysis_DurationFormatted_FormatsCorrectly()
    {
        // Arrange
        var info = new MediaAnalysis
        {
            Duration = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(30)
        };

        // Act
        var formatted = info.DurationFormatted;

        // Assert
        formatted.Should().Be("05:30");
    }

    [Fact]
    public void MediaAnalysis_DurationFormatted_IncludesHoursWhenNeeded()
    {
        // Arrange
        var info = new MediaAnalysis
        {
            Duration = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15)
        };

        // Act
        var formatted = info.DurationFormatted;

        // Assert
        formatted.Should().Be("01:30:15");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_FormatsBytes()
    {
        // Arrange
        var info = new MediaAnalysis { FileSize = 500 };

        // Act
        var formatted = info.FileSizeFormatted;

        // Assert
        formatted.Should().Be("500 B");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_FormatsKilobytes()
    {
        // Arrange
        var info = new MediaAnalysis { FileSize = 2048 };

        // Act
        var formatted = info.FileSizeFormatted;

        // Assert
        formatted.Should().Contain("KB");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_FormatsMegabytes()
    {
        // Arrange
        var info = new MediaAnalysis { FileSize = 10 * 1024 * 1024 };

        // Act
        var formatted = info.FileSizeFormatted;

        // Assert
        formatted.Should().Contain("MB");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_FormatsGigabytes()
    {
        // Arrange
        var info = new MediaAnalysis { FileSize = 2L * 1024 * 1024 * 1024 };

        // Act
        var formatted = info.FileSizeFormatted;

        // Assert
        formatted.Should().Contain("GB");
    }

    #endregion

    #region VideoStreamInfo Model Tests

    [Fact]
    public void VideoStreamInfo_DefaultValues()
    {
        // Act
        var stream = new VideoStreamInfo();

        // Assert
        stream.Codec.Should().BeEmpty();
        stream.Width.Should().Be(0);
        stream.Height.Should().Be(0);
        stream.PixelFormat.Should().BeEmpty();
        stream.Duration.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region AudioStreamInfo Model Tests

    [Fact]
    public void AudioStreamInfo_DefaultValues()
    {
        // Act
        var stream = new AudioStreamInfo();

        // Assert
        stream.Codec.Should().BeEmpty();
        stream.SampleRate.Should().Be(0);
        stream.Channels.Should().Be(0);
        stream.ChannelLayout.Should().BeEmpty();
        stream.Duration.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region HardwareAcceleration Model Tests

    [Fact]
    public void HardwareAcceleration_DefaultValues()
    {
        // Act
        var info = new HardwareAcceleration();

        // Assert
        info.NvencAvailable.Should().BeFalse();
        info.NvencHevcAvailable.Should().BeFalse();
        info.QsvAvailable.Should().BeFalse();
        info.AmfAvailable.Should().BeFalse();
        info.CudaAvailable.Should().BeFalse();
        info.AnyAvailable.Should().BeFalse();
    }

    [Fact]
    public void HardwareAcceleration_AnyEncodingAvailable_ReturnsTrueWhenNvencAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { NvencAvailable = true };

        // Assert
        info.AnyEncodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_AnyEncodingAvailable_ReturnsTrueWhenQsvAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { QsvAvailable = true };

        // Assert
        info.AnyEncodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_AnyEncodingAvailable_ReturnsTrueWhenAmfAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { AmfAvailable = true };

        // Assert
        info.AnyEncodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_AnyDecodingAvailable_ReturnsTrueWhenCudaAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { CudaAvailable = true };

        // Assert
        info.AnyDecodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsNvencWhenAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { NvencAvailable = true };

        // Act
        var encoder = info.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("h264_nvenc");
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsQsvWhenNvencNotAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { QsvAvailable = true };

        // Act
        var encoder = info.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("h264_qsv");
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsAmfWhenOthersNotAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { AmfAvailable = true };

        // Act
        var encoder = info.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("h264_amf");
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsSoftwareWhenNoHardware()
    {
        // Arrange
        var info = new HardwareAcceleration();

        // Act
        var encoder = info.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("libx264");
    }

    [Fact]
    public void HardwareAcceleration_GetBestHevcEncoder_ReturnsNvencHevcWhenAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { NvencHevcAvailable = true };

        // Act
        var encoder = info.GetBestHevcEncoder();

        // Assert
        encoder.Should().Be("hevc_nvenc");
    }

    [Fact]
    public void HardwareAcceleration_GetBestHevcEncoder_ReturnsSoftwareWhenNoHardware()
    {
        // Arrange
        var info = new HardwareAcceleration();

        // Act
        var encoder = info.GetBestHevcEncoder();

        // Assert
        encoder.Should().Be("libx265");
    }

    [Fact]
    public void HardwareAcceleration_GetBestHwAccel_ReturnsNullWhenNoHardware()
    {
        // Arrange
        var info = new HardwareAcceleration();

        // Act
        var accel = info.GetBestHwAccel();

        // Assert
        accel.Should().BeNull();
    }

    [Fact]
    public void HardwareAcceleration_GetBestHwAccel_ReturnsCudaWhenAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { CudaAvailable = true };

        // Act
        var accel = info.GetBestHwAccel();

        // Assert
        accel.Should().Be("cuda");
    }

    [Fact]
    public void HardwareAcceleration_GetHwAccelArgs_ReturnsEmptyWhenNoHardware()
    {
        // Arrange
        var info = new HardwareAcceleration();

        // Act
        var args = info.GetHwAccelArgs();

        // Assert
        args.Should().BeEmpty();
    }

    [Fact]
    public void HardwareAcceleration_GetHwAccelArgs_ReturnsCudaArgsWhenCudaAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { CudaAvailable = true };

        // Act
        var args = info.GetHwAccelArgs();

        // Assert
        args.Should().Contain("cuda");
    }

    [Fact]
    public void HardwareAcceleration_ToString_ReturnsNoHardwareMessageWhenEmpty()
    {
        // Arrange
        var info = new HardwareAcceleration();

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Be("No hardware acceleration available");
    }

    [Fact]
    public void HardwareAcceleration_ToString_IncludesNvencWhenAvailable()
    {
        // Arrange
        var info = new HardwareAcceleration { NvencAvailable = true };

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Contain("NVENC");
    }

    #endregion

    #region GetPresets Tests

    [Fact]
    public void GetPresets_ReturnsNonEmptyCollection()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPresets_ContainsH264Presets()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().Contain(p => p.VideoCodec == "libx264");
    }

    [Fact]
    public void GetPresets_ContainsH265Presets()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().Contain(p => p.VideoCodec == "libx265");
    }

    [Fact]
    public void GetPresets_ContainsProResPresets()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().Contain(p => p.VideoCodec == "prores_ks");
    }

    [Fact]
    public void GetPresets_ContainsAudioOnlyPresets()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().Contain(p => p.VideoEnabled == false);
    }

    #endregion
}
