using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Current project file format version
/// </summary>
public static class ProjectVersion
{
    public const string Current = "1.1";
    public const string MinSupported = "1.0";

    public static readonly Version CurrentVersion = new(1, 1);
    public static readonly Version MinSupportedVersion = new(1, 0);
}

/// <summary>
/// Result of project compatibility check
/// </summary>
public class ProjectCompatibilityResult
{
    public bool IsCompatible { get; init; }
    public bool NeedsMigration { get; init; }
    public string ProjectVersion { get; init; } = "";
    public string CurrentVersion { get; init; } = Services.ProjectVersion.Current;
    public string? Message { get; init; }
    public List<string> MigrationSteps { get; init; } = [];
}

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
    /// Import timeline data into a Timeline object
    /// </summary>
    Timeline ImportTimeline(TimelineData data);

    /// <summary>
    /// Export media pool items to references
    /// </summary>
    List<MediaReference> ExportMediaPool(IEnumerable<MediaItem> mediaItems, string projectPath);

    /// <summary>
    /// Import media pool from references
    /// </summary>
    List<MediaItem> ImportMediaPool(List<MediaReference> references, string projectPath);

    /// <summary>
    /// Validates media file paths in a project
    /// </summary>
    MediaValidationResult ValidateMediaPaths(Project project);

    /// <summary>
    /// Attempts to relink missing media files by searching in specified directories
    /// </summary>
    /// <param name="project">The project to relink</param>
    /// <param name="searchPaths">Directories to search for missing files</param>
    /// <returns>Number of files successfully relinked</returns>
    int RelinkMissingMedia(Project project, IEnumerable<string> searchPaths);

    /// <summary>
    /// Gets the auto-save path for a project
    /// </summary>
    string GetAutoSavePath(Project project);

    /// <summary>
    /// Check if a project file is compatible with the current version
    /// </summary>
    /// <param name="filePath">Path to project file</param>
    Task<ProjectCompatibilityResult> CheckCompatibilityAsync(string filePath);

    /// <summary>
    /// Migrate a project to the current version format
    /// </summary>
    /// <param name="project">Project to migrate</param>
    /// <returns>True if migration was successful</returns>
    bool MigrateProject(Project project);

    /// <summary>
    /// Creates a backup of a project file before migration
    /// </summary>
    /// <param name="filePath">Path to project file</param>
    /// <returns>Path to backup file</returns>
    string CreateBackup(string filePath);
}
