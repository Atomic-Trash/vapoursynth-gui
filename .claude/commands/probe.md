Analyze a media file and report its properties.

Arguments: `$ARGUMENTS` (file path to analyze)

Instructions:
1. If no file path provided, ask user for the file path
2. Verify the file exists using `Test-Path`
3. Run FFprobe to analyze the media file:
   ```powershell
   ffprobe -v quiet -print_format json -show_format -show_streams "$ARGUMENTS"
   ```
4. Parse and display the following information:

**Container Information:**
- Format name and long name
- Duration (formatted as HH:MM:SS.mmm)
- Total bitrate
- File size

**Video Stream(s):**
- Codec (name and profile)
- Resolution (width x height)
- Frame rate (fps)
- Pixel format
- Color space/range (if available)
- Bitrate

**Audio Stream(s):**
- Codec
- Sample rate
- Channels (with layout if available)
- Bitrate

**Subtitle Stream(s):**
- Codec/format
- Language (if tagged)

5. If FFprobe is not found, check these locations:
   - `dist/ffmpeg/ffprobe.exe`
   - System PATH
   - Suggest running `/build-portable` if not found

6. Provide recommendations based on analysis:
   - Suggest optimal export settings
   - Note any potential issues (variable frame rate, unusual codecs)
   - Recommend deinterlacing if interlaced content detected
