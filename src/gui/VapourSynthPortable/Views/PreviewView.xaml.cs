using System.Windows;
using System.Windows.Controls;
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
}
