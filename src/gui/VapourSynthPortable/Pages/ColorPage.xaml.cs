using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class ColorPage : UserControl
{
    private readonly ILogger<ColorPage> _logger = LoggingService.GetLogger<ColorPage>();
    private string? _currentSource;

    public ColorPage()
    {
        InitializeComponent();

        // Get ViewModel from DI to ensure shared MediaPoolService singleton
        DataContext = App.Services?.GetService(typeof(ColorViewModel))
            ?? new ColorViewModel();

        Loaded += ColorPage_Loaded;
        Unloaded += ColorPage_Unloaded;
        IsVisibleChanged += ColorPage_IsVisibleChanged;
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

    private void ColorPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // When page becomes visible, check if there's a source to load
        if ((bool)e.NewValue && DataContext is ColorViewModel viewModel)
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

        try
        {
            _currentSource = path;
            PreviewPlayer.LoadFile(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load video for color grading: {Path}", path);
            ToastService.Instance.ShowError("Failed to load video", ex.Message);
        }
    }
}
