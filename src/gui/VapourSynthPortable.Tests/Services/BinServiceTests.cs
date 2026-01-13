using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

/// <summary>
/// Tests for BinService media organization functionality.
/// </summary>
public class BinServiceTests
{
    private readonly Mock<ISettingsService> _mockSettings;
    private readonly UndoService _undoService;
    private readonly BinService _sut;

    public BinServiceTests()
    {
        _mockSettings = new Mock<ISettingsService>();
        _mockSettings.Setup(s => s.Load()).Returns(new AppSettings());
        _undoService = new UndoService();
        _sut = new BinService(_mockSettings.Object, _undoService);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_CreatesSystemBins()
    {
        // Assert
        Assert.Equal(4, _sut.Bins.Count);
        Assert.Contains(_sut.Bins, b => b.Name == "All Media");
        Assert.Contains(_sut.Bins, b => b.Name == "Video");
        Assert.Contains(_sut.Bins, b => b.Name == "Audio");
        Assert.Contains(_sut.Bins, b => b.Name == "Images");
    }

    [Fact]
    public void Constructor_SystemBinsAreNotCustom()
    {
        // Assert
        Assert.All(_sut.Bins, b => Assert.False(b.IsCustomBin));
    }

    [Fact]
    public void AllMediaBin_IsAccessible()
    {
        // Assert
        Assert.NotNull(_sut.AllMediaBin);
        Assert.Equal("All Media", _sut.AllMediaBin.Name);
    }

    #endregion

    #region CreateBin Tests

    [Fact]
    public void CreateBin_WithNoName_GeneratesUniqueName()
    {
        // Act
        var result = _sut.CreateBin();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.StartsWith("New Bin", result.Value.Name);
        Assert.True(result.Value.IsCustomBin);
    }

    [Fact]
    public void CreateBin_WithName_UsesProvidedName()
    {
        // Act
        var result = _sut.CreateBin("My Custom Bin");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("My Custom Bin", result.Value!.Name);
    }

    [Fact]
    public void CreateBin_WithDuplicateName_Fails()
    {
        // Arrange
        _sut.CreateBin("Test Bin");

        // Act
        var result = _sut.CreateBin("Test Bin");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public void CreateBin_WithWhitespaceOnly_AutoGeneratesName()
    {
        // Act - whitespace only triggers auto-name generation
        var result = _sut.CreateBin("   ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.StartsWith("New Bin", result.Value!.Name);
    }

    [Fact]
    public void CreateBin_WithInvalidCharacters_Fails()
    {
        // Act
        var result = _sut.CreateBin("Test<>Bin");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid", result.Error);
    }

    [Fact]
    public void CreateBin_AddsBinToCollection()
    {
        // Arrange
        var initialCount = _sut.Bins.Count;

        // Act
        _sut.CreateBin("New Bin");

        // Assert
        Assert.Equal(initialCount + 1, _sut.Bins.Count);
    }

    [Fact]
    public void CreateBin_RecordsUndoAction()
    {
        // Act
        _sut.CreateBin("Test Bin");

        // Assert
        Assert.True(_undoService.CanUndo);
    }

    [Fact]
    public void CreateBin_CanBeUndone()
    {
        // Arrange
        var initialCount = _sut.Bins.Count;
        _sut.CreateBin("Test Bin");

        // Act
        _undoService.Undo();

        // Assert
        Assert.Equal(initialCount, _sut.Bins.Count);
    }

    #endregion

    #region DeleteBin Tests

    [Fact]
    public void DeleteBin_RemovesCustomBin()
    {
        // Arrange
        var result = _sut.CreateBin("To Delete");
        var bin = result.Value!;
        var countBefore = _sut.Bins.Count;

        // Act
        var deleteResult = _sut.DeleteBin(bin);

        // Assert
        Assert.True(deleteResult.IsSuccess);
        Assert.Equal(countBefore - 1, _sut.Bins.Count);
        Assert.DoesNotContain(bin, _sut.Bins);
    }

    [Fact]
    public void DeleteBin_SystemBin_Fails()
    {
        // Act
        var result = _sut.DeleteBin(_sut.AllMediaBin);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("System bins", result.Error);
    }

    [Fact]
    public void DeleteBin_NullBin_Fails()
    {
        // Act
        var result = _sut.DeleteBin(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("null", result.Error);
    }

    [Fact]
    public void DeleteBin_CanBeUndone()
    {
        // Arrange
        var result = _sut.CreateBin("Undoable Delete");
        var bin = result.Value!;
        _undoService.Clear(); // Clear create action
        _sut.DeleteBin(bin);

        // Act
        _undoService.Undo();

        // Assert
        Assert.Contains(_sut.Bins, b => b.Name == "Undoable Delete");
    }

    [Fact]
    public void DeleteBin_RestoresItemsOnUndo()
    {
        // Arrange
        var result = _sut.CreateBin("Bin With Items");
        var bin = result.Value!;
        var item = new MediaItem { Name = "TestItem.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);
        _undoService.Clear();

        _sut.DeleteBin(bin);

        // Act
        _undoService.Undo();

        // Assert
        var restoredBin = _sut.Bins.FirstOrDefault(b => b.Name == "Bin With Items");
        Assert.NotNull(restoredBin);
        Assert.Single(restoredBin.Items);
    }

    #endregion

    #region RenameBin Tests

    [Fact]
    public void RenameBin_ChangesName()
    {
        // Arrange
        var result = _sut.CreateBin("Original Name");
        var bin = result.Value!;

        // Act
        var renameResult = _sut.RenameBin(bin, "New Name");

        // Assert
        Assert.True(renameResult.IsSuccess);
        Assert.Equal("New Name", bin.Name);
    }

    [Fact]
    public void RenameBin_SystemBin_Fails()
    {
        // Act
        var result = _sut.RenameBin(_sut.AllMediaBin, "New Name");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("System bins", result.Error);
    }

    [Fact]
    public void RenameBin_ToDuplicateName_Fails()
    {
        // Arrange
        _sut.CreateBin("Existing Name");
        var result = _sut.CreateBin("To Rename");
        var bin = result.Value!;

        // Act
        var renameResult = _sut.RenameBin(bin, "Existing Name");

        // Assert
        Assert.False(renameResult.IsSuccess);
        Assert.Contains("already exists", renameResult.Error);
    }

    [Fact]
    public void RenameBin_ToSameName_Succeeds()
    {
        // Arrange
        var result = _sut.CreateBin("Same Name");
        var bin = result.Value!;

        // Act
        var renameResult = _sut.RenameBin(bin, "Same Name");

        // Assert - renaming to same name is allowed (no collision with self)
        Assert.True(renameResult.IsSuccess);
    }

    [Fact]
    public void RenameBin_CanBeUndone()
    {
        // Arrange
        var result = _sut.CreateBin("Original");
        var bin = result.Value!;
        _undoService.Clear();

        _sut.RenameBin(bin, "Changed");

        // Act
        _undoService.Undo();

        // Assert
        Assert.Equal("Original", bin.Name);
    }

    #endregion

    #region AddItemToBin Tests

    [Fact]
    public void AddItemToBin_AddsItem()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };

        // Act
        var addResult = _sut.AddItemToBin(bin, item);

        // Assert
        Assert.True(addResult.IsSuccess);
        Assert.Single(bin.Items);
        Assert.Contains(item, bin.Items);
    }

    [Fact]
    public void AddItemToBin_SystemBin_Fails()
    {
        // Arrange
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };

        // Act
        var result = _sut.AddItemToBin(_sut.AllMediaBin, item);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("custom bins", result.Error);
    }

    [Fact]
    public void AddItemToBin_DuplicateItem_Fails()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);

        // Act
        var addResult = _sut.AddItemToBin(bin, item);

        // Assert
        Assert.False(addResult.IsSuccess);
        Assert.Contains("already in", addResult.Error);
    }

    [Fact]
    public void AddItemToBin_CanBeUndone()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _undoService.Clear();

        _sut.AddItemToBin(bin, item);

        // Act
        _undoService.Undo();

        // Assert
        Assert.Empty(bin.Items);
    }

    #endregion

    #region AddItemsToBin (Batch) Tests

    [Fact]
    public void AddItemsToBin_AddsMultipleItems()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var items = new[]
        {
            new MediaItem { Name = "test1.mp4", FilePath = @"C:\test1.mp4" },
            new MediaItem { Name = "test2.mp4", FilePath = @"C:\test2.mp4" },
            new MediaItem { Name = "test3.mp4", FilePath = @"C:\test3.mp4" }
        };

        // Act
        var addResult = _sut.AddItemsToBin(bin, items);

        // Assert
        Assert.True(addResult.IsSuccess);
        Assert.Equal(3, addResult.Value);
        Assert.Equal(3, bin.Items.Count);
    }

    [Fact]
    public void AddItemsToBin_SkipsDuplicates()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var existingItem = new MediaItem { Name = "existing.mp4", FilePath = @"C:\existing.mp4" };
        _sut.AddItemToBin(bin, existingItem);

        var items = new[]
        {
            existingItem,
            new MediaItem { Name = "new.mp4", FilePath = @"C:\new.mp4" }
        };

        // Act
        var addResult = _sut.AddItemsToBin(bin, items);

        // Assert
        Assert.True(addResult.IsSuccess);
        Assert.Equal(1, addResult.Value); // Only 1 new item added
        Assert.Equal(2, bin.Items.Count);
    }

    #endregion

    #region RemoveItemFromBin Tests

    [Fact]
    public void RemoveItemFromBin_RemovesItem()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);

        // Act
        var removeResult = _sut.RemoveItemFromBin(bin, item);

        // Assert
        Assert.True(removeResult.IsSuccess);
        Assert.Empty(bin.Items);
    }

    [Fact]
    public void RemoveItemFromBin_ItemNotInBin_Fails()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };

        // Act
        var removeResult = _sut.RemoveItemFromBin(bin, item);

        // Assert
        Assert.False(removeResult.IsSuccess);
        Assert.Contains("not in", removeResult.Error);
    }

    [Fact]
    public void RemoveItemFromBin_CanBeUndone()
    {
        // Arrange
        var result = _sut.CreateBin("Test Bin");
        var bin = result.Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);
        _undoService.Clear();

        _sut.RemoveItemFromBin(bin, item);

        // Act
        _undoService.Undo();

        // Assert
        Assert.Single(bin.Items);
        Assert.Contains(item, bin.Items);
    }

    #endregion

    #region MoveItemToBin Tests

    [Fact]
    public void MoveItemToBin_MovesItem()
    {
        // Arrange
        var source = _sut.CreateBin("Source").Value!;
        var target = _sut.CreateBin("Target").Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(source, item);

        // Act
        var moveResult = _sut.MoveItemToBin(item, source, target);

        // Assert
        Assert.True(moveResult.IsSuccess);
        Assert.Empty(source.Items);
        Assert.Single(target.Items);
        Assert.Contains(item, target.Items);
    }

    [Fact]
    public void MoveItemToBin_SameBin_Fails()
    {
        // Arrange
        var bin = _sut.CreateBin("Bin").Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);

        // Act
        var moveResult = _sut.MoveItemToBin(item, bin, bin);

        // Assert
        Assert.False(moveResult.IsSuccess);
        Assert.Contains("same", moveResult.Error);
    }

    [Fact]
    public void MoveItemToBin_CanBeUndone()
    {
        // Arrange
        var source = _sut.CreateBin("Source").Value!;
        var target = _sut.CreateBin("Target").Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(source, item);
        _undoService.Clear();

        _sut.MoveItemToBin(item, source, target);

        // Act
        _undoService.Undo();

        // Assert
        Assert.Single(source.Items);
        Assert.Empty(target.Items);
    }

    #endregion

    #region GetBinForItem Tests

    [Fact]
    public void GetBinForItem_ReturnsCorrectBin()
    {
        // Arrange
        var bin = _sut.CreateBin("Test Bin").Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin, item);

        // Act
        var foundBin = _sut.GetBinForItem(item);

        // Assert
        Assert.Equal(bin, foundBin);
    }

    [Fact]
    public void GetBinForItem_ItemNotInAnyBin_ReturnsNull()
    {
        // Arrange
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };

        // Act
        var foundBin = _sut.GetBinForItem(item);

        // Assert
        Assert.Null(foundBin);
    }

    [Fact]
    public void GetBinsForItem_ReturnsAllBinsContainingItem()
    {
        // Arrange
        var bin1 = _sut.CreateBin("Bin 1").Value!;
        var bin2 = _sut.CreateBin("Bin 2").Value!;
        var item = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        _sut.AddItemToBin(bin1, item);
        _sut.AddItemToBin(bin2, item);

        // Act
        var bins = _sut.GetBinsForItem(item);

        // Assert
        Assert.Equal(2, bins.Count);
        Assert.Contains(bin1, bins);
        Assert.Contains(bin2, bins);
    }

    #endregion

    #region ClearBin Tests

    [Fact]
    public void ClearBin_RemovesAllItems()
    {
        // Arrange
        var bin = _sut.CreateBin("Test Bin").Value!;
        _sut.AddItemToBin(bin, new MediaItem { Name = "1.mp4", FilePath = @"C:\1.mp4" });
        _sut.AddItemToBin(bin, new MediaItem { Name = "2.mp4", FilePath = @"C:\2.mp4" });
        _sut.AddItemToBin(bin, new MediaItem { Name = "3.mp4", FilePath = @"C:\3.mp4" });

        // Act
        var clearResult = _sut.ClearBin(bin);

        // Assert
        Assert.True(clearResult.IsSuccess);
        Assert.Empty(bin.Items);
    }

    [Fact]
    public void ClearBin_CanBeUndone()
    {
        // Arrange
        var bin = _sut.CreateBin("Test Bin").Value!;
        _sut.AddItemToBin(bin, new MediaItem { Name = "1.mp4", FilePath = @"C:\1.mp4" });
        _sut.AddItemToBin(bin, new MediaItem { Name = "2.mp4", FilePath = @"C:\2.mp4" });
        _undoService.Clear();

        _sut.ClearBin(bin);

        // Act
        _undoService.Undo();

        // Assert
        Assert.Equal(2, bin.Items.Count);
    }

    #endregion

    #region DuplicateBin Tests

    [Fact]
    public void DuplicateBin_CreatesNewBinWithItems()
    {
        // Arrange
        var original = _sut.CreateBin("Original").Value!;
        _sut.AddItemToBin(original, new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" });

        // Act
        var duplicateResult = _sut.DuplicateBin(original);

        // Assert
        Assert.True(duplicateResult.IsSuccess);
        Assert.NotEqual(original, duplicateResult.Value);
        Assert.StartsWith("Original Copy", duplicateResult.Value!.Name);
        Assert.Equal(original.Items.Count, duplicateResult.Value.Items.Count);
    }

    [Fact]
    public void DuplicateBin_SystemBin_Fails()
    {
        // Act
        var result = _sut.DuplicateBin(_sut.AllMediaBin);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("Valid Name", true)]
    [InlineData("Name With Spaces", true)]
    [InlineData("Name-With-Dashes", true)]
    [InlineData("Name_With_Underscores", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Name<Invalid>", false)]
    [InlineData("Name:Invalid", false)]
    [InlineData("Name/Invalid", false)]
    [InlineData("Name\\Invalid", false)]
    [InlineData("Name|Invalid", false)]
    [InlineData("Name?Invalid", false)]
    [InlineData("Name*Invalid", false)]
    public void IsValidBinName_ValidatesCorrectly(string name, bool expected)
    {
        // Act
        var result = _sut.IsValidBinName(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsBinNameInUse_ReturnsTrue_WhenNameExists()
    {
        // Arrange
        _sut.CreateBin("Existing Bin");

        // Act
        var result = _sut.IsBinNameInUse("Existing Bin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBinNameInUse_IsCaseInsensitive()
    {
        // Arrange
        _sut.CreateBin("Test Bin");

        // Act
        var result = _sut.IsBinNameInUse("TEST BIN");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBinNameInUse_ExcludesSpecifiedBin()
    {
        // Arrange
        var bin = _sut.CreateBin("Test Bin").Value!;

        // Act
        var result = _sut.IsBinNameInUse("Test Bin", bin);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void SaveBins_CallsSettingsService()
    {
        // Arrange
        _sut.CreateBin("Test Bin");

        // Assert
        _mockSettings.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void LoadBins_RestoresBinsFromSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            CustomBins =
            [
                new CustomBinSettings { Id = "1", Name = "Loaded Bin 1", ItemPaths = [] },
                new CustomBinSettings { Id = "2", Name = "Loaded Bin 2", ItemPaths = [] }
            ]
        };
        _mockSettings.Setup(s => s.Load()).Returns(settings);

        // Act
        _sut.LoadBins([]);

        // Assert
        Assert.Contains(_sut.Bins, b => b.Name == "Loaded Bin 1");
        Assert.Contains(_sut.Bins, b => b.Name == "Loaded Bin 2");
    }

    [Fact]
    public void LoadBins_ResolvesItemPaths()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\media\test.mp4" };
        var settings = new AppSettings
        {
            CustomBins =
            [
                new CustomBinSettings
                {
                    Id = "1",
                    Name = "Bin With Item",
                    ItemPaths = [@"C:\media\test.mp4"]
                }
            ]
        };
        _mockSettings.Setup(s => s.Load()).Returns(settings);

        // Act
        _sut.LoadBins([mediaItem]);

        // Assert
        var loadedBin = _sut.Bins.FirstOrDefault(b => b.Name == "Bin With Item");
        Assert.NotNull(loadedBin);
        Assert.Single(loadedBin.Items);
        Assert.Equal(mediaItem, loadedBin.Items[0]);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void CreateBin_RaisesBinsChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.BinsChanged += (s, e) => eventRaised = true;

        // Act
        _sut.CreateBin("Test");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void DeleteBin_RaisesBinsChangedEvent()
    {
        // Arrange
        var bin = _sut.CreateBin("Test").Value!;
        var eventRaised = false;
        _sut.BinsChanged += (s, e) => eventRaised = true;

        // Act
        _sut.DeleteBin(bin);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion
}
