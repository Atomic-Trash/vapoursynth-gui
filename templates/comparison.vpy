"""
Side-by-Side Comparison Template
================================
Compare original and processed video side by side.

Useful for evaluating filter effects before committing to a full encode.
"""

import vapoursynth as vs
from vapoursynth import core

# =============================================================================
# Configuration
# =============================================================================

INPUT_FILE = r"C:\path\to\your\video.mp4"

# Comparison mode
# "split"       - Left/right split with draggable line
# "stack"       - Horizontal stack (side by side)
# "stack_v"     - Vertical stack (top/bottom)
# "interleave"  - Alternating frames (for A/B comparison)
MODE = "stack"

# =============================================================================
# Load Video
# =============================================================================

clip = core.lsmas.LWLibavSource(INPUT_FILE)
original = clip

# =============================================================================
# Apply Your Processing Here
# =============================================================================

# Example: Apply denoising
processed = core.std.BoxBlur(clip, hradius=1, vradius=1)

# Example: Apply sharpening
# processed = core.cas.CAS(clip, sharpness=0.5)

# Example: Apply color correction
# processed = core.std.Levels(clip, min_in=16, max_in=235, min_out=0, max_out=255)

# =============================================================================
# Create Comparison
# =============================================================================

# Add text labels
original_labeled = core.text.Text(original, "ORIGINAL", alignment=7)
processed_labeled = core.text.Text(processed, "PROCESSED", alignment=9)

if MODE == "stack":
    # Horizontal stack (side by side)
    comparison = core.std.StackHorizontal([original_labeled, processed_labeled])

elif MODE == "stack_v":
    # Vertical stack (top/bottom)
    comparison = core.std.StackVertical([original_labeled, processed_labeled])

elif MODE == "split":
    # Split screen (left original, right processed)
    width = original.width
    half = width // 2

    left = core.std.CropRel(original_labeled, right=half)
    right = core.std.CropRel(processed_labeled, left=half)
    comparison = core.std.StackHorizontal([left, right])

    # Add center line
    comparison = core.std.AddBorders(comparison, left=0, right=0, top=0, bottom=0)

elif MODE == "interleave":
    # Interleave frames (A, B, A, B, ...)
    # Press frame-step in your player to compare
    comparison = core.std.Interleave([original_labeled, processed_labeled])

else:
    comparison = core.std.StackHorizontal([original_labeled, processed_labeled])

# =============================================================================
# Output
# =============================================================================

print(f"Comparison mode: {MODE}")
print(f"Output resolution: {comparison.width}x{comparison.height}")

comparison.set_output()

# Also output individual clips for multi-output players
original.set_output(1)
processed.set_output(2)
