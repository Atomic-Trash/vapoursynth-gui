"""
BM3D Denoising Template
=======================
Block-Matching 3D denoising for high-quality noise reduction.

BM3D works by finding similar blocks in the video and denoising them together,
which preserves detail while effectively removing noise.
"""

import vapoursynth as vs
from vapoursynth import core

# =============================================================================
# Configuration
# =============================================================================

INPUT_FILE = r"C:\path\to\your\video.mp4"

# Denoising strength (sigma)
# Low noise:    1-3
# Medium noise: 4-8
# Heavy noise:  10-16
SIGMA = 5

# Profile for speed vs quality tradeoff
# "fast" - faster processing, slightly lower quality
# "lc"   - low complexity, good balance
# "np"   - normal profile, default
# "high" - highest quality, slower
PROFILE = "lc"

# =============================================================================
# Load Video
# =============================================================================

clip = core.lsmas.LWLibavSource(INPUT_FILE)

# Store original for comparison
original = clip

# =============================================================================
# Prepare for BM3D
# =============================================================================

# BM3D works best on GRAY or YUV formats
# Convert to YUV444 for processing
if clip.format.color_family == vs.RGB:
    clip = core.resize.Bicubic(clip, format=vs.YUV444PS)
elif clip.format.bits_per_sample < 16:
    # Convert to 16-bit for better precision
    clip = core.resize.Bicubic(clip, format=vs.YUV444P16)

# =============================================================================
# Apply BM3D Denoising
# =============================================================================

# Split into planes
y = core.std.ShufflePlanes(clip, 0, vs.GRAY)
u = core.std.ShufflePlanes(clip, 1, vs.GRAY)
v = core.std.ShufflePlanes(clip, 2, vs.GRAY)

# Denoise luma (Y) - usually needs more denoising
y_denoised = core.bm3d.VBasic(y, sigma=SIGMA, profile1=PROFILE)
y_denoised = core.bm3d.VAggregate(y_denoised, radius=1, sample=1)

# Denoise chroma (U, V) - usually needs less denoising
chroma_sigma = SIGMA * 0.6  # Less denoising on chroma
u_denoised = core.bm3d.VBasic(u, sigma=chroma_sigma, profile1=PROFILE)
u_denoised = core.bm3d.VAggregate(u_denoised, radius=1, sample=1)
v_denoised = core.bm3d.VBasic(v, sigma=chroma_sigma, profile1=PROFILE)
v_denoised = core.bm3d.VAggregate(v_denoised, radius=1, sample=1)

# Merge planes back
clip = core.std.ShufflePlanes([y_denoised, u_denoised, v_denoised], [0, 0, 0], vs.YUV)

# =============================================================================
# Output
# =============================================================================

# Convert back to original format if needed
# clip = core.resize.Bicubic(clip, format=original.format)

clip.set_output()

# Optionally output original for comparison
# original.set_output(1)
