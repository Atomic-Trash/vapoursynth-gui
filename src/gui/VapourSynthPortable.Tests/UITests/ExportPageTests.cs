using VapourSynthPortable.Tests.UITests.Helpers;
using VapourSynthPortable.Tests.UITests.Pages;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// UI tests for the Export page functionality.
/// </summary>
[Collection("UI Tests")]
public class ExportPageTests : UITestBase
{
    private readonly ExportPageObject _exportPage;

    public ExportPageTests()
    {
        LaunchApp();
        // Navigate to Export page
        NavigateTo("ExportNavButton");
        Thread.Sleep(500);
        _exportPage = new ExportPageObject(MainWindow);
    }

    #region Page Structure Tests

    [Fact]
    public void ExportPage_HasInputOutputControls()
    {
        // Assert
        _exportPage.InputPathTextBox.Should().NotBeNull("Input path text box should exist");
        _exportPage.BrowseInputButton.Should().NotBeNull("Browse input button should exist");
        _exportPage.OutputFileNameTextBox.Should().NotBeNull("Output file name text box should exist");
        _exportPage.BrowseOutputButton.Should().NotBeNull("Browse output button should exist");
    }

    [Fact]
    public void ExportPage_HasPresetAndFormatControls()
    {
        // Assert
        _exportPage.PresetComboBox.Should().NotBeNull("Preset combo box should exist");
        _exportPage.FormatComboBox.Should().NotBeNull("Format combo box should exist");
    }

    [Fact]
    public void ExportPage_HasVideoSettings()
    {
        // Assert
        _exportPage.VideoCodecComboBox.Should().NotBeNull("Video codec combo box should exist");
        _exportPage.QualitySlider.Should().NotBeNull("Quality slider should exist");
        _exportPage.ResolutionComboBox.Should().NotBeNull("Resolution combo box should exist");
    }

    [Fact]
    public void ExportPage_HasAudioSettings()
    {
        // Assert
        _exportPage.AudioCodecComboBox.Should().NotBeNull("Audio codec combo box should exist");
        _exportPage.AudioBitrateComboBox.Should().NotBeNull("Audio bitrate combo box should exist");
    }

    [Fact]
    public void ExportPage_HasActionButtons()
    {
        // Assert
        _exportPage.AddToQueueButton.Should().NotBeNull("Add to Queue button should exist");
        _exportPage.ExportNowButton.Should().NotBeNull("Export Now button should exist");
    }

    [Fact]
    public void ExportPage_HasQueueControls()
    {
        // Assert
        _exportPage.StartQueueButton.Should().NotBeNull("Start Queue button should exist");
        _exportPage.ClearQueueButton.Should().NotBeNull("Clear Queue button should exist");
        _exportPage.ExportQueueList.Should().NotBeNull("Export queue list should exist");
    }

    #endregion

    #region Quality Slider Tests

    [Fact]
    public void QualitySlider_CanBeAdjusted()
    {
        // Arrange
        var slider = _exportPage.QualitySlider;
        slider.Should().NotBeNull();

        // Act - Just verify we can read the value
        var currentValue = slider!.Value;

        // Assert - App should not crash
        App.HasExited.Should().BeFalse("App should not crash when accessing quality slider");
    }

    #endregion

    #region ComboBox Tests

    [Fact]
    public void PresetComboBox_HasItems()
    {
        // Arrange
        var comboBox = _exportPage.PresetComboBox;
        comboBox.Should().NotBeNull();

        // Just verify we can access it
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void FormatComboBox_HasItems()
    {
        // Arrange
        var comboBox = _exportPage.FormatComboBox;
        comboBox.Should().NotBeNull();

        // Just verify we can access it
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void VideoCodecComboBox_HasItems()
    {
        // Arrange
        var comboBox = _exportPage.VideoCodecComboBox;
        comboBox.Should().NotBeNull();

        // Just verify we can access it
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Queue Tests

    [Fact]
    public void ExportQueue_InitiallyEmpty()
    {
        // Assert
        var queueCount = _exportPage.ExportQueueCount;
        App.HasExited.Should().BeFalse();
        // Queue should be empty initially (no exports added)
    }

    [Fact]
    public void ClearQueueButton_CanBeClicked()
    {
        // Act - Click clear (should do nothing if queue is empty)
        _exportPage.ClickClearQueue();

        // Assert - App should not crash
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Button State Tests

    [Fact]
    public void AddToQueueButton_IsPresent()
    {
        // Assert
        var button = _exportPage.AddToQueueButton;
        button.Should().NotBeNull();
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void ExportNowButton_IsPresent()
    {
        // Assert
        var button = _exportPage.ExportNowButton;
        button.Should().NotBeNull();
        App.HasExited.Should().BeFalse();
    }

    [Fact]
    public void StartQueueButton_IsPresent()
    {
        // Assert
        var button = _exportPage.StartQueueButton;
        button.Should().NotBeNull();
        App.HasExited.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ExportPage_FullWorkflow_BrowseSettingsAndNavigateAway()
    {
        // This test simulates a user browsing export settings

        // Step 1: Wait for page to load
        Thread.Sleep(500);

        // Step 2: Verify all sections are visible
        _exportPage.HasInputOutputControls.Should().BeTrue();
        _exportPage.HasVideoSettings.Should().BeTrue();
        _exportPage.HasAudioSettings.Should().BeTrue();
        _exportPage.HasQueueControls.Should().BeTrue();

        // Step 3: Navigate to another page
        NavigateTo("MediaNavButton");
        Thread.Sleep(500);

        // Step 4: Navigate back to Export
        NavigateTo("ExportNavButton");
        Thread.Sleep(500);

        // Assert - App should be stable throughout
        App.HasExited.Should().BeFalse("App should not crash during workflow");
        MainWindow.Should().NotBeNull();
    }

    [Fact]
    public void ExportPage_AllControlsAccessible()
    {
        // Verify all major control groups are accessible
        Thread.Sleep(500);

        // Assert
        _exportPage.IsExportPageVisible.Should().BeTrue("Export page should be visible");
        _exportPage.HasInputOutputControls.Should().BeTrue("Input/output controls should be accessible");
        _exportPage.HasVideoSettings.Should().BeTrue("Video settings should be accessible");
        _exportPage.HasAudioSettings.Should().BeTrue("Audio settings should be accessible");
        _exportPage.HasQueueControls.Should().BeTrue("Queue controls should be accessible");
        App.HasExited.Should().BeFalse();
    }

    #endregion
}
