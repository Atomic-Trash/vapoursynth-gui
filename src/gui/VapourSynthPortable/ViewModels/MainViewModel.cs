using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly BuildService _buildService;
    private readonly PluginService _pluginService;
    private CancellationTokenSource? _buildCts;
    private readonly List<(PluginViewModel vm, PropertyChangedEventHandler handler)> _pluginHandlers = [];
    private bool _disposed;

    public MainViewModel()
    {
        _buildService = new BuildService();
        _pluginService = new PluginService();

        PluginSets = new ObservableCollection<string> { "minimal", "standard", "full" };
        Plugins = new ObservableCollection<PluginViewModel>();
        Templates = new ObservableCollection<ScriptTemplate>();

        // Load plugins and templates from config
        LoadPlugins();
        LoadTemplates();

        StatusMessage = "Ready to build";
    }

    // Build Configuration Properties
    [ObservableProperty]
    private string _selectedPluginSet = "standard";

    [ObservableProperty]
    private bool _includePython = true;

    [ObservableProperty]
    private bool _includeVapourSynth = true;

    [ObservableProperty]
    private bool _includePlugins = true;

    [ObservableProperty]
    private bool _includePackages = true;

    [ObservableProperty]
    private bool _cleanBuild;

    [ObservableProperty]
    private bool _createLaunchers = true;

    [ObservableProperty]
    private bool _createVSCode = true;

    // Build State Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartBuild))]
    private bool _isBuilding;

    [ObservableProperty]
    private string _currentOperation = "";

    [ObservableProperty]
    private int _buildProgress;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _buildLog = "";

    [ObservableProperty]
    private string _statusMessage = "";

    // Template Properties
    [ObservableProperty]
    private ScriptTemplate? _selectedTemplate;

    // Plugin Filter Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPlugins))]
    private string _pluginSearchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPlugins))]
    private string _pluginSetFilter = "all";

    // Collections
    public ObservableCollection<string> PluginSets { get; }
    public ObservableCollection<string> PluginSetFilters { get; } = new() { "all", "minimal", "standard", "full" };
    public ObservableCollection<PluginViewModel> Plugins { get; }
    public ObservableCollection<ScriptTemplate> Templates { get; }

    public IEnumerable<PluginViewModel> FilteredPlugins
    {
        get
        {
            var filtered = Plugins.AsEnumerable();

            // Filter by set
            if (PluginSetFilter != "all")
                filtered = filtered.Where(p => p.Set == PluginSetFilter);

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(PluginSearchText))
            {
                var search = PluginSearchText.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Description.ToLower().Contains(search));
            }

            return filtered;
        }
    }

    public int PluginCount => Plugins.Count;
    public int EnabledPluginCount => Plugins.Count(p => p.IsEnabled);

    public bool CanStartBuild => !IsBuilding;

    private void LoadPlugins()
    {
        try
        {
            var plugins = _pluginService.LoadPlugins();
            var enabledPlugins = _pluginService.LoadEnabledPlugins();

            Plugins.Clear();
            foreach (var plugin in plugins)
            {
                var vm = new PluginViewModel(plugin);
                vm.IsEnabled = enabledPlugins.Contains(plugin.Name);
                vm.IsInstalled = _pluginService.IsPluginInstalled(plugin);
                vm.Status = _pluginService.GetPluginStatus(plugin, false);

                // Subscribe to IsEnabled changes to auto-save (store handler for cleanup)
                PropertyChangedEventHandler handler = (s, e) =>
                {
                    if (e.PropertyName == nameof(PluginViewModel.IsEnabled))
                    {
                        SaveEnabledPlugins();
                        OnPropertyChanged(nameof(EnabledPluginCount));
                    }
                };
                vm.PropertyChanged += handler;
                _pluginHandlers.Add((vm, handler));

                Plugins.Add(vm);
            }
            OnPropertyChanged(nameof(PluginCount));
            OnPropertyChanged(nameof(EnabledPluginCount));
            OnPropertyChanged(nameof(FilteredPlugins));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading plugins: {ex.Message}";
        }
    }

    private void SaveEnabledPlugins()
    {
        var enabled = Plugins.Where(p => p.IsEnabled).Select(p => p.Name);
        _pluginService.SaveEnabledPlugins(enabled);
    }

    [RelayCommand]
    private void SelectAllPlugins()
    {
        foreach (var plugin in FilteredPlugins)
            plugin.IsEnabled = true;
    }

    [RelayCommand]
    private void SelectNoPlugins()
    {
        foreach (var plugin in FilteredPlugins)
            plugin.IsEnabled = false;
    }

    [RelayCommand]
    private void RefreshPluginStatus()
    {
        foreach (var plugin in Plugins)
        {
            plugin.IsInstalled = _pluginService.IsPluginInstalled(plugin.Plugin);
            plugin.Status = _pluginService.GetPluginStatus(plugin.Plugin, plugin.HasUpdate);
        }
        StatusMessage = "Plugin status refreshed";
    }

    private void LoadTemplates()
    {
        try
        {
            Templates.Clear();

            // Find templates directory relative to the app
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = FindDirectory(baseDir, "templates");

            if (templatesDir == null || !Directory.Exists(templatesDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(templatesDir, "*.vpy"))
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);
                var name = Path.GetFileNameWithoutExtension(file);

                // Extract description from first docstring if present
                var description = ExtractDescription(content);

                Templates.Add(new ScriptTemplate
                {
                    Name = FormatTemplateName(name),
                    FileName = fileName,
                    FilePath = file,
                    Content = content,
                    Description = description
                });
            }

            // Select first template by default
            if (Templates.Count > 0)
            {
                SelectedTemplate = Templates[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading templates: {ex.Message}";
        }
    }

    private string? FindDirectory(string startDir, string dirName)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var targetDir = Path.Combine(dir.FullName, dirName);
            if (Directory.Exists(targetDir))
                return targetDir;
            dir = dir.Parent;
        }
        return null;
    }

    private string ExtractDescription(string content)
    {
        // Look for docstring at start of file
        if (content.StartsWith("\"\"\""))
        {
            var endIndex = content.IndexOf("\"\"\"", 3);
            if (endIndex > 3)
            {
                var docstring = content.Substring(3, endIndex - 3).Trim();
                // Get first line as description
                var firstLine = docstring.Split('\n')[0].Trim();
                // Remove any trailing = decorations
                if (!firstLine.Contains('='))
                    return firstLine;
                // If first line is title with ===, look for next non-decoration line
                var lines = docstring.Split('\n');
                foreach (var line in lines.Skip(1))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.All(c => c == '='))
                        return trimmed;
                }
            }
        }
        return "VapourSynth script template";
    }

    private string FormatTemplateName(string name)
    {
        // Convert "basic-source" to "Basic Source"
        return string.Join(" ", name.Split('-').Select(w =>
            char.ToUpper(w[0]) + w.Substring(1)));
    }

    [RelayCommand]
    private void CopyTemplateToClipboard()
    {
        if (SelectedTemplate != null)
        {
            Clipboard.SetText(SelectedTemplate.Content);
            StatusMessage = $"Template '{SelectedTemplate.Name}' copied to clipboard";
        }
    }

    [RelayCommand]
    private void OpenTemplateInEditor()
    {
        if (SelectedTemplate != null && File.Exists(SelectedTemplate.FilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedTemplate.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open template: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task StartBuildAsync()
    {
        if (IsBuilding) return;

        IsBuilding = true;
        BuildLog = "";
        BuildProgress = 0;
        _buildCts = new CancellationTokenSource();

        try
        {
            var components = new List<string>();
            if (IncludePython) components.Add("python");
            if (IncludeVapourSynth) components.Add("vapoursynth");
            if (IncludePlugins) components.Add("plugins");
            if (IncludePackages) components.Add("packages");
            if (CreateLaunchers) components.Add("launcher");
            if (CreateVSCode) components.Add("vscode");

            var config = new BuildConfiguration
            {
                PluginSet = SelectedPluginSet,
                Components = components,
                Clean = CleanBuild
            };

            StatusMessage = "Building...";

            await _buildService.RunBuildAsync(
                config,
                output => AppendLog(output),
                progress =>
                {
                    BuildProgress = progress.Percent;
                    CurrentOperation = progress.Operation;
                    ProgressText = $"{progress.Percent}%";
                },
                _buildCts.Token);

            StatusMessage = "Build completed successfully";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Build cancelled";
            AppendLog("\n[Build cancelled by user]");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Build failed: {ex.Message}";
            AppendLog($"\n[ERROR] {ex.Message}");
        }
        finally
        {
            IsBuilding = false;
            OnPropertyChanged(nameof(CanStartBuild));
            _buildCts?.Dispose();
            _buildCts = null;
        }
    }

    [RelayCommand]
    private void CancelBuild()
    {
        _buildCts?.Cancel();
        StatusMessage = "Cancelling...";
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        StatusMessage = "Checking for updates...";
        try
        {
            var updateService = new UpdateService();

            // Get plugin info from viewmodels
            var plugins = Plugins.Select(p => new Plugin
            {
                Name = p.Name,
                Url = p.Url,
                Version = p.Version
            }).ToList();

            var results = await updateService.CheckPluginUpdatesAsync(plugins, (current, total) =>
            {
                StatusMessage = $"Checking updates... ({current}/{total})";
            });

            // Update plugin viewmodels with results
            int updatesAvailable = 0;
            foreach (var result in results)
            {
                var pluginVm = Plugins.FirstOrDefault(p => p.Name == result.PluginName);
                if (pluginVm != null)
                {
                    pluginVm.HasUpdate = result.HasUpdate;
                    pluginVm.LatestVersion = result.LatestVersion;
                    if (result.HasUpdate)
                        updatesAvailable++;
                }
            }

            if (updatesAvailable > 0)
                StatusMessage = $"{updatesAvailable} update(s) available";
            else
                StatusMessage = "All plugins are up to date";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = Application.Current.MainWindow;
        settingsWindow.ShowDialog();

        // Reload settings after dialog closes
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        SelectedPluginSet = settings.DefaultPluginSet;
        StatusMessage = "Settings saved";
    }

    private void AppendLog(string text)
    {
        BuildLog += text;
        OnPropertyChanged(nameof(BuildLog));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe all plugin handlers
        foreach (var (vm, handler) in _pluginHandlers)
        {
            vm.PropertyChanged -= handler;
        }
        _pluginHandlers.Clear();

        _buildCts?.Dispose();
    }
}
