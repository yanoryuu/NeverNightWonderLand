# UI 実装メモ (タイトル / セーブスロット / ポーズ / アイテム画面) — 2026-10

素材の割り当ては `Docs/UI/UIAssignment.md` (ユーザー決定)。ここでは「何を作ったか・どう直すか」をまとめる。
§1〜§5 は 1 回目 (タイトル / セーブスロット / ポーズ)。「2回目の決定」の反映・アイテム画面・案内文の整理は §6 以降。

## 1. 素材のインポート
- `Assets/LocalData/UI/**/*.png` (完成形 `ui_アートボード*` / `装備_*` / `shiryou_detail_shiryou_detail.png` を除く) を
  Sprite (2D and UI) / Single / ミップマップなし / Alpha Is Transparency / Clamp / Bilinear / FullRect /
  最大サイズ = 元画像が収まる 2 の累乗 / 圧縮 High Quality (PC は BC7) に設定。
  レシピ: `Docs/UI/Build/import_ui.cs` (何度流してもよい)。
- **9 スライスは設定していない**: 完成形では枠・ボタン・紙はすべて素材のままの大きさで使われている
  (部品を完成形に照合すると拡大率 1.00)。引き伸ばす画面が出たら Sprite Editor で Border を付ける。
- 完成形から作った仮の背景 (縮小しただけ、描き直していない):
  - `Assets/LocalData/UI/_temp/title_bg_temp.png` … タイトル (1) の完成形そのもの (ロゴ・PRESS ANY BUTTON・光の枠が焼き込み)
  - `Assets/LocalData/UI/_temp/menu_bg_temp.png` … タイトル (2) の完成形 (ロゴの無い背景。中央の紙とボタンは上に置く本物の紙で隠れる)

## 2. 座標の決め方
完成形 (4000x2250) を 1920x1080 に縮め、各部品をテンプレート照合して中心位置を出した (OpenCV, 拡大率は全部品 1.00)。
UI 座標 = Canvas 中心原点・上が +y (anchor/pivot 中央, anchoredPosition)。文字は完成形の字面の外接矩形に合わせて
フォントサイズ・字間・位置を調整し、ゲーム画面のスクリーンショットと完成形を同じ方法で測って一致を確認した
(字の高さ・幅は ±1〜2px)。

| 画面 | 部品 | 位置 (x, y) |
|---|---|---|
| タイトル(2) | start2_paper | (18, -200.5) |
| | start2_frame ×4 | (0, 225.5) / (0, 46.5) / (0, -145.5) / (0, -337.5) |
| | start2_arrow | (-374, -476) |
| ロード | load_paper | (18, -200.5) |
| | load_frame | (0, -47) |
| | load_inframe ×3 | (0, 74) / (0, -83) / (0, -240) |
| | load_inframearrow | (-6.5, -350) |
| | load_arrow | (-375, -476) |
| ポーズ | all_background | 全画面 |
| | pause_paper | (18.5, -235.5) |
| | pause.png (看板) | (-2, 347) |
| | pause_button ×5 | (-0.5, 119.5) / (-0.5, -3.5) / (-0.5, -125.5) / (-0.5, -248.5) / (-0.5, -371.5) |
| | pause_arrow | (-295, -476) |
| タイトル(1) (非表示で待機) | start_logo / start_message | (-0.5, 112) / (3, -314.5) |

文字: フォントは `YDWbananaslipplus SDF` (静的アトラス 3891 字。画面の文言・Area1 の部屋名 27 個は全部収録済みを確認)。
色は完成形から採取: ボタン・見出し = #EBA044、スロットの文字 = #490000。
大きさ: タイトルのボタン 45 (字間 3) / ポーズのボタン 37 (字間 3) / 見出し 46 (字間 1) /
スロット 番号 40・章 38・場所 22・日付 14・プレイ時間 23.5。

## 3. 作ったもの

### スクリプト
| ファイル | 内容 |
|---|---|
| `Scripts/UI/MenuInput.cs` (新規) | メニュー入力の直読み (上下/決定/戻る、「何かボタン」)。MenuPanelView / ImageMenuView / SaveSlotView が共用 |
| `Scripts/UI/ImageMenuView.cs` (新規) | 画像ボタンの縦メニュー (MenuPanelView の画像版)。ボタン (枠 Image + 文字 TMP) は事前配置、`SetEntries` で有効/無効を切替。無効 = 枠を (0.6,0.6,0.6) に暗く + 文字のアルファ 0.45、選択中 = 1.06 倍に拡大。開いたフレームの入力は無視 |
| `Scripts/UI/SaveSlotView.cs` (新規) | セーブスロット画面 (3 スロット)。`Open(mode, onChosen, onCancelled, showBackground)`。Load = データのあるスロットだけ選べる / NewGame・Save = 見出しを「どのデータに記録しますか?」に差し替え、データのあるスロットは「上書きしますか? (もう一度決定)」で 2 回決定。空きスロットは「データなし」 |
| `Scripts/UI/TitleScreen.cs` (書き換え) | (1) 何かボタン → (2) メニュー (はじめから / つづきから / 設定 / トロフィー、設定・トロフィーは無効) → スロット。戻るで 1 つ前へ |
| `Scripts/UI/PauseMenuView.cs` (書き換え) | 再開 / セーブ / 設定 / メインメニューに戻る / ゲームを終了。セーブはプレイヤーが SavePoint のインタラクト範囲にいる時だけ有効 (`PlayerInteractor.FindInRange<SavePoint>()`)、選ぶとスロット画面 (Save) → 拠点の位置で記録 |
| `Scripts/UI/HomeUIView.cs` | 「セーブ」→ 確認ページの代わりにスロット画面 (Save) |
| `Scripts/UI/MenuPanelView.cs` | 入力を MenuInput に寄せた / `NotifyOpened/Closed` (他のメニューも AnyOpen に数える) / 開いたフレームの入力を無視 |
| `Scripts/Save/SaveSystem.cs` | 3 スロット (`save_1.json`〜`save_3.json`)。`ActiveSlot` (番号なし API は現在のスロットに働く = ゲームオーバーのリトライはそのまま)。旧 `save.json` はスロット 1 が空なら初回アクセスで `save_1.json` へ移動 |
| `Scripts/Save/SaveData.cs` | 一覧表示用に `savedAt` / `playTimeSeconds` / `chapterName` / `placeName` を追加 |
| `Scripts/Save/PlayerSaveBridge.cs` | 上の 4 項目を記録。`CollectAt(SavePoint)` (復帰位置 = 拠点) |
| `Scripts/Common/GameSession.cs` / `StageLoader.cs` | 累計プレイ時間 `PlayTimeSeconds` (StageLoader.Update で実時間を加算、ポーズ中も数える)。はじめから = 0、つづきから = セーブの値 |
| `Scripts/Common/StageInfo.cs` | `ChapterName` (未設定ならシーン名 `A{n}_` から「第n章」) |
| `Scripts/Player/PlayerInteractor.cs` | `FindInRange<T>()` (インタラクト範囲の T を探す) |
| `Scripts/UI/AreaTitleView.cs` | `AreaName` (旧ステージの場所名の代用) |
| `Scripts/Common/UISceneBootstrap.cs` | 既定リストに `SaveSlotUI` |

### プレハブ / シーン
| アセット | 内容 |
|---|---|
| `Prefabs/SaveSlotUI.prefab` (新規) | Canvas (sort 20) + SaveSlotView。`Root` 配下: Background (all_background、ゲーム中だけ表示) / Paper / Frame / HeadingCover (枠の地の色の板) / HeadingText / Slot01〜03 (Number・Chapter・Place・Date・PlayTime・Empty) / DownArrow / BackArrow |
| `Scenes/UI/SaveSlotUI.unity` (新規) | 上のプレハブだけ。Build Settings 登録済み、PlayerScene の UISceneBootstrap に追加済み |
| `Prefabs/PauseUI.prefab` | `PauseMenu` (ImageMenuView) を作り直し: Background / Paper / Sign / Button0〜4 / BackArrow。旧 MenuPanelView のテキストメニューは削除 |
| `Scenes/TitleScene.unity` | `TitleCanvas`: PressAnyScreen (Background + 非表示の FinalArtParts: StartBackground・Logo・Message) / MenuBackground / TitleMenu (ImageMenuView: Paper・Button0〜3・BackArrow)。+ SaveSlotUI プレハブ |

## 4. 直し方
- **位置・文字サイズを変える**: `Docs/UI/Build/build_ui.cs` の値を直して流し直す (プレハブ・シーンの該当部分を作り直す)。
  ```bash
  unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.SaveSlot --timeout_ms 600000 --timeout 620
  unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.Pause   --timeout_ms 600000 --timeout 620
  unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.Title   --timeout_ms 600000 --timeout 620
  ```
  (Title は SaveSlotUI プレハブを置き直すので、SaveSlot を直したら Title も流す。) 手で直す場合はプレハブ/シーンを直接編集してよい
  (その場合はレシピとずれるので、レシピの値も合わせておく)。
- **背景の差し替え** (本番の背景が来たら): TitleScene の `PressAnyScreen/Background` と `MenuBackground` の Sprite を差し替え、
  `PressAnyScreen/FinalArtParts` を表示にする (枠・ロゴ・メッセージは完成形の位置に置いてある。StartBackground は合成方法が不明なので位置未調整)。
- **選択中・無効の見た目**: ImageMenuView / SaveSlotView のインスペクタ (`_selectedScale` / `_disabledFrameColor` / `_disabledLabelAlpha`)。
- **文言**: SaveSlotView の `_recordHeading` / `_overwriteHeading` / `_emptyLabel`。ボタンの文字は各 Button の Label。
- **章の名前**: StageInfo の `_chapterName` (空ならシーン名から「第n章」)。

## 5. 確認したこと (2026-10-03, エディタ再生)
- タイトル: (1) → 何かキー → (2) → 戻る → (1) (画面が (1) と一致)。はじめから → 記録先 (空きスロット) → TutorialScene 開始 (PlayerScene + UI 一式 + SaveSlotUI)。
- 旧 `save.json` がスロット 1 に移行された (場所 = シーン名 ServiceShaftScene、日付 = ファイル更新日)。
- A1_H の拠点の横でポーズ → セーブ有効 → スロット 2 に記録 (章「第1章」・場所「回転木馬の待合所」・日付・プレイ時間が JSON と一覧に出る)。拠点から離れるとセーブは無効表示。
- 拠点 (ホーム画面) → セーブ → スロット 3 に記録 → ホーム画面へ戻る。
- ポーズ → メインメニューに戻る → TitleScene。つづきから → ロード画面 (3 スロットの情報) → スロット 3 をロード (A1_H、プレイ時間が復元)。
- ポーズ → ゲームを終了 → 再生停止。コンソールにエラー・警告なし。
- スクリーンショット: `Assets/Screenshots/UI/` (`final_*.png`, `cmp_*_side.png` = 左が完成形・右がゲーム)。

---

## 6. 2回目の決定の反映 (2026-10-03 後半)

| 決定 | 対応 |
|---|---|
| 1 紙目・周辺の暗さ | 重ね素材が無いので **手続きで再現して素材に焼き込む**: `Docs/UI/Build/gen_paper.py` → `Assets/LocalData/UI/_gen/`。明るさは「完成形 ÷ 素材のまま」の比を画面座標の多項式 (色ごと) に当てはめた倍率 (紙の無地の所で当てはめ。誤差 rms は赤 0.4〜0.5% / 緑 0.4〜0.8% / 青 0.2〜2%)、紙目は乱数から作る (まだら・細かい粒・短い繊維・小さな斑点。完成形からの切り出しは無し)。`paper_menu.png` (タイトル(2)・ロード) / `paper_pause.png` / `background_pause.png` (ポーズ背景 = all_background + 周辺の暗さ。ゲーム中のスロット画面も同じ) / `paper_journal.png` (資料タブ)。ボタン・枠は完成形でも暗くなっていないので、焼き込むのは紙と背景だけ |
| 2〜6, 8, 9, 12 | 変更なし (今のまま) |
| 7 章の表示 | `StageInfo.ChapterTitle(n)` =「第1章 鳴りやまぬ遊園地」(エリア 1 の名前だけ登録)。スロットの枠に 1 行では入らないので、最初の空白で改行して 2 行・自動縮小 (`SaveSlotView.FormatChapter`)。旧データの「第1章」も表示時に今の章名へそろえる (`SaveSystem.FillDisplayInfo`) |
| 10 「はじめから」中央揃え | ラベルのずれ (33.5, 2) → (0, 2) |
| 11 メインメニューに戻る の確認 | `PauseUI/ConfirmMenu` (ImageMenuView): 暗幕 + 文字の無い看板 `pause_frame` +「メインメニューに戻りますか?」+ ボタン `pause_button` ×2「はい / いいえ」(既定は いいえ)。**確認用の絵が無いので既存部品で組んだ仮** (質問中)。確認中はポーズのボタンを表示したまま操作だけ止める (`ImageMenuView.Interactable`、再開したフレームの押下は無視) |
| 13 プレイ時間 | `StageLoader.Update` は `MenuPanelView.AnyOpen` (ポーズ・拠点・ショップ・アイテム画面・スロット画面など) の間は加算しない。会話中は数える |
| 14 開始シーン | `TitleScreen._startSceneName` = `A1_T1` (既定値とシーンの値) |
| 15 セーブ後の選択位置 | `HomeUIView`: スロット画面から戻ったら (記録しても、戻るでも) 選択を「セーブ」に。`MenuPanelView.SetEntries(entries, selectIndex)` を追加 |
| 追加: 移したスロット | `SaveSystem.MigrateLegacy` が save.json を移す時に章・場所・日付を補って書き直す。表示時も `FillDisplayInfo` で補う (場所は `Resources/StageNameCatalog.asset` = シーン名 → 部屋名の対応表。`BuildUI.StageNames` が Build Settings のシーンの `_areaName` から作る。旧ステージ (GameField_old) の章は仮に第1章)。場所が分からない時はシーン名を出さず空。場所の文字は自動縮小 (22→14) + 入らなければ「…」 |

### 暗幕について
`shushu_black` (不透明度 80% の黒) をそのまま重ねると完成形より明るい: このプロジェクトは Linear 色空間で、UI は線形空間で重なるため。
完成形の暗くなり方 (背景 54〜63 → 14、紙 157 → 55) を測り、不透明度 0.89 の黒の板 (`BuildUI.Dim` / `DimAlpha`) に置き換えた (収集品・資料の詳細、ポーズの確認)。

## 7. アイテム画面 (ItemMenuUI)

素材: `item/` (完成形 `装備_soubi` / `装備_shushu` / `装備_shushu_detail` / `装備_shiryou` / `shiryou_detail_shiryou_detail`)。
部品はすべて素材のままの大きさ (拡大率 1.00) で、完成形を 1920x1080 に縮めて照合した位置に置いた。

### 開き方・操作
- ゲーム中に **キーボード Tab / パッド Select(View)** で開く (`PlayerInputFrame.ItemMenuPressed`)。他のメニューが開いている間・会話などでポーズ中・読み込み中・死亡中は開かない。開いている間は `GamePause` で止め、`MenuPanelView.AnyOpen` に数える (その間ポーズは開かない)。
- 閉じる: Tab / Select / Esc / B (詳細を開いている時の Esc / B は詳細だけ閉じる)。Esc で閉じたのと同じ押下でポーズが開かないよう `MenuPanelView.LastClosedFrame` を見る。
- タブ: Q / E・L1 / R1 (端で反対側へ回る)。上下左右: W/S/A/D・矢印・十字キー (`MenuInput.Frame.MoveX` / `Tab` を追加)。決定: Enter/Space/J・A。
- 開くと毎回「装備品」タブから (3回目の決定 7)。

### 画面
| タブ | 部品と位置 (x, y) | 中身 |
|---|---|---|
| 共通 | all_background (全画面。完成形と一致するので暗さは足さない) / 見出し `*_select` (566.5, 459) / 札 `*_title` (-660, 389.5)、収集品だけ (-661, 389) | 見出しの絵で選択中のタブを示す (soubi_select = 装備品が明るい、を確認) |
| 装備品 | frame1 (-522, -44.5) / frame2 名前 (-522, -397) / frame2 糸 (522, 308) / frame3 (0, -132.5) / frame4 (348, 40) (696, 40) (348, -312) (696, -312) | 左の大枠 = 選んでいる物の説明 (絵は素材が無いので非表示) / 帯 = 名前 / 中央 = 見出し「ハサミ強化」+ ○× 黄・青・赤 (旧ポーズの文言。持っていない物は文字を薄く) / 右上 =「糸 n」/ 小枠 = 見出し (□ 攻撃・△ 攻撃・↓ アイテム・← → アイテム) + 装備中の名前。上下左右で中央 3 行と小枠 4 つを移動 (表示だけ) |
| 収集品 | shushu_frame ×18: x = ±146.5 / ±440 / ±733, y = 120 / -122 / -365。page1〜3 と arrow (913, -115) | `CollectionCatalog` の収集品を先頭から。入手済み (フラグ) は名前 (絵があれば絵)、未入手は空の枠。上の段から上 / 下の段から下でページ送り。決定 → 詳細 |
| └ 詳細 | 暗幕 / frame_item (-422.5, -85.5) / frame_text (315.5, -85) / back (-818.5, -108.5) / next (826.5, -108.5) / close (752.5, 189) | 左 = 絵 (無ければ名前)、右 = 名前 + 説明。左右 (BACK / NEXT) で前後の入手済みへ (無い側は薄く) |
| 資料 | load_paper (+紙目・暗さ) (19, -300.5) / shiryou_frame ×5: y = 134 / -41 / -216.5 / -392 / -567.5 (間隔 175.5。紙の上端から画面下までで切り抜き) / shiryou_arrow (484, -216.5) | 日誌を 1 枠ずつ、入手済みはタイトル (未入手は空の枠)。見えている 4 枠を超えたら 1 行ずつ送る。溝・つまみは素材に無いので手続きの角丸 (`_gen/pill.png`、溝 = 橙、つまみ = 濃い赤) |
| └ 詳細 | 暗幕 / タイトル (5, 254) 74 / 区切り アセット 36 (-0.5, 178.5) / 本文 (4.5, -163.5) 39 行間 141 / 閉じる アセット 35 (751.5, 354) | 完成形の見本文 (「タイトル」「内容がないよう…」) を入れて、字の高さ・幅・1 行 28 字・行の間隔 93.5px が一致するように合わせた。本文は未作成なので TODO の仮文 |

文字の大きさ (完成形に見本の無い所は枠に収まる仮の値): `BuildUI.Item*Font` / `Collection*Font` / `Journal*Font`。
丸数字 (①〜④) は YDW フォントに無いので名前を「ハートピン1」表記にした (3回目の決定 9)。以前足していた `JapaneseUI SDF` のフォールバックは外した (`BuildUI.ItemMenu` が外す)。

### データ: `Assets/Items/CollectionCatalog.asset` (`CollectionCatalog`)
収集品 (並び = 枠の順): ハートピン1〜4 (A1_HeartPin1〜4) / 糸巻き1〜3 (A1_Spool1〜3) / 新技「回転斬り」の秘伝書 (A1_Skill_SpinSlash)。
資料: 日誌「閉園しなかった夜」その1〜5 (A1_Lore_Journal1〜5。名前は各ピックアップの `_itemName` と同じ)。
説明・本文はすべて「TODO: 説明文は未作成」「TODO: 本文は未作成」(決まったらアセットを直接書き換えてよい。`BuildUI.Catalog` を流すと仮文に戻るので注意)。絵 (Icon) は仮にステージの拾い物の絵 (`Area1Gen/props/heart_pin.png` / `thread_spool.png`)。秘伝書は合う絵が無いので空 (名前の文字)。

### スクリプト
| ファイル | 内容 |
|---|---|
| `Scripts/UI/ItemMenu/ItemMenuView.cs` | 開閉・タブ切替・見出し/札の差し替え・ポーズ |
| `Scripts/UI/ItemMenu/ItemMenuTab.cs` | タブの基底 (表示切替・入力の受け渡し・選択中の拡大 1.04) |
| `Scripts/UI/ItemMenu/ItemEquipTab.cs` / `ItemCollectionTab.cs` / `ItemJournalTab.cs` | 各タブ |
| `Scripts/Items/CollectionCatalog.cs` | 収集品・資料の一覧データ |
| `Scripts/Save/StageNameCatalog.cs` | シーン名 → 場所・章 (旧セーブの表示用) |
| `Scripts/UI/MenuInput.cs` | 左右 (`MoveX`) とタブ (`Tab`: Q/E・L1/R1) を追加 |
| `Scripts/Player/PlayerInputSource.cs` / `TestPlay/VirtualPlayerInput.cs` | `ItemMenuPressed` (Tab / Select)、仮想入力の `PlayerButton.ItemMenu` / `MenuMoveX` / `MenuTab` |
| `Scripts/UI/MenuPanelView.cs` | `LastClosedFrame`、`SetEntries(…, selectIndex)`、`OnDisable` で AnyOpen を戻す |
| `Scripts/UI/ImageMenuView.cs` | `Interactable` |

### プレハブ / シーン
`Prefabs/ItemMenuUI.prefab` (Canvas sort 9 + ItemMenuView。`Root`: Background / Tabs (Equip / Collection / Journal、それぞれ `Root` と `Detail`) / Header / TitleTicket) と
`Scenes/UI/ItemMenuUI.unity`。Build Settings 登録済み、`UISceneBootstrap` の既定値と PlayerScene の値に `ItemMenuUI` を追加。

### 作り直し
```bash
py -3 Docs/UI/Build/gen_paper.py                     # 紙目・暗さの素材 (_gen/)
unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/import_ui.cs --entry ImportUI.Run --timeout_ms 600000 --timeout 620
unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.Catalog    # 仮文に戻るので注意
unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.ItemMenu --timeout_ms 600000 --timeout 620
unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/build_ui.cs --entry BuildUI.StageNames
```
(SaveSlot → Title の順は §4 のまま。Pause は確認画面も作る。)

## 8. 案内文の整理 (2026-10-03, UIAssignment.md「案内文」)
Area1 のレシピ (`Docs/Area1/SceneScripts/build_A1_*.cs`) の `Hint(...)` を、チュートリアル区間の操作説明 (キーの説明) だけ残して削除し、27 シーンをレシピで作り直した。
残した 9 個: T1 移動 / ジャンプ / 棚の乗り降り (↓+Space) / 調べる、T2 ダッシュ / 攻撃、T3 回復 / 道具、T4 裁断。
T2 の 2 つは後半の呼びかけ・助言を削って「[Shift] で回避ダッシュ!」「[J] で攻撃!」にした。
A1_AB の撃破後の二段ジャンプの案内 (と FlagActivator の表示対象)、A1_MB1 の大ジャンプの案内と `SuperHintFlag` も削除。

## 9. 確認したこと (2026-10-03 後半, エディタ再生, `Docs/UI/Build/playtest_ui.cs`)
- コンパイル: エラーなし (警告は既存の PlayerSpineVisual の CS0414 のみ)。
- タイトル: (2) の紙の明るさ・紙目が完成形と一致 (`cmp_title2_side_v3.png`)、「はじめから」中央。つづきから → ロード: スロット 1 (移したデータ) に「第1章 / 鳴りやまぬ遊園地」「搬入昇降路」(`cmp_load_side_v2.png`)。はじめから → A1_T1 が開始 (PlayerScene + UI 一式 + ItemMenuUI)。
- アイテム画面: Tab で開く (ポーズ・timeScale 0・プレイ時間停止)、Q/E でタブが回る、Tab / Esc で閉じる、Esc で閉じてもポーズは開かない、ポーズ中は Tab で開かない。
  各タブと完成形の並べ比べ (左が完成形): `Assets/Screenshots/UI/cmp_item_soubi.png` / `cmp_item_shushu.png` / `cmp_item_shushu_detail.png` / `cmp_item_shiryou.png` / `cmp_item_shiryou_detail.png`。資料の紙の明るさは完成形との比 0.98〜1.02。
  収集品: 入手済み (フラグ) だけ名前、決定で詳細、NEXT で次の入手済み、Esc で一覧へ。資料: 入手済みのタイトル、詳細の開閉。
- ポーズ: メインメニューに戻る → 確認 (いいえ が選択) → いいえ でポーズへ戻る。ポーズ中はプレイ時間が止まる。
- 拠点 (A1_H): ホーム → セーブ → スロット画面 → 戻る で選択が「セーブ」のまま。ホーム中もプレイ時間が止まる。
- 案内文: 27 シーンを作り直して Validate 0 error / 0 warning。作り直し前後で全オブジェクト (位置・部品) とタイル数を比べ、違いは案内 (TutorialHints) と A1_MB1 の `SuperHintFlag` だけ。
  再生で A1_T1 は 4 つ (移動・ジャンプ・棚・調べる)、A1_H は 0。
- 暗幕: 完成形との差は背景・紙とも ±5 (0〜255) 以内 (`cmp_item_shushu_detail.png` / `cmp_item_shiryou_detail_sample.png` = 見本文を入れた資料の詳細)。

## 10. 3回目の決定の反映 (2026-10-03 夜, UIAssignment.md「3回目の決定」)

1・3・6・10・11 は変更なし (今のまま)。

| 決定 | 対応 |
|---|---|
| 2 収集品の絵 | `CollectionCatalog` の Icon にステージの拾い物の絵を仮に入れた: ハートピン = `Area1Gen/props/heart_pin.png`、糸巻き = `thread_spool.png` (`BuildUI.Catalog`)。一覧の枠と詳細の左の絵に出る (`ItemCollectionTab` は元から Icon 対応)。**秘伝書は合う絵が無いので名前の文字のまま** (ステージ側の拾い物も既定の絵) |
| 4 アイテムスロットの付け替え | 装備品タブの「↓ アイテム」「← → アイテム」の枠で決定 → **中央の縦長の枠 (ハサミ強化) が選び直しの一覧に替わる** (見出し = 対象のスロット「↓ 下スロット」、行 = 所持アイテム「名前 x所持数」+「はずす」、今セットされている物に ○)。上下で選んで決定でセット (`PlayerItemInventory.SetSlot`、はずす = null)、戻るで取り消し。左右スロットの枠では ← → で対象を左 / 右スロットに切り替える。カーソル中の物の説明・名前を左の大枠・帯に出す。旧ポーズの `ShowItemSelect` の流用で、一覧に出すのは **所持数 1 以上の物 (+ 今そのスロットにある物)**。HUD (ItemSlotView) はインベントリの購読でその場で変わる。**選び直し用の絵は無いので、中央の枠とハサミ強化の行と同じ文字の組み方で作った仮** (`ItemMenuUI/…/UpgradeFrame/ItemPicker`: 見出し + 7 行、間隔 70、入りきらない時は送る) |
| 5 資料の長い本文 | 詳細の本文を切り抜き (`BodyViewport`, RectMask2D, 870x560) の中に入れ、入りきらない時は上下で 1 行ずつ送る (行の間隔は TMP の行情報から測る = 約 92.7px)。目印は一覧と同じ `shiryou_arrow` + 溝・つまみ (`_gen/pill.png`) を本文の右 (484, 20) に置いた (暗幕の下に透ける一覧の目印 (484, -216.5) と重ならない高さ。仮)。送れない時は目印を隠す。未入手の日誌は今まで通り空の枠 |
| 7 開くタブ | `ItemMenuView` は毎回 `Tab.Equip` で開く |
| 8 オーバーレイ | パッドは **R3 を 0.6 秒長押し** (`TestPlayRunner.OverlayHoldSeconds`、実時間、押し続けても 1 回)。Select はアイテム画面だけ。F1 はそのまま。`Docs/TestPlay/README.md` も更新 |
| 9 丸数字 | `CollectionCatalog` と Area1 のレシピ (`build_A1_U3/W2/H/D4/FP/D5/D3.cs` の拾い物の `_itemName`) を「ハートピン1」「糸巻き1」表記にして 7 シーンを作り直した (Validate 0 error / 0 warning、作り直し前後で各シーンファイルの行数が同じ = 構成は不変)。YDW フォントから `JapaneseUI SDF` のフォールバックを外した (YDW を使う画面の文字 = 収集品・資料・アイテム・攻撃・部屋名・各画面の文言を調べ、YDW に無い字は丸数字だけだった)。※スピーカー①〜③の通知文 (B1/MB1/MB2) は HUD の通知 (JapaneseUI SDF) で出るので丸数字のまま |
| 12 入手通知の操作 | `PlayerProgression.Grant` / `GrantSkill` の通知に操作を 1 行足す (`ControlHint`、取った時の `InputDeviceTracker.Current` で キーボード / パッドを選ぶ)。赤ハサミ「空中で [Space] (×) で二段ジャンプ、落下中に [Space] (×) 長押しで滑空」/ 落下攻撃「空中で [↓]+[J] (↓+□)」/ 大ジャンプ「地上で [↑]+[Space] (↑+×) を長押しで溜め、離して大ジャンプ」/ 横突進「地上で [↓]+[Shift] (↓+R2) を長押しで溜め、離して横突進」。割り当ては `PlayerInputSource.ReadDevice`、条件は各ステートと同じ。黄・青ハサミ・パリィ・[K] には足していない |

### 確認したこと (エディタ再生, A1_T1, `Docs/UI/Build/playtest_ui.cs` の `PlayUIr3`)
- コンパイル: エラーなし (警告は既存の PlayerSpineVisual の CS0414 のみ)。
- アイテム画面: 資料タブで閉じて開き直すと装備品タブ。所持アイテム 3 種を持たせ、↓ にボビン爆弾、← に布カッター、→ にまち針をセット → HUD が「↓ ボビン爆弾 x1 / ← 布カッター x1 / → まち針 x1」。はずすで HUD が「---」に戻る。選び直し中の戻るは画面を閉じない。
- 収集品: 入手済みのハートピン・糸巻きの枠に絵、詳細の左に大きな絵、秘伝書は名前の文字。
- 資料: 14 行の仮文で送り 0〜8 行、つまみが下まで動く。短い本文では目印が出ない。
- オーバーレイ: 仮想パッド (`PadBegin`: 入力設定を複製してフォーカス無視、実機のパッドを一時無効) で R3 0.3 秒 / 0.5 秒は変化なし、0.75 秒で表示 / もう一度で非表示。Select はアイテム画面の開閉だけ。
- 通知: 赤ハサミ・落下攻撃・大ジャンプ・横突進を TestPlayActions で付与 → 2 行の通知 (パッド / キーボード両方。キーボードは CLI から仮想キーが届かなかったので `ForceDevice` で切替)。
- スクリーンショット: `Assets/Screenshots/UI/r3_equip_picker.png` / `r3_equip_assigned.png` / `r3_collection_icons.png` / `r3_collection_detail.png` / `r3_journal_scroll.png` / `r3_overlay_r3.png` / `r3_notice_superjump_kb.png` / `r3_notice_red_pad.png`。

