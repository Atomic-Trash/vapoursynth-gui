namespace VapourSynthPortable.Models;

public class AppSettings
{
    // Build settings
    public string OutputDirectory { get; set; } = "dist";
    public string CacheDirectory { get; set; } = "build";
    public string DefaultPluginSet { get; set; } = "standard";
    public string PythonVersion { get; set; } = "3.12.4";
    public string VapourSynthVersion { get; set; } = "R68";

    // Custom media bins
    public List<CustomBinSettings> CustomBins { get; set; } = [];

    // Export preferences
    public string DefaultExportFormat { get; set; } = "mp4";
    public string DefaultVideoCodec { get; set; } = "libx264";
    public string DefaultAudioCodec { get; set; } = "aac";
    public int DefaultVideoQuality { get; set; } = 22;
    public int DefaultAudioBitrate { get; set; } = 192;
    public string DefaultPresetSpeed { get; set; } = "medium";
    public string DefaultExportMode { get; set; } = "DirectEncode";
    public bool DefaultVideoEnabled { get; set; } = true;
    public bool DefaultAudioEnabled { get; set; } = true;
    public string DefaultResolution { get; set; } = "Source";
    public string DefaultFrameRate { get; set; } = "Source";
    public string DefaultNvencPreset { get; set; } = "p4 (balanced)";
    public string DefaultProresProfile { get; set; } = "2 - Standard";

    // Hardware acceleration
    public GpuPreference GpuPreference { get; set; } = GpuPreference.Auto;

    // Cache settings
    public int MaxCacheSizeMB { get; set; } = 1024;
    public bool AutoClearCache { get; set; } = false;

    // Project settings
    public int RecentProjectsLimit { get; set; } = 10;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool AutoSaveEnabled { get; set; } = true;

    // UI preferences
    public bool ShowLogPanel { get; set; } = false;
    public double TimelineZoom { get; set; } = 1.0;
    public bool ConfirmOnDelete { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    // Favorite presets (stored by name)
    public List<string> FavoritePresets { get; set; } = [];

    // Window state
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;
    public bool WindowMaximized { get; set; } = false;
    public string LastActivePage { get; set; } = "Media";
}

public enum GpuPreference
{
    Auto,
    NVIDIA,
    AMD,
    Intel,
    CPU
}

public class CustomBinSettings
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> ItemPaths { get; set; } = [];
}

public enum AppTheme
{
    Dark,
    Light
}
