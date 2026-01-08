using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class ThumbnailService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _semaphore = new(4); // Limit concurrent operations

    public ThumbnailService()
    {
        // Look for FFmpeg in common locations
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));

        _ffmpegPath = FindExecutable("ffmpeg.exe", distPath);
        _ffprobePath = FindExecutable("ffprobe.exe", distPath);

        _cacheDir = Path.Combine(Path.GetTempPath(), "VapourSynthStudio", "thumbnails");
        Directory.CreateDirectory(_cacheDir);
    }

    private static string FindExecutable(string name, string distPath)
    {
        // Check dist/ffmpeg folder
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");
        if (Directory.Exists(ffmpegDir))
        {
            var inFfmpegDir = Path.Combine(ffmpegDir, name);
            if (File.Exists(inFfmpegDir)) return inFfmpegDir;

            // Check bin subfolder
            var inBin = Path.Combine(ffmpegDir, "bin", name);
            if (File.Exists(inBin)) return inBin;
        }

        // Check PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, name);
            if (File.Exists(fullPath)) return fullPath;
        }

        return name; // Fall back to just the name, hope it's in PATH
    }

    public bool IsAvailable => File.Exists(_ffmpegPath) || _ffmpegPath == "ffmpeg.exe";

    public async Task<MediaInfo?> GetMediaInfoAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        await _semaphore.WaitAsync();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrEmpty(output)) return null;

            return ParseMediaInfo(output);
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static MediaInfo? ParseMediaInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var info = new MediaInfo();

            // Parse format
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var dur) &&
                    double.TryParse(dur.GetString(), out var duration))
                {
                    info.Duration = duration;
                }

                if (format.TryGetProperty("size", out var size) &&
                    long.TryParse(size.GetString(), out var fileSize))
                {
                    info.FileSize = fileSize;
                }
            }

            // Parse streams
            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (!stream.TryGetProperty("codec_type", out var codecType))
                        continue;

                    var type = codecType.GetString();

                    if (type == "video" && info.Width == 0)
                    {
                        info.HasVideo = true;

                        if (stream.TryGetProperty("width", out var w))
                            info.Width = w.GetInt32();

                        if (stream.TryGetProperty("height", out var h))
                            info.Height = h.GetInt32();

                        if (stream.TryGetProperty("codec_name", out var codec))
                            info.VideoCodec = codec.GetString() ?? "";

                        if (stream.TryGetProperty("r_frame_rate", out var fps))
                        {
                            var fpsStr = fps.GetString() ?? "0/1";
                            var parts = fpsStr.Split('/');
                            if (parts.Length == 2 &&
                                double.TryParse(parts[0], out var num) &&
                                double.TryParse(parts[1], out var den) &&
                                den > 0)
                            {
                                info.FrameRate = num / den;
                            }
                        }

                        if (stream.TryGetProperty("nb_frames", out var frames) &&
                            int.TryParse(frames.GetString(), out var frameCount))
                        {
                            info.FrameCount = frameCount;
                        }
                    }
                    else if (type == "audio" && !info.HasAudio)
                    {
                        info.HasAudio = true;

                        if (stream.TryGetProperty("codec_name", out var codec))
                            info.AudioCodec = codec.GetString() ?? "";
                    }
                }
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    public async Task<BitmapSource?> GenerateThumbnailAsync(string filePath, int width = 160, int height = 90)
    {
        if (!File.Exists(filePath)) return null;

        // Create cache key based on file path and modification time
        var fileInfo = new FileInfo(filePath);
        var cacheKey = $"{filePath}_{fileInfo.LastWriteTimeUtc.Ticks}_{width}x{height}".GetHashCode();
        var cachePath = Path.Combine(_cacheDir, $"{cacheKey:X8}.jpg");

        // Check cache
        if (File.Exists(cachePath))
        {
            return await LoadThumbnailFromFileAsync(cachePath);
        }

        await _semaphore.WaitAsync();
        try
        {
            // Generate thumbnail with FFmpeg
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{filePath}\" -ss 00:00:01 -vframes 1 -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:black\" -y \"{cachePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Read stderr to prevent blocking
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (File.Exists(cachePath))
            {
                return await LoadThumbnailFromFileAsync(cachePath);
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<BitmapSource?> LoadThumbnailFromFileAsync(string path)
    {
        try
        {
            return await Task.Run(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            });
        }
        catch
        {
            return null;
        }
    }

    public static BitmapSource? LoadImageThumbnail(string filePath, int maxSize = 160)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = maxSize;
            bitmap.UriSource = new Uri(filePath);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                Directory.Delete(_cacheDir, true);
                Directory.CreateDirectory(_cacheDir);
            }
        }
        catch { }
    }
}

public class MediaInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double Duration { get; set; }
    public double FrameRate { get; set; }
    public int FrameCount { get; set; }
    public long FileSize { get; set; }
    public string VideoCodec { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
}
