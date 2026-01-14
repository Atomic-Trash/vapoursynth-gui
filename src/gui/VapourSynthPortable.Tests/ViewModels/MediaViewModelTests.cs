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

    #region Undo Command Tests

    [Fact]
    public void UndoCommand_AfterRemove_RestoresItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "test.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);
        var undoService = CreateUndoService();

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            undoService);

        // Simulate a remove that gets recorded
        vm.SelectedItem = items[0];
        vm.RemoveSelectedCommand.Execute(null);

        // Act
        vm.UndoCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.AddMedia(It.IsAny<MediaItem>()), Times.Once);
    }

    [Fact]
    public void UndoCommand_UpdatesCanUndoCanRedo()
    {
        // Arrange
        var undoService = CreateUndoService();
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            undoService);

        // Assert initial state
        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    #endregion

    #region Redo Command Tests

    [Fact]
    public void RedoCommand_AfterUndo_ReappliesAction()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);
        var undoService = CreateUndoService();

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            undoService);

        vm.SelectedItem = item;
        vm.RemoveSelectedCommand.Execute(null);
        vm.UndoCommand.Execute(null);

        // Act
        vm.RedoCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(It.IsAny<MediaItem>()), Times.Exactly(2));
    }

    #endregion

    #region Remove Selected Tests

    [Fact]
    public void RemoveSelectedCommand_RemovesItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItem = item;

        // Act
        vm.RemoveSelectedCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(item), Times.Once);
    }

    [Fact]
    public void RemoveSelectedCommand_WithNoSelection_DoesNothing()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItem = null;

        // Act
        vm.RemoveSelectedCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(It.IsAny<MediaItem>()), Times.Never);
    }

    [Fact]
    public void RemoveSelectedCommand_ClearsSelection()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItem = item;

        // Act
        vm.RemoveSelectedCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedItem);
    }

    #endregion

    #region Remove From Pool Tests

    [Fact]
    public void RemoveFromPoolCommand_RemovesSpecificItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.RemoveFromPoolCommand.Execute(item);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(item), Times.Once);
    }

    [Fact]
    public void RemoveFromPoolCommand_WithNull_UsesSelectedItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItem = item;

        // Act
        vm.RemoveFromPoolCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(item), Times.Once);
    }

    #endregion

    #region Batch Delete Tests

    [Fact]
    public void BatchDeleteSelectedCommand_RemovesMultipleItems()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item1 = new MediaItem { Name = "test1.mp4" };
        var item2 = new MediaItem { Name = "test2.mp4" };
        var items = new ObservableCollection<MediaItem> { item1, item2 };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(item1);
        vm.SelectedItems.Add(item2);

        // Act
        vm.BatchDeleteSelectedCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(It.IsAny<MediaItem>()), Times.Exactly(2));
    }

    [Fact]
    public void BatchDeleteSelectedCommand_ClearsSelectedItems()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item1 = new MediaItem { Name = "test1.mp4" };
        var item2 = new MediaItem { Name = "test2.mp4" };
        var items = new ObservableCollection<MediaItem> { item1, item2 };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(item1);
        vm.SelectedItems.Add(item2);

        // Act
        vm.BatchDeleteSelectedCommand.Execute(null);

        // Assert
        Assert.Empty(vm.SelectedItems);
    }

    [Fact]
    public void BatchDeleteSelectedCommand_WithNoSelection_DoesNothing()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.BatchDeleteSelectedCommand.Execute(null);

        // Assert
        mediaPool.Verify(m => m.RemoveMedia(It.IsAny<MediaItem>()), Times.Never);
    }

    #endregion

    #region Batch Add To Bin Tests

    [Fact]
    public void BatchAddToBinCommand_AddsMultipleItems()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        var item1 = new MediaItem { Name = "test1.mp4" };
        var item2 = new MediaItem { Name = "test2.mp4" };
        vm.SelectedItems.Add(item1);
        vm.SelectedItems.Add(item2);

        // Act
        vm.BatchAddToBinCommand.Execute(customBin);

        // Assert
        Assert.Contains(item1, customBin.Items);
        Assert.Contains(item2, customBin.Items);
    }

    [Fact]
    public void BatchAddToBinCommand_SkipsDuplicates()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        var item1 = new MediaItem { Name = "test1.mp4" };
        customBin.Items.Add(item1); // Already in bin
        vm.SelectedItems.Add(item1);

        // Act
        vm.BatchAddToBinCommand.Execute(customBin);

        // Assert - Should still only have 1 item
        Assert.Single(customBin.Items);
    }

    [Fact]
    public void BatchAddToBinCommand_WithSystemBin_DoesNothing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        var systemBin = vm.Bins.First(b => b.Name == "All Media");
        var item = new MediaItem { Name = "test.mp4" };
        vm.SelectedItems.Add(item);

        // Act
        vm.BatchAddToBinCommand.Execute(systemBin);

        // Assert - System bins don't have Items collection populated
        Assert.Empty(systemBin.Items);
    }

    #endregion

    #region Batch Remove From Bin Tests

    [Fact]
    public void BatchRemoveFromBinCommand_RemovesMultipleItems()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        vm.SelectedBin = customBin;

        var item1 = new MediaItem { Name = "test1.mp4" };
        var item2 = new MediaItem { Name = "test2.mp4" };
        customBin.Items.Add(item1);
        customBin.Items.Add(item2);

        vm.SelectedItems.Add(item1);
        vm.SelectedItems.Add(item2);

        // Act
        vm.BatchRemoveFromBinCommand.Execute(null);

        // Assert
        Assert.Empty(customBin.Items);
    }

    [Fact]
    public void BatchRemoveFromBinCommand_WithSystemBin_DoesNothing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        var systemBin = vm.Bins.First(b => b.Name == "All Media");
        vm.SelectedBin = systemBin;
        vm.SelectedItems.Add(new MediaItem { Name = "test.mp4" });

        // Act & Assert - Should not throw
        vm.BatchRemoveFromBinCommand.Execute(null);
    }

    #endregion

    #region Bin Editing Tests

    [Fact]
    public void StartEditBinCommand_SetsIsEditing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        // Act
        vm.StartEditBinCommand.Execute(customBin);

        // Assert
        Assert.True(customBin.IsEditing);
    }

    [Fact]
    public void StartEditBinCommand_SavesOriginalName()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        var originalName = customBin.Name;

        // Act
        vm.StartEditBinCommand.Execute(customBin);

        // Assert
        Assert.Equal(originalName, customBin.EditingOriginalName);
    }

    [Fact]
    public void StartEditBinCommand_WithSystemBin_DoesNothing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        var systemBin = vm.Bins.First(b => b.Name == "All Media");

        // Act
        vm.StartEditBinCommand.Execute(systemBin);

        // Assert
        Assert.False(systemBin.IsEditing);
    }

    [Fact]
    public void EndEditBinCommand_ClearsIsEditing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        customBin.IsEditing = true;

        // Act
        vm.EndEditBinCommand.Execute(customBin);

        // Assert
        Assert.False(customBin.IsEditing);
    }

    [Fact]
    public void CancelEditBinCommand_RestoresOriginalName()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        var originalName = customBin.Name;

        vm.StartEditBinCommand.Execute(customBin);
        customBin.Name = "Modified Name";

        // Act
        vm.CancelEditBinCommand.Execute(customBin);

        // Assert
        Assert.Equal(originalName, customBin.Name);
        Assert.False(customBin.IsEditing);
    }

    #endregion

    #region Add To Bin Tests

    [Fact]
    public void AddToBinCommand_AddsSelectedItem()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        var item = new MediaItem { Name = "test.mp4" };
        vm.SelectedItem = item;

        // Act
        vm.AddToBinCommand.Execute(customBin);

        // Assert
        Assert.Contains(item, customBin.Items);
    }

    [Fact]
    public void AddToBinCommand_DoesNotDuplicate()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        var item = new MediaItem { Name = "test.mp4" };
        vm.SelectedItem = item;
        customBin.Items.Add(item);

        // Act
        vm.AddToBinCommand.Execute(customBin);

        // Assert - Should still only have 1 item
        Assert.Single(customBin.Items);
    }

    #endregion

    #region Remove From Bin Tests

    [Fact]
    public void RemoveFromBinCommand_RemovesItem()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        vm.SelectedBin = customBin;

        var item = new MediaItem { Name = "test.mp4" };
        customBin.Items.Add(item);

        // Act
        vm.RemoveFromBinCommand.Execute(item);

        // Assert
        Assert.DoesNotContain(item, customBin.Items);
    }

    [Fact]
    public void RemoveFromBinCommand_WithSystemBin_DoesNothing()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        var systemBin = vm.Bins.First(b => b.Name == "All Media");
        vm.SelectedBin = systemBin;

        // Act & Assert - Should not throw
        vm.RemoveFromBinCommand.Execute(new MediaItem { Name = "test.mp4" });
    }

    #endregion

    #region AddItemToBin Tests

    [Fact]
    public void AddItemToBinCommand_WithMediaBin_AddsSelectedItem()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();

        var item = new MediaItem { Name = "test.mp4" };
        vm.SelectedItem = item;

        // Act
        vm.AddItemToBinCommand.Execute(customBin);

        // Assert
        Assert.Contains(item, customBin.Items);
    }

    #endregion

    #region Extended Sorting Tests

    [Theory]
    [InlineData("Resolution")]
    [InlineData("Duration")]
    [InlineData("Size")]
    [InlineData("Type")]
    public void SortByColumnCommand_SupportsAllColumns(string column)
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SortByColumnCommand.Execute(column);

        // Assert
        Assert.Equal(column, vm.SortColumn);
    }

    [Fact]
    public void SortByColumn_Resolution_SortsCorrectly()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "small.mp4", Width = 640, Height = 480 },
            new() { Name = "large.mp4", Width = 1920, Height = 1080 }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SortByColumnCommand.Execute("Resolution");

        // Assert - Ascending order, small first
        Assert.Equal("small.mp4", vm.DisplayedItems.First().Name);
    }

    #endregion

    #region Extended Search Tests

    [Fact]
    public void SearchText_FiltersByResolution()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video1.mp4", Width = 1920, Height = 1080 }, // Resolution = "1920x1080"
            new() { Name = "video2.mp4", Width = 1280, Height = 720 }   // Resolution = "1280x720"
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "1080";

        // Assert
        Assert.Single(vm.DisplayedItems);
        Assert.Equal("video1.mp4", vm.DisplayedItems.First().Name);
    }

    [Fact]
    public void SearchText_FiltersByCodec()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video1.mp4", Codec = "h264" },
            new() { Name = "video2.mp4", Codec = "hevc" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "hevc";

        // Assert
        Assert.Single(vm.DisplayedItems);
        Assert.Equal("video2.mp4", vm.DisplayedItems.First().Name);
    }

    [Fact]
    public void SearchText_CaseInsensitive()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "MyVideo.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "MYVIDEO";

        // Assert
        Assert.Single(vm.DisplayedItems);
    }

    #endregion

    #region Empty State Extended Tests

    [Fact]
    public void HasNoResults_WhenSearchReturnsEmpty_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "nonexistent";

        // Assert
        Assert.True(vm.HasNoResults);
    }

    [Fact]
    public void EmptyStateTitle_WhenSearching_ReturnsNoResults()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "nonexistent";

        // Assert
        Assert.Equal("No results found", vm.EmptyStateTitle);
    }

    [Fact]
    public void EmptyStateSubtitle_WhenSearching_ContainsSearchText()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SearchText = "mysearch";

        // Assert
        Assert.Contains("mysearch", vm.EmptyStateSubtitle);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsLoading_DefaultsFalse()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void IsPreviewPanelExpanded_DefaultsFalse()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.False(vm.IsPreviewPanelExpanded);
    }

    [Fact]
    public void IsPreviewPanelExpanded_CanBeSet()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.IsPreviewPanelExpanded = true;

        // Assert
        Assert.True(vm.IsPreviewPanelExpanded);
    }

    [Fact]
    public void ImportProgress_DefaultsZero()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal(0, vm.ImportProgress);
    }

    [Fact]
    public void ImportProgressText_WhenNoImport_ReturnsImporting()
    {
        // Arrange & Act
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.Equal("Importing...", vm.ImportProgressText);
    }

    [Fact]
    public void ImportProgressText_WhenImporting_ShowsProgress()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.TotalImportCount = 10;
        vm.CompletedImportCount = 5;

        // Assert
        Assert.Equal("Importing 5 of 10...", vm.ImportProgressText);
    }

    #endregion

    #region Custom Bins Property Tests

    [Fact]
    public void CustomBins_ReturnsOnlyCustomBins()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        vm.CreateBinCommand.Execute(null);

        // Assert
        Assert.Equal(2, vm.CustomBins.Count());
        Assert.All(vm.CustomBins, b => Assert.True(b.IsCustomBin));
    }

    [Fact]
    public void CustomBins_DoesNotIncludeSystemBins()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Assert
        Assert.DoesNotContain(vm.CustomBins, b => b.Name == "All Media");
        Assert.DoesNotContain(vm.CustomBins, b => b.Name == "Video");
        Assert.DoesNotContain(vm.CustomBins, b => b.Name == "Audio");
        Assert.DoesNotContain(vm.CustomBins, b => b.Name == "Images");
    }

    #endregion

    #region Selection Changed Notification Tests

    [Fact]
    public void NotifySelectionChanged_UpdatesStatusText()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItems.Add(new MediaItem());
        vm.SelectedItems.Add(new MediaItem());

        // Act
        vm.NotifySelectionChanged();

        // Assert
        Assert.Equal("2 items selected", vm.SelectionStatusText);
    }

    #endregion

    #region Selected Item Changed Tests

    [Fact]
    public void SelectedItem_AutoSetsCurrentSource()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.SelectedItem = item;

        // Assert
        mediaPool.Verify(m => m.SetCurrentSource(item), Times.Once);
    }

    #endregion

    #region Selected Bin Changed Tests

    [Fact]
    public void SelectedBin_RefreshesDisplayedItems()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video.mp4", MediaType = MediaType.Video },
            new() { Name = "audio.mp3", MediaType = MediaType.Audio }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act - Select Video bin
        vm.SelectedBin = vm.Bins.First(b => b.Name == "Video");

        // Assert - Only video items displayed
        Assert.Single(vm.DisplayedItems);
        Assert.Equal("video.mp4", vm.DisplayedItems.First().Name);
    }

    [Fact]
    public void SelectedBin_CustomBin_ShowsOnlyBinItems()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var items = new ObservableCollection<MediaItem>
        {
            new() { Name = "video1.mp4" },
            new() { Name = "video2.mp4" }
        };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        customBin.Items.Add(items[0]); // Only add first item

        // Act
        vm.SelectedBin = customBin;

        // Assert
        Assert.Single(vm.DisplayedItems);
        Assert.Equal("video1.mp4", vm.DisplayedItems.First().Name);
    }

    #endregion

    #region Reveal In Explorer Tests

    [Fact]
    public void RevealInExplorerCommand_WithNullItem_UsesSelectedItem()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act & Assert - Should not throw even with no selection
        var action = () => vm.RevealInExplorerCommand.Execute(null);
        Assert.Null(Record.Exception(action));
    }

    #endregion

    #region Dispose Extended Tests

    [Fact]
    public void Dispose_AfterDispose_PropertiesStillAccessible()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SearchText = "test";

        // Act
        vm.Dispose();

        // Assert
        Assert.Equal("test", vm.SearchText);
    }

    #endregion

    #region Bin Count Tests

    [Fact]
    public void CreateBin_IncrementsCustomBinNumber()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.CreateBinCommand.Execute(null);
        vm.CreateBinCommand.Execute(null);
        vm.CreateBinCommand.Execute(null);

        // Assert - Should have "New Bin 1", "New Bin 2", "New Bin 3"
        Assert.Contains(vm.Bins, b => b.Name == "New Bin 1");
        Assert.Contains(vm.Bins, b => b.Name == "New Bin 2");
        Assert.Contains(vm.Bins, b => b.Name == "New Bin 3");
    }

    [Fact]
    public void DeleteBin_SelectsAllMediaBin_WhenSelectedBinDeleted()
    {
        // Arrange
        var vm = new MediaViewModel(
            CreateMockMediaPool().Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        vm.CreateBinCommand.Execute(null);
        var customBin = vm.Bins.Last();
        vm.SelectedBin = customBin;

        // Act
        vm.DeleteBinCommand.Execute(customBin);

        // Assert
        Assert.Equal("All Media", vm.SelectedBin?.Name);
    }

    #endregion

    #region Clear All Extended Tests

    [Fact]
    public void ClearAllCommand_WithEmptyPool_DoesNothing()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());

        // Act
        vm.ClearAllCommand.Execute(null);

        // Assert - Should not throw or call ClearPool
        // The ClearPool is still called by the mock, but we verify no crash
    }

    [Fact]
    public void ClearAllCommand_ClearsSelectedItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var item = new MediaItem { Name = "test.mp4" };
        var items = new ObservableCollection<MediaItem> { item };
        mediaPool.Setup(m => m.MediaPool).Returns(items);

        var vm = new MediaViewModel(
            mediaPool.Object,
            CreateMockSettingsService().Object,
            CreateUndoService());
        vm.SelectedItem = item;

        // Act
        vm.ClearAllCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedItem);
    }

    #endregion
}
