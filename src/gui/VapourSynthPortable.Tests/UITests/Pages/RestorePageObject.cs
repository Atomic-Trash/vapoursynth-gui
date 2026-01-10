using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Restore page, encapsulating UI interactions for testing.
/// </summary>
public class RestorePageObject
{
    private readonly Window _mainWindow;

    public RestorePageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Mode Selection

    /// <summary>
    /// Gets the Simple Mode radio button.
    /// </summary>
    public AutomationElement? SimpleModeButton =>
        ElementFinder.ById(_mainWindow, "SimpleModeButton");

    /// <summary>
    /// Gets the Advanced Mode radio button.
    /// </summary>
    public AutomationElement? AdvancedModeButton =>
        ElementFinder.ById(_mainWindow, "AdvancedModeButton");

    /// <summary>
    /// Checks if Simple Mode is currently selected.
    /// </summary>
    public bool IsSimpleModeSelected
    {
        get
        {
            var button = SimpleModeButton;
            if (button == null) return false;
            return button.Patterns.Toggle.PatternOrDefault?.ToggleState == ToggleState.On
                || button.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true;
        }
    }

    /// <summary>
    /// Switches to Simple Mode.
    /// </summary>
    public void SwitchToSimpleMode()
    {
        SimpleModeButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Switches to Advanced Mode.
    /// </summary>
    public void SwitchToAdvancedMode()
    {
        AdvancedModeButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Presets

    /// <summary>
    /// Gets the preset search box.
    /// </summary>
    public TextBox? PresetSearchBox =>
        ElementFinder.TextBox(_mainWindow, "PresetSearchBox");

    /// <summary>
    /// Gets the presets list.
    /// </summary>
    public ListBox? PresetsList =>
        ElementFinder.ListBox(_mainWindow, "PresetsList");

    /// <summary>
    /// Searches for presets by entering text in the search box.
    /// </summary>
    public void SearchPresets(string query)
    {
        var searchBox = PresetSearchBox;
        if (searchBox != null)
        {
            searchBox.Focus();
            searchBox.Text = query;
            Thread.Sleep(300); // Allow filtering
        }
    }

    /// <summary>
    /// Clears the preset search.
    /// </summary>
    public void ClearSearch()
    {
        var searchBox = PresetSearchBox;
        if (searchBox != null)
        {
            searchBox.Text = "";
            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Gets the count of visible presets in the list.
    /// </summary>
    public int VisiblePresetsCount
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

    /// <summary>
    /// Selects a preset by name.
    /// </summary>
    public bool SelectPresetByName(string name)
    {
        var list = PresetsList;
        if (list == null) return false;

        foreach (var item in list.Items)
        {
            if (item.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true)
            {
                item.Click();
                Thread.Sleep(300);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the currently selected preset name, if any.
    /// </summary>
    public string? SelectedPresetName
    {
        get
        {
            var list = PresetsList;
            if (list == null) return null;

            foreach (var item in list.Items)
            {
                if (item.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true)
                {
                    return item.Name;
                }
            }
            return null;
        }
    }

    #endregion

    #region Action Buttons

    /// <summary>
    /// Gets the Preview button.
    /// </summary>
    public Button? PreviewButton =>
        ElementFinder.Button(_mainWindow, "PreviewButton");

    /// <summary>
    /// Gets the Process Now button.
    /// </summary>
    public Button? ProcessNowButton =>
        ElementFinder.Button(_mainWindow, "ProcessNowButton");

    /// <summary>
    /// Gets the Add to Queue button.
    /// </summary>
    public Button? AddToQueueButton =>
        ElementFinder.Button(_mainWindow, "AddToQueueButton");

    /// <summary>
    /// Clicks the Preview button.
    /// </summary>
    public void ClickPreview()
    {
        PreviewButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Process Now button.
    /// </summary>
    public void ClickProcessNow()
    {
        ProcessNowButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Add to Queue button.
    /// </summary>
    public void ClickAddToQueue()
    {
        AddToQueueButton?.Click();
        Thread.Sleep(500);
    }

    #endregion

    #region Job Queue

    /// <summary>
    /// Gets the job queue list.
    /// </summary>
    public ListBox? JobQueueList =>
        ElementFinder.ListBox(_mainWindow, "JobQueueList");

    /// <summary>
    /// Gets the count of jobs in the queue.
    /// </summary>
    public int JobQueueCount
    {
        get
        {
            var list = JobQueueList;
            return list?.Items.Length ?? 0;
        }
    }

    /// <summary>
    /// Gets the Process Queue button.
    /// </summary>
    public Button? ProcessQueueButton =>
        ElementFinder.Button(_mainWindow, "ProcessQueueButton");

    /// <summary>
    /// Gets the Cancel Processing button.
    /// </summary>
    public Button? CancelProcessingButton =>
        ElementFinder.Button(_mainWindow, "CancelProcessingButton");

    /// <summary>
    /// Gets the Clear Queue button.
    /// </summary>
    public Button? ClearQueueButton =>
        ElementFinder.Button(_mainWindow, "ClearQueueButton");

    /// <summary>
    /// Clicks the Process Queue button to start processing.
    /// </summary>
    public void ClickProcessQueue()
    {
        ProcessQueueButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Cancel Processing button.
    /// </summary>
    public void ClickCancelProcessing()
    {
        CancelProcessingButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Clear Queue button.
    /// </summary>
    public void ClickClearQueue()
    {
        ClearQueueButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Checks if the Process Queue button is enabled.
    /// </summary>
    public bool IsProcessQueueEnabled =>
        ProcessQueueButton?.IsEnabled ?? false;

    /// <summary>
    /// Checks if the Cancel Processing button is enabled.
    /// </summary>
    public bool IsCancelProcessingEnabled =>
        CancelProcessingButton?.IsEnabled ?? false;

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Restore page is visible and in Simple mode.
    /// </summary>
    public bool IsSimpleModeVisible =>
        SimpleModeButton != null && PresetsList != null;

    /// <summary>
    /// Checks if any presets are loaded.
    /// </summary>
    public bool HasPresets => VisiblePresetsCount > 0;

    /// <summary>
    /// Waits for presets to load.
    /// </summary>
    public bool WaitForPresetsToLoad(TimeSpan? timeout = null)
    {
        return WaitHelpers.WaitUntil(() => VisiblePresetsCount > 0, timeout);
    }

    #endregion
}
