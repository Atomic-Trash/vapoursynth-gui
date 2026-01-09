Generate a VapourSynth script from a template.

Arguments: `$ARGUMENTS` (template name and options)

Templates available:
- `denoise` - Denoising with BM3D or KNLMeans
- `upscale` - Upscaling with various algorithms
- `deinterlace` - QTGMC or NNEDI3 deinterlacing
- `restore` - Film restoration (grain, stabilization)
- `basic` - Basic processing template

Instructions:
1. Parse arguments for template name and source file:
   - Example: `/vs-script denoise --source video.mp4`

2. For `denoise` template:
   ```python
   import vapoursynth as vs
   core = vs.core

   # Load source
   clip = core.lsmas.LWLibavSource("${source}")

   # Denoise options: bm3d (quality) or knlm (speed)
   # BM3D denoising
   clip = core.bm3dcpu.BM3D(clip, sigma=3)

   clip.set_output()
   ```

3. For `upscale` template:
   ```python
   import vapoursynth as vs
   core = vs.core

   clip = core.lsmas.LWLibavSource("${source}")

   # Upscale to target resolution
   clip = core.resize.Lanczos(clip, width=${width}, height=${height})

   clip.set_output()
   ```

4. For `deinterlace` template:
   ```python
   import vapoursynth as vs
   import havsfunc as haf
   core = vs.core

   clip = core.lsmas.LWLibavSource("${source}")

   # QTGMC deinterlacing (high quality)
   clip = haf.QTGMC(clip, Preset="Slow", TFF=True)

   clip.set_output()
   ```

5. For `restore` template:
   ```python
   import vapoursynth as vs
   core = vs.core

   clip = core.lsmas.LWLibavSource("${source}")

   # Stabilization
   clip = core.stab.Stabilize(clip)

   # Light denoise
   clip = core.bm3dcpu.BM3D(clip, sigma=1)

   # Mild sharpening
   clip = core.cas.CAS(clip, sharpness=0.5)

   clip.set_output()
   ```

6. For `basic` template:
   ```python
   import vapoursynth as vs
   core = vs.core

   # Load source video
   clip = core.lsmas.LWLibavSource("${source}")

   # Add your filters here
   # clip = core.resize.Bicubic(clip, width=1920, height=1080)

   clip.set_output()
   ```

7. After generating:
   - Save to `scripts/` directory with descriptive name
   - Suggest running `/vs-test <script>` to validate
   - Provide tips for customization
