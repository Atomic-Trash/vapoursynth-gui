using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Controls;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class ColorPage : UserControl
{
    private static readonly ILogger<ColorPage> _logger = LoggingService.GetLogger<ColorPage>();
    private string? _currentSource;
    private DateTime _lastScopeUpdate = DateTime.MinValue;
    private bool _scopeUpdatePending;
    private readonly TimeSpan _scopeUpdateInterval = TimeSpan.FromMilliseconds(500);

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
            viewModel.CurvesReset += ViewModel_CurvesReset;
            viewModel.CurvesLoaded += ViewModel_CurvesLoaded;

            // Wire up CurvesControl events
            CurvesEditor.CurveChanged += CurvesEditor_CurveChanged;

            // Wire up video player events for live scopes
            PreviewPlayer.PositionChanged += PreviewPlayer_PositionChanged;
            PreviewPlayer.FileLoaded += PreviewPlayer_FileLoaded;

            // Initialize CurvesControl with current curve points from grade
            InitializeCurvesFromGrade(viewModel);

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
            viewModel.CurvesReset -= ViewModel_CurvesReset;
            viewModel.CurvesLoaded -= ViewModel_CurvesLoaded;
        }

        CurvesEditor.CurveChanged -= CurvesEditor_CurveChanged;
        PreviewPlayer.PositionChanged -= PreviewPlayer_PositionChanged;
        PreviewPlayer.FileLoaded -= PreviewPlayer_FileLoaded;
    }

    private void CurvesEditor_CurveChanged(object? sender, CurveChangedEventArgs e)
    {
        if (DataContext is ColorViewModel viewModel)
        {
            var points = CurvesEditor.GetCurvePoints(e.Channel);
            viewModel.OnCurveChanged(e.Channel, e.LookupTable, points);
        }
    }

    private void ViewModel_CurvesReset(object? sender, EventArgs e)
    {
        // Reset all curves in the CurvesControl
        CurvesEditor.SetCurvePoints(CurveChannel.RGB, [new Point(0, 0), new Point(1, 1)]);
        CurvesEditor.SetCurvePoints(CurveChannel.Red, [new Point(0, 0), new Point(1, 1)]);
        CurvesEditor.SetCurvePoints(CurveChannel.Green, [new Point(0, 0), new Point(1, 1)]);
        CurvesEditor.SetCurvePoints(CurveChannel.Blue, [new Point(0, 0), new Point(1, 1)]);
    }

    private void ViewModel_CurvesLoaded(object? sender, EventArgs e)
    {
        // Load curves from project into the CurvesControl
        if (DataContext is ColorViewModel viewModel)
        {
            InitializeCurvesFromGrade(viewModel);
        }
    }

    private void InitializeCurvesFromGrade(ColorViewModel viewModel)
    {
        // Load curve points from the grade into the CurvesControl
        CurvesEditor.SetCurvePoints(CurveChannel.RGB, viewModel.GetCurvePoints(CurveChannel.RGB));
        CurvesEditor.SetCurvePoints(CurveChannel.Red, viewModel.GetCurvePoints(CurveChannel.Red));
        CurvesEditor.SetCurvePoints(CurveChannel.Green, viewModel.GetCurvePoints(CurveChannel.Green));
        CurvesEditor.SetCurvePoints(CurveChannel.Blue, viewModel.GetCurvePoints(CurveChannel.Blue));
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

    private void PreviewPlayer_FileLoaded(object? sender, string e)
    {
        // Update scopes when a new file is loaded
        _ = UpdateScopesAsync();
    }

    private void PreviewPlayer_PositionChanged(object? sender, double e)
    {
        // Throttle scope updates to avoid performance issues
        if (DataContext is not ColorViewModel viewModel || !viewModel.ShowScopes)
            return;

        var now = DateTime.Now;
        if (now - _lastScopeUpdate >= _scopeUpdateInterval && !_scopeUpdatePending)
        {
            _scopeUpdatePending = true;
            _ = UpdateScopesAsync();
        }
    }

    private async Task UpdateScopesAsync()
    {
        try
        {
            // Check if scopes are visible
            if (DataContext is not ColorViewModel viewModel || !viewModel.ShowScopes)
            {
                _scopeUpdatePending = false;
                return;
            }

            // Capture frame from video player
            var frameData = await PreviewPlayer.CaptureCurrentFrameAsync();

            if (frameData.HasValue)
            {
                // Update scopes on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    ScopesDisplay.UpdateFromFrame(frameData.Value.RgbData, frameData.Value.Width, frameData.Value.Height);
                }, DispatcherPriority.Background);

                _lastScopeUpdate = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to update scopes");
        }
        finally
        {
            _scopeUpdatePending = false;
        }
    }
}
