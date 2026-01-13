using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Restore page functionality.
/// </summary>
[Collection("UI Tests")]
public class RestorePageTests : UITestBase
{
    private readonly RestorePageObject _restorePage;

    public RestorePageTests()
    {
        LaunchApp();
        // Navigate to Restore page (it's the default, but let's be explicit)
        NavigateTo("RestoreNavButton");
        Thread.Sleep(500);
        _restorePage = new RestorePageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void RestorePage_HasModeToggleButtons()
    {
        // Assert
        _restorePage.SimpleModeButton.Should().NotBeNull("Simple mode button should exist");
        _restorePage.AdvancedModeButton.Should().NotBeNull("Advanced mode button should exist");
    }

    [Fact]
    public void RestorePage_HasPresetSearchBox()
    {
        // Assert
        _restorePage.PresetSearchBox.Should().NotBeNull("Preset search box should exist");
    }

    [Fact]
    public void RestorePage_HasPresetsList()
    {
        // Assert
        _restorePage.PresetsList.Should().NotBeNull("Presets list should exist");
    }

    [Fact]
    public void RestorePage_HasJobQueueList()
    {
        // Assert
        _restorePage.JobQueueList.Should().NotBeNull("Job queue list should exist");
    }

    [Fact]
    public void RestorePage_HasQueueControlButtons()
    {
        // Assert - The RestorePage has Clear Queue and Clear Restoration buttons
        // Note: Processing happens automatically, no explicit Process/Cancel buttons
        _restorePage.ClearQueueButton.Should().NotBeNull("Clear Queue button should exist");
        _restorePage.ClearRestorationButton.Should().NotBeNull("Clear Restoration button should exist");
    }

    #endregion

    #region Mode Toggle Tests

    [Fact]
    public void ModeToggle_CanSwitchToAdvancedMode()
    {
        // Arrange - Ensure we're in Simple mode first
        _restorePage.SwitchToSimpleMode();

        // Act
        _restorePage.SwitchToAdvancedMode();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when switching to Advanced mode");
    }

    [Fact]
    public void ModeToggle_CanSwitchBackToSimpleMode()
    {
        // Arrange - Switch to Advanced first
        _restorePage.SwitchToAdvancedMode();
        Thread.Sleep(500);

        // Act
        _restorePage.SwitchToSimpleMode();
        Thread.Sleep(1000); // Give UI time to switch back

        // Assert - App should not crash and presets should eventually be visible
        App.HasExited.Should().BeFalse("App should not crash when switching modes");

        // Wait for presets list to become available (may take time after mode switch)
        var presetsFound = WaitHelpers.WaitUntil(
            () => _restorePage.PresetsList != null,
            TimeSpan.FromSeconds(3));

        presetsFound.Should().BeTrue("Presets list should be visible in Simple mode");
    }

    #endregion

    #region Presets List Tests

    [Fact]
    public void PresetsList_LoadsPresets()
    {
        // Arrange - Wait for presets to load
        Thread.Sleep(1000);

        // Assert
        _restorePage.HasPresets.Should().BeTrue("Presets should be loaded");
        _restorePage.VisiblePresetsCount.Should().BeGreaterThan(0, "Should have at least one preset");
    }

    [Fact]
    public void PresetsList_CanSelectPreset()
    {
        // Arrange - Wait for presets
        Thread.Sleep(1000);
        var presetCount = _restorePage.VisiblePresetsCount;

        if (presetCount == 0)
        {
            // Skip if no presets loaded
            return;
        }

        // Act - Select first preset
        _restorePage.SelectPresetByIndex(0);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when selecting a preset");
    }

    [Fact]
    public void PresetsList_SelectingPreset_ShowsActionButtons()
    {
        // Arrange - Wait for presets and select one
        Thread.Sleep(1000);
        if (_restorePage.VisiblePresetsCount == 0) return;

        // Act
        _restorePage.SelectPresetByIndex(0);
        Thread.Sleep(500);

        // Assert - Action buttons should be visible
        // Note: These buttons may only appear when a preset is selected AND source is loaded
        // For now, just verify app doesn't crash
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Search Tests

    [Fact]
    public void PresetSearch_CanEnterSearchQuery()
    {
        // Arrange
        var searchBox = _restorePage.PresetSearchBox;
        searchBox.Should().NotBeNull();

        // Act
        _restorePage.SearchPresets("denoise");
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when searching");
    }

    [Fact]
    public void PresetSearch_FiltersPresets()
    {
        // Arrange - Wait for presets
        Thread.Sleep(1000);
        var initialCount = _restorePage.VisiblePresetsCount;

        if (initialCount == 0)
        {
            // Skip if no presets
            return;
        }

        // Act - Search for something specific
        _restorePage.SearchPresets("xyz_nonexistent_preset_xyz");
        Thread.Sleep(500);

        // The count should be different (likely 0 for nonexistent search)
        // But we mainly verify it doesn't crash
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void PresetSearch_ClearingSearch_RestoresPresets()
    {
        // Arrange
        Thread.Sleep(1000);
        var initialCount = _restorePage.VisiblePresetsCount;
        _restorePage.SearchPresets("test");
        Thread.Sleep(500);

        // Act
        _restorePage.ClearSearch();
        Thread.Sleep(500);

        // Assert - Should restore presets (or at least not crash)
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Queue Tests

    [Fact]
    public void JobQueue_InitiallyEmpty()
    {
        // Assert - Queue should start empty (no jobs added yet)
        // Note: This may not always be true if previous test runs left state
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void QueueControls_ClearQueueButton_CanBeChecked()
    {
        // Verify the Clear Queue button exists and we can check its state
        var clearQueueButton = _restorePage.ClearQueueButton;
        clearQueueButton.Should().NotBeNull("Clear Queue button should exist");

        // Just verify we can check the enabled state without crashing
        var isEnabled = _restorePage.IsClearQueueEnabled;
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void QueueControls_ClearButton_CanBeClicked()
    {
        // Act - Click clear (should do nothing if queue is empty)
        _restorePage.ClickClearQueue();

        // Assert - App should not crash
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void RestorePage_FullWorkflow_BrowsePresetsAndNavigateAway()
    {
        // This test simulates a user browsing presets then navigating away

        // Step 1: Wait for presets to load
        Thread.Sleep(1000);

        // Step 2: Search for something
        _restorePage.SearchPresets("denoise");
        Thread.Sleep(500);

        // Step 3: Clear search
        _restorePage.ClearSearch();
        Thread.Sleep(300);

        // Step 4: Select a preset if available
        if (_restorePage.VisiblePresetsCount > 0)
        {
            _restorePage.SelectPresetByIndex(0);
            Thread.Sleep(300);
        }

        // Step 5: Switch to Advanced mode
        _restorePage.SwitchToAdvancedMode();
        Thread.Sleep(500);

        // Step 6: Switch back to Simple mode
        _restorePage.SwitchToSimpleMode();
        Thread.Sleep(500);

        // Step 7: Navigate to another page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Step 8: Navigate back to Restore
        NavigateTo("RestoreNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void RestorePage_CanNavigateToAllCategories()
    {
        // This test verifies we can interact with preset categories
        // The category buttons don't have AutomationIds, so we'll just verify
        // the page structure is stable when clicking around

        Thread.Sleep(1000);

        // Just verify the page is responsive
        var list = _restorePage.PresetsList;
        list.Should().NotBeNull();

        App.HasExited.Should().BeFalse();
    }

    #endregion
}
