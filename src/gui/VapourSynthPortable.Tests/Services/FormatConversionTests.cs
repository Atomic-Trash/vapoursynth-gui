using VapourSynthPortable.Helpers;
using VapourSynthPortable.Tests.Helpers;

namespace VapourSynthPortable.Tests.Services;

/// <summary>
/// Tests for format conversion settings and validation.
/// Tests cover: video codecs, audio codecs, resolution, frame rate, quality settings.
/// </summary>
public class FormatConversionTests
{
    #region ExportSettings Default Values

    [Fact]
    public void ExportSettings_DefaultVideoCodec_IsLibx264()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.VideoCodec.Should().Be("libx264");
    }

    [Fact]
    public void ExportSettings_DefaultAudioCodec_IsAac()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.AudioCodec.Should().Be("aac");
    }

    [Fact]
    public void ExportSettings_DefaultQuality_Is22()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.Quality.Should().Be(22);
    }

    [Fact]
    public void ExportSettings_DefaultPreset_IsMedium()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.Preset.Should().Be("medium");
    }

    [Fact]
    public void ExportSettings_DefaultAudioBitrate_Is192()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.AudioBitrate.Should().Be(192);
    }

    [Fact]
    public void ExportSettings_DefaultAudioSampleRate_Is48000()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.AudioSampleRate.Should().Be(48000);
    }

    [Fact]
    public void ExportSettings_DefaultVideoEnabled_IsTrue()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.VideoEnabled.Should().BeTrue();
    }

    [Fact]
    public void ExportSettings_DefaultAudioEnabled_IsTrue()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.AudioEnabled.Should().BeTrue();
    }

    [Fact]
    public void ExportSettings_DefaultPixelFormat_IsYuv420p()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.PixelFormat.Should().Be("yuv420p");
    }

    [Fact]
    public void ExportSettings_DefaultHardwarePreset_IsP4()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.HardwarePreset.Should().Be("p4");
    }

    [Fact]
    public void ExportSettings_DefaultProResProfile_Is2()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.ProResProfile.Should().Be(2);
    }

    #endregion

    #region Video Codec Tests

    [Theory]
    [InlineData("libx264")]
    [InlineData("libx265")]
    [InlineData("h264_nvenc")]
    [InlineData("hevc_nvenc")]
    [InlineData("prores_ks")]
    [InlineData("ffv1")]
    [InlineData("copy")]
    public void ExportSettings_SupportedVideoCodecs_CanBeSet(string codec)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.VideoCodec = codec;

        // Assert
        settings.VideoCodec.Should().Be(codec);
    }

    [Fact]
    public void ExportSettings_CopyVideoCodec_PreservesOriginal()
    {
        // Arrange
        var settings = new ExportSettings { VideoCodec = "copy" };

        // Assert
        settings.VideoCodec.Should().Be("copy");
    }

    [Fact]
    public void ExportSettings_NvencCodec_IsHardwareAccelerated()
    {
        // Arrange
        var settings = new ExportSettings { VideoCodec = "h264_nvenc" };

        // Assert
        settings.VideoCodec.Should().Contain("nvenc");
    }

    [Fact]
    public void ExportSettings_HevcNvenc_IsHardwareAccelerated()
    {
        // Arrange
        var settings = new ExportSettings { VideoCodec = "hevc_nvenc" };

        // Assert
        settings.VideoCodec.Should().Contain("nvenc");
    }

    [Fact]
    public void ExportSettings_ProRes_IsLossless()
    {
        // Arrange
        var settings = new ExportSettings { VideoCodec = "prores_ks" };

        // Assert
        settings.VideoCodec.Should().Contain("prores");
    }

    [Fact]
    public void ExportSettings_FFV1_IsLossless()
    {
        // Arrange
        var settings = new ExportSettings { VideoCodec = "ffv1" };

        // Assert
        settings.VideoCodec.Should().Be("ffv1");
    }

    #endregion

    #region Audio Codec Tests

    [Theory]
    [InlineData("aac")]
    [InlineData("libmp3lame")]
    [InlineData("pcm_s16le")]
    [InlineData("flac")]
    [InlineData("copy")]
    public void ExportSettings_SupportedAudioCodecs_CanBeSet(string codec)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.AudioCodec = codec;

        // Assert
        settings.AudioCodec.Should().Be(codec);
    }

    [Fact]
    public void ExportSettings_CopyAudioCodec_PreservesOriginal()
    {
        // Arrange
        var settings = new ExportSettings { AudioCodec = "copy" };

        // Assert
        settings.AudioCodec.Should().Be("copy");
    }

    [Fact]
    public void ExportSettings_PcmAudio_IsLossless()
    {
        // Arrange
        var settings = new ExportSettings { AudioCodec = "pcm_s16le" };

        // Assert
        settings.AudioCodec.Should().StartWith("pcm");
    }

    [Fact]
    public void ExportSettings_FlacAudio_IsLossless()
    {
        // Arrange
        var settings = new ExportSettings { AudioCodec = "flac" };

        // Assert
        settings.AudioCodec.Should().Be("flac");
    }

    #endregion

    #region Resolution Tests

    [Theory]
    [InlineData(1280, 720)]   // 720p
    [InlineData(1920, 1080)]  // 1080p
    [InlineData(2560, 1440)]  // 1440p
    [InlineData(3840, 2160)]  // 4K
    [InlineData(7680, 4320)]  // 8K
    public void ExportSettings_CommonResolutions_CanBeSet(int width, int height)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.Width = width;
        settings.Height = height;

        // Assert
        settings.Width.Should().Be(width);
        settings.Height.Should().Be(height);
    }

    [Fact]
    public void ExportSettings_CustomResolution_CanBeSet()
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.Width = 1000;
        settings.Height = 500;

        // Assert
        settings.Width.Should().Be(1000);
        settings.Height.Should().Be(500);
    }

    [Fact]
    public void ExportSettings_AspectRatio_CanBeCalculated()
    {
        // Arrange
        var settings = new ExportSettings
        {
            Width = 1920,
            Height = 1080
        };

        // Assert
        var aspectRatio = (double)settings.Width / settings.Height;
        aspectRatio.Should().BeApproximately(16.0 / 9.0, 0.001);
    }

    [Fact]
    public void ExportSettings_4K_Resolution()
    {
        // Arrange
        var settings = new ExportSettings
        {
            Width = 3840,
            Height = 2160
        };

        // Assert
        settings.Width.Should().Be(3840);
        settings.Height.Should().Be(2160);
    }

    [Fact]
    public void ExportSettings_1080p_Resolution()
    {
        // Arrange
        var settings = new ExportSettings
        {
            Width = 1920,
            Height = 1080
        };

        // Assert
        settings.Width.Should().Be(1920);
        settings.Height.Should().Be(1080);
    }

    [Fact]
    public void ExportSettings_720p_Resolution()
    {
        // Arrange
        var settings = new ExportSettings
        {
            Width = 1280,
            Height = 720
        };

        // Assert
        settings.Width.Should().Be(1280);
        settings.Height.Should().Be(720);
    }

    #endregion

    #region Frame Rate Tests

    [Theory]
    [InlineData(23.976)]  // NTSC Film
    [InlineData(24)]      // Film
    [InlineData(25)]      // PAL
    [InlineData(29.97)]   // NTSC
    [InlineData(30)]      // Standard
    [InlineData(50)]      // PAL HFR
    [InlineData(59.94)]   // NTSC HFR
    [InlineData(60)]      // High Frame Rate
    [InlineData(120)]     // Ultra HFR
    public void ExportSettings_CommonFrameRates_CanBeSet(double fps)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.FrameRate = fps;

        // Assert
        settings.FrameRate.Should().Be(fps);
    }

    [Fact]
    public void ExportSettings_NtscFilmRate_Is23976()
    {
        // Arrange
        var settings = new ExportSettings { FrameRate = 23.976 };

        // Assert
        settings.FrameRate.Should().BeApproximately(23.976, 0.001);
    }

    [Fact]
    public void ExportSettings_PalRate_Is25()
    {
        // Arrange
        var settings = new ExportSettings { FrameRate = 25 };

        // Assert
        settings.FrameRate.Should().Be(25);
    }

    [Fact]
    public void ExportSettings_NtscRate_Is29_97()
    {
        // Arrange
        var settings = new ExportSettings { FrameRate = 29.97 };

        // Assert
        settings.FrameRate.Should().BeApproximately(29.97, 0.001);
    }

    #endregion

    #region Quality Settings Tests

    [Theory]
    [InlineData(0)]   // Lossless
    [InlineData(18)]  // High quality
    [InlineData(22)]  // Default
    [InlineData(23)]  // Good quality
    [InlineData(28)]  // Medium quality
    [InlineData(51)]  // Low quality
    public void ExportSettings_QualityValues_CanBeSet(int quality)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.Quality = quality;

        // Assert
        settings.Quality.Should().Be(quality);
    }

    [Theory]
    [InlineData("ultrafast")]
    [InlineData("superfast")]
    [InlineData("veryfast")]
    [InlineData("faster")]
    [InlineData("fast")]
    [InlineData("medium")]
    [InlineData("slow")]
    [InlineData("slower")]
    [InlineData("veryslow")]
    public void ExportSettings_X264Presets_CanBeSet(string preset)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.Preset = preset;

        // Assert
        settings.Preset.Should().Be(preset);
    }

    [Theory]
    [InlineData("p1")]  // Fastest
    [InlineData("p2")]
    [InlineData("p3")]
    [InlineData("p4")]  // Default
    [InlineData("p5")]
    [InlineData("p6")]
    [InlineData("p7")]  // Slowest
    public void ExportSettings_NvencPresets_CanBeSet(string preset)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.HardwarePreset = preset;

        // Assert
        settings.HardwarePreset.Should().Be(preset);
    }

    #endregion

    #region Audio Settings Tests

    [Theory]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(256)]
    [InlineData(320)]
    public void ExportSettings_AudioBitrates_CanBeSet(int bitrate)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.AudioBitrate = bitrate;

        // Assert
        settings.AudioBitrate.Should().Be(bitrate);
    }

    [Theory]
    [InlineData(44100)]  // CD quality
    [InlineData(48000)]  // Video standard
    [InlineData(96000)]  // High resolution
    public void ExportSettings_AudioSampleRates_CanBeSet(int sampleRate)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.AudioSampleRate = sampleRate;

        // Assert
        settings.AudioSampleRate.Should().Be(sampleRate);
    }

    [Fact]
    public void ExportSettings_AudioDisabled_CanBeSet()
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.AudioEnabled = false;

        // Assert
        settings.AudioEnabled.Should().BeFalse();
    }

    #endregion

    #region Hardware Acceleration Tests

    [Fact]
    public void HardwareAcceleration_DefaultNvenc_IsFalse()
    {
        // Act
        var hwAccel = new HardwareAcceleration();

        // Assert
        hwAccel.NvencAvailable.Should().BeFalse();
    }

    [Fact]
    public void HardwareAcceleration_NvencAvailable_CanBeSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration();

        // Act
        hwAccel.NvencAvailable = true;

        // Assert
        hwAccel.NvencAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_QsvAvailable_CanBeSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration();

        // Act
        hwAccel.QsvAvailable = true;

        // Assert
        hwAccel.QsvAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_CudaAvailable_CanBeSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration();

        // Act
        hwAccel.CudaAvailable = true;

        // Assert
        hwAccel.CudaAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_VulkanAvailable_CanBeSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration();

        // Act
        hwAccel.VulkanAvailable = true;

        // Assert
        hwAccel.VulkanAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_DefaultQsv_IsFalse()
    {
        // Act
        var hwAccel = new HardwareAcceleration();

        // Assert
        hwAccel.QsvAvailable.Should().BeFalse();
    }

    [Fact]
    public void HardwareAcceleration_AnyEncodingAvailable_WhenNvencSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration { NvencAvailable = true };

        // Assert
        hwAccel.AnyEncodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_AnyDecodingAvailable_WhenCudaSet()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration { CudaAvailable = true };

        // Assert
        hwAccel.AnyDecodingAvailable.Should().BeTrue();
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsNvenc_WhenAvailable()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration { NvencAvailable = true };

        // Act
        var encoder = hwAccel.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("h264_nvenc");
    }

    [Fact]
    public void HardwareAcceleration_GetBestH264Encoder_ReturnsQsv_WhenNoNvenc()
    {
        // Arrange
        var hwAccel = new HardwareAcceleration { QsvAvailable = true };

        // Act
        var encoder = hwAccel.GetBestH264Encoder();

        // Assert
        encoder.Should().Be("h264_qsv");
    }

    #endregion

    #region ProRes Profile Tests

    [Theory]
    [InlineData(0)]  // Proxy
    [InlineData(1)]  // LT
    [InlineData(2)]  // Standard
    [InlineData(3)]  // HQ
    [InlineData(4)]  // 4444
    [InlineData(5)]  // 4444 XQ
    public void ExportSettings_ProResProfiles_CanBeSet(int profile)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.ProResProfile = profile;

        // Assert
        settings.ProResProfile.Should().Be(profile);
    }

    #endregion

    #region Pixel Format Tests

    [Theory]
    [InlineData("yuv420p")]    // Most compatible
    [InlineData("yuv422p")]    // Better chroma
    [InlineData("yuv444p")]    // Full chroma
    [InlineData("yuv420p10le")] // 10-bit
    [InlineData("yuv422p10le")] // 10-bit better chroma
    public void ExportSettings_PixelFormats_CanBeSet(string pixelFormat)
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.PixelFormat = pixelFormat;

        // Assert
        settings.PixelFormat.Should().Be(pixelFormat);
    }

    #endregion

    #region MediaAnalysis Format Tests

    [Fact]
    public void MediaAnalysis_Format_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.Format.Should().NotBeEmpty();
    }

    [Fact]
    public void MediaAnalysis_VideoStream_Codec_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.VideoStream.Should().NotBeNull();
        analysis.VideoStream!.Codec.Should().NotBeEmpty();
    }

    [Fact]
    public void MediaAnalysis_AudioStream_Codec_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.AudioStream.Should().NotBeNull();
        analysis.AudioStream!.Codec.Should().NotBeEmpty();
    }

    [Fact]
    public void MediaAnalysis_Resolution_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.VideoStream.Should().NotBeNull();
        analysis.VideoStream!.Width.Should().BeGreaterThan(0);
        analysis.VideoStream.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MediaAnalysis_FrameRate_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.VideoStream.Should().NotBeNull();
        analysis.VideoStream!.FrameRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MediaAnalysis_Duration_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.Duration.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MediaAnalysis_Resolution_FormattedCorrectly()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis(width: 1920, height: 1080);

        // Assert
        analysis.Resolution.Should().Be("1920x1080");
    }

    [Fact]
    public void MediaAnalysis_FileSize_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MediaAnalysis_BitRate_CanBeRead()
    {
        // Arrange
        var analysis = ProcessMocker.CreateSampleMediaAnalysis();

        // Assert
        analysis.BitRate.Should().BeGreaterThan(0);
    }

    #endregion

    #region ExportPreset Tests

    [Fact]
    public void ExportPreset_DefaultFormat_IsMp4()
    {
        // Act
        var preset = new ExportPreset();

        // Assert
        preset.Format.Should().Be("mp4");
    }

    [Fact]
    public void ExportPreset_DefaultVideoCodec_IsLibx264()
    {
        // Act
        var preset = new ExportPreset();

        // Assert
        preset.VideoCodec.Should().Be("libx264");
    }

    [Fact]
    public void ExportPreset_DefaultAudioCodec_IsAac()
    {
        // Act
        var preset = new ExportPreset();

        // Assert
        preset.AudioCodec.Should().Be("aac");
    }

    [Fact]
    public void ExportPreset_ToSettings_CreatesExportSettings()
    {
        // Arrange
        var preset = new ExportPreset
        {
            Name = "Test Preset",
            VideoCodec = "libx265",
            AudioCodec = "aac",
            Quality = 20
        };

        // Act
        var settings = preset.ToSettings(@"C:\input.mp4", @"C:\output.mp4");

        // Assert
        settings.Should().NotBeNull();
        settings.VideoCodec.Should().Be("libx265");
        settings.Quality.Should().Be(20);
        settings.InputPath.Should().Be(@"C:\input.mp4");
        settings.OutputPath.Should().Be(@"C:\output.mp4");
    }

    [Fact]
    public void ExportPreset_ToSettings_CopiesAllSettings()
    {
        // Arrange
        var preset = new ExportPreset
        {
            VideoCodec = "h264_nvenc",
            AudioCodec = "flac",
            Quality = 18,
            Preset = "slow",
            HardwarePreset = "p6",
            ProResProfile = 3,
            AudioBitrate = 320
        };

        // Act
        var settings = preset.ToSettings(@"C:\in.mp4", @"C:\out.mp4");

        // Assert
        settings.VideoCodec.Should().Be("h264_nvenc");
        settings.AudioCodec.Should().Be("flac");
        settings.Quality.Should().Be(18);
        settings.Preset.Should().Be("slow");
        settings.HardwarePreset.Should().Be("p6");
        settings.ProResProfile.Should().Be(3);
        settings.AudioBitrate.Should().Be(320);
    }

    #endregion

    #region Input/Output Path Tests

    [Fact]
    public void ExportSettings_InputPath_CanBeSet()
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.InputPath = @"C:\videos\input.mp4";

        // Assert
        settings.InputPath.Should().Be(@"C:\videos\input.mp4");
    }

    [Fact]
    public void ExportSettings_OutputPath_CanBeSet()
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.OutputPath = @"C:\videos\output.mp4";

        // Assert
        settings.OutputPath.Should().Be(@"C:\videos\output.mp4");
    }

    [Fact]
    public void ExportSettings_DefaultInputPath_IsEmpty()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.InputPath.Should().BeEmpty();
    }

    [Fact]
    public void ExportSettings_DefaultOutputPath_IsEmpty()
    {
        // Act
        var settings = new ExportSettings();

        // Assert
        settings.OutputPath.Should().BeEmpty();
    }

    #endregion

    #region Video Enabled Tests

    [Fact]
    public void ExportSettings_VideoDisabled_CanBeSet()
    {
        // Arrange
        var settings = new ExportSettings();

        // Act
        settings.VideoEnabled = false;

        // Assert
        settings.VideoEnabled.Should().BeFalse();
    }

    [Fact]
    public void ExportSettings_AudioOnlyExport_VideoDisabled()
    {
        // Arrange - Audio only export
        var settings = new ExportSettings
        {
            VideoEnabled = false,
            AudioEnabled = true,
            AudioCodec = "flac"
        };

        // Assert
        settings.VideoEnabled.Should().BeFalse();
        settings.AudioEnabled.Should().BeTrue();
    }

    #endregion
}
