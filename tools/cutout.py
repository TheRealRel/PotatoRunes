# PotatoRunes icon (Valheim mod): takes the artwork off its black background. The fire's glow fades into black, so
# each pixel's brightness becomes its opacity there (the colour is brightened back to match), and everything
# enclosed by the figure stays solid.
#   python3 -I tools/cutout.py in.jpg out.png
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

img = np.asarray(Image.open(sys.argv[1]).convert("RGB"), float)
m = img.max(2)
alpha = np.clip((m - 10) / 55, 0, 1)
solid = ndimage.binary_fill_holes(alpha > 0.85)
alpha[solid] = 1
rgb = np.where(alpha[..., None] > 0, np.clip(img / np.maximum(alpha, 1e-3)[..., None], 0, 255), 0)
out = np.dstack([rgb, alpha * 255]).astype(np.uint8)
Image.fromarray(out, "RGBA").save(sys.argv[2])
