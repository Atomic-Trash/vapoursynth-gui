using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VapourSynthPortable.Models;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

public partial class MediaPage : UserControl
{
    private MediaViewModel ViewModel => (MediaViewModel)DataContext;

    public MediaPage()
    {
        InitializeComponent();

        // Get ViewModel from DI to ensure shared MediaPoolService singleton
        DataContext = App.Services?.GetService(typeof(MediaViewModel))
            ?? new MediaViewModel();

        AllowDrop = true;
        Drop += MediaPage_Drop;
        DragOver += MediaPage_DragOver;

        // Subscribe to selection changes
        DataContextChanged += MediaPage_DataContextChanged;
    }

    private void MediaPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MediaViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaViewModel.SelectedItem))
        {
            // When selection changes, we could auto-load preview
            // For now, just update the button state
        }
    }

    private void MediaPage_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void MediaPage_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await ViewModel.ImportFilesFromDropAsync(files);
            }
        }
    }

    private void PlaySelectedButton_Click(object sender, RoutedEventArgs e)
    {
        PlaySelectedItem();
    }

    private void PlaySelectedItem()
    {
        var selectedItem = ViewModel?.SelectedItem;
        if (selectedItem == null) return;

        // Only play video and audio files
        if (selectedItem.MediaType == MediaType.Video || selectedItem.MediaType == MediaType.Audio)
        {
            VideoPlayer.LoadFile(selectedItem.FilePath);
        }
        else if (selectedItem.MediaType == MediaType.Image)
        {
            // For images, just show the thumbnail preview
            ThumbnailPreview.Visibility = Visibility.Visible;
        }
    }

    // Handle double-click on media items to play them
    private void MediaListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var selectedItem = ViewModel?.SelectedItem;
        if (selectedItem != null)
        {
            if (selectedItem.MediaType == MediaType.Video || selectedItem.MediaType == MediaType.Audio)
            {
                VideoPlayer.LoadFile(selectedItem.FilePath);
            }
        }
    }

    // Handle Enter/Escape for inline bin rename
    private void BinNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (textBox.DataContext is not MediaBin bin) return;

        if (e.Key == Key.Enter)
        {
            // Confirm rename
            ViewModel?.EndEditBinCommand.Execute(bin);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Cancel rename (restore original name would require storing it, for now just end edit)
            ViewModel?.EndEditBinCommand.Execute(bin);
            e.Handled = true;
        }
    }

    // Confirm rename when focus is lost
    private void BinNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (textBox.DataContext is not MediaBin bin) return;

        if (bin.IsEditing)
        {
            ViewModel?.EndEditBinCommand.Execute(bin);
        }
    }
}
