using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly ThumbnailService _thumbnailService = new();

    private static readonly string[] VideoExtensions = [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".m2ts"];
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma"];
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"];

    [ObservableProperty]
    private ObservableCollection<MediaBin> _bins = [];

    [ObservableProperty]
    private MediaBin? _selectedBin;

    [ObservableProperty]
    private ObservableCollection<MediaItem> _displayedItems = [];

    [ObservableProperty]
    private MediaItem? _selectedItem;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Grid;

    // Smart bins for filtering
    private MediaBin _allMediaBin = null!;
    private MediaBin _videoBin = null!;
    private MediaBin _audioBin = null!;
    private MediaBin _imagesBin = null!;

    // Master list of all imported items
    private readonly List<MediaItem> _allItems = [];

    public MediaViewModel()
    {
        InitializeBins();
    }

    private void InitializeBins()
    {
        _allMediaBin = new MediaBin { Name = "All Media", Icon = "\uE8B7" };
        _videoBin = new MediaBin { Name = "Video", Icon = "\uE714", FilterType = MediaType.Video };
        _audioBin = new MediaBin { Name = "Audio", Icon = "\uE8D6", FilterType = MediaType.Audio };
        _imagesBin = new MediaBin { Name = "Images", Icon = "\uEB9F", FilterType = MediaType.Image };

        Bins.Add(_allMediaBin);
        Bins.Add(_videoBin);
        Bins.Add(_audioBin);
        Bins.Add(_imagesBin);

        SelectedBin = _allMediaBin;
    }

    partial void OnSelectedBinChanged(MediaBin? value)
    {
        RefreshDisplayedItems();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshDisplayedItems();
    }

    private void RefreshDisplayedItems()
    {
        DisplayedItems.Clear();

        IEnumerable<MediaItem> items = _allItems;

        // Filter by bin
        if (SelectedBin?.FilterType != null)
        {
            items = items.Where(i => i.MediaType == SelectedBin.FilterType);
        }
        else if (SelectedBin != _allMediaBin && SelectedBin != null)
        {
            // Custom user bin - use items in that bin
            items = SelectedBin.Items;
        }

        // Filter by search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            items = items.Where(i => i.Name.ToLowerInvariant().Contains(search));
        }

        foreach (var item in items)
        {
            DisplayedItems.Add(item);
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var count = DisplayedItems.Count;
        var total = _allItems.Count;
        StatusText = count == total
            ? $"{count} items"
            : $"{count} of {total} items";
    }

    [RelayCommand]
    private async Task ImportMediaAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Media",
            Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpg;*.mpeg;*.ts;*.mp3;*.wav;*.flac;*.aac;*.ogg;*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                     "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpg;*.mpeg;*.ts|" +
                     "Audio Files|*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a|" +
                     "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp|" +
                     "All Files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            await ImportFilesAsync(dialog.FileNames);
        }
    }

    [RelayCommand]
    private async Task ImportFolderAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Import Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            var files = Directory.GetFiles(dialog.FolderName, "*.*", SearchOption.AllDirectories)
                .Where(f => IsMediaFile(f))
                .ToArray();

            await ImportFilesAsync(files);
        }
    }

    /// <summary>
    /// Public method to import files from drag-drop
    /// </summary>
    public Task ImportFilesFromDropAsync(string[] filePaths) => ImportFilesAsync(filePaths);

    private async Task ImportFilesAsync(string[] filePaths)
    {
        IsLoading = true;
        StatusText = "Importing...";

        var newItems = new List<MediaItem>();

        foreach (var filePath in filePaths)
        {
            // Skip if already imported
            if (_allItems.Any(i => i.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                continue;

            var item = await CreateMediaItemAsync(filePath);
            if (item != null)
            {
                newItems.Add(item);
            }
        }

        // Add to master list
        _allItems.AddRange(newItems);

        // Update bin counts
        OnPropertyChanged(nameof(Bins));

        RefreshDisplayedItems();

        // Load thumbnails in background
        _ = LoadThumbnailsAsync(newItems);

        IsLoading = false;
        StatusText = $"Imported {newItems.Count} items";
    }

    private async Task<MediaItem?> CreateMediaItemAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var mediaType = GetMediaType(filePath);

            var item = new MediaItem
            {
                Name = fileInfo.Name,
                FilePath = filePath,
                MediaType = mediaType,
                FileSize = fileInfo.Length,
                DateModified = fileInfo.LastWriteTime,
                IsLoadingThumbnail = true
            };

            // Get media info via FFprobe
            var info = await _thumbnailService.GetMediaInfoAsync(filePath);
            if (info != null)
            {
                item.Width = info.Width;
                item.Height = info.Height;
                item.Duration = info.Duration;
                item.FrameRate = info.FrameRate;
                item.FrameCount = info.FrameCount;
                item.Codec = info.VideoCodec ?? info.AudioCodec ?? "";
            }

            return item;
        }
        catch
        {
            return null;
        }
    }

    private async Task LoadThumbnailsAsync(List<MediaItem> items)
    {
        foreach (var item in items)
        {
            try
            {
                if (item.MediaType == MediaType.Image)
                {
                    // Load image thumbnail directly
                    item.Thumbnail = ThumbnailService.LoadImageThumbnail(item.FilePath);
                }
                else if (item.MediaType == MediaType.Video)
                {
                    // Generate video thumbnail via FFmpeg
                    item.Thumbnail = await _thumbnailService.GenerateThumbnailAsync(item.FilePath);
                }

                item.IsLoadingThumbnail = false;
            }
            catch
            {
                item.IsLoadingThumbnail = false;
            }
        }
    }

    private static MediaType GetMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (VideoExtensions.Contains(ext)) return MediaType.Video;
        if (AudioExtensions.Contains(ext)) return MediaType.Audio;
        if (ImageExtensions.Contains(ext)) return MediaType.Image;

        return MediaType.Unknown;
    }

    private static bool IsMediaFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return VideoExtensions.Contains(ext) ||
               AudioExtensions.Contains(ext) ||
               ImageExtensions.Contains(ext);
    }

    [RelayCommand]
    private void CreateBin()
    {
        var newBin = new MediaBin { Name = $"New Bin {Bins.Count}" };
        Bins.Add(newBin);
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem == null) return;

        _allItems.Remove(SelectedItem);
        DisplayedItems.Remove(SelectedItem);
        SelectedItem = null;
        UpdateStatus();
    }

    [RelayCommand]
    private void ClearAll()
    {
        _allItems.Clear();
        DisplayedItems.Clear();
        SelectedItem = null;
        UpdateStatus();
    }

    [RelayCommand]
    private void SetViewMode(string mode)
    {
        CurrentViewMode = mode switch
        {
            "Grid" => ViewMode.Grid,
            "List" => ViewMode.List,
            _ => ViewMode.Grid
        };
    }

    public int VideoCount => _allItems.Count(i => i.MediaType == MediaType.Video);
    public int AudioCount => _allItems.Count(i => i.MediaType == MediaType.Audio);
    public int ImageCount => _allItems.Count(i => i.MediaType == MediaType.Image);
}

public enum ViewMode
{
    Grid,
    List
}
