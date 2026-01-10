using System.Windows;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        // Get services from DI and create ViewModel with close action
        var settingsService = App.Services?.GetService(typeof(ISettingsService)) as ISettingsService
            ?? new SettingsService();
        var vsService = App.Services?.GetService(typeof(IVapourSynthService)) as IVapourSynthService
            ?? new VapourSynthService();

        DataContext = new SettingsViewModel(settingsService, vsService, () => Close());
    }
}
