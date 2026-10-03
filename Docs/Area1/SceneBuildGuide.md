# エリア1 シーン組み立てガイド (ビルダー向け)

前提資料: `Area1Build.md` (仕様) / `Gimmicks.md` (ギミックのフィールド) / `area1_layout.json` (部屋・通路・意図) / `StageTileRules.md` (タイル)。
このガイドは「残りのシーンを、同じ道具で同じ品質に組む」ための手順書。パイロットとして **A1_T1 / A1_T2** をこの手順で作成済み
(レシピ: `Docs/Area1/SceneScripts/build_A1_T1.cs`, `build_A1_T2.cs`)。

---

## 1. 道具の一覧

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Editor/StageBuild/Area1StageBuilder.cs` | シーン組み立て API (静的メソッド, 名前空間 `NeverNight.StageBuild`) |
| `Assets/Scripts/Editor/StageBuild/Area1Layout.cs` | `area1_layout.json` の読み込み (更新されれば自動で読み直す) |
| `Assets/Scripts/Editor/StageBuild/Area1AssetPrep.cs` | 素材準備 (インポート設定 / Tile アセット / 観覧車プレハブ)。何度流してもよい |
| `Assets/Scripts/Stage/ParallaxLayer.cs` | 背景の視差 (ランタイム。プロジェクトに既存の視差部品が無かったので追加) |
| `Assets/Scripts/Common/StageInfo.cs` | ステージ側のデータ (部屋名 `_areaName`)。UI はステージに置かず UI シーン (`StageUI` / `ShopUI`) に分離 (§11)。Awake でシーン開始のリセット (GamePause.Reset / GameSession.ResetRun) |
| `Assets/Prefabs/Area1/A1_FerrisWheel.prefab` | 大観覧車 (背景、当たり無し)。ルート = ワールド (54,252)、外周半径 90 |
| `Assets/LocalData/StageAsset/Tiles/Area1/a1_*.asset` | ひび壁 / 落下・突進・大ジャンプブロック / トゲ / 梯子 / コンベアの Tile |
| `Assets/LocalData/StageAsset/Tiles/Area1/A1TerrainRule.asset` | **エリア1 専用の地形 RuleTile** (共有 `Rules/StageTerrainRule` の複製: 規則・当たりは同じ、内部の既定出力は絵なし、縁は切り抜き版)。旧シーンの共有ルールは触らない |
| `Tiles/Area1/a1_rock_tex_r{row}_c{col}.asset` (16) / `a1_rock_seal.asset` | 岩肌のスライス (当たり無し、色ロック無し) / 岩の外周用の絵なし・当たり Grid のタイル |
| `Area1Gen/rock/` (+ `rock/edges/`) | 岩肌 (`rock_tex*`)・岩の飾り (`rock_motif_*`, `decal_*`)。`edges/edge_*.png` は共有の縁タイルの「内側のベタ塗り」を透明にした生成物 (`Area1AssetPrep.MakeEdgeCutouts`) |
| `Assets/Scripts/Stage/WheelSpin.cs` | (polish A で追加、見た目だけ) 毎フレーム z 軸で回し、`_upright` の子 (吊られたゴンドラ) は直立のままにする。中景の観覧車 (`FerrisMidground`) に使う |
| `Assets/Scripts/Stage/BlinkingLight.cs` / `ConveyorBeltVisual.cs` / `SpriteWobble.cs` | (D 区画で追加、見た目だけのランタイム部品) 点滅する灯 (子の SpriteRenderer のアルファを周期で、`_phase` をずらすと流れる) / ベルトの矢印の帯を流れ (反転フラグ込み) に合わせて流す・ローラーを回す / 蒸気・水面のゆらぎ (scale と位置を sin で) |
| `Area1Gen/fx/` (`glow` / `water` / `water_surface` / `shade`) | 手続き生成の効果素材 (`Area1AssetPrep.PrepareFx()`, PrepareAll にも含む。白で作ってあるので色を乗せて使う)。光暈 / 継ぎ目なしの水 / 波打つ水面の線 / 下ほど濃い黒の縦グラデーション |
| `Area1Gen/polish/` (+ `mat/`) | (polish B) 部品の生成素材 (manifest.json: pivot / tileable)。噴水・水と油・蒸気・音楽堂の壁・レバーの柄と札・歯車の踏み板・電球の連なり・遠くのゴンドラ・外周の弧。`Area1AssetPrep.PreparePolish()` (PrepareAll にも含む) がインポート (tileable は wrapMode Repeat) と `mat/` のマテリアル (`A1_Additive` / `A1_WaterJet` / `A1_SteamRise` / `A1_SurfaceDrift` / `A1_WaterBody` / `A1_OilBody`) を作る |
| `Assets/Scripts/Stage/Shaders/A1SpriteScroll.shader` | (polish B) SpriteRenderer 用の unlit シェーダー: `_Scroll` (UV/秒) で継ぎ目なし素材を流す + `_SrcBlend/_DstBlend` で通常/加算。Tiled 描画でも 1 タイル = UV 0..1 なので継ぎ目は出ない (テクスチャは Repeat 必須) |
| `Assets/Scripts/Stage/FadeByCameraHeight.cs` | (polish B, 見た目だけ) カメラの高さで子の SpriteRenderer をまとめて薄くする (BlinkingLight の子はそのフレームの値に掛ける)。`FerrisMidground` が使う |
| `Docs/Area1/SceneScripts/*.cs` | シーンごとのレシピ (run_script で流す。Assets 外なのでコンパイル対象外) |

## 2. エディタの動かし方 (Unity CLI)

すべて `unity command <name> --caller plugin --skill unity-cli ...`。**エディタを操作するのは常に 1 エージェントだけ**。作業前に `list_open_scenes` で確認。

### C# を流す: `run_script` を使う (eval_file ではなく)
`eval_file` はエディタ側タイムアウトが **5 秒固定**になる (CLI の `--timeout` は CLI 自身が消費し、eval の `timeout` 引数に届かない)。
シーン 1 枚の組み立ては 5 秒を超えるので **`run_script`** を使う:

```bash
unity command run_script --caller plugin --skill unity-cli \
  --file Docs/Area1/SceneScripts/build_A1_T2.cs --entry BuildT2.Run \
  --timeout_ms 600000 --timeout 620 --format json
```
- `--file` はプロジェクトルート基準の相対パス (scratchpad の絶対パスも可)。ファイルは `public static class X { public static string Run() {...} }` の形。引数は `--args '["A1_T3"]'`。
- 戻り値 (string) が JSON の `data.result.result` に入る。`data.result.diagnostics` にコンパイルエラー。
- **ビルダー (Area1StageBuilder.cs) を書き換えたら `recompile` → `recompile_status` が `completed` になるまで待つ**。run_script はロード済みのエディタアセンブリを参照するので、再コンパイル前は古いコードが走る。
- 短い問い合わせ (5 秒以内) なら `eval --code '...'` / `eval_file --file x.cs` でもよい。

### 1 シーン = 呼び出し 2 回
1. `OpenOrCreate("A1_xx")` … シーンを開く (無ければ作って保存、Build Settings に登録)。**同じ呼び出しでタイルを塗らない** (StageTileRules.md: 開いた直後の同一呼び出しの SetTile が消える現象)。
2. レシピ (`BuildBase` → 中身 → `Finish`)。

```bash
unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/open.cs --entry Open.Run --args '["A1_T3"]'
unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/build_A1_T3.cs --entry BuildT3.Run --timeout_ms 600000 --timeout 620
```

### スクリーンショット
```bash
unity command run_script ... --file Docs/Area1/SceneScripts/frame.cs --entry Frame.Room --args '["T2"]'
unity command capture_scene_view --caller plugin --skill unity-cli --width 1600 --height 900 --save_path Screenshots/Area1/T2_v1.png
```
保存先は実際には `Assets/Screenshots/Area1/` (gitignore 対象)。**撮ったら必ず Read で画像を見る**。

## 3. API (Area1StageBuilder)

座標は全シーン共通のワールド整数座標。矩形は `(x0, y0, x1, y1)` = セル `[x0,x1) × [y0,y1)`。

### シーン・骨組み
| メソッド | 内容 |
|---|---|
| `OpenOrCreate(sceneId)` | `Assets/Scenes/Area1/<id>.unity` を開く/作る。Build Settings 登録 |
| `BuildBase(sceneId)` | **シーンを丸ごと作り直す** (ルートを全削除)。骨組み (PlayerSceneBootstrap=`["PlayerScene"]` / StageInfo(`_areaName`=シーン名。UI は置かない §11) / DamageFloaterSpawner / CameraLimit(min 部屋yMin+2) / Grid: Backfill・Terrain・Deco・Platforms) + `Carve` + `PlaceTransitions` + `Background` |
| `Carve(sceneId)` | 岩 (外周 6, 当たりあり, RuleTile) で埋め、部屋・同一シーン通路・他シーン通路 (+外へ 3 延長) を掘る。屋外 (`outdoor`) の部屋は上を空まで開ける。外周 2 マスは絵なし・当たりありの `a1_rock_seal` (縁飾りが出ない)、さらに外へ 26 マス Backfill (Finish で岩肌になる) |
| `PlaceTransitions(sceneId)` | (レイアウトで `scene:null` の移動装置の部屋 E1/WL への通路は**掘るだけ**で遷移ゾーン・スポーンを置かない = `BoundaryExit.Transport`。`TransportAt` + `Spawn` で自前配置。Validate は TransportInteractable の先シーン/スポーンも検査する) 他シーン通路ごとに `ExitTo<先>` (SceneTransitionZone, layer 2, `_entranceId="From"+自`) と `SpawnFrom<先>` (Id=`From<先>`)。一方通行は送り側に出口だけ、受け側にスポーンだけ。最初のスポーンを `SpawnDefault` にも置く |
| `Spawn(id, pos)` / `SetDefaultSpawn(x,y)` / `MoveSpawn(fromScene, x, y)` | スポーンの追加・移動 |
| `Finish(save=true)` | タイル再評価 + コライダー再生成 + **岩肌 (`PaintRock`) + 岩の飾り (`ScatterRockDecor(DecorDensity)`)** + `Validate` + 保存。**レシピの最後に必ず** |
| `Validate()` | 検証レポート (OK/WARN/ERR)。単体でも呼べる |
| `Describe(sceneId)` | 部屋の矩形・意図・features・ご褒美・通路・出口の向きを一覧 (設計の最初に読む) |
| `FrameRoom(roomId, pad)` / `FrameSceneView(cx, cy, halfH)` | SceneView を寄せる |
| `Bounds / OpenRects / BoundaryExits / LinksOf` | レイアウトの問い合わせ |

### 地形
| メソッド | 内容 |
|---|---|
| `Solid(x0,y0,x1,y1)` | 岩 (Terrain, RuleTile, 当たりあり) |
| `Ground(x0, x1, top, thickness=3)` | 上面 `top` の床 (= Solid(x0, top-th, x1, top)) |
| `Clear(x0,y0,x1,y1)` | 岩と Backfill を消して空間にする |
| `OneWay(x0, x1, top)` | すり抜け床 (floor_08 l/m/r)。上面がちょうど `top` になるようセル内オフセット済み (実測で当たり上面 = top)。↓+ジャンプで降りる |
| `Slope(xLow, yLow, width, count, ascending)` | 坂タイル (width 1/2/3 = 45°/26.6°/18.4°)、count 枚で count セル上昇。胴体は下 1 行だけなので下は Solid で支える。**パイロットでは未使用・未検証** — 使ったら必ずスクショと接地を確認 |

### 岩の見た目 (質感 + 飾り) — Finish が自動で行う
地形の当たり (Terrain の RuleTile / CompositeCollider2D) には一切触れず、見た目だけを足す。7 シーンで composite の形状が作り直し前と完全一致することを確認済み。

| メソッド | 内容 |
|---|---|
| `RockDecor(density=1f)` | 飾りの密度を指定 (BuildBase で 1 に戻る)。**1 = 標準 / 0.5 = 控えめ / 0 = なし**。レシピのどこで呼んでもよい (実際の配置は Finish で地形完成後に行う)。ボス部屋など画面を静かにしたい時に下げる |
| `PaintRock()` | Terrain の岩 (A1TerrainRule・外周の a1_rock_seal) と Backfill の全セルに、**Backfill タイルマップ**へ岩肌スライスを位置で敷く: セル (x,y) = `a1_rock_tex_r{(-y) mod 4}_c{x mod 4}` (4x4 セル周期で継ぎ目なし)。部屋の縁から奥へ 8 セルで明るさ 1.0→0.5 に落とす (`RockShadeDepth` / `RockShadeMin`)。Terrain の内部は絵なし・縁タイルは内側が透明なので、岩はすべてこの岩肌に見える。**polish A: 4x4 の繰り返し対策** = `RockCell(x,y)` が 4x4 のブロックごとに左右・上下の鏡映をワールド座標のハッシュで決め (ブロック内のスライスの並びも逆にするので継ぎ目は出ない。タイルの transform 行列で反転)、`RockNoise(x,y)` が 7 セル周期 (+3 セル周期の弱い成分) のなめらかな明暗 ±12% と 13 セル周期のわずかな赤み/青みを乗算する (`RockNoiseAmount`)。どちらもシーンに依らないので隣のシーンと同じ場所は同じ見た目。鉄板の壁 (PlateWalls) は対象外。`HiddenRoomCover` の幕も同じ反転・色むらで敷く |
| `ScatterRockDecor(density=1f)` | シーン名をシードにした決定的な散布 (同じレシピなら毎回同じ配置)。ルート `RockDecor` を作り直す。**Rock** (order -8): 部屋の縁から 2〜5 セル奥の岩に歯車・石・ひび・リベット板・化石・配管・光る苔 (縁の帯に掛からない、間隔 ≥5、陰影に合わせて暗く、配管の口は部屋側へ)。**光る苔は重み 0.35・シーンごと最大 `MossMax`=2 個・色を (0.55,0.62,0.68) で沈める** (polish A)。**Deep** (polish A): 縁から 6 セル以上奥の大きな岩の面に、石・ひび・化石・岩塊を 70 セルに 1 つ程度 (間隔 ≥6.5、陰影より少し明るい 0.55〜0.65、ひびは 90° 単位で回転) 埋めて単調さを崩す (鉄板の壁には置かない)。**Wall** (order -7, 屋内の部屋の奥の壁): 左右に続く床から 3 上にランプ (部屋幅 14 ごとに約 1)、床から 2 にポスター (30 ごと)、天井から旗飾り (26 ごと、下 4 マスが空いている所) と鎖 (22 ごと、下 6 マス空き)。すり抜け床・ギミック・ピックアップ・Props・敵・出口・スポーンの周り 1 マスは避ける |

描画順: 空 -100 / 部屋の背景 -90 / Backfill (岩肌) -10 / 岩の飾り -8 / 壁の飾り -7 / Props -5 / Terrain (縁) 0 / Deco 1 / すり抜け床 2 / ギミック 3。
素材を足した・共有ルールを直した時は `Area1AssetPrep.PrepareRock()` (= 切り抜き生成 → インポート → 岩の Tile → A1TerrainRule 作り直し) を流してからレシピを再実行。`PrepareAll()` にも含まれる。

### 配置物 (グループ: Transitions / Terrain Objects / Gimmicks / Pickups / Enemies / TutorialHints / Props / Background)
| メソッド | 内容 |
|---|---|
| `HiddenWall(x0,y0,x1,y1, flagId, hp=3)` | 隠し壁 = BreakableBox(`_flagId`) + ひび割れタイルの Tiled スプライト, layer 10。範囲の岩は自動で消す。flag は `A1_Hidden_<部屋>` |
| `SkillBlock(x0,y0,x1,y1, PlayerSkill, flagId)` | SkillBreakable (落下/突進/大ジャンプの専用タイル絵)。flag は `A1_Slam_<部屋>_<n>` 等 |
| `Spikes(x0, x1, y)` | トゲ床 (HazardZone, 安全地点へ戻す, layer 2) |
| `Ladder(x, y0, y1)` | 梯子 (見た目 + 2.5 間隔のすり抜け横木) |
| `ConveyorBelt(x0, x1, top, speed, reverseFlag)` | Conveyor (layer 10) |
| `Lift(pos, offsets[], speed, wait, requiredFlag)` | 往復リフト (MovingPlatform) |
| `Enemy(x, y, flying, scale, heavy, name)` | Enemy/FlyingEnemy プレハブ。y = 床+1 |
| `BossRoom(arenaRect, flag, bossPos, flying, gateRects[], msg)` | BossArena + 仮ボス (2倍+Heavy) + 非アクティブのゲート |
| `SpeakerAt(x, floorY, flag)` / `SavePointAt(x, floorY)` | スピーカー / セーブ (ベンチの絵) |
| `ThreadPickupAt(x, y, amount, pickupId)` / `KeyItemAt(x, floorY, name, flag, pickupId)` / `ConsumablePickupAt(...)` | ピックアップ。Id は `A1_Pickup_<部屋>_<n>` |
| `Hint(x0,y0,x1,y1, keyboard, gamepad, name)` | TutorialHintZone (layer 2) |
| `Prop(relPath, x, y, scale, order, flipX)` | Area1Gen の飾り (当たり無し) |
| `NpcAt(x, floorY, sprite, speakerName, lines[], repeatLines[], talkedFlag, rewardThread, flipX)` | NpcTalk (layer 11 + トリガー)。一度きりの糸は talkedFlag + rewardThread |
| `TransportAt(pos, size, scene, entranceId, requiredFlags[], prompt, lockedMessage, activateFlag, activateMessage, name)` | TransportInteractable (layer 11 + トリガー)。E1 搬出エレベーター / WL 非常用リフトの両端に置く |
| `GateObject(name, rect, order)` | fence_gate の絵で矩形を塞ぐ layer 10 の実体 (絵は子で、画像上部の余白を見込んで矩形いっぱいに伸ばす)。BossRoom のゲートや FlagDoor の扉に使う |
| `PlaceFerrisWheel(decorGondolas, baseStation, tint)` | 大観覧車プレハブをワールド (54,252) に置く (W*/U4 シーン)。`tint` (省略可) は全部品の色に乗算 = 奥に沈める (W2/W3 の支柱の中は (0.5,0.46,0.6)、W4 は (0.82,0.8,0.9))。明るいままだと本物の脚の筋交い (y37.8 等) が足場に見える |
| `FerrisMidground(spin, tint, alpha)` | (polish A) **中景の観覧車**: 縮めた輪 (半径 36・部品 0.9 倍: 外周トラス + 電球 3 組の流れる明滅 + スポーク 16 + 4 本おきの電球の連なり + ハブ + 直立して吊られたゴンドラ 12) を視差 (`MidWheelFactor` (0.85,0.82)、`MidWheelPos` (40,40) / `MidWheelAnchor` (40,0)) で重ね、`WheelSpin` で 1.2°/s 回す。order -93..-91、当たり無し。**W1〜W4 で同じ値** = 遷移してもつながって見える: 地上 (W1) では外周の下端とゴンドラが画面の上 1/3 を通り過ぎ、W2 で外周をくぐり、W3〜W4 ではスポークがハブへ集まる。乗れるゴンドラ (W5) と見間違えないよう、ゴンドラは暗く小さい (0.62 倍) シルエット。支柱の中 (W2/W3) は tint (0.8,0.76,0.9) |
| `ParallaxProp(relPath, pos, factor, anchor, scale, order, color, tiledSize)` | 視差つきの背景の絵 (Background グループ + ParallaxLayer)。pos = カメラが anchor にある時の表示位置。factor 1 = 無限遠 / 0 = ワールド固定。カメラは縦 11 (ortho 5.5) しか映さないので、本物の観覧車 (外周の下端 y162) は地上から見えない → U2 の西の空の遠景 (`props/ferris_wheel_bg.png`, factor 0.92) や U4 の頭上の外周の帯 (`ferris/far_wheel_silhouette_arc.png`, factor (0.9,1)) に使った。SceneView では pos に出るだけ |
| `LampPost(x, floorY, height, lampColor, order)` | 屋外の街灯 (支柱 = 観覧車の脚の絵を細く Tiled + 灯)。当たり無し・Props |
| `TintRock(x0,y0,x1,y1, tint)` | その範囲の岩肌 (Backfill) の色に tint を乗算 (Finish の PaintRock の後に適用、BuildBase で消える)。U3 の空中の売店の外壁を砂糖菓子色に |
| `PlateWalls(x0,y0,x1,y1)` / `PlateWallsAll()` | **観覧車の構造用 (W 区画で追加)**。その範囲の岩の見た目を「鉄板の壁」(`ferris/plate/plate_tex_r{r}_c{c}` = machine_room_wall を明るくした 2x2 周期、Tile `a1_plate_tex_*`) にする。当たり・縁タイル・陰影はそのまま。範囲内の岩の飾りは機械系 (歯車・配管・リベット板) だけになる。BuildBase で消える。素材は `Area1AssetPrep.PreparePlate()` (PrepareAll にも含む) |
| `Girder(x0, x1, top, color, name)` | 鉄骨の足場 (すり抜け: layer 10 BoxCollider 0.6 厚 + PlatformEffector2D、絵 = `ferris/leg_crossbrace` の Tiled、梁の上端 = top)。↓+ジャンプで降りられる。W 区画では floor_08 の代わりにこれを使う |
| `Truss(x, y0, y1, scale, order, color, angle, group)` / `Brace(x0, x1, y, scale, order, color, group)` | 飾りの柱 (`support_leg` を縦に Tiled) / 梁 (`leg_crossbrace` を横に Tiled)。当たり無し。**背景の梁は足場と見分けがつくよう暗く (例 (0.3,0.25,0.42)) 細く (scale 0.7)、足場の高さと重ねない** |
| `SkyBackdrop()` | 屋内だけのシーンの背景 (地下の機械室) を夜空に差し替える (W2/W3: 鉄骨の隙間から空と観覧車が見える)。BuildBase の後 |
| `Wind(x0,y0,x1,y1, velocity, gust, on, off, phase, groundedMultiplier, name)` | 横風 (WindArea, layer 2) + 吹いている間だけ出る風の線 (ParticleSystem `WindLines`, URP ParticlesUnlit) |
| `HiddenRoomCover(flagId, shade, rects...)` | 隠し部屋の目隠し: rects のセルを周りの壁と同じ質感 (岩肌 / PlateWalls 範囲は鉄板) の前面タイルマップ (`Grid/Cover_<flag>`, order 5) で覆い、flag で消す (FlagActivator)。部屋 + 内側の縁 1 セル。ひび壁のセルには掛けない。PlateWalls の後に呼ぶ |
| `PipePlatform(x0, x1, top, color, name)` | **地下の機械 (D 区画で追加)**。錆びた配管の足場 (すり抜け、`rock/decal_pipe_h` を 0.8 倍で Tiled、胴の上端 = top)。Girder の地下版 |
| `PipeRun(x0,y0,x1,y1, thickness, order, color, group)` | 飾りの配管 (水平 or 垂直、当たり無し、既定 order -6)。壁を這う管・天井に半分埋めた管・**迷路の通路の間の岩の塊を「配管の束」に見せる** (order `OrderBackfill+2`, 太さ 2.4 を間隔 1.92 で重ねる。D5) |
| `Glow(x, y, radius, color, order)` | 放射状の光暈 (fx/glow) |
| `GuideLamp(x, y, phase, color, hanging, period, name)` | 点滅する灯 (BlinkingLight) = 小さな灯 + 光暈 (+ 吊り鎖)。本道の目印 (D5: 緑、phase を -0.11 ずつ = 東へ流れる)・穴の縁の警告灯 (D1: 赤) |
| `SteamVent(x, y, length, on, off, phase, down, damage, name)` | 蒸気の噴き出し口 (HazardZone `_cycle`, layer 2, ノックバックのみ)。y = 根元 (床/天井面)、down で天井から下へ。噴いている間だけ蒸気の柱 (+ 下向きは先端で横に広がる湯気)。**polish A: 柱は steam_puff を縦横比そのまま (幅 1.1) で半分ずつ重ねて積む** (細い側を口に向け、口から離れるほど太い。短い柱は 1 個を 0.55〜1.1 倍)。**先端の湯気は絵を 90° 寝かせて左右へ 1 つずつ + 白いにじみ** (平たく潰すと針葉樹に見えるため。潰す・引き伸ばすは禁止)、噴く 0.6 秒前に口と先端に赤い光と白いもや (予兆)。**根元が画面外でも先端の予兆が見える**。当たりは幅 0.9 × 根元+0.5〜先端。damage 0 なら見た目だけ |
| `WaterBody(x0, y0, x1, surfaceY, tint, name)` | 水 (WaterZone 既定: 歩き 0.6 倍・ジャンプ高さ 0.9 倍) + 奥の暗い水 (order -6) + 手前の薄い水 (order 5, プレイヤーが透ける) + 揺れる水面の線。**蒸気は水面で止める (柱の長さ = 根元〜水面) と「沈んでいれば安全」** (D6) |
| `Belt(x0, x1, top, speed, reverseFlag, name)` | ConveyorBelt の床埋め込み版: 床の最上段を 1 セル掘って当たり (Conveyor) を top-1..top に、矢印の帯が流れの向き・速さで動く (反転フラグでその場で逆向き)、暗い機械の胴 (不透明の裏板付き) と両端の回るローラー。床と同じ高さで乗り降りできる。BoxCollider を外すと見た目だけの表示器 (D2w の配電盤) |
| `Shade(x0,y0,x1,y1, alphaBottom, flip, order)` | 暗がりの帯 (下ほど濃い、既定 order -9 = 岩肌と背景だけ沈め、足場・飾りは沈めない) |
| `TintRockGradient(x0,y0,x1,y1, bottom, top)` | 岩肌を縦のグラデーションで沈める (TintRock を 1 行ずつ)。縦穴で「下ほど暗い」 (D1) |
| `NewObject / Box / AddSprite / Art / AddComp<T>(go, ("_field", value)...) / Set(obj, "_field", value)` | 汎用。Gimmicks.md の任意のコンポーネント (WindArea, WaterZone, Piston, FallingObjectSpawner, TransportInteractable, FlagActivator, WaveArena, NpcTalk, FlagDoor, LeverSwitch …) はこれで置く。`Set` は int/float/bool/string/enum/Vector2/3/Color/Object/配列 に対応 |

## 4. 例: A1_T2 のレシピ (実際に使ったもの)

```csharp
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_T2 「崩れた搬入路」(T2 + 穴 T2 下段) + 「忘れ物の棚」(T2s, 隠し部屋)
// 先に別の呼び出しで OpenOrCreate("A1_T2") を実行しておくこと。
public static class BuildT2
{
    public static string Run()
    {
        var log = BuildBase("A1_T2");

        // 床: 左右の出口 (y-96..-87) の下端 y=-96。中央に幅 7 の穴 (x65..72)
        // 走りジャンプ (≈5.6) では届かず、ジャンプ+回避ダッシュ (≈8.4) で越える
        Ground(48, 65, -96);
        Ground(72, 90, -96);

        // 穴の底 (y-108) から歩いて左へ戻れる段 (各 +3)
        Solid(63, -108, 67, -99);   // P3: 上面 -99 (左の床へ +3)
        Solid(67, -108, 69, -102);  // P2
        Solid(69, -108, 71, -105);  // P1

        // 左上: 入口側は天井の低い通路 (y-87)。その上に隠し部屋への小部屋 (アルコーブ)
        Solid(48, -87, 57, -81);
        Solid(57, -87, 65, -84);                         // アルコーブの床 (上面 -84)
        HiddenWall(63, -84, 65, -81, "A1_Hidden_T2s");   // ひびの入った壁
        OneWay(58, 62, -81);                             // 天井の穴を登る
        OneWay(58, 62, -78);
        OneWay(57, 63, -75);                             // 穴のふた = T2s の床

        // 右側: 棚を登って上の渡り板へ (帰り道 + ひび壁の前に立つ足場)
        OneWay(82, 86, -93);
        OneWay(77, 81, -90);
        OneWay(72, 76, -87);
        OneWay(65, 73, -84);                             // 左からは届かない高さ

        Prop("props/elevator_cabin.png", 73.3f, -106.4f, 1.1f).transform.rotation = Quaternion.Euler(0, 0, 9f);
        Enemy(81f, -95f, name: "WeakEnemy");

        // T2s: 糸×30
        Solid(64, -75, 68, -74);
        ThreadPickupAt(66f, -73.2f, 30, "A1_Pickup_T2s_1");
        OneWay(51, 55, -69); OneWay(64, 69, -68); OneWay(56, 61, -65);
        Solid(51, -75, 54, -72);

        Hint(55, -96, 65, -91, "[Shift] で回避ダッシュ! ジャンプしてからダッシュすると穴を越えられる",
            "R2 で回避ダッシュ! ジャンプしてからダッシュすると穴を越えられる", "HintDash");
        Hint(63, -108, 75, -99, "落ちても大丈夫。左の段を登って戻ろう", null, "HintPit");
        Hint(72, -96, 79, -91, "[J] で攻撃! 敵を倒そう", "□ で攻撃! 敵を倒そう", "HintAttack");
        Hint(80, -96, 88, -91, "棚を登ると上の渡り板へ行ける", null, "HintClimb");
        Hint(65, -84, 73, -81, "ひびの入った壁…… [J] で叩いてみよう", "ひびの入った壁…… □ で叩いてみよう", "HintCrack");

        return log + Finish();
    }
}
```
(全文は `Docs/Area1/SceneScripts/build_A1_T2.cs`。)

## 5. シーンごとのチェックリスト

1. **設計を読む**: `Describe("A1_xx")` で部屋の矩形・`intent`・`features`・`rewards`・通路・出口の向きを確認。紙 (コメント) で床の高さと足場を先に決める。
2. **開く**: `OpenOrCreate` (別呼び出し)。
3. **骨組み + 掘削 + 遷移 + 背景**: `BuildBase`。部屋の矩形は「動ける空間」、外は岩。
4. **床の高さを決める**: レイアウトの通路は部屋の最下段セル (3 単位) の上に開いている。**床 = 通路の下端** (`Ground(x0, x1, passage.yMin)`) にして、部屋の最下段を床で埋める (T1/T2 はこれで y=-96)。出口の手前 3 マスは必ず床 (Validate が WARN を出す)。
5. **中身 (intent/features を正確に)**:
   - 段差 ≤ 3.0 (跳躍頂点 3.5)。二段ジャンプ前提の所は 4.5〜6.5。通路の高さ ≥ 2。
   - 穴: 走りジャンプ ≈5.6 では届かずダッシュ ≈8.4 で届く幅 = **6〜7.5**。落ちたら戻れる段/梯子を必ず付ける。
   - すり抜け床は 3 単位間隔のジグザグ (横の隙間 ≤ 3)。スポーンや出口の真上 1.5 以内に棚を置かない。
   - 隠し部屋の入口は「プレイヤーが横から叩ける位置」に `HiddenWall` (上向き攻撃は無い)。周囲を岩で囲み、壁を壊す以外で入れないことを確認。
   - ご褒美は `rewards` のとおり、Id `A1_Pickup_<部屋>_<n>`。フラグ名は Area1Build.md §6。
   - 敵は床 +1 に置く。
6. **ギミック**: Gimmicks.md のレイヤー規約 (地面=10 / インタラクト=11+トリガー / ゾーン=2)。専用ヘルパーが無い物は `NewObject` + `AddComp<T>` + `Set`。
7. **ヒント**: 新しい操作が初めて要る場所の手前に `Hint`。**実際の割り当ては PlayerController.ReadInput の直読み** (InputSystem_Actions.inputactions はプレイヤー操作に使われていない。例えば .inputactions の Interact=buttonNorth / Attack=Enter は実際と違う)。キーボード: 移動 A/D (←/→)・ジャンプ Space・ダッシュ Shift・攻撃 J・特殊 K・裁断 L・回復 S・調べる **E のみ** (↑ キーでは調べられない)・道具 I ホールド+方向・糸移動 F・すり抜け降り ↓+Space。パッド: 移動 左スティック/十字・ジャンプ ×・ダッシュ R2・攻撃 □・特殊 △・裁断 R1・回復 ○・調べる ↑ (十字/スティック)・道具 L1 ホールド+十字・糸移動 L2。スキル: 落下攻撃 = 空中で ↓+攻撃、大ジャンプ = 地上で ↑+ジャンプ長押し、横突進 = 地上で ↓+ダッシュ長押し。ヒントゾーン同士は重ねない、NpcTalk と重ねない。
8. **見た目**: 背景は自動 (屋内 = 地下の機械室 / 屋外 = 夜空 + 園内シルエット + 屋内の部屋に背景板。屋外シーンでは屋内の部屋に接する同一シーン内の通路と、屋内どうしの他シーンへの掘削にも背景板が付く = 床の格子の穴から空が覗かない)。屋外シーンの境の岩は Carve のままだと空まで届く柱になるので、跳び越えられない高さ (通路の上端 +9 程度) より上は `Clear` で空にする (U1/U2/U3/U4 で実施。隣のシーンと同じ高さにそろえる)。W*/U4 は `PlaceFerrisWheel()`。岩肌と岩・壁の飾りは Finish が自動で付ける (密度は `RockDecor(0.5f)` 等で調整、手置きの `Prop` の周りは自動で避ける)。飾りは `Prop` で意図に合う物だけ (下の注意を参照)。
9. **`Finish()`** → レポートの ERR を 0 に。WARN は内容を読む (未作成の先シーンは可)。
10. **スクショ点検ループ**: 部屋ごとに `FrameRoom` → `capture_scene_view` → 画像を見る → 粗い所 (浮いた床・継ぎ目・単調・届かない段・空っぽの大部屋) を直してレシピを再実行。レシピは毎回シーンを作り直すので何度流してもよい。
11. **プレイテスト** (下記)。
12. 保存済みを確認 (`list_open_scenes` の `isDirty:false`)。

## 6. プレイテストの手順

1. 目的のシーンを開いた状態で `clear_console` → `editor_play`。ステージを直接再生しても PlayerSceneBootstrap が PlayerScene を読み込み、StageLoader が開いているステージを現在のステージとして SpawnDefault に置く (StageLoader の既定ステージは変えない)。
2. 最初の呼び出しで `Application.runInBackground = true` (エディタ非フォーカスだと物理が進まない)。
3. `PlayerController` の `IsGrounded` / `GroundCollider` / 位置で接地を確認。`player.Teleport(pos)` で出口ゾーンへ飛ばして遷移と逆遷移を確認 (ロード完了まで 3〜5 秒待つ)。
4. `console --level warn` でエラーを確認 (MCP-FOR-UNITY の接続エラーは無関係)。
5. `editor_stop`。

例 (scratchpad の play.cs と同じ):
```csharp
public static class PlayT {
  public static string Status() { Application.runInBackground = true;
    var p = Object.FindAnyObjectByType<PlayerController>();
    return $"{StageLoader.Instance?.CurrentStageName} pos={p.transform.position} grounded={p.IsGrounded} ground={p.GroundCollider?.name}"; }
  public static string ToT2() { Object.FindAnyObjectByType<PlayerController>().Teleport(new Vector2(46.5f, -94.5f)); return "tp"; }
}
```

## 7. はまった所 (既知の落とし穴)

- **eval_file の 5 秒タイムアウト** → `run_script --timeout_ms ... --timeout ...` を使う (§2)。タイムアウトしても処理自体はエディタ側で最後まで走っていることがある (2 回目は「変更なし」になる)。
- **ビルダーを直したら recompile 待ち**。`recompile_status` が completed 前に run_script すると古いコードが走る。
- **新規 Terrain の CompositeCollider2D が形状 0 のまま** (TilemapCollider2D→Composite がエディタ上で再生成されない)。`Refresh()` が compositeOperation を None→Merge に付け直して再生成している。Validate は composite の path 数も検査する。手で Terrain を作り直した時も `Finish()` を通すこと。
- **`GetComponent<T>() ?? AddComponent<T>()` は使わない** (エディタでは「偽の null」が返り ?? を素通りする)。
- **SceneView のスクショの背景**: ParallaxLayer はカメラ追従なのでエディタ上ではシーン中心に置かれるだけ。背景はシーン全体を覆う大きさの Tiled にしてあるので見た目の確認はできるが、最終確認は Game View / Play で。
- **スポーンの真上の棚**: スポーン (床+1.5) の 0.6 上に棚のタイルがあると「埋まっている」と判定される (実際も頭がつかえる)。棚をずらす。
- **飾りの選び方**: 宝箱・糸巻き・ハートピンの絵はピックアップと同じ絵なので飾りに使わない (拾える物と誤解される)。T2 の穴の底の搬入かご (elevator_cabin) のように、部屋の意図 (崩れた搬入路) を語る物だけ置く。
- **ゾーン類は layer 2** (Ignore Raycast)。遷移ゾーン・ヒント・トゲ・ボス部屋トリガーは builder が layer 2 に置く。
- **GameField_old のシーンは保存しない**。(旧ステージは自前の SceneUI を持ったまま動く。§11)
- **地形の色**: Terrain の内部 (RuleTile の既定出力) は絵なし。`Clear` は Backfill も消すので部屋の背景が見える。`HiddenWall` / `SkillBlock` も範囲の Backfill を消す (壊した後に岩肌が残らない)。手で Terrain を消した所は Backfill も消すこと (Finish の PaintRock は Backfill に残っているセルを岩とみなす)。坂タイル (`Slope`) とマーカーのセルには岩肌を敷かない (坂の絵は自前の胴体を持つ)。
- **縁タイルの切り抜き**: 共有の縁タイルの外周に接するプラム色 (≈(80,0,54)) を塗りつぶしで透明にしている。浮き足場 (floor_07) は下面の暗いレンガ帯が欠けないよう、内側の帯の左右からだけ塗りつぶす。新しい縁タイルを RuleTile に足したら `Area1AssetPrep.EdgeSprites` にも足す。
- 外周の岩とカメラ: CameraLimit は下限だけ。上・左右はカメラが部屋外を映すので、Carve が岩 6 + Backfill 26 で埋めている。

## 8. パイロットの状態 (2026-10-03)

見た目の更新 (2026-10-03): 7 シーンすべてを岩肌 + 岩の飾りで作り直し (`polish_before_*` / `polish_after_*`, ゲーム倍率 `polish_after_game_*`)。Validate 0 error、Terrain の composite は作り直し前と一致、A1_T2 / A1_H で再生 → SpawnDefault の床に着地を確認。

| シーン | 内容 | 検証 |
|---|---|---|
| A1_T1 目覚めの楽屋 | 床 y-96。低い段差 2 (1段/2段)、左の衣装棚 = すり抜け棚 8 段 (3 間隔ジグザグ) で天井の穴から T1s へ。T1s に日誌その1 (KeyItemPickup, flag `A1_Lore_Journal1`, Id `A1_Pickup_T1s_1`)。ヒント: 移動/ジャンプ/棚/調べる/右へ | 0 error |
| A1_T2 崩れた搬入路 | 幅 7 の穴 (ダッシュ必須)、穴の底から左へ戻る段 3 つ、弱い敵 1、右の棚で上の渡り板へ → ひび壁 (`A1_Hidden_T2s`, HP3) → アルコーブ → 穴を登って T2s (糸×30, `A1_Pickup_T2s_1`)。ヒント: ダッシュ/穴/攻撃/登る/ひび壁 | 0 error, 1 warn (A1_T3 未作成) |
| A1_T3 縦の衣装ラック | T3: 床 y-96 から交互の足場 (すり抜け棚+壁の出っ張り、段差 3) で y-54 の出口 (→T4) へ、中段の踊り場に敵 1。天井 x96..102 に橙の点検口 (SkillBlock 落下, flag `A1_Slam_Uh_1` — A1_U4 側も同じフラグにすること)、上からの到着点の棚 y-45。T3b: トゲの溝 3 本 (深さ 1)+段、棚の上にまち針×3 (`A1_Pickup_T3b_1`)、回復/道具のヒント。T3c: 床の跳ね上げ戸 (FlagActivator `A1_LadderBackstage` で開いた見た目に)、到着 SpawnFromA1_D2、開かない勝手口 | 0 error |
| A1_T4 チケット売り場の廊下 | 床 y-54→y-57。幅 6 の溝 (ダッシュ越え、底から両側へ戻れる)、閉まった窓口の屋根の棚、重装甲の敵 (×1.3, Heavy) とブレイク→裁断のヒント、ボス前のベンチ | 0 error |
| A1_B1 改札の番人 | B1: 床 y-57 の平らな部屋、BossArena `A1_Boss1` (Enemy×2+Heavy)、入ると左のゲートが閉まる、S1 への扉 = FlagDoor(`A1_Boss1`)。S1: 渡り廊下 y-45 の上にスピーカー① (`A1_Speaker1`、壊すと speaker_broken を表示)、下は通り抜けられる | 0 error |
| A1_H 回転木馬の待合所 | ショップ画面は UI シーン ShopUI (§11)。床 y-54、回転木馬 (×1.8)、ベンチ/店/鍛冶師 (`_grantsUpgrade=false`, forge `A1_Hub` 糸30)。左の登り → 迷子センター H2 (迷子の人形 NpcTalk `A1_Npc_Doll` 糸50、日誌その2 `A1_Pickup_H2_1`)、渡り廊下 y-33 → 吹き抜け (A1_U1)、床の格子 (すり抜け) → A1_D1。空色の天井 (`A1_Super_Hs1`) → Hs1 (回転斬りの仮アイテム `A1_Pickup_Hs1_1`)、橙の床 (`A1_Slam_Hs2_1`) → Hs2 金庫 (ハートピン③の仮アイテム `A1_Pickup_Hs2_1`、棚で拠点へ戻れる) | 0 error, 2 warn (U1/D1 未作成) |
| A1_C1 灯の大扉の回廊 | 床 y-54。大扉 = FlagDoor(`A1_Speaker1`+`A1_Speaker2`,`A1_Speaker3`)、扉の 3 つの丸穴の灯を FlagActivator で点灯、ベンチ、床下の搬出エレベーター (TransportAt → A1_MB2 / FromA1_C1、要 `A1_ElevatorDeep`)、到着 SpawnFromA1_MB2 | 0 error, 2 warn (AB/MB2 未作成) |
| A1_U1 メリーの吹き抜け | U1: 縦穴 (屋内)。床 y-21 の中央が拠点への穴、回転木馬の支柱 (飾り) に沿って左右交互のすり抜け足場 7 段 → 上端の乗り場 y3 (左は岩、中〜右はすり抜け)。左上にジップラインの到着点 (乗り場の絵+ケーブル, `SpawnFromA1_MB1`)。屋根 (y12..18) の上は空にして回転木馬を載せた (A1_U2 側も同じ)。U2e: 屋外、床 y3、街灯と旗。柵 = FlagDoor(`A1_FenceOpen`, 当たりは高さ 24 で跳び越え不可)、柵の奥に `ExitToA1_FP` (開いた後の戻り道)。柵の向こうは空を開けて噴水 (prop/fountain.png) を見せる予告 | 0 error, 1 warn (FP 未作成) |
| A1_U2 月明かりの遊歩道 | U2: 屋外、床 y0 (西端 y6・東端 y3 へ段)。浮遊する敵×3、跳び上がって叩くための低い台 4 つ、街灯・旗・閉まった屋台、西の空に遠景の観覧車 (ParallaxProp)。床の格子 (x159..165) → U2r。U2r: 床 y-12、棚 3 段で格子へ戻れる、奥に紫の板壁 (`A1_Rush_U2r`)。U2r2: 糸の大束×80 (`A1_Pickup_U2r2_1`)。大きな売店 (×2.5) の屋根 → 棚 → 荷揚げリフト (y15→33) → 看板塔の骨組みに架かった足場 7 段 → U2a 頂上にボビン爆弾×3 (`A1_Pickup_U2a_1`, ConsumableItemPickup)、浮遊敵×1 | 0 error |
| A1_U3 コーヒーカップ広場 | U3: 屋外の闘技場、床 y0 (左右の出口 y6 へ 2 段)。回るカップ (MovingPlatform: 円軌道 2 客 + 上下往復 2 客、すり抜け)、固定の受け皿 3。WaveArena `A1_ArenaCups` (トリガー x111..141、ゲート = 両出口の通路内) 3 波: 地上×3 / 浮遊×2+地上×2 / 重装甲 (×1.4 Heavy)+浮遊×2。クリアで左の壁に梯子 (FlagActivator) → 高床の売店 (空に浮かぶ外壁 = TintRock で砂糖菓子色、支柱 3 本) の踊り場 → ひび壁 (`A1_Hidden_U3s`) → U3s にハートピン① (`A1_HeartPin1`, `A1_Pickup_U3s_1`)。壁を壊すまで中は岩肌の幕で隠れる (FlagActivator)。観覧車の東の脚が奥に見える | 0 error |
| A1_U4 観覧車の足元 | U4: 屋外、床 y0。西 (A1_W1) は乗り場の建物の前のすり抜け段 y3/y6、東 (A1_U3) は渡り板 y6。セーブ (ベンチ)、地図売り (NpcTalk `A1_Npc_MapSellerU4`: 観覧車はゴンドラを乗り継ぐ/てっぺんの非常用リフトのレバー/地下の蒸気の迷路にも地図売り/点検口の近道)、観覧車プレハブ (乗り場が西端に掛かる) + 頭上の外周の帯 (ParallaxProp)、待ち列の柵・街灯・旗。渡り板の下の床に橙の点検口 (SkillBlock GroundSlam `A1_Slam_Uh_1`)。Uh: 縦穴、途中のすり抜け棚に糸×40 (`A1_Pickup_Uh_1`)、下端 → A1_T3 (一方通行) | 0 error (W1 作成後) |
| A1_W1 観覧車の乗り場 | 屋外、床 y0。東 = 乗り場の建物 (プレハブの BaseStation) の踊り場 y3 / 日除けの屋根 y6 → A1_U4 (境の壁は U4 と同じく x54..57 の y6 以下と y18..27 だけ)。壊れて止まった乗り場のリフト (傾いた台 = 当たりは水平, y12) とそのレールの骨組みに架かった鉄骨の足場 (Girder) を y6→27 までジグザグ。上 = 支柱の付け根 (鉄板の塔 x6..36 y30..46、穴 x15..24) → A1_W2。穴の真下の足場 y27 に `SpawnFromA1_W2` (W2 の床の穴から落ちるとここに戻る)。西 = 非常用リフトの縦坑 (レール + 止まったかご) と TransportInteractable `EmergencyLift` (→ A1_W6 / `FromA1_W1`, 要 `A1_LiftWheel`、ロック時「てっぺんの機械室で動かせるらしい」)、到着 `SpawnFromA1_W6` (10.5,1.5)。頭上の外周の帯 (U4 と同じ)。待ち列の柵・街灯・旗 | 0 error |
| A1_W2 支柱の足場 | 屋内 (SkyBackdrop + PlateWallsAll + 観覧車を暗く)。床 y33 の中央 x15..24 が乗り場への穴 (落ちると W1 の y27 の足場)。広めの鉄骨の足場と壁の鉄板の棚を左右交互に 3 ずつ 18 段 → y90 の床 → A1_W3。浮遊する敵 1 (y58)。背景に支柱の骨組み (両側の柱 + 12 ごとの梁 + X の筋交い、暗色)。y63 の右の足場の先にひびの継ぎ目 (`A1_Hidden_W2s`, HP3) → W2s (床 y63) にハートピン② (`A1_HeartPin2`, `A1_Pickup_W2s_1`)。W2s は壊すまで鉄板の幕 (HiddenRoomCover) で隠れる | 0 error |
| A1_W3 整備用リフト | 屋内 (SkyBackdrop + PlateWallsAll)。坑 x36..54。右の坑のリフト A (y90→111) → 中の踊り場 L1 (y111) → 左の坑のリフト B (y111→132) → L2 (y132) → 右の坑のリフト C (y132→153) → 坑の上を渡るすり抜けの鉄骨 (y156) → A1_W4。リフトは MovingPlatform 往復 (速さ 3、停止 1.2、開始を 0/4/8 秒ずらす)、絵の天板を当たりの上面に合わせて子に移した。各坑にレール・ケーブル・巻き上げ機の歯車。浮遊する敵 2 (y122/146)。外周の下端 (y≈162) が坑の上端を横切って見える | 0 error |
| A1_W4 風の外周 | 屋外 (PlateWallsAll: 西 = 縦坑の外壁、東 = 外周の物置)。足元 (y150 より下) の岩を消して空を開け、眼下に園内の小さなシルエット (画面下端に張り付く ParallaxProp 2 枚)。入口の踊り場 y156 (縦坑の陰で無風) → 外周から吊った点検路 (y156/158/158/159, 幅 4 の隙間は凪を待つかダッシュ) を向かい風の中で東へ → 物置の扉 (紫 = SkillBlock ChargeRush `A1_Rush_W4r`) → W4r (床 y159) に日誌その5 (`A1_Lore_Journal5`, `A1_Pickup_W4r_1`)。扉の前から梯子 (x86, y159→172, 物置の陰で無風) で外周をくぐり、外周の上の点検路 (y172/171) を追い風で西へ → 上の口 x66..78 → A1_W5 (`SpawnFromA1_W5` は y171 の点検路の上)。**polish A**: 上の点検路は x61..78 (東の y172 の点検路と隙間なし) にし、追い風 `WindHigh` を x77..85 に縮めて鉄板のついたて (`WindScreen`, x77.4, 当たり無し) の西を凪にした = A1_W5 から落ちて着地 (72,171.9) し立ち止まっても押し出されない (12 秒静止で確認)。東の点検路で立ち止まると風でついたての所 (76.8) まで運ばれて止まる。横風 = WindArea 3 枚 (下 x62..85 / 上 x57..85 / 扉の下, (-5.5,0), 突風 2.6s/凪 2s, 接地 ×0.45)。落ちると安全網 (y150 の鉄骨) が受け止め、左の段で踊り場へ戻れる | 0 error |
| A1_W5 回るゴンドラ | 屋外 (PlateWallsAll、左右の壁は厚さ 3 の柱だけ残して外は空)。床 y180、下の口 (x66..78 → A1_W4) は鉄骨の格子 (`Grate`)。大観覧車のスポークに腕木で留めた「整備用の回転輪」3 基 (レシピの `Ring`: 腕・縁の弦・電球・小さな hub_axle が回る + 足場のゴンドラ) にゴンドラ 8 台 (MovingPlatform Orbit, 屋根がすり抜けの足場): 輪 A (58,190) r6 3 台 反時計 14°/s → 踊り場 L1 y196 → 輪 B (38,204) 3 台 時計 12°/s → L2 y211 → 輪 C (62,216) 2 台 反時計 14°/s → L3..L6 (y224/226/229/232) → 天井の口 (A1_W6)。飛び移れる角度は限られる (A は 110〜155°で左へ、B は 20〜60°で右へ、C は 95〜125°で左へ)。受け皿 C1 y203 / R y208。踊り場は中心軸の方へ張ったワイヤー (`HubWire`) で吊る。W5g (床 y213): R の右端 (y208) から 5 上 = 二段ジャンプでだけ届く。外れたゴンドラ (傾いた絵) と口の縁の吊り金具が下から見える。糸の大束×100 (`A1_Pickup_W5g_1`)。観覧車の脚は面より奥なのでさらに暗く | 0 error |
| A1_W6 中心軸の機械室 | 屋内 (SkyBackdrop + PlateWallsAll、観覧車は暗く・プレハブの Hub と Legs は非表示)。奥の壁 (machine_room_wall) の前にゆっくり回る巨大な軸 (hub_axle ×2.2, 中心 (54,252), 4°/s) と逆回りの歯車 3。床 y240 (下の口は格子)、セーブ (ベンチ x40.5)。左の段 → 踊り場 y249 = 非常用リフトの上の乗り口 (縦坑の口を x30..36 に開けてかご・レール、`EmergencyLift` → A1_W1/FromA1_W1 要 `A1_LiftWheel`、`LiftLever` = LeverSwitch `A1_LiftWheel`、ランプ赤→緑 FlagActivator、到着 `SpawnFromA1_W1`)。軸の左を 252/255/258/261 → 軸の上の渡り y264 → y267 → 天井の口 (A1_W8)。東の棚 y258 → W7。W7 (空の見える寄り道、床はトゲ): 逆さのゴンドラ 5 台 (gondola_upside_down を上下反転、下になったレール (幅 3.7) と上を向いた底 (+2.48, 幅 2.6) の 2 段がすり抜け足場、両端の鎖で天井から吊る)。仕切りの壁 (x89..92, 上面 y270) はゴンドラ 3 のレール (y266) からは届かず、中をすり抜けて底 (y268.5) に立つのが鍵。帰りは 4 の底 → 5 のレール → 5 の底 → 仕切り。奥の棚 (y261) の宝箱に道具「針山」×3 (ConsumableItemPickup `Assets/Items/PinCushion.asset`, `A1_Pickup_W7_1`) | 0 error |
| A1_W8 てっぺんへの梯子 | 屋内の点検坑 (SkyBackdrop + PlateWallsAll、坑の壁は厚さ 3 で外は空、真上のスポーク x54 が奥を貫く)。右の梯子 (x57, 276→288) → 細い鉄骨 (幅 4〜6) 2 枚 → 左の梯子 (x43, 292→304) → 2 枚 → 右の梯子 (308→320) → 2 枚 → 左の梯子 (324→336) → 最上段 y339 → 天井の口 (A1_MB1)。区画ごとに `FallingObjectSpawner` (荷物 = 新規 `props/cargo_crate.png`、揺れ 0.7s、2.0〜2.4s 間隔、鉄骨に当たると砕ける) 3 基と滑車の目印。浮遊する敵 2 (y299/329)。底の口へ落ちると A1_W6 の y267 の渡りに戻る | 0 error |
| A1_MB1 ゴンドラの道化 | 観覧車のてっぺん (SkyBackdrop、周りの岩はすべて消して空)。足場は外周 (中心線 y342) の真上の細い鉄骨 (y340/343/格子 344/343/340、隙間 3) + 浮き足場 4 (345/348/351/348)。カメラ下限を 341 に下げて足元の外周と電球を見せ、画面下端に眼下の園内 (ParallaxProp)。BossRoom `A1_MidBossUpper` (FlyingEnemy ×2 + Heavy, `Boss_GondolaClown`)、ゲート = 格子の錠 `GrateLock` (戦闘中は ↓+ジャンプで逃げられない)。隙間・両端から落ちると全幅の出口ゾーン (自動の `ExitToA1_W8` を y326..338, 幅 110 に広げた) → A1_W8 の最上段。撃破で東の扉 `S2Door` (FlagDoor) が上へ。S2 (床 y349、屋外の屋上): スピーカー② (`A1_Speaker2`) + `Speaker2Flag` (FlagActivator → `A1_ZiplineReady`) + 壊れた絵、ジップラインの乗り場 (zipline_station、ケーブルは東の空へ) `StarZipline` → A1_U1/FromA1_MB1 (ランプ赤→緑)。扉の柱 (x84..87) は y404 まで (大ジャンプでも越えられない)。アリーナの真上 y366 に星見台の足場 `StarDeck` (大ジャンプでだけ届く) と「星見台への梯子 (別エリア・準備中)」のヒント (遷移なし)。撃破後だけ大ジャンプのヒントが出る | 0 error |

プレイテスト済み (2026-10-03, U 区画): A1_H→U1→U2→U3→U4→(Uh の棚)→T3 と U4→U3→U2→U1→H の全遷移で着地・接地を確認 (U1 の `FromA1_MB1`/`FromA1_FP` のスポーンも接地)。U3 の闘技場: 入場でゲート 2 枚が閉じ、波 1(3)→2(4)→3(3) の順に出現、全滅で `A1_ArenaCups`・ゲート開・梯子出現、再入場ではクリア済みで何も起きない。U3s の幕が `A1_Hidden_U3s` で消える、ハートピン①/ボビン爆弾/糸×80 を Interact で取得、地図売りと会話。コンソールにエラーなし。手順は `SceneScripts/playtest_U.cs` (Status / Tp / Arena / KillAll / Go / Use / Active / Flag)。
未検証 (U 区画): 実入力での落下攻撃による点検口の破壊・突進での板壁破壊・回るカップの上での実戦の難度 (敵が FlyingEnemy/Enemy の仮見た目)。

プレイテスト済み (2026-10-03, W 区画): U4→W1→W2→W3→W4→W3→W2→W1(W2 の床の穴から落下 → W1 の y27 の足場)→U4 の全遷移で着地・接地を確認。W3 のリフト A に乗って y90→111→90 を往復 (プレイヤーはリフト上面に接地したまま運ばれる)、y156 から実入力 (simulate_key a) で坑へ落ちるとリフト B が受け止めた。W4 で突風中は点検路 (x66) から x62.5 まで押し戻され、凪で止まる。突風で踊り場から押し出されると W3 へ戻る (入口の踊り場は後で無風にした)。W2s: ひび壁に 3 ダメージ → `A1_Hidden_W2s` で壁と幕が消え、ハートピン②を Interact で取得 (テスト後にフラグは Unset)。W1 の非常用リフトは `A1_LiftWheel` 無しでロック。コンソールにエラーなし。手順は `SceneScripts/playtest_W.cs` (Status / Tp / Lifts / Wind / Go / Use / Flag / Unflag / Has / Exists / KillAll)。
未検証 (W 区画): 実入力での突進による物置の扉の破壊、W4 の幅 4 の隙間を凪で跳ぶ実際の手ざわり。(W6 との非常用リフトの往復は下の W5〜MB1 で確認済み)

プレイテスト済み (2026-10-03, W5〜MB1): W4→W5 (着地 (79.5,180.9) 接地)、W5→W4、W5→W6→W5 (格子を降りる)、W6→W8、W8 の底から落下 → W6 の y267、W8→MB1、MB1 の足場の隙間から落下 → W8 の最上段 y339 をすべて確認。W5 の輪 A のゴンドラに乗って 7 秒間、プレイヤーとゴンドラの相対位置が (0.000,1.374) から変わらない (運ばれている・ずれ無し)。W6: `A1_LiftWheel` 無しではリフトがロック → レバーで立つ → W6→W1 (`FromA1_W6` (10.5,0.9) に接地) → W1→W6 (`FromA1_W1` (38.5,249.9) に接地)。MB1: 入場で `GrateLock` が閉じ、ジップラインはロック。ボス撃破 (TakeDamage 直呼び) → `A1_MidBossUpper`・錠が開く・S2Door が上へ・大ジャンプのヒント出現 → スピーカー② 破壊 → `A1_Speaker2` + `A1_ZiplineReady` → `StarZipline` で A1_U1 の (257.5,3.9) に到着・接地。W8 の荷物は区画ごとに落ちてくる (2 個同時に確認)。コンソールにエラーなし。手順は `SceneScripts/playtest_W5.cs` (Status / Tp / OnGondola / Rel / Gondolas / Go / Use / Flag / Unflag / Has / Active / Enemies / KillBoss / BreakSpeaker / Crates)。ゲーム倍率のスクショ: `game_W5_*` / `game_W5g` / `game_W6_*` / `game_W7` / `game_W8_*` / `game_MB1_*`。
未検証 (W5〜MB1): 実入力でのゴンドラの乗り継ぎ (飛び移りの角度の窓は物理値からの計算)、二段ジャンプでの W5g (赤ハサミ未入手のため。高さ 5 = 通常ジャンプ 3.5 では届かないことは計算のみ。大ジャンプで横から入れるかは未確認)、実入力での大ジャンプによる星見台、荷物とボスの実戦の難度。~~W4 の `SpawnFromA1_W5` に着いて立ち止まると追い風で押し出される~~ → polish A で解消 (W4 の風よけ)。

### W5〜MB1 で使った作法
- **回る絵**: `MovingPlatform` を `_mode=Orbit, _radius=0, _angularSpeed=0, _spinSpeed=ω` で当たり無しの親に付けると、その場で ω°/s で回る (Kinematic Rigidbody2D の angularVelocity)。ゴンドラ (Orbit, 同じ ω, `_phaseDegrees` = 腕の角度) と同期して回る回転輪・機械室の軸・歯車に使った (W5 `Ring`、W6 `Spinner`)。足場には使わない。
- **ゴンドラの足場**: gondola_platform ×1.5 の屋根の上面 = 中心 +0.44、当たりは幅 3.1 のすり抜け。吊り金具 (絵の上端) = 中心 +1.5 を輪の腕の先に合わせる (軌道中心 = 軸 −(0,1.5))。`_orbitCenter` に空の Transform を渡すと SceneView でも最初の位置に見える。
- **逆さゴンドラ**: gondola_upside_down を flipY、レールの上面 = 中心 −1.18、上を向いた底 = 中心 +1.30。2 段のすり抜けを子に分けて付ける。
- **落下で前のシーンへ戻す**: 自動の下向き出口ゾーン (`ExitTo<下のシーン>`) の BoxCollider を足場の下の全幅に広げる (MB1)。Validate は下向き出口の床を検査しないので、名前はそのままにする。
- **カメラの下限**: `CameraLimit._minY` は BuildBase で「部屋の yMin+2」。足元に見せたい物がある時はレシピで下げる (MB1: 341)。
- **ParallaxProp の Tiled の縁**: 園内シルエットを Tiled 高さ 1.98 (画像いっぱい) で敷くと、上端に下端の行がにじんだ細い線が出た → 1.9 にした。
- `simulate_key` は `--action down|up|press`。押しっぱなし (isPressed: 移動) は効くが、`wasPressedThisFrame` で読む入力 (ジャンプ・攻撃) は届かなかった → 隠し壁は BreakableBox.TakeDamage を直接呼んで確認した。
- **プレイ中に Editor スクリプト (ビルダー) を保存すると再コンパイルでプレイが止まる**。ビルダーを直したらプレイを止め、recompile 完了後にレシピを流し直す。

プレイテスト済み (2026-10-03): A1_T2→T3→T4→B1→H→C1→H→B1→T4→T3→T2 の全遷移で着地・接地を確認。B1 で入場→ゲート閉→撃破→ゲート開・S1 扉が上へ開く、スピーカー破壊→broken 表示、H でショップが開く/閉じる・鍛冶師が赤を渡さない・人形の会話で糸+50、C1 で灯1 点灯・エレベーターはロック・3 フラグで大扉が開く。コンソールにエラーなし。

プレイテスト済み: A1_T1 で再生 → SpawnDefault の床に着地 → 出口で A1_T2 (SpawnFromA1_T1) → 戻り (SpawnFromA1_T2)。コンソールにエラーなし。
未検証: 実入力でのダッシュの穴越え (物理値からの計算のみ)、隠し壁を実際に叩いて壊す流れ。

### D 区画 (地下: A1_D1 / A1_D2 / A1_D5) — 2026-10-03

| シーン | 内容 | 検証 |
|---|---|---|
| A1_D1 配管の縦穴 | 縦穴 x237..255 y-96..-63。拠点の格子の真下の配管 y-66 (到着 `FromA1_H`) から、錆びた配管 (PipePlatform) と壁の岩棚を左右交互に 3 ずつ底 y-96 まで。底の穴 x240..249 → A1_D2、右の床に `FromA1_D2` (252,-94.5)。上ほど暖かい壁灯が多く、下ほど岩肌が暗く沈み (TintRockGradient + Shade)、底の穴の縁に赤い警告灯 (点滅)。壁を這う縦管・横管。ヒント「ここから地下の機械室……」 | 0 error |
| A1_D2 搬送ベルトの通路 | D2: 床 y-117。埋め込みベルト 3 本 (Belt, 既定 -3.5 = 西へ、`A1_BeltReversed` で東へ) の間に固定の島・格子 (Girder x186..195 → A1_D5、↓+ジャンプ)。ベルトの上に配管の足場 y-114 (流れを避ける道) と島の上 y-111。敵 3 (島・ベルトの上・浮遊)。東端は縦穴の真下の着地塔 (配管 y-102〜-114, `FromA1_D1` 244.5,-100.5)。天井に半分埋めた管・吊り灯・奥でゆっくり回る暗い歯車・置き去りの荷物。D2w 錆びた配電室 (床 y-123, PlateWalls を錆色に): 配電盤 (Cabinet: 鉄板 + ちらつく表示灯 + 電線) の前にレバー① `BeltLever` (盤の上に流れる向きの表示ベルト + 赤→緑のランプ)、跳ね上げ戸の下にレバー② `LadderLever` → 梯子が出て A1_T3 へ (一方通行)。棚 (配管 2 段) に糸玉×3 (`A1_Pickup_D2w_1`) と日誌その3 (`A1_Lore_Journal3`, `A1_Pickup_D2w_2`)。西の床の紫の鉄扉 (`A1_Rush_D2w`) の奥に地下水路 (準備中) のヒント。ちらつく裸電球 | 0 error |
| A1_D5 蒸気管の迷路 | D5: 部屋を岩で埋めて通路を掘る。到着の小部屋 (床 y-132, 天井の口の下の棚 y-126 → `FromA1_D2`) に地図売り (`A1_Npc_MapSellerD5`: 蒸気の予兆 / 3 方向 / 歯車の縦穴 / ボスの後のエレベーターが大扉の回廊のそばへ)。3 方向 = 上 (来た道) / 西の縦穴 (配管の棚 4 段) → D6 / 東の本道 (天井の蒸気 3 本、止む順番が 0.7 秒ずつ東へずれる → 段 y-135/-138 (床の蒸気 1) → 床 y-141 → A1_D3)。本道だけに点滅する緑の案内灯 9 個 (東へ流れる)。縦穴の底から下の通路 (床 y-150, 蒸気 4、窪みに糸×15 `A1_Pickup_D5_1`) → 東の縦穴を登って本道の床のすり抜けの蓋へ戻る輪。通路の間の岩の塊は太い配管の束に見せた。D6 水没した排水溝: 水面 y-148 (WaterBody x136..172)。底 y-153、入口の岸 y-147 から 2 ずつの段で入り、西の段を上がった岸 y-145 に糸巻き② (`A1_Spool2`, `A1_Pickup_D6_1`)。天井から下がった蒸気管 5 本 (口 y-138、柱は水面で止まる) = 沈んで歩けば当たらない / 水面の上の配管の近道 (y-144) と岸へ上がる段は蒸気にさらされる。浮遊する敵 1。鎖・光る苔 | 0 error, 1 warn (A1_D3 未作成) |

プレイテスト済み (2026-10-03, D 区画): A1_H の格子から落下 → D1 の `FromA1_H` (配管 y-66 に接地) → D1 の上の口 → H の `FromA1_D1` (251.5,-53.1 接地) → D1 → 底の穴 → D2 `FromA1_D1` (着地塔 y-102 に接地) → D2 の天井の口 → D1 `FromA1_D2` (252,-95.1 接地)、D2 の格子 → D5 `FromA1_D2` (棚 y-126 に接地) → D5 の天井の口 → D2 `FromA1_D5` (接地)、D2w の梯子の最上段 → A1_T3 `FromA1_D2` (155.5,-95.1 接地、T3 の戸も開いた見た目)。ベルト: 既定 -3.5 で立っているだけで西へ流される (BeltMid 上 vx=-3.50) → `BeltLever` で 4 本とも +3.5 → 東へ流されベルトの東端 (238.3) で止まる。`LadderLever` で梯子が出現・戸が消える。蒸気 (SteamC1, on 1.3/off 2.1): ON の瞬間に被弾、ON が続くと無敵明け (1.0 秒) ごとに再被弾、周期 3.4 秒。D6: 水中の歩き vx=-4.78 (乾いた床 -7.99 の 0.6 倍)、ジャンプの上昇 2.88 (乾いた床 3.33)。水中 (y-152) で SteamW2 の真下に 6 秒立っても被弾 0、真上の配管 (y-143) では 6 秒で 4 回被弾。地図売りと会話。コンソールにエラーなし。手順は `SceneScripts/playtest_D.cs` (Status / Tp / Go / Use / Flag / Unflag / Has / Active / Belts / Steam / SteamAll / KillAll) と `playtest_D_probes.cs` (SteamTest / WaterTest / Freeze)。ゲーム倍率のスクショ: `game_D1_*` / `game_D2_*` / `game_D2w*` / `game_T3c_fromD2` / `game_D5_*` / `game_D6_*`。
未検証 (D 区画): 実入力での蒸気の波に乗った走り抜け・水中の段の上り (水中ジャンプ 3.15 に対して段は 2)、ベルトの上での実戦の難度、A1_D3 側の到着 (未作成)。

### D 区画で使った作法
- **プレイ中の入力**: `simulate_key` の無い CLI では `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A))` で押しっぱなし、`new KeyboardState()` で離す (ジャンプも届く)。run_script は呼ぶたびに別アセンブリになるので、コルーチンの計測結果は `PlayerPrefs` 経由で次の呼び出しに渡す (終わったら DeleteKey)。
- **予兆の静止画**: コルーチンで `SteamHiss` が出た瞬間に `Time.timeScale = 0` → `capture_game_view` → 戻す (`Freeze`)。
- **steam_puff を平たく縮めると「針葉樹」に見える** (縦長の絵の先細り)。予兆のもや・床の湯気の飾りには fx/glow の白い小さなにじみを使う (SteamVent の `HissBlobs`)。低いアルファでも輪郭線が目立つので、背景の湯気の飾りには向かない (D1 では取りやめた)。
- **leg_crossbrace (筋交い) は隙間が透ける**。手前の機械の胴に使う時は後ろに不透明の鉄板 (`ferris/plate/plate_tex_r0_c0`) を敷く (Belt)。
- 地下の暗さは「岩肌と背景だけ沈める」(TintRock / TintRockGradient / Shade order -9)。足場・灯・ギミックは沈めない (見やすさ優先)。灯の光暈 (Glow) は alpha 0.25〜0.5、半径 1.5 前後。
- レバーが 2 本並ぶ部屋は、それぞれの作用先の近くにランプ (FlagActivator 赤→緑) と表示 (向きの表示ベルト / 跳ね上げ戸のランプ) を置いて区別する。

### D 区画 後半 (地下: A1_D3 / A1_D4 / A1_MB2) — 2026-10-03

ビルダー (Area1StageBuilder) は変更なし。敵の定数アセットを 2 つ追加 (`SceneScripts/prep_consts_D.cs` で作成・更新、何度流してもよい): `Assets/Prefabs/Area1/EnemyConsts_A1Rat.asset` (小ネズミ: HP3 / 防御1 / 速さ3 / 追跡 5・4.2 / 糸1) と `EnemyConsts_A1Clinger.asset` (壁の敵: HP6 / 防御3 / 巡回幅 0.2 / 追跡 3.6・4.5)。

| シーン | 内容 | 検証 |
|---|---|---|
| A1_D3 歯車の胃袋 | D3: 入口の岩棚 y-141 (A1_D5 と同じ床、緑の案内灯 2 + 穴の縁の赤い警告灯)。回る歯車 6 基 G1..G6 (レシピの `Gear`: 中心 (255,-145.5)/(249,-154)/(255,-162.5)/(249,-171)/(255,-179.5)/(249,-188.5)、足場の軌道半径 3.5、±20°/s で交互に逆回り、足場 3 枚 = lift_platform ×0.8 のすり抜け、上面 = 軌道上の点 = 腕木の先のピン) を乗り継いで底 y-198 (→ A1_D4) へ。上り側に乗れば帰り道にもなる (底の配管 y-195 → G6)。壁に張り付く敵 2 (`WallClingerW/E`: 浮遊敵 ×0.9 + Clinger 定数 + FloatingPatrolMotion の振れ幅 2.4/速さ 0.7 = 壁沿いにゆっくり上下し、近づくと飛びかかる)。上の機械室に奥の大歯車、下ほど岩肌を沈める (TintRockGradient + Shade)。D3a: 東の岩棚 y-141 の 6 上 (横穴の口の床 y-135) = 二段ジャンプでだけ届く。糸の大束 ×60 (`A1_Pickup_D3a_1`)。D7: 東の岩棚 y-171 (踏み台の配管 y-168 → G3 へ戻れる) の奥のネズミの巣 (床 y-171, 天井 y-158, 配管 3)。WaveArena `A1_RatNest` (ゲート `NestGate` = 口 x268..270): 小ネズミ (Enemy ×0.6 + Rat 定数) 4 → 5 (配管の上にも) → 5 + 大ネズミ (×1.1 Heavy)。クリアで宝箱 (KeyItemPickup 糸巻き③ `A1_Spool3`, `A1_Pickup_D7_1`) が FlagActivator で出現 | 0 error |
| A1_D4 ピストンの間 | D4: 床 y-198、天井 y-191 (入口・出口の手前は -189、中ほど x304..314 は -188)。ピストン 9 本 (レシピの `Piston`): 天井 C (x281.5/293.5/318.5/330.5/342, ヘッドの下面 -191.5 → -197.3) と床 F (x287.5/299.5/324.5/336.5, 格子 (Girder) の下の溝から ヘッドの上面 -198.7 → -193.5) を交互に。周期は全部 4.0 秒 (待ち 1.6 / 予兆 0.6 = 震え + 赤 / 伸び 0.2 / 伸びたまま 0.8 / 縮み 0.8)、F は C から半周期ずれ、東へ 0.15 秒ずつ遅れる。伸びている間はどちらも通れない壁になり、間の床 (幅 2.75) が安全地帯。絵は order -12 (岩肌の裏) なので岩の中の竿は隠れる。中ほどの床に橙の点検口 (`A1_Slam_D4s_1`, x306..312) → D4s。D4s: 床 y-216、油だまり (WaterBody x303..314 水面 -214, 茶色) の東の台 y-213 にハートピン④ (`A1_HeartPin4`, `A1_Pickup_D4s_1`)。東の床にもう 1 枚の橙の床 (`A1_Slam_D4s_2`, x314..317) の下は行き止まり + 「奈落の倉庫 (別エリア・準備中)」のヒント。帰りは西の岩棚 -213 → 配管 -210 / -207 / -204 / -201 → 床 -198。D8: 床 y-198、天井 y-186、鉄板の壁。ベンチ (SavePoint x357.5)、壁に中ボスの設計図 (レシピの `Blueprint`: 青く染めた鉄板 + 方眼 + carousel の馬 1 頭を SpriteMask で切り抜き + 腹の歯車と弱点の赤丸 + 床の大歯車 + 足 (ピストン) の部品図)、ロッカー 2、吊り灯、ボス部屋の口の上に赤い警告灯 | 0 error |
| A1_MB2 歯車の重馬 | MB2: 床 y-198、天井 y-176 (縦坑 x408..417 だけ上へ)。床に半分埋まった大歯車 (レシピの `FloorGear`: 中心 (408,-209) R13、-7°/s 時計回り。1 つの Kinematic 剛体 + MovingPlatform 半径 0 の `_spinSpeed` の子に踏み板 20 枚の BoxCollider2D (接線方向、layer 10) = 床から 2 盛り上がったこぶが傾きながら東へ流れる。絵は order -12/-11 で床の岩肌の裏に隠れる)。中ボス = BossRoom `A1_MidBossLower` (Enemy ×2 + Heavy, 真鍮色 + 腹に歯車、こぶの上 x408 を行き来)、西の口のゲート。回避用の配管 y-195/-192 (左右) と乗り場の鉄骨 `ElevatorDeck` y-189 (x398..418)。撃破で東の `S3Door` (FlagDoor) が上へ。S3: 床 y-195 (段 x434..438)、スピーカー③ (`A1_Speaker3`) + 壊れた絵 + `Speaker3Flag` (FlagActivator → `A1_ElevatorDeep`)。E1: 乗り場の `ElevatorDeep` (TransportInteractable → A1_C1 / FromA1_MB2, 要 `A1_ElevatorDeep`)、かごは縦坑の上 (y-169.5) → フラグで乗り場 (y-186.75) に下りた絵に切替 (`ElevatorFlag`)、ランプ赤→緑、到着 `SpawnFromA1_C1` (410.5,-187.5) | 0 error |

プレイテスト済み (2026-10-03, D 区画後半): A1_D3 で再生 → SpawnDefault (245.5,-140.06) に接地。D3→D4 (`FromA1_D3` 272.5,-197.06 接地)、D4→MB2 (`FromA1_D4` 380.5,-197.07 接地)、MB2→D4 (`FromA1_MB2` 372.5,-197.07)、D4→D3 (`FromA1_D4` 264.5,-197.07)、D3→D5 (`FromA1_D3` 237.5,-140.07)、D5→D3 (`FromA1_D5` 245.5,-140.07) をすべて確認。歯車: G1 の足場に乗せて 8 秒、相対位置 (0.00,0.93) が一定のまま接地 (`G1_Plate0`) で運ばれる。壁の敵 2 体が壁沿いに上下。ピストン: 天井 C0 の真下 = 伸びの終わり (0.2 秒) に被弾、周期 4.1 秒、伸びきった後は当たり無し。床 F1 の格子の上 = 突き上げの瞬間に被弾し、伸びた頭の上に乗った状態で止まる (上は天井まで 1.4 空き、押しつぶされない)。点検口の上に立つと `SkillBlock_A1_Slam_D4s_1` に接地 (通常では割れない)。MB2: 入場で西のゲートが閉じる、こぶの踏み板 (`Tread11`) に乗ると東へ 1.6/s で流され、こぶの端で床に降りる。エレベーターは `A1_ElevatorDeep` 無しでロック。ボス撃破 (TakeDamage 直呼び) → `A1_MidBossLower`・ゲート開・S3Door が上へ → スピーカー③破壊 → `A1_Speaker3` + `A1_ElevatorDeep`、かごが乗り場の絵に切替 → `ElevatorDeep` で A1_C1 の (301.5,-56.06) に到着・接地 → C1 の `ElevatorDeep` で MB2 の `FromA1_C1` (410.5,-188.06, 乗り場に接地)。D7: 入場でゲートが閉じ、波 1(4)→2(5)→3(6) の順に出現、全滅で `A1_RatNest`・ゲート開・宝箱出現 → Interact で `A1_Spool3`。D3a の糸 ×60 を取得。コンソールにエラーなし (MCP-FOR-UNITY の接続エラーのみ)。手順は `SceneScripts/playtest_D3.cs` (Status / Tp / OnPlate / Rel / Go / Use / Flag / Unflag / Has / Active / Enemies / Pistons / Arena / Gates / KillArena / KillAll / KillBoss / Boss / Break、`GearRide.Start/Log`、`PistonTest.Start/Log`)。ゲーム倍率のスクショ: `game_D3_*` / `game_D3a` / `game_D7` / `game_D4_*` / `game_D4s` / `game_D8` / `game_MB2_*` / `game_S3`。
未検証 (D 区画後半): 実入力での歯車の乗り継ぎ (跳び移りの高さ・距離は物理値からの計算: 隣の歯車の足場どうしは 120°/300° の位置で高低差 2.4〜2.9・横 2.5)、二段ジャンプでの D3a (赤ハサミ未入手。高さ 6 = 通常ジャンプ 3.5 では届かないことは計算のみ。大ジャンプで真上から入れるかは未確認)、実入力の落下攻撃での点検口の破壊、ネズミの群れ・重馬の実戦の難度 (ボスは仮見た目の Enemy、こぶの上の傾いた踏み板での戦い)、ピストンを実入力でタイミングよく抜ける手ざわり。

### D 区画後半で使った作法
- **回る歯車の足場**: 歯車の絵・腕木・ピンは回転する親 (W5 の `Ring` と同じ: Orbit 半径 0 + `_spinSpeed`)、足場は同じ角速度の Orbit (常に水平)。足場の上面を軌道上の点にする (Box offset (0,-0.2), 絵を -0.1 下げる) と、腕木の先のピンがいつも足場の上面の中央に来る。
- **傾く足場**: 1 つの Kinematic 剛体 (MovingPlatform の `_spinSpeed`) の子に BoxCollider2D を並べると複合の当たりになり、全体が回る (子に Rigidbody を付けない、子の layer を 10 に)。乗り手は接地点の速度 (回転込み) で運ばれる。
- **岩の中に隠れる動く物**: 絵の order を Backfill (-10) より奥 (-11/-12) にすると、岩の中では岩肌に隠れ、部屋の中に出た部分だけ見える (ピストンの竿・床に埋まった大歯車)。
- **絵の一部だけ使う**: SpriteMask (sprite = 不透明の鉄板 `ferris/plate/plate_tex_r0_c0`、scale で矩形に) + 対象の `maskInteraction = VisibleInsideMask` (D8 の設計図の馬)。
- プレイテスト中に闘技場の敵に倒されると HP 0 のまま動けなくなる (ゲームオーバー)。波は `KillArena` ですぐ消し、HP は `editor_stop` → `editor_play` で戻す。

### A 区画 (クライマックス: A1_AB / A1_FP) — 2026-10-03

ビルダー (Area1StageBuilder) は変更なし。追加したもの:
- **新規ランタイム部品 `Assets/Scripts/Gimmick/BossPhaseSwitch.cs`** (Gimmicks.md §1): ボス (EnemyController) の HP が最大 × `_threshold` 以下になったら前半の物 (`_phase1`) を点滅させて消し、後半の物 (`_phase2`) を出す。撃破・撃破済みなら両方を消す。EnemyController の `Hp` を毎フレーム見るだけなので、ボス側の改造は無し。
- 敵の定数 `Assets/Prefabs/Area1/EnemyConsts_A1Conductor.asset` (指揮者ノクターン: HP16 / 防御16 / ブレイク 3.5s / 速さ 2.2 / 巡回幅 11 / 接触 2 / 追跡 6・1.8 / 糸 40 / ノックバック 1.5。Heavy = HP8/防御14 より硬い)。`SceneScripts/prep_A.cs` で作成・更新 (何度流してもよい。AssetDatabase.Refresh + Area1Gen の新しい絵のインポートも兼ねる)。
- 新しい絵 `Area1Gen/hall/` (gpt-image-2 で緑一色の背景に生成 → クロマキーで透過 → 余白を切り抜き。manifest.json に pivot 登録): `organ_pipes` (bottom) / `stage_curtain` (top) / `music_note` (center) / `speaker_stack` (bottom) / `chandelier` (top) / `spotlight` (top) / `red_scissors` (center, 赤ハサミのピックアップ) / `inscription_plaque` (bottom, 碑文) / `park_gate` (bottom, 正門) / `avenue_tree` (bottom, 並木)。※ gpt-image-1 (透過背景) はこのプロジェクトのキーでは使えなかった (403)。キー抜きは `SceneScripts/chroma_key.py` (`<name>_raw.png` → `<name>.png`): α = 1 − clamp((g − max(r,b) − 0.15)/0.35)、緑を max(r,b) に抑えてにじみを消し、α>24 の bbox で切る。
- プレイテスト `SceneScripts/playtest_A.cs` (Status / Tp / Go / Use / Flag / Flags / SetFlags / UnsetFlags / HasRed / Red / RevokeRed / Active / BossInfo / HitBoss(n) / Lifts / Notes / **Jump(回数, 横入力 -1/0/1, 2 回目の秒, 横入力の開始秒, 終了秒) + JumpLog** = Keyboard への QueueStateEvent で実入力のジャンプ・二段ジャンプを再現し、最高到達と着地先を PlayerPrefs で返す)。

| シーン | 内容 | 検証 |
|---|---|---|
| A1_AB 噴水下の大音楽堂 + 噴水への大階段 | AB: 床 y-60 の大ホール (天井 y-36、x318..372)。西の入口の踊り場 y-54 (A1_C1 と同じ床) → 段 y-57 → ホール。東に舞台 y-57、その奥にパイプオルガン (hall/organ_pipes ×7.4)、左右の緞帳、スピーカーの山、舞台照明 (スポットライト + 光の筋)、シャンデリア 2、奥の壁 (machine_room_wall を葡萄色に) と付け柱・壁掛けの幕、3 本の電線が天井を這ってスピーカーの山へ集まる (3 つのスピーカーの本体)。BossRoom `A1_AreaBoss` (トリガー x324..372 y-60..-36、`Boss_Nocturne` = FlyingEnemy ×2.5 + `EnemyConsts_A1Conductor`、紫、金の指揮棒の飾り、上下の揺れ 1.3)、ゲート = 踊り場の口 x322..324。**前半** (HP > 8): 浮遊する音符の足場 4 枚 (`Phase1_Notes`: MovingPlatform 往復, すり抜け, hall/music_note ×2.0 の頭の上面 = 当たり 1.9 幅) を y-57 / -54 / -51 と通常ジャンプ (段差 3) で乗り継ぎ、空中のボスを叩く。**後半** (`BossPhase` = BossPhaseSwitch, HP ≤ 8): 通知「ノクターンが指揮棒を振り下ろした!…」→ 音符が 1.6 秒点滅して消え、床の昇降台 5 本 (`Phase2_FloorLifts`: Piston 上向き +3/+6、押しつぶし無し、周期 7.0 秒、A 組 3 本と B 組 2 本が半周期ずれて交互にせり上がる、予兆 0.9 秒の震え + ピンク) で地形が変わり続ける中で戦う。**撃破** (FlagActivator `AreaBossFlag`): 赤ハサミ (`RedScissors` = UpgradePickup Red, `A1_Pickup_AB_1`, ホール中央 (342,-58.6)。取得で「赤色のハサミを手に入れた!」+「空中でもう一度ジャンプすると二段ジャンプ! …滑空できる」) と二段ジャンプのヒント (`HintDoubleJump`) が出現、天井の点検口 `STHatch` (x345..354 y-37..-36) が消えて開いた格子の絵に、静止した音符の階段 4 段 (`AfterBoss_NoteStairs`: y-55 / -50 / -45 / -40、段差 5 = 二段ジャンプ) が穴へ。ST (x342..360 y-27..-3 + 穴 x345..354 y-36..-27): 左右交互の岩棚 7 段 (L1 -35 右 / L2 -30 左 / L3 -25 右 / L4 -20 左 / L5 -15 右 / L6 -10 左 / L7 -5 右。段差 5 = 通常ジャンプ 3.5 + 崖つかみ 1.3 = 4.8 では届かない。左の棚は x350 まで・右は x351 から)。石の井戸の背景 (rock_tex を青く暗く)、両壁の水の筋と配管、青い点滅灯、上から月明かり。L7 から跳ぶと噴水の排水口 → A1_FP。`SpawnFromA1_FP` (353.5,-3.5) = L7 の上。CameraLimit -56 | 0 error, 0 warn |
| A1_FP 噴水広場 + 正門前の並木道 | FP (屋外): 床 y3、中央に沈んだ噴水の池 (x341..358, 床 y0) と大噴水 (prop/fountain ×2, x349.5)、池の水面 (手前の薄い水 + 揺れる線)。池の底の排水口 x345..354 = 鉄の格子 `DrainGrate` (Girder, ↓+ジャンプで大階段へ降りる。格子の下は暗い壁で空が覗かない)。到着 `SpawnFromA1_AB` (355.5,1.5)。西端: A1_U1 と同じ位置 (x304) の柵 FlagDoor `A1_FenceOpen` + 閂のレバー `FenceLever` (LeverSwitch `A1_FenceOpen`, 「閂を外す」) + 表示灯 (小さな街灯、赤→緑)、柵の脇に U1 から見えていた小噴水 (×1, (309.2,6.4)) と街灯 (313.8)。柵の外 (x274..303) も空を開けて遊歩道の床 y3・街灯・旗を続けた。`ExitToA1_U1` は柵の外側 x300..302.5 だけ、`SpawnFromA1_U1` (308.5,4.5)。大噴水の前に碑文 = 日誌その4 (`A1_Lore_Journal4`, `A1_Pickup_FP_1`, 「読む」)。街灯 4・旗 2・並木 2。東は段 y6 / y9 → FP2。FP2: 床 y9 の並木道 (並木 3・街灯 3・旗)、閉じた売店 (×1.5、屋根 = 見えないすり抜け床 `StallRoof` y14 = 床から 5)、東端に正門 (hall/park_gate ×6.87、当たりは門の形の箱 `GateBody` x431..446 y9..20 = アーチの上に立てる、屋根から 6)。アーチの上に糸巻き① (`A1_Spool1`, `A1_Pickup_FP2_1`, 光暈付き)。アーチのプレートに TMP の看板「エリア2へ (準備中)」(JapaneseUI SDF)、扉は閉じたまま + ヒント (遷移なし)。共通の観覧車プレハブ (暗く、遠すぎて映らない) + 西の空の遠景の観覧車 (ParallaxProp ferris_wheel_bg)。CameraLimit 4.5 (広場の床が画面の下寄り) | 0 error, 0 warn |

プレイテスト済み (2026-10-03, A 区画): A1_C1 で再生 → `A1_Speaker1..3` を eval で立てる → 大扉が開く → 歩いて A1_AB (`FromA1_C1` (320.5,-53.06) 接地) → ホールに入ると西のゲートが閉じる。音符 `NoteA` に乗せて 4.5 秒、相対位置 (0.00,0.93) のまま接地して運ばれる。ボスの HP を 8 以下に削ると後半へ (`Phase1_Notes` 非アクティブ・`Phase2_FloorLifts` アクティブ、昇降台が -60 ↔ -57/-54 を周期で上下)。撃破で `A1_AreaBoss`・ゲート開・`RedScissors` / `AfterBoss_NoteStairs` / `STHatchOpen` / `HintDoubleJump` 出現・`STHatch` と昇降台が消える。赤ハサミを Interact → `PlayerProgression.Has(Red)=True`。**実入力** (QueueStateEvent): 赤ハサミ無しのジャンプは上昇 3.07〜3.32 で音符の階段 (5 上) に届かない。取得後の二段ジャンプは上昇 6.4 で、床 → Stair1 → 2 → 3 → 4 → L1 → … → L7 の全 11 段をそれぞれ着地まで確認 (横入力が早すぎると上の棚の裏に頭を打って落ちる = 「上がってから横へ」)。L7 から真上に跳ぶと A1_FP (池の底の格子の上に接地)。FP: 柵が閉じている間は西へ歩いても x305.15 で止まる → レバーで `A1_FenceOpen` → 西へ歩いて A1_U1 (`FromA1_FP` (300.5,3.9) 接地、U1 側の柵も開いている) → U1 の `ExitToA1_FP` → FP。FP2: 床から通常ジャンプでは売店の屋根に届かず (上昇 3.32)、二段ジャンプで屋根 (`StallRoof`) → 屋根から通常ジャンプではアーチに届かず床へ落ち、二段ジャンプで `GateBody` の上 → 糸巻き①を Interact で `A1_Spool1`。碑文を Interact で `A1_Lore_Journal4`。東へ歩くと門 (x430.85) で止まる。コンソールにエラーなし (MCP-FOR-UNITY の接続エラーのみ)。テスト後に `A1_Speaker1..3` / `A1_AreaBoss` / `A1_FenceOpen` / `A1_Lore_Journal4` / `A1_Spool1` / `A1_Pickup_AB_1` / `A1_Pickup_FP_1` / `A1_Pickup_FP2_1` を Unset、赤ハサミを Revoke (SavePoint で休んでいないのでセーブにも残っていない)。ゲーム倍率のスクショ: `game_AB_entry` / `game_AB_fight1` / `game_AB_note` / `game_AB_phase1` / `game_AB_phase2_v2` / `game_AB_defeated` / `game_ST_climb` / `game_FP_arrive_v2` / `game_FP_fence_v2` / `game_FP_plaza` / `game_U1_fenceopen` / `game_FP2_avenue` / `game_FP2_gate` / `game_FP2_arch_v2`。SceneView: `AB_v4` / `ST_v4` / `FP_v1` / `FP2_v1`。
未検証 (A 区画): 実戦でのボスの難度 (仮見た目の FlyingEnemy。HP を一気に削ると前半の音符をほとんど見ないまま後半へ進む)、滑空を使った横移動、大ジャンプ (24.5) を持っている場合の ST (棚の重なりで真上には抜けにくいが、1 段ずつなら大ジャンプでも登れる)。~~ST の後に FP へ着いた時の位置が格子の上 (353.9) になる~~ → polish A で原因を特定して解消 (下の「磨き込み A」)。

### A 区画で使った作法
- **前半/後半のある仮ボス**: BossArena は撃破しか知らないので、HP を見る `BossPhaseSwitch` を横に置き、段階ごとの地形 (足場の親 GameObject) を出し入れする。撃破後に出す物は FlagActivator (ボスのフラグ) 側に寄せる (ロード時の撃破済みにも対応)。
- **床からせり上がる台**: Piston を上向き (`_extendOffset=(0,+n)`) にし、当たり (3×8) の上端 = 根元の位置 = 床面。胴の絵は order -12/-11 (岩肌の裏) で床の中に隠れ、上に出た分だけ見える。天井まで十分高い時は `_crushHazard` 無しでよい (乗れば運ばれるだけ)。
- **二段ジャンプ専用の段**: 固い棚は「通常ジャンプ 3.5 + 崖つかみ 1.3 = 4.8」より高く (5 以上)。すり抜け床は崖つかみの対象外。実測の二段ジャンプは上昇 6.4 なので段差 5 が扱いやすい (6 だと縁ぎりぎりで崖つかみ頼み)。左右交互の棚は端を 1 ずらして (左 ..x350 / 右 x351..)、棚の端に立って跳んでも上の棚の裏に頭を打たないようにする。
- **生成した絵の大きさ**: ゲームのカメラは縦 11 なので、人物 (1.2) と比べて大きくしすぎない (音符の足場は ×2.6 だと全体 4 単位で大味 → ×2.0)。
- **QueueStateEvent の押しっぱなし**: 入力を流す MonoBehaviour (ステージのシーンに置いた物) は遷移で消えるので、キーを押したまま遷移すると離されずに歩き続ける。遷移をまたいだ後は空の KeyboardState を流して離す。
- **世界に置く文字 (看板)**: `TMPro.TextMeshPro` + `Assets/Art/Fonts/JapaneseUI SDF.asset`、fontSize 7 ≈ 高さ 0.7、`MeshRenderer.sortingOrder` で絵の手前に。

## 9. 磨き込み A (polish pass A, 2026-10-03) — 新しい素材なし

レシピ・ビルダーだけで直し、27 シーンを全てレシピから作り直した (Validate 全 0 error)。スクショは `Assets/Screenshots/Area1/polishA_*` (`_before` / `_after`、ゲーム倍率 = Play 中に `SceneScripts/polish_shot.cs` の `Shot.Freeze(x,y)` でプレイヤーを止めて `capture_game_view`、岩肌は SceneView `FrameAt.Run(cx,cy,halfH)`)。

| # | 内容 | 変更した所 |
|---|---|---|
| 1 | A1_W4: W5 から落ちて `SpawnFromA1_W5` (72,172.5) に着き立ち止まると追い風で押し出されていた → 上の点検路を x61..78 に延ばし (東の点検路と隙間なし)、追い風を x77..85 に縮め、境に鉄板のついたて `WindScreen` (飾り) | `build_A1_W4.cs` |
| 2 | A1_FP: ST から跳んで着くと格子の上 (353.9) になる原因 = **全シーン共通座標のため、遷移元 (A1_AB) の岩の当たりが遷移先のスポーン (355.5,1.5) と重なっており、旧シーンの非同期の破棄が終わるまでの物理ステップで押し出されていた** (AB の大階段の縦穴 x345..354 の方へ)。`StageLoader` の読み込み完了時に**旧ステージのルートを全て非アクティブにしてから**破棄するよう変更 (全ての遷移に効く)。実入力の二段ジャンプで L7 → FP (355.50,0.93 の岩の池の底に接地) を確認 | `Assets/Scripts/Common/StageLoader.cs` (ランタイム、5 行) |
| 3 | 岩肌の 4x4 の繰り返し → ブロックごとの鏡映 + 色むら + 奥の岩の飾り (Deep)。光る苔は重み・数・明るさを下げ、D3/D5 の手置きの苔も暗く (D5 は 3→2 個、光暈 0.25→0.12) | ビルダー `PaintRock` / `RockCell` / `RockNoise` / `ScatterRockDecor` / `HiddenRoomCover`、`build_A1_D3.cs` / `build_A1_D5.cs` |
| 4 | 蒸気の柱・先端の湯気を「潰さない」作りに (上の SteamVent の欄)。D2 の水路の口の湯気も縦横比 1:1 に | ビルダー `SteamVent`、`build_A1_D2.cs` |
| 5 | A1_D2 D2w の 2 本のレバー: 柄の色 (ベルト = 橙 / 跳ね上げ戸 = 青緑) + 柄の上の札 (色の縁 + 作用先の絵 (逆向きの矢印 2 つ / 梯子と上向き矢印) + 「ベルト」「はね戸」) + 跳ね上げ戸のレバーから戸へ引き綱、操作の文言も別 (「ベルトの向きを変える」「跳ね上げ戸を開ける」) | `build_A1_D2.cs` (`Lever` / `LeverPlaque`) |
| 6 | A1_U3 U3s: 奥の壁を砂糖菓子色、左右の緞帳・シャンデリア・旗飾り・ポスター 2・在庫の木箱 4・ハートピンを照らす舞台照明と光暈。A1_U2 U2a: 塔の中央に柱 (x235.5) を足し、7 枚の足場それぞれに一番近い柱からの腕木 (足場の真下の梁) と斜めの方杖 (`PlatformArm`)。背景の梁は細く暗く (足場と見分ける) | `build_A1_U3.cs` / `build_A1_U2.cs` |
| 7 | W1〜W4 の観覧車の存在感 → 中景の観覧車 `FerrisMidground` (上の API の欄) を 4 シーン共通の値で | ビルダー `FerrisMidground`、`WheelSpin.cs`、`build_A1_W1..W4.cs` |
| 8 | `area1_layout.json` の `FP-U2e` と `W6-WL` を `oneWay: false` に (実装どおり双方向)。これで A1_U1 に自動の `ExitToA1_FP` が出来るので U1 のレシピで消して柵の奥に置き直し、A1_FP は既定のスポーンを噴水の池に固定 (`SetDefaultSpawn(355.5,1.5)`)。遷移ゾーン・スポーンは変更前と同一 | `area1_layout.json` / `build_A1_U1.cs` / `build_A1_FP.cs` |

### 磨き込み A で使った作法
- **全シーン共通座標の落とし穴**: 縦の遷移では、遷移元の岩が遷移先の入口と重なりうる。StageLoader が旧ステージを先に止めるようにしたので、スポーンは遷移元の地形を気にせず置いてよい。
- **プレイ中の撮影**: `Shot.Freeze(x,y)` (Rigidbody2D.simulated=false) で空中にも止められる。ハザードの中で止めると被弾し続けて倒れるので、蒸気などは横から撮る。噴いた瞬間は `Shot.PauseWhenActive("SteamC2","SteamFx")` で timeScale 0 → 撮影 → `Shot.Resume`。
- **縦長の煙・炎の絵**は縦横比を変えない。長さは個数 (重ね) で、横に広げたい時は 90° 寝かせる。
- **見分けたい 2 つのギミック**は「色 (本体) + 札 (同じ色の縁・作用先の絵・短い文字) + 作用先へ伸びる線」の 3 点で。世界に置く文字は TMP + JapaneseUI SDF (fontSize 3.2 ≈ 高さ 0.3)。
- **背景の構造が足場に見える問題**: 足場には必ず支え (腕木・方杖・柱) を付け、背景の梁は細く暗く。中景のゴンドラは暗い小さなシルエットにして、乗れるゴンドラと区別する。

## 10. 磨き込み B (polish pass B, 2026-10-03) — 部品素材 `Area1Gen/polish/` を組み込み

新しい部品 28 枚 (`polish/manifest.json`) を `Area1AssetPrep.PreparePolish()` で取り込み (PPU546 / FullRect / mipmap off / 非圧縮 / Bilinear / manifest の pivot、tileable は Repeat)、
レシピとビルダー (後方互換: 引数の追加・既定値のみ) から 27 シーンを全て作り直した (Validate 全 0 error)。スクショは `Assets/Screenshots/Area1/polishB_*` (`_before` / `_after`、ゲーム倍率 = Play 中に `Shot.Freeze` → `capture_game_view`。蒸気は `Shot.PauseWhenActive`)。
全体の見回り = `polishB_walk_<シーン>` (各シーンの SpawnDefault で 1 枚)、その後の直し = `polishB_fix_<シーン>`。

| # | 内容 | 変更した所 |
|---|---|---|
| 1 | **噴水**: ぼやけた拡大絵 (prop/fountain ×2) をやめ、部品で組む `Fountain(x, floorY, s, tint, order, name, jets)` (受け皿 + 中段の鉢 + 頂部の馬 + 鉢の縁と小鉢の縁からの水の幕 (かすかに伸び縮み) + 受け皿の左右から上へ噴く水 (加算・流れる) + しぶき (揺らぎ) + もや)。A1_FP の大噴水は s=1.6 (幅 9.6・高さ ≈9.8 = カメラ下限 4.5 で画面に全部入る)、柵の脇の小噴水と **A1_U1 の柵越しの予告は同じ値** (x309.2 / 床 y3 / s=0.8 / 色 (0.86,0.82,0.98))。FP の池の水も `WaterVisual` | ビルダー `Fountain` / `WaterVisual`、`build_A1_FP.cs` / `build_A1_U1.cs` |
| 2 | **水・油**: 平らな不透明の青をやめ `WaterVisual` (半透明の模様 + 手前の薄い水 + 流れる水面の帯)。D5/D6 の水 (地下は少し暗く)、D4s の油 (`WaterLook.Oil`) | ビルダー `WaterBody` (引数 `look` を追加)、`build_A1_D4.cs` |
| 3 | **蒸気**: 部品 (ふき出し + 流れる柱 + 綿雲/先端のふき出し)。周期・予兆・当たりは変更なし | ビルダー `SteamVent` |
| 4 | **A1_AB 大音楽堂の奥の壁**: 格子に見えた鉄板をやめ `HallBackdrop()` = 壁紙 `hall_wall_panel` (Tiled) + 腰板 `hall_wainscot_plum` + 付け柱 `hall_pilaster` (8 ごと、梯子に見えないよう暗く) + 柱の間の桟敷席 `hall_balcony_box` ×3 (手すりが足場に見えないよう沈める) + 左右の壁灯 `hall_light_sconce` (対) + 天井際の飾り幕 `hall_curtain_valance`。全部 order -62..-56 で乗算で暗く (壁紙 0.5 / 縁 0.62 / 柱 0.42)、足場 (音符・昇降台, order 2〜3) と分離。シャンデリアは柱の線 (x334.5/342.5) へ | `build_A1_AB.cs` |
| 5 | **A1_MB2 床の大歯車**: 踏み板 20 枚の絵を `gear_tooth_tread` に (明るい縁 = 当たりの上面、弧の長さいっぱい、踏み板と一緒に回る) | `build_A1_MB2.cs` |
| 6 | **A1_D2 D2w のレバー**: 柄を `lever_handle_red` (ベルト) / `lever_handle_teal` (はね戸) に、札を `sign_belt` / `sign_ladder` + 下に同じ色の文字 (配電盤の筋交いより手前 order 4)。柄の色で塗る pass A の方法はやめた (濁る) | `build_A1_D2.cs` (`Lever` / `LeverPlaque`) |
| 7 | **観覧車の中にいる感じ**: 中景の輪 (`FerrisMidground`) は **カメラ y135→152 で消える** (`FadeByCameraHeight`) = 本物の外周 (y≈162) が見える高さ (W3 の上端〜W4) では本物の輪だけ。**A1_W4 は中景の輪を置かない** (ハブが頭上に見えて矛盾していた)、代わりに輪の向こう側を回る遠くのゴンドラ。`BulbString` (電球の連なり: W1 の街灯の間・W2 の梁の下 1 本おき・W3 の坑の上端)、`FarGondola` (暗い小さなゴンドラ、視差 + 揺れ: W1×2 / W2 / W3 / W4×2)。`rim_segment_near` は弧の半径 (4841px) が W5 の整備用の回転輪 (r6) にちょうど合うので **W5 の輪の縁** (8 枚, 45° ごと, 回る) に使った (細い弦 + 電球の作りを置き換え)。W1〜W4 の近景には置いていない (足場に掛かって邪魔になるため) | ビルダー `BulbString` / `FarGondola` / `NearRim` / `FerrisMidground`、`WheelSpin` 不変、`build_A1_W1..W5.cs` |
| 8 | **見回りで見つけた悪い所 5 つ** (下) | |

見回り (T1→T2→T3→T4→B1→H→U1→U2→U3→U4→W1→W2→W3→W4→W5→W6→W8→MB1、H→D1→D2→D5→D3→D4→MB2→C1→AB→FP) で直した 5 つ:
1. **屋外シーンの屋内の部屋の背景板** (A1_U1 の吹き抜け など) が 1 セル周期の暗い格子 (machine_room_wall) でのっぺり → 地下シーンと同じ機械室の絵 (`bg/bg_underground` ×3.2) を矩形に敷く (ビルダー `Background` → `IndoorBackdrop`。U3s の色の上書きはそのまま効く)。
2. **A1_H の奥の大歯車** (×2.2 など) が明るく平らでスポーンの真後ろに大きく出ていた → さらに暗く (0.22,0.19,0.3) + `WheelSpin` でゆっくり回す (向きは交互)。
3. **A1_AB の西の入口**: 回廊の機械室の背景と壁紙の境目が縦の継ぎ目に見えた → 境目 x318.45 に額縁の付け柱 (`EntranceFrame`)。
4. **A1_W8 の到着点の東**: のっぺりした鉄板の塊 → 下ほど暗く (TintRockGradient) + 太い配管 2 本。
5. **A1_FP の大噴水の左右の噴水**が太い柱に見えた → 細く (0.36s)・薄く (0.42)。

### 磨き込み B で使った作法
- **流れる素材**: tileable の部品は Repeat にして `A1SpriteScroll` のマテリアルで UV を流す (Tiled でも継ぎ目なし)。ただし **縦に繋がらない絵 (水の幕: 上端の縁の線 + 筋の先の滴) は流さない** (2 本目の縁が下りてくる) → `SpriteWobble` でかすかに伸び縮み。
- **インポートの wrapMode**: Sprite に切り替える同じ回で wrapMode が Clamp に戻ることがあった → `ImportArt` が `TextureImporterSettings.wrapMode` も明示する。Clamp のままスクロールすると端の行が引き伸ばされ「平らな棒」・透明な端なら「何も映らない」になる。
- **視差と揺れは別の GameObject に**: ParallaxLayer は LateUpdate で position を上書きするので、SpriteWobble (localPosition) は子に付ける (`FarGondola`)。
- **縮めた中景と本物の輪**: 同じ物を 2 つの大きさで同時に見せない。高さで切り替える (`FadeByCameraHeight`)。
- **弧の部品の合わせ方**: 絵の弧の半径・中心を alpha から円当てで測り (rim_segment_near: 半径 4841px、中心は絵の中心から 4619px 下)、倍率 = 目的の半径 / 絵の半径、絵の中心 = 軸から 4619/546×倍率。
- **背景の構造物は暗く・手前の足場は明るく**: 付け柱・桟敷席の手すり・梁の下の電球は乗算で沈め、足場 (音符・鉄骨・踏み板) より明るくしない。

## 11. UI (UI シーンへ分離, 2026-10-03)

エリア1 のステージには **UI (Canvas) を置かない**。他の UI (PlayerUI=HUD / PauseUI / HomeUI / GameOverUI / ResultUI) と同じ方式で、
UI ごとに専用プレハブ + 専用 UI シーン (`Assets/Scenes/UI/`, 名前が UI で終わる) を作り、PlayerScene の `UISceneBootstrap` が Additive で重ねる。

| UI シーン | プレハブ | 中身 (事前配置、実行時生成なし) | Canvas sortingOrder |
|---|---|---|---|
| `StageUI` | `Assets/Prefabs/StageUI.prefab` | `AreaTitle` (AreaTitleView: 部屋名 フェード 0.5 / 表示 1.8 / 消える 0.8) + `TutorialMessage` (TutorialMessageView: TutorialHintZone のヒント・NpcTalk のセリフ) | 1 (HUD 0 の上) |
| `ShopUI` | `Assets/Prefabs/ShopUI.prefab` | ルートに ShopView (品揃え 6 種) + `ShopPanel` (MenuPanelView, Title/Body/Row0..9) | 6 (HomeUI 5 と PauseUI 10 の間) |

- 読み込み: `UISceneBootstrap._uiSceneNames` (既定値と PlayerScene の値) = `PlayerUI, StageUI, PauseUI, HomeUI, ShopUI, GameOverUI, ResultUI`。Build Settings 登録済み。
  StageLoader は「名前が UI で終わる」でステージと区別するので、両シーンともステージ扱いされない。
  どちらも Presenter / Model 注入が不要なので LifetimeScope は持たない (GameOverUI / ResultUI と同じ)。
- **ステージ側はデータだけ**: `BuildBase` が置く `StageInfo` (`_areaName` = `area1_layout.json` の部屋名)。
  StageInfo は有効化時に `StageInfo.Entered` を発行し `StageInfo.Current` を持つ → StageUI の AreaTitleView が部屋名を表示する
  (ステージを直接開いて再生すると UI シーンが後から来るので、AreaTitleView は Awake で `Current` も見る。無効のままの MonoBehaviour は Start が呼ばれないため)。
  シーン開始のリセット (GamePause.Reset / GameSession.ResetRun) は StageInfo.Awake が行う (旧 AreaTitleView.Awake と同じタイミング)。
  ステージが入れ替わると StageUI の TutorialMessage は即座に消える (前の部屋のヒントを持ち越さない)。
- **View の探し方**: TutorialHintZone / NpcTalk / ShopKeeper は `UIViewLookup.Find<T>(this)` で「同じシーン → UI シーン → その他」の順に探す。
  UI シーンはステージより後にロードされることがあるので、Awake ではなく使う時 (ゾーンに入った / 話しかけた / 買い物) に探す。
  A1_H の ShopKeeper は `_shop` 未設定のまま、ShopUI の ShopView を開く。
- **旧ステージ (GameField_old) との両立**: 旧ステージは自前の `SceneUI` (AreaTitleView + TutorialMessageView (+ ShopView)) を持ったまま (保存しない)。
  - AreaTitleView は置かれたシーンで動きが変わる: ステージ内 (旧) = 従来どおり `_areaName` を表示 + リセット / UI シーン内 = StageInfo を待つ。
    旧ステージは StageInfo を持たないので StageUI のタイトルは出ず、二重表示にならない。
  - ヒント・NPC・ショップは同じシーンの View が優先されるので、旧ステージでは従来どおり自前の SceneUI に出る。
- Validate: `StageInfo` がちょうど 1 個 (名前が空なら WARN) / ステージに画面 UI の Canvas があれば ERR。
- 旧 `A1_SceneUI.prefab` / `A1_SceneUI_Shop.prefab`・`UseShopSceneUI()`・`Area1AssetPrep.EnsureSceneUIPrefab` / `EnsureShopSceneUIPrefab` は削除した。
- プレイ確認: `SceneScripts/playtest_UI.cs` (`PlayUI.State` = ロード済みシーン・画面 Canvas と sortingOrder・タイトル/案内/ショップの状態、`Hint(n)` / `Exit(name)` / `Shop` / `ShopClose` / `Npc` / `Pause(bool)` / `Home(bool)`)。2026-10-03: A1_T1 直接再生でタイトル表示、ヒント表示、T1→T2→T3 遷移で部屋名更新・前の部屋のヒントを持ち越さない、A1_H でショップ開閉 (ShopUI)・人形の会話 (StageUI)・ホーム/ポーズ開閉、画面 Canvas の重複なし、コンソールにエラーなし。旧 CarouselHallScene (保存せず) は自前の SceneUI のタイトル/ヒント/ショップが出て StageUI 側は無言。
