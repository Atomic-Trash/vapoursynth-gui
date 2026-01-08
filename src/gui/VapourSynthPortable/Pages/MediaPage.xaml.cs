using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VapourSynthPortable.Models;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Pages;

/// <summary>
/// Converts a string to a boolean (true if not empty/null)
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class MediaPage : UserControl
{
    private MediaViewModel ViewModel => (MediaViewModel)DataContext;
    private DispatcherTimer? _autoLoadTimer;
    private MediaItem? _pendingLoadItem;

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

        // Setup auto-load timer (200ms delay to avoid rapid loading during navigation)
        _autoLoadTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _autoLoadTimer.Tick += AutoLoadTimer_Tick;
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
            // Schedule auto-load with delay to avoid rapid loading during arrow-key navigation
            var selectedItem = ViewModel?.SelectedItem;
            if (selectedItem != null &&
                (selectedItem.MediaType == MediaType.Video || selectedItem.MediaType == MediaType.Audio))
            {
                _pendingLoadItem = selectedItem;
                _autoLoadTimer?.Stop();
                _autoLoadTimer?.Start();
            }
        }
    }

    private void AutoLoadTimer_Tick(object? sender, EventArgs e)
    {
        _autoLoadTimer?.Stop();

        // Load the pending item if it's still selected
        if (_pendingLoadItem != null && ViewModel?.SelectedItem == _pendingLoadItem)
        {
            VideoPlayer.LoadFile(_pendingLoadItem.FilePath);
        }
        _pendingLoadItem = null;
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

    // Clear search button click
    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SearchText = string.Empty;
        }
        SearchBox?.Focus();
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
            // Cancel rename - restore original name
            ViewModel?.CancelEditBinCommand.Execute(bin);
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

    // Sync multi-selection with ViewModel
    private void MediaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || ViewModel == null) return;

        // Sync selected items to ViewModel
        ViewModel.SelectedItems.Clear();
        foreach (var item in listBox.SelectedItems)
        {
            if (item is MediaItem mediaItem)
            {
                ViewModel.SelectedItems.Add(mediaItem);
            }
        }

        // Notify property change for selection status
        ViewModel.NotifySelectionChanged();
    }

    #region Keyboard Navigation

    // Handle keyboard shortcuts for the media page
    private void MediaPage_KeyDown(object sender, KeyEventArgs e)
    {
        // Escape: Clear search if in search box, otherwise deselect
        if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (e.OriginalSource is TextBox textBox && textBox == SearchBox)
            {
                if (!string.IsNullOrEmpty(ViewModel?.SearchText))
                {
                    ViewModel.SearchText = string.Empty;
                    e.Handled = true;
                    return;
                }
            }
        }

        // Ctrl+A: Select all
        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SelectAllItems();
            e.Handled = true;
            return;
        }

        // Ctrl+F: Focus search box
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox?.Focus();
            e.Handled = true;
            return;
        }

        // Ctrl+I: Import media
        if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ViewModel?.ImportMediaCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Delete: Remove selected items
        if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
        {
            RemoveSelectedItems();
            e.Handled = true;
            return;
        }

        // Enter or Space: Play selected item
        if ((e.Key == Key.Enter || e.Key == Key.Space) && Keyboard.Modifiers == ModifierKeys.None)
        {
            // Only if focus is not in a TextBox (like search)
            if (e.OriginalSource is not TextBox)
            {
                PlaySelectedItem();
                e.Handled = true;
            }
            return;
        }

        // P: Toggle preview panel
        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (e.OriginalSource is not TextBox && ViewModel != null)
            {
                ViewModel.IsPreviewPanelExpanded = !ViewModel.IsPreviewPanelExpanded;
                e.Handled = true;
            }
            return;
        }
    }

    private void SelectAllItems()
    {
        // Select all in the currently visible ListBox
        if (ViewModel?.CurrentViewMode == ViewMode.Grid)
        {
            MediaListBox?.SelectAll();
        }
        else
        {
            MediaListView?.SelectAll();
        }
    }

    private void RemoveSelectedItems()
    {
        if (ViewModel == null) return;

        var itemsToRemove = ViewModel.SelectedItems.ToList();
        if (itemsToRemove.Count == 0) return;

        foreach (var item in itemsToRemove)
        {
            ViewModel.RemoveFromPoolCommand.Execute(item);
        }
    }

    #endregion

    #region Drag & Drop to Bins

    private Point _dragStartPoint;
    private bool _isDragging;
    private const string MediaItemsDataFormat = "VapourSynth.MediaItems";

    // Called when mouse is pressed on media list
    private void MediaListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isDragging = false;
    }

    // Called when mouse moves on media list - detect drag start
    private void MediaListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

        var currentPosition = e.GetPosition(null);
        var diff = _dragStartPoint - currentPosition;

        // Check if we've moved enough to start a drag
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItems.Count > 0)
            {
                _isDragging = true;

                // Get selected items
                var items = listBox.SelectedItems.Cast<MediaItem>().ToArray();

                // Create data object with our items
                var data = new DataObject(MediaItemsDataFormat, items);
                DragDrop.DoDragDrop(listBox, data, DragDropEffects.Copy);

                _isDragging = false;
            }
        }
    }

    // Called when dragging over a bin
    private void BinItem_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(MediaItemsDataFormat))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // Check if it's a custom bin
        var element = sender as FrameworkElement;
        if (element?.DataContext is MediaBin bin && bin.IsCustomBin)
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    // Called when items are dropped onto a bin
    private void BinItem_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(MediaItemsDataFormat)) return;

        var element = sender as FrameworkElement;
        if (element?.DataContext is not MediaBin targetBin || !targetBin.IsCustomBin) return;

        var items = e.Data.GetData(MediaItemsDataFormat) as MediaItem[];
        if (items == null || items.Length == 0) return;

        int addedCount = 0;
        foreach (var item in items)
        {
            if (!targetBin.Items.Contains(item))
            {
                targetBin.Items.Add(item);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            Services.ToastService.Instance.ShowSuccess(
                addedCount == 1
                    ? $"Added 1 item to {targetBin.Name}"
                    : $"Added {addedCount} items to {targetBin.Name}");
        }

        e.Handled = true;
    }

    #endregion
}
