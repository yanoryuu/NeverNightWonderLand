# グリーンバック (#00FF00) で生成した絵をクロマキーで抜き、部品ごとに切り出す (PendingChanges 35/36)。
#   py chroma_slice.py single <raw.png> <out.png> <canvasPx>          … 1 個の絵を canvas×canvas の正方形に収める (中央, pivot center)
#   py chroma_slice.py sheet  <raw.png> <outPrefix> <scale> [minArea]  … 離れて並んだ部品を連結成分で切り出し <outPrefix>_01.png …
# 縁の緑かぶりは「g を max(r,b) までに抑える」デスピルと、半透明の縁の色を内側の色で置き換える処理で消す。
import sys
import numpy as np
from PIL import Image, ImageFilter


def key(path):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255.0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx = np.maximum(r, b)
    gx = g - mx  # 緑の強さ
    alpha = 1.0 - np.clip((gx - 0.12) / 0.30, 0, 1)
    # デスピル: 緑が r,b より強い所は g を抑える
    g2 = np.minimum(g, mx + 0.02)
    rgb = np.dstack([r, g2, b])
    # 縁 (半透明) の色は少し暗くして黒い輪郭になじませる
    edge = (alpha > 0) & (alpha < 1)
    rgb[edge] *= 0.6 + 0.4 * alpha[edge][:, None]
    out = np.dstack([rgb, alpha])
    img = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), "RGBA")
    # 1px 収縮で緑の縁を完全に落とす
    a8 = img.getchannel("A").filter(ImageFilter.MinFilter(3))
    img.putalpha(Image.fromarray(np.minimum(np.asarray(img.getchannel("A")), np.asarray(a8))))
    return img


def bbox_of(img, thr=24):
    return img.getchannel("A").point(lambda v: 255 if v > thr else 0).getbbox()


def single(src, dst, canvas):
    img = key(src)
    img = img.crop(bbox_of(img))
    pad = int(canvas * 0.03)
    s = (canvas - 2 * pad) / max(img.width, img.height)
    img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    out.alpha_composite(img, ((canvas - img.width) // 2, (canvas - img.height) // 2))
    out.save(dst)
    print(dst, out.size)


def label(mask):
    """4 近傍の連結成分 (scipy 無しで動くよう素朴な BFS)。戻り値 (ラベル配列, スライスのリスト)"""
    from collections import deque
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    objs = []
    n = 0
    for sy, sx in zip(*np.nonzero(mask)):
        if lab[sy, sx]:
            continue
        n += 1
        lab[sy, sx] = n
        q = deque([(sy, sx)])
        y0, y1, x0, x1 = sy, sy, sx, sx
        while q:
            y, x = q.popleft()
            y0, y1, x0, x1 = min(y0, y), max(y1, y), min(x0, x), max(x1, x)
            for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not lab[ny, nx]:
                    lab[ny, nx] = n
                    q.append((ny, nx))
        objs.append((slice(y0, y1 + 1), slice(x0, x1 + 1)))
    return lab, objs


def sheet(src, prefix, scale, min_area=400):
    img = key(src)
    a = img.getchannel("A").point(lambda v: 255 if v > 24 else 0).filter(ImageFilter.MaxFilter(9))
    lab, objs = label(np.asarray(a) > 0)
    parts = []
    for i, sl in enumerate(objs):
        if (lab[sl] == i + 1).sum() < min_area:
            continue
        parts.append((sl[0].start, sl[1].start, sl, i + 1))
    # 上の行から左→右
    parts.sort(key=lambda p: (round(p[0] / 200), p[1]))
    for k, (_, _, sl, li) in enumerate(parts):
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        piece = img.crop((x0, y0, x1, y1))
        m = Image.fromarray(((lab[sl] == li) * 255).astype(np.uint8))
        piece.putalpha(Image.fromarray(np.minimum(np.asarray(piece.getchannel("A")), np.asarray(m))))
        piece = piece.crop(bbox_of(piece, 8))
        pad = 4
        canvas = Image.new("RGBA", (piece.width + pad * 2, piece.height + pad * 2), (0, 0, 0, 0))
        canvas.alpha_composite(piece, (pad, pad))
        canvas = canvas.resize((max(1, round(canvas.width * scale)), max(1, round(canvas.height * scale))), Image.LANCZOS)
        dst = f"{prefix}_{k + 1:02d}.png"
        canvas.save(dst)
        print(dst, canvas.size)


if __name__ == "__main__":
    mode = sys.argv[1]
    if mode == "single":
        single(sys.argv[2], sys.argv[3], int(sys.argv[4]))
    else:
        sheet(sys.argv[2], sys.argv[3], float(sys.argv[4]), int(sys.argv[5]) if len(sys.argv) > 5 else 400)
