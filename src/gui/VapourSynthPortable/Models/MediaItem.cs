using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Services;

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

    [ObservableProperty]
    private bool _thumbnailLoadFailed;

    [ObservableProperty]
    private string _thumbnailErrorMessage = "";

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

    // Audio waveform data
    [ObservableProperty]
    private WaveformData? _waveformData;

    [ObservableProperty]
    private StereoWaveformData? _stereoWaveformData;

    [ObservableProperty]
    private bool _isLoadingWaveform;

    [ObservableProperty]
    private bool _hasAudioStream;

    [ObservableProperty]
    private int _audioChannels;

    [ObservableProperty]
    private int _audioSampleRate;

    /// <summary>
    /// Peak audio level in dB
    /// </summary>
    [ObservableProperty]
    private float _peakLevelDb = -96f;

    /// <summary>
    /// Whether the audio clips
    /// </summary>
    [ObservableProperty]
    private bool _hasClipping;

    /// <summary>
    /// Restoration settings applied to this media item.
    /// Set by the Restore page, used by Export page for rendering.
    /// </summary>
    [ObservableProperty]
    private RestorationSettings? _appliedRestoration;

    /// <summary>
    /// Whether this media item has restoration applied
    /// </summary>
    public bool HasRestoration => AppliedRestoration?.IsEnabled == true;

    /// <summary>
    /// Whether waveform data is available
    /// </summary>
    public bool HasWaveform => WaveformData != null || StereoWaveformData != null;

    /// <summary>
    /// Whether this is an audio-only file
    /// </summary>
    public bool IsAudio => MediaType == MediaType.Audio;

    /// <summary>
    /// Whether this is a video file
    /// </summary>
    public bool IsVideo => MediaType == MediaType.Video;

    /// <summary>
    /// Whether this is an image file
    /// </summary>
    public bool IsImage => MediaType == MediaType.Image;

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
