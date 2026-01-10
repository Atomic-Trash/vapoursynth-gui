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
}
