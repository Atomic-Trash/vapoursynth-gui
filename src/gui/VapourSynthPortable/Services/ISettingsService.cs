using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for application settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// The project root directory
    /// </summary>
    string ProjectRoot { get; }

    /// <summary>
    /// Load application settings
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Save application settings
    /// </summary>
    void Save(AppSettings settings);

    /// <summary>
    /// Get the configured output path
    /// </summary>
    string GetOutputPath();

    /// <summary>
    /// Get the configured cache path
    /// </summary>
    string GetCachePath();

    /// <summary>
    /// Get the current cache size in bytes
    /// </summary>
    long GetCacheSize();

    /// <summary>
    /// Clear the cache directory
    /// </summary>
    void ClearCache();
}
