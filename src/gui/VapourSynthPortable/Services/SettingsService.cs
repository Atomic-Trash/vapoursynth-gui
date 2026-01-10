using System.IO;
using Newtonsoft.Json;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly string _projectRoot;

    public SettingsService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory;
        _settingsPath = Path.Combine(_projectRoot, "settings.json");
    }

    public string ProjectRoot => _projectRoot;

    private string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            // Look for plugins.json as marker for project root
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings from {_settingsPath}: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }

    public string GetOutputPath()
    {
        var settings = Load();
        return Path.Combine(_projectRoot, settings.OutputDirectory);
    }

    public string GetCachePath()
    {
        var settings = Load();
        return Path.Combine(_projectRoot, settings.CacheDirectory);
    }

    public long GetCacheSize()
    {
        var cachePath = GetCachePath();
        if (!Directory.Exists(cachePath))
            return 0;

        return Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    public void ClearCache()
    {
        var cachePath = GetCachePath();
        if (Directory.Exists(cachePath))
        {
            foreach (var file in Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete cache file {file}: {ex.Message}");
                }
            }
        }
    }
}
