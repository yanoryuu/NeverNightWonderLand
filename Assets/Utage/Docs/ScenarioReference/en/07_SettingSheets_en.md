# Setting Sheet Reference

Sheets that define characters, images, sounds, layers, and variables, separate from the scenario text.
Sheet names are fixed (Param tables allow the derived name `Name{}`, Animation allows `Name[]`).

## Character

The top row of each character's patterns is its default display.

| Column | Meaning | Value / default |
|---|---|---|
| CharacterName | The label Utage manages the character by (used in scenario Arg1) | Blank = same as previous row (required on the top row) |
| NameText | Name displayed on screen | Blank = same as previous / all blank = CharacterName. `<param=name>` tags allowed |
| Pattern | Pattern name for expressions/poses (used in Arg2) | Required when a character has multiple patterns |
| X / Y / Z | Position offset | Default 0 |
| Pivot | Image pivot | `Center` (default) / Top / Bottom / Left / Right / TopLeft… or `x=0.5 y=0.5` |
| Pivot0 | Pivot for tween animation | As above |
| Scale | Display scale | Default 1. Per-axis form `x=1.5 y=0.5` allowed |
| Conditional | Condition for conditional display (outfits, gender branches, etc.) | e.g. `clothId==1`. The row whose other conditions fail is the default |
| FileName | Image path (relative under the Character folder; bmp/jpg/png) | Required |
| FileType | `2D` (default) / `Dicing` / `Avatar` / `3D` / `Video` etc. | |
| SubFileName | File name inside a dicing atlas | Dicing only |
| AnimationState | Animator state name | For 3D models etc. |
| Animation | Flipbook animation name for dicing (defined in Animation sheet) | |
| RenderTexture / RenderRect / RenderTextureScale | Texture-write mode (`Image` etc.) / rect / scale | For 2D-ifying 3D models or prefabs |
| EyeBlink / LipSynch | Eye-blink / lip-sync setting names (defined in those sheets; Dicing/Avatar only) | |
| Icon / IconSubFileName / IconRect / IconAutoFlip | Face icon image / dicing name / crop rect from the portrait / flip linkage (default TRUE) | |

**FileType=Video**: specifying a video file in FileName displays it as an object that plays a movie instead of a still image
(this is a kind of image type, not a dedicated command — it can be used with any display command, e.g. Sprite/Bg/Character).
Wait for playback to finish with the WaitVideo command from [chapter 04](04_Commands_Sound_Wait_en.md). Full-screen movie
playback is the separate [Video command](03_Commands_Effects_en.md) (that command waits for its own completion, so WaitVideo is not needed).
※`FileType=Video` is specified the same way on both the Character and Texture sheets (this section shows a Character sheet example, but the Texture sheet accepts it identically).

**Note on FileType (Dicing/Avatar/3D/RenderTexture)**: any FileType other than `2D` (the default) assumes assets have already been
prepared in the Unity Editor (e.g. `Dicing` requires conversion via the Dicing Converter, `Avatar` requires a texture already split into parts,
`3D` requires a model/Animator set up in a scene or prefab). Because preparing these assets is mostly GUI work in the Unity Editor,
this text-focused (CSV/code) reference does not cover it. See the official docs ([About graphic objects](https://madnesslabo.net/utage/?page_id=8810)).

```csv
CharacterName,NameText,Pattern,FileName
うたこ,うたこ,通常,utako_normal.png
,,笑い,utako_smile.png
太郎,太郎,通常,taro_normal.png
```

## Texture (backgrounds, event CGs, sprites)

| Column | Meaning | Value |
|---|---|---|
| Label | Identifier (used by Bg/BgEvent/Sprite commands) | Required |
| Type | `Bg` / `Event` / `Sprite` | Required |
| FileName | Relative image path (bmp/jpg/png; if the extension is omitted: Bg/Event=jpg, Sprite=png) | Required |
| X / Y / Z / Pivot / Scale / Conditional / FileType / SubFileName | As in the Character sheet | |
| Thumbnail | Thumbnail path for the CG gallery | For Event |
| CgCategolly | CG gallery category name | For Event |

Backgrounds without alpha should be jpg (less memory).

```csv
Label,Type,FileName
学校前,Bg,school_gate.jpg
教室,Bg,classroom.jpg
回想1,Event,event01.jpg
ball,Sprite,ball.png
```

## Sound

| Column | Meaning | Value / default |
|---|---|---|
| Label | Identifier (used by Bgm/Se/Ambience/Voice commands) | Required |
| Type | `Bgm` / `Se` / `Ambience` | |
| FileName | Relative audio path (wav/mp3/ogg; omitted extension = wav) | Required |
| Title | Track title in the sound room (localizable; blank = hidden) | |
| IntroTime | Intro seconds for intro-looping (loop without splitting files) | Blank = no intro |
| Volume | Volume | Default 1.0 |

```csv
Label,Type,FileName,Title
メインテーマ,Bgm,main_theme.ogg,メインテーマ
ドアの音,Se,door.wav,
街の喧騒,Ambience,street.ogg,
```

## Layer (drawing layers)

Drawing groups, comparable to uGUI Canvases. The first row of each Type is the default layer for that type. Only one character/background can be shown per layer.

For any Type with no layer defined, default layers named "Bg Default", "Character Default", "Sprite Default" are added automatically, so display commands work even if the Layer sheet is empty (header only), or if no Layer sheet exists in the project at all. Define rows when you want to control positions and draw order.

```csv
LayerName,Type,X,Y,Order
背景,Bg,0,0,0
スプライト,Sprite,0,0,100
キャラ中央,Character,0,-300,200
```

| Column | Meaning | Value / default |
|---|---|---|
| LayerName | Layer name | Required |
| Type | `Bg` / `Character` / `Sprite` | Required |
| X / Y | Layer center position | Default 0 |
| Order | Draw order (-32768–32767; Z = -Order/SortOrderToZUnit) | Required |
| LayerMask | Unity layer name | Default = same as GraphicManager |
| ScaleX / ScaleY | Layer scale | Default 1 |
| FlipX / FlipY | Flip | Default FALSE |
| Width / Height | Layer size | Default = screen size |
| BorderLeft/Right/Top/Bottom | Margins | |
| Align | Placement (TopLeft–BottomRight) | Default = center |

## Param (scenario variables)

| Column | Meaning | Value |
|---|---|---|
| Label | Variable name | Required |
| Type | `Int` / `Float` / `Bool` / `String` | Required |
| Value | Initial value | Required |
| FileType | `Default` (normal save) / `System` (system save, shared globally) / `Const` (constant, not saved) | Default: Default |

**When initial values apply**: every time the game starts from the beginning (AdvEngine.StartGame), `Default`-class variables are automatically reset to their sheet Value (`System` carries over from the system save, `Const` is always the sheet value).
They reset at the start of replay loops too, so there's no need to re-initialize them explicitly at the start of a scenario.

```csv
Label,Type,Value,FileType
love,Int,0,Default
flag_met,Bool,FALSE,Default
player_name,String,あなた,Default
```

### Expressions (used in Param commands, If conditions, Selection conditions, etc.)

- Arithmetic: `+ - * / %`
- Comparison: `== != >= <= > <`
- Logical: `&& || !`
- Assignment: `= += -= *= /= %=`
- Parentheses: `( )`
- Built-in functions: `Random(min,max)` (integer) / `RandomF(min,max)` (float) / `Ceil` `CeilToInt` `Floor` `FloorToInt`

```
point+=1
flag_a=true
(flag1 && flag2) || (point>3)
point=Random(1,6)
```

### Access from C#

```csharp
engine.Param.GetParameterInt("name");      // variants exist for Int/Float/Bool/String
engine.Param.SetParameterInt("name", 100);
// Accessing before initialization is an error; check engine.Param.IsInit
```

## ParamTbl (parameter table, sheet name `Name{}`)

Append `{}` to the sheet name (e.g. `StatusTbl{}`). Rows/columns are transposed relative to the normal Param sheet.

| Row | Contents |
|---|---|
| Row 1 | Parameter names |
| Row 2 | Types (Int/Float/Bool/String) |
| Row 3 | FileType |
| Rows 4+ | One record per key (one record per row) |

Access notation: `TableName[key].paramName` (e.g. `StatusTbl[うたこ].hp`). Usable from both scenarios and C#.

```csv
Name,hp,mp
Type,Int,Int
FileType,Default,Default
うたこ,100,50
太郎,80,30
```

**Do not leave the leading cell of rows 1–3 blank.** Write a reserved-looking label such as `Name`/`Type`/`FileType` that indicates the row's role
(the value itself is not parsed by code, but by convention it marks the meaning of the row).
Leaving it blank shifts the column alignment and causes misbehavior (`AdvParamStructTbl.AddTbl()` reads rows 1–3 as the header and rows 4+ as
data rows; the implementation of `AdvParamStruct.ToIndexCommentOuted()` counts column position by "the order in which non-blank cells appear",
so a blank leading cell shifts the starting point, and tables with more parameters increasingly run their later columns out of range,
causing an import error). The leading column from row 4 onward becomes the key.

## Localize (multilingual UI text)

Column 1 = Key, columns 2+ = language names (per Unity's SystemLanguage enum: Japanese, English, ...).
Used to translate wording outside the scenario body itself — character names, gallery titles, category names, UI text, and the like.
Scenario text (Text column) translation instead uses language-name columns added to scenario sheets ([01_ScenarioBasics_en.md](01_ScenarioBasics_en.md)).
For the whole localization feature (adding language columns, behavior when blank, skip_page, voice language switching, etc.), see [09_Localization_en.md](09_Localization_en.md).

## SceneGallery

| Column | Meaning |
|---|---|
| ScenarioLabel | Start label of the recollection (required) |
| Title | Title displayed in the gallery UI (localizable) |
| Thumbnail | Thumbnail relative path (required) |
| Categolly | Category (per character, etc.) |

Always place an `EndSceneGallery` command at the end position of the recollection.

```csv
ScenarioLabel,Title,Thumbnail
*回想1,うたこと出会った日,thumb01.png
*回想2,文化祭の思い出,thumb02.png
```

## Particle

| Column | Meaning |
|---|---|
| Label | Identifier (used by the Particle command) |
| FileName | Prefab relative path under `Resources/<ProjectName>/Particle/` |
| X / Y / Z / Pivot / Scale / Conditional / SubFileName | As in the Character sheet (parsed by the shared graphic-info parser, so these are also usable) |

```csv
Label,FileName
花吹雪,sakura.prefab
花火,firework1.prefab
```

## Animation (keyframe animation; derived sheet name `Name[]` allowed)

| Row | Contents |
|---|---|
| Row 1 | Animation label, WrapMode (loop setting), `Linear` (sharp interpolation; omitted = smooth) |
| Row 2 | Keyframe times (seconds) |
| Rows 3+ | Property name and value per keyframe |

Properties: `X Y Z` / `Scale ScaleX ScaleY ScaleZ` / `Angle AngleX AngleY AngleZ` / `Alpha` / `R G B` / `Texture` (flipbook) / `Pattern` (character pattern switch).
Component properties also work: e.g. `Utage.FishEye.strengthX`. Coordinates are local.
Used by the PlayAnimation command and by keyframe specs of FadeIn/RuleFade etc. (`Utage.ColorFade.strength` and the like).

```csv
*揺れる,Loop
Time,0,0.5,1
Y,0,-10,0
```

**Row 1 must always start with `*`** (`*LabelName`; `AdvAnimationSetting.IsHeader()` checks `row[0][0]=='*'`).
**On row 2 (keyframe times), the leading cell is discarded** (`ParseTimeTbl()` reads times starting only from index 1),
so put a dummy string (e.g. `Time`) in the leading cell. The same applies to property rows from row 3 on — the leading cell is the
property name, and the values from column 2 onward line up with the sequence of times.

## EyeBlink (eye blink) / LipSynch (lip sync)

For Dicing / Avatar types only. Linked via the EyeBlink / LipSynch columns of the Character sheet.

**EyeBlink**:

| Column | Meaning | Default |
|---|---|---|
| Label | Identifier (written as `*LabelName`; a leading `*` is required because it's parsed by `AdvCommandParser.ParseScenarioLabel`) | Required |
| IntervalMin / IntervalMax | Blink interval (seconds; randomized within this range) | 2 / 6 |
| RandomDouble | Probability of a double blink (0–1) | 0.2 |
| Tag | Tag used for texture switching | eye |
| Name0/Duration0, Name1/Duration1, ... | Texture name and display seconds per frame | — |

**LipSynch**:

| Column | Meaning | Default |
|---|---|---|
| Label | Identifier (written as `*LabelName`; a leading `*` is required because it's parsed by `AdvCommandParser.ParseScenarioLabel`) | Required |
| Type | `Text` / `Voice` / `TextAndVoice` | TextAndVoice |
| Interval | Switch interval (seconds) | 0.2 |
| ScaleVoiceVolume | Multiplier for how wide the mouth opens relative to volume | 1 |
| Tag | Tag used for texture switching | lip |
| Name0/Duration0, Name1/Duration1, ... | Texture name and display seconds per frame | — |

**There is no upper limit on the number of Name0/Duration0-style frames** (`MiniAnimationData.TryParse` keeps reading
"name, seconds" pairs rightward from the `Name0` column without limit, until both are blank. Adding columns like
Name5/Duration5 lets you use a 6th frame and beyond).
Texture names are `*_patternName` (appended to the base image name, e.g. `*_e0`) or direct names. With consistent naming, the same data can be reused across characters.

```csv
Label,IntervalMin,IntervalMax,Name0,Duration0,Name1,Duration1
まばたき1,2,6,*_e0,0.1,*_e1,0.1
```

## Boot (startup settings, reserved sheet)

System sheet for resource file management, version settings, etc. Normally used as-is from the template; rarely edited on the scenario side.

