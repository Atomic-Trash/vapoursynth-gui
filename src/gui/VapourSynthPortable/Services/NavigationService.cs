using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Implementation of navigation service with history support
/// </summary>
public class NavigationService : INavigationService
{
    private static readonly ILogger<NavigationService> _logger = LoggingService.GetLogger<NavigationService>();
    private const int MaxHistorySize = 50;

    private readonly Stack<PageType> _backStack = new();
    private readonly Stack<PageType> _forwardStack = new();
    private PageType _currentPage = PageType.Media;

    /// <inheritdoc />
    public PageType CurrentPage => _currentPage;

    /// <inheritdoc />
    public event EventHandler<PageChangedEventArgs>? PageChanged;

    /// <inheritdoc />
    public event EventHandler? HistoryChanged;

    /// <inheritdoc />
    public bool CanGoBack => _backStack.Count > 0;

    /// <inheritdoc />
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc />
    public IReadOnlyList<PageType> BackHistory => _backStack.Reverse().ToList();

    /// <inheritdoc />
    public IReadOnlyList<PageType> ForwardHistory => _forwardStack.Reverse().ToList();

    /// <inheritdoc />
    public void NavigateTo(PageType page)
    {
        if (page == _currentPage)
            return;

        var previousPage = _currentPage;

        // Add current page to back stack
        _backStack.Push(_currentPage);

        // Trim history if it exceeds max size
        TrimHistory(_backStack);

        // Clear forward stack when navigating to new page
        _forwardStack.Clear();

        // Update current page
        _currentPage = page;

        _logger.LogDebug("Navigated from {PreviousPage} to {NewPage}", previousPage, page);

        // Raise events
        RaiseNavigationEvents(previousPage, page);
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        return GoBack(1);
    }

    /// <inheritdoc />
    public bool GoBack(int steps)
    {
        if (steps <= 0 || steps > _backStack.Count)
            return false;

        var previousPage = _currentPage;

        // Push current and intermediate pages to forward stack
        _forwardStack.Push(_currentPage);

        for (int i = 1; i < steps; i++)
        {
            _forwardStack.Push(_backStack.Pop());
        }

        // Pop final destination from back stack
        _currentPage = _backStack.Pop();

        _logger.LogDebug("Navigated back {Steps} steps from {PreviousPage} to {NewPage}",
            steps, previousPage, _currentPage);

        // Raise events
        RaiseNavigationEvents(previousPage, _currentPage);

        return true;
    }

    /// <inheritdoc />
    public bool GoForward()
    {
        return GoForward(1);
    }

    /// <inheritdoc />
    public bool GoForward(int steps)
    {
        if (steps <= 0 || steps > _forwardStack.Count)
            return false;

        var previousPage = _currentPage;

        // Push current and intermediate pages to back stack
        _backStack.Push(_currentPage);

        for (int i = 1; i < steps; i++)
        {
            _backStack.Push(_forwardStack.Pop());
        }

        // Pop final destination from forward stack
        _currentPage = _forwardStack.Pop();

        _logger.LogDebug("Navigated forward {Steps} steps from {PreviousPage} to {NewPage}",
            steps, previousPage, _currentPage);

        // Raise events
        RaiseNavigationEvents(previousPage, _currentPage);

        return true;
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();

        _logger.LogDebug("Navigation history cleared");

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseNavigationEvents(PageType previousPage, PageType newPage)
    {
        PageChanged?.Invoke(this, new PageChangedEventArgs
        {
            PreviousPage = previousPage,
            NewPage = newPage
        });

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void TrimHistory(Stack<PageType> stack)
    {
        if (stack.Count <= MaxHistorySize)
            return;

        // Convert to list, trim, and rebuild stack
        var items = stack.ToList();
        stack.Clear();

        // Keep only the most recent items (they're in reverse order in the list)
        for (int i = MaxHistorySize - 1; i >= 0; i--)
        {
            stack.Push(items[i]);
        }
    }
}
