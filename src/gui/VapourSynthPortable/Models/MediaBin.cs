using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

public partial class MediaBin : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _icon = "\uE8B7"; // Folder icon

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private MediaBin? _parent;

    public ObservableCollection<MediaBin> Children { get; } = [];

    public ObservableCollection<MediaItem> Items { get; } = [];

    public int TotalItemCount
    {
        get
        {
            int count = Items.Count;
            foreach (var child in Children)
            {
                count += child.TotalItemCount;
            }
            return count;
        }
    }

    // Smart bins filter by media type
    public MediaType? FilterType { get; set; }

    public bool IsSmartBin => FilterType.HasValue;
}
