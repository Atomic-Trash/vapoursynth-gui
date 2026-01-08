using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class EditPage : UserControl
{
    public EditPage()
    {
        InitializeComponent();
    }

    private void MediaItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (sender is FrameworkElement element && element.DataContext is MediaItem mediaItem)
            {
                // Start drag operation
                var data = new DataObject(typeof(MediaItem), mediaItem);
                DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
            }
        }
    }

    private void TimelineControl_ClipSelected(object? sender, TimelineClip? clip)
    {
        if (DataContext is EditViewModel viewModel)
        {
            viewModel.Timeline.SelectedClip = clip;
        }
    }
}
