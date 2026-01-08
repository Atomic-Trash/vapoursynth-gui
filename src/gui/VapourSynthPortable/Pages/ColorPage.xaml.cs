using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class ColorPage : UserControl
{
    private string? _currentSource;

    public ColorPage()
    {
        InitializeComponent();
        Loaded += ColorPage_Loaded;
        Unloaded += ColorPage_Unloaded;
    }

    private void ColorPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ColorViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Load source if already set
            if (viewModel.HasSource && !string.IsNullOrEmpty(viewModel.SourcePath))
            {
                LoadVideo(viewModel.SourcePath);
            }
        }
    }

    private void ColorPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ColorViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ColorViewModel.SourcePath) || e.PropertyName == nameof(ColorViewModel.HasSource))
        {
            if (DataContext is ColorViewModel viewModel && viewModel.HasSource)
            {
                LoadVideo(viewModel.SourcePath);
            }
        }
    }

    private void LoadVideo(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == _currentSource) return;

        _currentSource = path;
        PreviewPlayer.LoadFile(path);
    }
}
