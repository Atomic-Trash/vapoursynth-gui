using System.Collections.ObjectModel;
using System.Windows;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class ColorViewModelTests
{
    private static Mock<IMediaPoolService> CreateMockMediaPool(MediaItem? currentSource = null)
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns(currentSource);
        mock.Setup(m => m.HasSource).Returns(currentSource != null);
        return mock;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.NotNull(vm.Presets);
        Assert.NotNull(vm.Luts);
        Assert.NotNull(vm.PresetCategories);
        Assert.NotNull(vm.LutCategories);
        Assert.NotNull(vm.CurrentGrade);
    }

    [Fact]
    public void Constructor_LoadsPresets()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.NotEmpty(vm.Presets);
        Assert.NotEmpty(vm.PresetCategories);
    }

    [Fact]
    public void Constructor_DefaultsCategoryToAll()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.Equal("All", vm.SelectedCategory);
        Assert.Equal("All", vm.SelectedLutCategory);
    }

    #endregion

    #region Source Tests

    [Fact]
    public void HasSource_WhenNoSource_ReturnsFalse()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.HasSource);
    }

    [Fact]
    public void HasSource_WhenHasSource_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool(new MediaItem { FilePath = @"C:\test.mp4" });

        // Act
        var vm = new ColorViewModel(mediaPool.Object);

        // Assert
        Assert.True(vm.HasSource);
    }

    [Fact]
    public void SourcePath_WhenNoSource_ReturnsEmpty()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.Equal("", vm.SourcePath);
    }

    [Fact]
    public void SourcePath_WhenHasSource_ReturnsPath()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool(new MediaItem { FilePath = @"C:\test.mp4" });

        // Act
        var vm = new ColorViewModel(mediaPool.Object);

        // Assert
        Assert.Equal(@"C:\test.mp4", vm.SourcePath);
    }

    #endregion

    #region Reset Commands Tests

    [Fact]
    public void ResetAllCommand_ResetsCurrentGrade()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.5;
        vm.CurrentGrade.Contrast = 0.5;
        vm.CurrentGrade.Saturation = 1.2;

        // Act
        vm.ResetAllCommand.Execute(null);

        // Assert
        Assert.Equal(0, vm.CurrentGrade.Exposure);
        Assert.Equal(0, vm.CurrentGrade.Contrast);
        Assert.Equal(0, vm.CurrentGrade.Saturation);
    }

    [Fact]
    public void ResetAllCommand_ClearsPresetSelection()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.SelectedPreset = vm.Presets.FirstOrDefault();

        // Act
        vm.ResetAllCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedPreset);
    }

    [Fact]
    public void ResetAllCommand_ClearsLutSelection()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.SelectedLut = new LutFile { Name = "Test", Path = "test.cube" };

        // Act
        vm.ResetAllCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedLut);
    }

    [Fact]
    public void ResetWheelsCommand_ResetsColorWheels()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.LiftX = 0.1;
        vm.CurrentGrade.GammaY = -0.2;
        vm.CurrentGrade.GainMaster = 0.3;

        // Act
        vm.ResetWheelsCommand.Execute(null);

        // Assert
        Assert.Equal(0, vm.CurrentGrade.LiftX);
        Assert.Equal(0, vm.CurrentGrade.GammaY);
        Assert.Equal(0, vm.CurrentGrade.GainMaster);
    }

    [Fact]
    public void ResetAdjustmentsCommand_ResetsAdjustments()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.0;
        vm.CurrentGrade.Temperature = 20;
        vm.CurrentGrade.Vibrance = 0.5;

        // Act
        vm.ResetAdjustmentsCommand.Execute(null);

        // Assert
        Assert.Equal(0, vm.CurrentGrade.Exposure);
        Assert.Equal(0, vm.CurrentGrade.Temperature);
        Assert.Equal(0, vm.CurrentGrade.Vibrance);
    }

    #endregion

    #region Undo/Redo Tests

    [Fact]
    public void CanUndo_InitiallyFalse()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.CanUndo);
    }

    [Fact]
    public void CanRedo_InitiallyFalse()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void UndoCommand_AfterReset_CanUndo()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.0;

        // Act
        vm.ResetAllCommand.Execute(null); // This saves undo state

        // Assert
        Assert.True(vm.CanUndo);
    }

    #endregion

    #region Compare Mode Tests

    [Fact]
    public void IsCompareMode_DefaultsFalse()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.IsCompareMode);
    }

    [Fact]
    public void ToggleCompareCommand_TogglesMode()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Act
        vm.ToggleCompareCommand.Execute(null);

        // Assert
        Assert.True(vm.IsCompareMode);
    }

    [Fact]
    public void ToggleCompareCommand_CapturesOriginalOnFirstToggle()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.5;

        // Act
        vm.ToggleCompareCommand.Execute(null);

        // Assert
        Assert.True(vm.HasOriginalGrade);
        Assert.NotNull(vm.OriginalGrade);
    }

    [Fact]
    public void CaptureOriginalCommand_CapturesCurrentGrade()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 2.0;
        vm.CurrentGrade.Contrast = 0.5;

        // Act
        vm.CaptureOriginalCommand.Execute(null);

        // Assert
        Assert.True(vm.HasOriginalGrade);
        Assert.NotNull(vm.OriginalGrade);
        Assert.Equal(2.0, vm.OriginalGrade.Exposure);
        Assert.Equal(0.5, vm.OriginalGrade.Contrast);
    }

    [Fact]
    public void ClearOriginalCommand_ClearsOriginal()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CaptureOriginalCommand.Execute(null);

        // Act
        vm.ClearOriginalCommand.Execute(null);

        // Assert
        Assert.False(vm.HasOriginalGrade);
        Assert.Null(vm.OriginalGrade);
        Assert.False(vm.IsCompareMode);
    }

    [Fact]
    public void ComparePosition_DefaultsToHalf()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.Equal(0.5, vm.ComparePosition);
    }

    [Theory]
    [InlineData("SideBySide", CompareDisplayMode.SideBySide)]
    [InlineData("VerticalSplit", CompareDisplayMode.VerticalSplit)]
    [InlineData("HorizontalSplit", CompareDisplayMode.HorizontalSplit)]
    [InlineData("Wipe", CompareDisplayMode.Wipe)]
    public void SetCompareModeCommand_SetsMode(string mode, CompareDisplayMode expected)
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Act
        vm.SetCompareModeCommand.Execute(mode);

        // Assert
        Assert.Equal(expected, vm.CompareMode);
    }

    #endregion

    #region Scope Mode Tests

    [Fact]
    public void ScopeMode_DefaultsToWaveform()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.Equal("Waveform", vm.ScopeMode);
    }

    [Fact]
    public void SetScopeModeCommand_SetsMode()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Act
        vm.SetScopeModeCommand.Execute("Histogram");

        // Assert
        Assert.Equal("Histogram", vm.ScopeMode);
    }

    [Fact]
    public void ShowScopes_DefaultsFalse()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.ShowScopes);
    }

    [Fact]
    public void ShowCurves_DefaultsFalse()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.False(vm.ShowCurves);
    }

    #endregion

    #region LUT Tests

    [Fact]
    public void ClearLutCommand_ClearsLut()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.LutPath = "test.cube";
        vm.CurrentGrade.LutIntensity = 0.8;
        vm.SelectedLut = new LutFile { Name = "Test", Path = "test.cube" };

        // Act
        vm.ClearLutCommand.Execute(null);

        // Assert
        Assert.Equal("", vm.CurrentGrade.LutPath);
        Assert.Equal(1.0, vm.CurrentGrade.LutIntensity);
        Assert.Null(vm.SelectedLut);
    }

    [Fact]
    public void SelectedLut_WhenChanged_SetsLutPath()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        var lut = new LutFile { Name = "Test LUT", Path = @"C:\luts\test.cube" };

        // Act
        vm.SelectedLut = lut;

        // Assert
        Assert.Equal(@"C:\luts\test.cube", vm.CurrentGrade.LutPath);
    }

    [Fact]
    public void FilteredLuts_WhenSearchQuerySet_FiltersResults()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.Luts.Clear();
        vm.Luts.Add(new LutFile { Name = "Cinematic Warm", Path = "warm.cube", Category = "Cinematic" });
        vm.Luts.Add(new LutFile { Name = "Cool Tone", Path = "cool.cube", Category = "Creative" });
        vm.Luts.Add(new LutFile { Name = "Vintage Film", Path = "vintage.cube", Category = "Vintage" });

        // Act
        vm.LutSearchQuery = "warm";
        var filtered = vm.FilteredLuts.ToList();

        // Assert
        Assert.Single(filtered);
        Assert.Equal("Cinematic Warm", filtered[0].Name);
    }

    [Fact]
    public void FilteredLuts_WhenCategorySelected_FiltersResults()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.Luts.Clear();
        vm.Luts.Add(new LutFile { Name = "Cinematic Warm", Path = "warm.cube", Category = "Cinematic" });
        vm.Luts.Add(new LutFile { Name = "Cinematic Cool", Path = "cool.cube", Category = "Cinematic" });
        vm.Luts.Add(new LutFile { Name = "Vintage Film", Path = "vintage.cube", Category = "Vintage" });

        // Act
        vm.SelectedLutCategory = "Cinematic";
        var filtered = vm.FilteredLuts.ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, l => Assert.Equal("Cinematic", l.Category));
    }

    [Fact]
    public void ToggleLutFavoriteCommand_TogglesFavorite()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        var lut = new LutFile { Name = "Test", Path = "test.cube", IsFavorite = false };

        // Act
        vm.ToggleLutFavoriteCommand.Execute(lut);

        // Assert
        Assert.True(lut.IsFavorite);
    }

    #endregion

    #region Preset Tests

    [Fact]
    public void FilteredPresets_WhenCategoryIsAll_ReturnsAll()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.SelectedCategory = "All";

        // Act
        var filtered = vm.FilteredPresets.ToList();

        // Assert
        Assert.Equal(vm.Presets.Count, filtered.Count);
    }

    [Fact]
    public void SelectedPreset_WhenChanged_AppliesPreset()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        var preset = vm.Presets.FirstOrDefault();
        if (preset == null) return;

        // Act
        vm.SelectedPreset = preset;

        // Assert
        // Preset should be applied to current grade
        // (We can't easily verify specific values without knowing preset content)
        Assert.Contains(preset.Name, vm.StatusText);
    }

    #endregion

    #region Swap/Revert Tests

    [Fact]
    public void SwapGradesCommand_SwapsCurrentAndOriginal()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.0;
        vm.CaptureOriginalCommand.Execute(null);
        vm.CurrentGrade.Exposure = 2.0;

        // Act
        vm.SwapGradesCommand.Execute(null);

        // Assert
        Assert.Equal(1.0, vm.CurrentGrade.Exposure);
        Assert.NotNull(vm.OriginalGrade);
        Assert.Equal(2.0, vm.OriginalGrade.Exposure);
    }

    [Fact]
    public void RevertToOriginalCommand_RevertsGrade()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.0;
        vm.CaptureOriginalCommand.Execute(null);
        vm.CurrentGrade.Exposure = 2.0;

        // Act
        vm.RevertToOriginalCommand.Execute(null);

        // Assert
        Assert.Equal(1.0, vm.CurrentGrade.Exposure);
    }

    #endregion

    #region Status Text Tests

    [Fact]
    public void StatusText_DefaultsToNoClipLoaded()
    {
        // Arrange & Act
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Assert
        Assert.Equal("No clip loaded", vm.StatusText);
    }

    #endregion

    #region Project Persistence Tests

    [Fact]
    public void ExportToProject_SavesColorGradeData()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.CurrentGrade.Exposure = 1.5;
        vm.CurrentGrade.Contrast = 0.3;
        var project = new Project();

        // Act
        vm.ExportToProject(project);

        // Assert
        Assert.NotNull(project.ColorGradeData);
        Assert.Equal(1.5, project.ColorGradeData.Exposure);
        Assert.Equal(0.3, project.ColorGradeData.Contrast);
    }

    [Fact]
    public void ImportFromProject_LoadsColorGradeData()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        var project = new Project
        {
            ColorGradeData = new ColorGradeData
            {
                Exposure = 2.0,
                Contrast = 0.5,
                Saturation = 0.8
            }
        };

        // Act
        vm.ImportFromProject(project);

        // Assert
        Assert.Equal(2.0, vm.CurrentGrade.Exposure);
        Assert.Equal(0.5, vm.CurrentGrade.Contrast);
        Assert.Equal(0.8, vm.CurrentGrade.Saturation);
    }

    [Fact]
    public void ImportFromProject_ClearsUndoStack()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);
        vm.ResetAllCommand.Execute(null); // Create undo state
        var project = new Project { ColorGradeData = new ColorGradeData() };

        // Act
        vm.ImportFromProject(project);

        // Assert
        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = new ColorViewModel(CreateMockMediaPool().Object);

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
    }

    #endregion

    #region LutFile Model Tests

    [Fact]
    public void LutFile_DefaultValues()
    {
        // Arrange & Act
        var lut = new LutFile();

        // Assert
        Assert.Equal("", lut.Name);
        Assert.Equal("", lut.Path);
        Assert.Equal("Uncategorized", lut.Category);
        Assert.False(lut.IsFavorite);
    }

    #endregion
}
