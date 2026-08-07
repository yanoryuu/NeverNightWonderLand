# 宴（Utage）シナリオリファレンス

Unity用ノベルゲームエンジン「宴（Utage）」のシナリオデータ（Excel/CSV）の構文・コマンド一覧リファレンス。

- 対象バージョン: 宴4（4.2.9時点）
- 各項目の背景説明・図解は公式サイト（https://madnesslabo.net/utage/ ）を参照
- English version: [../en/README.md](../en/README.md)（この日本語版が正本）

## 使い方

宴のシナリオはExcel（.xls/.xlsx）またはCSVで記述する。シナリオを作成・編集する際は:

1. まず [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) で列構成・ラベル・PageCtrl・タグを把握する
2. 使うコマンドの引数仕様を 02〜06 で確認する
3. キャラ名・画像ラベル・サウンドラベル・変数は [07_SettingSheets_ja.md](07_SettingSheets_ja.md) の各シートに定義されているものだけを使う（未定義名はインポートエラーになる）
4. 既存のシナリオファイル・設定シートがある場合は、必ずそれらを読んで実在するラベル・キャラ名に整合させる
5. インポートでエラーが出た場合は [08_CommonErrors_ja.md](08_CommonErrors_ja.md) を参照して修正する

## 目次

| ファイル | 内容 |
|---|---|
| [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) | ファイル構造・列・ラベル・マクロ・コメント・PageCtrl・WaitType・テキストタグ |
| [02_Commands_Display_ja.md](02_Commands_Display_ja.md) | テキスト・キャラ・背景・イベントCG・スプライト・パーティクル・レイヤー |
| [03_Commands_Effects_ja.md](03_Commands_Effects_ja.md) | Tween・Shake・フェード・アニメ・エフェクト・カメラ・スレッド |
| [04_Commands_Sound_Wait_ja.md](04_Commands_Sound_Wait_ja.md) | BGM/SE/ボイス/環境音・Wait系 |
| [05_Commands_FlowControl_ja.md](05_Commands_FlowControl_ja.md) | Param・Jump・選択肢・If・サブルーチン・終了系 |
| [06_Commands_UI_Integration_ja.md](06_Commands_UI_Integration_ja.md) | メッセージウィンドウ・GUI・SendMessage系 |
| [07_SettingSheets_ja.md](07_SettingSheets_ja.md) | Character/Texture/Sound/Layer/Param/Localize/SceneGallery/Particle/Animation/EyeBlink/LipSynch |
| [08_CommonErrors_ja.md](08_CommonErrors_ja.md) | インポート・実行時エラーの読み方・典型的な間違いと対処・検出されない落とし穴 |
| [09_Localization_ja.md](09_Localization_ja.md) | Localizeシート・シナリオ言語列・BlankTextType・skip_page・ボイスのローカライズ・言語切替 |

## 最小サンプル（シナリオシートCSV）

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl,Voice,WindowType
*Start
Bg,背景その1
,,,,,,,,物語のはじまり。,
,うたこ,通常,,,,,,「こんにちは！」,
,うたこ,笑い,,,,,,「今日はいい天気だね」,
Selection,*ルートA,,,,,,,散歩に行く,
Selection,*ルートB,,,,,,,家にいる,
*ルートA
,うたこ,,,,,,,「散歩日和だね！」,
Jump,*エンド
*ルートB
,うたこ,ため息,,,,,,「たまには外に出ようよ…」,
*エンド
EndScenario
```

注意点:
- キャラ名（うたこ）はCharacterシート、背景ラベル（背景その1）はTextureシートに定義済みであること
- ラベル `*Start` から実行が始まる（既定の開始ラベル。AdvEngineStarterの設定で変更可）
- 各セリフ行は既定で改ページ待ちになる（PageCtrl空欄）

