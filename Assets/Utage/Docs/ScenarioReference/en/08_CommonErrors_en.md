# Common Errors and Fixes

How to read the errors Utage prints to the Unity console when importing/running scenarios, and how to fix typical mistakes.
Sources: direct inspection of the error-output sites in the code + hands-on verification.

## How to read errors

Most import-time errors appear in the console in this form (measured example):

```
存在しない背景 is not contained in file setting
MyScenario:3 <color=#ff0000ff> Bg, 存在しない背景,</color>
<b>Assets/.../MyScenario.csv</b>  : 3
```

- Line 1 is the error message, line 2 is `<SheetName>:<LineNumber>` plus the offending row's contents (red markup), line 3 is the file path with the line number. **The row contents are included in the log**, so the cause can be located from the log alone.
- **The line number counts from the top of the file, header row included, 1-based.** For CSV, sheet name = file name (without extension).
- Commands are built and validated at import time, so mistakes in arguments, labels, and defined names are **mostly detected before execution**. If the import passes with no errors, most of the notation is correct.
- Errors come out **all together in one import** (in phases: argument/name errors → duplicate labels → If syntax → Param expressions → broken label links). Log order does not match row order; use the `SheetName:LineNumber` in each log entry when fixing.
- Error messages appear in Japanese or English depending on the environment's language setting (this document shows the Japanese forms; hard-coded English messages are shown as-is).

### Fixing steps

1. Locate the offending row via `SheetName:LineNumber`
2. Check the name in the message (label, character name, etc.) against the project's setting sheets (each sheet in [07_SettingSheets_en.md](07_SettingSheets_en.md))
3. Fix, re-import, and confirm the error is gone

## Import-time error list

| Mistake | Error message | Fix |
|---|---|---|
| Undefined image label in Bg/BgEvent/Sprite/Particle etc. | `<label> is not contained in file setting` (followed by `Not contains <label> in Texture sheet`) | Use a label defined in the Texture sheet |
| Undefined sound label in Bgm/Se/Ambience/Voice etc. | `<label> is not contained in file setting` | Use a label defined in the Sound sheet |
| Undefined layer name in Arg3 etc. | `<name> is not contained in layer setting` | Use a layer name defined in the Layer sheet |
| Undefined pattern (expression etc.) of a defined character | `<char>, <pattern> is not contained in Character Sheet` | Use a pattern name from the Character sheet |
| Jump/Selection destination label does not exist | `<label>: はリンク先が存在しないシナリオラベルです` | Add the label definition row (`*LabelName`) or fix the destination name |
| Duplicate scenario label | `<label>: 二重に定義されたシナリオラベルです` (**no line number**, file name only) | Rename one of them (consider `**localLabel` for sheet-local). **Fix this error first**: the section under the duplicate label is dropped from the import, so Param-expression errors etc. inside it go undetected |
| Missing `*` on a label reference | `<value>:はシナリオラベルではありません` | Destinations must be written as `*LabelName` |
| Command name typo (e.g. `Bgg`) | `不正な記述です` | Note it does **not** say "unknown command name". Check the Command column spelling against the command list (chapters 02–06) |
| Param expression error (undefined variable, `;` chaining, etc.) | `<token> :不明なパラメーターです。` etc. (`式の結果がbool型ではありません` / `不正な式の記述です`) | Only variables defined in the Param sheet may be used. **Multiple assignments cannot be joined with `;` in one cell** (split into separate Param rows) |
| If-family syntax error (ElseIf/Else/EndIf without If) | `Syntax error in if-else commands.Set the if command first.` (this one is English regardless of language setting) | Match up If–ElseIf–Else–EndIf |
| Non-numeric value in a numeric column (typical symptom of column shift) | `<n> 列目のデータ: <value> を値に変換できません。正しい書式で入力してください` followed by `列:<column> は存在しないかデータが空です。` as a pair | **Suspect a column shift.** Check comma counts and argument positions (especially time = Arg6) |
| Invalid WaitType value | `UNKNOWN WaitType` | Only the values listed in chapter 01 are valid in the WaitType column |
| Unclosed `"` (quote) in CSV | `Invalid CSV format. <file path> Double quotes are not closed. (line:<start line number>)` | The message shows the file path and the line where the unclosed `"` began. That CSV is excluded from the import, but **importing other CSVs is not interrupted and continues**. See chapter 01 for quoting rules |

## Mistakes NOT caught by the import (pitfalls)

- **An undefined character name is not an error.** A name not in the Character sheet placed in Arg1 passes as "name display only (no portrait)". A typo in a character name only shows up as "the art doesn't appear", so after generating, cross-check character name spellings against the Character sheet.
- A column shift whose values happen to fit the expected types passes silently (e.g. a string intended as a layer name gets interpreted as a different string argument). A clean import does not guarantee argument positions are correct.
- Some errors only appear at runtime: wrong target-object names in SetPivot/Tween/GUI commands (`<name> is not found` etc.), scene object names for the SendMessageByName family — anything depending on the runtime scene state.
- Runtime error example: with no Layer sheet in the project at all, Bg etc. import fine but throw a NullReferenceException at runtime on older versions (newer versions auto-add default layers, so this is resolved). If you hit an unexplained NRE, check whether a Layer sheet exists.

