using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for generating quick single-frame previews using VapourSynth
/// </summary>
public class QuickPreviewService
{
    private static readonly ILogger<QuickPreviewService> _logger = LoggingService.GetLogger<QuickPreviewService>();

    private readonly string _projectRoot;
    private readonly string _vspipePath;
    private readonly string _pythonPath;
    private readonly string _vapourSynthPath;
    private readonly string _pluginsPath;
    private readonly string _ffmpegPath;

    private readonly Dictionary<string, BitmapSource> _previewCache = new();
    private const int MaxCacheSize = 50;

    public bool IsAvailable => File.Exists(_vspipePath);

    public QuickPreviewService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        var distPath = Path.Combine(_projectRoot, "dist");
        _vspipePath = Path.Combine(distPath, "vapoursynth", "VSPipe.exe");
        _pythonPath = Path.Combine(distPath, "python");
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pluginsPath = Path.Combine(distPath, "plugins");
        _ffmpegPath = FindFFmpegPath(distPath);

        _logger.LogInformation("QuickPreviewService initialized. Available: {IsAvailable}", IsAvailable);
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string FindFFmpegPath(string distPath)
    {
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");
        if (Directory.Exists(ffmpegDir))
        {
            var direct = Path.Combine(ffmpegDir, "ffmpeg.exe");
            if (File.Exists(direct)) return direct;

            var inBin = Path.Combine(ffmpegDir, "bin", "ffmpeg.exe");
            if (File.Exists(inBin)) return inBin;
        }
        return "ffmpeg.exe";
    }

    /// <summary>
    /// Generate a quick preview frame using a restoration preset
    /// </summary>
    /// <param name="sourcePath">Source video path</param>
    /// <param name="preset">Restoration preset to apply</param>
    /// <param name="frameNumber">Frame number to extract</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Processed frame as BitmapSource, or null on failure</returns>
    public async Task<BitmapSource?> GeneratePreviewAsync(
        string sourcePath,
        RestorePreset preset,
        long frameNumber,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            _logger.LogWarning("Source file not found: {SourcePath}", sourcePath);
            return null;
        }

        // Check cache first
        var cacheKey = $"{sourcePath}|{preset.Name}|{frameNumber}";
        if (_previewCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Returning cached preview for {CacheKey}", cacheKey);
            return cached;
        }

        if (!IsAvailable)
        {
            _logger.LogWarning("VSPipe not available, generating simple preview");
            return await GenerateSimplePreviewAsync(sourcePath, frameNumber, ct);
        }

        var stopwatch = Stopwatch.StartNew();
        string? tempScript = null;
        string? tempOutput = null;

        try
        {
            // Generate temporary script
            tempScript = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid():N}.vpy");
            tempOutput = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid():N}.png");

            var script = GenerateSingleFrameScript(sourcePath, preset, frameNumber);
            await File.WriteAllTextAsync(tempScript, script, ct);

            _logger.LogDebug("Running quick preview for frame {Frame}", frameNumber);

            // Run VSPipe to output single frame
            var success = await RunVSPipeForFrameAsync(tempScript, tempOutput, ct);

            if (!success || !File.Exists(tempOutput))
            {
                _logger.LogWarning("VSPipe failed, falling back to simple preview");
                return await GenerateSimplePreviewAsync(sourcePath, frameNumber, ct);
            }

            // Load the output image
            var bitmap = await LoadBitmapAsync(tempOutput, ct);

            if (bitmap != null)
            {
                // Add to cache
                AddToCache(cacheKey, bitmap);
            }

            stopwatch.Stop();
            _logger.LogInformation("Quick preview generated in {Elapsed:F2}s", stopwatch.Elapsed.TotalSeconds);

            return bitmap;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Preview generation cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview");
            return await GenerateSimplePreviewAsync(sourcePath, frameNumber, ct);
        }
        finally
        {
            // Cleanup temp files
            TryDeleteFile(tempScript);
            TryDeleteFile(tempOutput);
        }
    }

    /// <summary>
    /// Generate original frame preview (without restoration effects)
    /// </summary>
    public async Task<BitmapSource?> GenerateOriginalPreviewAsync(
        string sourcePath,
        long frameNumber,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return null;

        // Check cache
        var cacheKey = $"{sourcePath}|ORIGINAL|{frameNumber}";
        if (_previewCache.TryGetValue(cacheKey, out var cached))
            return cached;

        return await GenerateSimplePreviewAsync(sourcePath, frameNumber, ct);
    }

    private string GenerateSingleFrameScript(string sourcePath, RestorePreset preset, long frameNumber)
    {
        var escapedPath = sourcePath.Replace("'", "\\'").Replace("\\", "\\\\");

        // Use the preset's GenerateScript() method which handles parameter substitution
        var presetScript = preset.GenerateScript();

        return $@"
import vapoursynth as vs
core = vs.core

# Load source
video_in = core.lsmas.LWLibavSource(r'{escapedPath}')

# Trim to single frame for fast processing
video_in = video_in[{frameNumber}:{frameNumber + 1}]

# Apply restoration preset
{presetScript}
";
    }

    private async Task<bool> RunVSPipeForFrameAsync(string scriptPath, string outputPath, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _vspipePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _projectRoot
        };

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("y4m");
        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add("0");  // Start frame
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("0");  // End frame (0 = first frame only)
        startInfo.ArgumentList.Add("-");
        startInfo.ArgumentList.Add(scriptPath);

        // Set up environment for portable VapourSynth
        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        startInfo.EnvironmentVariables["PATH"] = $"{_pythonPath};{_vapourSynthPath};{_pluginsPath};{existingPath}";
        startInfo.EnvironmentVariables["PYTHONPATH"] = _vapourSynthPath;
        startInfo.EnvironmentVariables["PYTHONHOME"] = _pythonPath;

        // FFmpeg to convert y4m to PNG
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-y -i - -frames:v 1 \"{outputPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var vspipe = new Process { StartInfo = startInfo };
            using var ffmpeg = new Process { StartInfo = ffmpegStartInfo };

            vspipe.Start();
            ffmpeg.Start();

            // Pipe VSPipe output to FFmpeg input
            var pipeTask = Task.Run(async () =>
            {
                try
                {
                    await vspipe.StandardOutput.BaseStream.CopyToAsync(
                        ffmpeg.StandardInput.BaseStream, ct);
                    ffmpeg.StandardInput.Close();
                }
                catch { }
            }, ct);

            // Wait with timeout (5 seconds max)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await Task.Run(() => vspipe.WaitForExit(5000), linkedCts.Token);
                await Task.Run(() => ffmpeg.WaitForExit(5000), linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(vspipe);
                TryKillProcess(ffmpeg);
                return false;
            }

            return vspipe.ExitCode == 0 && ffmpeg.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running VSPipe/FFmpeg pipeline");
            return false;
        }
    }

    private async Task<BitmapSource?> GenerateSimplePreviewAsync(string sourcePath, long frameNumber, CancellationToken ct)
    {
        // Fall back to FFmpeg for simple frame extraction
        var tempOutput = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid():N}.png");

        try
        {
            var fps = 24.0; // Assume 24fps if unknown
            var timestamp = TimeSpan.FromSeconds(frameNumber / fps);

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-ss {timestamp:hh\\:mm\\:ss\\.fff} -i \"{sourcePath}\" -frames:v 1 -y \"{tempOutput}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await Task.Run(() => process.WaitForExit(5000), ct);

            if (process.ExitCode == 0 && File.Exists(tempOutput))
            {
                return await LoadBitmapAsync(tempOutput, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate simple preview");
        }
        finally
        {
            TryDeleteFile(tempOutput);
        }

        return null;
    }

    private async Task<BitmapSource?> LoadBitmapAsync(string path, CancellationToken ct)
    {
        try
        {
            return await Task.Run(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze(); // Make thread-safe
                return (BitmapSource)bitmap;
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load bitmap from {Path}", path);
            return null;
        }
    }

    private void AddToCache(string key, BitmapSource bitmap)
    {
        // Simple cache eviction
        if (_previewCache.Count >= MaxCacheSize)
        {
            var oldest = _previewCache.Keys.First();
            _previewCache.Remove(oldest);
        }

        _previewCache[key] = bitmap;
    }

    /// <summary>
    /// Clear the preview cache
    /// </summary>
    public void ClearCache()
    {
        _previewCache.Clear();
    }

    /// <summary>
    /// Clear cache for a specific source file
    /// </summary>
    public void ClearCacheForFile(string sourcePath)
    {
        var keysToRemove = _previewCache.Keys
            .Where(k => k.StartsWith(sourcePath))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _previewCache.Remove(key);
        }
    }

    private void TryKillProcess(Process? process)
    {
        try
        {
            if (process != null && !process.HasExited)
                process.Kill();
        }
        catch { }
    }

    private void TryDeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
