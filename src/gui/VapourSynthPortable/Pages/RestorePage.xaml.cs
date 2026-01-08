using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class RestorePage : UserControl
{
    private string? _currentSource;

    public RestorePage()
    {
        InitializeComponent();
        Loaded += RestorePage_Loaded;
        Unloaded += RestorePage_Unloaded;
    }

    private void RestorePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RestoreViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Load source if already set
            if (viewModel.HasSource && !string.IsNullOrEmpty(viewModel.SourcePath))
            {
                LoadVideo(viewModel.SourcePath);
            }
        }
    }

    private void RestorePage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RestoreViewModel viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RestoreViewModel.SourcePath) || e.PropertyName == nameof(RestoreViewModel.HasSource))
        {
            if (DataContext is RestoreViewModel viewModel && viewModel.HasSource)
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
