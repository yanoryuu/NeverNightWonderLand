# 紙目 (グレイン) と周辺の暗さ (ビネット) を手続きで作り、紙・背景の素材に焼き込む (Docs/UI/UIImplementation.md §1.1)。
# 完成形には重ね素材が無い (UIAssignment.md「2回目の決定」1) ので、
#   - 明るさ: 完成形 ÷ 素材そのまま の比を画面座標の滑らかな多項式 (色ごと) で当てはめ、その倍率を掛ける
#   - 紙目: 乱数から作る (まだら + 細かい粒 + 短い繊維 + 小さな斑点)。完成形からは切り出さない
# 出力: Assets/LocalData/UI/_gen/*.png (素材と同じ大きさ。import_ui.cs が Sprite として取り込む)
# 実行: py -3 Docs/UI/Build/gen_paper.py   (Pillow / numpy / OpenCV が必要。何度流してもよい: 乱数の種は固定)
import os
import numpy as np
import cv2
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'Assets', 'LocalData', 'UI')
OUT = os.path.join(ROOT, '_gen')
os.makedirs(OUT, exist_ok=True)


def load_rgba(rel):
    return np.array(Image.open(os.path.join(ROOT, rel)).convert('RGBA')).astype(np.float32)


def mock(rel):
    return np.array(Image.open(os.path.join(ROOT, rel)).convert('RGB').resize((1920, 1080), Image.LANCZOS)).astype(np.float32)


def feats(nx, ny):
    # 画面中心からの距離の偶数次 + 上下・左右の偏り
    return np.stack([np.ones_like(nx), nx * nx, ny * ny, nx ** 4, ny ** 4, nx * nx * ny * ny, ny, nx], -1)


def screen_box(w, h, cx, cy):
    """UI 座標 (中心原点・上が +y) の中心 (cx, cy) に置いた w×h の左上の画面座標。"""
    return int(round(960 + cx - w / 2)), int(round(540 - cy - h / 2))


def fit_ratio(samples):
    """samples: [(sx, sy, ratio_rgb)] → 色ごとの多項式係数。"""
    s = np.array([(x, y) for x, y, _ in samples], np.float32)
    r = np.array([v for _, _, v in samples], np.float32)
    F = feats((s[:, 0] - 960) / 960, (s[:, 1] - 540) / 540)
    coef, *_ = np.linalg.lstsq(F, r, rcond=None)
    rms = np.sqrt(((F @ coef - r) ** 2).mean(0))
    return coef, rms


def paper_samples(paper, cx, cy, m, block=12):
    """紙の無地の所 (完成形でも模様の無い所) で 完成形 / 素材 の比を集める。"""
    h, w = paper.shape[:2]
    x0, y0 = screen_box(w, h, cx, cy)
    out = []
    for by in range(0, h - block, block):
        for bx in range(0, w - block, block):
            sx, sy = x0 + bx, y0 + by
            if sx < 0 or sy < 0 or sx + block > 1920 or sy + block > 1080:
                continue
            a = paper[by:by + block, bx:bx + block]
            if a[..., 3].min() < 255 or a[..., :3].reshape(-1, 3).std(0).max() > 3:
                continue
            mm = m[sy:sy + block, sx:sx + block].reshape(-1, 3)
            if mm.std(0).max() > 6:
                continue
            ratio = mm.mean(0) / np.maximum(a[..., :3].reshape(-1, 3).mean(0), 1)
            if not (0.5 <= ratio[0] <= 1.05):
                continue  # 紙の上に置かれた枠・ボタン
            out.append((sx + block / 2, sy + block / 2, ratio))
    return out


def bg_samples(bg, m, exclude, block=16):
    """背景の所 (exclude = 紙などを除く画面矩形のリスト) で比を集める。"""
    out = []
    for sy in range(0, 1080 - block, block):
        for sx in range(0, 1920 - block, block):
            if any(ex0 - 20 <= sx <= ex1 + 20 and ey0 - 20 <= sy <= ey1 + 20 for ex0, ey0, ex1, ey1 in exclude):
                continue
            b = bg[sy:sy + block, sx:sx + block].reshape(-1, 3).mean(0)
            mm = m[sy:sy + block, sx:sx + block].reshape(-1, 3).mean(0)
            out.append((sx + block / 2, sy + block / 2, mm / np.maximum(b, 1)))
    return out


def apply_ratio(img, coef, x0, y0):
    h, w = img.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    nx = (xs + x0 + 0.5 - 960) / 960
    ny = (ys + y0 + 0.5 - 540) / 540
    k = np.clip(feats(nx, ny) @ coef, 0, 1.2)
    out = img.copy()
    out[..., :3] = np.clip(img[..., :3] * k, 0, 255)
    return out


def grain(h, w, seed):
    """紙目の倍率マップ (h×w×3, 1 = 変化なし)。"""
    rng = np.random.default_rng(seed)
    g = np.ones((h, w, 3), np.float32)
    # まだら (大きなむら)
    mottle = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0), 28)
    mottle /= max(mottle.std(), 1e-6)
    g *= (1 + 0.012 * mottle)[..., None]
    # 細かい粒
    fine = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0), 0.8)
    fine /= max(fine.std(), 1e-6)
    g *= (1 + 0.006 * fine)[..., None]
    # 短い繊維 (細い曲線、やや緑がかった茶)
    fib = np.zeros((h, w), np.float32)
    for _ in range(int(h * w / 2200)):
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        ang = rng.uniform(0, np.pi)
        length = rng.uniform(4, 13)
        bend = rng.uniform(-0.25, 0.25)
        pts = []
        for t in np.linspace(0, 1, 6):
            a = ang + bend * t
            pts.append((x + np.cos(a) * length * t, y + np.sin(a) * length * t))
        cv2.polylines(fib, [np.round(np.array(pts) * 4).astype(np.int32)], False, float(rng.uniform(0.3, 1.0)), 1, cv2.LINE_AA, shift=2)
    fib = cv2.GaussianBlur(fib, (0, 0), 0.5)
    g *= (1 - 0.07 * fib[..., None] * np.array([1.0, 0.9, 1.2], np.float32))
    # 小さな斑点 (濃い茶)
    spk = np.zeros((h, w), np.float32)
    for _ in range(int(h * w / 30000)):
        cv2.circle(spk, (int(rng.uniform(0, w) * 4), int(rng.uniform(0, h) * 4)), int(rng.uniform(0.6, 1.8) * 4),
                   float(rng.uniform(0.4, 1.0)), -1, cv2.LINE_AA, shift=2)
    spk = cv2.GaussianBlur(spk, (0, 0), 0.6)
    g *= (1 - 0.35 * spk[..., None] * np.array([0.8, 1.0, 1.1], np.float32))
    return g


def save(img, name):
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, name))
    print('wrote', name)


def bake_paper(sprite, cx, cy, mockup, out_name, seed):
    paper = load_rgba(sprite)
    m = mock(mockup)
    coef, rms = fit_ratio(paper_samples(paper, cx, cy, m))
    h, w = paper.shape[:2]
    x0, y0 = screen_box(w, h, cx, cy)
    out = apply_ratio(paper, coef, x0, y0)
    out[..., :3] *= grain(h, w, seed)
    save(out, out_name)
    print('   rms', rms.round(4))
    return coef


if __name__ == '__main__':
    # タイトル (2) / ロード: start2_paper と load_paper は同じ絵・同じ位置 (18, -200.5)
    bake_paper('start2/start2_paper.png', 18, -200.5, 'start2/ui_アートボード 1 のコピー.png', 'paper_menu.png', 1)
    # ポーズの紙
    bake_paper('pause/pause_paper.png', 18.5, -235.5, 'pause/ui_アートボード 1 のコピー 4.png', 'paper_pause.png', 2)
    # 資料タブの紙 (load_paper を (19, -300.5) に置いたもの。アイテム画面の背景は素材のままで完成形と一致)
    bake_paper('load/load_paper.png', 19, -300.5, 'item/shiryou/装備_shiryou.png', 'paper_journal.png', 3)

    # ポーズの背景: all_background に周辺の暗さを掛ける (紙と看板の所は除いて当てはめる)
    bg = load_rgba('item/all_background.png')
    bg = np.array(Image.fromarray(bg.astype(np.uint8), 'RGBA').resize((1920, 1080), Image.LANCZOS)).astype(np.float32)
    m = mock('pause/ui_アートボード 1 のコピー 4.png')
    pw, ph = 1239, 1451
    px0, py0 = screen_box(pw, ph, 18.5, -235.5)
    coef, rms = fit_ratio(bg_samples(bg[..., :3], m, [(px0 - 80, py0 - 60, px0 + pw + 40, 1080), (660, 0, 1300, 380)]))
    save(apply_ratio(bg, coef, 0, 0), 'background_pause.png')
    print('   rms', rms.round(4))

    # 資料の一覧のスクロールのつまみ・溝 (角丸の白。色は UI 側で乗せる)
    pill = np.zeros((32, 12, 4), np.float32)
    mask = np.zeros((32 * 4, 12 * 4), np.uint8)
    cv2.rectangle(mask, (0, 24), (47, 103), 255, -1)
    cv2.circle(mask, (24, 24), 24, 255, -1)
    cv2.circle(mask, (24, 103), 24, 255, -1)
    pill[..., :3] = 255
    pill[..., 3] = cv2.resize(mask, (12, 32), interpolation=cv2.INTER_AREA)
    save(pill, 'pill.png')
