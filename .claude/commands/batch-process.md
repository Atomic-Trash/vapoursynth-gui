Process multiple video files with the same restoration/enhancement preset.

## Arguments
- `<folder>` - Folder containing video files to process (required)
- `[preset]` - Restoration preset name (optional, defaults to prompting user)

## Available Presets
From RestoreViewModel.cs:
- **Denoising**: Light Denoise, Medium Denoise, Heavy Denoise, BM3D Denoise
- **Upscaling**: AI Upscale 2x, AI Upscale 4x, Lanczos Upscale
- **Deinterlacing**: NNEDI3 Deinterlace, EEDI3 Deinterlace
- **Restoration**: Film Grain Removal, Scratch Removal, VHS Restoration
- **Enhancement**: Sharpen, Contrast Enhancement, Color Enhancement
- **Film**: Film Look, Vintage Look
- **Utility**: Stabilization, Crop & Resize

## Instructions

1. **Scan Folder for Media**
   ```powershell
   Get-ChildItem -Path "<folder>" -Include *.mp4,*.mkv,*.avi,*.mov,*.webm -Recurse
   ```
   Report: "Found X video files"

2. **Validate Input Files**
   For each file, run quick validation:
   - Check file is readable
   - Verify it's a valid video (has video stream)
   - Note duration for ETA calculation
   Report any invalid files

3. **Select Preset**
   If preset not specified:
   - List available presets by category
   - Ask user to choose
   If preset specified:
   - Validate it exists
   - Show preset description

4. **Configure Output**
   - Default output folder: `<input_folder>/processed/`
   - Output format: Same as input or user-specified
   - Naming: `<original_name>_<preset>.ext`

5. **Estimate Processing Time**
   Based on:
   - Total duration of all files
   - Preset complexity (upscaling is slower)
   - GPU availability
   Provide rough estimate: "Estimated processing time: ~X hours"

6. **Create Processing Queue**
   Build a list of jobs:
   ```
   Queue (5 files):
   1. video1.mp4 → processed/video1_ai-upscale-2x.mp4
   2. video2.mkv → processed/video2_ai-upscale-2x.mp4
   ...
   ```

7. **Confirm with User**
   Show summary and ask for confirmation:
   ```
   Batch Processing Summary
   ════════════════════════
   Input folder:  C:\Videos\Raw
   Output folder: C:\Videos\Raw\processed
   Preset:        AI Upscale 2x
   Files:         5 videos (total duration: 2h 15m)
   Est. time:     ~4 hours (with GPU)

   Proceed? [Y/n]
   ```

8. **Process Files**
   For each file:
   - Show current file and progress (1/5, 2/5, etc.)
   - Run the restoration preset
   - Report completion or any errors
   - Update ETA based on actual speed

9. **Generate Completion Report**
   ```
   Batch Processing Complete
   ═════════════════════════════════════════

   Results:
   ────────
   Successful:  4/5 files
   Failed:      1/5 files
   Total time:  3h 42m
   Output size: 12.4 GB

   Completed Files:
   ────────────────
   ✓ video1.mp4 → video1_ai-upscale-2x.mp4 (2.1 GB)
   ✓ video2.mkv → video2_ai-upscale-2x.mkv (3.2 GB)
   ✓ video3.mp4 → video3_ai-upscale-2x.mp4 (1.8 GB)
   ✓ video4.avi → video4_ai-upscale-2x.avi (5.3 GB)

   Failed:
   ───────
   ✗ corrupted.mp4
     Error: Source file appears corrupted
     Suggestion: Try re-encoding source first

   Output Location:
   C:\Videos\Raw\processed\
   ```

## Example Usage

```bash
# Process all videos in folder with a preset
/batch-process "C:\Videos\OldFootage" "VHS Restoration"

# Process with interactive preset selection
/batch-process "C:\Videos\ToUpscale"

# Dry run to see what would be processed
/batch-process "C:\Videos\Raw" --dry-run
```

## Notes
- Processing uses VapourSynth with vspipe → FFmpeg pipeline
- GPU acceleration is used when available (NVENC, AMF, QSV)
- Files are processed sequentially (parallel processing may be added later)
- Original files are never modified
- Existing output files are skipped unless --overwrite is specified
