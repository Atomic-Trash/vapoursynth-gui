using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for build operations
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Run the VapourSynth portable build
    /// </summary>
    Task<BuildResult> RunBuildAsync(
        BuildConfiguration config,
        Action<string> onOutput,
        Action<BuildProgress> onProgress,
        CancellationToken cancellationToken);
}
