using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Settings page functionality.
/// </summary>
[Collection("UI Tests")]
public class SettingsPageTests : UITestBase
{
    private readonly SettingsPageObject _settingsPage;

    public SettingsPageTests()
    {
        LaunchApp();
        // Navigate to Settings page
        NavigateTo("SettingsNavButton");
        Thread.Sleep(500);
        _settingsPage = new SettingsPageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void SettingsPage_HasExportDefaultControls()
    {
        // Assert
        _settingsPage.DefaultFormatComboBox.Should().NotBeNull("Default Format combo box should exist");
        _settingsPage.DefaultVideoCodecComboBox.Should().NotBeNull("Default Video Codec combo box should exist");
        _settingsPage.DefaultAudioCodecComboBox.Should().NotBeNull("Default Audio Codec combo box should exist");
    }

    [Fact]
    public void SettingsPage_HasCacheControls()
    {
        // Assert
        _settingsPage.ClearCacheButton.Should().NotBeNull("Clear Cache button should exist");
    }

    [Fact]
    public void SettingsPage_HasProjectSettings()
    {
        // Assert
        _settingsPage.AutoSaveCheckbox.Should().NotBeNull("Auto-save checkbox should exist");
    }

    [Fact]
    public void SettingsPage_HasUISettings()
    {
        // Assert
        _settingsPage.ShowLogPanelCheckbox.Should().NotBeNull("Show Log Panel checkbox should exist");
        _settingsPage.ConfirmOnDeleteCheckbox.Should().NotBeNull("Confirm on Delete checkbox should exist");
    }

    [Fact]
    public void SettingsPage_HasPluginControls()
    {
        // Assert
        _settingsPage.RefreshPluginsButton.Should().NotBeNull("Refresh Plugins button should exist");
    }

    [Fact]
    public void SettingsPage_HasActionButtons()
    {
        // Assert
        _settingsPage.SaveSettingsButton.Should().NotBeNull("Save Settings button should exist");
        _settingsPage.CancelButton.Should().NotBeNull("Cancel button should exist");
        _settingsPage.ResetToDefaultsButton.Should().NotBeNull("Reset to Defaults button should exist");
    }

    #endregion

    #region Checkbox Tests

    [Fact]
    public void AutoSaveCheckbox_CanBeToggled()
    {
        // Act
        _settingsPage.ToggleAutoSave();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when toggling auto-save");
    }

    [Fact]
    public void ShowLogPanelCheckbox_CanBeToggled()
    {
        // Act
        _settingsPage.ToggleShowLogPanel();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when toggling show log panel");
    }

    [Fact]
    public void ConfirmOnDeleteCheckbox_CanBeToggled()
    {
        // Act
        _settingsPage.ToggleConfirmOnDelete();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when toggling confirm on delete");
    }

    #endregion

    #region Button Tests

    [Fact]
    public void ClearCacheButton_CanBeClicked()
    {
        // Act
        _settingsPage.ClickClearCache();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Clear Cache");
    }

    [Fact]
    public void RefreshPluginsButton_CanBeClicked()
    {
        // Act
        _settingsPage.ClickRefreshPlugins();
        Thread.Sleep(1000); // Allow for plugin refresh

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Refresh Plugins");
    }

    [Fact]
    public void CancelButton_CanBeClicked()
    {
        // Act
        _settingsPage.ClickCancel();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Cancel");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SettingsPage_FullWorkflow_BrowseSettingsAndNavigateAway()
    {
        // This test simulates a user browsing settings

        // Step 1: Wait for page to load
        Thread.Sleep(500);

        // Step 2: Toggle some checkboxes
        _settingsPage.ToggleShowLogPanel();
        Thread.Sleep(200);
        _settingsPage.ToggleConfirmOnDelete();
        Thread.Sleep(200);

        // Step 3: Click refresh plugins
        _settingsPage.ClickRefreshPlugins();
        Thread.Sleep(1000);

        // Step 4: Click Cancel (don't save changes)
        _settingsPage.ClickCancel();
        Thread.Sleep(300);

        // Step 5: Navigate to another page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Step 6: Navigate back to Settings
        NavigateTo("SettingsNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void SettingsPage_AllControlsAccessible()
    {
        // Verify all major control groups are accessible
        Thread.Sleep(500);

        // Assert
        _settingsPage.IsSettingsPageVisible.Should().BeTrue("Settings page should be visible");
        _settingsPage.HasExportDefaultControls.Should().BeTrue("Export default controls should be accessible");
        _settingsPage.HasCacheControls.Should().BeTrue("Cache controls should be accessible");
        _settingsPage.HasProjectSettings.Should().BeTrue("Project settings should be accessible");
        _settingsPage.HasUISettings.Should().BeTrue("UI settings should be accessible");
        _settingsPage.HasPluginControls.Should().BeTrue("Plugin controls should be accessible");
        _settingsPage.HasActionButtons.Should().BeTrue("Action buttons should be accessible");
        App.HasExited.Should().BeFalse();
    }

    #endregion
}
