# Command Reference: Effects

See [01_ScenarioBasics_en.md](01_ScenarioBasics_en.md) for the WaitType column spec.
**Note**: effect fade/time values are, as a rule, **Arg6**, and the wait mode goes in the **WaitType column**.

## Practical pattern: run an effect in parallel with text display

The message window is not shown after a page break until the next text command executes.
So if you place a window-targeting effect (`Shake,MessageWindow` etc.) alone between texts,
**the effect runs on a hidden window and nothing appears to happen on screen**.

The standard idiom is to set the effect command's WaitType to `PageWait` so it doesn't block, and let it run in parallel with the text that follows.
`NoWait` also runs in parallel, but `PageWait` (which waits for the effect at page-break time) is safer (saving during the effect is not a problem).

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text
Shake,MessageWindow,,time=0.5 x=10 y=10,,,,PageWait
,うたこ,,,,,,,「テキスト表示と同時にウィンドウが揺れる」
```

Likewise, running an effect while its target (background, character, etc.) is not on screen produces no visible result. Arrange effects to run while their target is visible.

## Tween (general-purpose animation)

Columns: Arg1 = target, Arg2 = TweenType, Arg3 = parameters, Arg4 = EaseType, Arg5 = LoopType + WaitType column

**Arg1 (target)**: `MessageWindow` (message window) / `Graphics` (all graphics) / `Camera` (camera; blank-equivalent = main camera, or a name for a specific camera) / anything else resolves as an object name or layer name (character name, sprite name, `BG`, layer name, etc.; for layers, scale values are specified in 1/100).
The Shake command shares the same parsing logic, so it accepts the same values as Tween.

**Arg2 (TweenType)**:

| Group | TweenType | Description |
|---|---|---|
| Move | MoveTo / MoveFrom / MoveBy | Move to / from / by the given position |
| Move | PunchPosition / ShakePosition | Bounce back / shake back |
| Rotate | RotateTo / RotateFrom / RotateBy | Rotate to / from / by the given angle |
| Rotate | PunchRotation / ShakeRotation | Bouncing / jittering rotation |
| Scale | ScaleTo / ScaleFrom / ScaleBy | Scale to / from / by |
| Scale | PunchScale / ShakeScale | Bouncing / jittering scale |
| Color | ColorTo / ColorFrom | Change color to / from |

**Arg3 (parameters)**: `name=value` pairs separated by spaces.

| Parameter | Meaning |
|---|---|
| time | Seconds (blank = 1 s, 0 = instant) |
| speed | Speed instead of time |
| delay | Start delay in seconds (default 0) |
| x, y, z | Deltas (meaning depends on TweenType) |
| islocal | true = local coordinates / false = global (default) |
| alpha / r,g,b,a / color | Color and transparency (0.0–1.0) |

**Arg4 (EaseType)**: `linear` `spring` and `easeIn/Out/InOut` × `Quad/Cubic/Quart/Quint/Sine/Expo/Circ/Bounce/Back/Elastic` (e.g. `easeOutQuad`).
**The default when blank is `easeOutExpo`** (fast at first, decelerating sharply toward the end — note this is not `linear`).
Exception: `ColorTo`/`ColorFrom` default to `linear` when blank (easing a color change is barely perceptible, so it's a special case; confirmed in the iTween-side code).
**Also note**: the `Punch*` family (PunchPosition/PunchRotation/PunchScale) and the `Shake*` family (ShakePosition/ShakeRotation/ShakeScale) do not call the shared easing function `ease()` used by Move/Scale/Rotate/Color; instead they run on dedicated fixed curve functions (`punch()`, or for Shake, linear decay that uses `percentage` directly), so **whatever you put in Arg4 has no effect** (see the Shake section below for details).

**Arg5 (LoopType)**: `loop=count` (0 = infinite) / `pingPong=count` (back and forth). **Default when blank is no loop (plays once only)**.

```csv
Tween,うたこ,MoveTo,time=2 x=400 y=300,easeOutQuad
Tween,BG,ColorTo,time=1.5 alpha=0.5
Tween,sprite1,ScaleTo,time=1 x=1.5 y=1.5,,loop=2
```

## Shake

A simplified form of Tween (subclasses `AdvCommandTween` with TweenType fixed to `ShakePosition`).

| Arg1 | Arg2 | Arg3 | Arg4 | Arg5 |
|---|---|---|---|---|
| Target (same spec as Tween; see above) | Unused (TweenType is fixed to `ShakePosition`) | Parameters (`name=value` separated by spaces. **Default `x=30 y=30`**. For the meaning of time/delay etc., see the Tween Arg3 table above) | **Has no effect no matter what you specify** (see note below) | LoopType (assumed shared with Tween; supports repeating via loop/pingPong) |

**Note on Arg4 (EaseType)**: it is accepted as a value without error, but it has no effect on the shaking behavior.
In the iTween implementation itself, the progress `percentage` is always computed linearly (`runningTime/time`), and easing is only applied once each Tween type's `Apply*Targets()` function individually calls `ease(start,end,percentage)`. However Shake's `ApplyShakePositionTargets()` never makes that `ease()` call — it uses the linear `percentage` only as the "shake-amplitude decay" (`1-percentage`), jumping randomly every frame via `UnityEngine.Random.Range`.
So no matter what you set EaseType to, the shake always settles down linearly.

```csv
Shake,MessageWindow,,time=0.5 x=10 y=10
Shake,Camera,,time=0.3 x=5
```

## FadeOut / FadeIn (full-screen fade)

A color fade on the camera. **FadeIn is only valid after a FadeOut** (to start a scene dark, first do a 0-second FadeOut).
The default target is SpriteCamera (backgrounds, characters, sprites), so fading has no visible effect when nothing is displayed.

| Command | Arg1 | Arg2 | Arg3 | Arg4 | Arg6 |
|---|---|---|---|---|---|
| FadeOut | Fade color (color name or `#RRGGBB`/`#RRGGBBAA`. Default white) | Camera name (default SpriteCamera. `UICamera` also applies it to the UI layer) | Rule image file name (optional) | Rule boundary value 0.01–1.0 (default 0.2) | Fade seconds (number or a keyframe name from the Animation sheet. Default 0.2 s) |
| FadeIn | Same as above | Same as above | Same as above | Same as above | Same as above |

※Arg5 is unused on both commands.

```csv
FadeOut,black,,,,,1
FadeIn,black,,,,,1
```

## RuleFadeIn / RuleFadeOut (per-object rule-image fade)

| Command | Arg1 | Arg2 | Arg3 | Arg6 | WaitType column |
|---|---|---|---|---|---|
| RuleFadeIn | Target object name (required) | Rule image name (required) | Size of the transition band 0.01–1.0 (default 0.2) | Fade seconds or keyframe animation name (default 0.2 s) | Wait mode |
| RuleFadeOut | Same as above | Same as above | Same as above | Same as above | Same as above |

```csv
RuleFadeIn,BG,ルール画像1,0.3,,,1
RuleFadeOut,BG,ルール画像1,0.3,,,1
```

## CaptureImage (screen capture)

Captures the current screen into an object (used for scene transitions combined with rule fades).

| Command | Arg1 | Arg2 | Arg3 |
|---|---|---|---|
| CaptureImage | Created object name (required) | Camera to capture (required) | Display layer name (required) |

```csv
CaptureImage,capture1,SpriteCamera,サブレイヤー
RuleFadeIn,capture1,ルール画像1,0.3,,,1
```

## PlayAnimation (keyframe animation playback)

Requires a definition in the Animation sheet.

| Argument | Meaning |
|---|---|
| Arg1 | Target (character name / layer name) |
| Arg2 | Animation name (defined in the Animation sheet) |
| Arg3 | Include in save data TRUE/FALSE (default TRUE) |
| WaitType column | Wait mode |

```csv
PlayAnimation,うたこ,揺れ
```

## ImageEffect / ImageEffectOff (image effects, built-in RP)

| Command | Arg1 | Arg2 | Arg3 | Arg6 | WaitType column |
|---|---|---|---|---|---|
| ImageEffect | Camera name (SpriteCamera = backgrounds/characters only / UICamera = whole screen incl. UI) | Effect name: GrayScale / Sepia / NegaPosi / Blur / MotionBlur / Bloom / Mosaic / FishEye / Twirl / Vortex | Keyframe animation name (optional) | Fade seconds (blank = 0) | Wait mode |
| ImageEffectOff | Same as above | Effect name or `All` | Keyframe animation name (optional; same argument layout as ImageEffect) | Fade seconds (blank = 0) | Wait mode |

※On URP projects, the URP support package is required (PostEffect is recommended).

```csv
ImageEffect,SpriteCamera,Sepia,,,,1
ImageEffectOff,SpriteCamera,Sepia,,,,1
```

## PostEffect / PostEffectOff (post effects, URP)

URP only. Controls effects per camera Volume (AdvPostEffectVolume).

| Command | Arg1 | Arg2 | Arg3 | Arg6 | WaitType column |
|---|---|---|---|---|---|
| PostEffect | Camera name (required) | Volume name (required; object name under Volumes in the scene) | Effect names (comma-separated; blank = all effects in the volume) | Fade seconds (blank = 0) | Wait mode |
| PostEffectOff | Same as above | Volume name (blank = all volumes except CaptureVolume/FadeVolume) | — | Fade seconds (blank = 0) | Wait mode |

Note: using the same effect type in multiple volumes simultaneously gives undefined priority.

```csv
PostEffect,SpriteCamera,MainVolume,Bloom,,,1
PostEffectOff,SpriteCamera,MainVolume,,,,1
```

## ZoomCamera

| Command | Arg1 | Arg2 | Arg3 | Arg4 | Arg6 | WaitType column |
|---|---|---|---|---|---|---|
| ZoomCamera | Camera name | Zoom factor (blank = 1) | Zoom center X (blank together with Arg4 = keep the current center) | Zoom center Y | Animation seconds (default 0.2) | Wait mode |

Always restore the factor to 1 after the effect (restoring to 1 also resets the center to 0,0).

```csv
ZoomCamera,SpriteCamera,1.5,0,0,,1
ZoomCamera,SpriteCamera,1,,,,1
```

## SetPivot / ResetPivot

Change the pivot for rotation and scaling.

| Command | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| SetPivot | Object name | Pivot X: 0–1.0 or Left/Center/Right | Pivot Y: 0–1.0 or Bottom/Center/Top | Offset X / Y (default 0) | Type: SpritePos (default) / SpritePosLocal / SpritePosNoSize / WorldSpace / Direct |
| ResetPivot | Object name | — | — | — | — |

Note: changing the pivot doesn't move the visual position but does change coordinates, which can affect later animations.

```csv
SetPivot,うたこ,Center,Bottom,,,SpritePos
Tween,うたこ,RotateBy,time=1 z=360
ResetPivot,うたこ
```

## Vibrate

| Command | Function |
|---|---|
| Vibrate | Vibrates the device on Android/iOS (no arguments, duration not configurable) |

```csv
Vibrate
```

If unused, adding `UTAGE_IGNORE_VIBRATE` to Scripting Define Symbols avoids granting the Android VIBRATE permission.

## Video (movie playback)

Full-screen movie playback (plays as the background of the given camera. Distinct from the Character sheet's "FileType=Video" in [07_SettingSheets_en.md](07_SettingSheets_en.md), which is movie playback as a display object — do not confuse the two).

| Command | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| Video | Movie file name (under `Resources/<ProjectName>/Video/`; include the extension when using download distribution) | Camera name (required) | Loop TRUE/FALSE (default FALSE) | Click-to-skip TRUE/FALSE (default TRUE) |

The command itself waits for playback to finish, so **the WaitVideo command is not needed** (WaitVideo is for the FileType=Video case in chapter 07).
Source: `AdvCommandVideo.Wait()`.

```csv
Video,opening,SpriteCamera,FALSE,TRUE
```

※The legacy `Movie` command only survives as a constant (`IdMovie`) in `AdvCommandParser`; there is no matching case in the command-generation switch statement, so it does not function (a legacy, unimplemented command — do not use).

## Thread / WaitThread / EndThread (effect threads)

Run effects asynchronously with text display. Only Tween/effect commands can be used inside a thread (text and page-control commands cannot).

| Command | Arg1 | Arg2 |
|---|---|---|
| Thread | Scenario label of the thread | — |
| WaitThread | Scenario label | Cancelable TRUE/FALSE (default FALSE) |
| EndThread | — | — |

Place `EndThread` at the end of the thread.

```csv
Thread,*揺れ演出
WaitThread,*揺れ演出
,うたこ,,,,,,,「スレッドの完了を待ってから続く」

*揺れ演出
Shake,うたこ,,time=2 x=10 y=10
EndThread
```

To make cross-page effects survive save/load, enable "Restart Sub Thread" on AdvSaveManager (on load, the thread restarts from its beginning).

## SkipEffect (force-skip effects)

Force-ends running effects.

| Command | Arg1 | Arg2 |
|---|---|---|
| SkipEffect | Type to skip: `All` (default; all effects currently waiting) / `NoWait` (only NoWait effects) | Also skip looping effects TRUE/FALSE (default FALSE) |

```csv
SkipEffect
SkipEffect,All,TRUE
```

