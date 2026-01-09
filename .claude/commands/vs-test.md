Test VapourSynth script execution.

Arguments: `$ARGUMENTS` (optional: path to .vpy script)

Instructions:
1. Locate VapourSynth installation:
   - Check `dist/vapoursynth/` for portable installation
   - Check system PATH for vspipe
   - If not found, suggest running `/build-portable`

2. If script path provided in `$ARGUMENTS`:
   - Verify script file exists
   - Run: `vspipe --info "$ARGUMENTS"`
   - Report: resolution, frame count, fps, format

3. If no script path provided:
   - Create a minimal test script in temp directory:
   ```python
   import vapoursynth as vs
   core = vs.core
   # Create blank clip for testing
   clip = core.std.BlankClip(width=1920, height=1080, length=100)
   clip.set_output()
   ```
   - Run the test script with vspipe
   - Report success/failure

4. Test plugin availability:
   ```powershell
   vspipe --pluginlist
   ```
   - Categorize plugins by type (source, filter, output)
   - Highlight commonly used plugins (lsmas, ffms2, resize, etc.)

5. Report any errors with troubleshooting suggestions:
   - Missing dependencies → Check VC++ Runtime
   - Plugin load failures → Check architecture (x64)
   - Python import errors → Check PYTHONPATH configuration

6. Performance test (if `--benchmark` flag in arguments):
   - Process 100 frames and report:
     - Total time
     - FPS achieved
     - Memory usage estimate
