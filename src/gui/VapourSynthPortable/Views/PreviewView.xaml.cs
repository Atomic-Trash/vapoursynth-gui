using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Views;

public partial class PreviewView : UserControl
{
    public PreviewView()
    {
        InitializeComponent();

        // Handle mouse wheel for zoom
        FrameScrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    private PreviewViewModel? ViewModel => DataContext as PreviewViewModel;

    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        switch (e.Key)
        {
            case Key.Left:
                if (ViewModel.PreviousFrameCommand.CanExecute(null))
                    ViewModel.PreviousFrameCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Right:
                if (ViewModel.NextFrameCommand.CanExecute(null))
                    ViewModel.NextFrameCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Home:
                if (ViewModel.FirstFrameCommand.CanExecute(null))
                    ViewModel.FirstFrameCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.End:
                if (ViewModel.LastFrameCommand.CanExecute(null))
                    ViewModel.LastFrameCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Add:
            case Key.OemPlus:
                ViewModel.ZoomInCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Subtract:
            case Key.OemMinus:
                ViewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.D0:
            case Key.NumPad0:
                ViewModel.ToggleFitToWindowCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.D1:
            case Key.NumPad1:
                ViewModel.ResetZoomCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F5:
                if (ViewModel.RefreshCommand.CanExecute(null))
                    ViewModel.RefreshCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.C:
                // Toggle comparison mode
                if (ViewModel.ToggleComparisonCommand.CanExecute(null))
                    ViewModel.ToggleComparisonCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.M:
                // Cycle comparison modes when in comparison view
                if (ViewModel.ShowComparison && ViewModel.CycleComparisonModeCommand.CanExecute(null))
                    ViewModel.CycleComparisonModeCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.I:
                // Toggle metadata information overlay
                if (ViewModel.ToggleMetadataOverlayCommand.CanExecute(null))
                    ViewModel.ToggleMetadataOverlayCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && ViewModel != null)
        {
            if (e.Delta > 0)
                ViewModel.ZoomInCommand.Execute(null);
            else
                ViewModel.ZoomOutCommand.Execute(null);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Loads a script into the preview.
    /// </summary>
    public void LoadScript(string scriptPath)
    {
        if (ViewModel != null && ViewModel.LoadScriptCommand.CanExecute(scriptPath))
        {
            ViewModel.LoadScriptCommand.Execute(scriptPath);
        }
    }

    /// <summary>
    /// Gets the underlying view model for external access.
    /// </summary>
    public PreviewViewModel? GetViewModel() => ViewModel;

    /// <summary>
    /// Sets the comparison script for A/B preview
    /// </summary>
    public void SetComparisonScript(string? scriptPath)
    {
        if (ViewModel != null && ViewModel.SetComparisonScriptCommand.CanExecute(scriptPath))
        {
            ViewModel.SetComparisonScriptCommand.Execute(scriptPath);
        }
    }

    private void ComparisonModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || ComparisonModeCombo.SelectedIndex < 0) return;

        var mode = (ComparisonMode)ComparisonModeCombo.SelectedIndex;
        if (ViewModel.SetComparisonModeCommand.CanExecute(mode))
        {
            ViewModel.SetComparisonModeCommand.Execute(mode);
        }
    }

    private void ToggleImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Show B image when mouse is held down
        ToggleImageA.Visibility = Visibility.Collapsed;
        ToggleImageB.Visibility = Visibility.Visible;
        ToggleLabel.Text = "B (release for A)";
    }

    private void ToggleImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // Show A image when mouse is released
        ToggleImageA.Visibility = Visibility.Visible;
        ToggleImageB.Visibility = Visibility.Collapsed;
        ToggleLabel.Text = "A (hold to see B)";
    }
}

/// <summary>
/// Converts wipe position and dimensions to a clip rectangle
/// </summary>
public class WipeClipConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 ||
            values[0] is not double position ||
            values[1] is not double width ||
            values[2] is not double height)
        {
            return new Rect(0, 0, 100, 100);
        }

        return new Rect(0, 0, width * position, height);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}

/// <summary>
/// Converts wipe position and container width to a left margin for the divider line
/// </summary>
public class WipeMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 ||
            values[0] is not double position ||
            values[1] is not double containerWidth)
        {
            return new Thickness(0);
        }

        // Calculate left margin based on position (0-1) and container width
        var leftMargin = containerWidth * position;
        return new Thickness(leftMargin, 0, 0, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}
