using FlaUI.Core.AutomationElements;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Export page, encapsulating UI interactions for testing.
/// </summary>
public class ExportPageObject
{
    private readonly Window _mainWindow;

    public ExportPageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Input/Output Controls

    /// <summary>
    /// Gets the Input Path text box.
    /// </summary>
    public TextBox? InputPathTextBox =>
        ElementFinder.TextBox(_mainWindow, "InputPathTextBox");

    /// <summary>
    /// Gets the Browse Input button.
    /// </summary>
    public Button? BrowseInputButton =>
        ElementFinder.Button(_mainWindow, "BrowseInputButton");

    /// <summary>
    /// Gets the Output File Name text box.
    /// </summary>
    public TextBox? OutputFileNameTextBox =>
        ElementFinder.TextBox(_mainWindow, "OutputFileNameTextBox");

    /// <summary>
    /// Gets the Browse Output button.
    /// </summary>
    public Button? BrowseOutputButton =>
        ElementFinder.Button(_mainWindow, "BrowseOutputButton");

    #endregion

    #region Preset and Format

    /// <summary>
    /// Gets the Preset combo box.
    /// </summary>
    public ComboBox? PresetComboBox =>
        ElementFinder.ById(_mainWindow, "PresetComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Format combo box.
    /// </summary>
    public ComboBox? FormatComboBox =>
        ElementFinder.ById(_mainWindow, "FormatComboBox")?.AsComboBox();

    #endregion

    #region Video Settings

    /// <summary>
    /// Gets the Video Codec combo box.
    /// </summary>
    public ComboBox? VideoCodecComboBox =>
        ElementFinder.ById(_mainWindow, "VideoCodecComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Quality slider.
    /// </summary>
    public Slider? QualitySlider =>
        ElementFinder.ById(_mainWindow, "QualitySlider")?.AsSlider();

    /// <summary>
    /// Gets the Resolution combo box.
    /// </summary>
    public ComboBox? ResolutionComboBox =>
        ElementFinder.ById(_mainWindow, "ResolutionComboBox")?.AsComboBox();

    #endregion

    #region Audio Settings

    /// <summary>
    /// Gets the Audio Codec combo box.
    /// </summary>
    public ComboBox? AudioCodecComboBox =>
        ElementFinder.ById(_mainWindow, "AudioCodecComboBox")?.AsComboBox();

    /// <summary>
    /// Gets the Audio Bitrate combo box.
    /// </summary>
    public ComboBox? AudioBitrateComboBox =>
        ElementFinder.ById(_mainWindow, "AudioBitrateComboBox")?.AsComboBox();

    #endregion

    #region Action Buttons

    /// <summary>
    /// Gets the Add to Queue button.
    /// </summary>
    public Button? AddToQueueButton =>
        ElementFinder.Button(_mainWindow, "AddToQueueButton");

    /// <summary>
    /// Gets the Export Now button.
    /// </summary>
    public Button? ExportNowButton =>
        ElementFinder.Button(_mainWindow, "ExportNowButton");

    /// <summary>
    /// Gets the Cancel Export button.
    /// </summary>
    public Button? CancelExportButton =>
        ElementFinder.Button(_mainWindow, "CancelExportButton");

    /// <summary>
    /// Gets the Start Queue button.
    /// </summary>
    public Button? StartQueueButton =>
        ElementFinder.Button(_mainWindow, "StartQueueButton");

    /// <summary>
    /// Gets the Clear Queue button.
    /// </summary>
    public Button? ClearQueueButton =>
        ElementFinder.Button(_mainWindow, "ClearQueueButton");

    #endregion

    #region Export Queue

    /// <summary>
    /// Gets the export queue list.
    /// </summary>
    public ListBox? ExportQueueList =>
        ElementFinder.ListBox(_mainWindow, "ExportQueueList");

    /// <summary>
    /// Gets the count of items in the export queue.
    /// </summary>
    public int ExportQueueCount
    {
        get
        {
            var list = ExportQueueList;
            return list?.Items.Length ?? 0;
        }
    }

    #endregion

    #region Action Methods

    /// <summary>
    /// Clicks the Add to Queue button.
    /// </summary>
    public void ClickAddToQueue()
    {
        AddToQueueButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Export Now button.
    /// </summary>
    public void ClickExportNow()
    {
        ExportNowButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Start Queue button.
    /// </summary>
    public void ClickStartQueue()
    {
        StartQueueButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Clear Queue button.
    /// </summary>
    public void ClickClearQueue()
    {
        ClearQueueButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Sets the quality slider value.
    /// </summary>
    public void SetQuality(double value)
    {
        var slider = QualitySlider;
        if (slider != null)
        {
            slider.Value = value;
            Thread.Sleep(200);
        }
    }

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Export page is visible.
    /// </summary>
    public bool IsExportPageVisible =>
        ExportNowButton != null && PresetComboBox != null;

    /// <summary>
    /// Checks if input/output controls are visible.
    /// </summary>
    public bool HasInputOutputControls =>
        InputPathTextBox != null && OutputFileNameTextBox != null;

    /// <summary>
    /// Checks if video settings are visible.
    /// </summary>
    public bool HasVideoSettings =>
        VideoCodecComboBox != null && QualitySlider != null;

    /// <summary>
    /// Checks if audio settings are visible.
    /// </summary>
    public bool HasAudioSettings =>
        AudioCodecComboBox != null && AudioBitrateComboBox != null;

    /// <summary>
    /// Checks if queue controls are visible.
    /// </summary>
    public bool HasQueueControls =>
        StartQueueButton != null && ClearQueueButton != null && ExportQueueList != null;

    /// <summary>
    /// Checks if Export Now button is enabled.
    /// </summary>
    public bool IsExportNowEnabled =>
        ExportNowButton?.IsEnabled ?? false;

    #endregion
}
