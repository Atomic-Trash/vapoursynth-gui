namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for VapourSynth script execution
/// </summary>
public interface IVapourSynthService
{
    /// <summary>
    /// Whether the service is currently processing
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Whether VapourSynth is available (VSPipe exists)
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Progress update event
    /// </summary>
    event EventHandler<VapourSynthProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Log message event
    /// </summary>
    event EventHandler<string>? LogMessage;

    /// <summary>
    /// Processing started event
    /// </summary>
    event EventHandler? ProcessingStarted;

    /// <summary>
    /// Processing completed event
    /// </summary>
    event EventHandler<VapourSynthCompletedEventArgs>? ProcessingCompleted;

    /// <summary>
    /// Get information about a VapourSynth script
    /// </summary>
    Task<VapourSynthScriptInfo?> GetScriptInfoAsync(string scriptPath, CancellationToken ct = default);

    /// <summary>
    /// Execute a VapourSynth script and encode output with FFmpeg
    /// </summary>
    Task<bool> ProcessScriptAsync(
        string scriptPath,
        string outputPath,
        VapourSynthEncodingSettings? settings = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a VapourSynth script without processing
    /// </summary>
    Task<VapourSynthValidationResult> ValidateScriptAsync(string scriptPath, CancellationToken ct = default);

    /// <summary>
    /// List available VapourSynth plugins
    /// </summary>
    Task<List<VapourSynthPlugin>> GetPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Cancel the current processing operation
    /// </summary>
    void Cancel();
}
