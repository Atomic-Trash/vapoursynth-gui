namespace VapourSynthPortable.Models;

/// <summary>
/// Unified export settings for the single VapourSynth-based export pipeline.
/// All exports flow through the same pipeline regardless of complexity.
/// </summary>
public class UnifiedExportSettings
{
    // === Source Configuration ===

    /// <summary>
    /// Type of source for export (single file or timeline)
    /// </summary>
    public ExportSourceType SourceType { get; set; } = ExportSourceType.SingleFile;

    /// <summary>
    /// Path to single source file (when SourceType is SingleFile)
    /// </summary>
    public string? SingleFilePath { get; set; }

    /// <summary>
    /// Timeline to export (when SourceType is Timeline)
    /// </summary>
    public Timeline? Timeline { get; set; }

    // === Restoration Configuration ===

    /// <summary>
    /// Whether to include restoration in the export pipeline
    /// </summary>
    public bool IncludeRestoration { get; set; }

    /// <summary>
    /// Restoration settings to apply (if IncludeRestoration is true)
    /// </summary>
    public RestorationSettings? Restoration { get; set; }

    // === Color Grading ===

    /// <summary>
    /// Global color grade applied after per-clip grades (optional)
    /// </summary>
    public ColorGrade? GlobalColorGrade { get; set; }

    // === Output Configuration ===

    /// <summary>
    /// Output file path
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Output width (0 = use source resolution)
    /// </summary>
    public int OutputWidth { get; set; }

    /// <summary>
    /// Output height (0 = use source resolution)
    /// </summary>
    public int OutputHeight { get; set; }

    /// <summary>
    /// Output frame rate (0 = use source frame rate)
    /// </summary>
    public double OutputFrameRate { get; set; }

    // === Video Encoding ===

    /// <summary>
    /// Video codec (libx264, libx265, h264_nvenc, hevc_nvenc, prores_ks, ffv1)
    /// </summary>
    public string VideoCodec { get; set; } = "libx264";

    /// <summary>
    /// Quality/CRF value (0-51 for x264/x265, lower = better)
    /// </summary>
    public int Quality { get; set; } = 18;

    /// <summary>
    /// Encoding preset (ultrafast to veryslow for software, p1-p7 for NVENC)
    /// </summary>
    public string Preset { get; set; } = "medium";

    /// <summary>
    /// Hardware preset for NVENC (p1-p7)
    /// </summary>
    public string HardwarePreset { get; set; } = "p4";

    /// <summary>
    /// ProRes profile (0-5: proxy, lt, standard, hq, 4444, 4444xq)
    /// </summary>
    public int ProResProfile { get; set; } = 2;

    /// <summary>
    /// Pixel format (yuv420p, yuv422p, yuv444p, etc.)
    /// </summary>
    public string PixelFormat { get; set; } = "yuv420p";

    // === Audio Encoding ===

    /// <summary>
    /// Whether to include audio in the export
    /// </summary>
    public bool AudioEnabled { get; set; } = true;

    /// <summary>
    /// Audio codec (aac, libmp3lame, flac, pcm_s16le, copy)
    /// </summary>
    public string AudioCodec { get; set; } = "aac";

    /// <summary>
    /// Audio bitrate in kbps (e.g., 192, 256, 320)
    /// </summary>
    public int AudioBitrate { get; set; } = 192;

    /// <summary>
    /// Path to audio source file (usually the input file or primary timeline clip)
    /// </summary>
    public string? AudioSourcePath { get; set; }

    // === Helpers ===

    /// <summary>
    /// Whether this export has any video processing (effects, color, restoration)
    /// </summary>
    public bool HasProcessing =>
        IncludeRestoration ||
        GlobalColorGrade != null ||
        (Timeline?.HasClips == true && Timeline.Tracks.Any(t =>
            t.Clips.Any(c => c.HasEffects || c.HasColorGrade)));

    /// <summary>
    /// Gets the primary source path for audio extraction
    /// </summary>
    public string? GetPrimarySourcePath()
    {
        if (!string.IsNullOrEmpty(AudioSourcePath))
            return AudioSourcePath;

        if (SourceType == ExportSourceType.SingleFile)
            return SingleFilePath;

        // For timeline, get first clip with audio
        var firstClip = Timeline?.Tracks
            .Where(t => t.TrackType == TrackType.Video)
            .SelectMany(t => t.Clips)
            .OrderBy(c => c.StartFrame)
            .FirstOrDefault();

        return firstClip?.SourcePath ?? SingleFilePath;
    }
}

/// <summary>
/// Source type for export
/// </summary>
public enum ExportSourceType
{
    /// <summary>
    /// Export a single source file
    /// </summary>
    SingleFile,

    /// <summary>
    /// Export from timeline with clips
    /// </summary>
    Timeline
}
