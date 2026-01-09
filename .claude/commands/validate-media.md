Validate media files for compatibility with VapourSynth Studio.

## Arguments
- `<path>` - File or folder path to validate (required)

## Instructions

1. **Identify Target**
   - If path is a file: validate that single file
   - If path is a folder: find all media files recursively
   - Supported extensions: .mp4, .mkv, .avi, .mov, .webm, .m4v, .ts, .mts, .m2ts, .wmv, .flv
   - Also check audio: .mp3, .wav, .flac, .aac, .m4a, .ogg
   - Also check images: .png, .jpg, .jpeg, .tiff, .bmp, .webp

2. **Run FFprobe Analysis**
   For each media file, run:
   ```
   ffprobe -v quiet -print_format json -show_format -show_streams "<file>"
   ```

3. **Extract Key Information**
   - Container format
   - Video codec and profile
   - Audio codec
   - Resolution (width x height)
   - Frame rate (check for variable frame rate)
   - Duration
   - Color space and bit depth
   - HDR metadata if present

4. **Check for Known Issues**

   **Critical Issues (will cause problems):**
   - Variable frame rate (VFR) - VapourSynth works best with CFR
   - Unsupported codec (e.g., AV1 without proper decoder)
   - Corrupted or truncated file
   - Missing video stream
   - Encrypted content (DRM)

   **Warnings (may affect quality/performance):**
   - Unusual color space (BT.2020, Display P3) - may need conversion
   - 10-bit or higher - some filters require 8-bit
   - High resolution (4K+) - may be slow without GPU
   - Interlaced content - recommend deinterlacing
   - Non-standard frame rate (23.976 vs 24, etc.)
   - Missing audio stream (if video file)

   **Info (notable but not problematic):**
   - HDR content detected
   - Multiple audio tracks
   - Embedded subtitles
   - Chapter markers

5. **VapourSynth Filter Compatibility**
   Based on detected properties, suggest appropriate filters:
   - Interlaced → Suggest EEDI3 or NNEDI3 deinterlacing
   - VFR → Suggest using FFmpeg to convert to CFR first
   - 10-bit → Note which filters support high bit depth
   - 4K+ → Recommend GPU-accelerated filters
   - Noisy source → Suggest BM3D or KNLMeansCL

6. **Generate Report**

   **For single file:**
   ```
   Media Validation Report: video.mp4
   ═══════════════════════════════════════════════

   File Information:
   ─────────────────
   Format:      MP4 (mov,mp4,m4a,3gp,3g2,mj2)
   Duration:    01:23:45
   Size:        2.4 GB
   Bitrate:     4.2 Mbps

   Video Stream:
   ─────────────
   Codec:       H.264 (High Profile)
   Resolution:  1920x1080
   Frame Rate:  23.976 fps (constant)
   Color Space: BT.709 / 8-bit
   Scan Type:   Progressive

   Audio Stream:
   ─────────────
   Codec:       AAC
   Channels:    Stereo (2.0)
   Sample Rate: 48000 Hz

   Compatibility:     [OK]
   ─────────────────────────
   - Fully compatible with VapourSynth
   - Recommended source filter: lsmas.LWLibavSource

   Notes:
   - No issues detected
   ```

   **For folder:**
   ```
   Media Validation Report: /path/to/folder
   ═══════════════════════════════════════════════

   Scanned: 15 files

   Summary:
   ────────
   Compatible:    12 files (80%)
   Warnings:       2 files (13%)
   Errors:         1 file  (7%)

   Files with Issues:
   ──────────────────
   [WARN] video1.mp4
          Variable frame rate detected
          Fix: ffmpeg -i input.mp4 -vsync cfr -r 24 output.mp4

   [WARN] video2.mkv
          Interlaced content (1080i)
          Suggestion: Use NNEDI3 deinterlace preset

   [ERROR] corrupted.avi
           File appears truncated or corrupted
           Fix: Re-download or repair with ffmpeg

   Compatible Files:
   ─────────────────
   - file1.mp4 (1080p, H.264)
   - file2.mkv (4K, HEVC)
   ... and 10 more
   ```

7. **Provide Actionable Fixes**
   For each issue, provide:
   - Clear description of the problem
   - Impact on processing
   - Specific command or step to fix
   - Alternative workaround if applicable

## Examples

```bash
# Validate a single video file
/validate-media "C:\Videos\source.mp4"

# Validate all media in a folder
/validate-media "C:\Projects\Documentary"

# Validate with full details
/validate-media "C:\Videos" --verbose
```
