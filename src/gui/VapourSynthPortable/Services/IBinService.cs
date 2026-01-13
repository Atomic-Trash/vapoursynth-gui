using System.Collections.ObjectModel;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for managing media bins (folders for organizing media items).
/// </summary>
public interface IBinService
{
    /// <summary>
    /// Event raised when bins collection changes.
    /// </summary>
    event EventHandler? BinsChanged;

    /// <summary>
    /// Gets the collection of all bins.
    /// </summary>
    ObservableCollection<MediaBin> Bins { get; }

    /// <summary>
    /// Gets the "All Media" system bin.
    /// </summary>
    MediaBin AllMediaBin { get; }

    /// <summary>
    /// Creates a new custom bin with the specified name.
    /// </summary>
    /// <param name="name">Optional name for the bin. Auto-generated if null.</param>
    /// <returns>Result containing the created bin or error message.</returns>
    Result<MediaBin> CreateBin(string? name = null);

    /// <summary>
    /// Deletes a custom bin. System bins cannot be deleted.
    /// </summary>
    /// <param name="bin">The bin to delete.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result DeleteBin(MediaBin bin);

    /// <summary>
    /// Renames a custom bin. System bins cannot be renamed.
    /// </summary>
    /// <param name="bin">The bin to rename.</param>
    /// <param name="newName">The new name for the bin.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result RenameBin(MediaBin bin, string newName);

    /// <summary>
    /// Adds a media item to a custom bin.
    /// </summary>
    /// <param name="bin">The target bin.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result AddItemToBin(MediaBin bin, MediaItem item);

    /// <summary>
    /// Adds multiple media items to a custom bin.
    /// </summary>
    /// <param name="bin">The target bin.</param>
    /// <param name="items">The items to add.</param>
    /// <returns>Result containing the count of items added.</returns>
    Result<int> AddItemsToBin(MediaBin bin, IEnumerable<MediaItem> items);

    /// <summary>
    /// Removes a media item from a custom bin.
    /// </summary>
    /// <param name="bin">The bin to remove from.</param>
    /// <param name="item">The item to remove.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result RemoveItemFromBin(MediaBin bin, MediaItem item);

    /// <summary>
    /// Removes multiple media items from a custom bin.
    /// </summary>
    /// <param name="bin">The bin to remove from.</param>
    /// <param name="items">The items to remove.</param>
    /// <returns>Result containing the count of items removed.</returns>
    Result<int> RemoveItemsFromBin(MediaBin bin, IEnumerable<MediaItem> items);

    /// <summary>
    /// Moves an item from one bin to another.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="sourceBin">The source bin.</param>
    /// <param name="targetBin">The target bin.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result MoveItemToBin(MediaItem item, MediaBin sourceBin, MediaBin targetBin);

    /// <summary>
    /// Gets the bin containing a specific item, or null if not in any custom bin.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>The bin containing the item, or null.</returns>
    MediaBin? GetBinForItem(MediaItem item);

    /// <summary>
    /// Gets all bins containing a specific item.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>List of bins containing the item.</returns>
    IReadOnlyList<MediaBin> GetBinsForItem(MediaItem item);

    /// <summary>
    /// Checks if a bin name is valid.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidBinName(string name);

    /// <summary>
    /// Checks if a bin name is already in use.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="excludeBin">Optional bin to exclude from check (for rename).</param>
    /// <returns>True if the name is already in use.</returns>
    bool IsBinNameInUse(string name, MediaBin? excludeBin = null);

    /// <summary>
    /// Saves the current bin state to settings.
    /// </summary>
    void SaveBins();

    /// <summary>
    /// Loads bins from settings.
    /// </summary>
    /// <param name="mediaPool">The media pool to resolve item references.</param>
    void LoadBins(IEnumerable<MediaItem> mediaPool);

    /// <summary>
    /// Clears all items from a bin without deleting the bin.
    /// </summary>
    /// <param name="bin">The bin to clear.</param>
    /// <returns>Result indicating success or error message.</returns>
    Result ClearBin(MediaBin bin);

    /// <summary>
    /// Duplicates a bin with all its items.
    /// </summary>
    /// <param name="bin">The bin to duplicate.</param>
    /// <returns>Result containing the duplicated bin.</returns>
    Result<MediaBin> DuplicateBin(MediaBin bin);
}
