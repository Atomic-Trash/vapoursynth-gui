using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly Action _closeAction;

    public SettingsViewModel(Action closeAction)
    {
        _settingsService = new SettingsService();
        _closeAction = closeAction;

        PluginSets = new ObservableCollection<string> { "minimal", "standard", "full" };

        LoadSettings();
        UpdateCacheInfo();
    }

    [ObservableProperty]
    private string _outputDirectory = "dist";

    [ObservableProperty]
    private string _cacheDirectory = "build";

    [ObservableProperty]
    private string _defaultPluginSet = "standard";

    [ObservableProperty]
    private string _pythonVersion = "3.12.4";

    [ObservableProperty]
    private string _vapourSynthVersion = "R68";

    [ObservableProperty]
    private string _cacheSize = "0 MB";

    [ObservableProperty]
    private string _projectRoot = "";

    public ObservableCollection<string> PluginSets { get; }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        OutputDirectory = settings.OutputDirectory;
        CacheDirectory = settings.CacheDirectory;
        DefaultPluginSet = settings.DefaultPluginSet;
        PythonVersion = settings.PythonVersion;
        VapourSynthVersion = settings.VapourSynthVersion;
        ProjectRoot = _settingsService.ProjectRoot;
    }

    private void UpdateCacheInfo()
    {
        var bytes = _settingsService.GetCacheSize();
        if (bytes < 1024)
            CacheSize = $"{bytes} B";
        else if (bytes < 1024 * 1024)
            CacheSize = $"{bytes / 1024.0:F1} KB";
        else if (bytes < 1024 * 1024 * 1024)
            CacheSize = $"{bytes / (1024.0 * 1024.0):F1} MB";
        else
            CacheSize = $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    [RelayCommand]
    private void ClearCache()
    {
        _settingsService.ClearCache();
        UpdateCacheInfo();
    }

    [RelayCommand]
    private void Save()
    {
        var settings = new AppSettings
        {
            OutputDirectory = OutputDirectory,
            CacheDirectory = CacheDirectory,
            DefaultPluginSet = DefaultPluginSet,
            PythonVersion = PythonVersion,
            VapourSynthVersion = VapourSynthVersion
        };
        _settingsService.Save(settings);
        _closeAction();
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction();
    }
}
