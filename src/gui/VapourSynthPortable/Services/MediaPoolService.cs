using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Centralized media pool service following DaVinci Resolve's architecture.
/// Single source of truth for all media across pages.
/// </summary>
public interface IMediaPoolService
{
    // Media Pool - all imported media
    ObservableCollection<MediaItem> MediaPool { get; }

    // Current source - shared across all pages
    MediaItem? CurrentSource { get; set; }
    bool HasSource { get; }

    // Playhead state - synchronized across pages
    double PlayheadPosition { get; set; }
    double InPoint { get; set; }
    double OutPoint { get; set; }

    // Timeline reference - shared from Edit page for Export
    Timeline? EditTimeline { get; }
    void SetEditTimeline(Timeline? timeline);
    event EventHandler<Timeline?>? EditTimelineChanged;

    // Media operations
    Task<MediaItem?> ImportMediaAsync(string filePath);
    Task ImportMediaAsync(string[] filePaths);
    void AddMedia(MediaItem item);
    void RemoveMedia(MediaItem item);
    void ClearPool();

    // Source selection
    void SetCurrentSource(MediaItem? item);
    void SetCurrentSourceByPath(string filePath);

    // Events for page synchronization
    event EventHandler<MediaItem?>? CurrentSourceChanged;
    event EventHandler<double>? PlayheadPositionChanged;
    event EventHandler? MediaPoolChanged;
}

public partial class MediaPoolService : ObservableObject, IMediaPoolService
{
    private static readonly ILogger<MediaPoolService> _logger = LoggingService.GetLogger<MediaPoolService>();
    private readonly ThumbnailService _thumbnailService;
    private readonly AudioWaveformService _waveformService;

    public MediaPoolService(IPathResolver pathResolver)
    {
        _thumbnailService = new ThumbnailService(pathResolver);
        _waveformService = new AudioWaveformService();
        MediaPool = [];
        _logger.LogInformation("MediaPoolService initialized");
    }

    // Media Pool
    public ObservableCollection<MediaItem> MediaPool { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSource))]
    private MediaItem? _currentSource;

    public bool HasSource => CurrentSource != null;

    [ObservableProperty]
    private double _playheadPosition;

    [ObservableProperty]
    private double _inPoint;

    [ObservableProperty]
    private double _outPoint;

    // Timeline reference for Export page
    [ObservableProperty]
    private Timeline? _editTimeline;

    public void SetEditTimeline(Timeline? timeline)
    {
        EditTimeline = timeline;
        EditTimelineChanged?.Invoke(this, timeline);
        _logger.LogDebug("Edit timeline set: HasClips={HasClips}", timeline?.HasClips ?? false);
    }

    // Events
    public event EventHandler<MediaItem?>? CurrentSourceChanged;
    public event EventHandler<double>? PlayheadPositionChanged;
    public event EventHandler? MediaPoolChanged;
    public event EventHandler<Timeline?>? EditTimelineChanged;

    partial void OnCurrentSourceChanged(MediaItem? value)
    {
        // Reset playhead when source changes
        PlayheadPosition = 0;
        InPoint = 0;
        OutPoint = value?.Duration ?? 0;

        CurrentSourceChanged?.Invoke(this, value);
    }

    partial void OnPlayheadPositionChanged(double value)
    {
        PlayheadPositionChanged?.Invoke(this, value);
    }

    public async Task<MediaItem?> ImportMediaAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("ImportMediaAsync: Invalid path or file not found: {Path}", filePath);
            return null;
        }

        // Check if already in pool
        var existing = MediaPool.FirstOrDefault(m =>
            m.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _logger.LogDebug("Media already in pool: {Path}", filePath);
            return existing;
        }

        _logger.LogInformation("Importing media: {Path}", filePath);

        var item = new MediaItem
        {
            Name = Path.GetFileName(filePath),
            FilePath = filePath,
            MediaType = GetMediaType(filePath),
            DateModified = File.GetLastWriteTime(filePath),
            FileSize = new FileInfo(filePath).Length
        };

        // Load metadata and thumbnail
        await LoadMediaMetadataAsync(item);

        MediaPool.Add(item);
        MediaPoolChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation("Media imported: {Name} ({Width}x{Height}, {Duration:F1}s)",
            item.Name, item.Width, item.Height, item.Duration);

        return item;
    }

    public async Task ImportMediaAsync(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            await ImportMediaAsync(path);
        }
    }

    public void AddMedia(MediaItem item)
    {
        if (item == null) return;

        // Check if item already exists in pool
        if (!MediaPool.Contains(item) && !MediaPool.Any(m => m.FilePath == item.FilePath))
        {
            MediaPool.Add(item);
            MediaPoolChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Added media to pool: {Name}", item.Name);
        }
    }

    public void RemoveMedia(MediaItem item)
    {
        if (CurrentSource == item)
        {
            CurrentSource = null;
        }

        MediaPool.Remove(item);
        MediaPoolChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearPool()
    {
        CurrentSource = null;
        MediaPool.Clear();
        MediaPoolChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCurrentSource(MediaItem? item)
    {
        if (item != null && !MediaPool.Contains(item))
        {
            // Auto-add to pool if not present
            MediaPool.Add(item);
            MediaPoolChanged?.Invoke(this, EventArgs.Empty);
        }

        CurrentSource = item;
    }

    public void SetCurrentSourceByPath(string filePath)
    {
        var item = MediaPool.FirstOrDefault(m =>
            m.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            CurrentSource = item;
        }
    }

    private static MediaType GetMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm" or
            ".mpg" or ".mpeg" or ".ts" or ".m2ts" or ".flv" or ".vob" => MediaType.Video,

            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or
            ".wma" or ".m4a" or ".aiff" => MediaType.Audio,

            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or
            ".tiff" or ".tif" or ".webp" or ".exr" or ".dpx" => MediaType.Image,

            _ => MediaType.Unknown
        };
    }

    private async Task LoadMediaMetadataAsync(MediaItem item)
    {
        if (item.MediaType == MediaType.Video || item.MediaType == MediaType.Image)
        {
            item.IsLoadingThumbnail = true;
            item.ThumbnailLoadFailed = false;
            item.ThumbnailErrorMessage = "";

            try
            {
                // Generate thumbnail
                var thumbnail = await _thumbnailService.GenerateThumbnailAsync(item.FilePath);
                item.Thumbnail = thumbnail;

                if (thumbnail == null)
                {
                    item.ThumbnailLoadFailed = true;
                    item.ThumbnailErrorMessage = "Failed to generate";
                }

                // Get video metadata using FFprobe if available
                await LoadVideoMetadataAsync(item);
            }
            catch (OperationCanceledException)
            {
                item.ThumbnailLoadFailed = true;
                item.ThumbnailErrorMessage = "Cancelled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load metadata for {FilePath}", item.FilePath);
                item.ThumbnailLoadFailed = true;
                item.ThumbnailErrorMessage = "Error loading";
            }
            finally
            {
                item.IsLoadingThumbnail = false;
            }
        }

        // Generate waveform for video and audio files (non-blocking)
        if (item.MediaType == MediaType.Video || item.MediaType == MediaType.Audio)
        {
            _ = LoadWaveformAsync(item);
        }
    }

    private async Task LoadWaveformAsync(MediaItem item)
    {
        item.IsLoadingWaveform = true;

        try
        {
            // Extract stereo waveform for full channel info
            var stereoWaveform = await _waveformService.ExtractStereoWaveformAsync(
                item.FilePath,
                samplesPerSecond: 100);

            if (stereoWaveform != null)
            {
                item.StereoWaveformData = stereoWaveform;
                item.HasAudioStream = true;
                item.AudioChannels = stereoWaveform.ChannelCount;
                item.PeakLevelDb = stereoWaveform.MaxPeakDb;
                item.HasClipping = stereoWaveform.ClippingCount > 0;

                _logger.LogInformation("Loaded waveform for {Name}: {Channels}ch, peak={Peak:F1}dB, clipping={Clips}",
                    item.Name, stereoWaveform.ChannelCount, stereoWaveform.MaxPeakDb, item.HasClipping);
            }
            else
            {
                // Fall back to mono waveform
                var monoWaveform = await _waveformService.ExtractWaveformAsync(
                    item.FilePath,
                    samplesPerSecond: 100);

                if (monoWaveform != null)
                {
                    item.WaveformData = monoWaveform;
                    item.HasAudioStream = true;
                    item.AudioChannels = 1;

                    var peak = monoWaveform.Samples.Max(s => s.AbsolutePeak);
                    item.PeakLevelDb = peak > 0 ? 20f * (float)Math.Log10(peak) : -96f;
                    item.HasClipping = monoWaveform.Samples.Any(s => s.IsClipping);

                    _logger.LogInformation("Loaded mono waveform for {Name}: peak={Peak:F1}dB",
                        item.Name, item.PeakLevelDb);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load waveform for {FilePath}", item.FilePath);
        }
        finally
        {
            item.IsLoadingWaveform = false;
        }
    }

    private async Task LoadVideoMetadataAsync(MediaItem item)
    {
        // Try to get video info using FFprobe
        try
        {
            var ffprobePath = FindFFprobe();
            if (string.IsNullOrEmpty(ffprobePath))
                return;

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{item.FilePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                ParseFFprobeOutput(item, output);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FFprobe failed for {FilePath}", item.FilePath);
        }
    }

    private static string? FindFFprobe()
    {
        // Check common locations
        var paths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffprobe.exe"),
            @"C:\Program Files\ffmpeg\bin\ffprobe.exe",
            "ffprobe" // In PATH
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        // Check if in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            foreach (var dir in pathEnv.Split(';'))
            {
                var ffprobe = Path.Combine(dir, "ffprobe.exe");
                if (File.Exists(ffprobe))
                    return ffprobe;
            }
        }

        return null;
    }

    private static void ParseFFprobeOutput(MediaItem item, string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Get duration from format
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var duration))
                {
                    if (double.TryParse(duration.GetString(), out var dur))
                    {
                        item.Duration = dur;
                    }
                }
            }

            // Get video stream info
            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (stream.TryGetProperty("codec_type", out var codecType) &&
                        codecType.GetString() == "video")
                    {
                        if (stream.TryGetProperty("width", out var width))
                            item.Width = width.GetInt32();

                        if (stream.TryGetProperty("height", out var height))
                            item.Height = height.GetInt32();

                        if (stream.TryGetProperty("codec_name", out var codec))
                            item.Codec = codec.GetString() ?? "";

                        // Parse frame rate
                        if (stream.TryGetProperty("r_frame_rate", out var fpsStr))
                        {
                            var fps = fpsStr.GetString();
                            if (!string.IsNullOrEmpty(fps) && fps.Contains('/'))
                            {
                                var parts = fps.Split('/');
                                if (parts.Length == 2 &&
                                    double.TryParse(parts[0], out var num) &&
                                    double.TryParse(parts[1], out var den) &&
                                    den > 0)
                                {
                                    item.FrameRate = num / den;
                                }
                            }
                        }

                        if (stream.TryGetProperty("nb_frames", out var frames))
                        {
                            if (int.TryParse(frames.GetString(), out var frameCount))
                            {
                                item.FrameCount = frameCount;
                            }
                        }

                        break; // Only need first video stream
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse FFprobe output");
        }
    }
}
