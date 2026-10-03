# エリア1「鳴りやまぬ遊園地」実装仕様

設計図: https://claude.ai/artifact/7xY345tHsc2XYPKw2zgB2Z (ローカル: Desktop/nevernight-map.html)
レイアウトデータ: `Docs/Area1/area1_layout.json` (部屋の矩形・通路・ゲート・ご褒美・シーン割り当て・各部屋の設計意図)

## 0. 進め方の原則
- **既存部品を最優先で再利用する。** 新規コードは「既存で足りないもの」だけ (下の §5)。
- エディタ操作は **Unity CLI** (`unity command <name> --caller plugin --skill unity-cli`) で行う。長いコードは C# ファイルに書いて `unity command eval_file` で実行。
- **エディタを同時に操作するエージェントは1つだけ。** 作業前に `unity command list_open_scenes` で状態確認。
- 旧シーン `Assets/Scenes/GameField_old/` は触らない (ユーザーの未コミット変更あり)。
- ワンショットのセットアップメニューは残さない。繰り返し使う編集ツールは Editor スクリプトとして残してよい。

## 1. シーン構成
- フォルダ: `Assets/Scenes/Area1/`、シーン名 `A1_<ID>` (例 `A1_T1`)。名前を "UI" で終えない。
- 27シーン (割り当ては area1_layout.json の `scenes`)。小さな寄り道部屋は親部屋のシーン内に置く。
- `WL 非常用リフト` と `E1 搬出エレベーター` はシーンにせず、移動装置 (§5 TransportInteractable) で表現。
- 全シーンを Build Settings に登録する。

## 2. 座標
- **マップ1マス = 3 ワールド単位**。タイル1セル = 1 単位 (546px, PPU546)。
- 全シーンが**共通のワールド座標**を使う: `worldX = cellX*3`、`worldY = (34 - cellY)*3`。地上の地面 = y 0、地下は負、観覧車の頂上付近は y≈+372。layout.json の `rects[].world` に計算済みの xMin/xMax/yMin/yMax がある。
- 部屋の矩形 = プレイヤーが動ける空間 (内側)。その外側は岩 (地形タイル) で埋める (外周に最低6単位)。地上の屋外部屋 (`outdoor:true`) は空が見える: 地面より上は岩で埋めず、左右端だけ壁/遷移。
- 通路 (`links[].passage`) は部屋と部屋の間の開口。同一シーン内の通路は地形を掘ってつなぐ。別シーン間の通路は、各シーンの端にある開口 + SceneTransitionZone。

## 3. シーンの必須要素 (既存の作法)
- `PlayerSceneBootstrap` (UISceneBootstrap, `_uiSceneNames = ["PlayerScene"]`)
- `SpawnDefault` (PlayerSpawnPoint, Id 空) + 入口ごとの `SpawnFrom<元シーンID>` (Id = `From<元シーンID>`、例 `FromA1_T1`)
- `Exit To<先シーンID>` (SceneTransitionZone, BoxCollider2D trigger, `_sceneName = 先シーン名`, `_entranceId = "From" + 自シーン名`)。スポーンは出口ゾーンの少し内側に置く (アーミング仕様あり)。**出口ゾーンの全幅の下に床を通す** (落ちて届かない事故の再発防止)。
- `StageInfo` (`_areaName`=部屋名、シーン開始のリセット)。UI はステージに置かず、UI シーン `StageUI` (AreaTitle + TutorialMessage) / `ShopUI` (ShopView+ShopPanel) が表示する (SceneBuildGuide §11)。
- `DamageFloaterSpawner` (`_floaterPrefab = Assets/Prefabs/DamageFloater.prefab`)
- `CameraLimit` (地下や y<2 を含むシーンは必須、`_minY` = 部屋の yMin + 2 程度)。※上限/左右の制限がないため、カメラが部屋外を映すのは許容 (岩で埋めておく)。

## 4. 地形
- `Grid` (cell 1×1) 配下に Tilemap:
  - `Terrain`: `StageTerrainRule.asset` (TerrainRuleTile) で岩を塗る。**TilemapCollider2D + CompositeCollider2D (Rigidbody2D Static)**、layer 10 Ground。新シーンは見た目と当たりを一致させる (旧シーンの白Square方式は使わない)。
  - `Platforms`: すり抜け床 floor_08_l/m/r。TilemapCollider2D(usedByEffector) + PlatformEffector2D (oneWay, arc 180)。PlayerController は「usedByEffector + PlatformEffector2D」で判定するので Composite を使う場合も検証すること。
  - `Backfill` (sortingOrder -10, 素の coverbox): 地下シーンの外周を埋める。
  - 坂は ascent/descent_01〜03 (1:1, 1:2, 1:3) のみ。上昇量は整数セル。
- 床の上面は整数 Y。
- ジャンプの物理: 1段ジャンプの頂点 3.5 (段差は ≤3.0 で設計)、二段ジャンプで +3.5、ダッシュ距離 ≈3.6、走りジャンプの到達距離 ≈5.6 (ダッシュ後 8.4)、大ジャンプ 24.5、プレイヤー当たり判定 ≈0.7×1.2 (通路の高さは 2 以上)。二段ジャンプが必要な場所は高低差 4.5〜6.5、通常で届かないこと。

## 5. ギミック
既存 (再利用): SavePoint / BossArena / BreakableSpeaker / FlagDoor / Door / LeverSwitch / SkillBreakable (落下=GroundSlam, 突進=ChargeRush, 大ジャンプ=SuperJump) / BreakableBox / ItemPickup 系 (ThreadPickup, ConsumableItemPickup, EquipmentPickup, KeyItemPickup) / Blacksmith / ShopKeeper / TutorialHintZone / GoalZone / Enemy.prefab / FlyingEnemy.prefab / EnemyConsts_Heavy / すり抜け床。
新規 (この仕様で追加。実装後に `Docs/Area1/Gimmicks.md` に使い方を書く): §5a のリスト。

### 5a. 新規ギミック (足りないもの)
- MovingPlatform (往復・円軌道。整備用リフト/回るゴンドラ/歯車)。プレイヤーを運ぶ。
- 押し流し (ベルトコンベア) / 横風 / 水中の減速 — プレイヤーの速度に外力・倍率を足す口が必要なら PlayerController に最小限の拡張。
- HazardZone (トゲ・蒸気など接触ダメージ。タイマーで ON/OFF 可。必要なら直前の安全地点へ戻す)。
- Piston (タイマーで動く押しつぶし)。
- FallingObjectSpawner (上から落ちてくる荷物)。
- TransportInteractable (ジップライン=一方通行の遷移 / 非常用リフト / 搬出エレベーター: 必要フラグ、起動フラグ)。
- 複数フラグで開く扉 (スピーカー3つ) + フラグで点灯する表示 (大扉のランプ) / フラグで出現する物 (FlagActivator)。
- WaveArena (入ると出口が閉じ、敵の波を順に出し、全滅でフラグ+ご褒美出現)。
- 隠し壁の永続化 (BreakableBox に任意の flagId) 。
- 二段ジャンプ(赤ハサミ)を渡すピックアップ (エリアボス撃破後に出現)。
- NPC の会話 (迷子の人形・地図売り。短いセリフ表示)。

## 6. フラグ・ID 命名
- 進行: `A1_Boss1`, `A1_Speaker1`, `A1_MidBossUpper`, `A1_Speaker2`, `A1_MidBossLower`, `A1_Speaker3`, `A1_AreaBoss`
- 装置: `A1_ZiplineReady`(スピーカー②破壊で), `A1_LiftWheel`(W6 レバー), `A1_ElevatorDeep`(スピーカー③破壊で), `A1_FenceOpen`(FP のレバー), `A1_LadderBackstage`(D2w のレバー), `A1_BeltReversed`(D2w のレバー)
- 闘技場: `A1_ArenaCups`(U3), `A1_RatNest`(D7)
- 隠し壁・能力ブロック: `A1_Hidden_<部屋>`, `A1_Slam_<部屋>_<n>`, `A1_Rush_<部屋>`, `A1_Super_<部屋>`
- ピックアップ: `A1_Pickup_<部屋>_<n>`
- ボス: 仮ボス = Enemy/FlyingEnemy の2倍スケール + EnemyConsts_Heavy (従来と同じ)。地上型/浮遊型は設計図の説明に合わせる。

## 7a. 見た目の方針 (ユーザー指示)
- 素材は**全体絵ではなく部品ごと**に生成し、シーン側で組み立てる。繰り返す部品は継ぎ目なくタイルできること。再利用と、実際の表示サイズでの細かさ (1セル=546px) に注意する。
- 大観覧車は部品 (`Area1Gen/ferris/`) を**全シーン共通の中心・半径**で組み立てる: 中心 = ワールド (54, 252) (マップのセル (18,-50))、外周半径 = 90 単位。どのシーンからも観覧車の同じ部分が見えるようにする。
- 見た目は**常にブラッシュアップを続ける**。各エリアを組んだら必ずスクリーンショットで点検し、粗い所・つながらない所・単調な所を直す (素材の作り直しを含む)。

## 7. 素材
- タイル: `Assets/LocalData/StageAsset/Tiles/` (+ Rules/StageTerrainRule.asset)
- 生成素材: `Assets/LocalData/StageAsset/Area1Gen/` (manifest.json にサイズ・ピボット)。インポート設定は PPU 546 / FullRect / mipmap off / Uncompressed / Bilinear。
- ドア/レバー/ボタン/噴水: `Assets/LocalData/StageAsset/gimmick/`, `prop/`
