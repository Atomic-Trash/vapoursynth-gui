# VapourSynth Studio - Service API Reference

This document provides API reference for the key services in VapourSynth Studio.

## Table of Contents

- [MediaPoolService](#mediapoolservice)
- [FFmpegService](#ffmpegservice)
- [VapourSynthService](#vapoursyntheservice)
- [ProjectService](#projectservice)
- [FrameCacheService](#framecacheservice)
- [UndoService](#undoservice)
- [ToastService](#toastservice)
- [ColorGradingService](#colorgradingservice)

---

## MediaPoolService

Centralized registry for all media items in the application.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MediaItems` | `ObservableCollection<MediaItem>` | All registered media items |
| `SelectedItem` | `MediaItem?` | Currently selected item |
| `CustomBins` | `ObservableCollection<CustomBin>` | User-defined media bins |

### Methods

```csharp
// Add media from file path
Task<MediaItem?> AddMediaAsync(string path, CancellationToken ct = default);

// Add multiple files
Task<IEnumerable<MediaItem>> AddMediaRangeAsync(
    IEnumerable<string> paths,
    CancellationToken ct = default);

// Remove media item
void RemoveMedia(MediaItem item);

// Clear all media
void Clear();

// Get media by ID
MediaItem? GetById(string id);

// Check if path already exists
bool ContainsPath(string path);
```

### Events

| Event | Args | Description |
|-------|------|-------------|
| `MediaAdded` | `MediaItem` | Fired when media is added |
| `MediaRemoved` | `MediaItem` | Fired when media is removed |
| `SelectionChanged` | `MediaItem?` | Fired when selection changes |

---

## FFmpegService

Media encoding, decoding, and analysis using FFmpeg.

### Methods

```csharp
// Probe media file metadata
Task<MediaInfo?> ProbeAsync(string path, CancellationToken ct = default);

// Encode video with options
Task<bool> EncodeAsync(
    EncodeOptions options,
    IProgress<double>? progress = null,
    CancellationToken ct = default);

// Extract single frame as image
Task<string?> ExtractFrameAsync(
    string videoPath,
    TimeSpan timestamp,
    string outputPath,
    CancellationToken ct = default);

// Generate waveform data
Task<float[]?> ExtractWaveformAsync(
    string audioPath,
    int samplesPerSecond = 100,
    CancellationToken ct = default);

// Detect hardware acceleration
Task<HardwareAcceleration> DetectHardwareAccelerationAsync();
```

### EncodeOptions

```csharp
public class EncodeOptions
{
    public string InputPath { get; set; }
    public string OutputPath { get; set; }
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int Crf { get; set; } = 22;
    public int AudioBitrate { get; set; } = 192;
    public string? HwAccel { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? Duration { get; set; }
}
```

### HardwareAcceleration

```csharp
public class HardwareAcceleration
{
    public bool NvencAvailable { get; set; }
    public bool QsvAvailable { get; set; }
    public bool AmfAvailable { get; set; }
    public bool CudaAvailable { get; set; }
    public bool CuvidAvailable { get; set; }
    public bool D3d11vaAvailable { get; set; }

    public string? GetBestEncoder();  // Returns best available encoder
    public string? GetBestHwAccel();  // Returns best HW decoder
    public string GetHwAccelArgs();   // Returns FFmpeg args string
}
```

---

## VapourSynthService

Execute VapourSynth scripts and retrieve frames.

### Methods

```csharp
// Execute script and get output info
Task<VsOutputInfo?> ExecuteScriptAsync(
    string scriptPath,
    CancellationToken ct = default);

// Get specific frame from script
Task<BitmapSource?> GetFrameAsync(
    string scriptPath,
    int frameNumber,
    CancellationToken ct = default);

// Pipe output to FFmpeg for encoding
Task<bool> PipeToFFmpegAsync(
    string scriptPath,
    string outputPath,
    EncodeOptions options,
    IProgress<double>? progress = null,
    CancellationToken ct = default);

// Get available plugins
Task<List<VapourSynthPlugin>> GetPluginsAsync();

// Check if VapourSynth is available
Task<bool> IsAvailableAsync();

// Get version information
Task<string?> GetVersionAsync();
```

### VsOutputInfo

```csharp
public class VsOutputInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int NumFrames { get; set; }
    public double Fps { get; set; }
    public string Format { get; set; }
}
```

---

## ProjectService

Save and load project files.

### Methods

```csharp
// Save project to file
Task<bool> SaveAsync(Project project, string path);

// Load project from file
Task<Project?> LoadAsync(string path);

// Export to specific format
Task<bool> ExportAsync(Project project, ExportOptions options);

// Create new empty project
Project CreateNew(string name, Resolution resolution, double frameRate);

// Validate media paths (check if files still exist)
MediaValidationResult ValidateMediaPaths(Project project);
```

### Project Model

```csharp
public class Project
{
    public string Version { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public ProjectSettings Settings { get; set; }
    public List<MediaItemData> MediaItems { get; set; }
    public TimelineData? Timeline { get; set; }
    public ExportSettings? ExportSettings { get; set; }
}
```

---

## FrameCacheService

LRU cache for video frames with prefetching support.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CachedFrameCount` | `int` | Current number of cached frames |
| `MaxCacheSize` | `int` | Maximum cache capacity |

### Methods

```csharp
// Try to get cached frame (returns null if not cached)
BitmapSource? TryGetFrame(
    string filePath,
    long frameNumber,
    int width = 320,
    int height = 180);

// Get frame (extract if not cached)
Task<BitmapSource?> GetFrameAsync(
    string filePath,
    long frameNumber,
    double frameRate,
    int width = 320,
    int height = 180,
    CancellationToken ct = default);

// Prefetch frames around position
Task PrefetchFramesAsync(
    string filePath,
    long centerFrame,
    double frameRate,
    int radius = 5,
    int width = 320,
    int height = 180,
    CancellationToken ct = default);

// Clear cache for specific file
void ClearCacheForFile(string filePath);

// Clear entire cache
void Clear();

// Invalidate all cached frames
void Invalidate();

// Get cache statistics
FrameCacheStats GetStats();
```

### FrameCacheStats

```csharp
public class FrameCacheStats
{
    public int CachedFrames { get; }
    public int MaxCacheSize { get; }
    public long MemoryEstimate { get; }
    public string MemoryEstimateFormatted { get; }
}
```

### Events

| Event | Args | Description |
|-------|------|-------------|
| `FrameCached` | `FrameCachedEventArgs` | Fired when a frame is cached |

---

## UndoService

Generic undo/redo functionality with transaction support.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CanUndo` | `bool` | True if undo is available |
| `CanRedo` | `bool` | True if redo is available |
| `UndoDescription` | `string` | Description of next undo action |
| `RedoDescription` | `string` | Description of next redo action |
| `UndoHistory` | `ReadOnlyCollection<string>` | List of undo descriptions |
| `RedoHistory` | `ReadOnlyCollection<string>` | List of redo descriptions |
| `IsInTransaction` | `bool` | True if transaction is active |

### Methods

```csharp
// Record an undoable action
void RecordAction(IUndoAction action);

// Record with delegates
void RecordAction(string description, Action undoAction, Action redoAction);

// Record property change
void RecordPropertyChange<T>(
    object target,
    string propertyName,
    T oldValue,
    T newValue,
    string? description = null);

// Begin a transaction (groups actions)
UndoTransaction BeginTransaction(string description);

// Undo last action
void Undo();

// Redo last undone action
void Redo();

// Undo multiple actions
void UndoMultiple(int count);

// Redo multiple actions
void RedoMultiple(int count);

// Clear all history
void Clear();

// Mark current state as save point
void MarkSavePoint();
```

### Extension Methods

```csharp
// Record collection add
void RecordAdd<T>(IList<T> collection, T item, string? description = null);

// Record collection remove
void RecordRemove<T>(IList<T> collection, T item, string? description = null);
```

### Events

| Event | Args | Description |
|-------|------|-------------|
| `StateChanged` | `EventArgs` | Fired when undo/redo state changes |
| `ActionExecuted` | `UndoActionEventArgs` | Fired when action is undone/redone |

### Transaction Usage

```csharp
// Group multiple operations into one undo unit
var transaction = undoService.BeginTransaction("Move clips");
try
{
    // Multiple operations...
    undoService.RecordAction("Move clip 1", undo1, redo1);
    undoService.RecordAction("Move clip 2", undo2, redo2);
    transaction.Commit();  // Commits as single undo unit
}
catch
{
    transaction.Cancel();  // Reverts all actions
    throw;
}
```

---

## ToastService

Non-blocking notification system.

### Methods

```csharp
// Show toast notification
void Show(string message, ToastType type = ToastType.Info);

// Shorthand methods
void ShowInfo(string message);
void ShowSuccess(string message);
void ShowWarning(string message);
void ShowError(string message);

// Clear pending notifications
void ClearQueue();
```

### ToastType

| Type | Default Duration | Description |
|------|-----------------|-------------|
| `Info` | 3000ms | General information |
| `Success` | 2500ms | Success confirmation |
| `Warning` | 4000ms | Warning message |
| `Error` | 5000ms | Error notification |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentMessage` | `string` | Currently displayed message |
| `CurrentType` | `ToastType` | Type of current toast |
| `IsVisible` | `bool` | Whether toast is visible |
| `PendingCount` | `int` | Number of queued toasts |

### Events

| Event | Args | Description |
|-------|------|-------------|
| `ToastShown` | `ToastEventArgs` | Fired when toast appears |
| `ToastHidden` | `EventArgs` | Fired when toast disappears |

---

## ColorGradingService

Apply color grading adjustments to images.

### Methods

```csharp
// Apply full color grade pipeline
BitmapSource? ApplyGrade(BitmapSource source, ColorGrade grade);

// Apply only LUT
BitmapSource? ApplyLutOnly(
    BitmapSource source,
    string lutPath,
    double intensity = 1.0);

// Generate VapourSynth script for grade
static string GenerateVapourSynthScript(
    ColorGrade grade,
    string inputClip = "clip");
```

### ColorGrade Model

```csharp
public class ColorGrade
{
    // Lift (shadows)
    public double LiftX { get; set; }
    public double LiftY { get; set; }
    public double LiftMaster { get; set; }

    // Gamma (midtones)
    public double GammaX { get; set; }
    public double GammaY { get; set; }
    public double GammaMaster { get; set; }

    // Gain (highlights)
    public double GainX { get; set; }
    public double GainY { get; set; }
    public double GainMaster { get; set; }

    // Global adjustments (-100 to 100)
    public double Exposure { get; set; }      // -4 to +4 stops
    public double Contrast { get; set; }
    public double Saturation { get; set; }
    public double Temperature { get; set; }
    public double Tint { get; set; }
    public double Highlights { get; set; }
    public double Shadows { get; set; }
    public double Whites { get; set; }
    public double Blacks { get; set; }
    public double Vibrance { get; set; }

    // LUT
    public string LutPath { get; set; }
    public double LutIntensity { get; set; }  // 0.0 to 1.0

    // Methods
    ColorGrade Clone();
    void Reset();
    string ToVapourSynthScript(string clipName = "clip");
}
```

---

## Usage Examples

### Import and Process Media

```csharp
// Get services
var mediaPool = serviceProvider.GetRequiredService<MediaPoolService>();
var ffmpeg = serviceProvider.GetRequiredService<FFmpegService>();

// Import media
var item = await mediaPool.AddMediaAsync("video.mp4");
if (item != null)
{
    // Probe detailed info
    var info = await ffmpeg.ProbeAsync(item.FilePath);

    // Extract thumbnail
    await ffmpeg.ExtractFrameAsync(
        item.FilePath,
        TimeSpan.FromSeconds(5),
        "thumbnail.jpg");
}
```

### Encode with Hardware Acceleration

```csharp
var ffmpeg = serviceProvider.GetRequiredService<FFmpegService>();

// Detect available hardware
var hwAccel = await ffmpeg.DetectHardwareAccelerationAsync();
var encoder = hwAccel.GetBestEncoder() ?? "libx264";

// Encode
await ffmpeg.EncodeAsync(new EncodeOptions
{
    InputPath = "input.mp4",
    OutputPath = "output.mp4",
    VideoCodec = encoder,
    Crf = 22
}, progress);
```

### Save Project with Undo

```csharp
var undo = serviceProvider.GetRequiredService<UndoService>();
var project = serviceProvider.GetRequiredService<ProjectService>();

// Make changes with undo support
var transaction = undo.BeginTransaction("Rename clips");
foreach (var clip in clips)
{
    var oldName = clip.Name;
    clip.Name = "New Name";
    undo.RecordPropertyChange(clip, nameof(clip.Name), oldName, clip.Name);
}
transaction.Commit();

// Save project
await project.SaveAsync(currentProject, "project.vsproj");
undo.MarkSavePoint();
```

---

## Error Handling

All services follow consistent error handling patterns:

1. **Return null** for expected failures (file not found, etc.)
2. **Throw exceptions** for unexpected errors
3. **Log all errors** with context
4. **Accept CancellationToken** for async operations

```csharp
try
{
    var result = await service.DoWorkAsync(ct);
    if (result == null)
    {
        // Handle expected failure
        toast.ShowWarning("Could not process file");
        return;
    }

    // Success
    toast.ShowSuccess("Operation complete");
}
catch (OperationCanceledException)
{
    // User cancelled
    toast.ShowInfo("Operation cancelled");
}
catch (Exception ex)
{
    // Unexpected error
    logger.Error(ex, "Operation failed");
    toast.ShowError($"Error: {ex.Message}");
}
```
