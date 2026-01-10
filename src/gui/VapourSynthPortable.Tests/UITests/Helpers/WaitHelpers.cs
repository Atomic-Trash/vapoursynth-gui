using FlaUI.Core.AutomationElements;

namespace VapourSynthPortable.Tests.UITests.Helpers;

/// <summary>
/// Utility methods for waiting on UI elements and conditions.
/// </summary>
public static class WaitHelpers
{
    /// <summary>
    /// Default timeout for wait operations.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default polling interval for wait operations.
    /// </summary>
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Waits until a condition becomes true or timeout is reached.
    /// </summary>
    /// <param name="condition">The condition to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="pollingInterval">How often to check the condition</param>
    /// <returns>True if condition was met, false if timeout was reached</returns>
    public static bool WaitUntil(
        Func<bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null)
    {
        var actualTimeout = timeout ?? DefaultTimeout;
        var actualPolling = pollingInterval ?? DefaultPollingInterval;
        var deadline = DateTime.Now + actualTimeout;

        while (DateTime.Now < deadline)
        {
            try
            {
                if (condition()) return true;
            }
            catch
            {
                // Ignore exceptions during polling
            }
            Thread.Sleep(actualPolling);
        }
        return false;
    }

    /// <summary>
    /// Waits for an element to appear in the UI tree.
    /// </summary>
    /// <param name="parent">Parent element to search within</param>
    /// <param name="finder">Function to locate the element</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>The found element, or null if timeout was reached</returns>
    public static AutomationElement? WaitForElement(
        AutomationElement parent,
        Func<AutomationElement, AutomationElement?> finder,
        TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? DefaultTimeout;
        var deadline = DateTime.Now + actualTimeout;

        while (DateTime.Now < deadline)
        {
            try
            {
                var element = finder(parent);
                if (element != null) return element;
            }
            catch
            {
                // Ignore exceptions during search
            }
            Thread.Sleep(DefaultPollingInterval);
        }
        return null;
    }

    /// <summary>
    /// Waits for an element to become enabled.
    /// </summary>
    /// <param name="element">The element to wait on</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>True if element became enabled, false if timeout was reached</returns>
    public static bool WaitForEnabled(AutomationElement element, TimeSpan? timeout = null)
    {
        return WaitUntil(() => element.IsEnabled, timeout);
    }

    /// <summary>
    /// Waits for an element to become visible (not offscreen).
    /// </summary>
    /// <param name="element">The element to wait on</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>True if element became visible, false if timeout was reached</returns>
    public static bool WaitForVisible(AutomationElement element, TimeSpan? timeout = null)
    {
        return WaitUntil(() => !element.IsOffscreen, timeout);
    }

    /// <summary>
    /// Waits for an element to disappear from the UI tree.
    /// </summary>
    /// <param name="parent">Parent element to search within</param>
    /// <param name="finder">Function that returns the element (or null if not found)</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>True if element disappeared, false if timeout was reached</returns>
    public static bool WaitForElementToDisappear(
        AutomationElement parent,
        Func<AutomationElement, AutomationElement?> finder,
        TimeSpan? timeout = null)
    {
        return WaitUntil(() => finder(parent) == null, timeout);
    }

    /// <summary>
    /// Waits for a text element to contain specific text.
    /// </summary>
    /// <param name="element">The text element</param>
    /// <param name="expectedText">Text that should appear in the element</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>True if text was found, false if timeout was reached</returns>
    public static bool WaitForText(
        AutomationElement element,
        string expectedText,
        TimeSpan? timeout = null)
    {
        return WaitUntil(() =>
        {
            var text = element.Name ?? "";
            return text.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
        }, timeout);
    }
}
