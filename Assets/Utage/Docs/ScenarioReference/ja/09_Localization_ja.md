# ローカライズ（多言語対応）

宴の多言語対応は大きく2系統に分かれる。

- **シナリオ本文（Text列）の翻訳**: シナリオシートに言語名の列を追加する方式
- **UIテキスト・キャラ名・ギャラリー等の翻訳**: `Localize`設定シートにキー・言語別テキストを登録する方式

いずれも `LanguageManagerBase` クラスが中核。

## Localizeシート

| 列 | 意味 |
|---|---|
| 1列目 | Key（参照用の識別子） |
| 2列目以降 | 言語名の列（UnityのSystemLanguage列挙型に準拠する名前を推奨: Japanese, English, ...） |

キャラ名（NameText）・ギャラリーのタイトル・カテゴリ名・UIテキストなど、シナリオ本文以外の文言をここに登録する。
シート内容はインポート時に`LanguageManagerBase.OverwriteData()`へ渡されて保持される（`AdvLocalizeSetting.cs`）。

```csv
Key,Japanese,English
Title_Chapter1,第一章,Chapter 1
Category_Heroine,ヒロイン,Heroine
```

## シナリオ本文の翻訳（言語列）

シナリオシートの列見出しに、対応言語名の列（例: `English`）を追加し、その行のText列と同じ位置に翻訳テキストを書く。

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl,English
,,,,,,,,,今日も良い天気ですね。,,It's a nice day today.
```

- 追加した言語名は、`LanguageManagerBase`の**Text Column Languages**（`TextColumnLanguages`）設定に登録しておく必要がある
  （インポート時のエラーチェックや空欄判定は、ここに登録された言語名の列だけを対象に行われる）。
- 現在の表示言語は `LanguageManagerBase.CurrentLanguage` で切り替える（下記「言語の切り替え」参照）。
- どの列を実際に読むかは、後述の **Blank Text Type** 設定によって解決ロジックが変わる。

## Blank Text Type（翻訳が空欄のときの挙動）

`LanguageManagerBase.BlankTextType`（enum `LanguageBlankTextType`）で、翻訳セルが空欄の場合の扱いを設定する。3種類:

| 設定値 | 挙動 |
|---|---|
| SwapDefaultLanguage（従来動作） | 現在の言語の列が無い/空なら、`DefaultLanguage`の列、それも無ければText列（既定言語）にフォールバックして表示する。複雑な優先順位ロジックを持つ（`ParseCellLocalizedTextBySwapDefaultLanguage()`） |
| NoBlankText（推奨） | フォールバックしない。翻訳セルが空でPageCtrl列も空の行は**インポート時にエラー**になる（`<列名> is empty cell. Set localize text`）。未翻訳を機械的に検出できる。意図的に空にしたい行は`<skip_page>`タグを使う |
| AllowBlankText（非推奨） | フォールバックしない。翻訳セルが空でもエラーにならない。**そのページの全テキストコマンドが空翻訳の場合、ページ全体が自動的にスキップされる**（`AdvScenarioPageData.CheckSkipByLocalize()`）。ただし未翻訳の見落としをエラーで検出できなくなる |

（コード裏取り: `LanguageManagerBase`の各BlankTextType処理、`AdvScenarioPageData.CheckSkipByLocalize()`）

## ページ／テキストのスキップタグ

| タグ | 対象言語 | 効果 |
|---|---|---|
| `<skip_text>` | 全言語共通 | そのテキストの以降の表示をスキップする一般タグ（ローカライズ専用ではない） |
| `<skip_page>` | 現在の言語のセルにのみ有効 | 翻訳先の言語列のテキストの代わりにこの1語だけを書くと、その言語選択時にページ全体がスキップされる |

`<skip_page>`の判定は `LanguageManagerBase.CheckSkipPage()` と `AdvScenarioPageData.CheckSkipByLocalize()` で行われる。
**ページ内の全テキストコマンドの当該言語セルが`<skip_page>`である場合のみ**、ページ全体がスキップされる
（一部のテキストだけ`<skip_page>`にしても、他が翻訳済みならページは表示される）。
BlankTextTypeの設定に関わらず`<skip_page>`自体は常に有効。

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl,English
,,,,,,,,,このページは日本語版だけの小ネタです。,,<skip_page>
```

## ボイスのローカライズ

- `LanguageManagerBase.IgnoreLocalizeVoice`（既定`true`）をオフにし、`VoiceLanguages`に対応言語を登録すると、
  ボイスファイルの検索パスに言語名を付加したフォルダから読むようになる
  （`AdvBootSetting.GetLocalizeVoiceFilePath()`。通常のVoiceフォルダ名に言語名を連結したパス、
  例: `Voice` フォルダ運用なら `VoiceEnglish` フォルダに全ボイスファイルを配置する）。
- 対応言語フォルダに該当ファイルが無い場合は通常のVoiceフォルダにフォールバックする。
- テキストの言語とボイスの言語は独立して切り替え可能（`CurrentVoiceLanguage`は`VoiceLanguage`が
  設定されていればそちらを優先し、無ければ`CurrentLanguage`を使う）。

## 言語の切り替え

```csharp
LanguageManagerBase.Instance.CurrentLanguage = "English";  // テキストの表示言語を切り替え
LanguageManagerBase.Instance.VoiceLanguage = "English";    // ボイスの言語だけ独立して切り替え
```

## 言語設定のセーブ

`AdvEngine.LanguageKeyOfParam` / `VoiceLanguageKeyOfParam` にParamシートで定義したパラメーター名を設定すると、
現在の言語設定が自動的にそのパラメーターへ保存され、次回起動時に復元される
（`AdvEngine.cs`の`AutoChangeLanguageOnBoot()`/`ChangeLanguage()`。セーブデータにパラメーターとして
含まれるため、通常のセーブ・ロードにも自動対応する）。

## 応用: 言語による画像・条件分岐の切り替え

`Character`/`Texture`シート等の`Conditional`列（07章参照）にパラメーター条件式を書くと、行ごとに表示を出し分けられる。
`LanguageKeyOfParam`で言語名をパラメーターに保存している場合、そのパラメーターを条件式に使うことで
「言語ごとに異なる画像（テキスト入り看板等）を出し分ける」といった用途に応用できる。

