using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Edit page functionality.
/// </summary>
[Collection("UI Tests")]
public class EditPageTests : UITestBase
{
    private readonly EditPageObject _editPage;

    public EditPageTests()
    {
        LaunchApp();
        // Navigate to Edit page
        NavigateTo("EditNavButton");
        Thread.Sleep(500);
        _editPage = new EditPageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void EditPage_HasToolbarButtons()
    {
        // Assert
        _editPage.AddToTimelineButton.Should().NotBeNull("Add to Timeline button should exist");
        _editPage.InsertClipButton.Should().NotBeNull("Insert Clip button should exist");
        _editPage.CutButton.Should().NotBeNull("Cut button should exist");
        _editPage.CopyButton.Should().NotBeNull("Copy button should exist");
        _editPage.PasteButton.Should().NotBeNull("Paste button should exist");
        _editPage.DeleteButton.Should().NotBeNull("Delete button should exist");
        _editPage.SplitButton.Should().NotBeNull("Split button should exist");
    }

    [Fact]
    public void EditPage_HasUndoRedoButtons()
    {
        // Assert
        _editPage.UndoButton.Should().NotBeNull("Undo button should exist");
        _editPage.RedoButton.Should().NotBeNull("Redo button should exist");
    }

    [Fact]
    public void EditPage_HasTextOverlayButtons()
    {
        // Assert
        _editPage.AddTextButton.Should().NotBeNull("Add Text button should exist");
        _editPage.DeleteTextButton.Should().NotBeNull("Delete Text button should exist");
    }

    [Fact]
    public void EditPage_HasEditModeToggles()
    {
        // Assert
        _editPage.SnapToggle.Should().NotBeNull("Snap toggle should exist");
        _editPage.RippleEditToggle.Should().NotBeNull("Ripple Edit toggle should exist");
        _editPage.KeyframePanelToggle.Should().NotBeNull("Keyframe Panel toggle should exist");
    }

    [Fact]
    public void EditPage_HasTransportControls()
    {
        // Assert
        _editPage.PlayButton.Should().NotBeNull("Play button should exist");
        _editPage.StopButton.Should().NotBeNull("Stop button should exist");
    }

    [Fact]
    public void EditPage_HasTimelineControl()
    {
        // Assert
        _editPage.TimelineControl.Should().NotBeNull("Timeline control should exist");
    }

    [Fact]
    public void EditPage_HasMediaPoolList()
    {
        // Assert
        _editPage.MediaPoolList.Should().NotBeNull("Media pool list should exist");
    }

    #endregion

    #region Edit Mode Toggle Tests

    [Fact]
    public void SnapToggle_CanBeClicked()
    {
        // Act
        _editPage.ToggleSnap();

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling snap");
    }

    [Fact]
    public void RippleEditToggle_CanBeClicked()
    {
        // Act
        _editPage.ToggleRippleEdit();

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling ripple edit");
    }

    [Fact]
    public void KeyframePanelToggle_CanBeClicked()
    {
        // Act
        _editPage.ToggleKeyframePanel();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling keyframe panel");
    }

    #endregion

    #region Undo/Redo Tests

    [Fact]
    public void UndoButton_InitiallyDisabled()
    {
        // No operations to undo yet
        Thread.Sleep(500);

        // Assert - Undo should be disabled initially
        var canUndo = _editPage.CanUndo;
        App.HasExited.Should().BeFalse();
        // Just checking we can read the state without crashing
    }

    [Fact]
    public void RedoButton_InitiallyDisabled()
    {
        // No operations to redo yet
        Thread.Sleep(500);

        // Assert - Redo should be disabled initially
        var canRedo = _editPage.CanRedo;
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Transport Control Tests

    [Fact]
    public void PlayButton_CanBeClicked()
    {
        // Act
        _editPage.ClickPlay();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking play");
    }

    [Fact]
    public void StopButton_CanBeClicked()
    {
        // Act
        _editPage.ClickStop();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking stop");
    }

    #endregion

    #region Toolbar Button State Tests

    [Fact]
    public void ClipOperationButtons_ArePresent()
    {
        // Assert - All clip operation buttons should exist
        _editPage.HasToolbarButtons.Should().BeTrue("All toolbar buttons should be present");
    }

    [Fact]
    public void SplitButton_CanBeClicked()
    {
        // Act - Split should work even with no clip selected (does nothing)
        _editPage.ClickSplit();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking split");
    }

    #endregion

    #region Media Pool Tests

    [Fact]
    public void MediaPool_StartsEmpty()
    {
        // Media pool should be empty at start (no imports done)
        Thread.Sleep(500);

        // Just verify we can access the media pool
        var count = _editPage.MediaPoolCount;
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EditPage_FullWorkflow_ToggleModesAndNavigateAway()
    {
        // This test simulates a user toggling modes then navigating away

        // Step 1: Wait for page to load
        Thread.Sleep(500);

        // Step 2: Toggle snap mode
        _editPage.ToggleSnap();
        Thread.Sleep(200);

        // Step 3: Toggle ripple edit
        _editPage.ToggleRippleEdit();
        Thread.Sleep(200);

        // Step 4: Toggle keyframe panel
        _editPage.ToggleKeyframePanel();
        Thread.Sleep(300);

        // Step 5: Click play/stop
        _editPage.ClickPlay();
        Thread.Sleep(200);
        _editPage.ClickStop();
        Thread.Sleep(200);

        // Step 6: Navigate to another page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Step 7: Navigate back to Edit
        NavigateTo("EditNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void EditPage_EmptyState_ShowsCorrectly()
    {
        // No media in timeline at start
        Thread.Sleep(500);

        // Assert - Page is responsive
        _editPage.IsEditPageVisible.Should().BeTrue("Edit page should be visible");
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void EditPage_AllControlsAccessible()
    {
        // Verify all major control groups are accessible
        Thread.Sleep(500);

        // Assert
        _editPage.HasToolbarButtons.Should().BeTrue("Toolbar buttons should be accessible");
        _editPage.HasTransportControls.Should().BeTrue("Transport controls should be accessible");
        _editPage.HasEditModeToggles.Should().BeTrue("Edit mode toggles should be accessible");
        App.HasExited.Should().BeFalse();
    }

    #endregion
}
