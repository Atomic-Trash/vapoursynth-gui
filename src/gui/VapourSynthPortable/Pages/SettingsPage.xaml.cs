using System.Windows.Controls;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}
