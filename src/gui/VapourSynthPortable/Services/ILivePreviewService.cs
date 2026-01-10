using System.Windows.Media.Imaging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service interface for live preview generation with debouncing
/// </summary>
public interface ILivePreviewService
{
    /// <summary>
    /// Whether VapourSynth preview is available
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Whether a preview is currently being generated
    /// </summary>
    bool IsGenerating { get; }

    /// <summary>
    /// Raised when preview generation starts
    /// </summary>
    event EventHandler? PreviewStarted;

    /// <summary>
    /// Raised when preview generation completes
    /// </summary>
    event EventHandler<BitmapSource?>? PreviewCompleted;

    /// <summary>
    /// Request a preview update with debouncing (cancels pending requests)
    /// </summary>
    /// <param name="script">VapourSynth script to execute</param>
    /// <param name="frameNumber">Frame number to extract</param>
    /// <param name="sourcePath">Optional source file path for cache key</param>
    void RequestPreview(string script, int frameNumber, string? sourcePath = null);

    /// <summary>
    /// Request a preview update immediately (no debouncing)
    /// </summary>
    Task<BitmapSource?> GeneratePreviewAsync(string script, int frameNumber, CancellationToken ct = default);

    /// <summary>
    /// Cancel any pending or active preview generation
    /// </summary>
    void Cancel();

    /// <summary>
    /// Clear the preview cache
    /// </summary>
    void ClearCache();
}
