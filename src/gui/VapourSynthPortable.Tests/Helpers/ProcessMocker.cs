namespace VapourSynthPortable.Tests.Helpers;

using VapourSynthPortable.Services;

/// <summary>
/// Helper for creating mock process outputs and sample data for testing
/// external process integrations (FFmpeg, VapourSynth/VSPipe, Python).
/// </summary>
public static class ProcessMocker
{
    #region FFProbe Sample Outputs

    /// <summary>
    /// Returns sample FFProbe JSON output for a typical MP4 video file.
    /// </summary>
    public static string GetFFProbeVideoOutput(
        int width = 1920,
        int height = 1080,
        double frameRate = 23.976,
        double duration = 120.5,
        string videoCodec = "h264",
        string audioCodec = "aac",
        int audioChannels = 2,
        int audioSampleRate = 48000)
    {
        return $$"""
        {
            "format": {
                "filename": "test_video.mp4",
                "nb_streams": 2,
                "format_name": "mov,mp4,m4a,3gp,3g2,mj2",
                "format_long_name": "QuickTime / MOV",
                "duration": "{{duration:F6}}",
                "size": "{{(int)(duration * 1000000)}}",
                "bit_rate": "8000000"
            },
            "streams": [
                {
                    "index": 0,
                    "codec_name": "{{videoCodec}}",
                    "codec_long_name": "H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10",
                    "codec_type": "video",
                    "width": {{width}},
                    "height": {{height}},
                    "r_frame_rate": "{{(int)(frameRate * 1000)}}/1000",
                    "avg_frame_rate": "{{(int)(frameRate * 1000)}}/1000",
                    "duration": "{{duration:F6}}",
                    "bit_rate": "6000000",
                    "pix_fmt": "yuv420p"
                },
                {
                    "index": 1,
                    "codec_name": "{{audioCodec}}",
                    "codec_long_name": "AAC (Advanced Audio Coding)",
                    "codec_type": "audio",
                    "sample_rate": "{{audioSampleRate}}",
                    "channels": {{audioChannels}},
                    "channel_layout": "{{(audioChannels == 2 ? "stereo" : "mono")}}",
                    "duration": "{{duration:F6}}",
                    "bit_rate": "192000"
                }
            ]
        }
        """;
    }

    /// <summary>
    /// Returns sample FFProbe JSON output for an audio-only file.
    /// </summary>
    public static string GetFFProbeAudioOnlyOutput(
        double duration = 180.0,
        string codec = "mp3",
        int channels = 2,
        int sampleRate = 44100,
        int bitRate = 320000)
    {
        return $$"""
        {
            "format": {
                "filename": "test_audio.mp3",
                "nb_streams": 1,
                "format_name": "mp3",
                "format_long_name": "MP3 (MPEG audio layer 3)",
                "duration": "{{duration:F6}}",
                "size": "{{(int)(bitRate * duration / 8)}}",
                "bit_rate": "{{bitRate}}"
            },
            "streams": [
                {
                    "index": 0,
                    "codec_name": "{{codec}}",
                    "codec_long_name": "MP3 (MPEG audio layer 3)",
                    "codec_type": "audio",
                    "sample_rate": "{{sampleRate}}",
                    "channels": {{channels}},
                    "channel_layout": "{{(channels == 2 ? "stereo" : "mono")}}",
                    "duration": "{{duration:F6}}",
                    "bit_rate": "{{bitRate}}"
                }
            ]
        }
        """;
    }

    /// <summary>
    /// Returns sample FFProbe JSON output for an image file.
    /// </summary>
    public static string GetFFProbeImageOutput(
        int width = 1920,
        int height = 1080,
        string codec = "mjpeg")
    {
        return $$"""
        {
            "format": {
                "filename": "test_image.jpg",
                "nb_streams": 1,
                "format_name": "image2",
                "format_long_name": "image2 sequence",
                "duration": "0.040000",
                "size": "500000",
                "bit_rate": "100000000"
            },
            "streams": [
                {
                    "index": 0,
                    "codec_name": "{{codec}}",
                    "codec_long_name": "MJPEG (Motion JPEG)",
                    "codec_type": "video",
                    "width": {{width}},
                    "height": {{height}},
                    "pix_fmt": "yuvj420p"
                }
            ]
        }
        """;
    }

    /// <summary>
    /// Returns sample FFProbe output for a corrupted/invalid file.
    /// </summary>
    public static string GetFFProbeErrorOutput()
    {
        return """
        {
            "error": {
                "code": -1094995529,
                "string": "Invalid data found when processing input"
            }
        }
        """;
    }

    #endregion

    #region VSPipe Sample Outputs

    /// <summary>
    /// Returns sample VSPipe -i (info) output for a VapourSynth script.
    /// </summary>
    public static string GetVSPipeInfoOutput(
        int width = 1920,
        int height = 1080,
        int frameCount = 2880,
        int fpsNum = 24000,
        int fpsDen = 1001,
        string format = "YUV420P8",
        string colorFamily = "YUV",
        int bits = 8)
    {
        return $$"""
        Width: {{width}}
        Height: {{height}}
        Frames: {{frameCount}}
        FPS: {{fpsNum}}/{{fpsDen}} ({{(double)fpsNum / fpsDen:F3}} fps)
        Format Name: {{format}}
        Color Family: {{colorFamily}}
        Bits: {{bits}}
        SubSampling W: 1
        SubSampling H: 1
        """;
    }

    /// <summary>
    /// Returns sample VSPipe progress output lines.
    /// </summary>
    public static IEnumerable<string> GetVSPipeProgressLines(int totalFrames, int step = 100)
    {
        for (int frame = 0; frame <= totalFrames; frame += step)
        {
            yield return $"Frame: {Math.Min(frame, totalFrames)}/{totalFrames}";
        }
    }

    /// <summary>
    /// Returns VSPipe error output for a script syntax error.
    /// </summary>
    public static string GetVSPipeScriptErrorOutput(string errorMessage = "SyntaxError: invalid syntax")
    {
        return $"""
        Script error: line 10
        {errorMessage}
        Error: Script evaluation failed
        """;
    }

    /// <summary>
    /// Returns VSPipe error output for a missing plugin.
    /// </summary>
    public static string GetVSPipeMissingPluginOutput(string pluginName = "bm3d")
    {
        return $"""
        Error: Failed to retrieve output node. No attribute named '{pluginName}' in 'core' namespace
        Python exception: Module not found: 'vs{pluginName}'
        """;
    }

    #endregion

    #region FFmpeg Encoding Outputs

    /// <summary>
    /// Returns sample FFmpeg encoding progress output line.
    /// </summary>
    public static string GetFFmpegProgressLine(
        int frame = 1000,
        double fps = 45.5,
        double bitrate = 5234.2,
        double speed = 1.5,
        TimeSpan time = default)
    {
        if (time == default) time = TimeSpan.FromSeconds(frame / 24.0);
        return $"frame={frame} fps={fps:F1} q=28.0 size={frame * 10}kB time={time:hh\\:mm\\:ss\\.ff} bitrate={bitrate:F1}kbits/s speed={speed:F2}x";
    }

    /// <summary>
    /// Returns sample FFmpeg encoding completion output.
    /// </summary>
    public static string GetFFmpegCompletionOutput(int frames = 2880, double duration = 120.0, long fileSize = 15000000)
    {
        return $"""
        frame={frames} fps=45.0 q=-1.0 Lsize={fileSize / 1024}kB time={TimeSpan.FromSeconds(duration):hh\\:mm\\:ss\\.ff} bitrate={fileSize * 8 / duration / 1000:F1}kbits/s speed=1.88x
        video:{fileSize * 80 / 100 / 1024}kB audio:{fileSize * 15 / 100 / 1024}kB subtitle:0kB other streams:0kB global headers:0kB muxing overhead: 0.5%
        """;
    }

    /// <summary>
    /// Returns sample FFmpeg error output.
    /// </summary>
    public static string GetFFmpegErrorOutput(string error = "Unknown encoder 'invalid_codec'")
    {
        return $"""
        ffmpeg version 6.0-full Copyright (c) 2000-2023 the FFmpeg developers
        Unknown encoder 'invalid_codec'
        Error during encoding: {error}
        """;
    }

    /// <summary>
    /// Returns FFmpeg hardware encoders list output.
    /// </summary>
    public static string GetFFmpegEncodersOutput(bool hasNvenc = true, bool hasQsv = false, bool hasAmf = false)
    {
        var encoders = new List<string>
        {
            " V..... libx264              libx264 H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10 (codec h264)",
            " V..... libx265              libx265 H.265 / HEVC (codec hevc)",
            " V..... prores_ks            Apple ProRes (iCodec Pro) (codec prores)"
        };

        if (hasNvenc)
        {
            encoders.Add(" V..... h264_nvenc           NVIDIA NVENC H.264 encoder (codec h264)");
            encoders.Add(" V..... hevc_nvenc           NVIDIA NVENC hevc encoder (codec hevc)");
        }

        if (hasQsv)
        {
            encoders.Add(" V..... h264_qsv             H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10 (Intel Quick Sync Video acceleration) (codec h264)");
            encoders.Add(" V..... hevc_qsv             HEVC (Intel Quick Sync Video acceleration) (codec hevc)");
        }

        if (hasAmf)
        {
            encoders.Add(" V..... h264_amf             AMD AMF H.264 Encoder (codec h264)");
            encoders.Add(" V..... hevc_amf             AMD AMF HEVC Encoder (codec hevc)");
        }

        return string.Join(Environment.NewLine, encoders);
    }

    /// <summary>
    /// Returns FFmpeg hwaccels list output.
    /// </summary>
    public static string GetFFmpegHwaccelsOutput(bool hasCuda = true, bool hasQsv = false, bool hasD3d11 = true)
    {
        var hwaccels = new List<string> { "Hardware acceleration methods:" };

        if (hasCuda)
        {
            hwaccels.Add("cuda");
            hwaccels.Add("cuvid");
        }
        if (hasQsv) hwaccels.Add("qsv");
        if (hasD3d11)
        {
            hwaccels.Add("d3d11va");
            hwaccels.Add("dxva2");
        }
        hwaccels.Add("vulkan");

        return string.Join(Environment.NewLine, hwaccels);
    }

    #endregion

    #region Sample Data Generators

    /// <summary>
    /// Creates a sample MediaAnalysis object for testing.
    /// </summary>
    public static MediaAnalysis CreateSampleMediaAnalysis(
        string filePath = "test_video.mp4",
        int width = 1920,
        int height = 1080,
        double frameRate = 23.976,
        double durationSeconds = 120.0,
        string videoCodec = "h264",
        string audioCodec = "aac")
    {
        return new MediaAnalysis
        {
            FilePath = filePath,
            Duration = TimeSpan.FromSeconds(durationSeconds),
            Format = "mov,mp4,m4a,3gp,3g2,mj2",
            FormatLongName = "QuickTime / MOV",
            FileSize = (long)(durationSeconds * 1000000),
            BitRate = 8000000,
            VideoStream = new VideoStreamInfo
            {
                Codec = videoCodec,
                CodecLongName = "H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10",
                Width = width,
                Height = height,
                FrameRate = frameRate,
                BitRate = 6000000,
                PixelFormat = "yuv420p",
                Duration = TimeSpan.FromSeconds(durationSeconds)
            },
            AudioStream = new AudioStreamInfo
            {
                Codec = audioCodec,
                CodecLongName = "AAC (Advanced Audio Coding)",
                SampleRate = 48000,
                Channels = 2,
                ChannelLayout = "stereo",
                BitRate = 192000,
                Duration = TimeSpan.FromSeconds(durationSeconds)
            }
        };
    }

    /// <summary>
    /// Creates a sample VapourSynthScriptInfo object for testing.
    /// </summary>
    public static VapourSynthScriptInfo CreateSampleScriptInfo(
        int width = 1920,
        int height = 1080,
        int frameCount = 2880,
        double fps = 23.976,
        string format = "YUV420P8")
    {
        int fpsNum = (int)(fps * 1000);
        int fpsDen = 1000;

        return new VapourSynthScriptInfo
        {
            Width = width,
            Height = height,
            FrameCount = frameCount,
            Fps = fps,
            FpsNum = fpsNum,
            FpsDen = fpsDen,
            Format = format,
            ColorFamily = format.StartsWith("YUV") ? "YUV" : "RGB",
            BitsPerSample = 8
        };
    }

    /// <summary>
    /// Creates sample ExportSettings for testing.
    /// </summary>
    public static ExportSettings CreateSampleExportSettings(
        string inputPath = "input.mp4",
        string outputPath = "output.mp4",
        string videoCodec = "libx264",
        int quality = 22,
        string preset = "medium")
    {
        return new ExportSettings
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            VideoEnabled = true,
            VideoCodec = videoCodec,
            Quality = quality,
            Preset = preset,
            AudioEnabled = true,
            AudioCodec = "aac",
            AudioBitrate = 192,
            Width = 0, // Use source
            Height = 0,
            FrameRate = 0,
            PixelFormat = "yuv420p"
        };
    }

    /// <summary>
    /// Creates sample VapourSynthEncodingSettings for testing.
    /// </summary>
    public static VapourSynthEncodingSettings CreateSampleVSEncodingSettings(
        string videoCodec = "libx264",
        int quality = 18,
        string preset = "medium")
    {
        return new VapourSynthEncodingSettings
        {
            VideoCodec = videoCodec,
            Quality = quality,
            Preset = preset,
            HardwarePreset = "p4",
            PixelFormat = "yuv420p"
        };
    }

    /// <summary>
    /// Creates sample HardwareAcceleration result for testing.
    /// </summary>
    public static HardwareAcceleration CreateSampleHardwareAcceleration(
        bool nvenc = true,
        bool qsv = false,
        bool amf = false,
        bool cuda = true)
    {
        return new HardwareAcceleration
        {
            NvencAvailable = nvenc,
            NvencHevcAvailable = nvenc,
            QsvAvailable = qsv,
            QsvHevcAvailable = qsv,
            AmfAvailable = amf,
            AmfHevcAvailable = amf,
            CudaAvailable = cuda,
            CuvidAvailable = cuda,
            QsvDecodeAvailable = qsv,
            D3d11vaAvailable = true,
            Dxva2Available = true,
            VulkanAvailable = false
        };
    }

    #endregion

    #region Progress Event Generators

    /// <summary>
    /// Generates a sequence of EncodingProgressEventArgs for testing progress events.
    /// </summary>
    public static IEnumerable<EncodingProgressEventArgs> GenerateEncodingProgress(
        double totalDuration = 120.0,
        int steps = 10)
    {
        for (int i = 0; i <= steps; i++)
        {
            var progress = (double)i / steps * 100;
            var currentTime = totalDuration * i / steps;

            yield return new EncodingProgressEventArgs
            {
                Progress = progress,
                CurrentTime = currentTime,
                TotalDuration = totalDuration,
                Fps = 45.0,
                Bitrate = 5000,
                Speed = 1.5
            };
        }
    }

    /// <summary>
    /// Generates a sequence of VapourSynthProgressEventArgs for testing progress events.
    /// </summary>
    public static IEnumerable<VapourSynthProgressEventArgs> GenerateVSProgress(
        int totalFrames = 2880,
        int steps = 10)
    {
        var startTime = DateTime.Now;

        for (int i = 0; i <= steps; i++)
        {
            var currentFrame = totalFrames * i / steps;
            var progress = (double)i / steps * 100;
            var elapsed = TimeSpan.FromSeconds(i * 2); // Simulate 2 seconds per step

            yield return new VapourSynthProgressEventArgs
            {
                CurrentFrame = currentFrame,
                TotalFrames = totalFrames,
                Progress = progress,
                Fps = currentFrame > 0 ? currentFrame / elapsed.TotalSeconds : 0,
                ElapsedTime = elapsed
            };
        }
    }

    #endregion

    #region Error Scenario Helpers

    /// <summary>
    /// Creates sample error scenarios for testing error handling.
    /// </summary>
    public static class ErrorScenarios
    {
        /// <summary>File not found scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) FileNotFound =>
            (-1, "", "ffprobe: No such file or directory: 'nonexistent.mp4'");

        /// <summary>Corrupt file scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) CorruptFile =>
            (1, "", "Invalid data found when processing input");

        /// <summary>Permission denied scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) PermissionDenied =>
            (-1, "", "Permission denied: '/root/video.mp4'");

        /// <summary>Codec not found scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) CodecNotFound =>
            (1, "", "Unknown encoder 'invalid_codec'");

        /// <summary>Disk full scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) DiskFull =>
            (1, "", "No space left on device");

        /// <summary>Process timeout scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) Timeout =>
            (-1, "", "Process timed out after 30 seconds");

        /// <summary>VapourSynth script error scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) VSScriptError =>
            (1, "", GetVSPipeScriptErrorOutput());

        /// <summary>Missing VS plugin scenario</summary>
        public static (int ExitCode, string Stdout, string Stderr) VSPluginMissing =>
            (1, "", GetVSPipeMissingPluginOutput());
    }

    #endregion
}
