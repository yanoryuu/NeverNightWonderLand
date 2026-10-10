"""フレームを部屋ごとに空間の格子 (行 = カメラ y 上が上, 列 = カメラ x) に並べたシートを作る。
py sheet.py <frames_dir> <out_dir> [thumb_w] [max_cols]
大きい部屋は列で分割する。"""
import os, re, sys
from PIL import Image, ImageDraw

src, out = sys.argv[1], sys.argv[2]
tw = int(sys.argv[3]) if len(sys.argv) > 3 else 240
maxc = int(sys.argv[4]) if len(sys.argv) > 4 else 8
os.makedirs(out, exist_ok=True)
pat = re.compile(r"(A1_\w+?)_(\d+)_r(\d+)_x(-?[\d.]+)_y(-?[\d.]+)\.png")
rooms = {}
for f in os.listdir(src):
    m = pat.match(f)
    if not m:
        continue
    scene, i, r, x, y = m.group(1), int(m.group(2)), int(m.group(3)), float(m.group(4)), float(m.group(5))
    rooms.setdefault((scene, r), []).append((x, y, f))
for (scene, r), fr in sorted(rooms.items()):
    xs = sorted({x for x, _, _ in fr}); ys = sorted({y for _, y, _ in fr}, reverse=True)
    th = tw * 9 // 16
    maxr = 9
    for part in range(0, len(xs), maxc):
      for rp in range(0, len(ys), maxr):
        cols = xs[part:part + maxc]
        rows = ys[rp:rp + maxr]
        img = Image.new("RGB", (len(cols) * (tw + 2), len(rows) * (th + 2)), (255, 0, 255))
        d = ImageDraw.Draw(img)
        for x, y, f in fr:
            if x not in cols or y not in rows:
                continue
            cx, cy = cols.index(x), rows.index(y)
            t = Image.open(os.path.join(src, f)).convert("RGB").resize((tw, th))
            img.paste(t, (cx * (tw + 2), cy * (th + 2)))
            d.text((cx * (tw + 2) + 3, cy * (th + 2) + 2), f"{x:g},{y:g}", fill=(255, 255, 0))
        name = f"{scene}_r{r}_c{part // maxc}_y{rp // maxr}.png"
        img.save(os.path.join(out, name))
        print(name, img.size)
