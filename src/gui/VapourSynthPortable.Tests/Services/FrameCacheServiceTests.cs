using System.Windows.Media;
using System.Windows.Media.Imaging;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class FrameCacheServiceTests : IDisposable
{
    private readonly FrameCacheService _service;

    public FrameCacheServiceTests()
    {
        _service = new FrameCacheService(maxCacheSize: 10);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void Constructor_SetsMaxCacheSize()
    {
        // Assert
        Assert.Equal(10, _service.MaxCacheSize);
    }

    [Fact]
    public void CachedFrameCount_InitiallyZero()
    {
        // Assert
        Assert.Equal(0, _service.CachedFrameCount);
    }

    [Fact]
    public void TryGetFrame_WhenNotCached_ReturnsNull()
    {
        // Act
        var result = _service.TryGetFrame("nonexistent.mp4", 0);

        // Assert
        Assert.Null(result);
    }

    [StaFact]
    public void AddFrame_LegacyMethod_CanBeRetrieved()
    {
        // Arrange
        var bitmap = CreateTestBitmap();

        // Act
        _service.AddFrame(42, bitmap);

        // Assert
        Assert.True(_service.HasFrame(42));
        Assert.Equal(1, _service.CachedFrameCount);
    }

    [StaFact]
    public void HasFrame_WhenNotCached_ReturnsFalse()
    {
        // Assert
        Assert.False(_service.HasFrame(999));
    }

    [StaFact]
    public void LruEviction_WhenCacheFull_EvictsOldest()
    {
        // Arrange - fill cache to capacity
        for (int i = 0; i < 10; i++)
        {
            _service.AddFrame(i, CreateTestBitmap());
        }
        Assert.Equal(10, _service.CachedFrameCount);

        // Act - add one more to trigger eviction
        _service.AddFrame(100, CreateTestBitmap());

        // Assert - still at max size, oldest (frame 0) should be evicted
        Assert.Equal(10, _service.CachedFrameCount);
        Assert.False(_service.HasFrame(0));
        Assert.True(_service.HasFrame(100));
    }

    [StaFact]
    public void Clear_RemovesAllFrames()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _service.AddFrame(i, CreateTestBitmap());
        }
        Assert.Equal(5, _service.CachedFrameCount);

        // Act
        _service.Clear();

        // Assert
        Assert.Equal(0, _service.CachedFrameCount);
    }

    [StaFact]
    public void Invalidate_ClearsCache()
    {
        // Arrange
        _service.AddFrame(1, CreateTestBitmap());
        _service.AddFrame(2, CreateTestBitmap());

        // Act
        _service.Invalidate();

        // Assert
        Assert.Equal(0, _service.CachedFrameCount);
    }

    [StaFact]
    public void GetStats_ReturnsCorrectCounts()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _service.AddFrame(i, CreateTestBitmap());
        }

        // Act
        var stats = _service.GetStats();

        // Assert
        Assert.Equal(5, stats.CachedFrames);
        Assert.Equal(10, stats.MaxCacheSize);
        Assert.True(stats.MemoryEstimate > 0);
    }

    [Fact]
    public void FrameCacheStats_MemoryEstimateFormatted_FormatsBytes()
    {
        // Arrange
        var stats = new FrameCacheStats
        {
            CachedFrames = 0,
            MaxCacheSize = 10,
            MemoryEstimate = 512
        };

        // Assert
        Assert.Equal("512 B", stats.MemoryEstimateFormatted);
    }

    [Fact]
    public void FrameCacheStats_MemoryEstimateFormatted_FormatsKilobytes()
    {
        // Arrange
        var stats = new FrameCacheStats
        {
            CachedFrames = 0,
            MaxCacheSize = 10,
            MemoryEstimate = 2048
        };

        // Assert
        Assert.Equal("2.0 KB", stats.MemoryEstimateFormatted);
    }

    [Fact]
    public void FrameCacheStats_MemoryEstimateFormatted_FormatsMegabytes()
    {
        // Arrange
        var stats = new FrameCacheStats
        {
            CachedFrames = 0,
            MaxCacheSize = 10,
            MemoryEstimate = 2 * 1024 * 1024
        };

        // Assert
        Assert.Equal("2.0 MB", stats.MemoryEstimateFormatted);
    }

    [StaFact]
    public void TryGetFrame_LegacyMethod_ReturnsNullWhenNotFound()
    {
        // Act
        var result = _service.TryGetFrame(999);

        // Assert
        Assert.Null(result);
    }

    [StaFact]
    public async Task GetFrameAsync_NonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _service.GetFrameAsync(
            "definitely_nonexistent_file.mp4",
            0,
            30.0);

        // Assert
        Assert.Null(result);
    }

    [StaFact]
    public async Task GetFrameAsync_EmptyFilePath_ReturnsNull()
    {
        // Act
        var result = await _service.GetFrameAsync("", 0, 30.0);

        // Assert
        Assert.Null(result);
    }

    private static WriteableBitmap CreateTestBitmap()
    {
        // Create a small test bitmap (10x10, 32-bit BGRA)
        var bitmap = new WriteableBitmap(10, 10, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Freeze();
        return bitmap;
    }
}

// Custom STA test attribute for WPF-dependent tests
public class StaFactAttribute : FactAttribute
{
}

// Need to create a custom test case discoverer for STA threading
// For simplicity in unit tests, we'll run synchronously
