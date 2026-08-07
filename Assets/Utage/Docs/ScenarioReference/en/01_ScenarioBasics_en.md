# Scenario Writing Basics

Basic structure of Utage scenario files.

## File formats

- Excel files (.xls / .xlsx) or CSV files (Utage 4 and later).
- Excel: multiple sheets per file. CSV: one file = one sheet (**the file name without extension becomes the sheet name**).
- Sheets are either "setting sheets" (Character, Texture, Sound, Layer, Param, etc. → [07_SettingSheets_en.md](07_SettingSheets_en.md)) or "scenario sheets" (any other name).

### CSV physical spec

- Encoding: **UTF-8** (BOM optional; Utage's own tools output BOM, so matching that is safest)
- Line endings: CRLF or LF
- Quoting: RFC 4180. **Cells containing commas, line breaks, or `"` must be wrapped in `"`**. A `"` inside a cell is doubled as `""`. Inside quotes, a cell may contain line breaks.
- Trailing empty fields may be omitted (`Bg,背景1` — write only up to the last non-empty column)

## Scenario sheet columns

The first row holds column names. Columns are identified by name, so they can be reordered (except in Animation/EyeBlink/LipSynch sheets). Unused columns may be deleted.

```
Command, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, WaitType, Text, PageCtrl, Voice, WindowType, <LanguageName>...
```

| Column | Meaning |
|---|---|
| Command | Command name. If blank, see "Interpretation when Command is blank" below |
| Arg1–Arg6 | Command arguments; meaning differs per command. **Time values (fade/wait seconds) are, as a rule, Arg6** |
| WaitType | Wait mode for effect commands (see below) |
| Text | Displayed text (narration / dialogue) |
| PageCtrl | Page-feed control (see below) |
| Voice | Voice file name (name registered in the Sound sheet) |
| WindowType | Message window name to use |
| Language name (e.g. English) | Translated text column for localization |

## Interpretation when Command is blank

| Written state | Interpretation |
|---|---|
| Command blank + character name in Arg1 | Dialogue / character display (equivalent to Character command) |
| Command blank + Text only | Narration text (equivalent to Text command) |
| Command column is `*LabelName` | Scenario label definition |
| Command column is `//...` or `Comment` | Comment row (ignored) |

## Scenario labels

- `*LabelName` … globally unique label. Jump / Selection targets refer to it as `*LabelName`.
- `**LabelName` … local label (sheet-scoped). The same name may be reused across different sheets. The definition row is internally converted into the full name `SheetName*LabelName`.
  - **When referencing it from within the same sheet**, just write the same `**LabelName` used at the definition site (the reference side, e.g. Jump, goes through the same conversion logic and is automatically resolved against the sheet name that the referencing command belongs to).
  - **When referencing it from another sheet**, you must explicitly write the sheet name as `*SheetName*LocalLabelName` (a single leading `*`).
- **A sheet name itself also acts as a scenario label** (the top of each sheet is implicitly `*SheetName`; you don't need an explicit label row at the top — `Jump,*SheetName` jumps to the top of that sheet).
- The default start label is `Start` (configurable on AdvEngineStarter).
  **When using multiple scenario files (sheets), the label that is actually the entry point (default `Start`) must be defined on exactly one sheet across the whole project**
  (defining the same label twice is an error; see chapter 08). Other sheets automatically get their own sheet name as a label,
  so there is no need to duplicate `*Start` on them — jump into a given sheet with `Jump,*SheetName`.

## Commenting out

| Target | Notation |
|---|---|
| Whole sheet | Prefix the sheet name (Excel) or file name (CSV) with `#` |
| Whole column | Prefix the column name with `//` |
| Whole row | `//` in the first cell of the row, or `Comment` in the Command column |

Since a CSV file is one file = one sheet, prefixing the file name with `#` excludes that whole file from import (symmetric with Excel's sheet-name comment-out). Import targets are usually specified per folder, so this is handy for temporarily excluding work-in-progress or test CSVs.

## PageCtrl (page-feed control)

| Value | Behavior |
|---|---|
| (blank) | Wait for page-break input (default) |
| Input | Wait for input, then run next command (no line/page break) |
| InputBr | Wait for input, then line break |
| InputBrPage | Wait for input, then page break |
| Next | Run next command without waiting |
| Br | Line break without waiting, then continue |
| BrPage | Page break without waiting, then continue |

Line breaks inside text: `\n`. Alt+Enter also works in Excel.

**A row with Command, Arg1, and Text all blank and only PageCtrl filled in** does not error — it is interpreted as **a Text command with empty text**, and only the page feed executes
(because `AdvParser.IsEmptyTextCommand()` checks the PageCtrl column's content first).
Use this form when you want a page feed to happen on a row that has no text.

## WaitType (how effects wait)

Available on effect commands such as Tween, Shake, FadeIn/Out, PlayAnimation, ImageEffect.

| Value | Behavior |
|---|---|
| (blank) | Wait until the effect finishes before the next command |
| PageWait | Wait for the effect at page-break time |
| InputWait | Wait for the effect after click input |
| Add | Wait synchronized with the end of the next effect |
| NoWait | Continue immediately (saving during this period is discouraged) |
| Skippable | Wait for the effect, but clicking skips it |
| PageWaitSkippable / InputWaitSkippable / AddSkippable | Skippable variants of the above |
| SkippableOnWaitThread | Effect-thread only; skippable only while the main thread is in WaitThread |
| SkipOnInput | Click input force-ends the effect |
| SkipOnBrPage | Page break force-ends the effect |

※WaitType cannot be applied directly to character-display fades; use the WaitFadeObjects command instead.
※For effects targeting the message window, running them in parallel with text display via `PageWait` is the standard pattern (see "Practical patterns" in [03_Commands_Effects_en.md](03_Commands_Effects_en.md)).

## Text tags (TextMeshPro edition)

Standard TextMeshPro rich text tags (`<b>` `<i>` `<color>` `<size>` `<sprite>` etc.) work as-is except the page tag. Utage-specific tags:

| Tag | Syntax | Meaning |
|---|---|---|
| ruby | `<ruby=reading>target</ruby>` | Ruby (furigana) |
| em | `<em=char>target</em>` | Emphasis dots (character selectable, e.g. `<em=●>`) |
| param | `<param=variableName>` | Show a scenario variable's value |
| format | `<format={format}:variableName>` | Variable display with C# format (e.g. `<format={0,3}:num>`) |
| speed | `<speed=sec>target</speed>` | Seconds per character (`<speed=0>` = instant) |
| interval | `<interval=sec>` | Pause the text feed at this position for N seconds |
| tips | `<tips=ID>target</tips>` | Link to the TIPS feature |
| url | `<url=URL>target</url>` | Hyperlink (opens browser on click) |
| dash | `<dash=charCount>` | Long horizontal bar. **Deprecated** (do not use) |
| skip_text | `<skip_text>` | Skip remaining text display from this point on (a general tag usable regardless of language) |
| skip_page | `<skip_page>` | **Localization only**. Write this single word in a translated-language column cell instead of text, and when that language is selected the whole page is skipped (use it when a page does not need to be translated; every text command on the page must be `<skip_page>`). See [09_Localization_en.md](09_Localization_en.md) for details |

## Macros

A mechanism that lets you name a multi-line combination of commands (a fixed piece of staging, etc.) and invoke it from anywhere in a scenario with a single line.
It reduces both the effort of writing the same sequence repeatedly and the effort of rewriting every occurrence when it needs to change.
On the calling side, the call is simply replaced by the expanded command sequence, so it can be written anywhere in a scenario, just like a normal command.

Macros are defined on a sheet named `Macro` or `Macro`+number (`Macro1`, `Macro2`, ...) (checked via `AdvMacroManager.IsMacroName()`; other sheet names are not treated as macro sheets).
Definition: starts with `*MacroName` in the Command column, ends with `EndMacro`. Calling: just write the macro name in the Command column (callable from any scenario sheet).

- **Arguments**: referenced inside the macro as `%Arg1`, `%Text`, etc. (`%ColumnName`). The value of the corresponding column on the calling row is substituted in.
- **Default arguments**: the values written in each column of the `*MacroName` row are used when the caller leaves that column blank.
- **Entities**: writing `&ParameterName` inside a macro dynamically references the parameter's value at runtime (macro-only; not checked for errors at import time; does not work in columns that must be resolved before the command executes, such as PageCtrl).
- **Structured macros (Utage 4.2.6 and later)**: pass a bundle of values via the `Args` column in `prop1=value1,prop2=value2` form, and reference them inside the macro as `%ColumnName.PropertyName`. Used by assigning a StructuredMacroParser ScriptableObject to CustomProjectSetting.

Sample (Macro.csv from Sample/Scenarios, rewritten to the standard column order `Command,Arg1,Arg2,...` with no Args column):

```csv
//Fade-in/out background switch
*FadeBg
FadeOut,%Arg2,,,,,%Arg3
Bg,%Arg1
Wait,,,,,,%Arg4
FadeIn,%Arg2,,,,,%Arg5
EndMacro

//With default arguments
*FadeBgDefault,,white,.2,1,.2
FadeBg,%Arg1,%Arg2,%Arg3,%Arg4,%Arg5,%Arg6
EndMacro
```

※The actual Sample/Scenarios/Macro.csv is written with a header that includes the `Args` column (right after Command) used by structured macros, so copying it verbatim would shift every column by one. This sample's column positions have been adjusted on the assumption that the Args column is not used.

### Subroutine vs. macro

| Requirement | Subroutine | Macro |
|---|---|---|
| Call and return to an independent scenario | ○ | × |
| Boilerplate within a single page | × | ○ |
| Read-flag handling | at the callee, in one place | per usage site |
| Content varying by arguments | × (combine with parameters) | ○ (entities) |

For subroutine details, see the "Subroutines" section in [05_Commands_FlowControl_en.md](05_Commands_FlowControl_en.md).

