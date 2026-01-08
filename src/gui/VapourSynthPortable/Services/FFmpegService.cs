using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for FFmpeg encoding operations
/// </summary>
public class FFmpegService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private Process? _currentProcess;
    private bool _isCancelled;

    public event EventHandler<EncodingProgressEventArgs>? ProgressChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler? EncodingStarted;
    public event EventHandler<EncodingCompletedEventArgs>? EncodingCompleted;

    public bool IsEncoding => _currentProcess != null && !_currentProcess.HasExited;

    public FFmpegService()
    {
        _ffmpegPath = FindExecutable("ffmpeg.exe");
        _ffprobePath = FindExecutable("ffprobe.exe");
    }

    private static string FindExecutable(string name)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));

        // Check dist/ffmpeg folder
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");
        if (Directory.Exists(ffmpegDir))
        {
            var inFfmpegDir = Path.Combine(ffmpegDir, name);
            if (File.Exists(inFfmpegDir)) return inFfmpegDir;

            var inBin = Path.Combine(ffmpegDir, "bin", name);
            if (File.Exists(inBin)) return inBin;
        }

        // Check app directory
        var inApp = Path.Combine(basePath, name);
        if (File.Exists(inApp)) return inApp;

        // Check PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, name);
            if (File.Exists(fullPath)) return fullPath;
        }

        return name; // Fall back to just the name
    }

    public bool IsAvailable => File.Exists(_ffmpegPath) || CheckInPath("ffmpeg.exe");

    private static bool CheckInPath(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, "-version")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(1000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    public async Task<bool> EncodeAsync(ExportSettings settings, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settings.InputPath))
        {
            LogMessage?.Invoke(this, $"Error: Input file not found: {settings.InputPath}");
            return false;
        }

        _isCancelled = false;
        var args = BuildFFmpegArguments(settings);

        LogMessage?.Invoke(this, $"Starting encode: {Path.GetFileName(settings.InputPath)}");
        LogMessage?.Invoke(this, $"Output: {settings.OutputPath}");
        LogMessage?.Invoke(this, $"FFmpeg args: {args}");

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            _currentProcess = new Process { StartInfo = startInfo };
            _currentProcess.Start();

            EncodingStarted?.Invoke(this, EventArgs.Empty);

            // Get duration for progress calculation
            var duration = await GetDurationAsync(settings.InputPath);

            // Read stderr for progress (FFmpeg outputs to stderr)
            var progressTask = ReadProgressAsync(_currentProcess.StandardError, duration, cancellationToken);

            await _currentProcess.WaitForExitAsync(cancellationToken);
            await progressTask;

            var success = _currentProcess.ExitCode == 0 && !_isCancelled;

            EncodingCompleted?.Invoke(this, new EncodingCompletedEventArgs
            {
                Success = success,
                OutputPath = settings.OutputPath,
                Cancelled = _isCancelled
            });

            if (success)
            {
                LogMessage?.Invoke(this, "Encoding completed successfully!");
            }
            else if (_isCancelled)
            {
                LogMessage?.Invoke(this, "Encoding cancelled.");
                // Clean up partial file
                if (File.Exists(settings.OutputPath))
                {
                    try { File.Delete(settings.OutputPath); } catch { }
                }
            }
            else
            {
                LogMessage?.Invoke(this, $"Encoding failed with exit code: {_currentProcess.ExitCode}");
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            Cancel();
            return false;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke(this, $"Error: {ex.Message}");
            return false;
        }
        finally
        {
            _currentProcess = null;
        }
    }

    private async Task ReadProgressAsync(StreamReader stderr, double totalDuration, CancellationToken cancellationToken)
    {
        var timeRegex = new Regex(@"time=(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);
        var fpsRegex = new Regex(@"fps=\s*([\d.]+)", RegexOptions.Compiled);
        var bitrateRegex = new Regex(@"bitrate=\s*([\d.]+)kbits/s", RegexOptions.Compiled);
        var speedRegex = new Regex(@"speed=\s*([\d.]+)x", RegexOptions.Compiled);

        while (!stderr.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await stderr.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            // Log verbose output
            if (line.StartsWith("frame=") || line.Contains("error") || line.Contains("Error"))
            {
                LogMessage?.Invoke(this, line);
            }

            // Parse progress
            var timeMatch = timeRegex.Match(line);
            if (timeMatch.Success && totalDuration > 0)
            {
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = int.Parse(timeMatch.Groups[3].Value);
                var ms = int.Parse(timeMatch.Groups[4].Value);

                var currentTime = hours * 3600 + minutes * 60 + seconds + ms / 100.0;
                var progress = Math.Min(100, (currentTime / totalDuration) * 100);

                double fps = 0, bitrate = 0, speed = 0;

                var fpsMatch = fpsRegex.Match(line);
                if (fpsMatch.Success) double.TryParse(fpsMatch.Groups[1].Value, out fps);

                var bitrateMatch = bitrateRegex.Match(line);
                if (bitrateMatch.Success) double.TryParse(bitrateMatch.Groups[1].Value, out bitrate);

                var speedMatch = speedRegex.Match(line);
                if (speedMatch.Success) double.TryParse(speedMatch.Groups[1].Value, out speed);

                ProgressChanged?.Invoke(this, new EncodingProgressEventArgs
                {
                    Progress = progress,
                    CurrentTime = currentTime,
                    TotalDuration = totalDuration,
                    Fps = fps,
                    Bitrate = bitrate,
                    Speed = speed
                });
            }
        }
    }

    private string BuildFFmpegArguments(ExportSettings settings)
    {
        var args = new List<string>
        {
            "-y", // Overwrite output
            "-i", $"\"{settings.InputPath}\""
        };

        // Video codec settings
        if (settings.VideoEnabled)
        {
            args.Add($"-c:v {settings.VideoCodec}");

            if (settings.VideoCodec == "libx264" || settings.VideoCodec == "libx265")
            {
                args.Add($"-preset {settings.Preset}");
                args.Add($"-crf {settings.Quality}");

                if (settings.VideoCodec == "libx265")
                {
                    args.Add("-tag:v hvc1"); // For better compatibility
                }
            }
            else if (settings.VideoCodec == "h264_nvenc" || settings.VideoCodec == "hevc_nvenc")
            {
                args.Add($"-preset {settings.HardwarePreset}");
                args.Add($"-cq {settings.Quality}");
            }
            else if (settings.VideoCodec == "prores_ks")
            {
                args.Add($"-profile:v {settings.ProResProfile}");
            }

            // Resolution
            if (settings.Width > 0 && settings.Height > 0)
            {
                args.Add($"-vf scale={settings.Width}:{settings.Height}");
            }

            // Frame rate
            if (settings.FrameRate > 0)
            {
                args.Add($"-r {settings.FrameRate}");
            }

            // Pixel format
            if (!string.IsNullOrEmpty(settings.PixelFormat))
            {
                args.Add($"-pix_fmt {settings.PixelFormat}");
            }
        }
        else
        {
            args.Add("-vn"); // No video
        }

        // Audio codec settings
        if (settings.AudioEnabled)
        {
            args.Add($"-c:a {settings.AudioCodec}");

            if (settings.AudioCodec == "aac" || settings.AudioCodec == "libmp3lame")
            {
                args.Add($"-b:a {settings.AudioBitrate}k");
            }
            else if (settings.AudioCodec == "flac" || settings.AudioCodec == "pcm_s16le")
            {
                // Lossless - no bitrate needed
            }

            if (settings.AudioSampleRate > 0)
            {
                args.Add($"-ar {settings.AudioSampleRate}");
            }
        }
        else
        {
            args.Add("-an"); // No audio
        }

        // Container-specific options
        if (settings.OutputPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            args.Add("-movflags +faststart"); // Web optimization
        }

        args.Add($"\"{settings.OutputPath}\"");

        return string.Join(" ", args);
    }

    public async Task<double> GetDurationAsync(string filePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return 0;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (double.TryParse(output.Trim(), out var duration))
            {
                return duration;
            }
        }
        catch { }

        return 0;
    }

    public void Cancel()
    {
        _isCancelled = true;
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            try
            {
                _currentProcess.Kill();
            }
            catch { }
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
