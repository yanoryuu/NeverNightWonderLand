# ステージタイル配置の法則 (グレーボックス → 新タイル素材)

対象: `Assets/Scenes/GameField_old/` の全ステージシーン。
グレーボックス地形 (白 Square スプライト + BoxCollider2D / 緑 = すり抜け床) を、
`Assets/LocalData/StageAsset/tile_floor/` の新タイル素材で組んだ Tilemap に置き換える。
素材の規約 (1セル = 546px / PPU 546 / Tile アセットのオフセット) は既存の運用どおり。

## 1. 基本方針

| 項目 | 決定 | 理由 |
|---|---|---|
| 衝突 | **Tilemap は見た目のみ。既存の BoxCollider2D / PlatformEffector2D はそのまま残す** | ジャンプ設計 (跳び上がり ≤3.0、すり抜け床の厚み 0.4 等) を一切壊さない。準備室のように Tilemap 自体が地形 (TilemapCollider2D 持ち) の所だけはその方式を維持し、RuleTile の collider を Grid にして塗り直す |
| 白 Square の SpriteRenderer | **`enabled = false` にして残す** (GameObject もコライダーも削除しない) | 元に戻せる・コライダーの可視化が必要なら再有効化するだけ。色付きのギミック塊 (ボス扉/強化壁/スピーカー/SkillBreakable 等) は触らない |
| 坂 (回転した Square、全26シーンで10個) | **角度と長さが合う坂タイル (ascent/descent_01/02/03 = 45°/26.6°/18.4°、1セル上昇あたり 1/2/3 セル走行) を整数格子に敷く。合わない坂はスプライトを残して本体色 (80,0,54) に着色** | 採用条件は「角度差 ≤4°」かつ「タイル面とコライダー上面の最大ズレ ≤0.30」かつ「\|上昇量 − 枚数\| ≤0.25」(1枚=1セル上昇なので、上昇 1.5 の坂は何枚敷いても上の床と合わない)。ズレると足が浮く/沈むので、合わないものは正直に本体色の板にする (後述 §4) |
| 自動選択 | **RuleTile** (`Assets/LocalData/StageAsset/Tiles/Rules/StageTerrainRule.asset`) 1つで、上面/端/角/側面/下面/内部を隣接から決める | 26シーンぶん手で置くのは非現実的。RuleTile は再利用資産なので Assets に置く |
| 一発物スクリプト | Assets には置かない。変換ルーチンは `execute_code` で流す (下記) | プロジェクト方針 |

## 2. 素材の意味付け (実物を見て確定)

| タイル | サイズ(セル) | 役割 |
|---|---|---|
| `stagetile_floor_04_l / m / r` | 1×1 | **標準の床上面**。l/r は端キャップ (斜めの飾り)。2セル厚以上の足場の上段に使う |
| `stagetile_floor_05_l / m / r` | 2×1 | 04 と同じ絵の2セル幅版。RuleTile では使わない (04_m を2枚並べたのと同じ見え) |
| `stagetile_floor_01` | 1×1 | 端キャップ無しの上面。**1セル幅の柱の頭** に使う |
| `stagetile_floor_02` | 2×1 | 01 の2セル幅版。自動では使わない |
| `stagetile_floor_03` | 1×2/3 | 薄い上面 (中央寄せ)。自動では使わない (上面がセル境界に来ない) |
| `stagetile_floor_06_l / m` (1×2/3), `_r` (1×1) | — | 薄い床族だが r だけ丈が違う (素材の書き出し起因)。自動では使わない |
| `stagetile_floor_07_l / m / r` | 1×1 (下に ~23px はみ出し) | **1セル厚の浮き足場** (上下とも露出)。下面に装飾帯があるので下から見ても成立する |
| `stagetile_floor_08_l / m / r` | 1×0.79 | **すり抜け床** (薄い浮き足場、下端に細い帯)。別レイヤーにセル単位オフセットで置く |
| `stagetile_coverbox` | 1×1 | **内部の埋め** |
| `stagetile_wall_01` | 1×1 | **側面** (左向き。右向きは MirrorX) |
| `stagetile_ceiling_01` | 1×1 | **下面** (天井・足場の裏) |
| `角` | 1×1 | **上面の凸角で、下に壁面が続く所** (左上。右上は MirrorX)。上面の飾り + 縦の壁飾りが一体化した絵 |
| `平角` | 2/3×2/3 | 薄い床族の角。自動では使わない |
| `stagetile_ascent_0N / descent_0N` | 1×2, 2×2, 3×2 | 坂。**斜面は下1行の胴体の上に乗る**: スプライト左下原点で ascent は (0,1)→(N,2)、descent は (0,2)→(N,1) [セル単位、実測 45.0°/26.6°/18.6°]。既存の Tilemap に置かれているものはそのまま残す |
| `stagetile_marker_solid` | — | **透明マーカー** (sprite 無し・collider None)。坂スプライトが覆う空セルに置いて隣接判定だけ「実体」にする |

## 3. RuleTile の規則 (`StageTerrainRule.asset`, 型 `NeverNight.Stage.TerrainRuleTile`)

`TerrainRuleTile` (`Assets/Scripts/Stage/TerrainRuleTile.cs`) は RuleTile の隣接判定を
「同じ RuleTile か」→「**何かタイルがあるか**」に変えたもの。これで坂タイルやマーカーも実体として数えられ、坂の脇に端キャップや壁面が出ない。

規則は上から順に評価 (N/S/E/W = 上/下/右/左、SS = 2つ下、SW = 左下):

| # | 条件 | 出力 |
|---|---|---|
| 1 | N空 W空 S実 E実 SS実 SW空 | `角` (MirrorX で右角も) |
| 2 | N空 W空 S実 E実 | `floor_04_l` |
| 3 | N空 E空 S実 W実 | `floor_04_r` |
| 4 | N空 W空 E空 S実 | `floor_01` (柱の頭) |
| 5 | N空 S実 | `floor_04_m` |
| 6 | N空 S空 W空 E実 | `floor_07_l` |
| 7 | N空 S空 E空 W実 | `floor_07_r` |
| 8 | N空 S空 | `floor_07_m` (孤立1セルも) |
| 9 | S空 (Nは実) | `ceiling_01` |
| 10 | W空 | `wall_01` (MirrorX で右面も) |
| 既定 | — | `coverbox` |

collider は全規則 Grid (準備室方式のシーンで TilemapCollider2D がフルセルの箱になる。見た目専用シーンでは無関係)。

## 4. グリッド整列の法則 (グレーボックス → セル)

- **X**: セル中心 (x+0.5) がコライダー矩形に入っていれば実体。1.5 幅の壁は2セルになり (外側に 0.5 膨らむ)、位置が .25 端数の壁は最大 0.25 ズレる (許容)。
- **Y**: 矩形ごとに「どの辺をセル境界に正確に合わせるか」を決める:
  - 上面が整数 → 上面基準 (床)
  - 上面が小数で下面が整数 → **下面基準** (天井・庇)
  - 両方小数 → 上面基準 (プレイヤーが立つ面を優先)
  基準辺の小数部 f (0.05 刻み) ごとに **別 Tilemap レイヤー** `Terrain` (f=0) / `Terrain_y0.50` … を作り、レイヤーの Y 位置を f にする。これで上面 2.5 の段も正確に 2.5 に見える。
  基準でない辺は四捨五入 (半分以上覆うセルまで)。厚み < 0.5 でも最低1行。
- 同じレイヤー内の矩形は RuleTile が自動で繋がる。**レイヤーが違う矩形同士は繋がらない** (高さが違うので段差として見えて自然だが、同じ面のはずのものが .0 と .5 に分かれていると継ぎ目に端キャップが出る → その時は `extraTerrainNames` ではなく元のグレーボックスの座標を揃える)。
- **すり抜け床** (PlatformEffector2D): `Platforms` レイヤーに `floor_08_l/m/r` (1枚なら m) を置き、`Tilemap.SetTransformMatrix` でセル単位に Y を平行移動してスプライト上端 = コライダー上端に合わせる (08 の Tile は LockTransform 無しなので可)。
- **坂** (回転した Square): BoxCollider2D の上辺の線分 (低端 low → 高端 high) を求め、角度 atan(rise/run) に最も近い坂タイル幅 w (1/2/3 = 45°/26.6°/18.4°) を選ぶ。枚数 n = round(rise)、低端を整数格子に丸めて (xl, yl) とし、i 枚目を ascent なら斜面 (xl+i·w, yl+i)→(+w, +1)、descent なら (xl−(i+1)·w, yl+i+1)→(xl−i·w, yl+i) になるアンカーに置く。アンカーは `ax = (w==3 ? x0+1 : x0), ay = y0` (タイル資産に x+0.5 / y−0.5 / LockTransform が焼き込まれている)。足元 2×w セルのうちアンカー以外は `stagetile_marker_solid` で上書き (胴体はスプライトが描く。RuleTile の上面帯が三角の上に出ないように)。**採用条件: 角度差 ≤4° かつ、タイル面とコライダー上面のズレ (タイルが覆う範囲を21点サンプリング) ≤0.30 かつ |rise − n| ≤0.25**。3つ目が要る理由: UpperPromenade の 15° 坂 (rise 1.55) は前2条件 (Δ3.4°, ズレ 0.27) を通り `_03`×2 (上昇 2.0) が置かれて上の床 (1.5) から 0.5 突き出た。ズレ判定は上端をコライダー高さで打ち切るため段差の不一致を拾えない。不採用は着色スプライトのまま、ログに角度・最近似・ズレを出す。坂は基準レイヤー `Terrain` (f=0) に置く。

## 5. シーン内の Tilemap 構成

```
Grid (cellSize 1×1, 原点)
 ├ Terrain          sortingOrder 0  … 基準辺が整数の地形 (RuleTile)
 ├ Terrain_y0.50    sortingOrder 1  … 基準辺が .5 の地形 (Y=+0.5)。他の小数も同様に増える
 └ Platforms        sortingOrder 2  … すり抜け床 (floor_08, セル別オフセット)
```
Tile Anchor は全て (0.5, 0.5)。既存の装飾 Tilemap (旧 straight_mid の帯) は中身を消して `Terrain` として再利用する。
準備室方式 (TilemapCollider2D 持ち) のシーンは既存 Tilemap を Grid の子にしてそのまま塗り直す (レイヤー追加なし)。

## 6. 変換ルーチンと使い方 (Cエージェント向け)

ソース: `Docs/StageTileRetile.execute_code.cs.txt` (同じものが scratchpad/retile.cs にもある。メソッド本体なので `using` 無し、完全修飾名)。
`mcp__UnityMCP__execute_code` に **全文を貼って** 実行する。冒頭の設定だけ変える:

```csharp
string scenePath = "Assets/Scenes/GameField_old/<Scene>.unity";
bool saveScene = true;
var terrainColors = new[] { new Color(0.72f, 0.70f, 0.68f), new Color(0.7f, 0.7f, 0.7f) }; // 地形色 (グレーボックス規約 + PistonHall の Step)
string[] extraTerrainNames = { };   // 色が違っても地形にしたい GameObject 名
string[] excludeNames = { };        // 地形色でも触らない GameObject 名
```

**必ず先にシーンを開いておく** (`mcp__unity-editor-mcp__open_scene` か、ルーチンを1回流す = 開くだけで戻る)。
同じ execute_code 呼び出し内で OpenScene → SetTile すると、単発 `SetTile` (坂・マーカー・すり抜け床) が保存前に消え、
`GetUsedTilesCount()==0` 判定で Platforms 層が削除される現象を確認済み (バッチ `SetTiles` は残る)。ルーチンは開いた直後は自動で return する。

処理の流れ: (開いてあるシーンで) Grid を確保 → (A) TilemapCollider2D 持ちの Tilemap があれば全セルを RuleTile で塗り直し、既存の坂タイルは残してマーカーを敷く /
(B) 無ければ SpriteRenderer+BoxCollider2D を走査し、地形色→Terrain レイヤー群、Effector→Platforms、回転→坂タイル or 着色、色付き→スキップ → SpriteRenderer を無効化 → 空レイヤー削除 → 全タイル再評価 → 保存。
**再実行可能** (無効化済み/着色済みスプライトも対象にする、マーカーは保持)。1シーン数秒。
坂の採用しきい値は冒頭の `slopeMaxAngleDiff` (4°) / `slopeMaxDeviation` (0.30) / `slopeMaxRiseError` (0.25) で変えられる。
**実行は必ず 2 回**: 1回目はアクティブシーンが違うと「シーンを開きました…もう一度実行してください」で戻る。2回目で本処理。ログ 1 行目 `scene:` が目的のシーンか確認する。
`replay` は使わない (履歴インデックスが他の実行と混ざり、別シーンを開くコードが走ったことがある)。毎回コード全文を貼る。
同じエディタを別のエージェントが同時に操作しているとシーンが勝手に切り替わる。処理中に切り替わると「シーンを開きました」で戻るだけで壊れはしないが、1 シーンずつ「開く→処理→ログ確認」を続けて行う。

戻り値のログを読む: `terrain rects / platforms / ramps / colored-skipped / sprites hidden / painted`。
`colored-skipped` が想定外に多い (地形なのに色が違う) 場合は `extraTerrainNames` に足す。

確認: SceneView を寄せてスクリーンショット
```csharp
var sv = UnityEditor.SceneView.lastActiveSceneView; sv.in2DMode = true;
sv.LookAt(new Vector3(cx, cy, 0), Quaternion.identity, halfHeight, true, true); sv.Repaint();
```
→ `mcp__unity-editor-mcp__capture_scene_view` (save_path は `Screenshots/retile/<Scene>.png`。**実際には `Assets/Screenshots/retile/` に保存される**)。

## 7. 既知のエッジケース

- **坂**: 格子に乗る 2:1 の坂 (CarouselPlaza / FerrisWheelHill の RampUp 26.6°) は ascent_02×2 でタイル化される (ズレ 0.11)。**格子に乗らない坂は着色スプライトのまま**: MaintenanceTunnel RampDown は角度差 3.4° は許容だが両端が y=0.18/1.44 で整数に無く、_03×1 だと上端で 0.41 ズレる → 不採用。UpperReturnSlide (20°, 走行 9.4 上昇 3.42) も _03×3 で 0.4 前後ズレる見込み、Upper 系 14〜15° は角度差 3.4〜4.4° で、UpperPromenade (rise 1.55) は riseErr 0.45 で不採用を確認済み。UpperGarden (rise 1.21) / UpperTerrace (rise 1.55) も同様に不採用になる見込み。**結果として 10 個中タイル化されるのは 26.6° の 2 個だけ**。これらはグレーボックス側の座標を格子に合わせるか、作者に浅い坂素材を頼む。準備室方式の坂タイル (ascent/descent) は残る。
- **坂の下**: 坂タイルの胴体は下1行だけ。その下に地面が無いと坂の下に空が見える (グレーボックスの1厚の板と同じ)。必要なら床矩形を坂の下まで延ばす。
- **ドア上の梁** (1.5幅×1.5厚、上面が天井と同じ): 1セル×1セルになり、下端が 0.5 上がる (出入口の見た目が少し高くなる)。実害なし。
- **天井の上面** に床の飾り帯が出る (RuleTile は上面が空なら床と見なす)。部屋の外なので通常見えない。
- **1.5 幅の壁**: 2セル幅になる。部屋の内側面がコライダーより 0.25〜0.5 ずれることがある。
- **色付き塊は放置**: ボス扉 (0.35,0.5,0.9)、赤系ゲート、スピーカー (黄)、強化壁 (暗赤)、SkillBreakable (橙) は元のまま表示。ここは今後専用素材に差し替える。
- **Burst のコンソールノイズ**: ドメインリロード後に "Timings … While compiling job:" が error/warning として並ぶことがある。タイル処理とは無関係 (中身は Burst のタイミングダンプ)。
- SceneView のスクショに見える細い縦線はエディタのグリッド線。最終確認は Game View / Play で。

## 8. 検証結果 (2026-09-18)

- `CarouselAntechamberScene`: 8矩形 → 255セル (Terrain 1層)。天井を下面基準にしたことで壁と一体化。ボス扉3つはスキップ。
- `PreparationRoomScene`: 4729セルを RuleTile で塗り直し、坂4枚を保持、マーカー8。2段の高台に `角`、天井に `ceiling_01`、坂の下に余計な上面帯なし。
- `CarouselPlazaScene`: 6矩形 + すり抜け床2 (10セル) + 坂1 (ascent_02×2, ズレ0.11) → Terrain 162 / Terrain_y0.50 22 / Platforms 10。
- `MaintenanceTunnelScene`: 12矩形 + すり抜け床2 → Terrain 479。坂 RampDown は不採用 (着色)。
- `UpperPromenadeScene`: 8矩形 → Terrain 208 / Terrain_y0.50 9。坂 RampUp (15°, rise 1.55) は riseErr 0.45 で不採用 (着色)。上昇量チェック導入前は `_03`×2 が上の床から 0.5 突き出ていた → 再実行で修正済み。
- スクショ: `Assets/Screenshots/retile/CarouselAntechamber_v2.png`, `_v2_left.png`, `PreparationRoom_left.png`, `PreparationRoom_slope_v2.png`, `PreparationRoom_upperRight.png`, `CarouselPlaza_ramp_v2.png`, `MaintenanceTunnel_ramp.png`
