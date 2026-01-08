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

        // Get ViewModel from DI to ensure shared MediaPoolService singleton
        DataContext = App.Services?.GetService(typeof(RestoreViewModel))
            ?? new RestoreViewModel();

        Loaded += RestorePage_Loaded;
        Unloaded += RestorePage_Unloaded;
        IsVisibleChanged += RestorePage_IsVisibleChanged;
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

    private void RestorePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // When page becomes visible, check if there's a source to load
        if ((bool)e.NewValue && DataContext is RestoreViewModel viewModel)
        {
            if (viewModel.HasSource && !string.IsNullOrEmpty(viewModel.SourcePath))
            {
                // Only load if different from current
                if (_currentSource != viewModel.SourcePath)
                {
                    LoadVideo(viewModel.SourcePath);
                }
            }
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
