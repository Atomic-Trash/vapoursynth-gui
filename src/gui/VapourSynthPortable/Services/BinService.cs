using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for managing media bins with validation and undo support.
/// </summary>
public partial class BinService : IBinService
{
    private static readonly ILogger<BinService> _logger = LoggingService.GetLogger<BinService>();

    private readonly ISettingsService _settingsService;
    private readonly UndoService _undoService;
    private readonly MediaBin _allMediaBin;
    private readonly MediaBin _videoBin;
    private readonly MediaBin _audioBin;
    private readonly MediaBin _imagesBin;

    // Regex for invalid filename characters
    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1F]")]
    private static partial Regex InvalidCharsRegex();

    public event EventHandler? BinsChanged;

    public ObservableCollection<MediaBin> Bins { get; } = [];

    public MediaBin AllMediaBin => _allMediaBin;

    public BinService(ISettingsService settingsService, UndoService undoService)
    {
        _settingsService = settingsService;
        _undoService = undoService;

        // Create system bins
        _allMediaBin = new MediaBin
        {
            Name = "All Media",
            Icon = "\uE8B7", // Folder
            IsCustomBin = false
        };

        _videoBin = new MediaBin
        {
            Name = "Video",
            Icon = "\uE714", // Video
            FilterType = MediaType.Video,
            IsCustomBin = false
        };

        _audioBin = new MediaBin
        {
            Name = "Audio",
            Icon = "\uE8D6", // Audio
            FilterType = MediaType.Audio,
            IsCustomBin = false
        };

        _imagesBin = new MediaBin
        {
            Name = "Images",
            Icon = "\uEB9F", // Photo
            FilterType = MediaType.Image,
            IsCustomBin = false
        };

        Bins.Add(_allMediaBin);
        Bins.Add(_videoBin);
        Bins.Add(_audioBin);
        Bins.Add(_imagesBin);

        _logger.LogDebug("BinService initialized with {Count} system bins", Bins.Count);
    }

    public Result<MediaBin> CreateBin(string? name = null)
    {
        // Generate name if not provided
        var binName = name?.Trim();
        if (string.IsNullOrEmpty(binName))
        {
            var binNumber = Bins.Count(b => b.IsCustomBin) + 1;
            binName = $"New Bin {binNumber}";

            // Ensure unique name
            while (IsBinNameInUse(binName))
            {
                binNumber++;
                binName = $"New Bin {binNumber}";
            }
        }

        // Validate name
        if (!IsValidBinName(binName))
        {
            return Result<MediaBin>.Failure("Invalid bin name. Name cannot be empty or contain special characters.");
        }

        if (IsBinNameInUse(binName))
        {
            return Result<MediaBin>.Failure($"A bin named '{binName}' already exists.");
        }

        var newBin = new MediaBin
        {
            Name = binName,
            IsCustomBin = true,
            Icon = "\uE8F4" // Custom folder icon
        };

        Bins.Add(newBin);

        // Record undo action
        _undoService.RecordAdd(Bins, newBin, $"Create bin '{binName}'");

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Created bin: {Name}", binName);
        return Result<MediaBin>.Success(newBin);
    }

    public Result DeleteBin(MediaBin bin)
    {
        if (bin == null)
        {
            return Result.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result.Failure("System bins cannot be deleted.");
        }

        if (!Bins.Contains(bin))
        {
            return Result.Failure("Bin not found.");
        }

        var binName = bin.Name;
        var binIndex = Bins.IndexOf(bin);
        var itemsCopy = bin.Items.ToList();

        // Record undo action before removal
        _undoService.RecordAction(new BinDeleteUndoAction(
            $"Delete bin '{binName}'",
            Bins,
            bin,
            binIndex,
            itemsCopy));

        // Clear items (they remain in media pool)
        bin.Items.Clear();
        Bins.Remove(bin);

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Deleted bin: {Name} (had {Count} items)", binName, itemsCopy.Count);
        return Result.Success();
    }

    public Result RenameBin(MediaBin bin, string newName)
    {
        if (bin == null)
        {
            return Result.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result.Failure("System bins cannot be renamed.");
        }

        var trimmedName = newName?.Trim() ?? "";

        if (!IsValidBinName(trimmedName))
        {
            return Result.Failure("Invalid bin name. Name cannot be empty or contain special characters.");
        }

        if (IsBinNameInUse(trimmedName, bin))
        {
            return Result.Failure($"A bin named '{trimmedName}' already exists.");
        }

        var oldName = bin.Name;

        // Record undo action
        _undoService.RecordAction(new PropertyChangeUndoAction<string>(
            $"Rename bin '{oldName}' to '{trimmedName}'",
            () => bin.Name,
            v => bin.Name = v,
            oldName,
            trimmedName));

        bin.Name = trimmedName;

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Renamed bin: {OldName} -> {NewName}", oldName, trimmedName);
        return Result.Success();
    }

    public Result AddItemToBin(MediaBin bin, MediaItem item)
    {
        if (bin == null)
        {
            return Result.Failure("Bin cannot be null.");
        }

        if (item == null)
        {
            return Result.Failure("Item cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result.Failure("Items can only be added to custom bins.");
        }

        if (bin.Items.Contains(item))
        {
            return Result.Failure("Item is already in this bin.");
        }

        bin.Items.Add(item);

        // Record undo action
        _undoService.RecordAdd(bin.Items, item, $"Add '{item.Name}' to bin '{bin.Name}'");

        SaveBins();
        OnBinsChanged();

        _logger.LogDebug("Added '{Item}' to bin '{Bin}'", item.Name, bin.Name);
        return Result.Success();
    }

    public Result<int> AddItemsToBin(MediaBin bin, IEnumerable<MediaItem> items)
    {
        if (bin == null)
        {
            return Result<int>.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result<int>.Failure("Items can only be added to custom bins.");
        }

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var addedCount = 0;
        var addedItems = new List<MediaItem>();

        using (_undoService.BeginTransaction($"Add {itemList.Count} items to bin '{bin.Name}'"))
        {
            foreach (var item in itemList)
            {
                if (!bin.Items.Contains(item))
                {
                    bin.Items.Add(item);
                    addedItems.Add(item);
                    addedCount++;
                }
            }

            // Record single undo action for batch
            if (addedItems.Count > 0)
            {
                _undoService.RecordAction(new BatchCollectionUndoAction<MediaItem>(
                    $"Add {addedCount} items to bin '{bin.Name}'",
                    bin.Items,
                    addedItems,
                    isAdd: true));
            }
        }

        if (addedCount > 0)
        {
            SaveBins();
            OnBinsChanged();
        }

        _logger.LogInformation("Added {Count} items to bin '{Bin}'", addedCount, bin.Name);
        return Result<int>.Success(addedCount);
    }

    public Result RemoveItemFromBin(MediaBin bin, MediaItem item)
    {
        if (bin == null)
        {
            return Result.Failure("Bin cannot be null.");
        }

        if (item == null)
        {
            return Result.Failure("Item cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result.Failure("Items can only be removed from custom bins.");
        }

        if (!bin.Items.Contains(item))
        {
            return Result.Failure("Item is not in this bin.");
        }

        var index = bin.Items.IndexOf(item);

        // Record undo action before removal
        _undoService.RecordAction(new CollectionUndoAction<MediaItem>(
            $"Remove '{item.Name}' from bin '{bin.Name}'",
            bin.Items,
            item,
            index,
            isAdd: false));

        bin.Items.Remove(item);

        SaveBins();
        OnBinsChanged();

        _logger.LogDebug("Removed '{Item}' from bin '{Bin}'", item.Name, bin.Name);
        return Result.Success();
    }

    public Result<int> RemoveItemsFromBin(MediaBin bin, IEnumerable<MediaItem> items)
    {
        if (bin == null)
        {
            return Result<int>.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result<int>.Failure("Items can only be removed from custom bins.");
        }

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var removedCount = 0;
        var removedItems = new List<MediaItem>();

        using (_undoService.BeginTransaction($"Remove {itemList.Count} items from bin '{bin.Name}'"))
        {
            foreach (var item in itemList)
            {
                if (bin.Items.Contains(item))
                {
                    removedItems.Add(item);
                    bin.Items.Remove(item);
                    removedCount++;
                }
            }

            // Record single undo action for batch
            if (removedItems.Count > 0)
            {
                _undoService.RecordAction(new BatchCollectionUndoAction<MediaItem>(
                    $"Remove {removedCount} items from bin '{bin.Name}'",
                    bin.Items,
                    removedItems,
                    isAdd: false));
            }
        }

        if (removedCount > 0)
        {
            SaveBins();
            OnBinsChanged();
        }

        _logger.LogInformation("Removed {Count} items from bin '{Bin}'", removedCount, bin.Name);
        return Result<int>.Success(removedCount);
    }

    public Result MoveItemToBin(MediaItem item, MediaBin sourceBin, MediaBin targetBin)
    {
        if (item == null)
        {
            return Result.Failure("Item cannot be null.");
        }

        if (sourceBin == null || targetBin == null)
        {
            return Result.Failure("Source and target bins cannot be null.");
        }

        if (!sourceBin.IsCustomBin || !targetBin.IsCustomBin)
        {
            return Result.Failure("Items can only be moved between custom bins.");
        }

        if (sourceBin == targetBin)
        {
            return Result.Failure("Source and target bins are the same.");
        }

        if (!sourceBin.Items.Contains(item))
        {
            return Result.Failure("Item is not in the source bin.");
        }

        if (targetBin.Items.Contains(item))
        {
            return Result.Failure("Item is already in the target bin.");
        }

        using (_undoService.BeginTransaction($"Move '{item.Name}' from '{sourceBin.Name}' to '{targetBin.Name}'"))
        {
            var sourceIndex = sourceBin.Items.IndexOf(item);

            sourceBin.Items.Remove(item);
            targetBin.Items.Add(item);

            // Record both operations for undo
            _undoService.RecordAction(new MoveItemUndoAction(
                $"Move '{item.Name}' from '{sourceBin.Name}' to '{targetBin.Name}'",
                item,
                sourceBin,
                targetBin,
                sourceIndex));
        }

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Moved '{Item}' from '{Source}' to '{Target}'",
            item.Name, sourceBin.Name, targetBin.Name);
        return Result.Success();
    }

    public MediaBin? GetBinForItem(MediaItem item)
    {
        if (item == null) return null;

        return Bins.FirstOrDefault(b => b.IsCustomBin && b.Items.Contains(item));
    }

    public IReadOnlyList<MediaBin> GetBinsForItem(MediaItem item)
    {
        if (item == null) return [];

        return Bins.Where(b => b.IsCustomBin && b.Items.Contains(item)).ToList();
    }

    public bool IsValidBinName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (name.Length > 255)
        {
            return false;
        }

        // Check for invalid filename characters
        if (InvalidCharsRegex().IsMatch(name))
        {
            return false;
        }

        return true;
    }

    public bool IsBinNameInUse(string name, MediaBin? excludeBin = null)
    {
        return Bins.Any(b =>
            b != excludeBin &&
            string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveBins()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.CustomBins.Clear();

            foreach (var bin in Bins.Where(b => b.IsCustomBin))
            {
                settings.CustomBins.Add(new CustomBinSettings
                {
                    Id = bin.Id,
                    Name = bin.Name,
                    ItemPaths = bin.Items.Select(i => i.FilePath).ToList()
                });
            }

            _settingsService.Save(settings);
            _logger.LogDebug("Saved {Count} custom bins to settings", settings.CustomBins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save bins to settings");
        }
    }

    public void LoadBins(IEnumerable<MediaItem> mediaPool)
    {
        try
        {
            var settings = _settingsService.Load();
            var mediaLookup = mediaPool.ToDictionary(m => m.FilePath, StringComparer.OrdinalIgnoreCase);

            // Remove existing custom bins
            var customBins = Bins.Where(b => b.IsCustomBin).ToList();
            foreach (var bin in customBins)
            {
                Bins.Remove(bin);
            }

            // Load custom bins from settings
            foreach (var binSettings in settings.CustomBins)
            {
                var bin = new MediaBin
                {
                    Id = binSettings.Id,
                    Name = binSettings.Name,
                    IsCustomBin = true,
                    Icon = "\uE8F4"
                };

                // Resolve item references
                foreach (var path in binSettings.ItemPaths)
                {
                    if (mediaLookup.TryGetValue(path, out var item))
                    {
                        bin.Items.Add(item);
                    }
                    else
                    {
                        _logger.LogDebug("Bin '{Bin}' references missing item: {Path}", bin.Name, path);
                    }
                }

                Bins.Add(bin);
            }

            OnBinsChanged();
            _logger.LogInformation("Loaded {Count} custom bins from settings", settings.CustomBins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load bins from settings");
        }
    }

    public Result ClearBin(MediaBin bin)
    {
        if (bin == null)
        {
            return Result.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result.Failure("Only custom bins can be cleared.");
        }

        if (bin.Items.Count == 0)
        {
            return Result.Success();
        }

        var itemsCopy = bin.Items.ToList();

        // Record undo action
        _undoService.RecordAction(new BatchCollectionUndoAction<MediaItem>(
            $"Clear bin '{bin.Name}' ({itemsCopy.Count} items)",
            bin.Items,
            itemsCopy,
            isAdd: false));

        bin.Items.Clear();

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Cleared bin '{Name}' ({Count} items)", bin.Name, itemsCopy.Count);
        return Result.Success();
    }

    public Result<MediaBin> DuplicateBin(MediaBin bin)
    {
        if (bin == null)
        {
            return Result<MediaBin>.Failure("Bin cannot be null.");
        }

        if (!bin.IsCustomBin)
        {
            return Result<MediaBin>.Failure("Only custom bins can be duplicated.");
        }

        // Generate unique name
        var baseName = bin.Name;
        var newName = $"{baseName} Copy";
        var counter = 1;

        while (IsBinNameInUse(newName))
        {
            counter++;
            newName = $"{baseName} Copy {counter}";
        }

        var newBin = new MediaBin
        {
            Name = newName,
            IsCustomBin = true,
            Icon = bin.Icon
        };

        // Copy items
        foreach (var item in bin.Items)
        {
            newBin.Items.Add(item);
        }

        Bins.Add(newBin);

        // Record undo action
        _undoService.RecordAdd(Bins, newBin, $"Duplicate bin '{bin.Name}'");

        SaveBins();
        OnBinsChanged();

        _logger.LogInformation("Duplicated bin '{Original}' as '{Copy}' with {Count} items",
            bin.Name, newBin.Name, newBin.Items.Count);
        return Result<MediaBin>.Success(newBin);
    }

    private void OnBinsChanged()
    {
        BinsChanged?.Invoke(this, EventArgs.Empty);
    }
}

#region Undo Actions

/// <summary>
/// Undo action for deleting a bin.
/// </summary>
internal class BinDeleteUndoAction : IUndoAction
{
    private readonly IList<MediaBin> _collection;
    private readonly MediaBin _bin;
    private readonly int _index;
    private readonly List<MediaItem> _items;

    public string Description { get; }

    public BinDeleteUndoAction(
        string description,
        IList<MediaBin> collection,
        MediaBin bin,
        int index,
        List<MediaItem> items)
    {
        Description = description;
        _collection = collection;
        _bin = bin;
        _index = index;
        _items = items;
    }

    public void Undo()
    {
        // Restore items
        foreach (var item in _items)
        {
            _bin.Items.Add(item);
        }

        // Restore bin at original position
        if (_index >= 0 && _index <= _collection.Count)
        {
            _collection.Insert(_index, _bin);
        }
        else
        {
            _collection.Add(_bin);
        }
    }

    public void Redo()
    {
        _bin.Items.Clear();
        _collection.Remove(_bin);
    }
}

/// <summary>
/// Undo action for moving an item between bins.
/// </summary>
internal class MoveItemUndoAction : IUndoAction
{
    private readonly MediaItem _item;
    private readonly MediaBin _sourceBin;
    private readonly MediaBin _targetBin;
    private readonly int _sourceIndex;

    public string Description { get; }

    public MoveItemUndoAction(
        string description,
        MediaItem item,
        MediaBin sourceBin,
        MediaBin targetBin,
        int sourceIndex)
    {
        Description = description;
        _item = item;
        _sourceBin = sourceBin;
        _targetBin = targetBin;
        _sourceIndex = sourceIndex;
    }

    public void Undo()
    {
        _targetBin.Items.Remove(_item);

        if (_sourceIndex >= 0 && _sourceIndex <= _sourceBin.Items.Count)
        {
            _sourceBin.Items.Insert(_sourceIndex, _item);
        }
        else
        {
            _sourceBin.Items.Add(_item);
        }
    }

    public void Redo()
    {
        _sourceBin.Items.Remove(_item);
        _targetBin.Items.Add(_item);
    }
}

/// <summary>
/// Undo action for batch collection operations.
/// </summary>
internal class BatchCollectionUndoAction<T> : IUndoAction
{
    private readonly IList<T> _collection;
    private readonly List<T> _items;
    private readonly bool _isAdd;

    public string Description { get; }

    public BatchCollectionUndoAction(
        string description,
        IList<T> collection,
        List<T> items,
        bool isAdd)
    {
        Description = description;
        _collection = collection;
        _items = items;
        _isAdd = isAdd;
    }

    public void Undo()
    {
        if (_isAdd)
        {
            // Was an add, so remove items
            foreach (var item in _items)
            {
                _collection.Remove(item);
            }
        }
        else
        {
            // Was a remove, so add items back
            foreach (var item in _items)
            {
                _collection.Add(item);
            }
        }
    }

    public void Redo()
    {
        if (_isAdd)
        {
            // Re-add items
            foreach (var item in _items)
            {
                _collection.Add(item);
            }
        }
        else
        {
            // Re-remove items
            foreach (var item in _items)
            {
                _collection.Remove(item);
            }
        }
    }
}

/// <summary>
/// Undo action for property changes.
/// </summary>
internal class PropertyChangeUndoAction<T> : IUndoAction
{
    private readonly Func<T> _getter;
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;

    public string Description { get; }

    public PropertyChangeUndoAction(
        string description,
        Func<T> getter,
        Action<T> setter,
        T oldValue,
        T newValue)
    {
        Description = description;
        _getter = getter;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Undo()
    {
        _setter(_oldValue);
    }

    public void Redo()
    {
        _setter(_newValue);
    }
}

#endregion
