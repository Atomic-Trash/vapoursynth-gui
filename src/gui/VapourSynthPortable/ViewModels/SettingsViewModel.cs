using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly Action? _closeAction;

    public SettingsViewModel() : this(null)
    {
    }

    public SettingsViewModel(Action? closeAction)
    {
        _settingsService = new SettingsService();
        _closeAction = closeAction;

        PluginSets = new ObservableCollection<string> { "minimal", "standard", "full" };
        ExportFormats = new ObservableCollection<string> { "mp4", "mkv", "mov", "avi", "webm" };
        VideoCodecs = new ObservableCollection<string> { "libx264", "libx265", "h264_nvenc", "hevc_nvenc", "prores_ks", "ffv1" };
        AudioCodecs = new ObservableCollection<string> { "aac", "libmp3lame", "pcm_s16le", "flac" };
        GpuPreferences = new ObservableCollection<GpuPreference>(Enum.GetValues<GpuPreference>());

        LoadSettings();
        UpdateCacheInfo();
    }

    #region Build Settings
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
    private string _projectRoot = "";
    #endregion

    #region Export Settings
    [ObservableProperty]
    private string _defaultExportFormat = "mp4";

    [ObservableProperty]
    private string _defaultVideoCodec = "libx264";

    [ObservableProperty]
    private string _defaultAudioCodec = "aac";

    [ObservableProperty]
    private int _defaultVideoQuality = 22;

    [ObservableProperty]
    private int _defaultAudioBitrate = 192;

    [ObservableProperty]
    private GpuPreference _gpuPreference = GpuPreference.Auto;
    #endregion

    #region Cache Settings
    [ObservableProperty]
    private string _cacheSize = "0 MB";

    [ObservableProperty]
    private int _maxCacheSizeMB = 1024;

    [ObservableProperty]
    private bool _autoClearCache;
    #endregion

    #region Project Settings
    [ObservableProperty]
    private int _recentProjectsLimit = 10;

    [ObservableProperty]
    private int _autoSaveIntervalMinutes = 5;

    [ObservableProperty]
    private bool _autoSaveEnabled = true;
    #endregion

    #region UI Settings
    [ObservableProperty]
    private bool _showLogPanel;

    [ObservableProperty]
    private double _timelineZoom = 1.0;

    [ObservableProperty]
    private bool _confirmOnDelete = true;
    #endregion

    #region Collections
    public ObservableCollection<string> PluginSets { get; }
    public ObservableCollection<string> ExportFormats { get; }
    public ObservableCollection<string> VideoCodecs { get; }
    public ObservableCollection<string> AudioCodecs { get; }
    public ObservableCollection<GpuPreference> GpuPreferences { get; }
    #endregion

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        // Build settings
        OutputDirectory = settings.OutputDirectory;
        CacheDirectory = settings.CacheDirectory;
        DefaultPluginSet = settings.DefaultPluginSet;
        PythonVersion = settings.PythonVersion;
        VapourSynthVersion = settings.VapourSynthVersion;
        ProjectRoot = _settingsService.ProjectRoot;

        // Export settings
        DefaultExportFormat = settings.DefaultExportFormat;
        DefaultVideoCodec = settings.DefaultVideoCodec;
        DefaultAudioCodec = settings.DefaultAudioCodec;
        DefaultVideoQuality = settings.DefaultVideoQuality;
        DefaultAudioBitrate = settings.DefaultAudioBitrate;
        GpuPreference = settings.GpuPreference;

        // Cache settings
        MaxCacheSizeMB = settings.MaxCacheSizeMB;
        AutoClearCache = settings.AutoClearCache;

        // Project settings
        RecentProjectsLimit = settings.RecentProjectsLimit;
        AutoSaveIntervalMinutes = settings.AutoSaveIntervalMinutes;
        AutoSaveEnabled = settings.AutoSaveEnabled;

        // UI settings
        ShowLogPanel = settings.ShowLogPanel;
        TimelineZoom = settings.TimelineZoom;
        ConfirmOnDelete = settings.ConfirmOnDelete;
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
        ToastService.Instance.ShowSuccess("Cache cleared");
    }

    [RelayCommand]
    private void Save()
    {
        var settings = new AppSettings
        {
            // Build settings
            OutputDirectory = OutputDirectory,
            CacheDirectory = CacheDirectory,
            DefaultPluginSet = DefaultPluginSet,
            PythonVersion = PythonVersion,
            VapourSynthVersion = VapourSynthVersion,

            // Export settings
            DefaultExportFormat = DefaultExportFormat,
            DefaultVideoCodec = DefaultVideoCodec,
            DefaultAudioCodec = DefaultAudioCodec,
            DefaultVideoQuality = DefaultVideoQuality,
            DefaultAudioBitrate = DefaultAudioBitrate,
            GpuPreference = GpuPreference,

            // Cache settings
            MaxCacheSizeMB = MaxCacheSizeMB,
            AutoClearCache = AutoClearCache,

            // Project settings
            RecentProjectsLimit = RecentProjectsLimit,
            AutoSaveIntervalMinutes = AutoSaveIntervalMinutes,
            AutoSaveEnabled = AutoSaveEnabled,

            // UI settings
            ShowLogPanel = ShowLogPanel,
            TimelineZoom = TimelineZoom,
            ConfirmOnDelete = ConfirmOnDelete
        };
        _settingsService.Save(settings);
        ToastService.Instance.ShowSuccess("Settings saved");
        _closeAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new AppSettings();

        DefaultExportFormat = defaults.DefaultExportFormat;
        DefaultVideoCodec = defaults.DefaultVideoCodec;
        DefaultAudioCodec = defaults.DefaultAudioCodec;
        DefaultVideoQuality = defaults.DefaultVideoQuality;
        DefaultAudioBitrate = defaults.DefaultAudioBitrate;
        GpuPreference = defaults.GpuPreference;
        MaxCacheSizeMB = defaults.MaxCacheSizeMB;
        AutoClearCache = defaults.AutoClearCache;
        RecentProjectsLimit = defaults.RecentProjectsLimit;
        AutoSaveIntervalMinutes = defaults.AutoSaveIntervalMinutes;
        AutoSaveEnabled = defaults.AutoSaveEnabled;
        ShowLogPanel = defaults.ShowLogPanel;
        TimelineZoom = defaults.TimelineZoom;
        ConfirmOnDelete = defaults.ConfirmOnDelete;

        ToastService.Instance.ShowInfo("Settings reset to defaults");
    }
}
