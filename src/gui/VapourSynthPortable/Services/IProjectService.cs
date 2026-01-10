using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for project file management
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Create a new empty project
    /// </summary>
    Project CreateNew();

    /// <summary>
    /// Load a project from file
    /// </summary>
    Task<Project?> LoadAsync(string filePath);

    /// <summary>
    /// Save a project to file
    /// </summary>
    Task SaveAsync(Project project, string filePath);

    /// <summary>
    /// Get the list of recent project paths
    /// </summary>
    List<string> GetRecentProjects();

    /// <summary>
    /// Add a project path to the recent projects list
    /// </summary>
    void AddToRecentProjects(string filePath);

    /// <summary>
    /// Export timeline data from a Timeline object
    /// </summary>
    TimelineData ExportTimeline(Timeline timeline);

    /// <summary>
    /// Export media pool items to references
    /// </summary>
    List<MediaReference> ExportMediaPool(IEnumerable<MediaItem> mediaItems, string projectPath);

    /// <summary>
    /// Import media pool from references
    /// </summary>
    List<MediaItem> ImportMediaPool(List<MediaReference> references, string projectPath);
}
