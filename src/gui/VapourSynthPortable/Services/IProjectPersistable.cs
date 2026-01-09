using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Interface for ViewModels that can persist their state to/from a project
/// </summary>
public interface IProjectPersistable
{
    /// <summary>
    /// Exports the current state to the project model
    /// </summary>
    /// <param name="project">The project to export state to</param>
    void ExportToProject(Project project);

    /// <summary>
    /// Imports state from the project model
    /// </summary>
    /// <param name="project">The project to import state from</param>
    void ImportFromProject(Project project);
}
