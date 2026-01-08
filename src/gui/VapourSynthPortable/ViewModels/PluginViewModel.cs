using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.ViewModels;

public partial class PluginViewModel : ObservableObject
{
    private readonly Plugin _plugin;

    public PluginViewModel(Plugin plugin)
    {
        _plugin = plugin;
        IsEnabled = true;
        Status = "Not Installed";
    }

    public Plugin Plugin => _plugin;
    public string Name => _plugin.Name;
    public string Description => _plugin.Description;
    public string Version => _plugin.Version;
    public string Set => _plugin.Set;
    public string Url => _plugin.Url;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _hasUpdate;

    [ObservableProperty]
    private string? _latestVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private string _status = "Not Installed";

    [ObservableProperty]
    private bool _isInstalled;

    public string StatusColor
    {
        get
        {
            if (HasUpdate) return "#F59E0B";  // Orange
            if (IsInstalled) return "#10B981"; // Green
            return "#6B7280";  // Gray
        }
    }
}
