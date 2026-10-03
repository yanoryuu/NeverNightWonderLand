# 会話システム (Utage) の使い方

ゲーム内会話は Unity アセット「宴 (Utage)」で実装している。
会話は専用シーン `DialogueScene` (Additive) 上で再生され、再生中はゲームが完全ポーズ (Time.timeScale = 0) になる。

- 参考: 宴マニュアル「会話シーンとして宴を使う」 https://madnesslabo.net/utage/?page_id=402

---

## 1. 会話を書く (シナリオの追加・編集)

シナリオ本体は **`Assets/Dialogue/Scenarios/Dialogue.xls`** を **Excel で直接編集**する (Utage の標準ワークフロー)。

- 会話は **Start シート**に書く。増えてきたらシートを追加してよい
  (シート名は自由。ただし `Character` `Texture` `Sound` `Param` `Layer` `Localize` `SceneGallery` `Macro` `Boot` `Scenario` は設定用の予約名なので避ける)
- Character / Layer / Texture などの設定シートも同じ xls にある

### 書式

| 列 | 意味 |
|---|---|
| Command | `*ラベル名` (会話の開始点) / `EndScenario` (会話の終わり) などのコマンド |
| Arg1 | 話者名 (Characterシート未登録の名前はそのまま名前表示のみになる) |
| Text | セリフ本文 |

| Command | Arg1 | Text |
|---|---|---|
| *Shop_Greeting | | |
| | 店主 | いらっしゃい。夜しか開いてない店だけど、品揃えは悪くないよ。 |
| | 店主 | 支払いは糸玉で頼むよ。この園じゃ、糸が何よりの通貨なのさ。 |
| EndScenario | | |

- 1つの会話 = `*ラベル` から `EndScenario` まで。ラベル名は全シートで一意にする
- 1行 = 1ページ (クリックで次の行へ)
- 話者なしの行 (Arg1を空欄) は地の文になる
- 空行は無視されるので、会話の区切りに入れてよい
- テンプレート由来のサンプルシナリオ行が残っている場合は削除してよい

### 編集後の反映

xls を保存 → メニュー **NeverNight > 会話シナリオを再インポート** を実行する。

### 注意

- **列の位置に注意** (実際に起きた事故): Start シートの列順は
  `Command | Arg1 | Arg2 | ... | Text(I列) | ...`。セリフ本文は必ず **I列 (Text)** に書くこと。
  外部からコピペするときに3列目 (Arg2) にセリフが入ると、エラーは出ないのに
  「テキストなしのキャラ表示コマンド」扱いになり、会話が一瞬で終わって何も表示されなくなる
- Claude が xls に書き込む時は、必ず バックアップ → NPOI で書き込み → 読み戻し検証 (Text=I列、Arg2 に入れない、既存行が変わっていない) → 再インポート の順で行う
- xls が壊れた場合はテンプレートの
  `Assets/Utage/Templates/Template/Template/Scenarios/Template.xls` をコピーして復元する
  (プロジェクトルートに `Dialogue_backup.xls` のバックアップもある)
- Utage は `.tsv` / `.csv` のシナリオファイルにも対応しているが、本プロジェクトは xls 運用に統一 (2026-08-07)

---

## 2. 会話をゲーム内で再生する

### NPC に付ける場合 (基本)

1. シーンに GameObject を置き、**Collider2D** (IsTrigger可) を付ける
2. レイヤーを SavePoint 等と同じ **インタラクト用レイヤー** にする
3. **`NpcTalkPoint`** コンポーネントを付け、`Scenario Label` にラベル名 (例: `Shop_Greeting`、`*` は不要) を設定

プレイヤーが近づくと「話す」プロンプトが出て、E キーで会話が始まる。

任意 (2026-10-03 追加、未設定なら従来どおり毎回 `Scenario Label`):
- `Repeat Label` + `Talked Flag`: 初回の会話の終わりに Talked Flag (GameProgress) が立ち、2 回目以降は Repeat Label を再生する
- `Reward Thread`: 初回の会話の終わりに糸を渡す (Talked Flag があれば一度きり)

鍛冶師 (`Blacksmith`) も場面ごとのラベル (`_lockedLabel` / `_lockedRepeatLabel` / `_grantLabel` / `_forgeLabel` / `_afterLabel`) で Utage 会話を再生できる (Docs/Area1/Gimmicks.md)。

### エリア1 のラベル (2026-10-03, Start シートの末尾)

| ラベル | 話者 | 使う所 |
|---|---|---|
| `A1_Doll_First` / `A1_Doll_Repeat` | 迷子の人形 | A1_H 迷子センター (初回の終わりに糸 50) |
| `A1_MapSellerU4_First` / `A1_MapSellerU4_Repeat` | 地図売り | A1_U4 観覧車の足元 |
| `A1_MapSellerD5_First` / `A1_MapSellerD5_Repeat` | 地図売り | A1_D5 蒸気管の迷路 |
| `A1_Smith_Locked` / `A1_Smith_LockedRepeat` | 木馬の鍛冶師 | A1_H 鍛冶師: 中ボス撃破前 (初回 / 2 回目以降) |
| `A1_Smith_Forge` | 木馬の鍛冶師 | 有償の鍛冶強化 (糸 30) の申し出。会話の後に糸を払って強化 |
| `A1_Smith_Grant` | 木馬の鍛冶師 | 赤ハサミを渡す時。会話の後に取得通知 |
| `A1_Smith_After` | 木馬の鍛冶師 | 赤ハサミも鍛冶強化も済んだ後 |

ラベル名はシーン (ビルダーのレシピ `Docs/Area1/SceneScripts/build_A1_*.cs`) が参照しているので、名前を変える時はレシピも直してシーンを作り直す。

### スクリプトから呼ぶ場合

```csharp
DialogueService.Play("Shop_Greeting");                    // 再生するだけ
DialogueService.Play("Shop_Greeting", () => { /*終了時*/ }); // 終了コールバック付き
bool playing = DialogueService.IsPlaying;                  // 再生中か
bool busy = DialogueService.IsBusy;                        // 再生中 or DialogueScene のロード中
bool canTalk = DialogueService.CanStartFromInteract;       // NPC の CanInteract 用 (busy でなく、閉じてから 0.35 秒経った)
```

- 会話は同時に 1 つだけ。再生中・ロード中の `Play` は警告ログを出して無視する (2026-10-03, §7)
- 会話で話す IInteractable (`NpcTalkPoint` / `Blacksmith` など) の `CanInteract` は `DialogueService.CanStartFromInteract` にする
  (`IsPlaying` だと、最初の会話のシーンのロード中や閉じた直後にもう一度話しかけられてしまう)
- 会話の終わりのコールバックから続けて `Play` するのは可 (鍛冶師の 解禁前の会話 → 鍛冶強化の申し出)。クールダウンはインタラクトだけに掛かる
- 会話中はゲーム側の Screen Space - Overlay の UI (HUD・StageUI など、sortingOrder 100 未満) を隠し、終わったら戻す (§7)

- DialogueScene が未ロードでも自動で Additive ロードされる (Build Settings 登録済みが前提)
- 再生中は GamePause で時間停止、終了で自動解除
- 会話を開いたフレームには、ゲームのボタンでは文字送りしない (開いたインタラクトの E / 上入力で 1 ページ目が飛ばないように)
- 文字送り・改ページは **マウスクリック** に加えて、キーボード **Enter / テンキー Enter / Space / J / E**、パッド **South (×/A) / West (□/X) / 上入力** (= ゲームの決定・ジャンプ・攻撃・インタラクト) (2026-10-03)。
  宴の公式の入力の差し替え口 `Utage.InputUtil.SetInputStrategy` に `Assets/Scripts/Dialogue/DialogueInputStrategy.cs` (宴の `InputSystemStrategy` を継承し `IsInputNextButton` を広げる) を入れている。
  `UtageDialogueScene` が会話のたびに `DialogueInputStrategy.Install()` する (宴の初期化 (BeforeSceneLoad) と順番が決まらないため)。会話中 (`DialogueService.IsPlaying`) 以外は宴の既定のまま。
  テストプレイの仮想入力 (`PlayerInputSource.Override` の Attack / Interact / Jump のタップ) でも送れる (ボットは会話中これで読み進める)。

---

## 3. 会話のデバッグ

### 会話テストシーン (推奨)

**`Assets/Scenes/Test/DialogueTestScene.unity`** を開いてプレイ。
画面左に全シナリオラベルのボタン一覧が出るので、クリックだけで任意の会話を再生できる。
ボタン一覧はプレイ開始時にインポート済みのシナリオデータから自動取得するため、
会話を追加してもシーンの更新は不要 (再インポートだけ忘れずに)。

### Debug Panel

ゲームプレイ中に **NeverNight > Debug Panel** の「会話 (Utage)」セクションからも再生できる。
どのステージにいても動くので、実際のゲーム進行と組み合わせた確認に使う。

---

## 4. セットアップについて

初期構築用のエディタメニュー (構築/修復) は **UnityMCP 接続に伴い廃止済み** (2026-08-07)。
構築は完了しており、シーン側の調整も DialogueScene に保存済みなので通常は触る必要がない。
再調整が必要になった場合は Claude Code に依頼する (MCP 経由で直接操作する)。

DialogueScene に適用済みの調整 (参考):

- `AdvTime.Unscaled = true` (ポーズ中でも会話が動く)
- セーブ機能の無効化 (`AdvSaveManager.isAutoSave = false` 等)
- 宴カメラの Overlay 化 (ゲームのメインカメラに URP カメラスタックで重ねる。スタック接続はランタイムで実施)
- メッセージウィンドウ透明度のコンフィグ連動解除

残っているメニューは **NeverNight > 会話シナリオを再インポート** (TSV 編集後の反映) のみ。

---

## 5. 立ち絵の表示 (未セットアップ・手順のみ)

立ち絵は Character シートへの登録で表示できる。画像素材が用意できたら以下の手順。

1. 画像を `Assets/Dialogue/Resources/Dialogue/Texture/Character/` に置く
2. `Dialogue.xls` の **Character シート**にキャラを登録する

   | CharacterName | Pattern | NameText | FileName |
   |---|---|---|---|
   | Annaijin | 通常 | 案内人 | annaijin_normal.png |
   | Annaijin | 笑顔 | | annaijin_smile.png |

3. シナリオのセリフ行の **Arg2** に表情パターンを書く

   | Command | Arg1 | Arg2 | Text |
   |---|---|---|---|
   | | Annaijin | 笑顔 | ようこそ、ネバーナイト・ワンダーランドへ。 |
   | | Annaijin | \<Off\> | (立ち絵を消す) |

- 立ち絵は一度出すと会話終了まで維持される。`<Off>` または `CharacterOff` コマンドで消す
- 立ち位置 (左右) を分けたい場合は Layer シートにキャラ用レイヤーを定義して Arg3 で指定

---

## 6. 実装ファイル一覧

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Dialogue/DialogueService.cs` | 静的入口。`Play(ラベル)` でシーンロード込みで再生 |
| `Assets/Scripts/Dialogue/UtageDialogueScene.cs` | シーン内コントローラ (AdvEngine制御・ポーズ・カメラスタック) |
| `Assets/Scripts/Dialogue/NpcTalkPoint.cs` | NPC用 IInteractable |
| `Assets/Scripts/Dialogue/DialogueTestDriver.cs` | テストシーンのボタンUI (ラベルはインポート済みデータから自動取得) |
| `Assets/Scripts/Editor/DialogueScenarioImporter.cs` | 再インポートメニュー |
| `Assets/Scripts/Editor/DialogueLabelScanner.cs` | xls/tsv からのラベル抽出 (Debug Panel 用) |
| `Assets/Dialogue/Scenarios/Dialogue.xls` | シナリオ本体 (Excel で編集) |
| `Assets/Scenes/UI/DialogueScene.unity` | 会話シーン本体 (Additive・常駐) |
| `Assets/Scenes/Test/DialogueTestScene.unity` | 会話デバッグ専用シーン |
| `Assets/Dialogue/` | Utage シナリオプロジェクト (TSV・xls・書き出しアセット) |

## 7. トラブルシューティング

- **会話が表示されない** → コンソールの赤エラーを確認して Claude Code に相談 (DialogueScene の調整が壊れている場合は MCP 経由で再調整する)
- **ラベルが見つからないという警告** → 再インポート忘れ。「NeverNight > 会話シナリオを再インポート」を実行
- **インポートで例外** → `Dialogue.xls` の破損を疑う。テンプレートからコピーで復元 (上記 §1 注意欄)

### 2026-10-03 のバグ修正: 会話が表示されない / 会話を 2 つ開ける

ユーザー報告「描画順の問題で会話シーンが表示されないのと、2 枚会話シーンを開くことができます」。A1_H (PlayerScene 常駐 + UI シーン群) の人形・鍛冶師で再現して確認した。

**原因**

1. **DialogueScene の二重ロード** (2 枚開ける): 最初の会話では DialogueScene を Additive でロードするが、ロード中 (数フレーム) は
   `DialogueService.IsPlaying` が false のままだったので、NPC の `CanInteract` も true のまま。その間にもう一度インタラクトすると
   (E の連打・2 つの NPC など) `LoadSceneAsync` がもう一度走り、DialogueScene が 2 つ並んだ
2. **会話が見えない** (1 の結果): `UtageDialogueScene` はゲーム側カメラの cullingMask から Utage / UtageUI レイヤーを外すが、
   「自分の `_overlayCameras` 以外の全カメラ」を対象にしていたため、DialogueScene が 2 つあると **互いの宴カメラの cullingMask を 0 にし合い**、
   メインカメラのスタックに載った宴カメラが何も描かなくなった (ゲームは会話中のポーズで止まったまま、画面には何も出ない)。
   ユーザーのエディタに残っていた状態 (DialogueScene ×2、宴カメラ全部 mask=0) と、修正前の再現 (人形に 2 回続けて話す) で同じ
3. **描画順** (1 枚でも起きる): HUD・StageUI などゲーム側の UI は Screen Space - Overlay キャンバスで、URP では
   **カメラスタック (宴の会話 UI) より必ず手前に描かれる** (sortingOrder では下に回せない)。HUD の左下の枠・ミニマップ・体力などが
   会話の黒背景とメッセージウィンドウの上に重なっていた
4. (付随) 会話を連打で閉じた直後に、同じ NPC の会話がすぐ開き直せた

**修正**

- `DialogueService`: ロード中を `IsLoading` で持ち、ロード中の要求は完了後にまとめて実行 (2 回目の `LoadSceneAsync` はしない)。
  再生中・開始待ちの `Play` は無視。`IsBusy` / `CanStartFromInteract` (閉じてから `ReopenCooldown` = 0.35 秒) を追加。静的状態はプレイ開始時にリセット
- `UtageDialogueScene`:
  - Utage レイヤーを外す対象から DialogueScene のカメラを除外 (自分以外の DialogueScene のカメラも消さない)
  - それでも二重にロードされた時は、後から来た方が Start で自分のシーンをアンロードする (保険)
  - 会話中はゲーム側の Screen Space - Overlay キャンバス (sortingOrder 100 未満。FadeCanvas 500 は残す) を `Canvas.enabled = false` で隠し、終わりに戻す
  - 会話の終わりの後始末 (カメラスタック・UI・ポーズ) を 1 か所にまとめ、会話中にシーンが破棄されても後始末する
- `NpcTalkPoint` / `Blacksmith`: `CanInteract` を `DialogueService.CanStartFromInteract` に
- `DialogueInputStrategy`: 会話を開いたフレームにはゲームのボタンで送らない
- Debug Panel / DialogueTestDriver: `IsBusy` で無効化

**確認 (プレイモード, A1_H)**: 人形に 2 回続けてインタラクト → 修正前は DialogueScene ×2・画面に何も出ない
(`Assets/Screenshots/Dialogue/before_double_open.png`)、修正後は 1 つだけで会話が表示され HUD は隠れる (`after_double_open.png`)。
E を 2 フレームに 1 回連打 → 閉じてから 0.35 秒は開き直さない、糸 50 は一度だけ、HUD は戻る。鍛冶師も同様 (`after_smith.png`)、
解禁前の会話 → 鍛冶強化の申し出の連続再生は従来どおり。DialogueScene を手動で二重ロードしても後の方が消え、会話は表示される。

**黒背景について (未決定・変更していない)**: DialogueScene の `Canvas-BackGround` (ClearCamera の Screen Space - Camera, 全画面の不透明な黒) が
会話中はステージを完全に隠す。描画順のバグとは別 (修正後も黒背景の上に会話が出る)。残すか・半透明にするか・消すかはユーザーの判断待ち。


### 2026-10-03 追補: 「DialogueScene が二重にロード」「There are 2 event systems」の警告

シーン遷移を繰り返した後にコンソールにこの 2 つの警告が出ていた件を調査した。

**調査結果**

- PlayerScene 方式の通常プレイ (はじめから A1_T1 → T2 → T1 → T2 → T1 → T2 → T3 → T4 → B1 → A1_H、人形と会話 → B1 へ出て戻る → もう一度会話、
  セーブ復帰・リトライ (PlayerScene ごと読み直し)、話しかけた同じフレームでの遷移) では **DialogueScene は常に 1 つ**、EventSystem も 1 つ
  (ゲーム側のシーンには EventSystem が無く、DialogueScene の 1 つだけ)。警告は出ない。`DialogueService` の静的状態も遷移では失われない
  (遷移は Additive の入れ替えで、DialogueScene は降ろされない)
- エディタログ (Editor.log) に残っていた警告は 2 回とも、上の修正の確認で **DialogueScene を意図的に二重ロードしたテスト** の直後のもの。
  1 回目は保険 (後の方のアンロード) を入れる前のコードで、2 つ目の EventSystem が残ったまま「There are 2 event systems」が毎フレーム 5000 回以上出ていた
  (コンソールを消していなかったので遷移後に見えていた)
- ただし、実際に二重ロードになる経路が 2 つあった:
  1. **テストプレイのワープ一覧に DialogueScene が出ていた**。「ステージ = PlayerScene でも、名前が UI で終わるシーンでもないもの」という規約で判定していたが、
     DialogueScene は名前が UI で終わらない。オーバーレイの「その他のシーン」→ DialogueScene → デフォルト入口で
     `StageLoader.TransitionTo("DialogueScene")` が走り、**2 つ目の DialogueScene がステージとして Additive ロード**され (両方の警告が出る)、
     元のステージも降ろされて足場の無い状態になっていた
  2. `StageLoader.FindLoadedStage` (エディタでステージを直接開いて再生した時) も同じ判定で、DialogueScene をエディタで一緒に開いていると
     それを現在のステージと見なしうる (次の遷移で DialogueScene が「旧ステージ」として降ろされる)
- 保険が動いた時も、2 つ目の EventSystem はアンロードまでの数フレーム有効だった (`There can be only one active Event System` + `There are 2 event systems` が出る)

**修正**

- `StageLoader.IsStageSceneName(name)` を追加 (PlayerScene・名前が UI で終わるシーン・`DialogueService.SceneName` を除く)。
  `FindLoadedStage` と `TestPlayActions.StageScenes` (デバッグパネル・テストプレイのワープ一覧) はこれで判定し、`TransitionTo` はステージ以外を警告して拒否する
- `UtageDialogueScene` を `[DefaultExecutionOrder(-10000)]` にし、二重ロードの 2 つ目は Awake で自分以外のルート (EventSystem・AdvEngine など) を
  すぐ非アクティブにする (同じシーンの EventSystem の OnEnable より先に止まるので EventSystem の警告は出ない)。アンロードは従来どおり Start で。
  EventSystem の重複判定も「このシーン以外に EventSystem があるか」に直した

**確認 (プレイモード)**: 上の通常プレイの手順を通しでコンソールの警告 0 件、各段階のシーン一覧は PlayerScene + UI 9 つ + ステージ (+ 会話後は DialogueScene 1 つ)、
会話は表示 → 送り → 閉じる (糸の受け渡し・2 回目の短い会話も従来どおり)。`TestPlayActions.Warp("DialogueScene")` は拒否 (遷移せず警告 1 件)、
ワープ一覧に DialogueScene が出ない。DialogueScene を手動で二重ロード → 「二重にロード」の警告 1 件だけで EventSystem の警告は出ず、後の方が消えて会話も正常。

(付記・未修正) 会話中に遷移すると、遷移先のステージ (StageInfo / SceneTransitionZone) が `GamePause.Reset` するため会話中なのにポーズが外れる。
通常は会話中に遷移できない (時間が止まっている) ので実害は小さいが、テスト用の強制遷移では起きる。
