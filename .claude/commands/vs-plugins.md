List installed VapourSynth plugins and their versions.

Instructions:
1. Locate VapourSynth installation:
   - Check `dist/vapoursynth/` for portable installation
   - Check `dist/plugins/` for plugin DLLs
   - Check system installation if portable not found

2. Get plugin list from vspipe:
   ```powershell
   vspipe --pluginlist
   ```

3. Parse and categorize plugins:

**Source Filters:**
- lsmas (L-SMASH Works) - Modern container/codec support
- ffms2 (FFmpegSource) - FFmpeg-based source
- dgdecodenv - DGDecNV CUDA decoder

**Resize/Scaling:**
- resize (built-in) - Multiple algorithms
- nnedi3 - Neural network upscaling
- znedi3 - Optimized nnedi3

**Denoising:**
- bm3d/bm3dcpu/bm3dcuda - BM3D denoiser
- knlm - KNLMeansCL (OpenCL)
- dfttest - FFT-based denoiser

**Deinterlacing:**
- nnedi3 - Field interpolation
- eedi3 - Edge-directed interpolation
- vivtc - Inverse telecine

**Sharpening/Enhancement:**
- cas - Contrast Adaptive Sharpening
- unsharp - Unsharp mask

**Color/Grading:**
- colorbars - Test patterns
- hist - Histogram analysis

**Misc:**
- std (built-in) - Standard library
- text - Text overlay

4. For each plugin, report:
   - Plugin namespace
   - Version (if available)
   - Functions provided
   - Status (loaded/error)

5. Compare against `config/plugins.json`:
   - Show plugins defined but not installed
   - Show installed but not in config
   - Suggest updates if newer versions available

6. Check for common issues:
   - Architecture mismatches (x86 vs x64)
   - Missing VC++ Runtime dependencies
   - Python module availability
