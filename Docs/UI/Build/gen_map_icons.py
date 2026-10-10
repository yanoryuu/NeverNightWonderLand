# 全体マップ・ミニマップの仮素材 (単色の図形) を作る (Docs/UI/MapScreen.md)。
# 素材が届いたら同じ名前の PNG を差し替えるか、MapDrawing / MapScreenView の Sprite 参照を付け替える。
# 出力: Assets/LocalData/UI/_temp/map/*.png
#   - map_room.png    部屋の矩形 (白地 + 灰の縁。9 スライスで伸ばし、区画の色を掛ける)
#   - map_marker.png  現在地 (色付きのまま使う)
#   - icon_*.png      アイコン (白地 + 濃い縁取り。Image.color で色を掛ける)
# 実行: py -3 Docs/UI/Build/gen_map_icons.py   (Pillow が必要。何度流してもよい)
import math
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'Assets', 'LocalData', 'UI', '_temp', 'map')
os.makedirs(ROOT, exist_ok=True)

SIZE = 64
SS = 4  # 拡大して描いてから縮める (アンチエイリアス)
BIG = SIZE * SS
INK = (73, 0, 0, 255)  # 縁取り (#490000、UI の文字色)


def save(img, name):
    img.save(os.path.join(ROOT, name))


def glyph(draw_fn, name, outline=3):
    """draw_fn(d) で白い図形のマスクを描き、濃い縁取りを付けて保存する。"""
    mask = Image.new('L', (BIG, BIG), 0)
    d = ImageDraw.Draw(mask)
    draw_fn(d)
    grow = mask.filter(ImageFilter.MaxFilter(outline * SS * 2 + 1))
    out = Image.new('RGBA', (BIG, BIG), (0, 0, 0, 0))
    out.paste(Image.new('RGBA', (BIG, BIG), INK), (0, 0), grow)
    out.paste(Image.new('RGBA', (BIG, BIG), (255, 255, 255, 255)), (0, 0), mask)
    save(out.resize((SIZE, SIZE), Image.LANCZOS), name)


def S(*v):
    return [x * SS for x in v]


def poly(d, pts, fill=255):
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=fill)


# ---- 部屋 (9 スライス用 32x32, 縁 4px) ----
room = Image.new('RGBA', (32, 32), (150, 150, 150, 255))
ImageDraw.Draw(room).rectangle([4, 4, 27, 27], fill=(255, 255, 255, 255))
save(room, 'map_room.png')

# ---- 現在地 (色付き) ----
m = Image.new('RGBA', (BIG, BIG), (0, 0, 0, 0))
d = ImageDraw.Draw(m)
d.ellipse(S(6, 6, 58, 58), fill=INK)
d.ellipse(S(12, 12, 52, 52), fill=(235, 160, 68, 255))
d.ellipse(S(24, 24, 40, 40), fill=(255, 245, 220, 255))
save(m.resize((SIZE, SIZE), Image.LANCZOS), 'map_marker.png')

# ---- カーソル (十字の照準。白, 色を掛ける) ----
def cursor(d):
    w = 4 * SS
    d.ellipse(S(14, 14, 50, 50), outline=255, width=w)
    for a, b in (((32, 2), (32, 16)), ((32, 48), (32, 62)), ((2, 32), (16, 32)), ((48, 32), (62, 32))):
        d.line([(a[0] * SS, a[1] * SS), (b[0] * SS, b[1] * SS)], fill=255, width=w)
glyph(cursor, 'icon_cursor.png', outline=2)

# ---- 拠点 (セーブ): 家 ----
glyph(lambda d: (poly(d, [(32, 8), (58, 30), (52, 30), (52, 56), (12, 56), (12, 30), (6, 30)]),
                 d.rectangle(S(26, 38, 38, 56), fill=0)), 'icon_save.png')

# ---- ショップ: 袋 (コイン袋) ----
def shop(d):
    d.ellipse(S(10, 22, 54, 60), fill=255)
    poly(d, [(22, 10), (42, 10), (36, 24), (28, 24)])
    d.ellipse(S(26, 34, 38, 50), fill=0)
    d.ellipse(S(29, 37, 35, 47), fill=255)
glyph(shop, 'icon_shop.png')

# ---- 鍛冶屋: 金床 ----
glyph(lambda d: (poly(d, [(4, 18), (60, 18), (60, 24), (50, 30), (44, 30), (40, 40), (48, 52), (16, 52), (24, 40), (20, 30), (14, 30), (4, 24)])),
      'icon_smith.png')


# ---- ボス: 冠 / 撃破後: 冠に ×(斜線) ----
def crown(d):
    poly(d, [(6, 50), (6, 18), (20, 32), (32, 10), (44, 32), (58, 18), (58, 50)])
    d.rectangle(S(6, 50, 58, 58), fill=255)
glyph(crown, 'icon_boss.png')


def crown_done(d):
    crown(d)
    d.line(S(8, 56, 56, 8), fill=0, width=7 * SS)
glyph(crown_done, 'icon_boss_done.png')


# ---- スピーカー / 破壊後 ----
def speaker(d):
    d.rectangle(S(8, 22, 24, 42), fill=255)
    poly(d, [(24, 22), (44, 8), (44, 56), (24, 42)])
    d.arc(S(40, 18, 60, 46), -60, 60, fill=255, width=4 * SS)
glyph(speaker, 'icon_speaker.png')


def speaker_broken(d):
    d.rectangle(S(8, 22, 24, 42), fill=255)
    poly(d, [(24, 22), (44, 8), (44, 56), (24, 42)])
    poly(d, [(30, 6), (38, 26), (28, 34), (36, 58), (40, 58), (34, 34), (44, 26), (36, 6)], fill=0)
glyph(speaker_broken, 'icon_speaker_done.png')

# ---- 別エリアへの出口: 右向きの太い矢印 (向きは回転で付ける) ----
glyph(lambda d: poly(d, [(6, 24), (34, 24), (34, 10), (60, 32), (34, 54), (34, 40), (6, 40)]), 'icon_exit.png')


# ---- 近道 (ジップライン): 斜めの綱 + 滑車 ----
def zipline(d):
    d.line(S(4, 10, 60, 40), fill=255, width=5 * SS)
    d.ellipse(S(22, 14, 38, 30), fill=255)
    d.rectangle(S(28, 28, 32, 46), fill=255)
    d.rectangle(S(20, 44, 40, 50), fill=255)
glyph(zipline, 'icon_zipline.png')


# ---- 近道 (リフト・エレベーター): 箱 + 上下の三角 ----
def elevator(d):
    d.rectangle(S(12, 6, 52, 58), fill=255)
    poly(d, [(32, 12), (44, 28), (20, 28)], fill=0)
    poly(d, [(20, 36), (44, 36), (32, 52)], fill=0)
glyph(elevator, 'icon_elevator.png')

# ---- 一方通行: 右向きの山形 (向きは回転で付ける) ----
glyph(lambda d: poly(d, [(14, 8), (52, 32), (14, 56), (14, 42), (30, 32), (14, 22)]), 'icon_oneway.png', outline=2)

print('written to', os.path.abspath(ROOT))
