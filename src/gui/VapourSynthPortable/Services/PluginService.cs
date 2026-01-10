using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class PluginService : IPluginService
{
    private readonly string _configPath;
    private readonly string _enabledPluginsPath;
    private readonly string _pluginsDir;
    private readonly ILogger<PluginService> _logger;

    // Lazy loading cache
    private List<Plugin>? _cachedPlugins;
    private List<PythonPackage>? _cachedPythonPackages;
    private DateTime _cacheTimestamp;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);
    private readonly object _cacheLock = new();

    public PluginService()
    {
        _logger = LoggingService.GetLogger<PluginService>();
        // Look for plugins.json in parent directories
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = FindConfig(baseDir, "plugins.json") ?? "";

        // Set paths relative to project root
        var projectRoot = !string.IsNullOrEmpty(_configPath)
            ? Path.GetDirectoryName(_configPath) ?? baseDir
            : baseDir;

        _enabledPluginsPath = Path.Combine(projectRoot, "enabled-plugins.json");
        _pluginsDir = Path.Combine(projectRoot, "dist", "plugins");
    }

    private string? FindConfig(string startDir, string fileName)
    {
        // Search up to 10 parent directories to find the config file
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var filePath = Path.Combine(dir.FullName, fileName);
            if (File.Exists(filePath))
                return filePath;
            dir = dir.Parent;
        }
        return null;
    }

    public HashSet<string> LoadEnabledPlugins()
    {
        try
        {
            if (File.Exists(_enabledPluginsPath))
            {
                var json = File.ReadAllText(_enabledPluginsPath);
                var list = JsonConvert.DeserializeObject<List<string>>(json);
                return list != null ? new HashSet<string>(list) : new HashSet<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load enabled plugins from {EnabledPluginsPath}", _enabledPluginsPath);
        }

        // Default: all plugins enabled
        return LoadPlugins().Select(p => p.Name).ToHashSet();
    }

    public void SaveEnabledPlugins(IEnumerable<string> enabledPlugins)
    {
        var json = JsonConvert.SerializeObject(enabledPlugins.ToList(), Formatting.Indented);
        File.WriteAllText(_enabledPluginsPath, json);
    }

    public bool IsPluginInstalled(Plugin plugin)
    {
        if (!Directory.Exists(_pluginsDir))
            return false;

        // Check if any of the plugin's DLL files exist
        foreach (var file in plugin.Files)
        {
            var filePath = Path.Combine(_pluginsDir, file);
            if (File.Exists(filePath))
                return true;
        }
        return false;
    }

    public string GetPluginStatus(Plugin plugin, bool hasUpdate)
    {
        if (hasUpdate)
            return "Update Available";
        if (IsPluginInstalled(plugin))
            return "Installed";
        return "Not Installed";
    }

    public List<Plugin> LoadPlugins()
    {
        lock (_cacheLock)
        {
            // Return cached data if still valid
            if (_cachedPlugins != null && DateTime.UtcNow - _cacheTimestamp < CacheExpiry)
            {
                _logger.LogDebug("Returning cached plugins ({Count} plugins)", _cachedPlugins.Count);
                return _cachedPlugins;
            }

            if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
            {
                return new List<Plugin>();
            }

            try
            {
                _logger.LogDebug("Loading plugins from {ConfigPath}", _configPath);
                var json = File.ReadAllText(_configPath);
                var config = JsonConvert.DeserializeObject<PluginConfig>(json);
                _cachedPlugins = config?.Plugins ?? new List<Plugin>();
                _cachedPythonPackages = config?.PythonPackages ?? new List<PythonPackage>();
                _cacheTimestamp = DateTime.UtcNow;
                _logger.LogDebug("Loaded {Count} plugins", _cachedPlugins.Count);
                return _cachedPlugins;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugins from {ConfigPath}", _configPath);
                return new List<Plugin>();
            }
        }
    }

    public List<PythonPackage> LoadPythonPackages()
    {
        lock (_cacheLock)
        {
            // Return cached data if still valid
            if (_cachedPythonPackages != null && DateTime.UtcNow - _cacheTimestamp < CacheExpiry)
            {
                return _cachedPythonPackages;
            }

            // LoadPlugins will also populate _cachedPythonPackages
            LoadPlugins();
            return _cachedPythonPackages ?? new List<PythonPackage>();
        }
    }

    /// <summary>
    /// Invalidates the plugin cache, forcing a reload on next access
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedPlugins = null;
            _cachedPythonPackages = null;
            _logger.LogDebug("Plugin cache invalidated");
        }
    }

    public void SavePlugins(List<Plugin> plugins)
    {
        if (string.IsNullOrEmpty(_configPath))
            throw new InvalidOperationException("Config path not found");

        var pythonPackages = LoadPythonPackages();

        var config = new PluginConfig
        {
            Version = "1.0.0",
            Description = "VapourSynth plugins configuration for portable distribution",
            Plugins = plugins,
            PythonPackages = pythonPackages
        };

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_configPath, json);

        // Invalidate cache after saving
        InvalidateCache();
        _logger.LogInformation("Saved {Count} plugins to {ConfigPath}", plugins.Count, _configPath);
    }
}
