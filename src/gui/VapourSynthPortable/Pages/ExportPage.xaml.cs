using System.Windows.Controls;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class ExportPage : UserControl
{
    public ExportPage()
    {
        InitializeComponent();

        // Get ViewModel from DI to ensure shared MediaPoolService singleton
        DataContext = App.Services?.GetService(typeof(ExportViewModel))
            ?? new ExportViewModel();
    }
}
