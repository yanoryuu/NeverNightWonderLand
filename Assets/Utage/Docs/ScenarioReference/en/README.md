# Utage Scenario Reference

A reference for the syntax and command list of scenario data (Excel/CSV) for "Utage", the visual novel engine for Unity.

- Target version: Utage 4 (as of 4.2.9)
- See the official site (https://madnesslabo.net/utage/ ) for background explanations and illustrations.
- 日本語版 (Japanese version): [../ja/README.md](../ja/README.md) — the Japanese version is the master document; this English version is generated from it.

## How to use

Utage scenarios are written in Excel (.xls/.xlsx) or CSV. When creating or editing a scenario:

1. First read [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md) for the column layout, labels, PageCtrl, and text tags
2. Check the argument specs of the commands you use in chapters 02–06
3. Use only character names, texture labels, sound labels, and variables defined in the sheets described in [07_SettingSheets_en.md](07_SettingSheets_en.md) (undefined names cause import errors)
4. If your project already contains scenario files and setting sheets, always stay consistent with the existing labels and character names
5. If the import produces errors, consult [08_CommonErrors_en.md](08_CommonErrors_en.md) and fix them

## Table of contents

| File | Contents |
|---|---|
| [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md) | File structure, columns, labels, comments, PageCtrl, WaitType, text tags |
| [02_Commands_Display_en.md](02_Commands_Display_en.md) | Text, characters, backgrounds, event CGs, sprites, particles, layers |
| [03_Commands_Effects_en.md](03_Commands_Effects_en.md) | Tween, Shake, fades, animation, effects, camera, threads |
| [04_Commands_Sound_Wait_en.md](04_Commands_Sound_Wait_en.md) | BGM / SE / voice / ambience, Wait commands |
| [05_Commands_FlowControl_en.md](05_Commands_FlowControl_en.md) | Param, Jump, selections, If, subroutines, end commands |
| [06_Commands_UI_Integration_en.md](06_Commands_UI_Integration_en.md) | Message window, GUI, SendMessage family |
| [07_SettingSheets_en.md](07_SettingSheets_en.md) | Character/Texture/Sound/Layer/Param/Localize/SceneGallery/Particle/Animation/EyeBlink/LipSynch |
| [08_CommonErrors_en.md](08_CommonErrors_en.md) | Reading import/runtime errors, typical mistakes and fixes, silent pitfalls |
| [09_Localization_en.md](09_Localization_en.md) | Localize sheet, scenario language columns, BlankTextType, skip_page, voice localization, language switching |

## Minimal sample (scenario sheet CSV)

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

Notes:
- Character names (うたこ) must be defined in the Character sheet, background labels (背景その1) in the Texture sheet. Labels can be any language, including English.
- Execution starts from the label `*Start` (default start label; configurable via AdvEngineStarter)
- Each dialogue line waits for a page break by default (PageCtrl blank)

