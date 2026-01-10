using FlaUI.Core.AutomationElements;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Settings page, encapsulating UI interactions for testing.
/// </summary>
public class SettingsPageObject
{
    private readonly Window _mainWindow;

    public SettingsPageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Export Defaults

    /// <summary>
    /// Gets the Default Format combo box.
    /// </summary>
    public ComboBox? DefaultFormatComboBox =>
        ElementFinder.ById(_mainWindow, "DefaultFormatComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Default Video Codec combo box.
    /// </summary>
    public ComboBox? DefaultVideoCodecComboBox =>
        ElementFinder.ById(_mainWindow, "DefaultVideoCodecComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Default Audio Codec combo box.
    /// </summary>
    public ComboBox? DefaultAudioCodecComboBox =>
        ElementFinder.ById(_mainWindow, "DefaultAudioCodecComboBox")?.AsComboBox();

    #endregion

    #region Cache Controls

    /// <summary>
    /// Gets the Clear Cache button.
    /// </summary>
    public Button? ClearCacheButton =>
        ElementFinder.Button(_mainWindow, "ClearCacheButton");

    #endregion

    #region Project Settings

    /// <summary>
    /// Gets the Auto-save checkbox.
    /// </summary>
    public CheckBox? AutoSaveCheckbox =>
        ElementFinder.ById(_mainWindow, "AutoSaveCheckbox")?.AsCheckBox();

    #endregion

    #region UI Settings

    /// <summary>
    /// Gets the Show Log Panel checkbox.
    /// </summary>
    public CheckBox? ShowLogPanelCheckbox =>
        ElementFinder.ById(_mainWindow, "ShowLogPanelCheckbox")?.AsCheckBox();

    /// <summary>
    /// Gets the Confirm on Delete checkbox.
    /// </summary>
    public CheckBox? ConfirmOnDeleteCheckbox =>
        ElementFinder.ById(_mainWindow, "ConfirmOnDeleteCheckbox")?.AsCheckBox();

    #endregion

    #region Plugin Controls

    /// <summary>
    /// Gets the Refresh Plugins button.
    /// </summary>
    public Button? RefreshPluginsButton =>
        ElementFinder.Button(_mainWindow, "RefreshPluginsButton");

    #endregion

    #region Action Buttons

    /// <summary>
    /// Gets the Reset to Defaults button.
    /// </summary>
    public Button? ResetToDefaultsButton =>
        ElementFinder.Button(_mainWindow, "ResetToDefaultsButton");

    /// <summary>
    /// Gets the Cancel button.
    /// </summary>
    public Button? CancelButton =>
        ElementFinder.Button(_mainWindow, "CancelButton");

    /// <summary>
    /// Gets the Save Settings button.
    /// </summary>
    public Button? SaveSettingsButton =>
        ElementFinder.Button(_mainWindow, "SaveSettingsButton");

    #endregion

    #region Action Methods

    /// <summary>
    /// Clicks the Clear Cache button.
    /// </summary>
    public void ClickClearCache()
    {
        ClearCacheButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Refresh Plugins button.
    /// </summary>
    public void ClickRefreshPlugins()
    {
        RefreshPluginsButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Reset to Defaults button.
    /// </summary>
    public void ClickResetToDefaults()
    {
        ResetToDefaultsButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Cancel button.
    /// </summary>
    public void ClickCancel()
    {
        CancelButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Save Settings button.
    /// </summary>
    public void ClickSaveSettings()
    {
        SaveSettingsButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Toggles auto-save.
    /// </summary>
    public void ToggleAutoSave()
    {
        AutoSaveCheckbox?.Click();
        Thread.Sleep(200);
    }

    /// <summary>
    /// Toggles show log panel.
    /// </summary>
    public void ToggleShowLogPanel()
    {
        ShowLogPanelCheckbox?.Click();
        Thread.Sleep(200);
    }

    /// <summary>
    /// Toggles confirm on delete.
    /// </summary>
    public void ToggleConfirmOnDelete()
    {
        ConfirmOnDeleteCheckbox?.Click();
        Thread.Sleep(200);
    }

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Settings page is visible.
    /// </summary>
    public bool IsSettingsPageVisible =>
        SaveSettingsButton != null && CancelButton != null;

    /// <summary>
    /// Checks if export default controls are visible.
    /// </summary>
    public bool HasExportDefaultControls =>
        DefaultFormatComboBox != null && DefaultVideoCodecComboBox != null && DefaultAudioCodecComboBox != null;

    /// <summary>
    /// Checks if cache controls are visible.
    /// </summary>
    public bool HasCacheControls =>
        ClearCacheButton != null;

    /// <summary>
    /// Checks if project settings are visible.
    /// </summary>
    public bool HasProjectSettings =>
        AutoSaveCheckbox != null;

    /// <summary>
    /// Checks if UI settings are visible.
    /// </summary>
    public bool HasUISettings =>
        ShowLogPanelCheckbox != null && ConfirmOnDeleteCheckbox != null;

    /// <summary>
    /// Checks if plugin controls are visible.
    /// </summary>
    public bool HasPluginControls =>
        RefreshPluginsButton != null;

    /// <summary>
    /// Checks if action buttons are visible.
    /// </summary>
    public bool HasActionButtons =>
        SaveSettingsButton != null && CancelButton != null && ResetToDefaultsButton != null;

    #endregion
}
