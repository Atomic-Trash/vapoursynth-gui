Run a comprehensive system diagnostic to verify the VapourSynth Studio environment.

## Instructions

1. **Check .NET Environment**
   - Run `dotnet --version` to verify .NET 8+ is installed
   - Run `dotnet --list-sdks` to show all installed SDKs
   - Report if .NET 8.0 SDK is missing

2. **Check VapourSynth Installation**
   - Look for `dist/vapoursynth/` directory
   - Check if `vspipe.exe` exists in dist/vapoursynth/
   - Run `vspipe --version` if available
   - Report if VapourSynth is not set up (suggest running Build-Portable.ps1)

3. **Check FFmpeg Installation**
   - Look for `dist/ffmpeg/` directory
   - Check if `ffmpeg.exe` exists
   - Run `ffmpeg -version` to get version info
   - Report if FFmpeg is missing

4. **Check Python Environment**
   - Look for `dist/python/` embedded Python
   - Check if `python.exe` exists
   - Report Python version if available

5. **Check GPU Acceleration**
   - Run `wmic path win32_VideoController get name` to detect GPUs
   - Check for NVIDIA GPU (NVENC support)
   - Check for AMD GPU (AMF support)
   - Check for Intel GPU (QSV support)
   - Report available hardware encoders

6. **Check Plugin Status**
   - Read `plugins.json` to get plugin definitions
   - Check `dist/vapoursynth/vapoursynth64/plugins/` for installed .dll files
   - Compare expected vs installed plugins
   - Report any missing plugins

7. **Check Application Build**
   - Run `dotnet build src/gui/VapourSynthPortable.sln --verbosity quiet`
   - Report build success or failures
   - Note any missing dependencies

8. **Check libmpv (Video Playback)**
   - Look for `dist/mpv/` directory
   - Check if `libmpv-2.dll` or `mpv-2.dll` exists
   - Report if mpv is missing (suggest running install-mpv.ps1)

9. **Generate Summary Report**
   Present a clear status table:
   ```
   Component          Status    Notes
   ─────────────────────────────────────────
   .NET 8 SDK         [OK]      8.0.xxx
   VapourSynth        [OK]      R68
   FFmpeg             [OK]      7.x
   Python             [OK]      3.12.x
   GPU (NVIDIA)       [OK]      NVENC available
   GPU (AMD)          [--]      Not detected
   Plugins            [OK]      15/15 installed
   mpv/libmpv         [!!]      Not installed
   Build              [OK]      Compiles successfully
   ```

10. **Provide Recommendations**
    - For any [!!] (error) or [--] (warning) items, provide specific fix instructions
    - Suggest next steps to resolve issues
    - Point to relevant documentation or scripts

## Example Output

```
VapourSynth Studio - System Diagnostic Report
═══════════════════════════════════════════════

Checking environment...

Component          Status    Details
─────────────────────────────────────────────────────
.NET SDK           [OK]      8.0.401
VapourSynth        [OK]      R68 (vspipe available)
FFmpeg             [OK]      7.0.2-full
Python             [OK]      3.12.4 (embedded)
NVIDIA GPU         [OK]      RTX 3080 - NVENC available
AMD GPU            [--]      Not detected
Intel GPU          [--]      Not detected
VS Plugins         [OK]      18/18 installed
libmpv             [!!]      Not found
Application        [OK]      Builds successfully

Issues Found:
─────────────
1. libmpv not installed
   Fix: Run `.\scripts\util\install-mpv.ps1`
   This enables real-time video preview in the application.

Recommendations:
─────────────────
- Install libmpv for video playback support
- All core functionality is ready to use
```
