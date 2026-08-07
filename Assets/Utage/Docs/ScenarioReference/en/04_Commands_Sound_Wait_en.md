# Command Reference: Sound & Wait

Labels are defined in the Sound sheet ([07_SettingSheets_en.md](07_SettingSheets_en.md)).
**Note**: fade/wait seconds are, as a rule, **Arg6**, wait mode goes in the **WaitType column**.

## Sound commands

### Se (sound effect) / StopSe

| Command | Arg1 | Arg2 | Arg3 | Arg6 |
|---|---|---|---|---|
| Se | SE label (required) | Loop TRUE/FALSE (default FALSE) | Volume 0–1 (default 1) | — |
| StopSe | SE label (blank = stop all SE) | — | — | Fade seconds (default 0.2) |

If the same SE is playing multiple times, StopSe stops every instance with that label.

```csv
Se,ドアの音
StopSe,ドアの音,,,,,0.5
```

### Bgm / StopBgm

| Command | Arg1 | Arg2 | Arg3 | Arg5 | Arg6 |
|---|---|---|---|---|---|
| Bgm | BGM label (required) | Loop TRUE/FALSE (default TRUE) | Volume 0–1 (default 1) | Fade-out seconds of the previous track (default 0.2) | Fade-in seconds (default 0) |
| StopBgm | — | — | — | — | Fade seconds (default 0.2) |

※StopBgm has no Arg1 (label). Only one BGM track ever plays at a time, so there is no need to choose which one to stop.

```csv
Bgm,メインテーマ
StopBgm,,,,,,0.5
```

### Ambience (ambient sound) / StopAmbience

| Command | Arg1 | Arg2 | Arg3 | Arg5 | Arg6 |
|---|---|---|---|---|---|
| Ambience | Ambience label (required) | Loop TRUE/FALSE (**default FALSE** — note this default differs from Bgm) | Volume 0–1 (default 1) | Fade-out seconds of the previous track (default 0.2) | Fade-in seconds (default 0) |
| StopAmbience | — | — | — | — | Fade seconds (default 0.2) |

※StopAmbience has no Arg1 (same as StopBgm, only ever one track).

```csv
Ambience,街の喧騒,TRUE
StopAmbience,,,,,,0.5
```

### Voice / StopVoice

Plays a voice at any timing, separate from normal dialogue voice playback (Voice column).

| Command | Arg1 | Arg2 | Arg3 | Voice column | Arg6 |
|---|---|---|---|---|---|
| Voice | Character label (required) | Loop TRUE/FALSE (default FALSE) | Volume 0–1 (default 1) | Voice file name (required) | — |
| StopVoice | — | — | — | — | Fade seconds (default 0.2) |

※StopVoice has no Arg1 (only ever one track).

```csv
Voice,うたこ,,,,,,,,,voice001.wav
StopVoice,,,,,,0.5
```

> **Caution**: auto page-feed (auto mode) waits for the voice to finish before breaking the page.
> If you leave a looping voice (Arg2=TRUE) playing, auto page-feed stays blocked until StopVoice is called,
> so use looping voices carefully (normal dialogue voices should not loop).

### StopSound (bulk stop) / ChangeSoundVolume (group volume change)

| Command | Arg1 | Arg2 | Arg6 |
|---|---|---|---|
| StopSound | Kind(s) (`Bgm` `Se` `Ambience` `Voice` `All`, comma-separated allowed. Blank = default `Bgm,Ambience`) | — | Fade seconds (**default 0.15**) |
| ChangeSoundVolume | Kind(s) (same as above, required; cannot be blank) | Volume 0–1 (required) | Fade seconds (default 0) |

**Caution (ChangeSoundVolume)**: the setting persists even after the track stops, so restore the volume explicitly.
Effective volume is determined by "config setting × Arg3 at play time × ChangeSoundVolume" multiplied together.

```csv
Bgm,メインテーマ
Se,ドアの音
ChangeSoundVolume,Bgm,0.3,,,,0.5
StopSound,All,,,,,1
```

## Wait commands

### Wait / WaitInput

| Command | Arg6 |
|---|---|
| Wait | Seconds to wait (required) |
| WaitInput | Timeout seconds for the input wait (omit to wait indefinitely for input) |

```csv
Wait,,,,,,1.5
WaitInput,,,,,,3
```

### WaitCustom

| Command | Arguments |
|---|---|
| WaitCustom | None. Waits for release from code — call `AdvEngine.UiManager.IsInputTrigCustom = true;` on the program side to release. Used to wait for custom UI interactions to complete |

```csv
WaitCustom
```

### WaitConditional

**Waits while the conditional expression holds** (proceeds once the expression becomes false — note this is NOT "wait until it becomes true").
An always-true expression makes the scenario wait forever.
Source: AdvCommandWaitConditional.Wait() (the keep-waiting condition is `within minimum wait time || expression is true`).

| Command | Arg1 | Arg6 |
|---|---|---|
| WaitConditional | Conditional expression (e.g. with `flag1==true`, it keeps waiting while flag1 is true and proceeds once it becomes false) | Minimum wait seconds (optional) |

```csv
WaitConditional,is_loading==true
```

### WaitFadeObjects

Wait for object fades to finish (character-display fades can't take WaitType directly, so use this instead).

| Command | Arg1 | WaitType column |
|---|---|---|
| WaitFadeObjects | Target(s) (comma-separated allowed): object name / layer name / `AllBgLayers` `AllCharacterLayers` `AllSpriteLayers` / `AllBgObjects` `AllCharacterObjects` `AllSpriteObjects` / `All` (default = All) | Wait mode (Skippable variants allowed) |

```csv
CharacterOff,うたこ,,,,,1
WaitFadeObjects,うたこ
```

### WaitEffectTime

A timed wait that supports WaitType.

| Command | Arg6 | WaitType column |
|---|---|---|
| WaitEffectTime | Seconds to wait (required) | Wait mode |

```csv
WaitEffectTime,,,,,,2,Skippable
```

### WaitSound

Wait for sound playback to finish.

| Command | Arg1 | Arg2 | WaitType column |
|---|---|---|---|
| WaitSound | Kind (`Bgm`/`Ambience`/`Voice`/`Se`) | Target name (Se = SE label, blank = all SE; Voice = character label, blank = all characters; not needed for Bgm/Ambience) | Wait mode (Skippable does not stop the audio itself) |

```csv
Se,足音
WaitSound,Se,足音
```

### WaitVideo

Wait for a video object to finish playing. The target's loop setting must be false.

| Command | Arg1 | WaitType column |
|---|---|---|
| WaitVideo | Video object name | Wait mode |

```csv
WaitVideo,opening_movie
```

