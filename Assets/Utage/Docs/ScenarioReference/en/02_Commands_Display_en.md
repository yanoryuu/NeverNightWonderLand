# Command Reference: Display

Text, characters, backgrounds, event CGs, sprites, particles, layer operations.
For shared specs (PageCtrl, WaitType, tags) see [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md).
**Note**: fade seconds are **Arg6** for most commands in this chapter.
**Exceptions**: Particle, ParticleOff, and LayerReset have no fade-seconds argument (Particle appears instantly, ParticleOff specifies how to clear via Arg2, LayerReset resets immediately).

## Text display (narration)

Leave Command and Arg1 blank and write text in the Text column.

| Column | Meaning |
|---|---|
| Text | Displayed text |
| PageCtrl | Page-feed control (blank = wait for page break) |
| Voice | Voice file name (Sound sheet registration; blank = none) |

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl
,,,,,,,,今日も良い天気ですね。,
,,,,,,,,雨が降ってきた…,Br
```

## Dialogue / character display

Leave Command blank and put the character name in Arg1.

| Argument | Meaning | Value |
|---|---|---|
| Arg1 | Character name | Label registered in the Character sheet. An unregistered name shows in the name field only (no portrait) |
| Arg2 | Display pattern | Pattern (expression etc.) from the Character sheet. Blank = keep previous. `<Off>` hides the portrait, dialogue only |
| Arg3 | Layer name | Blank = default layer. Only one character can be shown per layer |
| Arg4 / Arg5 | X / Y position | Number (added to layer position). Blank = unchanged |
| Arg6 | Fade seconds | Blank = 0.2 s |
| Text | Dialogue | Blank = show character only |
| Voice | Voice | Sound sheet registration name |

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text
,うたこ,笑い,,,,,,「こんにちは！」
,うたこ,<Off>,,,,,,「（立ち絵なしでセリフだけ）」
,太郎,通常,layer1,100,,,,「複数キャラはレイヤーを分ける」
```

## CharacterOff (hide character)

| Argument | Meaning | Value |
|---|---|---|
| Arg1 | Target | Character name; a layer name hides everything on that layer; blank = all character-type layers |
| Arg6 | Fade seconds | Blank = 0.2 s |

## Bg (background display) / BgOff

Bg also cancels event-CG display mode. The background object is automatically named "BG" (used as the target for Tween etc.).

| Command | Arg1 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|
| Bg | Texture label (Texture sheet registration name, required) | Layer name (blank = default BG layer) | X / Y position (blank = unchanged) | Fade seconds (blank = 0.2 s) |
| BgOff | — | — | — | Fade seconds (blank = 0.2 s) |

※Bg's Arg2 is unused.

```csv
Bg,学校前
Bg,学校前,,BG,0,0,1.0
BgOff,,,,,,0.5
```

## BgEvent (event CG display) / BgEventOff

Shows an event CG and automatically turns character display OFF. Use the Bg command to leave event mode.

| Command | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| BgEvent | Texture label (Event-type registration in the Texture sheet, required) | Mode switch (FALSE keeps portraits visible; default TRUE = portraits OFF) | Layer name (blank = default BG layer) | X / Y position (blank = unchanged) | Fade seconds (blank = 0.2 s) |
| BgEventOff | — | — | — | — | Fade seconds (blank = 0.2 s) |

```csv
BgEvent,回想シーン1
BgEventOff,,,,,,0.5
```

## Sprite (sprite display) / SpriteOff

| Command | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| Sprite | Sprite name (unique, required. "Bg", "MessageWindow", "Graphics" are reserved and cannot be used) | Texture label (Texture sheet registration name. Blank = same as Arg1) | Layer name (blank = default) | X / Y position (default 0) | Fade seconds (blank = 0.2 s) |
| SpriteOff | Target: sprite name / layer name / `AllSpriteObjects` / blank (= the whole sprite layer) | — | — | — | Fade seconds (blank = 0.2 s) |

To show the same image more than once, give each instance a distinct Arg1 and specify the same label in Arg2. Later ones draw in front.

```csv
Sprite,ball1,ball,,100,50
Sprite,ball2,ball,,200,50
SpriteOff,ball1,,,,,0.3
```

## Particle (particle display) / ParticleOff

| Command | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 |
|---|---|---|---|---|
| Particle | Particle name (unique, required) | Particle label (Particle sheet registration name. Blank = same as Arg1) | Layer name (blank = default) | X / Y position (default 0) |
| ParticleOff | Target: particle name / layer name (blank = clear all) | How to clear: blank = follow prefab setting / `Clear` = delete immediately / `StopEmitting` = stop emitting and fade naturally | — | — |

※ParticleOff has no fade-seconds argument (Arg6) (see the exception note at the top of this chapter).

```csv
Particle,fireworks,firework1,,300,100
ParticleOff,fireworks,StopEmitting
```

## LayerOff / LayerReset / ChangeLayer (layer operations)

| Command | Arg1 | Arg2 | Arg3 | Arg6 |
|---|---|---|---|---|
| LayerOff | Layer name (required) | — | — | Fade seconds (blank = 0.2 s) |
| LayerReset | Layer name or `All` (required) | — | — | — |
| ChangeLayer | Target object (character name, Bg name, etc.) | Position policy: `KeepGlobal` (keep on-screen position, default) / `KeepLocal` (keep local position) / `ResetLocal` (reset to initial) | Destination layer name | Fade seconds (blank = 0.2 s) |

- **LayerOff**: hides all objects on a layer.
- **LayerReset**: restores a layer changed by Tween/Shake etc. to its initial state (no fade, resets instantly).
- **ChangeLayer**: moves a displayed object to another layer. If the destination layer already has a character/background, it fades out automatically. The movement animation itself is done with Tween.

```csv
LayerOff,キャラ中央,,,,,0.5
LayerReset,All
ChangeLayer,うたこ,KeepGlobal,サブレイヤー,,,0.3
```

