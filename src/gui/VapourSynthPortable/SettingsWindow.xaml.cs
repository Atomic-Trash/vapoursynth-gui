using System.Windows;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(() => Close());
    }
}
