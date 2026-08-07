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

### スクリプトから呼ぶ場合

```csharp
DialogueService.Play("Shop_Greeting");                    // 再生するだけ
DialogueService.Play("Shop_Greeting", () => { /*終了時*/ }); // 終了コールバック付き
bool playing = DialogueService.IsPlaying;                  // 再生中か
```

- DialogueScene が未ロードでも自動で Additive ロードされる (Build Settings 登録済みが前提)
- 再生中は GamePause で時間停止、終了で自動解除
- 文字送り・改ページは **マウスクリック** (キー送りは未実装)

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
