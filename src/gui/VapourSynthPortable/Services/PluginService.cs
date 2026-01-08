using System.IO;
using Newtonsoft.Json;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class PluginService
{
    private readonly string _configPath;
    private readonly string _enabledPluginsPath;
    private readonly string _pluginsDir;

    public PluginService()
    {
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
        catch { }

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
        if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
        {
            return new List<Plugin>();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonConvert.DeserializeObject<PluginConfig>(json);
            return config?.Plugins ?? new List<Plugin>();
        }
        catch (Exception)
        {
            return new List<Plugin>();
        }
    }

    public List<PythonPackage> LoadPythonPackages()
    {
        if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
        {
            return new List<PythonPackage>();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonConvert.DeserializeObject<PluginConfig>(json);
            return config?.PythonPackages ?? new List<PythonPackage>();
        }
        catch (Exception)
        {
            return new List<PythonPackage>();
        }
    }

    public void SavePlugins(List<Plugin> plugins)
    {
        if (string.IsNullOrEmpty(_configPath))
            throw new InvalidOperationException("Config path not found");

        var config = new PluginConfig
        {
            Version = "1.0.0",
            Description = "VapourSynth plugins configuration for portable distribution",
            Plugins = plugins,
            PythonPackages = LoadPythonPackages()
        };

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_configPath, json);
    }
}
