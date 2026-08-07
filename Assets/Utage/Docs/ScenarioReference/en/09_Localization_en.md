# Localization (multilingual support)

Utage's multilingual support splits broadly into two systems.

- **Translating scenario text (Text column)**: add language-name columns to scenario sheets
- **Translating UI text, character names, gallery entries, etc.**: register key/per-language text in the `Localize` setting sheet

Both are centered on the `LanguageManagerBase` class.

## Localize sheet

| Column | Meaning |
|---|---|
| Column 1 | Key (identifier used for lookup) |
| Column 2+ | Language-name columns (names following Unity's SystemLanguage enum are recommended: Japanese, English, ...) |

Register wording outside the scenario body here — character names (NameText), gallery titles, category names, UI text, etc.
The sheet contents are passed to `LanguageManagerBase.OverwriteData()` at import time and kept there (`AdvLocalizeSetting.cs`).

```csv
Key,Japanese,English
Title_Chapter1,第一章,Chapter 1
Category_Heroine,ヒロイン,Heroine
```

## Translating scenario text (language columns)

Add a column for the target language (e.g. `English`) to a scenario sheet's column headers, and write the translated text in that column at the same row as the Text column.

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl,English
,,,,,,,,,今日も良い天気ですね。,,It's a nice day today.
```

- Any language name you add must also be registered in `LanguageManagerBase`'s **Text Column Languages** (`TextColumnLanguages`) setting
  (import-time error checking and blank-cell detection only apply to columns whose language name is registered here).
- The currently displayed language is switched via `LanguageManagerBase.CurrentLanguage` (see "Switching languages" below).
- Which column actually gets read depends on the resolution logic controlled by the **Blank Text Type** setting described next.

## Blank Text Type (behavior when a translation cell is blank)

`LanguageManagerBase.BlankTextType` (enum `LanguageBlankTextType`) configures how a blank translation cell is handled. Three modes:

| Value | Behavior |
|---|---|
| SwapDefaultLanguage (legacy behavior) | If the current language's column is missing/blank, falls back to the `DefaultLanguage` column, and if that is also missing, to the Text column (the base language). Has fairly involved priority logic (`ParseCellLocalizedTextBySwapDefaultLanguage()`) |
| NoBlankText (recommended) | No fallback. A row whose translation cell is blank and whose PageCtrl column is also blank **errors at import time** (`<column> is empty cell. Set localize text`). Lets you mechanically detect untranslated rows. For rows you intend to leave blank on purpose, use the `<skip_page>` tag |
| AllowBlankText (not recommended) | No fallback. A blank translation cell does not error. **If every text command on a page has a blank translation, the whole page is automatically skipped** (`AdvScenarioPageData.CheckSkipByLocalize()`). However, missed translations can no longer be caught via errors |

(Verified in code: each BlankTextType branch in `LanguageManagerBase`, `AdvScenarioPageData.CheckSkipByLocalize()`)

## Page/text skip tags

| Tag | Applies to | Effect |
|---|---|---|
| `<skip_text>` | All languages | A general tag (not localization-specific) that skips the remaining display of that text |
| `<skip_page>` | Only the current language's cell | Write this single word in a translated-language column cell instead of text, and when that language is selected the whole page is skipped |

`<skip_page>` is evaluated by `LanguageManagerBase.CheckSkipPage()` and `AdvScenarioPageData.CheckSkipByLocalize()`.
The whole page is skipped **only when the target-language cell of every text command on the page is `<skip_page>`**
(if only some text is `<skip_page>` while the rest is translated, the page is still shown).
`<skip_page>` itself always works regardless of the BlankTextType setting.

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl,English
,,,,,,,,,このページは日本語版だけの小ネタです。,,<skip_page>
```

## Voice localization

- Turning off `LanguageManagerBase.IgnoreLocalizeVoice` (default `true`) and registering the target language in `VoiceLanguages`
  makes voice files load from a folder whose search path has the language name appended
  (`AdvBootSetting.GetLocalizeVoiceFilePath()`. The language name is concatenated onto the normal Voice folder name —
  e.g. for a `Voice` folder setup, place all voice files under a `VoiceEnglish` folder).
- If the target-language folder has no matching file, it falls back to the normal Voice folder.
- The text language and the voice language can be switched independently (`CurrentVoiceLanguage` prefers `VoiceLanguage`
  when it is set, and otherwise falls back to `CurrentLanguage`).

## Switching languages

```csharp
LanguageManagerBase.Instance.CurrentLanguage = "English";  // switch the displayed text language
LanguageManagerBase.Instance.VoiceLanguage = "English";    // switch the voice language independently
```

## Saving the language setting

Setting `AdvEngine.LanguageKeyOfParam` / `VoiceLanguageKeyOfParam` to a parameter name defined in the Param sheet causes
the current language setting to be automatically saved to that parameter and restored on next launch
(`AdvEngine.cs`'s `AutoChangeLanguageOnBoot()`/`ChangeLanguage()`. Because it is stored as a parameter, it is
automatically covered by normal save/load too).

## Applying this: switching images or branches by language

Writing a parameter conditional expression in the `Conditional` column (see chapter 07) of the `Character`/`Texture` sheets etc. lets you show a different row per condition.
If you're saving the language name into a parameter via `LanguageKeyOfParam`, you can use that parameter in a conditional expression to
apply this technique to things like "show a different image (e.g. a sign with embedded text) per language".

