using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class MediaViewModelTests
{
    private static Mock<IMediaPoolService> CreateMockMediaPool()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        return mock;
    }

    private static Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        return mock;
    }

    private static UndoService CreateUndoService() => new();

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesBins()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.NotNull(vm.Bins);
        Assert.Contains(vm.Bins, b => b.Name == "All Media");
        Assert.Contains(vm.Bins, b => b.Name == "Video");
        Assert.Contains(vm.Bins, b => b.Name == "Audio");
        Assert.Contains(vm.Bins, b => b.Name == "Images");
    }

    [Fact]
    public void Constructor_SelectsAllMediaBin()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.NotNull(vm.SelectedBin);
        Assert.Equal("All Media", vm.SelectedBin.Name);
    }

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.NotNull(vm.DisplayedItems);
        Assert.NotNull(vm.SelectedItems);
    }

    #endregion

    #region View Mode Tests

    [Theory]
    [InlineData("Grid", ViewMode.Grid)]
    [InlineData("List", ViewMode.List)]
    public void SetViewModeCommand_SetsCorrectMode(string mode, ViewMode expected)
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SetViewModeCommand.Execute(mode);

        // Assert
        Assert.Equal(expected, vm.CurrentViewMode);
    }

    [Fact]
    public void CurrentViewMode_DefaultIsGrid()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal(ViewMode.Grid, vm.CurrentViewMode);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public void SortByColumnCommand_SetsSortColumn()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SortByColumnCommand.Execute("Duration");

        // Assert
        Assert.Equal("Duration", vm.SortColumn);
    }

    [Fact]
    public void SortByColumnCommand_TogglesDirectionOnSameColumn()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // First, set to a non-default column with ascending
        vm.SortByColumnCommand.Execute("Duration");
        Assert.Equal(System.ComponentModel.ListSortDirection.Ascending, vm.SortDirection);

        // Act - Toggle to descending by clicking same column
        vm.SortByColumnCommand.Execute("Duration");

        // Assert
        Assert.Equal(System.ComponentModel.ListSortDirection.Descending, vm.SortDirection);
    }

    [Fact]
    public void SortByColumnCommand_ResetsDirectionOnDifferentColumn()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SortByColumnCommand.Execute("Name");
        vm.SortByColumnCommand.Execute("Name"); // Now descending

        // Act
        vm.SortByColumnCommand.Execute("Duration"); // Different column

        // Assert
        Assert.Equal(System.ComponentModel.ListSortDirection.Ascending, vm.SortDirection);
    }

    #endregion

    #region Search Tests

    [Fact]
    public void SearchText_DefaultIsEmpty()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal("", vm.SearchText);
    }

    [Fact]
    public void IsSearching_WhenSearchTextEmpty_ReturnsFalse()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService())
        {
            SearchText = ""
        };

        // Assert
        Assert.False(vm.IsSearching);
    }

    [Fact]
    public void IsSearching_WhenSearchTextNotEmpty_ReturnsTrue()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService())
        {
            SearchText = "test"
        };

        // Assert
        Assert.True(vm.IsSearching);
    }

    #endregion

    #region Bin Management Tests

    [Fact]
    public void CreateBinCommand_AddsBin()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        var initialCount = vm.Bins.Count;

        // Act
        vm.CreateBinCommand.Execute(null);

        // Assert
        Assert.Equal(initialCount + 1, vm.Bins.Count);
    }

    [Fact]
    public void CreateBinCommand_CreatesCustomBin()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.CreateBinCommand.Execute(null);

        // Assert
        var newBin = vm.Bins.Last();
        Assert.True(newBin.IsCustomBin);
        Assert.StartsWith("New Bin", newBin.Name);
    }

    [Fact]
    public void DeleteBinCommand_DoesNotDeleteSystemBins()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        var systemBin = vm.Bins.First(b => b.Name == "All Media");
        var initialCount = vm.Bins.Count;

        // Act
        vm.DeleteBinCommand.Execute(systemBin);

        // Assert - System bin should not be deleted
        Assert.Equal(initialCount, vm.Bins.Count);
        Assert.Contains(systemBin, vm.Bins);
    }

    [Fact]
    public void DeleteBinCommand_DeletesCustomBin()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        var countAfterCreate = vm.Bins.Count;

        // Act
        vm.DeleteBinCommand.Execute(customBin);

        // Assert
        Assert.Equal(countAfterCreate - 1, vm.Bins.Count);
        Assert.DoesNotContain(customBin, vm.Bins);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void SelectionStatusText_WhenNoSelection_ReturnsEmpty()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal("", vm.SelectionStatusText);
    }

    [Fact]
    public void SelectionStatusText_WhenOneSelected_ReturnsSingular()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(new MediaItem());

        // Assert
        Assert.Equal("1 item selected", vm.SelectionStatusText);
    }

    [Fact]
    public void SelectionStatusText_WhenMultipleSelected_ReturnsPlural()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(new MediaItem());
        vm.SelectedItems.Add(new MediaItem());
        vm.SelectedItems.Add(new MediaItem());

        // Assert
        Assert.Equal("3 items selected", vm.SelectionStatusText);
    }

    [Fact]
    public void HasMultipleSelected_WhenOneItem_ReturnsFalse()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(new MediaItem());

        // Assert
        Assert.False(vm.HasMultipleSelected);
    }

    [Fact]
    public void HasMultipleSelected_WhenMultipleItems_ReturnsTrue()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(new MediaItem());
        vm.SelectedItems.Add(new MediaItem());

        // Assert
        Assert.True(vm.HasMultipleSelected);
    }

    #endregion

    #region Media Pool Operations Tests

    [Fact]
    public void SetAsCurrentSourceCommand_SetsSourceOnService()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        var item = new MediaItem { Name = "test.mp4" };

        // Act
        vm.SetAsCurrentSourceCommand.Execute(item);

        // Assert
        mediaPool.Verify(m => m.SetCurrentSource(item), Times.Once);
    }

    [Fact]
    public void SetAsCurrentSourceCommand_UpdatesStatusText()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        var item = new MediaItem { Name = "test.mp4" };

        // Act
        vm.SetAsCurrentSourceCommand.Execute(item);

        // Assert
        Assert.Contains("test.mp4", vm.StatusText);
    }

    [Fact]
    public void ClearAllCommand_ClearsMediaPool()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "file1.mp4" },
            new() { Name = "file2.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.ClearAllCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.ClearPool(), Times.Once);
    }

    #endregion

    #region Undo/Redo Tests

    [Fact]
    public void CanUndo_InitiallyFalse()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.False(vm.CanUndo);
    }

    [Fact]
    public void CanRedo_InitiallyFalse()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.False(vm.CanRedo);
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public void HasNoMedia_WhenPoolEmpty_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.True(vm.HasNoMedia);
    }

    [Fact]
    public void HasNoMedia_WhenPoolHasItems_ReturnsFalse()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>
        {
            new() { Name = "file.mp4" }
        });

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.False(vm.HasNoMedia);
    }

    [Fact]
    public void EmptyStateTitle_WhenNoMedia_ReturnsNoMediaMessage()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal("No media imported", vm.EmptyStateTitle);
    }

    [Fact]
    public void EmptyStateSubtitle_WhenNoMedia_ReturnsImportInstructions()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Contains("Import Media", vm.EmptyStateSubtitle);
    }

    #endregion

    #region Media Count Tests

    [Fact]
    public void VideoCount_CountsOnlyVideos()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>
        {
            new() { MediaType = MediaType.Video },
            new() { MediaType = MediaType.Video },
            new() { MediaType = MediaType.Audio },
            new() { MediaType = MediaType.Image }
        });

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal(2, vm.VideoCount);
    }

    [Fact]
    public void AudioCount_CountsOnlyAudio()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>
        {
            new() { MediaType = MediaType.Video },
            new() { MediaType = MediaType.Audio },
            new() { MediaType = MediaType.Audio },
            new() { MediaType = MediaType.Audio }
        });

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal(3, vm.AudioCount);
    }

    [Fact]
    public void ImageCount_CountsOnlyImages()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>
        {
            new() { MediaType = MediaType.Video },
            new() { MediaType = MediaType.Image },
            new() { MediaType = MediaType.Image }
        });

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal(2, vm.ImageCount);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
    }

    #endregion

    #region Status Text Tests

    [Fact]
    public void StatusText_DefaultValue()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Contains("items", vm.StatusText);
    }

    #endregion
}
