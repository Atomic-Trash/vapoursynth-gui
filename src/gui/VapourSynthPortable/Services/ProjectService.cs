using System.IO;
using System.Text.Json;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for managing project save/load operations
/// </summary>
public class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ILogger<ProjectService> _logger = LoggingService.GetLogger<ProjectService>();

    /// <summary>
    /// Creates a new empty project
    /// </summary>
    public Project CreateNew()
    {
        return new Project
        {
            Name = "Untitled",
            Version = ProjectVersion.Current,
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

        // Ensure directory exists before writing
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
                var clipData = new ClipData
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
                };

                // Export per-clip color grade
                if (clip.ColorGrade != null)
                {
                    clipData.ColorGrade = ColorGradeData.FromColorGrade(clip.ColorGrade);
                }

                // Export effects with keyframes
                foreach (var effect in clip.Effects)
                {
                    clipData.Effects.Add(ExportEffect(effect));
                }

                trackData.Clips.Add(clipData);
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
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse clip color '{ColorHex}', using default", clipData.ColorHex);
                    clip.Color = clipData.TrackType == TrackType.Video
                        ? Color.FromRgb(0x4A, 0x9E, 0xCF)
                        : Color.FromRgb(0x4A, 0xCF, 0x6A);
                }

                // Import per-clip color grade
                if (clipData.ColorGrade != null)
                {
                    clip.ColorGrade = clipData.ColorGrade.ToColorGrade();
                }

                // Import effects with keyframes
                foreach (var effectData in clipData.Effects)
                {
                    clip.Effects.Add(ImportEffect(effectData));
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to create relative path from '{FullPath}' to '{BasePath}'", fullPath, basePath);
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read recent projects from settings");
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load project settings, using defaults");
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

    /// <summary>
    /// Validates media file paths in a project
    /// </summary>
    public MediaValidationResult ValidateMediaPaths(Project project)
    {
        var result = new MediaValidationResult();
        var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check media pool references
        foreach (var reference in project.MediaReferences)
        {
            if (checkedPaths.Contains(reference.FilePath))
                continue;

            checkedPaths.Add(reference.FilePath);

            if (File.Exists(reference.FilePath))
            {
                result.ValidFiles.Add(reference.FilePath);
            }
            else
            {
                result.MissingFiles.Add(reference.FilePath);
            }
        }

        // Check clip source paths
        foreach (var track in project.TimelineData.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (string.IsNullOrEmpty(clip.SourcePath) || checkedPaths.Contains(clip.SourcePath))
                    continue;

                checkedPaths.Add(clip.SourcePath);

                if (File.Exists(clip.SourcePath))
                {
                    result.ValidFiles.Add(clip.SourcePath);
                }
                else
                {
                    result.MissingFiles.Add(clip.SourcePath);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Exports a TimelineEffect to serializable EffectData
    /// </summary>
    private static EffectData ExportEffect(TimelineEffect effect)
    {
        var effectData = new EffectData
        {
            Id = effect.Id,
            Name = effect.Name,
            Category = effect.Category,
            EffectType = effect.EffectType,
            IsEnabled = effect.IsEnabled,
            IsExpanded = effect.IsExpanded,
            VsNamespace = effect.VsNamespace,
            VsFunction = effect.VsFunction
        };

        // Export parameters
        foreach (var param in effect.Parameters)
        {
            effectData.Parameters.Add(new EffectParameterData
            {
                Name = param.Name,
                DisplayName = param.DisplayName,
                Description = param.Description,
                ParameterType = param.ParameterType,
                ValueJson = SerializeValue(param.Value),
                DefaultValueJson = SerializeValue(param.DefaultValue),
                MinValue = param.MinValue,
                MaxValue = param.MaxValue,
                Options = [.. param.Options]
            });
        }

        // Export keyframe tracks
        foreach (var track in effect.KeyframeTracks)
        {
            var trackData = new KeyframeTrackData
            {
                Id = track.Id,
                ParameterName = track.ParameterName,
                DisplayName = track.DisplayName,
                IsExpanded = track.IsExpanded,
                IsEnabled = track.IsEnabled
            };

            foreach (var keyframe in track.Keyframes)
            {
                trackData.Keyframes.Add(new KeyframeData
                {
                    Id = keyframe.Id,
                    Frame = keyframe.Frame,
                    ValueJson = SerializeValue(keyframe.Value),
                    Interpolation = keyframe.Interpolation,
                    EaseInX = keyframe.EaseInX,
                    EaseInY = keyframe.EaseInY,
                    EaseOutX = keyframe.EaseOutX,
                    EaseOutY = keyframe.EaseOutY
                });
            }

            effectData.KeyframeTracks.Add(trackData);
        }

        return effectData;
    }

    /// <summary>
    /// Imports EffectData to a TimelineEffect
    /// </summary>
    private static TimelineEffect ImportEffect(EffectData data)
    {
        var effect = new TimelineEffect
        {
            Name = data.Name,
            Category = data.Category,
            EffectType = data.EffectType,
            IsEnabled = data.IsEnabled,
            IsExpanded = data.IsExpanded,
            VsNamespace = data.VsNamespace,
            VsFunction = data.VsFunction
        };

        // Import parameters
        foreach (var paramData in data.Parameters)
        {
            var param = new EffectParameter
            {
                Name = paramData.Name,
                DisplayName = paramData.DisplayName,
                Description = paramData.Description,
                ParameterType = paramData.ParameterType,
                Value = DeserializeValue(paramData.ValueJson, paramData.ParameterType),
                DefaultValue = DeserializeValue(paramData.DefaultValueJson, paramData.ParameterType),
                MinValue = paramData.MinValue,
                MaxValue = paramData.MaxValue,
                Options = [.. paramData.Options]
            };
            effect.Parameters.Add(param);
        }

        // Import keyframe tracks
        foreach (var trackData in data.KeyframeTracks)
        {
            var track = new KeyframeTrack
            {
                ParameterName = trackData.ParameterName,
                DisplayName = trackData.DisplayName,
                IsExpanded = trackData.IsExpanded,
                IsEnabled = trackData.IsEnabled,
                Effect = effect,
                Parameter = effect.Parameters.FirstOrDefault(p => p.Name == trackData.ParameterName)
            };

            foreach (var keyframeData in trackData.Keyframes)
            {
                var paramType = effect.Parameters.FirstOrDefault(p => p.Name == trackData.ParameterName)?.ParameterType
                    ?? EffectParameterType.Float;

                var keyframe = new Keyframe
                {
                    Frame = keyframeData.Frame,
                    Value = DeserializeValue(keyframeData.ValueJson, paramType),
                    Interpolation = keyframeData.Interpolation,
                    EaseInX = keyframeData.EaseInX,
                    EaseInY = keyframeData.EaseInY,
                    EaseOutX = keyframeData.EaseOutX,
                    EaseOutY = keyframeData.EaseOutY
                };
                track.Keyframes.Add(keyframe);
            }

            effect.KeyframeTracks.Add(track);
        }

        return effect;
    }

    /// <summary>
    /// Serializes a parameter value to JSON string
    /// </summary>
    private static string? SerializeValue(object? value)
    {
        if (value == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Deserializes a parameter value from JSON string
    /// </summary>
    private static object? DeserializeValue(string? json, EffectParameterType paramType)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return paramType switch
            {
                EffectParameterType.Integer => JsonSerializer.Deserialize<int>(json),
                EffectParameterType.Float => JsonSerializer.Deserialize<double>(json),
                EffectParameterType.Boolean => JsonSerializer.Deserialize<bool>(json),
                EffectParameterType.String => JsonSerializer.Deserialize<string>(json),
                EffectParameterType.Enum => JsonSerializer.Deserialize<string>(json),
                _ => json
            };
        }
        catch
        {
            return json;
        }
    }
    /// <summary>
    /// Attempts to relink missing media files by searching in specified directories
    /// </summary>
    public int RelinkMissingMedia(Project project, IEnumerable<string> searchPaths)
    {
        var relinkedCount = 0;
        var searchPathsList = searchPaths.ToList();

        // Build a map of filename to potential paths
        var fileMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var searchPath in searchPathsList)
        {
            if (!Directory.Exists(searchPath))
                continue;

            try
            {
                foreach (var file in Directory.EnumerateFiles(searchPath, "*.*", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    if (!fileMap.ContainsKey(fileName))
                        fileMap[fileName] = [];
                    fileMap[fileName].Add(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching directory {SearchPath}", searchPath);
            }
        }

        // Relink media references
        foreach (var reference in project.MediaReferences)
        {
            if (File.Exists(reference.FilePath))
                continue;

            var fileName = Path.GetFileName(reference.FilePath);
            if (fileMap.TryGetValue(fileName, out var candidates))
            {
                // Prefer exact filename match, then any match
                var newPath = candidates.FirstOrDefault(c =>
                    Path.GetFileName(c).Equals(fileName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(newPath))
                {
                    _logger.LogInformation("Relinked media: {OldPath} -> {NewPath}", reference.FilePath, newPath);
                    reference.FilePath = newPath;
                    relinkedCount++;
                }
            }
        }

        // Relink clip source paths
        foreach (var track in project.TimelineData.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (string.IsNullOrEmpty(clip.SourcePath) || File.Exists(clip.SourcePath))
                    continue;

                var fileName = Path.GetFileName(clip.SourcePath);
                if (fileMap.TryGetValue(fileName, out var candidates))
                {
                    var newPath = candidates.FirstOrDefault(c =>
                        Path.GetFileName(c).Equals(fileName, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(newPath))
                    {
                        _logger.LogInformation("Relinked clip source: {OldPath} -> {NewPath}", clip.SourcePath, newPath);
                        clip.SourcePath = newPath;
                        relinkedCount++;
                    }
                }
            }
        }

        return relinkedCount;
    }

    /// <summary>
    /// Check if a project file is compatible with the current version
    /// </summary>
    public async Task<ProjectCompatibilityResult> CheckCompatibilityAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new ProjectCompatibilityResult
            {
                IsCompatible = false,
                Message = "Project file not found"
            };
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            using var doc = JsonDocument.Parse(json);

            var versionString = "1.0"; // Default for old projects without version
            if (doc.RootElement.TryGetProperty("version", out var versionElement))
            {
                versionString = versionElement.GetString() ?? "1.0";
            }

            if (!System.Version.TryParse(versionString, out var projectVersion))
            {
                // Try to parse as simple version like "1.0" or "1.1"
                projectVersion = new System.Version(1, 0);
            }

            var migrationSteps = new List<string>();

            // Check if too old
            if (projectVersion < ProjectVersion.MinSupportedVersion)
            {
                return new ProjectCompatibilityResult
                {
                    IsCompatible = false,
                    ProjectVersion = versionString,
                    Message = $"Project version {versionString} is too old. Minimum supported version is {ProjectVersion.MinSupported}."
                };
            }

            // Check if needs migration
            if (projectVersion < ProjectVersion.CurrentVersion)
            {
                // Determine migration steps based on version
                if (projectVersion < new System.Version(1, 1))
                {
                    migrationSteps.Add("Update version format to 1.1");
                    migrationSteps.Add("Add keyframe easing properties to effects");
                }

                return new ProjectCompatibilityResult
                {
                    IsCompatible = true,
                    NeedsMigration = true,
                    ProjectVersion = versionString,
                    Message = $"Project version {versionString} can be migrated to {ProjectVersion.Current}.",
                    MigrationSteps = migrationSteps
                };
            }

            // Check if newer than current (created by future version)
            if (projectVersion > ProjectVersion.CurrentVersion)
            {
                return new ProjectCompatibilityResult
                {
                    IsCompatible = false,
                    ProjectVersion = versionString,
                    Message = $"Project was created with a newer version ({versionString}). Please update VapourSynth Studio."
                };
            }

            return new ProjectCompatibilityResult
            {
                IsCompatible = true,
                NeedsMigration = false,
                ProjectVersion = versionString,
                Message = "Project is compatible"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking project compatibility for {FilePath}", filePath);
            return new ProjectCompatibilityResult
            {
                IsCompatible = false,
                Message = $"Error reading project file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Migrate a project to the current version format
    /// </summary>
    public bool MigrateProject(Project project)
    {
        if (string.IsNullOrEmpty(project.Version))
            project.Version = "1.0";

        if (!System.Version.TryParse(project.Version, out var currentVersion))
            currentVersion = new System.Version(1, 0);

        try
        {
            // Migrate from 1.0 to 1.1
            if (currentVersion < new System.Version(1, 1))
            {
                _logger.LogInformation("Migrating project from {OldVersion} to 1.1", project.Version);

                // Ensure all keyframe tracks have proper easing values
                foreach (var track in project.TimelineData.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        foreach (var effect in clip.Effects)
                        {
                            foreach (var keyframeTrack in effect.KeyframeTracks)
                            {
                                foreach (var keyframe in keyframeTrack.Keyframes)
                                {
                                    // Set default bezier easing if not set
                                    if (keyframe.EaseInX == 0 && keyframe.EaseInY == 0)
                                    {
                                        keyframe.EaseInX = 0.25;
                                        keyframe.EaseInY = 0.25;
                                    }
                                    if (keyframe.EaseOutX == 0 && keyframe.EaseOutY == 0)
                                    {
                                        keyframe.EaseOutX = 0.75;
                                        keyframe.EaseOutY = 0.75;
                                    }
                                }
                            }
                        }
                    }
                }

                currentVersion = new System.Version(1, 1);
            }

            // Future migrations would go here...

            project.Version = ProjectVersion.Current;
            project.MarkDirty();
            _logger.LogInformation("Project migrated to version {NewVersion}", project.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating project");
            return false;
        }
    }

    /// <summary>
    /// Creates a backup of a project file before migration
    /// </summary>
    public string CreateBackup(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project file not found", filePath);

        var dir = Path.GetDirectoryName(filePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(dir, $"{name}_backup_{timestamp}{ext}");

        File.Copy(filePath, backupPath, overwrite: false);
        _logger.LogInformation("Created project backup: {BackupPath}", backupPath);

        return backupPath;
    }
}

internal class ProjectAppSettings
{
    public List<string> RecentProjects { get; set; } = [];
    public string LastProjectPath { get; set; } = "";
    public string DefaultExportPath { get; set; } = "";
}

/// <summary>
/// Result of validating media paths in a project
/// </summary>
public class MediaValidationResult
{
    public bool IsValid => MissingFiles.Count == 0;
    public List<string> MissingFiles { get; set; } = [];
    public List<string> ValidFiles { get; set; } = [];
}
