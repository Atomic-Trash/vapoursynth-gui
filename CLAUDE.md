# VapourSynth Portable - Claude Code Context

## Project Overview

This project creates a portable VapourSynth distribution for Windows 11. It bundles VapourSynth, embedded Python, and popular plugins into a self-contained package that runs without installation.

## Key Technologies

- **VapourSynth**: Video processing framework (Python-scriptable)
- **Python 3.12**: Embedded distribution for portability
- **PowerShell**: Build and utility scripts
- **Windows 11 x64**: Target platform

## Project Structure

```
vapoursynth-portable/
├── scripts/                    # All scripts organized by purpose
│   ├── build/
│   │   ├── Build-Portable.ps1  # Main build script
│   │   └── Check-Updates.ps1   # Dependency checker
│   ├── util/
│   │   ├── VSPortable.ps1      # PowerShell launcher
│   │   ├── install-mpv.ps1     # MPV player installer
│   │   └── Launch-VapourSynth.bat  # Batch launcher
│   ├── screenshots/            # Screenshot automation
│   └── test/                   # Test scripts
├── src/gui/                    # WPF application source
│   └── VapourSynthPortable/    # Main app project
├── config/                     # Configuration files
│   └── plugins.json            # Plugin definitions
├── dist/                       # Output distribution
│   ├── python/                 # Embedded Python
│   ├── vapoursynth/            # VS core files
│   ├── plugins/                # VS plugins (.dll)
│   └── scripts/                # Python scripts (.vpy)
└── docs/                       # Documentation
```

## Common Tasks

### Adding a New Plugin

1. Find the plugin's GitHub releases page
2. Add entry to `config/plugins.json`:
   ```json
   {
     "name": "plugin-name",
     "description": "What it does",
     "set": "standard",
     "url": "https://github.com/.../releases/download/.../plugin.zip",
     "version": "vX.Y",
     "files": ["plugin.dll"]
   }
   ```
3. Run `.\scripts\build\Build-Portable.ps1 -Components plugins`

### Creating VapourSynth Scripts

VapourSynth scripts use `.vpy` extension and are Python files:

```python
import vapoursynth as vs
core = vs.core

# Load video
clip = core.lsmas.LWLibavSource("video.mp4")

# Apply filters
clip = core.std.Crop(clip, left=2, right=2)
clip = core.resize.Lanczos(clip, width=1920, height=1080)

# Output
clip.set_output()
```

### Common Plugin APIs

- **Source filters**: `core.lsmas.LWLibavSource()`, `core.ffms2.Source()`
- **Resize**: `core.resize.Bicubic()`, `core.resize.Lanczos()`
- **Denoise**: `core.bm3d.VAggregate()`, `core.knlm.KNLMeansCL()`
- **Deinterlace**: `core.nnedi3.nnedi3()`, `core.eedi3.eedi3()`

## Build System Notes

- PowerShell 5.1+ required (7+ recommended)
- Build downloads ~200-500MB depending on plugin set
- Plugin sets: `minimal`, `standard` (default), `full`
- Use `-Clean` flag to rebuild from scratch

## Troubleshooting

### Plugin Won't Load
1. Check architecture (must be x64)
2. Run `.\scripts\build\Check-Updates.ps1`
3. Ensure VC++ Runtime is installed
4. Check VapourSynth version compatibility

### Python Import Errors
1. Verify PYTHONPATH includes `dist/vapoursynth`
2. Use bundled Python, not system Python
3. Check `python*._pth` file has correct paths

## Claude Code Commands

This project includes custom slash commands for common workflows:

### Build & Test
- `/build` - Build the application (--debug, --clean flags)
- `/test` - Run unit tests (--filter flag)
- `/run` - Build and launch the app
- `/build-portable` - Build VapourSynth distribution (minimal|standard|full)

### Git Workflow
- `/commit` - Create conventional commit (feat, fix, docs, etc.)
- `/pr` - Create pull request with structured template
- `/release` - Prepare a release (major|minor|patch, --dry-run)

### Documentation
- `/docs architecture` - Generate architecture documentation
- `/docs readme` - Update README with current features
- `/docs changelog` - Generate changelog from commits
- `/check-updates` - Check for NuGet and plugin updates

## Resources

- [VapourSynth Docs](http://www.vapoursynth.com/doc/)
- [Plugin Database](https://vsdb.top/)
- [VapourSynth GitHub](https://github.com/vapoursynth/vapoursynth)
