using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace VapourSynthPortable.Tests.UITests.Helpers;

/// <summary>
/// Utility methods for finding UI elements.
/// </summary>
public static class ElementFinder
{
    /// <summary>
    /// Default timeout for finding elements (in milliseconds).
    /// </summary>
    private const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Finds an element by its AutomationId with retry mechanism.
    /// </summary>
    public static AutomationElement? ById(AutomationElement parent, string automationId, int timeoutMs = DefaultTimeoutMs)
    {
        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        while (DateTime.Now < endTime)
        {
            try
            {
                var element = parent.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (element != null)
                    return element;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Retry on COM exceptions (common in UI automation)
            }

            Thread.Sleep(100);
        }

        return null;
    }

    /// <summary>
    /// Finds an element by its Name property.
    /// </summary>
    public static AutomationElement? ByName(AutomationElement parent, string name)
    {
        return parent.FindFirstDescendant(cf => cf.ByName(name));
    }

    /// <summary>
    /// Finds an element by its control type.
    /// </summary>
    public static AutomationElement? ByType(AutomationElement parent, ControlType type)
    {
        return parent.FindFirstDescendant(cf => cf.ByControlType(type));
    }

    /// <summary>
    /// Finds all elements of a specific control type.
    /// </summary>
    public static AutomationElement[] AllByType(AutomationElement parent, ControlType type)
    {
        return parent.FindAllDescendants(cf => cf.ByControlType(type));
    }

    /// <summary>
    /// Finds an element by AutomationId and control type.
    /// </summary>
    public static AutomationElement? ByIdAndType(
        AutomationElement parent,
        string automationId,
        ControlType type)
    {
        return parent.FindFirstDescendant(cf =>
            cf.ByAutomationId(automationId).And(cf.ByControlType(type)));
    }

    /// <summary>
    /// Finds a button by AutomationId.
    /// </summary>
    public static Button? Button(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsButton();
    }

    /// <summary>
    /// Finds a button by its content/text.
    /// </summary>
    public static Button? ButtonByText(AutomationElement parent, string text)
    {
        return ByName(parent, text)?.AsButton();
    }

    /// <summary>
    /// Finds a TextBox by AutomationId.
    /// </summary>
    public static TextBox? TextBox(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsTextBox();
    }

    /// <summary>
    /// Finds a ListBox by AutomationId.
    /// </summary>
    public static ListBox? ListBox(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsListBox();
    }

    /// <summary>
    /// Finds a ComboBox by AutomationId.
    /// </summary>
    public static ComboBox? ComboBox(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsComboBox();
    }

    /// <summary>
    /// Finds a CheckBox by AutomationId.
    /// </summary>
    public static CheckBox? CheckBox(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsCheckBox();
    }

    /// <summary>
    /// Finds a Slider by AutomationId.
    /// </summary>
    public static Slider? Slider(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsSlider();
    }

    /// <summary>
    /// Finds a Tab control by AutomationId.
    /// </summary>
    public static Tab? Tab(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId)?.AsTab();
    }

    /// <summary>
    /// Finds all buttons in a container.
    /// </summary>
    public static Button[] AllButtons(AutomationElement parent)
    {
        return AllByType(parent, ControlType.Button)
            .Select(e => e.AsButton())
            .ToArray();
    }

    /// <summary>
    /// Checks if an element exists.
    /// </summary>
    public static bool Exists(AutomationElement parent, string automationId)
    {
        return ById(parent, automationId) != null;
    }

    /// <summary>
    /// Gets the text content of an element by AutomationId.
    /// </summary>
    public static string? GetText(AutomationElement parent, string automationId)
    {
        var element = ById(parent, automationId);
        if (element == null) return null;

        // Try different patterns to get text
        if (element.Patterns.Value.IsSupported)
        {
            return element.Patterns.Value.Pattern.Value.Value;
        }

        return element.Name;
    }
}
