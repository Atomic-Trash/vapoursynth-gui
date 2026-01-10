using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VapourSynthPortable.Tests.UITests.Helpers;

namespace VapourSynthPortable.Tests.UITests.Pages;

/// <summary>
/// Page Object for the Media page, encapsulating UI interactions for testing.
/// </summary>
public class MediaPageObject
{
    private readonly Window _mainWindow;

    public MediaPageObject(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    #region Import Controls

    /// <summary>
    /// Gets the Import Media button.
    /// </summary>
    public Button? ImportMediaButton =>
        ElementFinder.Button(_mainWindow, "ImportMediaButton");

    /// <summary>
    /// Gets the Import Folder button.
    /// </summary>
    public Button? ImportFolderButton =>
        ElementFinder.Button(_mainWindow, "ImportFolderButton");

    /// <summary>
    /// Gets the Create Bin button.
    /// </summary>
    public Button? CreateBinButton =>
        ElementFinder.Button(_mainWindow, "CreateBinButton");

    #endregion

    #region Search Controls

    /// <summary>
    /// Gets the media search box.
    /// </summary>
    public TextBox? MediaSearchBox =>
        ElementFinder.TextBox(_mainWindow, "MediaSearchBox");

    /// <summary>
    /// Gets the clear search button.
    /// </summary>
    public Button? ClearSearchButton =>
        ElementFinder.Button(_mainWindow, "ClearSearchButton");

    /// <summary>
    /// Searches for media by entering text in the search box.
    /// </summary>
    public void SearchMedia(string query)
    {
        var searchBox = MediaSearchBox;
        if (searchBox != null)
        {
            searchBox.Focus();
            searchBox.Text = query;
            Thread.Sleep(300); // Allow filtering
        }
    }

    /// <summary>
    /// Clears the media search.
    /// </summary>
    public void ClearSearch()
    {
        var clearButton = ClearSearchButton;
        if (clearButton != null && clearButton.IsEnabled)
        {
            clearButton.Click();
            Thread.Sleep(300);
        }
        else
        {
            var searchBox = MediaSearchBox;
            if (searchBox != null)
            {
                searchBox.Text = "";
                Thread.Sleep(300);
            }
        }
    }

    #endregion

    #region View Mode Controls

    /// <summary>
    /// Gets the Grid View radio button.
    /// </summary>
    public AutomationElement? GridViewButton =>
        ElementFinder.ById(_mainWindow, "GridViewButton");

    /// <summary>
    /// Gets the List View radio button.
    /// </summary>
    public AutomationElement? ListViewButton =>
        ElementFinder.ById(_mainWindow, "ListViewButton");

    /// <summary>
    /// Switches to Grid view mode.
    /// </summary>
    public void SwitchToGridView()
    {
        GridViewButton?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Switches to List view mode.
    /// </summary>
    public void SwitchToListView()
    {
        ListViewButton?.Click();
        Thread.Sleep(300);
    }

    #endregion

    #region Media Lists

    /// <summary>
    /// Gets the media items list.
    /// </summary>
    public ListBox? MediaItemsList =>
        ElementFinder.ListBox(_mainWindow, "MediaItemsList");

    /// <summary>
    /// Gets the bins list.
    /// </summary>
    public ListBox? BinsList =>
        ElementFinder.ListBox(_mainWindow, "BinsList");

    /// <summary>
    /// Gets the count of visible media items.
    /// </summary>
    public int VisibleMediaCount
    {
        get
        {
            var list = MediaItemsList;
            return list?.Items.Length ?? 0;
        }
    }

    /// <summary>
    /// Gets the count of bins.
    /// </summary>
    public int BinsCount
    {
        get
        {
            var list = BinsList;
            return list?.Items.Length ?? 0;
        }
    }

    /// <summary>
    /// Selects a media item by index.
    /// </summary>
    public void SelectMediaByIndex(int index)
    {
        var list = MediaItemsList;
        if (list != null && index < list.Items.Length)
        {
            list.Items[index].Click();
            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Selects a bin by index.
    /// </summary>
    public void SelectBinByIndex(int index)
    {
        var list = BinsList;
        if (list != null && index < list.Items.Length)
        {
            list.Items[index].Click();
            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Selects a bin by name.
    /// </summary>
    public bool SelectBinByName(string name)
    {
        var list = BinsList;
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

    #endregion

    #region Preview Controls

    /// <summary>
    /// Gets the preview toggle button.
    /// </summary>
    public AutomationElement? PreviewToggle =>
        ElementFinder.ById(_mainWindow, "PreviewToggle");

    /// <summary>
    /// Gets the Play Selected button.
    /// </summary>
    public Button? PlaySelectedButton =>
        ElementFinder.Button(_mainWindow, "PlaySelectedButton");

    /// <summary>
    /// Toggles the preview panel.
    /// </summary>
    public void TogglePreviewPanel()
    {
        PreviewToggle?.Click();
        Thread.Sleep(300);
    }

    /// <summary>
    /// Clicks the Play Selected button.
    /// </summary>
    public void ClickPlaySelected()
    {
        PlaySelectedButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Checks if Play Selected button is enabled.
    /// </summary>
    public bool IsPlaySelectedEnabled =>
        PlaySelectedButton?.IsEnabled ?? false;

    #endregion

    #region Action Methods

    /// <summary>
    /// Clicks the Import Media button.
    /// </summary>
    public void ClickImportMedia()
    {
        ImportMediaButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Import Folder button.
    /// </summary>
    public void ClickImportFolder()
    {
        ImportFolderButton?.Click();
        Thread.Sleep(500);
    }

    /// <summary>
    /// Clicks the Create Bin button.
    /// </summary>
    public void ClickCreateBin()
    {
        CreateBinButton?.Click();
        Thread.Sleep(500);
    }

    #endregion

    #region Verification Helpers

    /// <summary>
    /// Checks if the Media page is visible.
    /// </summary>
    public bool IsMediaPageVisible =>
        ImportMediaButton != null && MediaItemsList != null;

    /// <summary>
    /// Checks if any media items are loaded.
    /// </summary>
    public bool HasMedia => VisibleMediaCount > 0;

    /// <summary>
    /// Waits for media to load.
    /// </summary>
    public bool WaitForMediaToLoad(TimeSpan? timeout = null)
    {
        return WaitHelpers.WaitUntil(() => VisibleMediaCount > 0, timeout);
    }

    /// <summary>
    /// Checks if the bins list has default bins.
    /// </summary>
    public bool HasDefaultBins => BinsCount >= 3; // All Media, Videos, Audio, Images

    #endregion
}
