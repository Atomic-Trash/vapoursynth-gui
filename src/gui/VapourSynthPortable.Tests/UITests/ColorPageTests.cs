using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Color page functionality.
/// </summary>
[Collection("UI Tests")]
public class ColorPageTests : UITestBase
{
    private readonly ColorPageObject _colorPage;

    public ColorPageTests()
    {
        LaunchApp();
        // Navigate to Color page
        NavigateTo("ColorNavButton");
        Thread.Sleep(1500); // Allow extra time for page to fully render
        _colorPage = new ColorPageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void ColorPage_HasGradeControls()
    {
        // Assert
        _colorPage.ImportGradeButton.Should().NotBeNull("Import Grade button should exist");
        _colorPage.ExportGradeButton.Should().NotBeNull("Export Grade button should exist");
    }

    [Fact]
    public void ColorPage_HasUndoRedoButtons()
    {
        // Assert
        _colorPage.UndoGradeButton.Should().NotBeNull("Undo Grade button should exist");
        _colorPage.RedoGradeButton.Should().NotBeNull("Redo Grade button should exist");
    }

    [Fact]
    public void ColorPage_HasResetButtons()
    {
        // Assert
        _colorPage.ResetAllButton.Should().NotBeNull("Reset All button should exist");
        _colorPage.ResetWheelsButton.Should().NotBeNull("Reset Wheels button should exist");
        _colorPage.ResetAdjustmentsButton.Should().NotBeNull("Reset Adjustments button should exist");
    }

    [Fact]
    public void ColorPage_HasViewToggles()
    {
        // Assert
        _colorPage.CompareToggle.Should().NotBeNull("Compare toggle should exist");
        _colorPage.CurvesToggle.Should().NotBeNull("Curves toggle should exist");
        _colorPage.ScopesToggle.Should().NotBeNull("Scopes toggle should exist");
    }

    [Fact]
    public void ColorPage_HasPresetControls()
    {
        // Assert
        _colorPage.PresetCategoryComboBox.Should().NotBeNull("Preset Category combo box should exist");
        _colorPage.PresetsList.Should().NotBeNull("Presets list should exist");
    }

    [Fact]
    public void ColorPage_HasLutControls()
    {
        // Assert
        _colorPage.LutSearchBox.Should().NotBeNull("LUT search box should exist");
        _colorPage.LutsList.Should().NotBeNull("LUTs list should exist");
        _colorPage.LoadLutButton.Should().NotBeNull("Load LUT button should exist");
        _colorPage.ClearLutButton.Should().NotBeNull("Clear LUT button should exist");
    }

    #endregion

    #region View Toggle Tests

    [Fact]
    public void CompareToggle_CanBeClicked()
    {
        // Act
        _colorPage.ToggleCompare();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling compare");
    }

    [Fact]
    public void CurvesToggle_CanBeClicked()
    {
        // Act
        _colorPage.ToggleCurves();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling curves");
    }

    [Fact(Skip = "ScopesControl creates thousands of child elements that cause UI Automation timeouts")]
    public void ScopesToggle_CanBeClicked()
    {
        // This test is skipped because toggling scopes makes ScopesControl visible,
        // which creates thousands of Shape elements that overwhelm UI Automation.
        // The functionality works correctly - only the UI Automation is affected.

        // Act
        _colorPage.ToggleScopes();
        Thread.Sleep(500);

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when toggling scopes");
    }

    #endregion

    #region Reset Button Tests

    [Fact]
    public void ResetAllButton_CanBeClicked()
    {
        // Act
        _colorPage.ClickResetAll();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Reset All");
    }

    [Fact]
    public void ResetWheelsButton_CanBeClicked()
    {
        // Act
        _colorPage.ClickResetWheels();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Reset Wheels");
    }

    [Fact]
    public void ResetAdjustmentsButton_CanBeClicked()
    {
        // Act
        _colorPage.ClickResetAdjustments();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clicking Reset Adjustments");
    }

    #endregion

    #region LUT Tests

    [Fact]
    public void LutSearchBox_CanEnterQuery()
    {
        // Act
        _colorPage.SearchLuts("test");
        Thread.Sleep(500);

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when searching LUTs");
    }

    [Fact]
    public void ClearLutButton_CanBeClicked()
    {
        // Act
        _colorPage.ClickClearLut();

        // Assert
        App.HasExited.Should().BeFalse("App should not crash when clearing LUT");
    }

    #endregion

    #region Integration Tests

    [Fact(Skip = "Complex controls (CurvesControl) create many child elements that cause UI Automation timeouts when navigating")]
    public void ColorPage_FullWorkflow_ToggleControlsAndNavigateAway()
    {
        // This test is skipped because toggling Curves and Compare, then navigating
        // away and back, causes UI Automation timeouts due to many child elements
        // created by the visual controls (ScopesControl, CurvesControl).
        // The functionality works correctly - only the UI Automation is affected.

        // Step 1: Wait for page to load
        Thread.Sleep(500);

        // Step 2: Toggle view modes (skip Scopes due to UI Automation issues)
        _colorPage.ToggleCurves();
        Thread.Sleep(300);
        _colorPage.ToggleCompare();
        Thread.Sleep(300);

        // Step 3: Click reset buttons
        _colorPage.ClickResetWheels();
        Thread.Sleep(200);
        _colorPage.ClickResetAdjustments();
        Thread.Sleep(200);

        // Step 4: Search LUTs
        _colorPage.SearchLuts("film");
        Thread.Sleep(300);

        // Step 5: Navigate to another page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Step 6: Navigate back to Color
        NavigateTo("ColorNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void ColorPage_AllControlsAccessible()
    {
        // Verify all major control groups are accessible
        Thread.Sleep(500);

        // Assert
        _colorPage.IsColorPageVisible.Should().BeTrue("Color page should be visible");
        _colorPage.HasGradeControls.Should().BeTrue("Grade controls should be accessible");
        _colorPage.HasResetButtons.Should().BeTrue("Reset buttons should be accessible");
        _colorPage.HasViewToggles.Should().BeTrue("View toggles should be accessible");
        _colorPage.HasPresetControls.Should().BeTrue("Preset controls should be accessible");
        _colorPage.HasLutControls.Should().BeTrue("LUT controls should be accessible");
        App.HasExited.Should().BeFalse();
    }

    #endregion
}
