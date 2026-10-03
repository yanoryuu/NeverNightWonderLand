import sys, os
import numpy as np
from PIL import Image

src_dir = os.path.dirname(os.path.abspath(__file__))
names = sys.argv[1:] or [f[:-8] for f in os.listdir(src_dir) if f.endswith("_raw.png")]
for n in names:
    im = Image.open(os.path.join(src_dir, n + "_raw.png")).convert("RGB")
    a = np.asarray(im).astype(np.float32) / 255.0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # greenness: how much g exceeds max(r,b)
    gx = g - np.maximum(r, b)
    alpha = 1.0 - np.clip((gx - 0.15) / 0.35, 0, 1)
    # despill: clamp green to max(r,b) where it's spilling
    g2 = np.where(gx > 0, np.maximum(r, b) + np.clip(gx, 0, 0.0) , g)
    out = np.dstack([r, np.minimum(g, np.maximum(r, b) * 1.0 + 0.02), b, alpha])
    out = (np.clip(out, 0, 1) * 255).astype(np.uint8)
    img = Image.fromarray(out, "RGBA")
    bbox = img.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    if bbox:
        pad = 6
        bbox = (max(0, bbox[0] - pad), max(0, bbox[1] - pad), min(img.width, bbox[2] + pad), min(img.height, bbox[3] + pad))
        img = img.crop(bbox)
    img.save(os.path.join(src_dir, n + ".png"))
    print(n, img.size)
