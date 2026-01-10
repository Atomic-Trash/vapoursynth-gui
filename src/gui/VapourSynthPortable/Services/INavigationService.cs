using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for navigating between pages in the application
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// The currently displayed page
    /// </summary>
    PageType CurrentPage { get; }

    /// <summary>
    /// Event raised when the current page changes
    /// </summary>
    event EventHandler<PageChangedEventArgs>? PageChanged;

    /// <summary>
    /// Navigate to the specified page
    /// </summary>
    /// <param name="page">The page to navigate to</param>
    void NavigateTo(PageType page);

    /// <summary>
    /// Navigate to the previous page in history
    /// </summary>
    /// <returns>True if navigation occurred, false if no history</returns>
    bool GoBack();

    /// <summary>
    /// Check if back navigation is available
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigate to the next page in history (after going back)
    /// </summary>
    /// <returns>True if navigation occurred, false if no forward history</returns>
    bool GoForward();

    /// <summary>
    /// Check if forward navigation is available
    /// </summary>
    bool CanGoForward { get; }
}

/// <summary>
/// Event arguments for page change events
/// </summary>
public class PageChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous page (null if first navigation)
    /// </summary>
    public PageType? PreviousPage { get; init; }

    /// <summary>
    /// The new current page
    /// </summary>
    public PageType NewPage { get; init; }
}
