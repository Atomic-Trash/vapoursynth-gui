using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class RestorePage : UserControl
{
    private readonly ILogger<RestorePage> _logger = LoggingService.GetLogger<RestorePage>();
    private string? _currentSource;
    private bool _showingOriginal;

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

        try
        {
            _currentSource = path;
            PreviewPlayer.LoadFile(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load video for restoration: {Path}", path);
            ToastService.Instance.ShowError("Failed to load video", ex.Message);
        }
    }

    private void ComparisonPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (DataContext is not RestoreViewModel viewModel) return;

            // Toggle between showing original and processed
            _showingOriginal = !_showingOriginal;

            if (_showingOriginal)
            {
                ToggleImage.Source = viewModel.OriginalFrame;
                ToggleLabel.Text = "ORIGINAL (click to toggle)";
                ToggleLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
            }
            else
            {
                ToggleImage.Source = viewModel.ProcessedFrame;
                ToggleLabel.Text = "PROCESSED (click to toggle)";
                ToggleLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle comparison view");
        }
    }
}
