# VapourSynth Portable

A fully portable VapourSynth distribution for Windows 11 with embedded Python, plugins, and a graphical interface for easy management.

## Features

- **Portable** - No installation required, runs from any directory
- **Complete** - Includes Python 3.12, VapourSynth R68, and 20+ plugins
- **GUI Builder** - WPF application for easy builds (optional)
- **Update Checker** - Automatically check for plugin updates
- **Script Templates** - Ready-to-use VapourSynth script examples
- **VS Code Integration** - Workspace configuration included

## Project Structure

```
VapourSynth GUI/
├── Build-Portable.ps1      # Main build script
├── Check-Updates.ps1       # Plugin update checker
├── plugins.json            # Plugin definitions
├── templates/              # VapourSynth script templates
│   ├── basic-source.vpy
│   ├── denoise-bm3d.vpy
│   ├── upscale-nnedi3.vpy
│   ├── comparison.vpy
│   └── batch-process.vpy
├── src/gui/                # WPF GUI application
│   └── VapourSynthPortable/
├── build/                  # Downloaded files (cache)
└── dist/                   # Built distribution
    ├── python/             # Embedded Python 3.12
    ├── vapoursynth/        # VapourSynth core
    ├── plugins/            # VS plugins (.dll)
    ├── scripts/            # User scripts
    ├── templates/          # Script templates
    └── presets/            # User presets
```

## Requirements

- Windows 11 (x64)
- PowerShell 5.1+ (7+ recommended)
- Internet connection (for initial build)
- ~500MB disk space

## Quick Start

### Using PowerShell (Recommended)

```powershell
# Build with default settings (standard plugin set)
.\Build-Portable.ps1

# Build with full plugin set
.\Build-Portable.ps1 -PluginSet full

# Clean rebuild
.\Build-Portable.ps1 -Clean
```

### Using the GUI

```powershell
# Build and run the GUI (requires .NET 8 SDK)
cd src/gui
dotnet run
```

## Build Options

| Parameter | Values | Description |
|-----------|--------|-------------|
| `-PluginSet` | minimal, standard, full | Plugin collection to install |
| `-Components` | python, vapoursynth, plugins, packages, templates, launcher, vscode | Specific components to build |
| `-Clean` | switch | Remove existing build before starting |

### Plugin Sets

- **Minimal** (2 plugins): ffms2, lsmashsource - Basic source filters
- **Standard** (12 plugins): Adds mvtools, fmtconv, znedi3, bm3d, knlmeanscl, descale, eedi3, and more
- **Full** (20 plugins): Adds CUDA filters, advanced denoising, and specialized tools

## After Building

```batch
# Start VapourSynth environment
Launch-VapourSynth.bat

# Or with PowerShell (more options)
.\VSPortable.ps1 -Info          # Show version info
.\VSPortable.ps1 -ListPlugins   # List loaded plugins
.\VSPortable.ps1 -Shell         # Python REPL
.\VSPortable.ps1 script.vpy     # Run a script
```

## Check for Updates

```powershell
# Check all components
.\Check-Updates.ps1

# Check plugins only and apply updates
.\Check-Updates.ps1 -Plugins -Apply
```

## Script Templates

The `templates/` directory contains example scripts:

| Template | Description |
|----------|-------------|
| `basic-source.vpy` | Load video and output |
| `denoise-bm3d.vpy` | BM3D denoising example |
| `upscale-nnedi3.vpy` | NNEDI3 upscaling |
| `comparison.vpy` | Side-by-side comparison |
| `batch-process.vpy` | Process multiple files |

## Components Included

### Core
- **Python 3.12.4** (embedded)
- **VapourSynth R68** (portable)

### Standard Plugins
| Plugin | Description |
|--------|-------------|
| ffms2 | FFmpeg-based source filter |
| lsmashsource | L-SMASH source filter |
| mvtools | Motion compensation |
| fmtconv | Format conversion |
| znedi3 | Neural network interpolation |
| nnedi3cl | OpenCL NNEDI3 |
| bm3d | Block-matching denoising |
| knlmeanscl | OpenCL denoising |
| descale | Reverse scaling |
| eedi3 | Edge interpolation |
| dfttest | Frequency denoiser |
| vsfiltermod | Subtitle rendering |
| subtext | libass subtitles |

### Python Packages
- vapoursynth, vsutil (minimal)
- havsfunc, mvsfunc, nnedi3-resample (standard)
- vsdenoise, vsdehalo, vsaa (full)

## GUI Application

The WPF GUI provides:
- Visual build configuration
- Plugin management
- Real-time build log
- Update checking

### Building the GUI

```powershell
cd src/gui
dotnet build
dotnet run
```

Requirements: .NET 8 SDK

## Configuration

### plugins.json

Add custom plugins:

```json
{
  "plugins": [
    {
      "name": "myplugin",
      "description": "Description",
      "set": "standard",
      "url": "https://github.com/.../releases/download/.../plugin.zip",
      "version": "1.0",
      "files": ["myplugin.dll"]
    }
  ]
}
```

### Environment Variables

Set automatically by launchers:
- `VAPOURSYNTH_PORTABLE` - Root directory
- `PYTHONPATH` - Python and VS modules
- `VAPOURSYNTH_PLUGINS` - Plugin directory
- `PATH` - Includes Python and VS

## Troubleshooting

### Build Fails

1. Check the build log in `build/build-*.log`
2. Verify internet connection
3. Try with `-Clean` flag
4. Check PowerShell execution policy

### Plugin Not Loading

1. Verify architecture (must be x64)
2. Check for missing dependencies (VC++ Runtime, CUDA, OpenCL)
3. Run `.\VSPortable.ps1 -ListPlugins` to see loaded plugins
4. Check VapourSynth console output

### Python Import Errors

1. Use the bundled Python only
2. Verify `dist/python/python*._pth` includes correct paths
3. Check `PYTHONPATH` environment variable

## Development

### Adding Plugins

1. Find the plugin's GitHub releases
2. Add entry to `plugins.json`
3. Run `.\Build-Portable.ps1 -Components plugins`

### Creating Scripts

1. Copy a template from `templates/`
2. Edit in VS Code (Python extension recommended)
3. Run with `.\VSPortable.ps1 yourscript.vpy`

## License

This is a packaging tool. Individual components have their own licenses:
- VapourSynth: LGPL
- Python: PSF License
- Plugins: Various (see individual licenses)

## Resources

- [VapourSynth Documentation](http://www.vapoursynth.com/doc/)
- [VapourSynth GitHub](https://github.com/vapoursynth/vapoursynth)
- [Plugin Database](https://vsdb.top/)
