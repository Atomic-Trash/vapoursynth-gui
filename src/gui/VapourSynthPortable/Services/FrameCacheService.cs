using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for caching video frames with LRU eviction for timeline preview.
/// Extracts frames using FFmpeg and caches them for quick scrubbing.
/// </summary>
public class FrameCacheService : IDisposable
{
    private static readonly ILogger<FrameCacheService> _logger = LoggingService.GetLogger<FrameCacheService>();

    private readonly string _ffmpegPath;
    private readonly int _maxCacheSize;
    private readonly ConcurrentDictionary<string, CachedFrame> _cache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly object _accessLock = new();
    private readonly SemaphoreSlim _extractionSemaphore;
    private readonly ConcurrentDictionary<string, Task<BitmapSource?>> _pendingExtractions = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a frame is loaded into cache
    /// </summary>
    public event EventHandler<FrameCachedEventArgs>? FrameCached;

    public int CachedFrameCount => _cache.Count;
    public int MaxCacheSize => _maxCacheSize;

    /// <summary>
    /// Creates a new FrameCacheService
    /// </summary>
    /// <param name="maxCacheSize">Maximum number of frames to cache (default 100)</param>
    /// <param name="maxConcurrentExtractions">Max concurrent FFmpeg extractions (default 4)</param>
    public FrameCacheService(int maxCacheSize = 100, int maxConcurrentExtractions = 4)
    {
        _maxCacheSize = maxCacheSize;
        _extractionSemaphore = new SemaphoreSlim(maxConcurrentExtractions);

        // Find FFmpeg
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));
        _ffmpegPath = FindFFmpegPath(distPath);

        _logger.LogInformation("FrameCacheService initialized. FFmpeg: {FFmpegPath}, MaxCache: {MaxCache}",
            _ffmpegPath, _maxCacheSize);
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

        // Check PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(fullPath)) return fullPath;
        }

        return "ffmpeg.exe";
    }

    /// <summary>
    /// Generate a cache key for a frame
    /// </summary>
    private static string GetCacheKey(string filePath, long frameNumber, int width, int height)
    {
        return $"{filePath}|{frameNumber}|{width}x{height}";
    }

    /// <summary>
    /// Try to get a cached frame (synchronous, no extraction)
    /// </summary>
    public BitmapSource? TryGetFrame(string filePath, long frameNumber, int width = 320, int height = 180)
    {
        var cacheKey = GetCacheKey(filePath, frameNumber, width, height);

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            UpdateAccessOrder(cacheKey);
            return cached.Frame;
        }

        return null;
    }

    /// <summary>
    /// Legacy method for simple frame number lookups
    /// </summary>
    public WriteableBitmap? TryGetFrame(int frameNumber)
    {
        // For backwards compatibility - look for any frame with this number
        var matchingKey = _cache.Keys.FirstOrDefault(k => k.Contains($"|{frameNumber}|"));
        if (matchingKey != null && _cache.TryGetValue(matchingKey, out var cached))
        {
            UpdateAccessOrder(matchingKey);
            return cached.Frame as WriteableBitmap;
        }
        return null;
    }

    /// <summary>
    /// Legacy method to add frame by number only
    /// </summary>
    public void AddFrame(int frameNumber, WriteableBitmap bitmap)
    {
        var cacheKey = $"legacy|{frameNumber}|0x0";
        AddToCache(cacheKey, bitmap, "legacy", frameNumber);
    }

    /// <summary>
    /// Check if a frame is cached
    /// </summary>
    public bool HasFrame(int frameNumber)
    {
        return _cache.Keys.Any(k => k.Contains($"|{frameNumber}|"));
    }

    /// <summary>
    /// Get a frame from cache or extract it asynchronously
    /// </summary>
    /// <param name="filePath">Path to video file</param>
    /// <param name="frameNumber">Frame number to extract</param>
    /// <param name="frameRate">Video frame rate</param>
    /// <param name="width">Target width (default 320)</param>
    /// <param name="height">Target height (default 180)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>BitmapSource of the frame, or null on failure</returns>
    public async Task<BitmapSource?> GetFrameAsync(
        string filePath,
        long frameNumber,
        double frameRate,
        int width = 320,
        int height = 180,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var cacheKey = GetCacheKey(filePath, frameNumber, width, height);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            UpdateAccessOrder(cacheKey);
            return cached.Frame;
        }

        // Check if extraction is already in progress
        if (_pendingExtractions.TryGetValue(cacheKey, out var pendingTask))
        {
            return await pendingTask;
        }

        // Start extraction
        var extractionTask = ExtractFrameInternalAsync(filePath, frameNumber, frameRate, width, height, cacheKey, ct);
        _pendingExtractions[cacheKey] = extractionTask;

        try
        {
            return await extractionTask;
        }
        finally
        {
            _pendingExtractions.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// Prefetch frames around the current position for smooth scrubbing
    /// </summary>
    /// <param name="filePath">Path to video file</param>
    /// <param name="centerFrame">Current frame position</param>
    /// <param name="frameRate">Video frame rate</param>
    /// <param name="radius">Number of frames before/after to prefetch</param>
    /// <param name="width">Target width</param>
    /// <param name="height">Target height</param>
    /// <param name="ct">Cancellation token</param>
    public async Task PrefetchFramesAsync(
        string filePath,
        long centerFrame,
        double frameRate,
        int radius = 5,
        int width = 320,
        int height = 180,
        CancellationToken ct = default)
    {
        var tasks = new List<Task>();

        for (int offset = -radius; offset <= radius; offset++)
        {
            var frame = centerFrame + offset;
            if (frame < 0) continue;

            var cacheKey = GetCacheKey(filePath, frame, width, height);
            if (_cache.ContainsKey(cacheKey)) continue;

            // Fire and forget prefetch (don't wait)
            var task = GetFrameAsync(filePath, frame, frameRate, width, height, ct);
            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during prefetch
        }
    }

    private async Task<BitmapSource?> ExtractFrameInternalAsync(
        string filePath,
        long frameNumber,
        double frameRate,
        int width,
        int height,
        string cacheKey,
        CancellationToken ct)
    {
        await _extractionSemaphore.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring semaphore
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                UpdateAccessOrder(cacheKey);
                return cached.Frame;
            }

            var timestamp = TimeSpan.FromSeconds(frameNumber / frameRate);
            var frame = await ExtractFrameFromVideoAsync(filePath, timestamp, width, height, ct);

            if (frame != null)
            {
                AddToCache(cacheKey, frame, filePath, frameNumber);
            }

            return frame;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract frame {Frame} from {File}", frameNumber, filePath);
            return null;
        }
        finally
        {
            _extractionSemaphore.Release();
        }
    }

    private async Task<BitmapSource?> ExtractFrameFromVideoAsync(
        string filePath,
        TimeSpan timestamp,
        int width,
        int height,
        CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"frame_{Guid.NewGuid():N}.jpg");

        try
        {
            // Use fast seeking: seek before input, then accurate seek after
            var seekTime = timestamp.TotalSeconds > 0.5 ? timestamp.TotalSeconds - 0.5 : 0;
            var accurateSeek = timestamp.TotalSeconds > 0.5 ? 0.5 : timestamp.TotalSeconds;

            var args = seekTime > 0
                ? $"-ss {seekTime:F3} -i \"{filePath}\" -ss {accurateSeek:F3} -vframes 1 -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease\" -q:v 2 -y \"{tempPath}\""
                : $"-i \"{filePath}\" -ss {timestamp.TotalSeconds:F3} -vframes 1 -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease\" -q:v 2 -y \"{tempPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Wait with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout per frame

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch (Exception ex) { _logger.LogDebug(ex, "Process already terminated during cancellation"); }
                throw;
            }

            if (File.Exists(tempPath))
            {
                return await LoadBitmapAsync(tempPath);
            }

            return null;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to delete temp file: {Path}", tempPath);
            }
        }
    }

    private static async Task<BitmapSource?> LoadBitmapAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return (BitmapSource)bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load bitmap from {Path}", path);
                return null;
            }
        });
    }

    private void AddToCache(string key, BitmapSource frame, string filePath, long frameNumber)
    {
        // Evict if necessary
        while (_cache.Count >= _maxCacheSize)
        {
            EvictOldest();
        }

        var cachedFrame = new CachedFrame
        {
            Frame = frame,
            FilePath = filePath,
            FrameNumber = frameNumber,
            CachedAt = DateTime.UtcNow
        };

        if (_cache.TryAdd(key, cachedFrame))
        {
            lock (_accessLock)
            {
                _accessOrder.AddFirst(key);
            }

            FrameCached?.Invoke(this, new FrameCachedEventArgs
            {
                FilePath = filePath,
                FrameNumber = frameNumber,
                Frame = frame
            });
        }
    }

    private void UpdateAccessOrder(string key)
    {
        lock (_accessLock)
        {
            _accessOrder.Remove(key);
            _accessOrder.AddFirst(key);
        }
    }

    private void EvictOldest()
    {
        string? keyToRemove = null;

        lock (_accessLock)
        {
            if (_accessOrder.Count > 0)
            {
                keyToRemove = _accessOrder.Last?.Value;
                if (keyToRemove != null)
                {
                    _accessOrder.RemoveLast();
                }
            }
        }

        if (keyToRemove != null)
        {
            _cache.TryRemove(keyToRemove, out _);
        }
    }

    /// <summary>
    /// Clear all cached frames for a specific file
    /// </summary>
    public void ClearCacheForFile(string filePath)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(filePath + "|")).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
            lock (_accessLock)
            {
                _accessOrder.Remove(key);
            }
        }

        _logger.LogDebug("Cleared {Count} cached frames for: {FilePath}", keysToRemove.Count, filePath);
    }

    /// <summary>
    /// Clear entire cache
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        lock (_accessLock)
        {
            _accessOrder.Clear();
        }
        _logger.LogInformation("Frame cache cleared");
    }

    /// <summary>
    /// Invalidate the cache (call when script changes)
    /// </summary>
    public void Invalidate()
    {
        Clear();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public FrameCacheStats GetStats()
    {
        return new FrameCacheStats
        {
            CachedFrames = _cache.Count,
            MaxCacheSize = _maxCacheSize,
            MemoryEstimate = _cache.Values.Sum(f => EstimateFrameMemory(f.Frame))
        };
    }

    private static long EstimateFrameMemory(BitmapSource frame)
    {
        return (long)(frame.PixelWidth * frame.PixelHeight * 4); // Assume 32bpp
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Clear();
        _extractionSemaphore.Dispose();
    }
}

#region Models

public class CachedFrame
{
    public required BitmapSource Frame { get; init; }
    public required string FilePath { get; init; }
    public required long FrameNumber { get; init; }
    public required DateTime CachedAt { get; init; }
}

public class FrameCachedEventArgs : EventArgs
{
    public required string FilePath { get; init; }
    public required long FrameNumber { get; init; }
    public required BitmapSource Frame { get; init; }
}

public class FrameCacheStats
{
    public int CachedFrames { get; init; }
    public int MaxCacheSize { get; init; }
    public long MemoryEstimate { get; init; }

    public string MemoryEstimateFormatted => MemoryEstimate switch
    {
        < 1024 => $"{MemoryEstimate} B",
        < 1024 * 1024 => $"{MemoryEstimate / 1024.0:F1} KB",
        _ => $"{MemoryEstimate / (1024.0 * 1024.0):F1} MB"
    };
}

#endregion
