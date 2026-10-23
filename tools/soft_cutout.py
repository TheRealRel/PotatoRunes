# PotatoRunes icon (Valheim mod): takes the art off its black background while keeping the soft glow.
# The hard cut-out (art/cutout-mask.png) keeps the solid parts; around them, the glow over black becomes see-through
# glow of the same colour, so it fades into any page colour instead of being clipped.
#   python3 -I tools/soft_cutout.py art.jpg mask.png out.png
import sys
import numpy as np
from PIL import Image, ImageFilter

art = np.asarray(Image.open(sys.argv[1]).convert("RGB"), float) / 255.0
mask = np.asarray(Image.open(sys.argv[2]).convert("RGBA").resize((art.shape[1], art.shape[0])), float)[..., 3] / 255.0
mask = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)), float) / 255.0

# Over black, a pixel's brightest channel is how opaque its glow is; dividing by it gives the glow's own colour.
lum = art.max(-1)
glow_a = np.clip((lum - 0.04) / 0.96, 0, 1) ** 0.85
alpha = np.maximum(mask, glow_a)
colour = np.where(mask[..., None] > 0.5, art, art / np.maximum(lum, 1e-3)[..., None] * np.clip(lum, 0, 1)[..., None] / np.maximum(alpha, 1e-3)[..., None])
out = np.dstack([np.clip(colour, 0, 1), alpha])
Image.fromarray((out * 255).astype(np.uint8), "RGBA").save(sys.argv[3])
