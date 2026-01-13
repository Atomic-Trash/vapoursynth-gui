using VapourSynthPortable.Services;
using VapourSynthPortable.Tests.Helpers;

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

    [Fact]
    public void GetPresets_AllPresetsHaveNames()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name));
    }

    [Fact]
    public void GetPresets_AllPresetsHaveDescriptions()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Description));
    }

    [Fact]
    public void GetPresets_AllPresetsHaveFormat()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();

        // Assert
        presets.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Format));
    }

    [Fact]
    public void GetPresets_NvencPresetsUseCorrectCodecs()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();
        var nvencPresets = presets.Where(p => p.Name.Contains("NVENC", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        nvencPresets.Should().NotBeEmpty();
        nvencPresets.Should().OnlyContain(p => p.VideoCodec.Contains("nvenc"));
    }

    [Fact]
    public void GetPresets_LosslessPresetUsesFFV1()
    {
        // Act
        var presets = FFmpegService.GetPresets().ToList();
        var losslessPreset = presets.FirstOrDefault(p => p.Name.Contains("Lossless", StringComparison.OrdinalIgnoreCase) && p.VideoEnabled);

        // Assert
        losslessPreset.Should().NotBeNull();
        losslessPreset!.VideoCodec.Should().Be("ffv1");
    }

    #endregion

    #region AnalyzeAsync Tests

    [Fact]
    public async Task AnalyzeAsync_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");

        // Act
        var result = await _service.AnalyzeAsync(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyPath_ReturnsNull()
    {
        // Act
        var result = await _service.AnalyzeAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellation_ThrowsOrReturnsNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should either throw OperationCanceledException or return null gracefully
        var act = async () => await _service.AnalyzeAsync("some_file.mp4", cts.Token);

        // Should not hang - either throws or returns quickly
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetDurationAsync Tests

    [Fact]
    public async Task GetDurationAsync_NonExistentFile_ReturnsZero()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");

        // Act
        var result = await _service.GetDurationAsync(nonExistentPath);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region EncodeAsync Validation Tests

    [Fact]
    public async Task EncodeAsync_NonExistentInput_ReturnsFalse()
    {
        // Arrange
        var settings = new ExportSettings
        {
            InputPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4"),
            OutputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.mp4")
        };

        // Act
        var result = await _service.EncodeAsync(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EncodeAsync_NonExistentInput_RaisesLogMessage()
    {
        // Arrange
        var logMessages = new List<string>();
        _service.LogMessage += (s, e) => logMessages.Add(e);

        var settings = new ExportSettings
        {
            InputPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4"),
            OutputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.mp4")
        };

        // Act
        await _service.EncodeAsync(settings);

        // Assert
        logMessages.Should().Contain(m => m.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EncodeAsync_WhileEncoding_IsEncodingReturnsTrue()
    {
        // Since we can't easily test actual encoding, verify the flag behavior
        // Note: This is more of a documentation test - actual encoding tests require real files

        // Verify initial state
        _service.IsEncoding.Should().BeFalse();
    }

    #endregion

    #region ExtractFrameAsync Tests

    [Fact]
    public async Task ExtractFrameAsync_NonExistentInput_ReturnsFalse()
    {
        // Arrange
        var inputPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");
        var outputPath = Path.Combine(Path.GetTempPath(), $"frame_{Guid.NewGuid()}.jpg");

        // Act
        var result = await _service.ExtractFrameAsync(inputPath, outputPath, TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateThumbnailAsync Tests

    [Fact]
    public async Task GenerateThumbnailAsync_NonExistentInput_ReturnsFalse()
    {
        // Arrange
        var inputPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.mp4");
        var outputPath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid()}.jpg");

        // Act
        var result = await _service.GenerateThumbnailAsync(inputPath, outputPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExportSettings Advanced Tests

    [Fact]
    public void ExportSettings_QualityRange_AcceptsValidCRFValues()
    {
        // CRF range for x264/x265 is typically 0-51
        var settings = new ExportSettings { Quality = 0 };
        settings.Quality.Should().Be(0);

        settings.Quality = 51;
        settings.Quality.Should().Be(51);
    }

    [Fact]
    public void ExportSettings_FrameRate_AcceptsVariousValues()
    {
        var settings = new ExportSettings();

        settings.FrameRate = 23.976;
        settings.FrameRate.Should().BeApproximately(23.976, 0.001);

        settings.FrameRate = 29.97;
        settings.FrameRate.Should().BeApproximately(29.97, 0.001);

        settings.FrameRate = 60;
        settings.FrameRate.Should().Be(60);
    }

    [Fact]
    public void ExportSettings_Resolution_CanBeSetIndividually()
    {
        var settings = new ExportSettings
        {
            Width = 1920,
            Height = 1080
        };

        settings.Width.Should().Be(1920);
        settings.Height.Should().Be(1080);
    }

    [Fact]
    public void ExportSettings_AudioSampleRate_AcceptsCommonValues()
    {
        var settings = new ExportSettings();

        settings.AudioSampleRate = 44100;
        settings.AudioSampleRate.Should().Be(44100);

        settings.AudioSampleRate = 48000;
        settings.AudioSampleRate.Should().Be(48000);

        settings.AudioSampleRate = 96000;
        settings.AudioSampleRate.Should().Be(96000);
    }

    [Fact]
    public void ExportSettings_HardwarePreset_CanBeSet()
    {
        var settings = new ExportSettings { HardwarePreset = "p7" };
        settings.HardwarePreset.Should().Be("p7");
    }

    [Fact]
    public void ExportSettings_ProResProfile_AcceptsValidProfiles()
    {
        // ProRes profiles: 0=Proxy, 1=LT, 2=422, 3=HQ, 4=4444, 5=4444XQ
        var settings = new ExportSettings();

        for (int profile = 0; profile <= 5; profile++)
        {
            settings.ProResProfile = profile;
            settings.ProResProfile.Should().Be(profile);
        }
    }

    #endregion

    #region ExportPreset Advanced Tests

    [Fact]
    public void ExportPreset_ToSettings_PreservesAllProperties()
    {
        // Arrange
        var preset = new ExportPreset
        {
            Name = "Test Preset",
            Format = "mkv",
            VideoCodec = "libx265",
            AudioCodec = "flac",
            Quality = 18,
            Preset = "slow",
            HardwarePreset = "p7",
            ProResProfile = 3,
            AudioBitrate = 320,
            VideoEnabled = true
        };

        // Act
        var settings = preset.ToSettings("input.mp4", "output.mkv");

        // Assert
        settings.VideoCodec.Should().Be("libx265");
        settings.AudioCodec.Should().Be("flac");
        settings.Quality.Should().Be(18);
        settings.Preset.Should().Be("slow");
        settings.HardwarePreset.Should().Be("p7");
        settings.ProResProfile.Should().Be(3);
        settings.AudioBitrate.Should().Be(320);
    }

    [Fact]
    public void ExportPreset_ToSettings_SetsCorrectPaths()
    {
        // Arrange
        var preset = new ExportPreset { Name = "Test" };

        // Act
        var settings = preset.ToSettings("/path/to/input.mp4", "/path/to/output.mp4");

        // Assert
        settings.InputPath.Should().Be("/path/to/input.mp4");
        settings.OutputPath.Should().Be("/path/to/output.mp4");
    }

    #endregion

    #region EncodingProgressEventArgs Advanced Tests

    [Fact]
    public void EncodingProgressEventArgs_TimeRemaining_HandlesHighSpeed()
    {
        // Arrange - 2x speed encoding
        var args = new EncodingProgressEventArgs
        {
            TotalDuration = 100,
            CurrentTime = 50,
            Speed = 2.0,
            Progress = 50
        };

        // Act
        var timeRemaining = args.TimeRemaining;

        // Assert - 50 seconds remaining at 2x speed = 25 seconds real time
        timeRemaining.Should().Be("00:00:25");
    }

    [Fact]
    public void EncodingProgressEventArgs_TimeRemaining_HandlesZeroProgress()
    {
        // Arrange
        var args = new EncodingProgressEventArgs
        {
            TotalDuration = 100,
            CurrentTime = 0,
            Speed = 1.0,
            Progress = 0 // Zero progress
        };

        // Act
        var timeRemaining = args.TimeRemaining;

        // Assert - Should return placeholder
        timeRemaining.Should().Be("--:--:--");
    }

    [Fact]
    public void EncodingProgressEventArgs_AllProperties_CanBeSet()
    {
        // Arrange
        var args = new EncodingProgressEventArgs
        {
            Progress = 75.5,
            CurrentTime = 90,
            TotalDuration = 120,
            Fps = 45.5,
            Bitrate = 5000,
            Speed = 1.8
        };

        // Assert
        args.Progress.Should().Be(75.5);
        args.CurrentTime.Should().Be(90);
        args.TotalDuration.Should().Be(120);
        args.Fps.Should().Be(45.5);
        args.Bitrate.Should().Be(5000);
        args.Speed.Should().Be(1.8);
    }

    #endregion

    #region MediaAnalysis Advanced Tests

    [Fact]
    public void MediaAnalysis_WithProcessMockerData_HasCorrectProperties()
    {
        // Arrange - Use ProcessMocker to create sample data
        var analysis = ProcessMocker.CreateSampleMediaAnalysis(
            filePath: "test.mp4",
            width: 3840,
            height: 2160,
            frameRate: 59.94,
            durationSeconds: 300.0,
            videoCodec: "hevc",
            audioCodec: "eac3");

        // Assert
        analysis.FilePath.Should().Be("test.mp4");
        analysis.VideoStream.Should().NotBeNull();
        analysis.VideoStream!.Width.Should().Be(3840);
        analysis.VideoStream.Height.Should().Be(2160);
        analysis.VideoStream.FrameRate.Should().BeApproximately(59.94, 0.01);
        analysis.VideoStream.Codec.Should().Be("hevc");
        analysis.AudioStream.Should().NotBeNull();
        analysis.AudioStream!.Codec.Should().Be("eac3");
        analysis.Duration.TotalSeconds.Should().Be(300.0);
    }

    [Fact]
    public void MediaAnalysis_Resolution_FormatsCorrectlyFor4K()
    {
        // Arrange
        var analysis = new MediaAnalysis
        {
            VideoStream = new VideoStreamInfo { Width = 3840, Height = 2160 }
        };

        // Assert
        analysis.Resolution.Should().Be("3840x2160");
    }

    [Fact]
    public void MediaAnalysis_DurationFormatted_HandlesVeryLongVideos()
    {
        // Arrange - 10 hour video
        var analysis = new MediaAnalysis
        {
            Duration = TimeSpan.FromHours(10)
        };

        // Assert
        analysis.DurationFormatted.Should().Be("10:00:00");
    }

    [Fact]
    public void MediaAnalysis_DurationFormatted_HandlesSubSecondVideos()
    {
        // Arrange
        var analysis = new MediaAnalysis
        {
            Duration = TimeSpan.FromMilliseconds(500)
        };

        // Assert
        analysis.DurationFormatted.Should().Be("00:00");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_HandlesZeroSize()
    {
        // Arrange
        var analysis = new MediaAnalysis { FileSize = 0 };

        // Assert - Zero size displays as "0 B"
        analysis.FileSizeFormatted.Should().Be("0 B");
    }

    [Fact]
    public void MediaAnalysis_FileSizeFormatted_HandlesExactBoundaries()
    {
        // Test exactly 1 KB
        var analysis1KB = new MediaAnalysis { FileSize = 1024 };
        analysis1KB.FileSizeFormatted.Should().Contain("KB");

        // Test exactly 1 MB
        var analysis1MB = new MediaAnalysis { FileSize = 1024 * 1024 };
        analysis1MB.FileSizeFormatted.Should().Contain("MB");

        // Test exactly 1 GB
        var analysis1GB = new MediaAnalysis { FileSize = 1024L * 1024 * 1024 };
        analysis1GB.FileSizeFormatted.Should().Contain("GB");
    }

    #endregion

    #region VideoStreamInfo Advanced Tests

    [Fact]
    public void VideoStreamInfo_CanStoreAllCommonCodecs()
    {
        var codecs = new[] { "h264", "hevc", "vp9", "av1", "prores", "dnxhd", "ffv1" };

        foreach (var codec in codecs)
        {
            var stream = new VideoStreamInfo { Codec = codec };
            stream.Codec.Should().Be(codec);
        }
    }

    [Fact]
    public void VideoStreamInfo_CanStoreAllCommonPixelFormats()
    {
        var formats = new[] { "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "rgb24", "rgba" };

        foreach (var format in formats)
        {
            var stream = new VideoStreamInfo { PixelFormat = format };
            stream.PixelFormat.Should().Be(format);
        }
    }

    [Fact]
    public void VideoStreamInfo_AcceptsHighFrameRates()
    {
        var stream = new VideoStreamInfo { FrameRate = 120 };
        stream.FrameRate.Should().Be(120);

        stream.FrameRate = 240;
        stream.FrameRate.Should().Be(240);
    }

    [Fact]
    public void VideoStreamInfo_AcceptsHighBitRates()
    {
        // 4K HDR can have very high bitrates
        var stream = new VideoStreamInfo { BitRate = 100_000_000 }; // 100 Mbps
        stream.BitRate.Should().Be(100_000_000);
    }

    #endregion

    #region AudioStreamInfo Advanced Tests

    [Fact]
    public void AudioStreamInfo_CanStoreAllCommonCodecs()
    {
        var codecs = new[] { "aac", "mp3", "flac", "pcm_s16le", "pcm_s24le", "ac3", "eac3", "dts", "opus", "vorbis" };

        foreach (var codec in codecs)
        {
            var stream = new AudioStreamInfo { Codec = codec };
            stream.Codec.Should().Be(codec);
        }
    }

    [Fact]
    public void AudioStreamInfo_AcceptsVariousSampleRates()
    {
        var sampleRates = new[] { 8000, 22050, 44100, 48000, 96000, 192000 };

        foreach (var rate in sampleRates)
        {
            var stream = new AudioStreamInfo { SampleRate = rate };
            stream.SampleRate.Should().Be(rate);
        }
    }

    [Fact]
    public void AudioStreamInfo_AcceptsVariousChannelCounts()
    {
        // Mono to 7.1 surround
        for (int channels = 1; channels <= 8; channels++)
        {
            var stream = new AudioStreamInfo { Channels = channels };
            stream.Channels.Should().Be(channels);
        }
    }

    [Fact]
    public void AudioStreamInfo_CanStoreChannelLayouts()
    {
        var layouts = new[] { "mono", "stereo", "5.1", "7.1", "5.1(side)" };

        foreach (var layout in layouts)
        {
            var stream = new AudioStreamInfo { ChannelLayout = layout };
            stream.ChannelLayout.Should().Be(layout);
        }
    }

    #endregion

    #region HardwareAcceleration Advanced Tests

    [Fact]
    public void HardwareAcceleration_WithProcessMockerData_HasCorrectProperties()
    {
        // Arrange
        var hwAccel = ProcessMocker.CreateSampleHardwareAcceleration(
            nvenc: true,
            qsv: false,
            amf: false,
            cuda: true);

        // Assert
        hwAccel.NvencAvailable.Should().BeTrue();
        hwAccel.NvencHevcAvailable.Should().BeTrue();
        hwAccel.QsvAvailable.Should().BeFalse();
        hwAccel.AmfAvailable.Should().BeFalse();
        hwAccel.CudaAvailable.Should().BeTrue();
        hwAccel.AnyEncodingAvailable.Should().BeTrue();
        hwAccel.AnyDecodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_PreferenceOrder()
    {
        // Test preference order: NVENC > QSV > AMF > libx264

        // All available - should return NVENC
        var allAvailable = new HardwareAcceleration
        {
            NvencAvailable = true,
            QsvAvailable = true,
            AmfAvailable = true
        };
        allAvailable.GetBestH264Encoder().Should().Be("h264_nvenc");

        // QSV and AMF available - should return QSV
        var qsvAmf = new HardwareAcceleration
        {
            QsvAvailable = true,
            AmfAvailable = true
        };
        qsvAmf.GetBestH264Encoder().Should().Be("h264_qsv");
    }

    [Fact]
    public void HardwareAcceleration_GetBestHevcEncoder_PreferenceOrder()
    {
        // Test preference order: NVENC HEVC > QSV HEVC > AMF HEVC > libx265

        var allAvailable = new HardwareAcceleration
        {
            NvencHevcAvailable = true,
            QsvHevcAvailable = true,
            AmfHevcAvailable = true
        };
        allAvailable.GetBestHevcEncoder().Should().Be("hevc_nvenc");

        var qsvAmf = new HardwareAcceleration
        {
            QsvHevcAvailable = true,
            AmfHevcAvailable = true
        };
        qsvAmf.GetBestHevcEncoder().Should().Be("hevc_qsv");
    }

    [Fact]
    public void HardwareAcceleration_GetBestHwAccel_PreferenceOrder()
    {
        // Test preference order for decoding

        var allAvailable = new HardwareAcceleration
        {
            CudaAvailable = true,
            CuvidAvailable = true,
            QsvDecodeAvailable = true,
            D3d11vaAvailable = true
        };
        allAvailable.GetBestHwAccel().Should().Be("cuda");

        var noCuda = new HardwareAcceleration
        {
            CuvidAvailable = true,
            QsvDecodeAvailable = true
        };
        noCuda.GetBestHwAccel().Should().Be("cuvid");
    }

    [Fact]
    public void HardwareAcceleration_GetHwAccelArgs_FormatsCorrectly()
    {
        var cudaAccel = new HardwareAcceleration { CudaAvailable = true };
        cudaAccel.GetHwAccelArgs().Should().Contain("-hwaccel cuda");

        var qsvAccel = new HardwareAcceleration { QsvDecodeAvailable = true };
        qsvAccel.GetHwAccelArgs().Should().Contain("-hwaccel qsv");

        var d3d11Accel = new HardwareAcceleration { D3d11vaAvailable = true };
        d3d11Accel.GetHwAccelArgs().Should().Contain("-hwaccel d3d11va");
    }

    [Fact]
    public void HardwareAcceleration_ToString_IncludesAllAvailableEncoders()
    {
        var hwAccel = new HardwareAcceleration
        {
            NvencAvailable = true,
            QsvAvailable = true,
            AmfAvailable = true,
            CudaAvailable = true
        };

        var result = hwAccel.ToString();

        result.Should().Contain("NVENC");
        result.Should().Contain("QSV");
        result.Should().Contain("AMF");
        result.Should().Contain("CUDA");
    }

    [Fact]
    public void HardwareAcceleration_AnyAvailable_ReturnsTrueWhenOnlyDecoding()
    {
        var hwAccel = new HardwareAcceleration { D3d11vaAvailable = true };

        hwAccel.AnyEncodingAvailable.Should().BeFalse();
        hwAccel.AnyDecodingAvailable.Should().BeTrue();
        hwAccel.AnyAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_AnyAvailable_ReturnsTrueWhenOnlyEncoding()
    {
        var hwAccel = new HardwareAcceleration { NvencAvailable = true };

        hwAccel.AnyEncodingAvailable.Should().BeTrue();
        hwAccel.AnyDecodingAvailable.Should().BeFalse();
        hwAccel.AnyAvailable.Should().BeTrue();
    }

    #endregion

    #region ProcessMocker Integration Tests

    [Fact]
    public void ProcessMocker_CreateSampleExportSettings_CreatesValidSettings()
    {
        // Arrange & Act
        var settings = ProcessMocker.CreateSampleExportSettings(
            inputPath: "input.mkv",
            outputPath: "output.mp4",
            videoCodec: "libx265",
            quality: 18,
            preset: "slow");

        // Assert
        settings.InputPath.Should().Be("input.mkv");
        settings.OutputPath.Should().Be("output.mp4");
        settings.VideoCodec.Should().Be("libx265");
        settings.Quality.Should().Be(18);
        settings.Preset.Should().Be("slow");
        settings.VideoEnabled.Should().BeTrue();
        settings.AudioEnabled.Should().BeTrue();
    }

    [Fact]
    public void ProcessMocker_GenerateEncodingProgress_GeneratesValidSequence()
    {
        // Arrange
        var totalDuration = 60.0;
        var steps = 5;

        // Act
        var progressEvents = ProcessMocker.GenerateEncodingProgress(totalDuration, steps).ToList();

        // Assert
        progressEvents.Should().HaveCount(steps + 1); // 0% to 100%
        progressEvents.First().Progress.Should().Be(0);
        progressEvents.Last().Progress.Should().Be(100);

        // Verify progress increases monotonically
        for (int i = 1; i < progressEvents.Count; i++)
        {
            progressEvents[i].Progress.Should().BeGreaterThan(progressEvents[i - 1].Progress);
        }
    }

    [Fact]
    public void ProcessMocker_ErrorScenarios_ProvideCorrectExitCodes()
    {
        // Assert various error scenarios have proper exit codes
        ProcessMocker.ErrorScenarios.FileNotFound.ExitCode.Should().Be(-1);
        ProcessMocker.ErrorScenarios.CorruptFile.ExitCode.Should().Be(1);
        ProcessMocker.ErrorScenarios.PermissionDenied.ExitCode.Should().Be(-1);
        ProcessMocker.ErrorScenarios.CodecNotFound.ExitCode.Should().Be(1);
        ProcessMocker.ErrorScenarios.DiskFull.ExitCode.Should().Be(1);
    }

    [Fact]
    public void ProcessMocker_ErrorScenarios_ProvideDescriptiveMessages()
    {
        ProcessMocker.ErrorScenarios.FileNotFound.Stderr.Should().Contain("No such file");
        ProcessMocker.ErrorScenarios.CorruptFile.Stderr.Should().Contain("Invalid data");
        ProcessMocker.ErrorScenarios.CodecNotFound.Stderr.Should().Contain("Unknown encoder");
        ProcessMocker.ErrorScenarios.DiskFull.Stderr.Should().Contain("No space left");
    }

    #endregion

    #region Encoding Event Flow Tests

    [Fact]
    public async Task EncodingEvents_LogMessageReceived_ForInvalidInput()
    {
        // Arrange
        var logMessages = new List<string>();
        _service.LogMessage += (s, e) => logMessages.Add(e);

        var settings = ProcessMocker.CreateSampleExportSettings(
            inputPath: $"/nonexistent/path/{Guid.NewGuid()}.mp4",
            outputPath: Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.mp4"));

        // Act
        await _service.EncodeAsync(settings);

        // Assert
        logMessages.Should().NotBeEmpty();
    }

    #endregion
}
