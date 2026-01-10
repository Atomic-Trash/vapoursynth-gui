namespace VapourSynthPortable.Models;

/// <summary>
/// Enumeration of navigable pages in the application
/// </summary>
public enum PageType
{
    /// <summary>
    /// Media library and import page
    /// </summary>
    Media,

    /// <summary>
    /// Video restoration presets page
    /// </summary>
    Restore,

    /// <summary>
    /// Timeline editing page
    /// </summary>
    Edit,

    /// <summary>
    /// Color grading page
    /// </summary>
    Color,

    /// <summary>
    /// Export and encoding page
    /// </summary>
    Export,

    /// <summary>
    /// Application settings page
    /// </summary>
    Settings
}

/// <summary>
/// Extension methods for PageType
/// </summary>
public static class PageTypeExtensions
{
    /// <summary>
    /// Gets the display name for a page type
    /// </summary>
    public static string ToDisplayName(this PageType pageType) => pageType switch
    {
        PageType.Media => "Media",
        PageType.Restore => "Restore",
        PageType.Edit => "Edit",
        PageType.Color => "Color",
        PageType.Export => "Export",
        PageType.Settings => "Settings",
        _ => pageType.ToString()
    };

    /// <summary>
    /// Parses a string to PageType (case-insensitive)
    /// </summary>
    public static PageType? ParsePageType(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return Enum.TryParse<PageType>(name, ignoreCase: true, out var result)
            ? result
            : null;
    }
}
