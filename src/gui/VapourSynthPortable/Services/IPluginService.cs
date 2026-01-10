using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for plugin management
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Load the set of enabled plugin names
    /// </summary>
    HashSet<string> LoadEnabledPlugins();

    /// <summary>
    /// Save the set of enabled plugin names
    /// </summary>
    void SaveEnabledPlugins(IEnumerable<string> enabledPlugins);

    /// <summary>
    /// Check if a plugin is installed
    /// </summary>
    bool IsPluginInstalled(Plugin plugin);

    /// <summary>
    /// Get the display status of a plugin
    /// </summary>
    string GetPluginStatus(Plugin plugin, bool hasUpdate);

    /// <summary>
    /// Load all plugins from configuration
    /// </summary>
    List<Plugin> LoadPlugins();

    /// <summary>
    /// Load all Python packages from configuration
    /// </summary>
    List<PythonPackage> LoadPythonPackages();

    /// <summary>
    /// Save plugins to configuration
    /// </summary>
    void SavePlugins(List<Plugin> plugins);

    /// <summary>
    /// Get the installed version of a plugin (from DLL file info)
    /// </summary>
    string? GetInstalledVersion(Plugin plugin);

    /// <summary>
    /// Check if a plugin has an update available
    /// </summary>
    bool HasUpdate(Plugin plugin);

    /// <summary>
    /// Install a plugin from its configured URL
    /// </summary>
    /// <param name="plugin">The plugin to install</param>
    /// <param name="progress">Progress reporter (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result<bool>> InstallPluginAsync(Plugin plugin, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall a plugin by removing its files
    /// </summary>
    /// <param name="plugin">The plugin to uninstall</param>
    Result<bool> UninstallPlugin(Plugin plugin);

    /// <summary>
    /// Get list of installed Python packages with versions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<InstalledPythonPackage>> GetInstalledPythonPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the plugin cache
    /// </summary>
    void InvalidateCache();
}

/// <summary>
/// Represents an installed Python package with version info
/// </summary>
public class InstalledPythonPackage
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Location { get; init; } = "";
}
