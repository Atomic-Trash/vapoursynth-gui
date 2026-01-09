# VapourSynth Studio - Architecture Documentation

## Overview

VapourSynth Studio is a professional video editing and processing application built with WPF/.NET 8 using the MVVM (Model-View-ViewModel) pattern. It integrates VapourSynth for advanced video filtering and FFmpeg for media encoding.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           VapourSynth Studio                             │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │  MediaPage  │  │   EditPage  │  │  ColorPage  │  │ RestorePage │     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │
│         │                │                │                │            │
│  ┌──────┴──────┐  ┌──────┴──────┐  ┌──────┴──────┐  ┌──────┴──────┐     │
│  │ MediaVM     │  │  EditVM     │  │  ColorVM    │  │ RestoreVM   │     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┴──────┘     │
│         │                │                │                │            │
├─────────┴────────────────┴────────────────┴────────────────┴────────────┤
│                              Service Layer                               │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐             │
│  │ MediaPoolSvc   │  │ VapourSynthSvc │  │  FFmpegService │             │
│  │ ThumbnailSvc   │  │ EffectService  │  │  ExportService │             │
│  │ ProjectService │  │ FrameCacheSvc  │  │  ColorGradeSvc │             │
│  └────────────────┘  └────────────────┘  └────────────────┘             │
├─────────────────────────────────────────────────────────────────────────┤
│                           External Dependencies                          │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐             │
│  │  VapourSynth   │  │    FFmpeg      │  │    libmpv      │             │
│  │   (vspipe)     │  │  (ffmpeg.exe)  │  │ (mpv-2.dll)    │             │
│  └────────────────┘  └────────────────┘  └────────────────┘             │
└─────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
src/gui/VapourSynthPortable/
├── App.xaml              # Application resources, styles, DI container
├── MainWindow.xaml       # Shell with navigation
├── Pages/                # UI pages (Views)
│   ├── MediaPage.xaml    # Media pool & organization
│   ├── EditPage.xaml     # Timeline editor
│   ├── ColorPage.xaml    # Color grading
│   ├── RestorePage.xaml  # AI restoration
│   └── ExportPage.xaml   # Export queue
├── ViewModels/           # Page logic & state
│   ├── MediaViewModel.cs
│   ├── EditViewModel.cs
│   ├── ColorViewModel.cs
│   ├── RestoreViewModel.cs
│   └── ExportViewModel.cs
├── Models/               # Data structures
│   ├── MediaItem.cs
│   ├── TimelineModels.cs
│   ├── ColorGradeModels.cs
│   ├── RestoreModels.cs
│   └── ProjectModels.cs
├── Services/             # Business logic
│   ├── MediaPoolService.cs
│   ├── VapourSynthService.cs
│   ├── FFmpegService.cs
│   └── ... (20+ services)
├── Controls/             # Reusable UI components
│   ├── TimelineControl.xaml
│   ├── ColorWheelControl.xaml
│   └── VideoPlayerControl.xaml
└── Helpers/              # Utilities & converters
```

## MVVM Pattern

### View (XAML Pages)
- Declarative UI with data binding
- Minimal code-behind (initialization only)
- Uses DataTemplates for dynamic content

### ViewModel
- Implements `ObservableObject` from CommunityToolkit.Mvvm
- Uses `[ObservableProperty]` for reactive properties
- Uses `[RelayCommand]` for command binding
- Injected services via constructor

### Model
- Plain data objects or `ObservableObject` for reactive models
- No UI dependencies
- Serializable for project persistence

## Service Layer

### Core Services

| Service | Lifetime | Responsibility |
|---------|----------|----------------|
| `MediaPoolService` | Singleton | Central media registry |
| `VapourSynthService` | Transient | VapourSynth script execution |
| `FFmpegService` | Transient | Media encoding/analysis |
| `ThumbnailService` | Singleton | Thumbnail generation & cache |
| `FrameCacheService` | Singleton | Preview frame caching |
| `ProjectService` | Transient | Project save/load |
| `SettingsService` | Singleton | App configuration |
| `LoggingService` | Singleton | Serilog integration |

### Service Registration (App.xaml.cs)

```csharp
private void ConfigureServices(ServiceCollection services)
{
    // Singleton services (shared state)
    services.AddSingleton<MediaPoolService>();
    services.AddSingleton<ThumbnailService>();
    services.AddSingleton<FrameCacheService>();
    services.AddSingleton<SettingsService>();
    services.AddSingleton<LoggingService>();

    // Transient services (new instance per request)
    services.AddTransient<VapourSynthService>();
    services.AddTransient<FFmpegService>();
    services.AddTransient<ProjectService>();

    // ViewModels (transient for fresh state per navigation)
    services.AddTransient<MediaViewModel>();
    services.AddTransient<EditViewModel>();
    services.AddTransient<ColorViewModel>();
    services.AddTransient<RestoreViewModel>();
    services.AddTransient<ExportViewModel>();
}
```

## Data Flow

### Media Import Flow
```
User drops file → MediaPage → MediaViewModel
                                    │
                                    ▼
                            MediaPoolService.AddMediaAsync()
                                    │
                   ┌────────────────┼────────────────┐
                   ▼                ▼                ▼
            FFmpegService    ThumbnailService   AudioWaveform
            (probe metadata)  (generate thumb)   (extract peaks)
                   │                │                │
                   └────────────────┴────────────────┘
                                    │
                                    ▼
                              MediaItem created
                                    │
                                    ▼
                         ObservableCollection updated → UI refresh
```

### Video Processing Pipeline
```
                    ┌──────────────────────┐
                    │   VapourSynth Script │
                    │   (generated .vpy)   │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │      vspipe.exe      │
                    │ (process script to   │
                    │  raw video frames)   │
                    └──────────┬───────────┘
                               │
                               ▼ (pipe Y4M frames)
                    ┌──────────────────────┐
                    │     ffmpeg.exe       │
                    │  (encode to output   │
                    │   format/codec)      │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │    Output File       │
                    │  (.mp4, .mkv, etc)   │
                    └──────────────────────┘
```

### Color Grading Flow
```
Source Frame → ColorGradingService
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
   Color Wheels              LUT Application
   (Lift/Gamma/Gain)         (3D LUT interpolation)
        │                         │
        └────────────┬────────────┘
                     ▼
               Curves Adjustment
                     │
                     ▼
               Final Grade
```

## Key Design Decisions

### 1. Async/Await Everywhere
All potentially long-running operations are async:
- Media import
- Thumbnail generation
- Video encoding
- Frame extraction

### 2. Cancellation Token Support
All async operations accept `CancellationToken`:
```csharp
public async Task<MediaItem> ImportAsync(
    string path,
    CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    // ...
}
```

### 3. Observable Collections
UI-bound collections use `ObservableCollection<T>`:
- Automatic UI updates on add/remove
- Thread-safe access via dispatcher

### 4. Logging with Serilog
Structured logging throughout:
```csharp
_logger.Information("Imported {FileName} ({Duration})",
    item.FileName, item.Duration);
```

### 5. VapourSynth Script Generation
Dynamic script generation based on user settings:
- Presets stored as script templates
- Parameter substitution at runtime
- GPU detection for hardware filters

## Thread Safety

### UI Thread (Dispatcher)
- All UI updates must be on dispatcher thread
- Use `Application.Current.Dispatcher.InvokeAsync()`

### Background Processing
- Long operations run on thread pool
- Results marshaled to UI thread
- Progress reported via events

### Service State
- Singleton services use locks for shared state
- Immutable data structures where possible

## Error Handling

### Strategy
1. Catch exceptions at service boundary
2. Log with context
3. Return result type or throw typed exception
4. Show user-friendly toast notification

### Pattern
```csharp
try
{
    await ProcessAsync(ct);
    _logger.Information("Processing complete");
}
catch (OperationCanceledException)
{
    _logger.Information("Processing cancelled by user");
}
catch (Exception ex)
{
    _logger.Error(ex, "Processing failed");
    ToastService.Show("Processing failed: " + ex.Message, ToastType.Error);
}
```

## External Dependencies

### VapourSynth
- Location: `dist/vapoursynth/`
- Entry point: `vspipe.exe`
- Plugins: `vapoursynth64/plugins/*.dll`

### FFmpeg
- Location: `dist/ffmpeg/`
- Entry points: `ffmpeg.exe`, `ffprobe.exe`
- Used via process spawning

### libmpv
- Location: `dist/mpv/`
- Entry point: `mpv-2.dll` (P/Invoke)
- Provides video playback

## Configuration

### Application Settings
- Stored in: `%LocalAppData%\VapourSynthStudio\settings.json`
- Loaded at startup by `SettingsService`

### Plugin Configuration
- Stored in: `plugins.json` (project root)
- Defines available VapourSynth plugins

### Project Files
- Format: JSON
- Extension: `.vsproj`
- Contains: timeline, media references, color grades

## Testing

### Unit Tests
- Project: `VapourSynthPortable.Tests`
- Framework: xUnit
- Focus: Service layer, model logic

### Manual Testing
- See `docs/MANUAL_TEST_CHECKLIST.md`
- Covers all pages and workflows

## Performance Considerations

### Frame Caching
- LRU cache for preview frames
- Prefetch around playhead position
- Configurable cache size

### Thumbnail Caching
- Disk cache with expiration
- Lazy generation
- Concurrent extraction limit (4)

### GPU Acceleration
- NVENC for NVIDIA GPUs
- AMF for AMD GPUs
- QSV for Intel GPUs
- Auto-detected at runtime
