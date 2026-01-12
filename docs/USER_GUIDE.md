# VapourSynth Studio User Guide

## Overview

VapourSynth Studio is a modern video processing application that provides a graphical interface for VapourSynth, a powerful video processing framework.

## Getting Started

### System Requirements

- Windows 10 (version 1809 or later) or Windows 11
- .NET 8.0 Runtime (included in self-contained builds)
- 4GB RAM minimum (8GB recommended)
- DirectX 11 compatible graphics

### Installation

#### Portable Version (Recommended)
1. Download the latest release from [GitHub Releases](https://github.com/Atomic-Trash/vapoursynth-gui/releases)
2. Extract the ZIP file to your preferred location
3. Run `VapourSynthPortable.exe`

#### Installer Version
1. Download the installer from [GitHub Releases](https://github.com/Atomic-Trash/vapoursynth-gui/releases)
2. Run the installer and follow the prompts
3. Launch from the Start Menu or Desktop shortcut

## Main Features

### Media Page

The Media page is your media library where you can:

- **Import Media**: Drag and drop files or use the Import button
- **Organize with Bins**: Create custom bins to organize your media
- **Filter by Type**: Use the built-in Video, Audio, and Images bins
- **Search**: Use the search bar to find media by name
- **Set as Source**: Right-click a clip to set it as the current source

### Edit Page

The Edit page provides a timeline-based editing interface:

#### Timeline Controls
- **Playback**: Space to play/pause, Home/End to jump to start/end
- **Navigation**: Left/Right arrows to step frame-by-frame
- **Markers**: Press M to add a marker, Shift+M to delete
- **Keyframes**: Press K to add a keyframe, Shift+K to remove
- **In/Out Points**: Press I to set in point, O to set out point

#### Keyboard Shortcuts
| Action | Shortcut |
|--------|----------|
| Play/Pause | Space |
| Step Backward | Left Arrow |
| Step Forward | Right Arrow |
| Go to Start | Home |
| Go to End | End |
| Add Marker | M |
| Delete Marker | Shift+M |
| Previous Marker | Ctrl+Left |
| Next Marker | Ctrl+Right |
| Add Keyframe | K |
| Remove Keyframe | Shift+K |
| Previous Keyframe | [ or Ctrl+Shift+Left |
| Next Keyframe | ] or Ctrl+Shift+Right |
| Set In Point | I |
| Set Out Point | O |
| Add Text Overlay | T |

### Color Page

The Color page provides professional color grading tools:

#### Color Wheels
- **Lift**: Adjust shadows
- **Gamma**: Adjust midtones
- **Gain**: Adjust highlights

Each wheel shows X/Y position values and has a master slider for overall brightness adjustment.

#### Adjustments
- Exposure, Contrast, Saturation
- Temperature, Tint
- Highlights, Shadows
- Whites, Blacks

#### Scopes
- **Waveform**: Shows luminance distribution
- **Parade**: Shows RGB channels separately
- **Vectorscope**: Shows color/saturation distribution
- **Histogram**: Shows tonal distribution

#### LUT Support
Load .cube LUT files to apply color looks. Adjust intensity with the LUT slider.

### Restore Page

The Restore page provides AI-powered video restoration:

#### Available Presets
- **Upscaling**: Real-ESRGAN, Anime4K, ESPCN
- **Denoising**: BM3D, NLMeans, DFTTest
- **Face Restoration**: GFPGAN, CodeFormer
- **Colorization**: DeOldify, DDColor
- **Deinterlacing**: NNEDI3, QTGMC
- **Grain**: Film grain matching
- **Stabilization**: Video stabilization
- **Interpolation**: Frame interpolation (RIFE, SVP)

#### Processing Queue
1. Configure your preset settings
2. Click "Add to Queue" to queue the job
3. Click "Process Queue" to start processing
4. Monitor progress in the queue panel

### Export Page

The Export page lets you render your project:

#### Output Settings
- Format: MP4, MKV, MOV, AVI, WebM
- Video Codec: H.264, H.265, H.264 NVENC, HEVC NVENC
- Audio Codec: AAC, MP3, FLAC, Opus
- Quality: CRF-based quality control

### Settings

Access settings via the gear icon:

#### General Settings
- Default plugin set (minimal, standard, full)
- Cache directory and size limits
- Auto-save preferences
- Recent projects limit

#### Export Defaults
- Default format, codec, and quality
- GPU acceleration preferences

## Accessibility

VapourSynth Studio supports Windows High Contrast mode:

1. Press Windows+U to open Accessibility settings
2. Enable High Contrast mode
3. VapourSynth Studio will automatically adapt colors

## Troubleshooting

### Video won't play
- Ensure libmpv-2.dll is in the application directory
- Try installing the latest mpv from https://mpv.io

### VapourSynth plugins not loading
- Check that plugins are in the correct directory
- Run the diagnostics from Help menu
- Verify plugin architecture matches (x64)

### Application crashes on startup
- Check the crash report in %APPDATA%\VapourSynthStudio\logs
- Ensure your graphics drivers are up to date
- Try running as administrator

## Getting Help

- [GitHub Issues](https://github.com/Atomic-Trash/vapoursynth-gui/issues)
- [VapourSynth Documentation](http://www.vapoursynth.com/doc/)

## License

VapourSynth Studio is open source software. See LICENSE file for details.
