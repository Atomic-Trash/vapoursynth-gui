using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.ViewModels;

public partial class MediaViewModel : ObservableObject, IDisposable
{
    private static readonly ILogger<MediaViewModel> _logger = LoggingService.GetLogger<MediaViewModel>();
    private readonly IMediaPoolService _mediaPool;
    private readonly ISettingsService _settingsService;
    private readonly UndoService _undoService;
    private bool _disposed;

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

    // Multi-selection support
    private ObservableCollection<MediaItem> _selectedItems = [];
    public ObservableCollection<MediaItem> SelectedItems
    {
        get => _selectedItems;
        set
        {
            if (SetProperty(ref _selectedItems, value))
            {
                OnPropertyChanged(nameof(SelectionStatusText));
            }
        }
    }

    public string SelectionStatusText => SelectedItems.Count switch
    {
        0 => "",
        1 => "1 item selected",
        _ => $"{SelectedItems.Count} items selected"
    };

    /// <summary>
    /// Notifies that the selection has changed (called from code-behind)
    /// </summary>
    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectionStatusText));
        OnPropertyChanged(nameof(HasMultipleSelected));
    }

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _importProgress;

    [ObservableProperty]
    private int _totalImportCount;

    [ObservableProperty]
    private int _completedImportCount;

    public string ImportProgressText => TotalImportCount > 0
        ? $"Importing {CompletedImportCount} of {TotalImportCount}..."
        : "Importing...";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Grid;

    // Note: Default to false to avoid UI Automation issues with VideoPlayerControl's HwndHost.
    // The preview can still be expanded by the user toggling the Preview button.
    [ObservableProperty]
    private bool _isPreviewPanelExpanded;

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    // Smart bins for filtering
    private MediaBin _allMediaBin = null!;
    private MediaBin _videoBin = null!;
    private MediaBin _audioBin = null!;
    private MediaBin _imagesBin = null!;

    // Media pool is now shared across all pages via IMediaPoolService
    private IEnumerable<MediaItem> AllItems => _mediaPool.MediaPool;

    public MediaViewModel(IMediaPoolService mediaPool, ISettingsService settingsService, UndoService undoService)
    {
        _mediaPool = mediaPool;
        _settingsService = settingsService;
        _undoService = undoService;
        _mediaPool.MediaPoolChanged += OnMediaPoolChanged;
        _mediaPool.CurrentSourceChanged += OnCurrentSourceChanged;

        InitializeBins();
        LoadCustomBinsFromSettings();
    }

    // Parameterless constructor for XAML design-time support
    public MediaViewModel() : this(
        App.Services?.GetService(typeof(IMediaPoolService)) as IMediaPoolService ?? new MediaPoolService(),
        App.Services?.GetService(typeof(ISettingsService)) as ISettingsService ?? new SettingsService(),
        App.Services?.GetService(typeof(UndoService)) as UndoService ?? new UndoService())
    {
    }

    /// <summary>
    /// Gets whether undo is available
    /// </summary>
    public bool CanUndo => _undoService.CanUndo;

    /// <summary>
    /// Gets whether redo is available
    /// </summary>
    public bool CanRedo => _undoService.CanRedo;

    /// <summary>
    /// Undo the last media pool operation
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        _undoService.Undo();
        RefreshDisplayedItems();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    /// <summary>
    /// Redo the last undone operation
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        _undoService.Redo();
        RefreshDisplayedItems();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void OnMediaPoolChanged(object? sender, EventArgs e)
    {
        RefreshDisplayedItems();
    }

    private void OnCurrentSourceChanged(object? sender, MediaItem? item)
    {
        // Keep selection in sync with current source
        if (item != null && DisplayedItems.Contains(item))
        {
            SelectedItem = item;
        }
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

    [RelayCommand]
    private void SortByColumn(string columnName)
    {
        if (SortColumn == columnName)
        {
            // Toggle direction if same column
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            SortColumn = columnName;
            SortDirection = ListSortDirection.Ascending;
        }
        RefreshDisplayedItems();
    }

    private void RefreshDisplayedItems()
    {
        DisplayedItems.Clear();

        IEnumerable<MediaItem> items = AllItems;

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

        // Filter by search (searches name, resolution, codec, and duration)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            items = items.Where(i =>
                i.Name.ToLowerInvariant().Contains(search) ||
                (i.Resolution?.ToLowerInvariant().Contains(search) ?? false) ||
                (i.Codec?.ToLowerInvariant().Contains(search) ?? false) ||
                (i.DurationFormatted?.ToLowerInvariant().Contains(search) ?? false));
        }

        // Apply sorting
        items = SortColumn switch
        {
            "Name" => SortDirection == ListSortDirection.Ascending
                ? items.OrderBy(i => i.Name)
                : items.OrderByDescending(i => i.Name),
            "Resolution" => SortDirection == ListSortDirection.Ascending
                ? items.OrderBy(i => i.Width * i.Height)
                : items.OrderByDescending(i => i.Width * i.Height),
            "Duration" => SortDirection == ListSortDirection.Ascending
                ? items.OrderBy(i => i.Duration)
                : items.OrderByDescending(i => i.Duration),
            "Size" => SortDirection == ListSortDirection.Ascending
                ? items.OrderBy(i => i.FileSize)
                : items.OrderByDescending(i => i.FileSize),
            "Type" => SortDirection == ListSortDirection.Ascending
                ? items.OrderBy(i => i.MediaType)
                : items.OrderByDescending(i => i.MediaType),
            _ => items
        };

        foreach (var item in items)
        {
            DisplayedItems.Add(item);
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var count = DisplayedItems.Count;
        var total = _mediaPool.MediaPool.Count;
        StatusText = count == total
            ? $"{count} items"
            : $"{count} of {total} items";

        // Notify empty state properties
        OnPropertyChanged(nameof(HasNoMedia));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateSubtitle));
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

        var countBefore = _mediaPool.MediaPool.Count;

        // Delegate import to the shared service
        await _mediaPool.ImportMediaAsync(filePaths);

        var countAfter = _mediaPool.MediaPool.Count;
        var imported = countAfter - countBefore;

        // Update bin counts
        OnPropertyChanged(nameof(Bins));
        OnPropertyChanged(nameof(VideoCount));
        OnPropertyChanged(nameof(AudioCount));
        OnPropertyChanged(nameof(ImageCount));

        RefreshDisplayedItems();

        IsLoading = false;
        StatusText = imported > 0 ? $"Imported {imported} items" : "No new items imported";
    }

    private static bool IsMediaFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return VideoExtensions.Contains(ext) ||
               AudioExtensions.Contains(ext) ||
               ImageExtensions.Contains(ext);
    }

    /// <summary>
    /// Sets the selected item as the current source for all pages
    /// </summary>
    [RelayCommand]
    private void SetAsCurrentSource(MediaItem? item)
    {
        if (item != null)
        {
            _mediaPool.SetCurrentSource(item);
            StatusText = $"Current source: {item.Name}";
        }
    }

    partial void OnSelectedItemChanged(MediaItem? value)
    {
        // Auto-set current source when selection changes (like DaVinci Resolve)
        if (value != null)
        {
            _mediaPool.SetCurrentSource(value);
        }
    }

    [RelayCommand]
    private void CreateBin()
    {
        var binNumber = Bins.Count(b => b.IsCustomBin) + 1;
        var newBin = new MediaBin
        {
            Name = $"New Bin {binNumber}",
            IsCustomBin = true,
            Icon = "\uE8F4" // Custom bin icon
        };
        Bins.Add(newBin);
        SaveCustomBinsToSettings();
        _logger.LogInformation("Created new bin: {Name}", newBin.Name);
    }

    [RelayCommand]
    private void StartEditBin(MediaBin? bin)
    {
        if (bin?.IsCustomBin != true) return;
        bin.EditingOriginalName = bin.Name;
        bin.IsEditing = true;
    }

    [RelayCommand]
    private void EndEditBin(MediaBin? bin)
    {
        if (bin == null) return;
        bin.IsEditing = false;
        bin.EditingOriginalName = null;
        SaveCustomBinsToSettings();
        _logger.LogInformation("Renamed bin to: {Name}", bin.Name);
    }

    [RelayCommand]
    private void CancelEditBin(MediaBin? bin)
    {
        if (bin == null) return;
        // Restore original name if we have one
        if (bin.EditingOriginalName != null)
        {
            bin.Name = bin.EditingOriginalName;
            bin.EditingOriginalName = null;
        }
        bin.IsEditing = false;
        _logger.LogDebug("Cancelled bin rename, restored to: {Name}", bin.Name);
    }

    [RelayCommand]
    private void DeleteBin(MediaBin? bin)
    {
        if (bin?.IsCustomBin != true) return;

        // Move items back to All Media
        foreach (var item in bin.Items.ToList())
        {
            bin.Items.Remove(item);
        }

        Bins.Remove(bin);

        // Select All Media bin if we deleted the selected bin
        if (SelectedBin == bin)
        {
            SelectedBin = _allMediaBin;
        }

        SaveCustomBinsToSettings();
        _logger.LogInformation("Deleted bin: {Name}", bin.Name);
    }

    [RelayCommand]
    private void AddToBin(MediaBin? targetBin)
    {
        if (targetBin?.IsCustomBin != true || SelectedItem == null) return;

        // Check if item is already in the bin
        if (!targetBin.Items.Contains(SelectedItem))
        {
            targetBin.Items.Add(SelectedItem);
            SaveCustomBinsToSettings();
            _logger.LogInformation("Added {Item} to bin {Bin}", SelectedItem.Name, targetBin.Name);
        }
    }

    [RelayCommand]
    private void RemoveFromBin(MediaItem? item)
    {
        if (item == null || SelectedBin?.IsCustomBin != true) return;

        SelectedBin.Items.Remove(item);
        SaveCustomBinsToSettings();
        RefreshDisplayedItems();
        _logger.LogInformation("Removed {Item} from bin {Bin}", item.Name, SelectedBin.Name);
    }

    private void LoadCustomBinsFromSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            foreach (var binSettings in settings.CustomBins)
            {
                var bin = new MediaBin
                {
                    Id = binSettings.Id,
                    Name = binSettings.Name,
                    IsCustomBin = true,
                    Icon = "\uE8F4"
                };

                // Note: Item paths are stored but items are populated when media is imported
                // This is a reference-based approach - items are added to bins dynamically
                Bins.Add(bin);
            }
            _logger.LogInformation("Loaded {Count} custom bins from settings", settings.CustomBins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load custom bins from settings");
        }
    }

    private void SaveCustomBinsToSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.CustomBins = Bins
                .Where(b => b.IsCustomBin)
                .Select(b => new CustomBinSettings
                {
                    Id = b.Id,
                    Name = b.Name,
                    ItemPaths = b.Items.Select(i => i.FilePath).ToList()
                })
                .ToList();
            _settingsService.Save(settings);
            _logger.LogDebug("Saved {Count} custom bins to settings", settings.CustomBins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save custom bins to settings");
        }
    }

    // Get custom bins for context menu
    public IEnumerable<MediaBin> CustomBins => Bins.Where(b => b.IsCustomBin);

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem == null) return;

        var item = SelectedItem;
        var index = _mediaPool.MediaPool.IndexOf(item);

        // Record undo action before removing
        _undoService.RecordAction(
            $"Remove {item.Name}",
            undoAction: () =>
            {
                _mediaPool.AddMedia(item);
                RefreshDisplayedItems();
            },
            redoAction: () =>
            {
                _mediaPool.RemoveMedia(item);
                RefreshDisplayedItems();
            });

        _mediaPool.RemoveMedia(item);
        DisplayedItems.Remove(item);
        SelectedItem = null;

        OnPropertyChanged(nameof(VideoCount));
        OnPropertyChanged(nameof(AudioCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));

        UpdateStatus();
        ToastService.Instance.ShowInfo("Removed", $"{item.Name} removed (Ctrl+Z to undo)");
    }

    [RelayCommand]
    private void RevealInExplorer(MediaItem? item)
    {
        item ??= SelectedItem;
        if (item == null || !File.Exists(item.FilePath)) return;

        try
        {
            // Select the file in Explorer
            Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
            _logger.LogDebug("Revealed in Explorer: {Path}", item.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reveal in Explorer: {Path}", item.FilePath);
        }
    }

    [RelayCommand]
    private void RemoveFromPool(MediaItem? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        var removedItem = item;

        // Record undo action before removing
        _undoService.RecordAction(
            $"Remove {removedItem.Name}",
            undoAction: () =>
            {
                _mediaPool.AddMedia(removedItem);
                RefreshDisplayedItems();
            },
            redoAction: () =>
            {
                _mediaPool.RemoveMedia(removedItem);
                RefreshDisplayedItems();
            });

        _mediaPool.RemoveMedia(removedItem);
        DisplayedItems.Remove(removedItem);

        if (SelectedItem == removedItem)
            SelectedItem = null;

        OnPropertyChanged(nameof(VideoCount));
        OnPropertyChanged(nameof(AudioCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));

        UpdateStatus();
        ToastService.Instance.ShowInfo("Removed", $"{removedItem.Name} removed (Ctrl+Z to undo)");
        _logger.LogInformation("Removed from pool: {Name}", removedItem.Name);
    }

    [RelayCommand]
    private void AddItemToBin(object? parameter)
    {
        // Parameter is a tuple of (MediaItem, MediaBin) or just MediaBin if using SelectedItem
        if (parameter is MediaBin targetBin)
        {
            if (SelectedItem == null || targetBin.IsCustomBin != true) return;
            if (!targetBin.Items.Contains(SelectedItem))
            {
                targetBin.Items.Add(SelectedItem);
                SaveCustomBinsToSettings();
                ToastService.Instance.ShowSuccess($"Added to {targetBin.Name}");
                _logger.LogInformation("Added {Item} to bin {Bin}", SelectedItem.Name, targetBin.Name);
            }
        }
    }

    // Batch Operations

    [RelayCommand]
    private void BatchDeleteSelected()
    {
        if (SelectedItems.Count == 0) return;

        // Capture all selected items for undo
        var itemsToDelete = SelectedItems.ToList();
        var count = itemsToDelete.Count;

        // Record undo action before removing
        _undoService.RecordAction(
            $"Delete {count} items",
            undoAction: () =>
            {
                foreach (var item in itemsToDelete)
                {
                    _mediaPool.AddMedia(item);
                }
                RefreshDisplayedItems();
            },
            redoAction: () =>
            {
                foreach (var item in itemsToDelete)
                {
                    _mediaPool.RemoveMedia(item);
                }
                RefreshDisplayedItems();
            });

        // Remove all selected items
        foreach (var item in itemsToDelete)
        {
            _mediaPool.RemoveMedia(item);
            DisplayedItems.Remove(item);
        }

        SelectedItems.Clear();
        SelectedItem = null;

        OnPropertyChanged(nameof(VideoCount));
        OnPropertyChanged(nameof(AudioCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        NotifySelectionChanged();

        UpdateStatus();
        ToastService.Instance.ShowInfo("Deleted", $"{count} items removed (Ctrl+Z to undo)");
        _logger.LogInformation("Batch deleted {Count} items from pool", count);
    }

    [RelayCommand]
    private void BatchAddToBin(MediaBin? targetBin)
    {
        if (targetBin?.IsCustomBin != true || SelectedItems.Count == 0) return;

        var itemsToAdd = SelectedItems.ToList();
        var addedCount = 0;

        foreach (var item in itemsToAdd)
        {
            // Check if item is already in the bin
            if (!targetBin.Items.Contains(item))
            {
                targetBin.Items.Add(item);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            SaveCustomBinsToSettings();
            ToastService.Instance.ShowSuccess($"Added {addedCount} items to {targetBin.Name}");
            _logger.LogInformation("Batch added {Count} items to bin {Bin}", addedCount, targetBin.Name);
        }
        else
        {
            ToastService.Instance.ShowInfo("Items already in bin");
        }
    }

    [RelayCommand]
    private void BatchRemoveFromBin()
    {
        if (SelectedBin?.IsCustomBin != true || SelectedItems.Count == 0) return;

        var itemsToRemove = SelectedItems.ToList();
        var removedCount = 0;

        foreach (var item in itemsToRemove)
        {
            if (SelectedBin.Items.Remove(item))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            SaveCustomBinsToSettings();
            RefreshDisplayedItems();
            SelectedItems.Clear();
            NotifySelectionChanged();
            ToastService.Instance.ShowInfo($"Removed {removedCount} items from {SelectedBin.Name}");
            _logger.LogInformation("Batch removed {Count} items from bin {Bin}", removedCount, SelectedBin.Name);
        }
    }

    /// <summary>
    /// Indicates if batch operations are available (multiple items selected)
    /// </summary>
    public bool HasMultipleSelected => SelectedItems.Count > 1;

    [RelayCommand]
    private void ClearAll()
    {
        if (_mediaPool.MediaPool.Count == 0) return;

        // Capture all items for undo
        var allItems = _mediaPool.MediaPool.ToList();

        // Record undo action before clearing
        _undoService.RecordAction(
            $"Clear all ({allItems.Count} items)",
            undoAction: () =>
            {
                foreach (var item in allItems)
                {
                    _mediaPool.AddMedia(item);
                }
                RefreshDisplayedItems();
            },
            redoAction: () =>
            {
                _mediaPool.ClearPool();
                RefreshDisplayedItems();
            });

        _mediaPool.ClearPool();
        DisplayedItems.Clear();
        SelectedItem = null;

        OnPropertyChanged(nameof(VideoCount));
        OnPropertyChanged(nameof(AudioCount));
        OnPropertyChanged(nameof(ImageCount));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));

        UpdateStatus();
        ToastService.Instance.ShowInfo("Cleared", $"Removed {allItems.Count} items (Ctrl+Z to undo)");
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

    public int VideoCount => _mediaPool.MediaPool.Count(i => i.MediaType == MediaType.Video);
    public int AudioCount => _mediaPool.MediaPool.Count(i => i.MediaType == MediaType.Audio);
    public int ImageCount => _mediaPool.MediaPool.Count(i => i.MediaType == MediaType.Image);

    // Empty state helpers
    public bool HasNoMedia => _mediaPool.MediaPool.Count == 0;
    public bool HasNoResults => !HasNoMedia && DisplayedItems.Count == 0;
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

    public string EmptyStateTitle => HasNoMedia
        ? "No media imported"
        : (IsSearching ? "No results found" : "No items in this bin");

    public string EmptyStateSubtitle => HasNoMedia
        ? "Click 'Import Media' or drag files here to get started"
        : (IsSearching ? $"No items match \"{SearchText}\"" : "Add items to this bin from the media pool");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPool.MediaPoolChanged -= OnMediaPoolChanged;
        _mediaPool.CurrentSourceChanged -= OnCurrentSourceChanged;
    }
}

public enum ViewMode
{
    Grid,
    List
}
