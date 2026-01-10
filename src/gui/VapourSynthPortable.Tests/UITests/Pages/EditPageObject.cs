using FlaUI.Core.AutomationElements;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Edit page, encapsulating UI interactions for testing.
/// </summary>
public class EditPageObject
{
    private readonly Window _mainWindow;

    public EditPageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Clip Operations

    /// <summary>
    /// Gets the Add to Timeline button.
    /// </summary>
    public Button? AddToTimelineButton =>
        ElementFinder.Button(_mainWindow, "AddToTimelineButton");

    /// <summary>
    /// Gets the Insert Clip button.
    /// </summary>
    public Button? InsertClipButton =>
        ElementFinder.Button(_mainWindow, "InsertClipButton");

    /// <summary>
    /// Gets the Cut button.
    /// </summary>
    public Button? CutButton =>
        ElementFinder.Button(_mainWindow, "CutButton");

    /// <summary>
    /// Gets the Copy button.
    /// </summary>
    public Button? CopyButton =>
        ElementFinder.Button(_mainWindow, "CopyButton");

    /// <summary>
    /// Gets the Paste button.
    /// </summary>
    public Button? PasteButton =>
        ElementFinder.Button(_mainWindow, "PasteButton");

    /// <summary>
    /// Gets the Delete button.
    /// </summary>
    public Button? DeleteButton =>
        ElementFinder.Button(_mainWindow, "DeleteButton");

    /// <summary>
    /// Gets the Split button.
    /// </summary>
    public Button? SplitButton =>
        ElementFinder.Button(_mainWindow, "SplitButton");

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Gets the Undo button.
    /// </summary>
    public Button? UndoButton =>
        ElementFinder.Button(_mainWindow, "UndoButton");

    /// <summary>
    /// Gets the Redo button.
    /// </summary>
    public Button? RedoButton =>
        ElementFinder.Button(_mainWindow, "RedoButton");

    /// <summary>
    /// Checks if Undo is available.
    /// </summary>
    public bool CanUndo => UndoButton?.IsEnabled ?? false;

    /// <summary>
    /// Checks if Redo is available.
    /// </summary>
    public bool CanRedo => RedoButton?.IsEnabled ?? false;

    /// <summary>
    /// Clicks the Undo button.
    /// </summary>
    public void ClickUndo()
    {
        UndoButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Redo button.
    /// </summary>
    public void ClickRedo()
    {
        RedoButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Text Overlay

    /// <summary>
    /// Gets the Add Text button.
    /// </summary>
    public Button? AddTextButton =>
        ElementFinder.Button(_mainWindow, "AddTextButton");

    /// <summary>
    /// Gets the Delete Text button.
    /// </summary>
    public Button? DeleteTextButton =>
        ElementFinder.Button(_mainWindow, "DeleteTextButton");

    #endregion

    #region Edit Mode Toggles

    /// <summary>
    /// Gets the Snap toggle.
    /// </summary>
    public AutomationElement? SnapToggle =>
        ElementFinder.ById(_mainWindow, "SnapToggle");

    /// <summary>
    /// Gets the Ripple Edit toggle.
    /// </summary>
    public AutomationElement? RippleEditToggle =>
        ElementFinder.ById(_mainWindow, "RippleEditToggle");

    /// <summary>
    /// Gets the Keyframe Panel toggle.
    /// </summary>
    public AutomationElement? KeyframePanelToggle =>
        ElementFinder.ById(_mainWindow, "KeyframePanelToggle");

    /// <summary>
    /// Toggles snap mode.
    /// </summary>
    public void ToggleSnap()
    {
        SnapToggle?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Toggles ripple edit mode.
    /// </summary>
    public void ToggleRippleEdit()
    {
        RippleEditToggle?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Toggles keyframe panel.
    /// </summary>
    public void ToggleKeyframePanel()
    {
        KeyframePanelToggle?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Media Pool

    /// <summary>
    /// Gets the media pool list.
    /// </summary>
    public ListBox? MediaPoolList =>
        ElementFinder.ListBox(_mainWindow, "MediaPoolList");

    /// <summary>
    /// Gets the count of items in the media pool.
    /// </summary>
    public int MediaPoolCount
    {
        get
        {
            var list = MediaPoolList;
            return list?.Items.Length ?? 0;
        }
    }

    /// <summary>
    /// Selects a media item by index.
    /// </summary>
    public void SelectMediaByIndex(int index)
    {
        var list = MediaPoolList;
        if (list != null && index < list.Items.Length)
        {
            list.Items[index].Click();
            Thread.Sleep(300);
        }
    }

    #endregion

    #region Transport Controls

    /// <summary>
    /// Gets the Play button.
    /// </summary>
    public Button? PlayButton =>
        ElementFinder.Button(_mainWindow, "PlayButton");

    /// <summary>
    /// Gets the Stop button.
    /// </summary>
    public Button? StopButton =>
        ElementFinder.Button(_mainWindow, "StopButton");

    /// <summary>
    /// Clicks the Play button.
    /// </summary>
    public void ClickPlay()
    {
        PlayButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Stop button.
    /// </summary>
    public void ClickStop()
    {
        StopButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Timeline

    /// <summary>
    /// Gets the Timeline control.
    /// </summary>
    public AutomationElement? TimelineControl =>
        ElementFinder.ById(_mainWindow, "TimelineControl");

    #endregion

    #region Action Methods

    /// <summary>
    /// Clicks the Add to Timeline button.
    /// </summary>
    public void ClickAddToTimeline()
    {
        AddToTimelineButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Insert Clip button.
    /// </summary>
    public void ClickInsertClip()
    {
        InsertClipButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Split button.
    /// </summary>
    public void ClickSplit()
    {
        SplitButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Delete button.
    /// </summary>
    public void ClickDelete()
    {
        DeleteButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Edit page is visible.
    /// </summary>
    public bool IsEditPageVisible =>
        PlayButton != null && TimelineControl != null;

    /// <summary>
    /// Checks if the toolbar buttons are visible.
    /// </summary>
    public bool HasToolbarButtons =>
        AddToTimelineButton != null && UndoButton != null && SplitButton != null;

    /// <summary>
    /// Checks if transport controls are visible.
    /// </summary>
    public bool HasTransportControls =>
        PlayButton != null && StopButton != null;

    /// <summary>
    /// Checks if edit mode toggles are visible.
    /// </summary>
    public bool HasEditModeToggles =>
        SnapToggle != null && RippleEditToggle != null && KeyframePanelToggle != null;

    #endregion
}
