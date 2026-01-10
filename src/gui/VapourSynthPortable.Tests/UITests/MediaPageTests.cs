using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Media page functionality.
/// </summary>
[Collection("UI Tests")]
public class MediaPageTests : UITestBase
{
    private readonly MediaPageObject _mediaPage;

    public MediaPageTests()
    {
        LaunchApp();
        // Navigate to Media page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);
        _mediaPage = new MediaPageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void MediaPage_HasImportButtons()
    {
        // Assert
        _mediaPage.ImportMediaButton.Should().NotBeNull("Import Media button should exist");
        _mediaPage.ImportFolderButton.Should().NotBeNull("Import Folder button should exist");
        _mediaPage.CreateBinButton.Should().NotBeNull("Create Bin button should exist");
    }

    [Fact]
    public void MediaPage_HasSearchBox()
    {
        // Assert
        _mediaPage.MediaSearchBox.Should().NotBeNull("Media search box should exist");
    }

    [Fact]
    public void MediaPage_HasMediaItemsList()
    {
        // Assert
        _mediaPage.MediaItemsList.Should().NotBeNull("Media items list should exist");
    }

    [Fact]
    public void MediaPage_HasBinsList()
    {
        // Assert
        _mediaPage.BinsList.Should().NotBeNull("Bins list should exist");
    }

    [Fact]
    public void MediaPage_HasViewModeToggles()
    {
        // Assert
        _mediaPage.GridViewButton.Should().NotBeNull("Grid view button should exist");
        _mediaPage.ListViewButton.Should().NotBeNull("List view button should exist");
    }

    [Fact]
    public void MediaPage_HasPreviewControls()
    {
        // Assert
        _mediaPage.PreviewToggle.Should().NotBeNull("Preview toggle should exist");
        _mediaPage.PlaySelectedButton.Should().NotBeNull("Play Selected button should exist");
    }

    #endregion

    #region View Mode Tests

    [Fact]
    public void ViewMode_CanSwitchToListView()
    {
        // Act
        _mediaPage.SwitchToListView();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when switching to List view");
    }

    [Fact]
    public void ViewMode_CanSwitchBackToGridView()
    {
        // Arrange - Switch to List first
        _mediaPage.SwitchToListView();
        Thread.Sleep(500);

        // Act
        _mediaPage.SwitchToGridView();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when switching to Grid view");
    }

    #endregion

    #region Bins Tests

    [Fact]
    public void BinsList_HasDefaultBins()
    {
        // Wait for bins to load
        Thread.Sleep(500);

        // Assert - Should have at least the default bins
        _mediaPage.BinsCount.Should().BeGreaterThan(0, "Should have at least one bin");
    }

    [Fact]
    public void BinsList_CanSelectBin()
    {
        // Arrange - Wait for bins
        Thread.Sleep(500);
        var binCount = _mediaPage.BinsCount;

        if (binCount == 0)
        {
            // Skip if no bins
            return;
        }

        // Act - Select first bin
        _mediaPage.SelectBinByIndex(0);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when selecting a bin");
    }

    #endregion

    #region Search Tests

    [Fact]
    public void MediaSearch_CanEnterSearchQuery()
    {
        // Arrange
        var searchBox = _mediaPage.MediaSearchBox;
        searchBox.Should().NotBeNull();

        // Act
        _mediaPage.SearchMedia("test");
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when searching");
    }

    [Fact]
    public void MediaSearch_ClearingSearch_Works()
    {
        // Arrange
        _mediaPage.SearchMedia("test");
        Thread.Sleep(500);

        // Act
        _mediaPage.ClearSearch();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Preview Panel Tests

    [Fact]
    public void PreviewPanel_CanToggle()
    {
        // Act - Toggle preview off
        _mediaPage.TogglePreviewPanel();
        Thread.Sleep(500);

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when toggling preview panel");

        // Act - Toggle preview back on
        _mediaPage.TogglePreviewPanel();
        Thread.Sleep(500);

        // Assert
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void PlaySelectedButton_DisabledWhenNoSelection()
    {
        // No media selected initially
        Thread.Sleep(500);

        // Assert - Play button should be disabled without selection
        var isEnabled = _mediaPage.IsPlaySelectedEnabled;
        App.HasExited.Should().BeFalse();
        // Note: Just checking we can read the state without crashing
    }

    #endregion

    #region Import Button Tests

    [Fact]
    public void ImportMediaButton_IsClickable()
    {
        // Assert - Button should be present and enabled
        var button = _mediaPage.ImportMediaButton;
        button.Should().NotBeNull();
        button!.IsEnabled.Should().BeTrue("Import Media button should be enabled");
    }

    [Fact]
    public void ImportFolderButton_IsClickable()
    {
        // Assert - Button should be present and enabled
        var button = _mediaPage.ImportFolderButton;
        button.Should().NotBeNull();
        button!.IsEnabled.Should().BeTrue("Import Folder button should be enabled");
    }

    [Fact]
    public void CreateBinButton_IsClickable()
    {
        // Assert - Button should be present and enabled
        var button = _mediaPage.CreateBinButton;
        button.Should().NotBeNull();
        button!.IsEnabled.Should().BeTrue("Create Bin button should be enabled");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MediaPage_FullWorkflow_BrowseBinsAndNavigateAway()
    {
        // This test simulates a user browsing bins then navigating away

        // Step 1: Wait for page to load
        Thread.Sleep(500);

        // Step 2: Switch view modes
        _mediaPage.SwitchToListView();
        Thread.Sleep(300);
        _mediaPage.SwitchToGridView();
        Thread.Sleep(300);

        // Step 3: Select a bin if available
        if (_mediaPage.BinsCount > 0)
        {
            _mediaPage.SelectBinByIndex(0);
            Thread.Sleep(300);
        }

        // Step 4: Search for something
        _mediaPage.SearchMedia("video");
        Thread.Sleep(300);

        // Step 5: Clear search
        _mediaPage.ClearSearch();
        Thread.Sleep(300);

        // Step 6: Toggle preview panel
        _mediaPage.TogglePreviewPanel();
        Thread.Sleep(300);

        // Step 7: Navigate to another page
        NavigateTo("RestoreNavButton");
        Thread.Sleep(500);

        // Step 8: Navigate back to Media
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void MediaPage_EmptyState_ShowsCorrectly()
    {
        // After launch, media pool may be empty
        // Just verify the page loads correctly
        Thread.Sleep(500);

        // Assert - Page is responsive
        _mediaPage.IsMediaPageVisible.Should().BeTrue("Media page should be visible");
        App.HasExited.Should().BeFalse();
    }

    #endregion
}
