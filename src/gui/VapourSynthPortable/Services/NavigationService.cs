using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Implementation of navigation service with history support
/// </summary>
public class NavigationService : INavigationService
{
    private static readonly ILogger<NavigationService> _logger = LoggingService.GetLogger<NavigationService>();
    private readonly Stack<PageType> _backStack = new();
    private readonly Stack<PageType> _forwardStack = new();
    private PageType _currentPage = PageType.Restore;

    /// <inheritdoc />
    public PageType CurrentPage => _currentPage;

    /// <inheritdoc />
    public event EventHandler<PageChangedEventArgs>? PageChanged;

    /// <inheritdoc />
    public bool CanGoBack => _backStack.Count > 0;

    /// <inheritdoc />
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc />
    public void NavigateTo(PageType page)
    {
        if (page == _currentPage)
            return;

        var previousPage = _currentPage;

        // Add current page to back stack
        _backStack.Push(_currentPage);

        // Clear forward stack when navigating to new page
        _forwardStack.Clear();

        // Update current page
        _currentPage = page;

        _logger.LogDebug("Navigated from {PreviousPage} to {NewPage}", previousPage, page);

        // Raise event
        PageChanged?.Invoke(this, new PageChangedEventArgs
        {
            PreviousPage = previousPage,
            NewPage = page
        });
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        var previousPage = _currentPage;

        // Push current to forward stack
        _forwardStack.Push(_currentPage);

        // Pop from back stack
        _currentPage = _backStack.Pop();

        _logger.LogDebug("Navigated back from {PreviousPage} to {NewPage}", previousPage, _currentPage);

        // Raise event
        PageChanged?.Invoke(this, new PageChangedEventArgs
        {
            PreviousPage = previousPage,
            NewPage = _currentPage
        });

        return true;
    }

    /// <inheritdoc />
    public bool GoForward()
    {
        if (!CanGoForward)
            return false;

        var previousPage = _currentPage;

        // Push current to back stack
        _backStack.Push(_currentPage);

        // Pop from forward stack
        _currentPage = _forwardStack.Pop();

        _logger.LogDebug("Navigated forward from {PreviousPage} to {NewPage}", previousPage, _currentPage);

        // Raise event
        PageChanged?.Invoke(this, new PageChangedEventArgs
        {
            PreviousPage = previousPage,
            NewPage = _currentPage
        });

        return true;
    }
}
