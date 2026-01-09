# VapourSynth Studio - Configuration Reference

This document describes all configuration files and their schemas.

## Table of Contents

- [Application Settings (settings.json)](#application-settings)
- [Plugin Configuration (plugins.json)](#plugin-configuration)
- [Project Files (.vsproj)](#project-files)

---

## Application Settings

Application settings are stored in `%LocalAppData%\VapourSynthStudio\settings.json`.

### Schema

```json
{
  "OutputDirectory": "dist",
  "CacheDirectory": "build",
  "DefaultPluginSet": "standard",
  "PythonVersion": "3.12.4",
  "VapourSynthVersion": "R68",
  "CustomBins": [],
  "DefaultExportFormat": "mp4",
  "DefaultVideoCodec": "libx264",
  "DefaultAudioCodec": "aac",
  "DefaultVideoQuality": 22,
  "DefaultAudioBitrate": 192,
  "GpuPreference": "Auto",
  "MaxCacheSizeMB": 1024,
  "AutoClearCache": false,
  "RecentProjectsLimit": 10,
  "AutoSaveIntervalMinutes": 5,
  "AutoSaveEnabled": true,
  "ShowLogPanel": false,
  "TimelineZoom": 1.0,
  "ConfirmOnDelete": true,
  "WindowLeft": -1,
  "WindowTop": -1,
  "WindowWidth": 1400,
  "WindowHeight": 900,
  "WindowMaximized": false,
  "LastActivePage": "Media"
}
```

### Settings Reference

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `OutputDirectory` | string | `"dist"` | Directory where portable distribution is built |
| `CacheDirectory` | string | `"build"` | Directory for temporary build files |
| `DefaultPluginSet` | string | `"standard"` | Plugin set to use: `minimal`, `standard`, or `full` |
| `PythonVersion` | string | `"3.12.4"` | Embedded Python version |
| `VapourSynthVersion` | string | `"R68"` | VapourSynth version |
| `CustomBins` | array | `[]` | Custom media bin configurations |
| `DefaultExportFormat` | string | `"mp4"` | Default container format for exports |
| `DefaultVideoCodec` | string | `"libx264"` | Default video codec (libx264, libx265, etc.) |
| `DefaultAudioCodec` | string | `"aac"` | Default audio codec |
| `DefaultVideoQuality` | int | `22` | CRF value (0-51, lower = better quality) |
| `DefaultAudioBitrate` | int | `192` | Audio bitrate in kbps |
| `GpuPreference` | enum | `"Auto"` | GPU preference: `Auto`, `NVIDIA`, `AMD`, `Intel`, `CPU` |
| `MaxCacheSizeMB` | int | `1024` | Maximum cache size in megabytes |
| `AutoClearCache` | bool | `false` | Clear cache on application exit |
| `RecentProjectsLimit` | int | `10` | Number of recent projects to remember |
| `AutoSaveIntervalMinutes` | int | `5` | Auto-save interval (0 to disable) |
| `AutoSaveEnabled` | bool | `true` | Enable auto-save feature |
| `ShowLogPanel` | bool | `false` | Show log panel at startup |
| `TimelineZoom` | double | `1.0` | Default timeline zoom level (0.1-5.0) |
| `ConfirmOnDelete` | bool | `true` | Show confirmation dialogs for delete operations |
| `WindowLeft` | double | `-1` | Window X position (-1 = center) |
| `WindowTop` | double | `-1` | Window Y position (-1 = center) |
| `WindowWidth` | double | `1400` | Window width in pixels |
| `WindowHeight` | double | `900` | Window height in pixels |
| `WindowMaximized` | bool | `false` | Start maximized |
| `LastActivePage` | string | `"Media"` | Last active page on startup |

### GPU Preference Values

| Value | Description |
|-------|-------------|
| `Auto` | Automatically detect best GPU |
| `NVIDIA` | Prefer NVIDIA GPU (NVENC encoder) |
| `AMD` | Prefer AMD GPU (AMF encoder) |
| `Intel` | Prefer Intel GPU (QSV encoder) |
| `CPU` | Disable GPU acceleration |

### Custom Bins

Custom bins allow organizing media items into user-defined categories:

```json
{
  "CustomBins": [
    {
      "Id": "bin-001",
      "Name": "B-Roll",
      "ItemPaths": [
        "/path/to/video1.mp4",
        "/path/to/video2.mp4"
      ]
    }
  ]
}
```

---

## Plugin Configuration

Plugin configuration is stored in `plugins.json` at the project root.

### Schema

```json
{
  "$schema": "./plugins-schema.json",
  "version": "1.0.0",
  "description": "VapourSynth plugins configuration",
  "plugins": [
    {
      "name": "plugin-name",
      "description": "What the plugin does",
      "set": "standard",
      "url": "https://github.com/.../releases/...",
      "version": "1.0",
      "files": ["plugin.dll"],
      "dependencies": []
    }
  ],
  "pythonPackages": [
    {
      "name": "package-name",
      "description": "What the package does",
      "set": "standard"
    }
  ]
}
```

### Plugin Entry Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Unique plugin identifier |
| `description` | string | Yes | Human-readable description |
| `set` | string | Yes | Plugin set: `minimal`, `standard`, or `full` |
| `url` | string | Yes | Download URL (direct link to archive) |
| `version` | string | Yes | Plugin version string |
| `files` | array | Yes | DLL files to extract from archive |
| `dependencies` | array | No | Runtime dependencies (informational) |

### Plugin Sets

| Set | Description | Use Case |
|-----|-------------|----------|
| `minimal` | Essential source filters only | Basic playback |
| `standard` | Common filters for editing | General video editing |
| `full` | All available plugins | Professional workflows |

### Python Package Entry Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | PyPI package name |
| `description` | string | Yes | Human-readable description |
| `set` | string | Yes | Package set: `minimal`, `standard`, or `full` |

### Included Plugins by Set

#### Minimal Set
- `ffms2` - FFmpeg-based source filter
- `lsmashsource` - L-SMASH based source filter

#### Standard Set (includes Minimal)
- `mvtools` - Motion compensation
- `fmtconv` - Format conversion
- `znedi3` - Neural network interpolation
- `nnedi3cl` - OpenCL NNEDI3
- `bm3d` - Block-matching denoising
- `knlmeanscl` - Non-local means denoising
- `descale` - Reverse scaling
- `eedi3` - Edge directed interpolation
- `dfttest` - Frequency domain denoising
- `vsfiltermod` - Subtitle rendering
- `subtext` - libass subtitles

#### Full Set (includes Standard)
- `bm3dcuda` - CUDA BM3D
- `tcanny` - Edge detection
- `bilateral` - Bilateral filtering
- `ctmf` - Median filtering
- `retinex` - Color correction
- `cas` - Contrast adaptive sharpening
- `remap` - Frame remapping

### Adding Custom Plugins

To add a new plugin:

1. Find the plugin's GitHub releases page
2. Get the direct download URL for the latest release
3. Add entry to `plugins.json`:

```json
{
  "name": "my-plugin",
  "description": "Description of what it does",
  "set": "standard",
  "url": "https://github.com/user/repo/releases/download/v1.0/plugin.7z",
  "version": "1.0",
  "files": ["plugin.dll"],
  "dependencies": []
}
```

4. Run the build script to download and install:
```powershell
.\scripts\build\Build-Portable.ps1 -Components plugins
```

---

## Project Files

Project files use `.vsproj` extension and are JSON format.

### Schema Overview

```json
{
  "Version": "1.0",
  "Name": "Project Name",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ModifiedAt": "2024-01-01T00:00:00Z",
  "Settings": {
    "Resolution": { "Width": 1920, "Height": 1080 },
    "FrameRate": 24.0,
    "SampleRate": 48000,
    "ChannelCount": 2
  },
  "MediaItems": [...],
  "Timeline": {
    "Tracks": [...],
    "TextOverlays": [...]
  },
  "ExportSettings": {...}
}
```

### Project Settings

| Field | Type | Description |
|-------|------|-------------|
| `Resolution.Width` | int | Output width in pixels |
| `Resolution.Height` | int | Output height in pixels |
| `FrameRate` | double | Output frame rate |
| `SampleRate` | int | Audio sample rate in Hz |
| `ChannelCount` | int | Audio channels (1=mono, 2=stereo) |

### Media Item Reference

```json
{
  "Id": "unique-id",
  "FilePath": "/path/to/media.mp4",
  "Name": "Display Name",
  "Duration": 120.5,
  "Width": 1920,
  "Height": 1080,
  "FrameRate": 24.0,
  "VideoCodec": "h264",
  "AudioCodec": "aac",
  "Thumbnail": "base64-thumbnail-data"
}
```

### Timeline Track

```json
{
  "Id": "track-id",
  "Name": "V1",
  "TrackType": "Video",
  "Height": 60,
  "IsMuted": false,
  "IsSolo": false,
  "IsLocked": false,
  "Clips": [...]
}
```

### Timeline Clip

```json
{
  "Id": "clip-id",
  "MediaItemId": "media-id",
  "Name": "Clip Name",
  "StartFrame": 0,
  "DurationFrames": 720,
  "SourceInFrame": 0,
  "SourceOutFrame": 720,
  "Color": "#6366F1",
  "Effects": [...],
  "ColorGrade": {...}
}
```

### Color Grade

```json
{
  "Exposure": 0.0,
  "Contrast": 0.0,
  "Saturation": 0.0,
  "Temperature": 0.0,
  "Tint": 0.0,
  "Highlights": 0.0,
  "Shadows": 0.0,
  "Whites": 0.0,
  "Blacks": 0.0,
  "Vibrance": 0.0,
  "LiftX": 0.0,
  "LiftY": 0.0,
  "LiftMaster": 0.0,
  "GammaX": 0.0,
  "GammaY": 0.0,
  "GammaMaster": 0.0,
  "GainX": 0.0,
  "GainY": 0.0,
  "GainMaster": 0.0,
  "LutPath": "",
  "LutIntensity": 1.0
}
```

### Effect Definition

```json
{
  "Id": 1,
  "Name": "Denoise",
  "Category": "Enhancement",
  "EffectType": "Denoise",
  "IsEnabled": true,
  "Parameters": [
    {
      "Name": "Strength",
      "DisplayName": "Strength",
      "ParameterType": "Float",
      "Value": 0.5,
      "MinValue": 0.0,
      "MaxValue": 1.0
    }
  ],
  "KeyframeTracks": [...]
}
```

### Keyframe Track

```json
{
  "Id": 1,
  "ParameterName": "Strength",
  "DisplayName": "Denoise Strength",
  "IsEnabled": true,
  "Keyframes": [
    {
      "Id": 1,
      "Frame": 0,
      "ValueJson": "0.5",
      "Interpolation": "Linear"
    }
  ]
}
```

### Keyframe Interpolation Types

| Type | Description |
|------|-------------|
| `Hold` | Constant value until next keyframe |
| `Linear` | Linear interpolation |
| `EaseIn` | Accelerate into keyframe |
| `EaseOut` | Decelerate out of keyframe |
| `EaseInOut` | Smooth acceleration/deceleration |

---

## Environment Variables

The application respects the following environment variables:

| Variable | Description |
|----------|-------------|
| `VAPOURSYNTH_STUDIO_CONFIG` | Override settings file location |
| `VAPOURSYNTH_PLUGINS_PATH` | Additional plugin search path |
| `FFMPEG_PATH` | Override FFmpeg location |
| `MPV_PATH` | Override mpv library location |

---

## Logging

Application logs are stored in `%LocalAppData%\VapourSynthStudio\logs\`.

Log files follow the pattern: `vapoursynth-studio-YYYYMMDD.log`

### Log Levels

| Level | Description |
|-------|-------------|
| `Verbose` | Detailed debugging information |
| `Debug` | Debugging information |
| `Information` | General information |
| `Warning` | Warning conditions |
| `Error` | Error conditions |
| `Fatal` | Critical failures |

Configure logging verbosity in settings or via command line:
```
VapourSynthStudio.exe --log-level Debug
```
