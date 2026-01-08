"""
Batch Processing Template
=========================
Process multiple video files with the same filter chain.

This script is designed to be called with different input files:
    vspipe batch-process.vpy -a input=video1.mp4 -c y4m - | ffmpeg -i - output1.mkv
    vspipe batch-process.vpy -a input=video2.mp4 -c y4m - | ffmpeg -i - output2.mkv

Or use the included batch wrapper script.
"""

import vapoursynth as vs
from vapoursynth import core
import os
import sys

# =============================================================================
# Get Input File
# =============================================================================

# Check for command-line argument (vspipe -a input=...)
INPUT_FILE = None

# Method 1: VSPipe argument
if hasattr(vs, 'get_output'):
    # Try to get from VSPipe argument
    pass

# Method 2: Environment variable
if not INPUT_FILE:
    INPUT_FILE = os.environ.get('VS_INPUT_FILE')

# Method 3: Hardcoded default for testing
if not INPUT_FILE:
    INPUT_FILE = r"C:\path\to\your\video.mp4"

# Validate input
if not INPUT_FILE or not os.path.exists(INPUT_FILE):
    raise ValueError(f"Input file not found: {INPUT_FILE}")

print(f"Processing: {INPUT_FILE}")

# =============================================================================
# Load Video
# =============================================================================

clip = core.lsmas.LWLibavSource(INPUT_FILE)

# Store original info
original_width = clip.width
original_height = clip.height
original_fps = clip.fps

print(f"Input: {original_width}x{original_height} @ {original_fps}")

# =============================================================================
# Common Processing Pipeline
# =============================================================================

# Step 1: Convert to 16-bit for processing
if clip.format.bits_per_sample < 16:
    clip = core.resize.Bicubic(clip, format=clip.format.replace(bits_per_sample=16))

# Step 2: Denoising (light)
# Uncomment to enable
# clip = core.std.BoxBlur(clip, hradius=1, vradius=1)

# Step 3: Color correction
# Adjust levels if needed
# clip = core.std.Levels(clip, min_in=16, max_in=235, min_out=0, max_out=255)

# Step 4: Resize if needed
# Uncomment and adjust target resolution
# TARGET_WIDTH = 1920
# TARGET_HEIGHT = 1080
# if clip.width != TARGET_WIDTH or clip.height != TARGET_HEIGHT:
#     clip = core.resize.Lanczos(clip, width=TARGET_WIDTH, height=TARGET_HEIGHT)

# Step 5: Convert to output format
# Common formats:
# - YUV420P10 for high quality 10-bit encoding
# - YUV420P8 for standard 8-bit encoding
clip = core.resize.Bicubic(clip, format=vs.YUV420P10)

# =============================================================================
# Output
# =============================================================================

print(f"Output: {clip.width}x{clip.height} @ {clip.fps}")
print(f"Format: {clip.format.name}")

clip.set_output()
