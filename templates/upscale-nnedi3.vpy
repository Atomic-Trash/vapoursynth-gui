"""
NNEDI3 Upscaling Template
=========================
Neural network-based upscaling for high-quality image enlargement.

NNEDI3 uses a neural network trained on natural images to intelligently
interpolate and upscale video while preserving sharp edges.
"""

import vapoursynth as vs
from vapoursynth import core

# =============================================================================
# Configuration
# =============================================================================

INPUT_FILE = r"C:\path\to\your\video.mp4"

# Target resolution
TARGET_WIDTH = 1920
TARGET_HEIGHT = 1080

# Neural network strength (0-4, higher = better quality, slower)
# 0: fast
# 1: medium
# 2: slow (recommended)
# 3: slower
# 4: slowest, highest quality
NSIZE = 4  # neurons count
NPASS = 2  # processing passes

# =============================================================================
# Load Video
# =============================================================================

clip = core.lsmas.LWLibavSource(INPUT_FILE)

print(f"Input: {clip.width}x{clip.height}")
print(f"Target: {TARGET_WIDTH}x{TARGET_HEIGHT}")

# =============================================================================
# Calculate scaling
# =============================================================================

# Determine how many 2x upscales we need
import math

scale_x = TARGET_WIDTH / clip.width
scale_y = TARGET_HEIGHT / clip.height
scale = max(scale_x, scale_y)

# NNEDI3 doubles resolution, so we need log2(scale) passes
passes_needed = max(1, int(math.ceil(math.log2(scale))))

print(f"Upscale factor: {scale:.2f}x (using {passes_needed} NNEDI3 passes)")

# =============================================================================
# Upscale with NNEDI3
# =============================================================================

# Convert to YUV if RGB
if clip.format.color_family == vs.RGB:
    clip = core.resize.Bicubic(clip, format=vs.YUV420P16)
elif clip.format.bits_per_sample < 16:
    clip = core.resize.Bicubic(clip, format=clip.format.replace(bits_per_sample=16))

# Double resolution using NNEDI3
for i in range(passes_needed):
    # Transpose -> NNEDI3 -> Transpose -> NNEDI3 for 2x upscale
    clip = core.std.Transpose(clip)
    clip = core.znedi3.nnedi3(clip, field=1, dh=True, nsize=NSIZE, nns=NPASS)
    clip = core.std.Transpose(clip)
    clip = core.znedi3.nnedi3(clip, field=1, dh=True, nsize=NSIZE, nns=NPASS)

# =============================================================================
# Resize to exact target resolution
# =============================================================================

# Use high-quality Lanczos for final resize
clip = core.resize.Lanczos(clip, width=TARGET_WIDTH, height=TARGET_HEIGHT)

print(f"Output: {clip.width}x{clip.height}")

# =============================================================================
# Output
# =============================================================================

clip.set_output()
