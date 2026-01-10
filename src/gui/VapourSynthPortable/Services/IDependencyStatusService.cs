using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Represents the status of a dependency
/// </summary>
public class DependencyStatus
{
    public string Name { get; init; } = "";
    public bool IsAvailable { get; init; }
    public string? Version { get; init; }
    public string? Path { get; init; }
    public string? ErrorMessage { get; init; }

    public static DependencyStatus Available(string name, string? version = null, string? path = null) =>
        new() { Name = name, IsAvailable = true, Version = version, Path = path };

    public static DependencyStatus Unavailable(string name, string? errorMessage = null) =>
        new() { Name = name, IsAvailable = false, ErrorMessage = errorMessage ?? $"{name} is not available" };
}

/// <summary>
/// Aggregates status of all application dependencies
/// </summary>
public class DependencyStatusReport
{
    public DependencyStatus VapourSynth { get; init; } = DependencyStatus.Unavailable("VapourSynth");
    public DependencyStatus FFmpeg { get; init; } = DependencyStatus.Unavailable("FFmpeg");
    public DependencyStatus Python { get; init; } = DependencyStatus.Unavailable("Python");
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether all required dependencies are available
    /// </summary>
    public bool AllRequiredAvailable => VapourSynth.IsAvailable && FFmpeg.IsAvailable;

    /// <summary>
    /// Whether all dependencies are available
    /// </summary>
    public bool AllAvailable => VapourSynth.IsAvailable && FFmpeg.IsAvailable && Python.IsAvailable;

    /// <summary>
    /// Gets a list of missing required dependencies
    /// </summary>
    public IReadOnlyList<DependencyStatus> GetMissingRequired()
    {
        var missing = new List<DependencyStatus>();
        if (!VapourSynth.IsAvailable) missing.Add(VapourSynth);
        if (!FFmpeg.IsAvailable) missing.Add(FFmpeg);
        return missing;
    }

    /// <summary>
    /// Gets a summary message about missing dependencies
    /// </summary>
    public string? GetMissingSummary()
    {
        var missing = GetMissingRequired();
        if (missing.Count == 0) return null;

        return $"Missing dependencies: {string.Join(", ", missing.Select(d => d.Name))}";
    }
}

/// <summary>
/// Service for checking and reporting dependency availability
/// </summary>
public interface IDependencyStatusService
{
    /// <summary>
    /// Event raised when dependency status changes
    /// </summary>
    event EventHandler<DependencyStatusReport>? StatusChanged;

    /// <summary>
    /// The current dependency status report
    /// </summary>
    DependencyStatusReport CurrentStatus { get; }

    /// <summary>
    /// Whether all required dependencies are available
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Check all dependencies and update status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated status report</returns>
    Task<DependencyStatusReport> CheckDependenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check a specific dependency
    /// </summary>
    /// <param name="dependencyName">The name of the dependency to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with details</returns>
    Task<Result<DependencyStatus>> CheckDependencyAsync(string dependencyName, CancellationToken cancellationToken = default);
}
