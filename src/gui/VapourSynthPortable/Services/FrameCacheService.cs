using System.Windows.Media.Imaging;

namespace VapourSynthPortable.Services;

/// <summary>
/// LRU cache for decoded video frames.
/// </summary>
public class FrameCacheService
{
    private readonly int _maxCacheSize;
    private readonly Dictionary<int, WriteableBitmap> _cache = new();
    private readonly LinkedList<int> _accessOrder = new();
    private readonly object _lock = new();

    public FrameCacheService(int maxCacheSize = 50)
    {
        _maxCacheSize = maxCacheSize;
    }

    public int CachedFrameCount
    {
        get { lock (_lock) { return _cache.Count; } }
    }

    public int MaxCacheSize => _maxCacheSize;

    /// <summary>
    /// Try to get a cached frame.
    /// </summary>
    public WriteableBitmap? TryGetFrame(int frameNumber)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(frameNumber, out var bitmap))
            {
                // Move to front of access order (most recently used)
                _accessOrder.Remove(frameNumber);
                _accessOrder.AddFirst(frameNumber);
                return bitmap;
            }
            return null;
        }
    }

    /// <summary>
    /// Add a frame to the cache.
    /// </summary>
    public void AddFrame(int frameNumber, WriteableBitmap bitmap)
    {
        lock (_lock)
        {
            // If already in cache, just update access order
            if (_cache.ContainsKey(frameNumber))
            {
                _accessOrder.Remove(frameNumber);
                _accessOrder.AddFirst(frameNumber);
                return;
            }

            // Evict oldest frames if at capacity
            while (_cache.Count >= _maxCacheSize && _accessOrder.Count > 0)
            {
                var oldest = _accessOrder.Last!.Value;
                _accessOrder.RemoveLast();
                _cache.Remove(oldest);
            }

            // Add new frame
            _cache[frameNumber] = bitmap;
            _accessOrder.AddFirst(frameNumber);
        }
    }

    /// <summary>
    /// Check if a frame is cached.
    /// </summary>
    public bool HasFrame(int frameNumber)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(frameNumber);
        }
    }

    /// <summary>
    /// Clear all cached frames.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _accessOrder.Clear();
        }
    }

    /// <summary>
    /// Invalidate the cache (call when script changes).
    /// </summary>
    public void Invalidate()
    {
        Clear();
    }
}
