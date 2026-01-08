"""
Basic VapourSynth Script Template
=================================
This template demonstrates how to load a video file and output it.

Usage:
    vspipe basic-source.vpy -c y4m - | ffmpeg -i - output.mkv

Or preview with a player:
    vspipe basic-source.vpy -c y4m - | mpv -
"""

import vapoursynth as vs
from vapoursynth import core

# =============================================================================
# Configuration - Edit these values
# =============================================================================

INPUT_FILE = r"C:\path\to\your\video.mp4"

# =============================================================================
# Load Video
# =============================================================================

# Method 1: Using L-SMASH Works (recommended for most files)
# clip = core.lsmas.LWLibavSource(INPUT_FILE)

# Method 2: Using FFMS2 (alternative, good for indexing)
clip = core.ffms2.Source(INPUT_FILE)

# =============================================================================
# Basic Info (printed to console)
# =============================================================================

print(f"Resolution: {clip.width}x{clip.height}")
print(f"Frames: {clip.num_frames}")
print(f"FPS: {clip.fps}")
print(f"Format: {clip.format.name}")

# =============================================================================
# Output
# =============================================================================

clip.set_output()
