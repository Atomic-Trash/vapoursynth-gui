using System.Diagnostics;
using System.IO;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for FFmpeg encoding operations using FFMpegCore
/// </summary>
public class FFmpegService
{
    private static readonly ILogger<FFmpegService> _logger = LoggingService.GetLogger<FFmpegService>();

    private CancellationTokenSource? _encodingCts;
    private bool _isEncoding;
    private static bool _isConfigured;
    private static readonly object _configLock = new();

    public event EventHandler<EncodingProgressEventArgs>? ProgressChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler? EncodingStarted;
    public event EventHandler<EncodingCompletedEventArgs>? EncodingCompleted;

    public bool IsEncoding => _isEncoding;

    public FFmpegService()
    {
        ConfigureFFMpegCore();
        _logger.LogInformation("FFmpegService initialized with FFMpegCore. Available: {IsAvailable}", IsAvailable);
    }

    private static void ConfigureFFMpegCore()
    {
        lock (_configLock)
        {
            if (_isConfigured) return;

            var ffmpegPath = FindFFmpegDirectory();
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                GlobalFFOptions.Configure(options =>
                {
                    options.BinaryFolder = ffmpegPath;
                    options.TemporaryFilesFolder = Path.GetTempPath();
                });
                _logger.LogInformation("FFMpegCore configured with binary folder: {Path}", ffmpegPath);
            }
            else
            {
                _logger.LogWarning("FFmpeg binaries not found in expected locations");
            }

            _isConfigured = true;
        }
    }

    private static string? FindFFmpegDirectory()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));

        // Check dist/ffmpeg folder
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");
        if (Directory.Exists(ffmpegDir))
        {
            if (File.Exists(Path.Combine(ffmpegDir, "ffmpeg.exe")))
                return ffmpegDir;

            var binDir = Path.Combine(ffmpegDir, "bin");
            if (File.Exists(Path.Combine(binDir, "ffmpeg.exe")))
                return binDir;
        }

        // Check app directory
        if (File.Exists(Path.Combine(basePath, "ffmpeg.exe")))
            return basePath;

        // Check PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            if (File.Exists(Path.Combine(dir, "ffmpeg.exe")))
                return dir;
        }

        return null;
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                var ffmpegPath = GlobalFFOptions.Current.BinaryFolder;
                return !string.IsNullOrEmpty(ffmpegPath) &&
                       File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe"));
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Analyze a media file and return detailed information
    /// </summary>
    public async Task<MediaAnalysis?> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found for analysis: {FilePath}", filePath);
            return null;
        }

        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);
            _logger.LogDebug("Analyzed {FilePath}: {Duration}s, {Width}x{Height}",
                filePath, mediaInfo.Duration.TotalSeconds,
                mediaInfo.PrimaryVideoStream?.Width ?? 0,
                mediaInfo.PrimaryVideoStream?.Height ?? 0);

            // Get file size from FileInfo since FFMpegCore doesn't expose it directly
            var fileInfo = new FileInfo(filePath);

            return new MediaAnalysis
            {
                FilePath = filePath,
                Duration = mediaInfo.Duration,
                Format = mediaInfo.Format.FormatName,
                FormatLongName = mediaInfo.Format.FormatLongName,
                FileSize = fileInfo.Length,
                BitRate = (long)mediaInfo.Format.BitRate,
                VideoStream = mediaInfo.PrimaryVideoStream != null ? new VideoStreamInfo
                {
                    Codec = mediaInfo.PrimaryVideoStream.CodecName,
                    CodecLongName = mediaInfo.PrimaryVideoStream.CodecLongName,
                    Width = mediaInfo.PrimaryVideoStream.Width,
                    Height = mediaInfo.PrimaryVideoStream.Height,
                    FrameRate = mediaInfo.PrimaryVideoStream.FrameRate,
                    BitRate = mediaInfo.PrimaryVideoStream.BitRate,
                    PixelFormat = mediaInfo.PrimaryVideoStream.PixelFormat,
                    Duration = mediaInfo.PrimaryVideoStream.Duration
                } : null,
                AudioStream = mediaInfo.PrimaryAudioStream != null ? new AudioStreamInfo
                {
                    Codec = mediaInfo.PrimaryAudioStream.CodecName,
                    CodecLongName = mediaInfo.PrimaryAudioStream.CodecLongName,
                    SampleRate = mediaInfo.PrimaryAudioStream.SampleRateHz,
                    Channels = mediaInfo.PrimaryAudioStream.Channels,
                    ChannelLayout = mediaInfo.PrimaryAudioStream.ChannelLayout,
                    BitRate = mediaInfo.PrimaryAudioStream.BitRate,
                    Duration = mediaInfo.PrimaryAudioStream.Duration
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Get duration of a media file in seconds
    /// </summary>
    public async Task<double> GetDurationAsync(string filePath)
    {
        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);
            return mediaInfo.Duration.TotalSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get duration for: {FilePath}", filePath);
            return 0;
        }
    }

    /// <summary>
    /// Encode a file with the specified settings
    /// </summary>
    public async Task<bool> EncodeAsync(ExportSettings settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting encode: {InputPath} -> {OutputPath}", settings.InputPath, settings.OutputPath);

        if (!File.Exists(settings.InputPath))
        {
            _logger.LogError("Input file not found: {InputPath}", settings.InputPath);
            LogMessage?.Invoke(this, $"Error: Input file not found: {settings.InputPath}");
            return false;
        }

        _encodingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isEncoding = true;

        LogMessage?.Invoke(this, $"Starting encode: {Path.GetFileName(settings.InputPath)}");
        LogMessage?.Invoke(this, $"Output: {settings.OutputPath}");

        try
        {
            // Get duration for progress reporting
            var mediaInfo = await FFProbe.AnalyseAsync(settings.InputPath, cancellationToken: _encodingCts.Token);
            var totalDuration = mediaInfo.Duration.TotalSeconds;

            EncodingStarted?.Invoke(this, EventArgs.Empty);

            LogMessage?.Invoke(this, $"Encoding with: {settings.VideoCodec} / {settings.AudioCodec}");

            // Build FFMpeg arguments using fluent API
            var processor = BuildFFMpegArgumentProcessor(settings);

            // Execute with progress tracking
            var totalTimeSpan = TimeSpan.FromSeconds(totalDuration);
            var success = await processor
                .NotifyOnProgress((double currentSeconds) =>
                {
                    // currentSeconds is a double representing elapsed seconds
                    var progressPercent = totalDuration > 0
                        ? (currentSeconds / totalDuration) * 100
                        : 0.0;

                    ProgressChanged?.Invoke(this, new EncodingProgressEventArgs
                    {
                        Progress = Math.Min(100, progressPercent),
                        CurrentTime = currentSeconds,
                        TotalDuration = totalDuration,
                        Fps = 0, // FFMpegCore doesn't provide FPS in progress
                        Bitrate = 0,
                        Speed = 0
                    });
                }, totalTimeSpan)
                .NotifyOnError(error =>
                {
                    LogMessage?.Invoke(this, $"FFmpeg: {error}");
                })
                .NotifyOnOutput(output =>
                {
                    if (output.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        output.Contains("warning", StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage?.Invoke(this, $"FFmpeg: {output}");
                    }
                })
                .CancellableThrough(_encodingCts.Token)
                .ProcessAsynchronously(throwOnError: false);

            var cancelled = _encodingCts.IsCancellationRequested;

            EncodingCompleted?.Invoke(this, new EncodingCompletedEventArgs
            {
                Success = success && !cancelled,
                OutputPath = settings.OutputPath,
                Cancelled = cancelled
            });

            if (success && !cancelled)
            {
                _logger.LogInformation("Encoding completed successfully: {OutputPath}", settings.OutputPath);
                LogMessage?.Invoke(this, "Encoding completed successfully!");
            }
            else if (cancelled)
            {
                _logger.LogInformation("Encoding cancelled by user");
                LogMessage?.Invoke(this, "Encoding cancelled.");
                // Clean up partial file
                TryDeleteFile(settings.OutputPath);
            }
            else
            {
                _logger.LogError("Encoding failed");
                LogMessage?.Invoke(this, "Encoding failed.");
            }

            return success && !cancelled;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Encoding operation cancelled");
            TryDeleteFile(settings.OutputPath);
            EncodingCompleted?.Invoke(this, new EncodingCompletedEventArgs
            {
                Success = false,
                OutputPath = settings.OutputPath,
                Cancelled = true
            });
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encoding failed with exception");
            LogMessage?.Invoke(this, $"Error: {ex.Message}");
            EncodingCompleted?.Invoke(this, new EncodingCompletedEventArgs
            {
                Success = false,
                OutputPath = settings.OutputPath,
                Cancelled = false
            });
            return false;
        }
        finally
        {
            _isEncoding = false;
            _encodingCts?.Dispose();
            _encodingCts = null;
        }
    }

    private FFMpegArgumentProcessor BuildFFMpegArgumentProcessor(ExportSettings settings)
    {
        return FFMpegArguments
            .FromFileInput(settings.InputPath)
            .OutputToFile(settings.OutputPath, overwrite: true, options =>
            {
                // Video settings
                if (settings.VideoEnabled)
                {
                    // Set video codec
                    options.WithVideoCodec(settings.VideoCodec);

                    // Codec-specific settings
                    if (settings.VideoCodec == "libx264" || settings.VideoCodec == "libx265")
                    {
                        options.WithConstantRateFactor(settings.Quality);
                        options.WithSpeedPreset((Speed)Enum.Parse(typeof(Speed), settings.Preset, true));

                        if (settings.VideoCodec == "libx265")
                        {
                            options.WithCustomArgument("-tag:v hvc1");
                        }
                    }
                    else if (settings.VideoCodec == "h264_nvenc" || settings.VideoCodec == "hevc_nvenc")
                    {
                        options.WithCustomArgument($"-preset {settings.HardwarePreset}");
                        options.WithCustomArgument($"-cq {settings.Quality}");
                    }
                    else if (settings.VideoCodec == "prores_ks")
                    {
                        options.WithCustomArgument($"-profile:v {settings.ProResProfile}");
                    }

                    // Resolution
                    if (settings.Width > 0 && settings.Height > 0)
                    {
                        options.WithVideoFilters(filters =>
                            filters.Scale(settings.Width, settings.Height));
                    }

                    // Frame rate
                    if (settings.FrameRate > 0)
                    {
                        options.WithFramerate(settings.FrameRate);
                    }

                    // Pixel format
                    if (!string.IsNullOrEmpty(settings.PixelFormat))
                    {
                        options.WithCustomArgument($"-pix_fmt {settings.PixelFormat}");
                    }
                }
                else
                {
                    options.DisableChannel(Channel.Video);
                }

                // Audio settings
                if (settings.AudioEnabled)
                {
                    options.WithAudioCodec(settings.AudioCodec);

                    if (settings.AudioCodec == "aac" || settings.AudioCodec == "libmp3lame")
                    {
                        options.WithAudioBitrate(settings.AudioBitrate);
                    }

                    if (settings.AudioSampleRate > 0)
                    {
                        options.WithAudioSamplingRate(settings.AudioSampleRate);
                    }
                }
                else
                {
                    options.DisableChannel(Channel.Audio);
                }

                // Container-specific options
                if (settings.OutputPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    options.WithCustomArgument("-movflags +faststart");
                }

                // Fast seek for better performance
                options.WithFastStart();
            });
    }

    /// <summary>
    /// Extract a single frame as an image
    /// </summary>
    public async Task<bool> ExtractFrameAsync(string inputPath, string outputPath, TimeSpan position,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath, verifyExists: true, options => options.Seek(position))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithVideoCodec("mjpeg")
                    .WithFrameOutputCount(1)
                    .DisableChannel(Channel.Audio))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            return File.Exists(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract frame at {Position} from {InputPath}", position, inputPath);
            return false;
        }
    }

    /// <summary>
    /// Generate a thumbnail image
    /// </summary>
    public async Task<bool> GenerateThumbnailAsync(string inputPath, string outputPath,
        int width = 320, int height = 180, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get duration and seek to 10% of the video
            var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
            var seekPosition = TimeSpan.FromSeconds(mediaInfo.Duration.TotalSeconds * 0.1);

            await FFMpegArguments
                .FromFileInput(inputPath, verifyExists: true, options => options.Seek(seekPosition))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithVideoCodec("mjpeg")
                    .WithVideoFilters(filters => filters.Scale(width, height))
                    .WithFrameOutputCount(1)
                    .DisableChannel(Channel.Audio))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            return File.Exists(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {InputPath}", inputPath);
            return false;
        }
    }

    /// <summary>
    /// Detect available hardware encoders and decoders
    /// </summary>
    public async Task<HardwareAcceleration> DetectHardwareAccelerationAsync()
    {
        var result = new HardwareAcceleration();

        try
        {
            // Check for NVIDIA NVENC encoding
            result.NvencAvailable = await CheckEncoderAvailableAsync("h264_nvenc");
            result.NvencHevcAvailable = await CheckEncoderAvailableAsync("hevc_nvenc");

            // Check for Intel QSV encoding
            result.QsvAvailable = await CheckEncoderAvailableAsync("h264_qsv");
            result.QsvHevcAvailable = await CheckEncoderAvailableAsync("hevc_qsv");

            // Check for AMD AMF encoding
            result.AmfAvailable = await CheckEncoderAvailableAsync("h264_amf");
            result.AmfHevcAvailable = await CheckEncoderAvailableAsync("hevc_amf");

            // Check for hardware decoding (hwaccels)
            var hwaccels = await GetAvailableHwAccelsAsync();
            result.CudaAvailable = hwaccels.Contains("cuda");
            result.CuvidAvailable = hwaccels.Contains("cuvid");
            result.QsvDecodeAvailable = hwaccels.Contains("qsv");
            result.D3d11vaAvailable = hwaccels.Contains("d3d11va");
            result.Dxva2Available = hwaccels.Contains("dxva2");
            result.VulkanAvailable = hwaccels.Contains("vulkan");

            _logger.LogInformation("Hardware acceleration detected - Encoding: NVENC={Nvenc}, QSV={Qsv}, AMF={Amf}; Decoding: CUDA={Cuda}, QSV={QsvDec}, D3D11VA={D3d11}",
                result.NvencAvailable, result.QsvAvailable, result.AmfAvailable,
                result.CudaAvailable, result.QsvDecodeAvailable, result.D3d11vaAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting hardware acceleration");
        }

        return result;
    }

    private async Task<HashSet<string>> GetAvailableHwAccelsAsync()
    {
        var hwaccels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var ffmpegPath = Path.Combine(GlobalFFOptions.Current.BinaryFolder, "ffmpeg.exe");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-hide_banner -hwaccels",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output - each line after "Hardware acceleration methods:" is a hwaccel name
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var parsing = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("Hardware acceleration methods"))
                {
                    parsing = true;
                    continue;
                }
                if (parsing && !string.IsNullOrEmpty(trimmed))
                {
                    hwaccels.Add(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get available hwaccels");
        }

        return hwaccels;
    }

    private async Task<bool> CheckEncoderAvailableAsync(string encoderName)
    {
        try
        {
            var ffmpegPath = Path.Combine(GlobalFFOptions.Current.BinaryFolder, "ffmpeg.exe");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-hide_banner -encoders",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Contains(encoderName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void Cancel()
    {
        _logger.LogInformation("Cancelling encoding");
        _encodingCts?.Cancel();
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Path}", path);
        }
    }

    public static IEnumerable<ExportPreset> GetPresets()
    {
        return new List<ExportPreset>
        {
            // H.264 presets
            new() { Name = "H.264 High Quality", Format = "mp4", VideoCodec = "libx264", Quality = 18, Preset = "slow", Description = "Best quality, slower encoding" },
            new() { Name = "H.264 Balanced", Format = "mp4", VideoCodec = "libx264", Quality = 22, Preset = "medium", Description = "Good quality, reasonable speed" },
            new() { Name = "H.264 Fast", Format = "mp4", VideoCodec = "libx264", Quality = 25, Preset = "fast", Description = "Faster encoding, larger file" },
            new() { Name = "H.264 Web/YouTube", Format = "mp4", VideoCodec = "libx264", Quality = 20, Preset = "medium", AudioBitrate = 192, Description = "Optimized for web streaming" },

            // H.265/HEVC presets
            new() { Name = "H.265 High Quality", Format = "mp4", VideoCodec = "libx265", Quality = 20, Preset = "slow", Description = "Better compression than H.264" },
            new() { Name = "H.265 Balanced", Format = "mp4", VideoCodec = "libx265", Quality = 24, Preset = "medium", Description = "Good quality, smaller file size" },

            // NVIDIA GPU presets
            new() { Name = "NVENC H.264 (GPU)", Format = "mp4", VideoCodec = "h264_nvenc", Quality = 20, HardwarePreset = "p4", Description = "Fast GPU encoding (NVIDIA)" },
            new() { Name = "NVENC H.265 (GPU)", Format = "mp4", VideoCodec = "hevc_nvenc", Quality = 22, HardwarePreset = "p4", Description = "Fast GPU HEVC encoding (NVIDIA)" },

            // ProRes presets
            new() { Name = "ProRes 422", Format = "mov", VideoCodec = "prores_ks", ProResProfile = 2, AudioCodec = "pcm_s16le", Description = "Professional editing codec" },
            new() { Name = "ProRes 422 HQ", Format = "mov", VideoCodec = "prores_ks", ProResProfile = 3, AudioCodec = "pcm_s16le", Description = "Higher quality ProRes" },
            new() { Name = "ProRes 4444", Format = "mov", VideoCodec = "prores_ks", ProResProfile = 4, AudioCodec = "pcm_s16le", Description = "With alpha channel support" },

            // Lossless
            new() { Name = "FFV1 Lossless", Format = "mkv", VideoCodec = "ffv1", AudioCodec = "flac", Description = "Archival lossless format" },

            // Audio only
            new() { Name = "MP3 320kbps", Format = "mp3", VideoCodec = "", AudioCodec = "libmp3lame", AudioBitrate = 320, VideoEnabled = false, Description = "High quality MP3" },
            new() { Name = "AAC 256kbps", Format = "m4a", VideoCodec = "", AudioCodec = "aac", AudioBitrate = 256, VideoEnabled = false, Description = "High quality AAC" },
            new() { Name = "FLAC Lossless", Format = "flac", VideoCodec = "", AudioCodec = "flac", VideoEnabled = false, Description = "Lossless audio" },
        };
    }
}

#region Models

public class ExportSettings
{
    public string InputPath { get; set; } = "";
    public string OutputPath { get; set; } = "";

    // Video settings
    public bool VideoEnabled { get; set; } = true;
    public string VideoCodec { get; set; } = "libx264";
    public int Quality { get; set; } = 22; // CRF for x264/x265, CQ for NVENC
    public string Preset { get; set; } = "medium";
    public string HardwarePreset { get; set; } = "p4";
    public int ProResProfile { get; set; } = 2;
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public string PixelFormat { get; set; } = "yuv420p";

    // Audio settings
    public bool AudioEnabled { get; set; } = true;
    public string AudioCodec { get; set; } = "aac";
    public int AudioBitrate { get; set; } = 192;
    public int AudioSampleRate { get; set; } = 48000;
}

public class ExportPreset
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Format { get; set; } = "mp4";
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public int Quality { get; set; } = 22;
    public string Preset { get; set; } = "medium";
    public string HardwarePreset { get; set; } = "p4";
    public int ProResProfile { get; set; } = 2;
    public int AudioBitrate { get; set; } = 192;
    public bool VideoEnabled { get; set; } = true;

    public ExportSettings ToSettings(string inputPath, string outputPath)
    {
        return new ExportSettings
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            VideoEnabled = VideoEnabled,
            VideoCodec = VideoCodec,
            Quality = Quality,
            Preset = Preset,
            HardwarePreset = HardwarePreset,
            ProResProfile = ProResProfile,
            AudioEnabled = true,
            AudioCodec = AudioCodec,
            AudioBitrate = AudioBitrate
        };
    }
}

public class EncodingProgressEventArgs : EventArgs
{
    public double Progress { get; set; }
    public double CurrentTime { get; set; }
    public double TotalDuration { get; set; }
    public double Fps { get; set; }
    public double Bitrate { get; set; }
    public double Speed { get; set; }

    public string TimeRemaining
    {
        get
        {
            if (Speed <= 0 || Progress <= 0) return "--:--:--";
            var remaining = (TotalDuration - CurrentTime) / Speed;
            var ts = TimeSpan.FromSeconds(remaining);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}

public class EncodingCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public bool Cancelled { get; set; }
    public string OutputPath { get; set; } = "";
}

/// <summary>
/// Detailed media file analysis results
/// </summary>
public class MediaAnalysis
{
    public string FilePath { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public string Format { get; set; } = "";
    public string FormatLongName { get; set; } = "";
    public long FileSize { get; set; }
    public long BitRate { get; set; }
    public VideoStreamInfo? VideoStream { get; set; }
    public AudioStreamInfo? AudioStream { get; set; }

    public string Resolution => VideoStream != null
        ? $"{VideoStream.Width}x{VideoStream.Height}"
        : "";

    public string DurationFormatted
    {
        get
        {
            var ts = Duration;
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F1} MB";
            return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
        }
    }
}

public class VideoStreamInfo
{
    public string Codec { get; set; } = "";
    public string CodecLongName { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public long BitRate { get; set; }
    public string PixelFormat { get; set; } = "";
    public TimeSpan Duration { get; set; }
}

public class AudioStreamInfo
{
    public string Codec { get; set; } = "";
    public string CodecLongName { get; set; } = "";
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public string ChannelLayout { get; set; } = "";
    public long BitRate { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Hardware acceleration availability
/// </summary>
public class HardwareAcceleration
{
    // Encoding capabilities
    public bool NvencAvailable { get; set; }
    public bool NvencHevcAvailable { get; set; }
    public bool QsvAvailable { get; set; }
    public bool QsvHevcAvailable { get; set; }
    public bool AmfAvailable { get; set; }
    public bool AmfHevcAvailable { get; set; }

    // Decoding capabilities
    public bool CudaAvailable { get; set; }
    public bool CuvidAvailable { get; set; }
    public bool QsvDecodeAvailable { get; set; }
    public bool D3d11vaAvailable { get; set; }
    public bool Dxva2Available { get; set; }
    public bool VulkanAvailable { get; set; }

    public bool AnyEncodingAvailable =>
        NvencAvailable || QsvAvailable || AmfAvailable;

    public bool AnyDecodingAvailable =>
        CudaAvailable || CuvidAvailable || QsvDecodeAvailable || D3d11vaAvailable || Dxva2Available || VulkanAvailable;

    public bool AnyAvailable =>
        AnyEncodingAvailable || AnyDecodingAvailable;

    public string GetBestH264Encoder()
    {
        if (NvencAvailable) return "h264_nvenc";
        if (QsvAvailable) return "h264_qsv";
        if (AmfAvailable) return "h264_amf";
        return "libx264";
    }

    public string GetBestHevcEncoder()
    {
        if (NvencHevcAvailable) return "hevc_nvenc";
        if (QsvHevcAvailable) return "hevc_qsv";
        if (AmfHevcAvailable) return "hevc_amf";
        return "libx265";
    }

    /// <summary>
    /// Gets the best available hardware acceleration method for decoding
    /// </summary>
    public string? GetBestHwAccel()
    {
        if (CudaAvailable) return "cuda";
        if (CuvidAvailable) return "cuvid";
        if (QsvDecodeAvailable) return "qsv";
        if (D3d11vaAvailable) return "d3d11va";
        if (Dxva2Available) return "dxva2";
        if (VulkanAvailable) return "vulkan";
        return null;
    }

    /// <summary>
    /// Gets FFmpeg hwaccel arguments for decoding
    /// </summary>
    public string GetHwAccelArgs()
    {
        var hwaccel = GetBestHwAccel();
        if (hwaccel == null) return "";

        return hwaccel switch
        {
            "cuda" => "-hwaccel cuda -hwaccel_output_format cuda",
            "cuvid" => "-hwaccel cuvid",
            "qsv" => "-hwaccel qsv -hwaccel_output_format qsv",
            "d3d11va" => "-hwaccel d3d11va",
            "dxva2" => "-hwaccel dxva2",
            "vulkan" => "-hwaccel vulkan",
            _ => ""
        };
    }

    public override string ToString()
    {
        var parts = new List<string>();

        // Encoding
        if (NvencAvailable || NvencHevcAvailable)
            parts.Add($"NVENC (H.264={NvencAvailable}, HEVC={NvencHevcAvailable})");
        if (QsvAvailable || QsvHevcAvailable)
            parts.Add($"QSV (H.264={QsvAvailable}, HEVC={QsvHevcAvailable})");
        if (AmfAvailable || AmfHevcAvailable)
            parts.Add($"AMF (H.264={AmfAvailable}, HEVC={AmfHevcAvailable})");

        // Decoding
        var decoders = new List<string>();
        if (CudaAvailable) decoders.Add("CUDA");
        if (CuvidAvailable) decoders.Add("CUVID");
        if (QsvDecodeAvailable) decoders.Add("QSV");
        if (D3d11vaAvailable) decoders.Add("D3D11VA");
        if (Dxva2Available) decoders.Add("DXVA2");
        if (VulkanAvailable) decoders.Add("Vulkan");

        if (decoders.Count > 0)
            parts.Add($"Decode: {string.Join(", ", decoders)}");

        return parts.Count > 0 ? string.Join("; ", parts) : "No hardware acceleration available";
    }
}

#endregion
