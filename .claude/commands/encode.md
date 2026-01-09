Encode the current project or a specific source file.

Arguments: `$ARGUMENTS` (optional: source file path or preset name)

Instructions:
1. Parse arguments:
   - If file path: encode that specific file
   - If preset name: use that encoding preset
   - If empty: prompt for source and settings

2. Available presets:
   - `web` - H.264, 1080p, 8Mbps, AAC 192kbps
   - `archive` - H.265, source resolution, CRF 18, FLAC
   - `preview` - H.264, 720p, 2Mbps, fast encode
   - `social` - H.264, 1080p, 10Mbps, optimized for streaming
   - `prores` - ProRes 422, source resolution, professional

3. Gather encoding parameters:
   ```
   Source: [file path]
   Output: [auto-generated or specified]
   Codec: [h264/h265/prores/vp9/av1]
   Resolution: [source/720p/1080p/4k]
   Bitrate/CRF: [value]
   Audio: [aac/flac/copy]
   ```

4. Build FFmpeg command:
   ```powershell
   ffmpeg -i "$source" `
     -c:v libx264 -preset medium -crf 23 `
     -c:a aac -b:a 192k `
     -movflags +faststart `
     "$output"
   ```

5. Hardware acceleration detection:
   - Check for NVENC: `ffmpeg -encoders | Select-String nvenc`
   - Check for QSV: `ffmpeg -encoders | Select-String qsv`
   - Check for AMF: `ffmpeg -encoders | Select-String amf`
   - Suggest hardware encoder if available

6. Execute with progress monitoring:
   - Parse FFmpeg stderr for progress
   - Report: frame, fps, bitrate, time, size
   - Show ETA based on progress

7. Post-encode verification:
   - Run `/probe` on output file
   - Compare source vs output metrics
   - Report compression ratio
   - Check for quality issues (low bitrate warnings)

8. VapourSynth integration (if .vpy source):
   ```powershell
   vspipe --y4m "script.vpy" - | ffmpeg -i - [encoding options] output.mp4
   ```

9. Error handling:
   - FFmpeg not found → suggest `/build-portable`
   - Codec not available → suggest alternatives
   - Disk space → check before encoding
   - Permission denied → check output path
