# Command Reference: Flow Control & Logic

For scenario label notation see [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md).
For parameter (variable) definitions and expression syntax see the Param sheet in [07_SettingSheets_en.md](07_SettingSheets_en.md).

## Param (parameter operation)

Changes a variable defined in the Param sheet.
**One Param command executes exactly one expression.** To change multiple variables, use separate rows (joining with `;` etc. is not supported).
See the Param sheet for operator specs.

| Command | Arg1 |
|---|---|
| Param | Expression (e.g. `flag1=true` `point+=10`) |

```csv
Param,point+=10
Param,flag1=true
```

## Jump (scenario jump / automatic branching)

| Command | Arg1 | Arg2 |
|---|---|---|
| Jump | Destination scenario label (`*LabelName`) | Conditional expression (bool). Blank = unconditional. If false, skipped and the next row runs |

Consecutive Jumps form multi-way branching: only the first Jump whose condition holds executes.

```csv
Jump,*GoodEnd,point>=10
Jump,*NormalEnd,point>=5
Jump,*BadEnd
```

## JumpRandom (random branching)

**JumpRandom is meant to be placed consecutively** (a single one always jumps there). The whole consecutive run forms one draw group, from which one entry is picked at random.

| Command | Arg1 | Arg2 | Arg3 |
|---|---|---|---|
| JumpRandom | Destination label | Condition (false = excluded from the draw) | Probability weight (blank = 1; relative; parameter expressions allowed, e.g. `lv/2`) |

```csv
JumpRandom,*分岐先1,,5
JumpRandom,*分岐先2,,3
JumpRandom,*分岐先3,,1
```

## Selection (choices)

**Selection is meant to be placed consecutively.** Consecutive Selection rows are displayed together as choices.

| Command | Arg1 | Arg2 | Arg3 | Arg4 | Arg5 | Arg6 | Text |
|---|---|---|---|---|---|---|---|
| Selection | Destination label when chosen (required) | Display condition (false = hidden; blank = always shown) | Expression executed when chosen (flag setting etc.); runs right after selection, before the jump | Choice UI prefab name (default SelectionItem) | X position (free layout; must be given together with Arg6) | Y position (free layout; must be given together with Arg5) | Choice display text (required) |

```csv
Selection,*ルートA,,,,,,,選択肢A
Selection,*ルートB,flag_secret,,,,,,隠し選択肢
Selection,*ルートC,,point+=1,,,,,好感度が上がる選択肢
```

**If the display condition (Arg2) is false for every choice and none are shown**, processing does not wait for input and automatically
proceeds to the row right after the group (`AdvSelectionManager.TryStartWaitInputIfShowing()` returns `false` when there are zero choices).
If one or more are shown, one is always chosen and jumped to (the group is never passed through unselected).

## SelectionClick (branch by clicking a displayed object)

Branch by clicking characters, sprites, etc. Arg1–Arg3 are the same as Selection.

| Command | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| SelectionClick | Destination label | Enabling condition | Expression on selection | Clickable object name (character/sprite name) |

※Arg5 is unused (a deprecated leftover that remains in the code but has no effect in the spec; specifying it does nothing).

**SelectionClick is also meant to be placed consecutively** (as with Selection, the whole consecutive run is treated as one group).
Regular objects, dicing, and avatars are supported out of the box. Prefabs based on UGUI work automatically (AdvClickEvent is generated); custom hit shapes need an `IAdvClickEvent` implementation.

```csv
,うたこ,笑い,,,,,,「クリックして話しかけてみて」
SelectionClick,*Route1,,,うたこ
SelectionClick,*Route2,,,BG
```

## If / ElseIf / Else / EndIf (conditionals)

| Command | Arg1 |
|---|---|
| If | Condition (bool) |
| ElseIf | Condition |
| Else | none |
| EndIf | none |

**Important restriction**: do not put scenario text (page processing) inside If–EndIf. Use it for conditional parameter operations and display commands; branch the scenario itself with Jump / Selection / subroutines.

```csv
If,point>=10
Bg,豪華な部屋
Else
Bg,普通の部屋
EndIf
```

## Subroutines

| Command | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| JumpSubroutine | Subroutine label | Condition | Return label after finishing (blank = return to the call site) | — |
| JumpSubroutineRandom | Subroutine label | Condition | Return label after finishing | Probability weight |
| EndSubroutine | — | — | — | — |
| ExitSubroutine | — | — | — | — |

EndSubroutine ends the subroutine and returns to the caller; ExitSubroutine unwinds every active subroutine and continues the original scenario (neither takes arguments).

- **JumpSubroutineRandom is also meant to be placed consecutively** (as with JumpRandom, the whole consecutive run forms one draw group).
- Nested calls are allowed.
- If the player saves inside a subroutine and the calling scenario is later updated, the return position can shift (avoid by specifying an explicit return label).
- **Scenario text (dialogue/narration) may be placed inside a subroutine.** A subroutine is nothing more than a jump to a label plus recording the return position
  (`AdvCommandJumpSubroutine`); it does not carry the "must not mix in text" restriction that If–EndIf has.

For macro notation, see [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md).

## EndPage / EndScenario / PauseScenario / EndSceneGallery

| Command | Function |
|---|---|
| EndPage | Marks an explicit page-break position (no arguments) |
| EndScenario | Ends the scenario and returns to the title screen etc. (no arguments) |
| PauseScenario | Suspends the scenario; resume from code via `AdvEngine.ResumeScenario()` (no arguments) |
| EndSceneGallery | End position for scene-gallery playback; gallery replays finish here (no arguments) |

**EndScenario automatically stops sound** (with default settings): BGM, ambience, and any looping sound always stop,
and voice also stops by default. SE (sound effects) do not stop by default (`AdvEngine`'s `IsStopSoundOnEnd`
defaults to true, `isStopVoiceOnSoundStop` defaults to true, `isStopSeOnSoundStop` defaults to false — all of these can be changed in the Inspector).

