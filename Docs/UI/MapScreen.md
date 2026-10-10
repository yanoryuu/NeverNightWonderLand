# 全体マップ画面とミニマップ (Issue #5, 2026-10-10)

要件は GitHub Issue #5 (Q1〜Q5 はすべておすすめ案)。ここは「何を作ったか・どう直すか」のメモ。

## 1. 操作

| | キーボード | パッド |
|---|---|---|
| 開く / 閉じる | M (閉じるは Esc でも) | タッチパッド (PS) / R3 (タッチパッドの無いパッド用)。閉じるは ○(B) でも |
| スクロール | WASD・矢印キー | 左スティック・十字キー |
| ズーム (3 段階) | Q = 縮小 / E = 拡大 | L1 = 縮小 / R1 = 拡大 |
| 現在地に戻る | Space・Enter | ×(A) |

- Select(View) と Tab はアイテム画面。ポーズには項目を足していない。
- 開けないとき: ポーズ・アイテム画面・拠点などのメニュー中、会話中 (ポーズ中)、読み込み中、死亡中、地図の無いステージ (GameField_old など)。
- 開いている間は `GamePause.Push` で止め、`MenuPanelView.NotifyOpened` でメニュー数に数える (プレイ時間は StageLoader が止める)。
  閉じた同じフレームはポーズ・アイテム画面が開かない (`MenuPanelView.LastClosedFrame`)。
- 操作ガイドは最後に使った入力機器 (`InputDeviceTracker`) で切り替わる。

## 2. 構成

```
Docs/Area1/area1_layout.json ─┐
各 A1_ ステージ (読むだけ) ────┴─ BuildMap.Export ─→ Assets/Data/Map/Area1MapData.asset (MapData)
                                                         │
                       BuildMap.Screen ─→ Prefabs/MapUI.prefab ─ Scenes/UI/MapUI.unity (Additive)
                       BuildMap.Minimap ─→ Prefabs/HUD.prefab の Minimap (PlayerUI)
```

| 種類 | ファイル | 役割 |
|---|---|---|
| データ | `Scripts/UI/Map/MapData.cs` | 区画 (色)・部屋 (矩形, シーン, 乗り物の開通フラグ)・通路 (線の両端, 一方通行, ジップライン)・アイコン (種類, 部屋, 位置, フラグ)。ワールド座標 |
| 踏破 | `Scripts/UI/Map/MapExploration.cs` | 入った部屋 ID の静的な記録 (GameProgress と同じ作り)。`Visit / Collect / LoadFrom / Clear`、`Changed / Reset` |
| 踏破の判定 | `Scripts/UI/Map/MapExplorationTracker.cs` | ITickable。0.1 秒ごとにプレイヤー位置が今のステージのどの部屋の矩形 (遊び 0.5) に入ったかを調べて記録。すき間 (通路) では記録しない |
| 見せ方 | `Scripts/UI/Map/MapDrawingState.cs` | どの部屋・通路・アイコンを出すか (下の §3)。全体マップとミニマップで共通 |
| 絵 (View) | `Scripts/UI/Map/MapDrawing.cs` | 事前配置した部屋・通路・アイコン・現在地の参照。表示/非表示・絵の差し替え・Content の位置と拡大だけ (アイコンは拡大しても大きさ一定) |
| 全体マップ | `Scripts/UI/Map/MapScreenView.cs` / `MapScreenPresenter.cs` | View (表示だけ) と Presenter (開閉・スクロール・ズーム・場所名) |
| 入力 | `Scripts/UI/Map/MapInput.cs` | 開いている間の直読み。開閉は `PlayerInputSource.MapPressed` |
| ミニマップ | `Scripts/UI/MinimapView.cs` / `UI/Presenters/MinimapPresenter.cs` | 同じ MapData を拡大率 2 で、プレイヤー中心に描く (カメラ + RenderTexture 方式は廃止) |
| DI | `Scripts/DI/MapLifetimeScope.cs` | MapUI プレハブのルート。MapData・View を登録し、Presenter と Tracker を起動 |
| | `Scripts/DI/UILifetimeScope.cs` | `_mapData` を追加。ミニマップを `IMinimapView` + `MinimapPresenter` で登録 |

- UI シーン `MapUI` (Canvas sortingOrder 9 = アイテム画面と同じ。ポーズ 10 より下) は `UISceneBootstrap` の既定リストと PlayerScene の値、Build Settings に追加済み。
- 画面の部品 (紙・地図の窓・凡例・場所名・操作ガイド・ズーム表示・カーソル) はすべてビルダーで事前配置。実行時に生成しない。
- 仮素材: `Assets/LocalData/UI/_temp/map/` (`Docs/UI/Build/gen_map_icons.py` で作る単色の図形)。
  部屋は `map_room.png` (白地 + 灰の縁、9 スライス) に区画の色を掛ける。アイコンは白地 + 濃い縁取りに色を掛ける。
  色はポーズ画面の紙 (#D6AC70 前後) と文字の色 (#490000) に寄せた。

## 3. 何を見せるか

- 部屋: 入ったことのある部屋だけ (入手制なし)。乗り物の通路 (非常用リフト WL・搬出エレベーター E1) は開通フラグ
  (`A1_LiftWheel` / `A1_ElevatorDeep`) が立ち、つながる部屋のどちらかに入っていれば出る。
- 通路: 両側が見えていれば出る。片側だけ見えている時も、隠し通路 (gate = hidden) でなければ短い線が出る (行き先の手がかり)。
  ジップライン (S2 → U1) は `A1_ZiplineReady` が立つまで出ない。一方通行は線の上に矢印 (A → B)。
- アイコン (置かれた部屋が見えている時):

| アイコン | 出どころ (Export がステージを開いて読む) | 変化 |
|---|---|---|
| 拠点 (セーブ) | `SavePoint` (T4 / H / C1 / U4 / W6 / D8) | なし |
| ショップ / 鍛冶屋 | `ShopKeeper` / `Blacksmith` (H) | なし |
| ボス | `BossArena` の `_boss` 位置と `_flagId` (`A1_Boss1` / `A1_MidBossUpper` / `A1_MidBossLower` / `A1_AreaBoss`) | 撃破で灰色の「×の冠」 |
| スピーカー | `BreakableSpeaker._flagId` (`A1_Speaker1〜3`) | 破壊で灰色の割れたスピーカー |
| 近道 | `TransportInteractable` (行き先がジップラインの通路ならジップライン、それ以外はリフト・エレベーター)。フラグは `_requiredFlags[0]` | 開通前は灰色、開通で水色 |
| 別エリアへの出口 | layout の `exits` (at = セル座標, dir で向き) | なし |

- 近いアイコンどうしは書き出し時に上へずらして重ならないようにする (`IconSpacing`)。
- 区画の色: チュートリアル (layout の T と B) / 拠点 (H) / 地上 (U) / 観覧車 (W) / 地下 (D) / クライマックス (A)。
- 下の欄の場所名: 画面中央 (カーソル) の見えている部屋、無ければ現在地の部屋を「鳴りやまぬ遊園地 — 部屋名」。名前の無い部屋 (B1) はエリア名だけ。

## 4. セーブ

- `SaveData.visitedRooms` (string[]) を追加。`PlayerSaveBridge` が `MapExploration.Collect()` を書き、`TryApplyPendingLoad` で `LoadFrom` する。
  新しい部屋に入ったら他の進行と同じく自動書き込みの対象 (`MapExploration.Changed`)。
- 古いセーブ (項目なし) は JsonUtility で null → 踏破なしとして読む (エラーにならない)。
- はじめから (`TitleScreen.StartNewGame`) で `MapExploration.Clear()`。

## 5. 作り直し方 (レイアウトを変えた・素材を差し替えた時)

```bash
py -3 Docs/UI/Build/gen_map_icons.py     # 仮素材を作り直す時だけ
unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_map.cs --entry BuildMap.All --timeout_ms 600000 --timeout 620
```

- `All` = `Import` (仮素材のインポート設定) → `Export` (JSON + ステージ → MapData) → `Screen` (MapUI.prefab / MapUI.unity を作り直し、
  Build Settings と PlayerScene の UISceneBootstrap に登録) → `Minimap` (HUD.prefab の Minimap の中身を作り直す)。
- **レイアウト JSON を変えたら `All`**。部屋・通路・アイコンの並びは MapData と絵で揃っている必要があるので、Export だけで止めない。
- Export は A1_ のステージを Additive で開いて読み、保存せずに閉じる (開いているシーンはそのまま)。プレイ中は流さない。
- エリア2 以降: 同じ形の `areaN_layout.json` を用意し、`build_map.cs` の `LayoutPath` / `DataPath` / `AreaName` / ステージのフォルダを
  エリアごとに切り替える形に広げる (今はエリア1 だけ)。
- 素材の差し替え: 部屋 = `MapDrawing` の各 Block の Sprite (ビルダーの `map_room.png`)、アイコン = `MapDrawing._iconStyles`
  (種類ごとに通常 / 変化後の Sprite と色)、画面の紙・枠 = MapUI.prefab の `Root/Paper` `Root/Frame` `Root/Viewport`。
  ビルダーを流し直すと上書きされるので、恒久的な差し替えは `build_map.cs` の `Art(...)` / `IconStyles()` を直す。
- 大きさ・色の調整は `build_map.cs` 冒頭の値 (`RoomBorderMultiplier` / `LinkThickness` / `IconSize` / 区画の色)、
  ズームの段階・スクロール速度は MapUI.prefab の `MapScreenView` (`_zoomLevels` = 1.3 / 2.6 / 5、開いた時 = 2 段目)、
  ミニマップの拡大率は HUD.prefab の `MinimapView._zoom` (2)。

## 6. 確認したこと (2026-10-10, エディタのプレイ)

- A1_H / A1_T1 / A1_W5 / A1_FP で開閉・現在地マーカーがプレイヤーの位置 (= 部屋の中の実位置) に出る。スクショ `Assets/Screenshots/Map/`。
- 入った部屋だけ出る (A1_H 開始で H だけ → FP→FP2 へ歩くと FP2 が増える)。PlayerScene ごと読み直すセーブ復帰 (`LoadWithPlayerScene` + PendingLoad) 後も残る。
  古い形式のセーブ (スロット 1〜3、`visitedRooms` なし) を読んでもエラーなし・踏破 0。
- フラグを立てると ボス → 撃破、スピーカー → 破壊、リフト / エレベーター / ジップライン → 開通 (通路とジップラインの線も出る)。
- キーボード (M / WASD / Q・E / Space / Esc) と仮想パッド (R3 / 左スティック / R1 / × / ○) で開閉・スクロール・ズーム・現在地に戻る。
  タッチパッドは実機の DualShock でしか押せないので、コード経路 (`DualShockGamepad.touchpadButton`) の確認のみ。
- 開いている間 `Time.timeScale = 0`・プレイ時間停止、閉じると戻る。マップ中に Esc で閉じてもポーズは開かない。ポーズ中・アイテム画面中に M を押しても開かない。
- ミニマップが同じデータでプレイヤー中心に描かれる (踏破・フラグの変化で更新)。

## 7. 未実装・メモ

- S 項目 F11 (取り逃したご褒美の印) は一度作ったが、ユーザー決定 (2026-10-10「★マークはいらない」) で削除。F12 (能力ゲートの印)・F13 (ピン) も今回は入れない (ユーザー決定)。layout の links の `gate` (slam / rush / dj / super) と MapData.Link.Gate に情報はあるので、
  通路の上にアイコンを足す形で入れられる。
- 部屋の矩形は layout のまま (床の厚みを含む) なので、地上の部屋では床が矩形の下端より 2〜3 マス上に見える。
- Input System のアクションアセット (`InputSystem_Actions.inputactions`) はゲームでは使っていない (入力は `PlayerInputSource` の直読み) ので触っていない。
