using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VapourSynthPortable.Helpers;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class PluginService : IPluginService
{
    private readonly string _configPath;
    private readonly string _enabledPluginsPath;
    private readonly string _pluginsDir;
    private readonly string _pythonPath;
    private readonly string _projectRoot;
    private readonly ILogger<PluginService> _logger;
    private readonly HttpClient _httpClient;

    // Lazy loading cache
    private List<Plugin>? _cachedPlugins;
    private List<PythonPackage>? _cachedPythonPackages;
    private Dictionary<string, string>? _installedVersionsCache;
    private DateTime _cacheTimestamp;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);
    private readonly object _cacheLock = new();

    public PluginService()
    {
        _logger = LoggingService.GetLogger<PluginService>();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VapourSynthPortable/1.0");

        // Look for plugins.json in parent directories
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = FindConfig(baseDir, "plugins.json") ?? "";

        // Set paths relative to project root
        _projectRoot = !string.IsNullOrEmpty(_configPath)
            ? Path.GetDirectoryName(_configPath) ?? baseDir
            : baseDir;

        _enabledPluginsPath = Path.Combine(_projectRoot, "enabled-plugins.json");
        _pluginsDir = Path.Combine(_projectRoot, "dist", "plugins");
        _pythonPath = Path.Combine(_projectRoot, "dist", "python", "python.exe");
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

    /// <summary>
    /// Gets the installed version of a plugin from DLL file version info
    /// </summary>
    public string? GetInstalledVersion(Plugin plugin)
    {
        if (!IsPluginInstalled(plugin))
            return null;

        try
        {
            // Check cached versions first
            lock (_cacheLock)
            {
                if (_installedVersionsCache?.TryGetValue(plugin.Name, out var cachedVersion) == true)
                    return cachedVersion;
            }

            // Find the first DLL file and get its version
            foreach (var file in plugin.Files.Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                var filePath = Path.Combine(_pluginsDir, file);
                if (File.Exists(filePath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    var version = versionInfo.FileVersion ?? versionInfo.ProductVersion;

                    if (!string.IsNullOrEmpty(version))
                    {
                        // Cache the version
                        lock (_cacheLock)
                        {
                            _installedVersionsCache ??= new Dictionary<string, string>();
                            _installedVersionsCache[plugin.Name] = version;
                        }
                        return version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get version for plugin {PluginName}", plugin.Name);
        }

        return null;
    }

    /// <summary>
    /// Checks if a plugin has an update available by comparing versions
    /// </summary>
    public bool HasUpdate(Plugin plugin)
    {
        var installedVersion = GetInstalledVersion(plugin);
        if (string.IsNullOrEmpty(installedVersion))
            return false;

        var configVersion = plugin.Version;
        if (string.IsNullOrEmpty(configVersion))
            return false;

        // Normalize versions for comparison (remove 'v' prefix, etc.)
        var installed = NormalizeVersion(installedVersion);
        var available = NormalizeVersion(configVersion);

        try
        {
            // Try semantic version comparison
            if (Version.TryParse(installed, out var installedVer) &&
                Version.TryParse(available, out var availableVer))
            {
                return availableVer > installedVer;
            }

            // Fall back to string comparison
            return string.Compare(available, installed, StringComparison.OrdinalIgnoreCase) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeVersion(string version)
    {
        // Remove common prefixes and clean up version string
        var normalized = version
            .TrimStart('v', 'V', 'r', 'R')
            .Split('-')[0]  // Remove pre-release suffixes
            .Split('+')[0]; // Remove build metadata

        // Pad version parts for proper comparison
        var parts = normalized.Split('.');
        if (parts.Length < 4)
        {
            var paddedParts = new string[4];
            for (int i = 0; i < 4; i++)
            {
                paddedParts[i] = i < parts.Length ? parts[i] : "0";
            }
            normalized = string.Join(".", paddedParts);
        }

        return normalized;
    }

    /// <summary>
    /// Installs a plugin from its configured URL
    /// </summary>
    public async Task<Result<bool>> InstallPluginAsync(Plugin plugin, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plugin.Url))
            return Result<bool>.Failure($"No download URL configured for plugin {plugin.Name}");

        try
        {
            _logger.LogInformation("Installing plugin {PluginName} from {Url}", plugin.Name, plugin.Url);
            progress?.Report(0);

            // Ensure plugins directory exists
            Directory.CreateDirectory(_pluginsDir);

            // Download the file
            var tempFile = Path.Combine(Path.GetTempPath(), $"{plugin.Name}_{Guid.NewGuid():N}.zip");

            try
            {
                using var response = await _httpClient.GetAsync(plugin.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0L;

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var percentage = (int)((downloadedBytes * 50) / totalBytes); // 0-50% for download
                        progress?.Report(percentage);
                    }
                }

                progress?.Report(50);

                // Extract or copy the file
                if (plugin.Url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    IsZipFile(tempFile))
                {
                    await ExtractPluginZipAsync(tempFile, plugin, progress, cancellationToken);
                }
                else
                {
                    // Single file - copy directly
                    var targetFile = plugin.Files.FirstOrDefault() ?? Path.GetFileName(plugin.Url);
                    var targetPath = Path.Combine(_pluginsDir, targetFile);
                    File.Copy(tempFile, targetPath, overwrite: true);
                }

                progress?.Report(100);
                InvalidateCache();

                _logger.LogInformation("Successfully installed plugin {PluginName}", plugin.Name);
                return Result<bool>.Success(true);
            }
            finally
            {
                // Cleanup temp file
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Installation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin {PluginName}", plugin.Name);
            return Result<bool>.Failure($"Failed to install plugin: {ex.Message}");
        }
    }

    private static bool IsZipFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) == 4)
            {
                // ZIP file magic number: PK\x03\x04
                return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;
            }
        }
        catch { }
        return false;
    }

    private async Task ExtractPluginZipAsync(string zipPath, Plugin plugin, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.ToList();
        var processedCount = 0;

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip directories
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            // Check if this file is one we want
            var fileName = entry.Name;
            var shouldExtract = plugin.Files.Count == 0 || // Extract all if no files specified
                                plugin.Files.Any(f => f.Equals(fileName, StringComparison.OrdinalIgnoreCase)) ||
                                fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

            if (shouldExtract)
            {
                var targetPath = Path.Combine(_pluginsDir, fileName);
                entry.ExtractToFile(targetPath, overwrite: true);
                _logger.LogDebug("Extracted {FileName} to {TargetPath}", fileName, targetPath);
            }

            processedCount++;
            var percentage = 50 + (int)((processedCount * 50.0) / entries.Count); // 50-100% for extraction
            progress?.Report(percentage);
        }

        await Task.CompletedTask; // Placeholder for async operation
    }

    /// <summary>
    /// Uninstalls a plugin by removing its files
    /// </summary>
    public Result<bool> UninstallPlugin(Plugin plugin)
    {
        if (!IsPluginInstalled(plugin))
            return Result<bool>.Failure($"Plugin {plugin.Name} is not installed");

        try
        {
            _logger.LogInformation("Uninstalling plugin {PluginName}", plugin.Name);
            var removedCount = 0;

            foreach (var file in plugin.Files)
            {
                var filePath = Path.Combine(_pluginsDir, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    removedCount++;
                    _logger.LogDebug("Removed {FilePath}", filePath);
                }
            }

            InvalidateCache();
            _logger.LogInformation("Uninstalled plugin {PluginName}, removed {Count} files", plugin.Name, removedCount);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin {PluginName}", plugin.Name);
            return Result<bool>.Failure($"Failed to uninstall plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets list of installed Python packages with versions using pip
    /// </summary>
    public async Task<List<InstalledPythonPackage>> GetInstalledPythonPackagesAsync(CancellationToken cancellationToken = default)
    {
        var packages = new List<InstalledPythonPackage>();

        if (!ProcessHelper.Paths.IsPythonAvailable)
        {
            _logger.LogWarning("Python not found at {PythonPath}", ProcessHelper.Paths.PythonExePath);
            return packages;
        }

        try
        {
            var startInfo = ProcessHelper.CreatePythonProcess("-m pip list --format=json");
            var (exitCode, output, _) = await ProcessHelper.RunProcessAsync(startInfo, cancellationToken);

            if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse JSON output from pip list
                var pipPackages = JsonConvert.DeserializeObject<List<PipPackage>>(output);
                if (pipPackages != null)
                {
                    packages = pipPackages.Select(p => new InstalledPythonPackage
                    {
                        Name = p.Name,
                        Version = p.Version,
                        Location = ""
                    }).ToList();
                }
            }

            _logger.LogDebug("Found {Count} installed Python packages", packages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get installed Python packages");
        }

        return packages;
    }

    // Helper class for pip list JSON output
    private class PipPackage
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("version")]
        public string Version { get; set; } = "";
    }
}
