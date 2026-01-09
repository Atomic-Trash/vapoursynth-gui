Build the VapourSynth portable distribution.

Instructions:
1. Determine plugin set from user argument (default: standard)
   - `minimal` - Core plugins only
   - `standard` - Common plugins for typical workflows
   - `full` - All available plugins

2. Run the build script:
   `powershell -ExecutionPolicy Bypass -File scripts/build/Build-Portable.ps1 -PluginSet <set>`

3. If user specifies `--clean`, add `-Clean` flag to rebuild from scratch

4. Monitor build progress and report:
   - Download progress for dependencies
   - Plugin installation status
   - Any errors or warnings
   - Final distribution size

5. After successful build, report:
   - Location of output (dist/ folder)
   - Included components
   - How to test the portable distribution
