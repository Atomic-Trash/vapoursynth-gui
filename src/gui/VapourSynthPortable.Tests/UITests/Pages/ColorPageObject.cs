using FlaUI.Core.AutomationElements;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Color page, encapsulating UI interactions for testing.
/// </summary>
public class ColorPageObject
{
    private readonly Window _mainWindow;

    public ColorPageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Grade Import/Export

    /// <summary>
    /// Gets the Import Grade button.
    /// </summary>
    public Button? ImportGradeButton =>
        ElementFinder.Button(_mainWindow, "ImportGradeButton");

    /// <summary>
    /// Gets the Export Grade button.
    /// </summary>
    public Button? ExportGradeButton =>
        ElementFinder.Button(_mainWindow, "ExportGradeButton");

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Gets the Undo Grade button.
    /// </summary>
    public Button? UndoGradeButton =>
        ElementFinder.Button(_mainWindow, "UndoGradeButton");

    /// <summary>
    /// Gets the Redo Grade button.
    /// </summary>
    public Button? RedoGradeButton =>
        ElementFinder.Button(_mainWindow, "RedoGradeButton");

    #endregion

    #region Reset Buttons

    /// <summary>
    /// Gets the Reset All button.
    /// </summary>
    public Button? ResetAllButton =>
        ElementFinder.Button(_mainWindow, "ResetAllButton");

    /// <summary>
    /// Gets the Reset Wheels button.
    /// </summary>
    public Button? ResetWheelsButton =>
        ElementFinder.Button(_mainWindow, "ResetWheelsButton");

    /// <summary>
    /// Gets the Reset Adjustments button.
    /// </summary>
    public Button? ResetAdjustmentsButton =>
        ElementFinder.Button(_mainWindow, "ResetAdjustmentsButton");

    #endregion

    #region View Toggles

    /// <summary>
    /// Gets the Compare toggle.
    /// </summary>
    public AutomationElement? CompareToggle =>
        ElementFinder.ById(_mainWindow, "CompareToggle");

    /// <summary>
    /// Gets the Curves toggle.
    /// </summary>
    public AutomationElement? CurvesToggle =>
        ElementFinder.ById(_mainWindow, "CurvesToggle");

    /// <summary>
    /// Gets the Scopes toggle.
    /// </summary>
    public AutomationElement? ScopesToggle =>
        ElementFinder.ById(_mainWindow, "ScopesToggle");

    /// <summary>
    /// Toggles compare mode.
    /// </summary>
    public void ToggleCompare()
    {
        CompareToggle?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Toggles curves display.
    /// </summary>
    public void ToggleCurves()
    {
        CurvesToggle?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Toggles scopes display.
    /// </summary>
    public void ToggleScopes()
    {
        ScopesToggle?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Presets

    /// <summary>
    /// Gets the Preset Category combo box.
    /// </summary>
    public ComboBox? PresetCategoryComboBox =>
        ElementFinder.ById(_mainWindow, "PresetCategoryComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Presets list.
    /// </summary>
    public ListBox? PresetsList =>
        ElementFinder.ListBox(_mainWindow, "PresetsList");

    /// <summary>
    /// Gets the count of presets.
    /// </summary>
    public int PresetsCount
    {
        get
        {
            var list = PresetsList;
            return list?.Items.Length ?? 0;
        }
    }

    /// <summary>
    /// Selects a preset by index.
    /// </summary>
    public void SelectPresetByIndex(int index)
    {
        var list = PresetsList;
        if (list != null && index < list.Items.Length)
        {
            list.Items[index].Click();
            Thread.Sleep(300);
        }
    }

    #endregion

    #region LUTs

    /// <summary>
    /// Gets the LUT search box.
    /// </summary>
    public TextBox? LutSearchBox =>
        ElementFinder.TextBox(_mainWindow, "LutSearchBox");

    /// <summary>
    /// Gets the LUTs list.
    /// </summary>
    public ListBox? LutsList =>
        ElementFinder.ListBox(_mainWindow, "LutsList");

    /// <summary>
    /// Gets the Load LUT button.
    /// </summary>
    public Button? LoadLutButton =>
        ElementFinder.Button(_mainWindow, "LoadLutButton");

    /// <summary>
    /// Gets the Clear LUT button.
    /// </summary>
    public Button? ClearLutButton =>
        ElementFinder.Button(_mainWindow, "ClearLutButton");

    /// <summary>
    /// Searches for LUTs.
    /// </summary>
    public void SearchLuts(string query)
    {
        var searchBox = LutSearchBox;
        if (searchBox != null)
        {
            searchBox.Focus();
            searchBox.Text = query;
            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Gets the count of LUTs.
    /// </summary>
    public int LutsCount
    {
        get
        {
            var list = LutsList;
            return list?.Items.Length ?? 0;
        }
    }

    #endregion

    #region Action Methods

    /// <summary>
    /// Clicks the Reset All button.
    /// </summary>
    public void ClickResetAll()
    {
        ResetAllButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Reset Wheels button.
    /// </summary>
    public void ClickResetWheels()
    {
        ResetWheelsButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Reset Adjustments button.
    /// </summary>
    public void ClickResetAdjustments()
    {
        ResetAdjustmentsButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Clear LUT button.
    /// </summary>
    public void ClickClearLut()
    {
        ClearLutButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Color page is visible.
    /// </summary>
    public bool IsColorPageVisible =>
        ResetAllButton != null && CompareToggle != null;

    /// <summary>
    /// Checks if grade controls are visible.
    /// </summary>
    public bool HasGradeControls =>
        ImportGradeButton != null && ExportGradeButton != null;

    /// <summary>
    /// Checks if reset buttons are visible.
    /// </summary>
    public bool HasResetButtons =>
        ResetAllButton != null && ResetWheelsButton != null && ResetAdjustmentsButton != null;

    /// <summary>
    /// Checks if view toggles are visible.
    /// </summary>
    public bool HasViewToggles =>
        CompareToggle != null && CurvesToggle != null && ScopesToggle != null;

    /// <summary>
    /// Checks if preset controls are visible.
    /// </summary>
    public bool HasPresetControls =>
        PresetCategoryComboBox != null && PresetsList != null;

    /// <summary>
    /// Checks if LUT controls are visible.
    /// </summary>
    public bool HasLutControls =>
        LutSearchBox != null && LutsList != null && LoadLutButton != null && ClearLutButton != null;

    #endregion
}
