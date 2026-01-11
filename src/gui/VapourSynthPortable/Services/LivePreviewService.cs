using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Live preview service with debouncing for real-time node editor updates
/// </summary>
public class LivePreviewService : ILivePreviewService
{
    private static readonly ILogger<LivePreviewService> _logger = LoggingService.GetLogger<LivePreviewService>();

    private readonly string _projectRoot;
    private readonly string _vspipePath;
    private readonly string _pythonPath;
    private readonly string _vapourSynthPath;
    private readonly string _pluginsPath;
    private readonly string _ffmpegPath;

    private readonly ConcurrentDictionary<string, BitmapSource> _previewCache = new();
    private const int MaxCacheSize = 10;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(150);

    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _activeCts;
    private readonly object _lock = new();

    public bool IsAvailable => File.Exists(_vspipePath);
    public bool IsGenerating { get; private set; }

    public event EventHandler? PreviewStarted;
    public event EventHandler<BitmapSource?>? PreviewCompleted;

    public LivePreviewService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        var distPath = Path.Combine(_projectRoot, "dist");
        _vspipePath = Path.Combine(distPath, "vapoursynth", "VSPipe.exe");
        _pythonPath = Path.Combine(distPath, "python");
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pluginsPath = Path.Combine(distPath, "plugins");
        _ffmpegPath = FindFFmpegPath(distPath);

        _logger.LogInformation("LivePreviewService initialized. Available: {IsAvailable}", IsAvailable);
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

    public void RequestPreview(string script, int frameNumber, string? sourcePath = null)
    {
        lock (_lock)
        {
            // Cancel any pending debounce
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var ct = _debounceCts.Token;

            // Start debounced preview generation
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for debounce delay
                    await Task.Delay(DebounceDelay, ct);

                    // Generate preview
                    var result = await GeneratePreviewAsync(script, frameNumber, ct);

                    // Raise completion event on UI thread
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        PreviewCompleted?.Invoke(this, result);
                    });
                }
                catch (OperationCanceledException)
                {
                    // Debounce cancelled, ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in debounced preview generation");
                }
            });
        }
    }

    public async Task<BitmapSource?> GeneratePreviewAsync(string script, int frameNumber, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("VapourSynth not available for live preview");
            return null;
        }

        // Cancel any active generation
        lock (_lock)
        {
            _activeCts?.Cancel();
            _activeCts = new CancellationTokenSource();
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _activeCts.Token);

        // Check cache first
        var cacheKey = $"{script.GetHashCode()}|{frameNumber}";
        if (_previewCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Returning cached live preview");
            return cached;
        }

        IsGenerating = true;
        PreviewStarted?.Invoke(this, EventArgs.Empty);

        var stopwatch = Stopwatch.StartNew();
        string? tempScript = null;
        string? tempOutput = null;

        try
        {
            tempScript = Path.Combine(Path.GetTempPath(), $"livepreview_{Guid.NewGuid():N}.vpy");
            tempOutput = Path.Combine(Path.GetTempPath(), $"livepreview_{Guid.NewGuid():N}.png");

            // Wrap script to output specific frame
            var wrappedScript = WrapScriptForFrame(script, frameNumber);
            await File.WriteAllTextAsync(tempScript, wrappedScript, linkedCts.Token);

            _logger.LogDebug("Generating live preview for frame {Frame}", frameNumber);

            var success = await RunVSPipeAsync(tempScript, tempOutput, linkedCts.Token);

            if (!success || !File.Exists(tempOutput))
            {
                _logger.LogWarning("Live preview generation failed");
                return null;
            }

            var bitmap = await LoadBitmapAsync(tempOutput, linkedCts.Token);

            if (bitmap != null)
            {
                AddToCache(cacheKey, bitmap);
            }

            stopwatch.Stop();
            _logger.LogInformation("Live preview generated in {Elapsed:F2}ms", stopwatch.Elapsed.TotalMilliseconds);

            return bitmap;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Live preview generation cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate live preview");
            return null;
        }
        finally
        {
            IsGenerating = false;
            TryDeleteFile(tempScript);
            TryDeleteFile(tempOutput);
        }
    }

    private string WrapScriptForFrame(string script, int frameNumber)
    {
        // The script should already define `video_out` or call `set_output()`
        // We just need to ensure it outputs the correct frame
        return $@"{script}

# Live preview: output single frame
if 'video_out' in dir():
    video_out = video_out[{frameNumber}:{frameNumber + 1}]
    video_out.set_output()
";
    }

    private async Task<bool> RunVSPipeAsync(string scriptPath, string outputPath, CancellationToken ct)
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
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("-");
        startInfo.ArgumentList.Add(scriptPath);

        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        startInfo.EnvironmentVariables["PATH"] = $"{_pythonPath};{_vapourSynthPath};{_pluginsPath};{existingPath}";
        startInfo.EnvironmentVariables["PYTHONPATH"] = _vapourSynthPath;
        startInfo.EnvironmentVariables["PYTHONHOME"] = _pythonPath;

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

            var pipeTask = Task.Run(async () =>
            {
                try
                {
                    await vspipe.StandardOutput.BaseStream.CopyToAsync(
                        ffmpeg.StandardInput.BaseStream, ct);
                    ffmpeg.StandardInput.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Pipe transfer interrupted during preview generation");
                }
            }, ct);

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
                bitmap.Freeze();
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
        if (_previewCache.Count >= MaxCacheSize)
        {
            // Remove oldest entry
            var oldest = _previewCache.Keys.FirstOrDefault();
            if (oldest != null)
            {
                _previewCache.TryRemove(oldest, out _);
            }
        }

        _previewCache[key] = bitmap;
    }

    public void Cancel()
    {
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _activeCts?.Cancel();
        }
    }

    public void ClearCache()
    {
        _previewCache.Clear();
    }

    private void TryKillProcess(Process? process)
    {
        try
        {
            if (process != null && !process.HasExited)
                process.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Process already terminated or could not be killed");
        }
    }

    private void TryDeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete temp file: {Path}", path);
        }
    }
}
