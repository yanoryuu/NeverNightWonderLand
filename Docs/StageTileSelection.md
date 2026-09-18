# ステージタイル素材の選定 (旧26シーン → 新タイル素材)

対象: `Assets/Scenes/GameField_old/*.unity` の全26シーン。
新素材: `Assets/LocalData/StageAsset/tile_floor/` + タイルアセット `Assets/LocalData/StageAsset/Tiles/*.asset`。

この文書は **「どのシーンにどの素材族を割り当てるか」** だけを決める。
幾何 → タイルの変換ルール本体、および実際のシーン編集は別担当。

---

## 0. 前提 (YAMLと素材の実測で確認済み)

| 事項 | 実測値 | 出典 |
|---|---|---|
| グリッド | `Grid.m_CellSize = {1,1,0}` = **1セル = 1ワールドユニット** | 各シーンの Grid コンポーネント |
| タイル解像度 | 1セル = 546px / PPU 546 | PNG実寸 + `nevernight-tile-grid` メモリ |
| 床上面の座標 | グレーボックスの床上面はほぼ全て **整数 y** に乗っている (例: CarouselPlaza FloorA 中心 y=-1・厚み2 → 上面 y=0) | geo解析 |
| 既存タイルマップ | 13シーンに Grid/Tilemap が既にある。うち **PreparationRoom は完全にタイル化済み (4733セル)**、他11シーンは1種類のタイルで床上面だけをなぞった**試し塗り** (39〜244セル)、LowerChainVault は0セル | `m_TileIndex` カウント |
| 既存タイルマップの参照先 | **削除済みの旧タイル** (`straight_mid` 145枚 / `straight_mid_big` 9 / `corner_edge` 12 / `stagetile_wall_01` 32 / `stagetile_coverbox` **4389** / `stagetile_ceiling_01` 139 / `stagetile_ascent_03` 4 / `stagetile_floor_01` 1)。試し塗り11シーンは全て `straight_mid` 単独 | guid逆引き + `git grep` |

### 素材の実測寸法

| 素材 | 実寸(px) | セル換算 | 下面 |
|---|---|---|---|
| `stagetile_floor_01` | 546x546 | 1 x 1 | 開放 (端キャップ無し) |
| `stagetile_floor_02` | 1092x546 | 2 x 1 | 開放 |
| `stagetile_floor_03` | 546x364 | 1 x 2/3 | 開放 |
| `floor_04` l/m/r | 546x546 | 1 x 1 | **開放** (胴体が下端まで続く) |
| `floor_05` l/m/r | 1092x546 | 2 x 1 | **開放** |
| `floor_06` l/m | 546x364 | 1 x 2/3 | 開放 |
| `floor_06` r | 546x546 | 1 x 1 のキャンバス。絵は**上2/3のみ**+右上が面取り | 開放 |
| `floor_07` l/m/r | 546x569〜572 | 1 x 約1.04 | **装飾帯あり** (上面ブロックを暗くした「窓の影」) |
| `floor_08` l/m/r | 546x433〜435 | 1 x 約0.79 | **装飾帯あり** (細い3分割の暗帯) |
| `stagetile_wall_01` / `ceiling_01` / `coverbox` | 546x546 | 1 x 1 | — |
| `角` / `平角` | 546x546 / 364x364 | 1x1 / 2/3x2/3 | — |
| `ascent_01` / `descent_01` | 546x1092 | 1セル走行で**1セル上昇 = 45.0°** | — |
| `ascent_02` / `descent_02` | 1092x1092 | 2セル走行で1セル上昇 = **26.6°** | — |
| `ascent_03` / `descent_03` | 1638x1092 | 3セル走行で1セル上昇 = **18.6°** | — |

坂の角度は PNG の不透明上端を最小二乗フィットして実測 (0.996 / 0.500 / 0.337 px/px)。
**坂タイルは3種とも「1セル上昇」で角度だけが違う** ので、グレーボックスの坂は角度で選べばよい。

---

## 1. 選定方針

### 1-1. 構造の作り分け

- **胴体は `coverbox` で塗り潰し、上面1段だけを床族にする。**
  準備室の既存タイルマップが実際にこの構成 (coverbox 4389枚に対し上面床 145枚)。新素材でも同じ作法を踏襲する。
- **`l` / `r` の端キャップは「床が空中で途切れる所」だけに使う。** 壁に突き当たる所・次の床と連続する所は `m` のまま。
- **下面が見えるかどうかで族を選び分ける** (これが今回の素材の一番大きな分かれ目):
  - 下面が**見えない**床 (地面に接する床・厚い土台・アリーナの床) → **`floor_04` / `floor_05`** (下面が開放で、下は coverbox で埋める前提)
  - 下面が**見える**床 (2階床・庇・屋根・多層デッキ・バルコニー) → **`floor_07`** (下面の装飾帯が「見上げた時の窓の影」に見える)
  - 宙に浮く**薄い足場・棚** → **`floor_08`** (0.8セル厚 + 細い下面帯)
  - さらに薄く見せたい所・小さい1セルの欠片 → **`floor_03` / `floor_06`** (2/3セル)

### 1-2. 族の使い分け (シーン種別)

| シーン種別 | 主床 | 理由 |
|---|---|---|
| 拠点・長い一本道の通路 | **`floor_05`** (2セル幅) | 継ぎ目が半分になり、落ち着いて読める。大広間70幅・ダクト52幅などに効く |
| ボスアリーナ (長方形・完全平坦) | **`floor_04`** | 1セル刻みの均一な反復で「戦うための無地の床」にする。装飾差で足場を読み違えない |
| 地上ルート (空・坂) | **`floor_05`** 基調 + 端は `floor_04` | 横に長く高低差が少ない |
| 屋根伝い / 多層デッキ | **`floor_07`** | 下の層・下の道から下面が見えるのでここが主役 |
| 縦坑・シャフト | **`floor_04`** | 床が短く分断されるので1セル族が素直 |

### 1-3. すり抜け床 (PlatformEffector2D)

- **すり抜け床は地形タイルマップに混ぜない。** `TilemapCollider2D` は一部セルだけに Effector を効かせられないため、
  **別 Tilemap (専用 `TilemapCollider2D` + `CompositeCollider2D` + `PlatformEffector2D`)** か、
  **従来どおり個別 GameObject + スライスしたスプライト** のどちらかで保持する。
- 表現は原則 **`floor_08` (l/m/r)**。下面に装飾帯があり、宙に浮いている足場として一番読める。
- 幅2セル以下の細い足場 (`GapOW` 2.0 / `HoleLadder` 1.6 など) は `floor_08_l + floor_08_r` の2枚、
  もしくはキャップ無しの **`floor_03`** 1枚で済ませる。
- 「下をくぐる」前提のすり抜け床 (UpperGarden の東屋の屋根など) だけは **`floor_07`** を使うと下から見上げた絵になる。
- **注意**: グレーボックスのすり抜け床は厚み 0.4 (一部 0.6) だが、最も薄い素材でも 2/3セル = 0.67。
  見た目は必ず厚くなる。**コライダーは 0.4 のまま据え置き**、絵だけ厚くするか、当たり判定を 0.67 に合わせるかは要判断 (→ 4章)。

### 1-4. 囲い (天井・壁)

- **地下 (`enclosed: yes`)**: 天井の下面に **`ceiling_01`**、側壁に **`wall_01`**、その外側は `coverbox`。床と壁の出会う外角は **`角`**、2/3高の床に付く角は **`平角`**。
- **地上 (`enclosed: no`)**: 天井タイルを置かず空を残す。左右の `LeftBackstop` / `RightBackstop` は画面外の見切りなので、
  `wall_01` を貼るか無地のままにするかはカメラ範囲次第 (表では `partial` と記す)。
- 「地上だが奥に小部屋がある」シーン (UpperBossArena / LowerBossArena の Chamber) は、その部屋だけ `ceiling_01` で囲う。

### 1-5. 坂

グレーボックスの坂は回転させた Square なので、**実測角度に最も近い坂タイルを選ぶ**。

| グレーボックスの実測角 | 採用タイル | 該当シーン |
|---|---|---|
| ±26.6° | `ascent_02` / `descent_02` (26.6°、完全一致) | CarouselPlaza, FerrisWheelHill |
| ±14〜21.8° | `ascent_03` / `descent_03` (18.6°) | MaintenanceTunnel, UpperPromenade, UpperGarden, UpperTerrace, UpperReturnSlide |
| 急な短い繋ぎ (段差1セルを斜めにしたい所) | `ascent_01` / `descent_01` (45°) | 任意 (現状グレーボックスには該当なし) |

坂タイルは 2セル高キャンバスなので、下半分は `coverbox` と重ねて設置する。

---

## 2. シーン別の割り当て (26シーン)

`floorFamily` = 主床 / `platformFamily` = 高台・小足場 / `oneWayFamily` = すり抜け床の表現。
「実測」はシーンYAMLから読めた値、「推定」は設計書・メモリからの意図。

### 2-1. チュートリアル群 (5)

| シーン | 名称・テーマ | floorFamily | platformFamily | oneWayFamily | enclosed | walls | 坂 | 備考 |
|---|---|---|---|---|---|---|---|---|
| PreparationRoomScene | 準備室 (地下・訓練室) | `floor_04` + 長い直線に `floor_05` | `floor_08` | (現状なし) | yes | `wall_01` | `ascent_03` | **唯一タイル化済み** (4733セル)。ただし参照タイルが削除済みで欠損状態 → 新族へ貼り替えが必要。旧構成は coverbox 4389 / 上面床 145 / ceiling 139 / wall 32 / ascent_03 4 |
| MaintenanceTunnelScene | 地下整備通路 | `floor_05` (床83幅の一本道) | `floor_04` (Plateau 9x4.5 の塊) / 台座は `floor_06` | `floor_08` | yes | `wall_01` | `descent_03` (実測 -21.8°, 幅3.4) | 天井85幅。中2階すり抜け×2 (4.0/5.0幅)。資材箱の壁は箱プレハブなのでタイル化しない |
| ServiceShaftScene | 搬入昇降路 (縦) | `floor_04` | `floor_08` | `floor_08` | **partial** (下部の通路のみ天井あり / 上部の立坑〜地上は空) | `wall_01` (ShaftWallL/R) | なし | **縦シーン** (高さ33.5)。P1〜P9 のすり抜け足場 3x0.6 が9枚。上端 SurfaceFloor から地上へ抜けるので、そこから上は天井なし |
| CarouselPlazaScene | カルーセル広場 (地上) | `floor_05` (FloorA 21 / FloorB 22) | `floor_04` (Plateau) | `floor_08` (CarouselTier1/2) | no | partial (右壁のみ) | `ascent_02` ×2 (実測 26.6°, 幅4.6 → 走行4.1/上昇2.1) | 試し塗り済み (49セル, y=-1 に41枚 / y=1 に8枚)。段の2層は「カルーセルの段」なので `floor_08` の下面帯が効く |
| FerrisWheelHillScene | 観覧車の丘 (地上) | `floor_04` + `floor_05` | `floor_06` (低い段差 Step1/Step2) | `floor_08` (WheelOneWay×4, 3.5x0.4) | no | partial | `ascent_02` ×2 (実測 26.6°, 幅4.6) | 本編への正規ルートの起点 (頂上 TowerTop(1) → 大広間)。すり抜け4枚のタワーは宙に浮くので `floor_08` 必須 |

### 2-2. メリーゴーランド本編 (13)

| シーン | 名称・テーマ | floorFamily | platformFamily | oneWayFamily | enclosed | walls | 坂 | 備考 |
|---|---|---|---|---|---|---|---|---|
| CarouselHallScene | [1] 回転木馬の大広間 (拠点) | **`floor_05`** (床66幅・天井70幅) | **`floor_07`** (BalconyL/R 6x0.5 は下から見上げる) | `floor_08` (BalconyOW×4, 3x0.4) | yes | `wall_01` | なし | 安全地帯。中央 AxisPillar (2.5x6) は `wall_01`+`角`。StepL/R は端の緩い段差 → `floor_06`。試し塗り84セル |
| CarouselGateArenaScene | [2] 木馬の門 (中ボス①) | **`floor_04`** (床52幅・完全平坦) | `floor_08` (SpeakerShelf 7x0.5) | なし | yes | `wall_01` | なし | 長方形アリーナ 56x15。BossGateL/R は開閉する扉なのでタイル化しない (個別オブジェクト維持) |
| CarouselJunctionScene | [3] 昇降分岐塔 (縦・分岐) | `floor_04` | `floor_08` | `floor_08` (P1〜P9, 3x0.4 が9枚) | yes | `wall_01` | なし | **縦シーン** (34 x 29.5)。上下2方向+右(回転軸の内部)へ3分岐。TowerCap/TopCeil/MidCorridorCeil の3枚の天井 |
| UpperPromenadeScene | [4] 木馬の園路 (地上) | `floor_05` (FloorA 36 / FloorB 29) | `floor_04` | なし | **no (空)** | partial | `ascent_03` ×2 (実測 15.0°, 幅6.0) | 平坦基調。地形要素が9個だけの素直なシーン。浮遊敵の初見せ |
| UpperGardenScene | [5] 花壇の遊歩道 (地上) | `floor_05` (FloorA24/FloorB17/FloorC19) | `floor_08` (HighLedge 4x0.6) / GardenStep は `floor_06` | **`floor_07`** (PavilionRoof 6x0.4 = 東屋の屋根。下をくぐるので下面装飾が要る) | no | partial | `ascent_03` + `descent_03` (実測 ±14.0°, 幅5.0) | WideSlash ピックアップのある高台 (2段ジャンプ必須) |
| UpperTerraceScene | [6] テラス通り (地上) | `floor_05` (FloorA18/FloorB13/FloorC28.2 の上り基調) | `floor_06` (Step1 等の低段差) | なし | no | partial | `ascent_03` ×2 (実測 15.0°, 幅6.0) | FloorC が厚み2.5 → 土台は coverbox |
| UpperRooftopScene | [7] 屋根の上の道 (地上) | **`floor_07`** ← 本シーンの主役 | `floor_08` (SkyLedge 3x0.6) | `floor_03` または `floor_08_l+r` (GapOW 2.0x0.4 が4種) | no | no (両端backstopのみ) | なし | Roof1〜Roof5 は厚み2.0〜3.5 の独立した屋根の塊で、**横と下面が空に露出する** → `floor_07` の下面装飾帯がそのまま「屋根裏の窓」になる。下の UnderFloor (75.9x1.0, y=-3.55) は落下防止の見えない床 → `floor_01` かタイル化せず放置 |
| UpperBossArenaScene | [8] 空中木馬の間 (上中ボス) | **`floor_04`** (床46幅・平坦) | `floor_08` (PlatformL/R 4x0.6) | なし | **no** (本体は空)。奥の小部屋 Chamber のみ `ceiling_01` | partial | なし | 浮遊ボス戦。低い足場2枚はボスに届くための台なので、`floor_08` で「浮いている感」を出す |
| LowerGearShaftScene | [9] 歯車の縦坑 (地下・縦) | `floor_04` | `floor_08` | `floor_08` (D1〜D5, 3x0.4) | yes | `wall_01` | なし | **縦シーン** (33 x 28)。EntryCeil/ExitCeil/ShaftCap の3天井 |
| LowerChainVaultScene | [10] 鎖の大空洞 (地下) | `floor_05` (床58幅・天井62幅) | **`floor_07`** (UpperDeck 20x0.6 = 上段。下段から見上げる二層構造) | `floor_08` (DeckOW 3x0.4) | yes | `wall_01` | なし | Stair1〜3 は段差 → `floor_06` + `平角`。HighNook (3x0.6) は `floor_08`。Tilemapはあるが未塗り |
| LowerPistonHallScene | [11] ピストンの回廊 (地下) | `floor_05` (床61幅・天井78幅) | **`floor_08`** (Step1〜6 の幅3小足場が主役) | `floor_03` (HoleLadder 1.6x0.4 の梯子) | yes | `wall_01` | なし | 最大のシーン (79 x 40)。**下層あり** (Floor(1) 57.6幅 @ y=-8.7)。SlamBlock×6 は `SkillBreakable` なのでタイル化せず個別オブジェクトのまま |
| LowerFlywheelPitScene | [12] 弾み車の底 (地下・縦) | `floor_04` | `floor_08` | `floor_08` (D1〜D4, 3x0.4) | yes | `wall_01` | なし | **縦シーン** (37 x 23)。切り返しジャンプの縦ジグザグ |
| LowerBossArenaScene | [13] 大歯車の間 (下中ボス) | **`floor_04`** (床52幅・平坦) | **`floor_04`** (SidePlatformL/R は 5x2 の実体ブロック) | なし | yes | `wall_01` | なし | 両端の一段高い足場は厚み2 → 薄い族ではなく `floor_04` + coverbox。奥に別室 (ChamberFloor/ChamberCeil) |

### 2-3. 後半コンテンツ・帰還路 (8)

| シーン | 名称・テーマ | floorFamily | platformFamily | oneWayFamily | enclosed | walls | 坂 | 備考 |
|---|---|---|---|---|---|---|---|---|
| CarouselSpireScene | 回転軸の内部 (縦・関門) | `floor_04` | `floor_08` (Ledge1〜5, 3x0.6) | なし | yes | `wall_01` (ShaftWallL/R が26〜28高) | なし | **縦シーン** (40 x 36.5)。三重扉 (SpeakerDoor1-3) と `UpgradeWall` は可変オブジェクトなのでタイル化しない |
| CarouselMazeScene | 歯車迷宮 (4層デッキ) | `floor_05` (床71幅・天井71幅) | **`floor_07`** ← 本シーンの主役 | なし | yes | `wall_01` | なし | DeckA〜I (幅12〜20 x 厚み1.0) が4層に重なる。**どのデッキも下の層から下面が見える** → `floor_07` 一択。Step_* (3〜4x0.6) は `floor_08`。縦壁 Baffle_* は `wall_01`。試し塗り244セル |
| CarouselAntechamberScene | 三重扉の間 | `floor_05` (床50幅・天井54幅) | なし | なし | yes | `wall_01` | なし | 地形は床+天井+壁だけの素直な部屋 (11要素)。BossDoorA/B/C は `FlagDoor` なのでタイル化しない |
| CarouselBossArenaScene | 大回転木馬・中枢 (エリアボス) | **`floor_04`** (床58幅・天井62幅) | `floor_08` (PlatformL/R 5x0.6) | なし | yes | `wall_01` | なし | 62 x 18 の大長方形。壁が18高あるので `wall_01` の縦積みが目立つ |
| UpperReturnSlideScene | 大すべり台 (上・帰還路) | `floor_04` | `floor_04` (Landing1/2) | なし | no | partial | **`descent_03` ×3枚/本** (Slide1〜3、実測 -20.0°, 幅10.0 → 走行9.4/落差3.4 ≒ 3セル走行1セル落差×3) | 滑り降りる一方通行の帰還路。坂タイルの主役シーン |
| UpperReturnBalconyScene | 観覧バルコニー (上・帰還路) | `floor_04` | 段下がりの端に **`floor_06_r`** | なし | no | partial | なし (段差1.0ずつ) | FloorA(26幅 y-1) → FloorB(14 y-2) → FloorC(14 y-3) と1ずつ下る階段状。`floor_06` の「r だけ1セル高」が段下がりの端そのもの |
| LowerReturnConveyorScene | 搬出コンベア坑 (下・帰還路) | `floor_04` | `floor_08` | `floor_08` (C1〜C5 + C4(1), 3x0.4 が6枚) | yes | `wall_01` | なし | 下段(LowCeil 22幅)と上段(UpperCeil 16.6幅)の2段構成、縦寄り (41 x 21.5) |
| LowerReturnDuctScene | 送風ダクト (下・帰還路) | **`floor_05`** (床52幅+天井56幅の一本道) | なし | なし | yes | `wall_01` | なし | 地形11要素の素直なダクト。Baffle1〜3 は天井から下がる邪魔板 → `wall_01` + `ceiling_01` |

---

## 3. 機械可読ブロック (変換ルーチン用)

```json
{
  "_schema": {
    "floorFamily": "主床に使うタイル族",
    "platformFamily": "高台・小足場に使う族",
    "oneWayFamily": "すり抜け床 (PlatformEffector2D) の見た目。null = 該当なし",
    "enclosed": "true=天井を ceiling_01 で閉じる / false=空 / \"partial\"=一部のみ",
    "walls": "yes=側壁を wall_01 で覆う / partial=見切りのみ / no=不要",
    "slopes": "使用する坂タイル (空配列 = 坂なし)",
    "notes": "備考"
  },
  "_conventions": {
    "cellSize": 1.0,
    "pixelsPerUnit": 546,
    "bodyFill": "stagetile_coverbox",
    "ceiling": "stagetile_ceiling_01",
    "wall": "stagetile_wall_01",
    "outerCorner": "角",
    "smallCorner": "平角",
    "slopeAngles": { "ascent_01": 45.0, "ascent_02": 26.6, "ascent_03": 18.6, "descent_01": -45.0, "descent_02": -26.6, "descent_03": -18.6 },
    "openBottomFamilies": ["floor_04", "floor_05", "floor_01", "floor_02", "floor_03", "floor_06"],
    "finishedBottomFamilies": ["floor_07", "floor_08"],
    "oneWayMustStayOffTerrainTilemap": true
  },

  "PreparationRoomScene":      { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": ["ascent_03"], "notes": "唯一タイル化済み(4733セル)だが参照タイルが削除済みで欠損。新族へ貼り替えが必要。長い直線は floor_05 を併用" },
  "MaintenanceTunnelScene":    { "floorFamily": "floor_05", "platformFamily": "floor_04", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": ["descent_03"], "notes": "床83幅の一本道+天井85幅。実測坂 -21.8°/幅3.4。中2階すり抜け2枚。台座は floor_06" },
  "ServiceShaftScene":         { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": "floor_08", "enclosed": "partial", "walls": "yes",    "slopes": [], "notes": "縦シーン(高さ33.5)。下部通路のみ天井、上部立坑〜地上は空。すり抜け足場 P1-P9 の9枚" },
  "CarouselPlazaScene":        { "floorFamily": "floor_05", "platformFamily": "floor_04", "oneWayFamily": "floor_08", "enclosed": false,    "walls": "partial", "slopes": ["ascent_02"], "notes": "地上。実測坂 26.6°/幅4.6 → ascent_02 を2枚。試し塗り49セル済み" },
  "FerrisWheelHillScene":      { "floorFamily": "floor_04", "platformFamily": "floor_06", "oneWayFamily": "floor_08", "enclosed": false,    "walls": "partial", "slopes": ["ascent_02"], "notes": "地上。すり抜け4枚のタワー(3.5x0.4)。頂上が大広間への正規ルート。長い床は floor_05 併用" },

  "CarouselHallScene":         { "floorFamily": "floor_05", "platformFamily": "floor_07", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "拠点。床66/天井70幅。バルコニーは下から見上げるので floor_07。中央 AxisPillar は wall_01+角。端の段差は floor_06" },
  "CarouselGateArenaScene":    { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "中ボス①。56x15の長方形・完全平坦。BossGateL/R は開閉オブジェクトなのでタイル化しない" },
  "CarouselJunctionScene":     { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "縦シーン(34x29.5)。3分岐。すり抜け P1-P9 の9枚。天井3枚(TowerCap/TopCeil/MidCorridorCeil)" },
  "UpperPromenadeScene":       { "floorFamily": "floor_05", "platformFamily": "floor_04", "oneWayFamily": null,      "enclosed": false,     "walls": "partial", "slopes": ["ascent_03"], "notes": "地上・空。実測坂 15.0°/幅6.0 → ascent_03 を2枚。地形9要素の最小構成" },
  "UpperGardenScene":          { "floorFamily": "floor_05", "platformFamily": "floor_08", "oneWayFamily": "floor_07", "enclosed": false,    "walls": "partial", "slopes": ["ascent_03", "descent_03"], "notes": "地上。実測坂 ±14.0°/幅5.0。東屋の屋根(PavilionRoof)は下をくぐるので oneWay に floor_07。GardenStep は floor_06" },
  "UpperTerraceScene":         { "floorFamily": "floor_05", "platformFamily": "floor_06", "oneWayFamily": null,      "enclosed": false,     "walls": "partial", "slopes": ["ascent_03"], "notes": "地上・上り基調。実測坂 15.0°/幅6.0。FloorC は厚み2.5" },
  "UpperRooftopScene":         { "floorFamily": "floor_07", "platformFamily": "floor_08", "oneWayFamily": "floor_03", "enclosed": false,    "walls": "no",      "slopes": [], "notes": "屋根伝い。Roof1-5 は厚み2.0-3.5の独立した塊で下面が空に露出 → floor_07 が主役。GapOW は幅2.0なので floor_03 か floor_08_l+r。UnderFloor(75.9x1.0)は落下防止の裏床" },
  "UpperBossArenaScene":       { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": null,      "enclosed": "partial", "walls": "partial", "slopes": [], "notes": "上中ボス(浮遊)。本体は空、奥の Chamber のみ ceiling_01 で囲う。足場2枚は 4x0.6" },
  "LowerGearShaftScene":       { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "縦シーン(33x28)。すり抜け D1-D5。天井3枚" },
  "LowerChainVaultScene":      { "floorFamily": "floor_05", "platformFamily": "floor_07", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "二層の大空洞。UpperDeck(20x0.6)は下段から見上げるので floor_07。Stair1-3 は floor_06+平角" },
  "LowerPistonHallScene":      { "floorFamily": "floor_05", "platformFamily": "floor_08", "oneWayFamily": "floor_03", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "最大シーン(79x40)。下層あり(Floor(1) @ y=-8.7)。Step1-6の幅3小足場が主役。SlamBlock×6 は SkillBreakable なのでタイル化しない。HoleLadder は幅1.6" },
  "LowerFlywheelPitScene":     { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "縦シーン(37x23)。切り返しジャンプ。すり抜け D1-D4" },
  "LowerBossArenaScene":       { "floorFamily": "floor_04", "platformFamily": "floor_04", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "下中ボス(地上)。両端の SidePlatform は 5x2 の実体ブロックなので floor_04+coverbox。奥に別室あり" },

  "CarouselSpireScene":        { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "縦シーン(40x36.5)。Ledge1-5(3x0.6)。三重扉と UpgradeWall は可変オブジェクトなのでタイル化しない" },
  "CarouselMazeScene":         { "floorFamily": "floor_05", "platformFamily": "floor_07", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "4層デッキ(DeckA-I, 幅12-20 x 厚み1.0)。全デッキの下面が見えるので floor_07 一択。Step_* は floor_08、Baffle_* は wall_01。試し塗り244セル済み" },
  "CarouselAntechamberScene":  { "floorFamily": "floor_05", "platformFamily": null,      "oneWayFamily": null,       "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "床50+天井54幅の素直な部屋(地形11要素)。BossDoorA/B/C は FlagDoor なのでタイル化しない" },
  "CarouselBossArenaScene":    { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": null,      "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "エリアボス。62x18の大長方形。壁18高なので wall_01 の縦積みが目立つ" },
  "UpperReturnSlideScene":     { "floorFamily": "floor_04", "platformFamily": "floor_04", "oneWayFamily": null,      "enclosed": false,     "walls": "partial", "slopes": ["descent_03"], "notes": "帰還路(上)。Slide1-3 は実測 -20.0°/幅10.0 → descent_03 を3枚/本。坂タイルの主役シーン" },
  "UpperReturnBalconyScene":   { "floorFamily": "floor_04", "platformFamily": "floor_06", "oneWayFamily": null,      "enclosed": false,     "walls": "partial", "slopes": [], "notes": "帰還路(上)。FloorA/B/C が y=-1/-2/-3 と1ずつ下る階段状。段下がりの端に floor_06_r" },
  "LowerReturnConveyorScene":  { "floorFamily": "floor_04", "platformFamily": "floor_08", "oneWayFamily": "floor_08", "enclosed": true,     "walls": "yes",     "slopes": [], "notes": "帰還路(下)。下段/上段の2段(41x21.5)。すり抜け C1-C5+C4(1) の6枚" },
  "LowerReturnDuctScene":      { "floorFamily": "floor_05", "platformFamily": null,      "oneWayFamily": null,       "enclosed": true,      "walls": "yes",     "slopes": [], "notes": "帰還路(下)。床52+天井56幅の一本道。Baffle1-3 は天井から下がる邪魔板 → ceiling_01+wall_01" }
}
```

---

## 4. 判断に迷った点・要確認事項

1. **すり抜け床の厚みが素材と合わない (最重要)**
   グレーボックスのすり抜け床は厚み **0.4** (ServiceShaft のみ 0.6)。一方、最も薄い素材でも **2/3セル = 0.67** (`floor_03` / `floor_06_l,m`)、標準の `floor_08` は **0.79**。
   → 「絵だけ厚くしてコライダーは 0.4 のまま」にするか、「コライダーも 0.67/0.79 に揃える」かの判断が要る。後者は跳び上がり高低差 3.0 の設計余裕 (メモリ `nevernight-tutorial-stages`) に影響しうる。**現状は前者を推奨**。

2. **`floor_06_r` の絵の位置**
   `floor_06_r` は 546x546 のキャンバスだが、**絵は上2/3 (約364px) にしかなく、下1/3 が透明**。PPU546 / Center ピボットで取り込むと、`floor_06_l/m` (546x364) と上面の高さが 1/6 セル (約91px) ずれる可能性がある。オフセットの実測確認が必要。

3. **`floor_07` の高さが 1セルちょうどでない**
   `floor_07` は 569〜572px (約1.04セル)。メモリ `nevernight-tile-grid` の方針では「高さはセル内で中央寄せになるだけなので実害なし」とされているが、**下面の装飾帯が 1セル下のタイルに 0.04セル分かぶる**。多層デッキ (CarouselMaze / LowerChainVault / UpperRooftop) で目立つかどうか、実機で要確認。

4. **既存タイルマップの扱い**
   - `PreparationRoomScene` は旧タイル (`straight_mid` 等、**working tree で削除済み**) を 4733セル参照しており、現状は欠損タイルになっているはず。**新族への貼り替えが必要**。旧構成の比率 (coverbox 4389 : 上面床 145 : ceiling 139 : wall 32) は変換ルールの良い雛形になる。
   - 他11シーン (`CarouselAntechamber` / `CarouselBossArena` / `CarouselGateArena` / `CarouselHall` / `CarouselJunction` / `CarouselMaze` / `CarouselPlaza` / `CarouselSpire` / `FerrisWheelHill` / `LowerBossArena` / `MaintenanceTunnel`) には、削除済みの `straight_mid` 単独で床上面をなぞった**試し塗りが残っている** (39〜244セル)。本番変換の前に消すか、上書きするかを決める必要がある。
   - `LowerChainVaultScene` は Grid/Tilemap があるが未塗り。残り13シーンには Grid 自体が無いので新規作成が要る。

5. **坂タイル `descent_02` の実測**
   `descent_02` は列ごとの単純走査だとほぼ水平に見えたが、上端の最小二乗フィットでは **-26.6° (2セル走行1セル落差)** で `ascent_02` の正しい鏡像だった。キャンバスの余白のとり方が `ascent_02` と違うだけと思われるが、**Tile アセットのオフセット設定を実機で要確認**。

6. **「地上シーンの左右の見切り壁」をどう描くか**
   地上シーン (`UpperPromenade` / `UpperTerrace` / `UpperGarden` / `CarouselPlaza` / `FerrisWheelHill` 等) の `LeftBackstop` / `RightBackstop` は高さ14〜18の透明な見切り壁。空が背景なので `wall_01` を貼ると不自然になりうる。**カメラの可視範囲を見てから決める**。表では `partial` としてある。

7. **厚い実体ブロックの上面族**
   `Plateau` (9x4.5)、`Roof5` (14x3.0)、`SidePlatformL/R` (5x2)、`FloorC` (28.2x2.5) のような「厚い塊」は、上面だけ床族・中身は `coverbox` でよいが、**側面を `wall_01` にするか `coverbox` のままにするか**が未決。屋根 (UpperRooftop) は側面も見えるので `wall_01` + `角` が要りそう。

8. **ボスゲート / 破壊壁 / 扉はタイル化しない前提で書いている**
   `BossGate*`、`SpeakerDoor1-3`、`SpireUpgradeWall`、`SlamBlock1-6` (`SkillBreakable`)、`FlagDoor` は実行時に開閉・破壊されるので、Tilemap ではなく個別 GameObject のまま残す想定。**別素材 (扉・破壊壁の絵) が必要かどうかは未確認**。

9. **ユーザー手編集による重複オブジェクト**
   変換前に整理が要りそうな重複を見つけた (いずれも名前末尾が ` (1)` / ` (2)`):
   - `UpperGardenScene`: `PavilionRoof` と `PavilionRoof (1)` が**完全に同じ位置** (20.0, 3.4, 6x0.4)
   - `UpperRooftopScene`: `GapOW1〜4` がそれぞれ上下2組ある (例 `GapOW1` y=0.9 / `GapOW1 (1)` y=-1.34)
   - `FerrisWheelHillScene`: `Step1 (1)` (縦長 0.92x3.21) / `TowerTop (1)`
   - `LowerPistonHallScene`: `Floor (1)` (57.6x2.23 @ y=-8.7 = 下層の床。これは意図的と思われる) / `Piston4 (2)` (20.18x2.38 @ y=13.66)
   - `LowerReturnConveyorScene`: `C4 (1)`
   意図的なものと事故の区別がついていない。**変換前にユーザー確認を推奨**。
