using System.Collections.ObjectModel;

namespace VapourSynthPortable.Tests.Services;

public class MediaPoolServiceTests
{
    private readonly MediaPoolService _service;

    public MediaPoolServiceTests()
    {
        _service = new MediaPoolService();
    }

    #region Instantiation Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new MediaPoolService();
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesMediaPool()
    {
        // Assert
        _service.MediaPool.Should().NotBeNull();
        _service.MediaPool.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCurrentSourceAsNull()
    {
        // Assert
        _service.CurrentSource.Should().BeNull();
        _service.HasSource.Should().BeFalse();
    }

    #endregion

    #region SetCurrentSource Tests

    [Fact]
    public void SetCurrentSource_SetsCurrentSource()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };

        // Act
        _service.SetCurrentSource(item);

        // Assert
        _service.CurrentSource.Should().Be(item);
        _service.HasSource.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentSource_AddsToPoolIfNotPresent()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Should().BeEmpty();

        // Act
        _service.SetCurrentSource(item);

        // Assert
        _service.MediaPool.Should().Contain(item);
    }

    [Fact]
    public void SetCurrentSource_DoesNotDuplicateIfAlreadyInPool()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Add(item);
        var initialCount = _service.MediaPool.Count;

        // Act
        _service.SetCurrentSource(item);

        // Assert
        _service.MediaPool.Count.Should().Be(initialCount);
    }

    [Fact]
    public void SetCurrentSource_ToNull_ClearsCurrentSource()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.SetCurrentSource(item);

        // Act
        _service.SetCurrentSource(null);

        // Assert
        _service.CurrentSource.Should().BeNull();
        _service.HasSource.Should().BeFalse();
    }

    [Fact]
    public void SetCurrentSource_ResetsPlayheadPosition()
    {
        // Arrange
        _service.PlayheadPosition = 100;
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };

        // Act
        _service.SetCurrentSource(item);

        // Assert
        _service.PlayheadPosition.Should().Be(0);
    }

    [Fact]
    public void SetCurrentSource_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.CurrentSourceChanged += (s, e) => eventRaised = true;
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };

        // Act
        _service.SetCurrentSource(item);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region SetCurrentSourceByPath Tests

    [Fact]
    public void SetCurrentSourceByPath_SelectsItemFromPool()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Add(item);

        // Act
        _service.SetCurrentSourceByPath(@"C:\test\video.mp4");

        // Assert
        _service.CurrentSource.Should().Be(item);
    }

    [Fact]
    public void SetCurrentSourceByPath_IsCaseInsensitive()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Add(item);

        // Act
        _service.SetCurrentSourceByPath(@"C:\TEST\VIDEO.MP4");

        // Assert
        _service.CurrentSource.Should().Be(item);
    }

    [Fact]
    public void SetCurrentSourceByPath_DoesNothingIfNotFound()
    {
        // Arrange - pool is empty

        // Act
        _service.SetCurrentSourceByPath(@"C:\test\nonexistent.mp4");

        // Assert
        _service.CurrentSource.Should().BeNull();
    }

    #endregion

    #region RemoveMedia Tests

    [Fact]
    public void RemoveMedia_RemovesFromPool()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Add(item);

        // Act
        _service.RemoveMedia(item);

        // Assert
        _service.MediaPool.Should().NotContain(item);
    }

    [Fact]
    public void RemoveMedia_ClearsCurrentSourceIfRemoved()
    {
        // Arrange
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.SetCurrentSource(item);

        // Act
        _service.RemoveMedia(item);

        // Assert
        _service.CurrentSource.Should().BeNull();
    }

    [Fact]
    public void RemoveMedia_RaisesMediaPoolChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        var item = new MediaItem { Name = "Test", FilePath = @"C:\test\video.mp4" };
        _service.MediaPool.Add(item);
        _service.MediaPoolChanged += (s, e) => eventRaised = true;

        // Act
        _service.RemoveMedia(item);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region ClearPool Tests

    [Fact]
    public void ClearPool_RemovesAllItems()
    {
        // Arrange
        _service.MediaPool.Add(new MediaItem { Name = "Item1" });
        _service.MediaPool.Add(new MediaItem { Name = "Item2" });
        _service.MediaPool.Add(new MediaItem { Name = "Item3" });

        // Act
        _service.ClearPool();

        // Assert
        _service.MediaPool.Should().BeEmpty();
    }

    [Fact]
    public void ClearPool_ClearsCurrentSource()
    {
        // Arrange
        var item = new MediaItem { Name = "Test" };
        _service.SetCurrentSource(item);

        // Act
        _service.ClearPool();

        // Assert
        _service.CurrentSource.Should().BeNull();
    }

    [Fact]
    public void ClearPool_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.MediaPoolChanged += (s, e) => eventRaised = true;

        // Act
        _service.ClearPool();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region ImportMediaAsync Tests

    [Fact]
    public async Task ImportMediaAsync_ReturnsNull_ForNullPath()
    {
        // Act
        var result = await _service.ImportMediaAsync((string)null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportMediaAsync_ReturnsNull_ForEmptyPath()
    {
        // Act
        var result = await _service.ImportMediaAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportMediaAsync_ReturnsNull_ForNonExistentFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mp4");

        // Act
        var result = await _service.ImportMediaAsync(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PlayheadPosition Tests

    [Fact]
    public void PlayheadPosition_CanBeSet()
    {
        // Act
        _service.PlayheadPosition = 50.5;

        // Assert
        _service.PlayheadPosition.Should().Be(50.5);
    }

    [Fact]
    public void PlayheadPosition_RaisesEvent()
    {
        // Arrange
        double? eventValue = null;
        _service.PlayheadPositionChanged += (s, e) => eventValue = e;

        // Act
        _service.PlayheadPosition = 25.0;

        // Assert
        eventValue.Should().Be(25.0);
    }

    #endregion

    #region InPoint/OutPoint Tests

    [Fact]
    public void InPoint_CanBeSet()
    {
        // Act
        _service.InPoint = 10.0;

        // Assert
        _service.InPoint.Should().Be(10.0);
    }

    [Fact]
    public void OutPoint_CanBeSet()
    {
        // Act
        _service.OutPoint = 100.0;

        // Assert
        _service.OutPoint.Should().Be(100.0);
    }

    #endregion
}
