using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

public enum MediaType
{
    Video,
    Audio,
    Image,
    Unknown
}

public partial class MediaItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private MediaType _mediaType = MediaType.Unknown;

    [ObservableProperty]
    private BitmapSource? _thumbnail;

    [ObservableProperty]
    private bool _isLoadingThumbnail;

    // Video/Image properties
    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private double _frameRate;

    [ObservableProperty]
    private int _frameCount;

    // Duration in seconds
    [ObservableProperty]
    private double _duration;

    // File info
    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private string _codec = "";

    [ObservableProperty]
    private DateTime _dateModified;

    [ObservableProperty]
    private bool _isSelected;

    public string Resolution => Width > 0 && Height > 0 ? $"{Width}x{Height}" : "";

    public string DurationFormatted
    {
        get
        {
            if (Duration <= 0) return "";
            var ts = TimeSpan.FromSeconds(Duration);
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize <= 0) return "";
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            int order = 0;
            double size = FileSize;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    public string MediaTypeIcon => MediaType switch
    {
        MediaType.Video => "\uE714",  // Video icon
        MediaType.Audio => "\uE8D6",  // Audio icon
        MediaType.Image => "\uEB9F",  // Image icon
        _ => "\uE8A5"                 // File icon
    };
}
