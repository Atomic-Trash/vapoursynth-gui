using System.IO;
using System.Text.Json;
using System.Windows.Media;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for managing project save/load operations
/// </summary>
public class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new empty project
    /// </summary>
    public Project CreateNew()
    {
        return new Project
        {
            Name = "Untitled",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            Settings = new ProjectSettings(),
            TimelineData = new TimelineData
            {
                Tracks =
                [
                    new TrackData { Id = 1, Name = "V1", TrackType = TrackType.Video, Height = 60 },
                    new TrackData { Id = 2, Name = "V2", TrackType = TrackType.Video, Height = 60 },
                    new TrackData { Id = 3, Name = "A1", TrackType = TrackType.Audio, Height = 40 },
                    new TrackData { Id = 4, Name = "A2", TrackType = TrackType.Audio, Height = 40 }
                ]
            }
        };
    }

    /// <summary>
    /// Saves a project to file
    /// </summary>
    public async Task SaveAsync(Project project, string filePath)
    {
        project.FilePath = filePath;
        project.ModifiedDate = DateTime.Now;

        var json = JsonSerializer.Serialize(project, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        project.MarkClean();
    }

    /// <summary>
    /// Loads a project from file
    /// </summary>
    public async Task<Project?> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<Project>(json, JsonOptions);

        if (project != null)
        {
            project.FilePath = filePath;
            project.MarkClean();
        }

        return project;
    }

    /// <summary>
    /// Exports timeline data from a Timeline object
    /// </summary>
    public TimelineData ExportTimeline(Timeline timeline)
    {
        var data = new TimelineData
        {
            FrameRate = timeline.FrameRate,
            PlayheadFrame = timeline.PlayheadFrame,
            InPoint = timeline.InPoint,
            OutPoint = timeline.OutPoint,
            Zoom = timeline.Zoom
        };

        foreach (var track in timeline.Tracks)
        {
            var trackData = new TrackData
            {
                Id = track.Id,
                Name = track.Name,
                TrackType = track.TrackType,
                IsVisible = track.IsVisible,
                IsMuted = track.IsMuted,
                IsLocked = track.IsLocked,
                IsSolo = track.IsSolo,
                Height = track.Height,
                Volume = track.Volume
            };

            foreach (var clip in track.Clips)
            {
                trackData.Clips.Add(new ClipData
                {
                    Id = clip.Id,
                    Name = clip.Name,
                    SourcePath = clip.SourcePath,
                    TrackType = clip.TrackType,
                    StartFrame = clip.StartFrame,
                    EndFrame = clip.EndFrame,
                    SourceInFrame = clip.SourceInFrame,
                    SourceOutFrame = clip.SourceOutFrame,
                    SourceDurationFrames = clip.SourceDurationFrames,
                    FrameRate = clip.FrameRate,
                    IsMuted = clip.IsMuted,
                    IsLocked = clip.IsLocked,
                    ColorHex = clip.Color.ToString(),
                    Volume = clip.Volume
                });
            }

            foreach (var transition in track.Transitions)
            {
                trackData.Transitions.Add(new TransitionData
                {
                    Id = transition.Id,
                    Name = transition.Name,
                    TransitionType = transition.TransitionType,
                    DurationFrames = transition.DurationFrames,
                    StartFrame = transition.StartFrame,
                    ClipAId = transition.ClipA?.Id ?? 0,
                    ClipBId = transition.ClipB?.Id ?? 0,
                    WipeDirection = transition.WipeDirection,
                    Easing = transition.Easing
                });
            }

            data.Tracks.Add(trackData);
        }

        return data;
    }

    /// <summary>
    /// Imports timeline data into a Timeline object
    /// </summary>
    public Timeline ImportTimeline(TimelineData data)
    {
        var timeline = new Timeline
        {
            FrameRate = data.FrameRate,
            PlayheadFrame = data.PlayheadFrame,
            InPoint = data.InPoint,
            OutPoint = data.OutPoint,
            Zoom = data.Zoom
        };

        // Create a dictionary to map clip IDs for transition references
        var clipMap = new Dictionary<int, TimelineClip>();

        foreach (var trackData in data.Tracks)
        {
            var track = new TimelineTrack
            {
                Name = trackData.Name,
                TrackType = trackData.TrackType,
                IsVisible = trackData.IsVisible,
                IsMuted = trackData.IsMuted,
                IsLocked = trackData.IsLocked,
                IsSolo = trackData.IsSolo,
                Height = trackData.Height,
                Volume = trackData.Volume
            };

            foreach (var clipData in trackData.Clips)
            {
                var clip = new TimelineClip
                {
                    Name = clipData.Name,
                    SourcePath = clipData.SourcePath,
                    TrackType = clipData.TrackType,
                    StartFrame = clipData.StartFrame,
                    EndFrame = clipData.EndFrame,
                    SourceInFrame = clipData.SourceInFrame,
                    SourceOutFrame = clipData.SourceOutFrame,
                    SourceDurationFrames = clipData.SourceDurationFrames,
                    FrameRate = clipData.FrameRate,
                    IsMuted = clipData.IsMuted,
                    IsLocked = clipData.IsLocked,
                    Volume = clipData.Volume
                };

                // Parse color
                try
                {
                    clip.Color = (Color)ColorConverter.ConvertFromString(clipData.ColorHex);
                }
                catch
                {
                    clip.Color = clipData.TrackType == TrackType.Video
                        ? Color.FromRgb(0x4A, 0x9E, 0xCF)
                        : Color.FromRgb(0x4A, 0xCF, 0x6A);
                }

                track.Clips.Add(clip);
                clipMap[clipData.Id] = clip;
            }

            // Import transitions with clip references
            foreach (var transitionData in trackData.Transitions)
            {
                var transition = new TimelineTransition
                {
                    Name = transitionData.Name,
                    TransitionType = transitionData.TransitionType,
                    DurationFrames = transitionData.DurationFrames,
                    StartFrame = transitionData.StartFrame,
                    WipeDirection = transitionData.WipeDirection,
                    Easing = transitionData.Easing
                };

                if (clipMap.TryGetValue(transitionData.ClipAId, out var clipA))
                    transition.ClipA = clipA;
                if (clipMap.TryGetValue(transitionData.ClipBId, out var clipB))
                    transition.ClipB = clipB;

                track.Transitions.Add(transition);
            }

            timeline.Tracks.Add(track);
        }

        return timeline;
    }

    /// <summary>
    /// Exports media items to media references
    /// </summary>
    public List<MediaReference> ExportMediaPool(IEnumerable<MediaItem> mediaItems, string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var references = new List<MediaReference>();

        foreach (var item in mediaItems)
        {
            var reference = new MediaReference
            {
                Id = Guid.NewGuid().ToString(),
                Name = item.Name,
                FilePath = item.FilePath,
                RelativePath = GetRelativePath(projectDir, item.FilePath),
                MediaType = item.MediaType,
                Duration = item.Duration,
                Width = item.Width,
                Height = item.Height,
                FrameRate = item.FrameRate,
                Codec = item.Codec,
                FileSize = item.FileSize,
                DateImported = item.DateModified
            };
            references.Add(reference);
        }

        return references;
    }

    /// <summary>
    /// Imports media references to media items
    /// </summary>
    public List<MediaItem> ImportMediaPool(List<MediaReference> references, string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var items = new List<MediaItem>();

        foreach (var reference in references)
        {
            // Try to resolve the file path
            var filePath = reference.FilePath;
            if (!File.Exists(filePath) && !string.IsNullOrEmpty(reference.RelativePath))
            {
                var absolutePath = Path.GetFullPath(Path.Combine(projectDir, reference.RelativePath));
                if (File.Exists(absolutePath))
                    filePath = absolutePath;
            }

            var item = new MediaItem
            {
                Name = reference.Name,
                FilePath = filePath,
                MediaType = reference.MediaType,
                Duration = reference.Duration,
                Width = reference.Width,
                Height = reference.Height,
                FrameRate = reference.FrameRate,
                Codec = reference.Codec,
                FileSize = reference.FileSize,
                DateModified = reference.DateImported
            };
            items.Add(item);
        }

        return items;
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
            return fullPath;

        try
        {
            var baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? basePath
                : basePath + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            return fullPath;
        }
    }

    /// <summary>
    /// Gets the auto-save path for a project
    /// </summary>
    public string GetAutoSavePath(Project project)
    {
        if (string.IsNullOrEmpty(project.FilePath))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "VapourSynthStudio", "autosave");
            Directory.CreateDirectory(tempDir);
            return Path.Combine(tempDir, $"autosave_{project.CreatedDate:yyyyMMdd_HHmmss}.vsproj");
        }

        var dir = Path.GetDirectoryName(project.FilePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(project.FilePath);
        return Path.Combine(dir, $".{name}_autosave.vsproj");
    }

    /// <summary>
    /// Gets recent projects from settings
    /// </summary>
    public List<string> GetRecentProjects()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
            return [];

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<ProjectAppSettings>(json);
            return settings?.RecentProjects ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Adds a project to recent projects list
    /// </summary>
    public void AddToRecentProjects(string projectPath)
    {
        var settings = LoadSettings();
        settings.RecentProjects.Remove(projectPath);
        settings.RecentProjects.Insert(0, projectPath);

        // Keep only last 10 projects
        if (settings.RecentProjects.Count > 10)
            settings.RecentProjects = settings.RecentProjects.Take(10).ToList();

        SaveSettings(settings);
    }

    private ProjectAppSettings LoadSettings()
    {
        var settingsPath = GetSettingsPath();
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<ProjectAppSettings>(json) ?? new ProjectAppSettings();
            }
            catch
            {
                return new ProjectAppSettings();
            }
        }
        return new ProjectAppSettings();
    }

    private void SaveSettings(ProjectAppSettings settings)
    {
        var settingsPath = GetSettingsPath();
        var dir = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "VapourSynthStudio", "settings.json");
    }
}

internal class ProjectAppSettings
{
    public List<string> RecentProjects { get; set; } = [];
    public string LastProjectPath { get; set; } = "";
    public string DefaultExportPath { get; set; } = "";
}
