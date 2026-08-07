# 設定シートリファレンス

シナリオ本文とは別に、キャラ・画像・音・レイヤー・変数などを定義するシート群。
シート名は固定（Paramテーブルは `名前{}`、Animationは `名前[]` の派生名も可）。

## Character（キャラクター設定）

各キャラクターの最上行のパターンがデフォルト表示になる。

| 列 | 意味 | 値・既定 |
|---|---|---|
| CharacterName | 宴が管理するキャラのラベル名（シナリオのArg1で使用） | 空欄=直前と同じ（最上行は必須） |
| NameText | 画面に表示する名前 | 空欄=直前と同じ／全空欄=CharacterName。`<param=名前>` タグ可 |
| Pattern | 表情・ポーズ等のパターン名（Arg2で使用） | 複数パターン時は必須 |
| X / Y / Z | 表示位置オフセット | 既定0 |
| Pivot | 画像の中心位置 | `Center`（既定）/ Top / Bottom / Left / Right / TopLeft… または `x=0.5 y=0.5` |
| Pivot0 | Tweenアニメの中心位置 | 同上 |
| Scale | 表示倍率 | 既定1。`x=1.5 y=0.5` の個別指定可 |
| Conditional | 条件付き表示の判定式（着替え・性別分岐等） | 例 `clothId==1`。他条件が不成立の行が既定 |
| FileName | 画像パス（Characterフォルダ以下の相対パス。bmp/jpg/png） | 必須 |
| FileType | `2D`（既定）/ `Dicing` / `Avatar` / `3D` / `Video` 等 | |
| SubFileName | ダイシング内のファイル名 | Dicing時のみ |
| AnimationState | Animatorのステート名 | 3Dモデル等 |
| Animation | ダイシング用パラパラアニメ名（Animationシート定義） | |
| RenderTexture / RenderRect / RenderTextureScale | テクスチャ書き込み方式（`Image`等）/ 矩形 / 倍率 | 3Dモデルやプレハブの2D化に使用 |
| EyeBlink / LipSynch | 目パチ・口パク設定名（各シート定義。Dicing/Avatarのみ） | |
| Icon / IconSubFileName / IconRect / IconAutoFlip | 顔アイコン画像／ダイシング名／立ち絵からの切り抜き矩形／反転連動（既定TRUE） | |

**FileType=Video**: FileNameに動画ファイルを指定すると、静止画の代わりに動画を再生するオブジェクトとして
表示される（専用コマンドではなく画像種別の一種）。Sprite/Bg/Character等どの表示コマンドでも使用可。
再生終了待ちは[04章](04_Commands_Sound_Wait_ja.md)のWaitVideoコマンド。全画面演出としての動画再生は
これとは別の[Videoコマンド](03_Commands_Effects_ja.md)（自前で再生完了を待つためWaitVideo不要）。
※`FileType=Video`はCharacter・Texture両シート共通の指定方法（本節はCharacterシートの例だが、Textureシートでも同様に指定できる）。

**FileType（Dicing/Avatar/3D/RenderTexture）に関する注意**: `2D`（既定）以外のFileTypeは、Unityエディタ上での
アセット準備が前提となる（例: `Dicing`はDicing Converterでの変換、`Avatar`はパーツ分割済みテクスチャの用意、
`3D`はモデル・Animatorのシーン/プレハブ設定）。これらのアセット作成手順自体はUnityエディタでのGUI操作が中心のため、
テキスト（CSV/コード）主体の本リファレンスでは扱わない。公式ドキュメント（[グラフィックオブジェクトについて](https://madnesslabo.net/utage/?page_id=8810)）を参照。

```csv
CharacterName,NameText,Pattern,FileName
うたこ,うたこ,通常,utako_normal.png
,,笑い,utako_smile.png
太郎,太郎,通常,taro_normal.png
```

## Texture（背景・イベントCG・スプライト）

| 列 | 意味 | 値 |
|---|---|---|
| Label | 識別ラベル（Bg/BgEvent/Spriteコマンドで使用） | 必須 |
| Type | `Bg` / `Event` / `Sprite` | 必須 |
| FileName | 画像相対パス（bmp/jpg/png。拡張子省略時 Bg/Event=jpg、Sprite=png） | 必須 |
| X / Y / Z / Pivot / Scale / Conditional / FileType / SubFileName | Characterシートと同様 | |
| Thumbnail | CG回想用サムネイルパス | Event用 |
| CgCategolly | CG回想のカテゴリ名 | Event用 |

アルファ不要な背景はjpg推奨（メモリ削減）。

```csv
Label,Type,FileName
学校前,Bg,school_gate.jpg
教室,Bg,classroom.jpg
回想1,Event,event01.jpg
ball,Sprite,ball.png
```

## Sound（サウンド）

| 列 | 意味 | 値・既定 |
|---|---|---|
| Label | 識別ラベル（Bgm/Se/Ambience/Voice系コマンドで使用） | 必須 |
| Type | `Bgm` / `Se` / `Ambience` | |
| FileName | 音声相対パス（wav/mp3/ogg。拡張子省略=wav） | 必須 |
| Title | サウンドルームでの曲名表示（ローカライズ可。空欄=非表示） | |
| IntroTime | イントロループ用のイントロ秒数（ファイル分割不要のループ機能） | 空欄=イントロなし |
| Volume | 音量 | 既定1.0 |

```csv
Label,Type,FileName,Title
メインテーマ,Bgm,main_theme.ogg,メインテーマ
ドアの音,Se,door.wav,
街の喧騒,Ambience,street.ogg,
```

## Layer（描画レイヤー）

uGUIのCanvas相当の描画グループ。各Typeの最初の行がデフォルトレイヤー。同一レイヤーに表示できるキャラ・背景は1つ。

レイヤーが1つも定義されていないTypeには、「Bg Default」「Character Default」「Sprite Default」という
デフォルトレイヤーが自動で追加されるため、Layerシートが空（ヘッダーのみ）でも、
またLayerシート自体がプロジェクトに1枚も無くても表示コマンドは動作する。
表示位置や描画順を制御したい場合に定義を書く。

```csv
LayerName,Type,X,Y,Order
背景,Bg,0,0,0
スプライト,Sprite,0,0,100
キャラ中央,Character,0,-300,200
```

| 列 | 意味 | 値・既定 |
|---|---|---|
| LayerName | レイヤー名 | 必須 |
| Type | `Bg` / `Character` / `Sprite` | 必須 |
| X / Y | レイヤー中心座標 | 既定0 |
| Order | 描画順（-32768〜32767。Z値= -Order/SortOrderToZUnit） | 必須 |
| LayerMask | Unityレイヤー名 | 既定=GraphicManagerと同じ |
| ScaleX / ScaleY | レイヤースケール | 既定1 |
| FlipX / FlipY | 反転 | 既定FALSE |
| Width / Height | レイヤーサイズ | 既定=画面サイズ |
| BorderLeft/Right/Top/Bottom | 余白 | |
| Align | 配置（TopLeft〜BottomRight） | 既定=中央 |

## Param（シナリオ変数）

| 列 | 意味 | 値 |
|---|---|---|
| Label | 変数名 | 必須 |
| Type | `Int` / `Float` / `Bool` / `String` | 必須 |
| Value | 初期値 | 必須 |
| FileType | `Default`（通常セーブ）/ `System`（システムセーブ・全体共通）/ `Const`（定数・セーブ対象外） | 既定 Default |

**初期値の適用タイミング**: 「最初から開始」（AdvEngine.StartGame）のたびに、`Default` 区分の変数はValueの初期値へ
自動リセットされる（`System` はシステムセーブデータから引き継ぎ、`Const` は常にシート値）。
周回プレイでも開始時にリセットされるため、シナリオ冒頭で明示的に初期化し直す必要はない。

```csv
Label,Type,Value,FileType
love,Int,0,Default
flag_met,Bool,FALSE,Default
player_name,String,あなた,Default
```

### 計算式（Paramコマンド・If条件式・Selection条件式などで使用）

- 四則演算: `+ - * / %`
- 比較: `== != >= <= > <`
- 論理: `&& || !`
- 代入: `= += -= *= /= %=`
- 括弧: `( )`
- 組み込み関数: `Random(min,max)`（整数乱数）/ `RandomF(min,max)`（小数乱数）/ `Ceil` `CeilToInt` `Floor` `FloorToInt`

```
point+=1
flag_a=true
(flag1 && flag2) || (point>3)
point=Random(1,6)
```

### C#からのアクセス

```csharp
engine.Param.GetParameterInt("名前");      // Int/Float/Bool/String 各型あり
engine.Param.SetParameterInt("名前", 100);
// 初期化前アクセスはエラー。engine.Param.IsInit で確認
```

## ParamTbl（パラメーターテーブル `名前{}`）

シート名に `{}` を付ける（例 `StatusTbl{}`）。通常のParamと縦横が逆の構成。

| 行 | 内容 |
|---|---|
| 1行目 | パラメーター名 |
| 2行目 | 型（Int/Float/Bool/String） |
| 3行目 | FileType |
| 4行目以降 | 各キー（行ごとに1レコード） |

アクセス記法: `テーブル名[キー].パラメーター名`（例 `StatusTbl[うたこ].hp`）。シナリオ・C#両方から使用可。

```csv
Name,hp,mp
Type,Int,Int
FileType,Default,Default
うたこ,100,50
太郎,80,30
```

**1〜3行目の先頭セルは空欄にしないこと**。`Name`/`Type`/`FileType`のように、その行の役割を示す予約語的なラベルを書く
（値そのものはコードで解析されないが、コメントのように行の意味を示す慣例）。
空欄にすると列位置がずれて誤動作する（`AdvParamStructTbl.AddTbl()`が1〜3行目をヘッダー、4行目以降を
データ行として読む際、`AdvParamStruct.ToIndexCommentOuted()`が「空欄でないセルの出現順」で列位置を数える
実装になっており、先頭セルが空欄だと数え始めの基準がずれて、パラメーター数が多いテーブルほど後ろの列で
インデックスが範囲外になりインポートエラーになる）。4行目以降の先頭列がキーになる。

## Localize（UIテキストの多言語化）

第1列=Key、第2列以降=言語名（UnityのSystemLanguage列挙型に準拠: Japanese, English, ...）。
キャラ名・ギャラリーのタイトル・カテゴリ名・UIテキスト等、シナリオ本文以外の文言を翻訳するのに使う。
シナリオ本文（Text列）の翻訳はシナリオシートに言語名列を追加する方式（[01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md)）。
ローカライズ機能全体（言語列の追加・空欄時の挙動設定・skip_page・ボイスの言語切替等）は[09_Localization_ja.md](09_Localization_ja.md)を参照。

## SceneGallery（シーン回想）

| 列 | 意味 |
|---|---|
| ScenarioLabel | 回想の開始シナリオラベル（必須） |
| Title | 回想UIに表示するタイトル（ローカライズ可） |
| Thumbnail | サムネイル相対パス（必須） |
| Categolly | カテゴリ分け（キャラ別等） |

回想の終了位置に `EndSceneGallery` コマンドを必ず置く。

```csv
ScenarioLabel,Title,Thumbnail
*回想1,うたこと出会った日,thumb01.png
*回想2,文化祭の思い出,thumb02.png
```

## Particle（パーティクル）

| 列 | 意味 |
|---|---|
| Label | 識別ラベル（Particleコマンドで使用） |
| FileName | `Resources/<プロジェクト名>/Particle/` 以下のプレハブ相対パス |
| X / Y / Z / Pivot / Scale / Conditional / SubFileName | Characterシートと同様（共通のグラフィック情報パーサーで解析されるため使用可） |

```csv
Label,FileName
花吹雪,sakura.prefab
花火,firework1.prefab
```

## Animation（キーフレームアニメーション。`名前[]` の派生シートも可）

| 行 | 内容 |
|---|---|
| 1行目 | アニメーションラベル、WrapMode（ループ設定）、`Linear`（補間をシャープに。省略=滑らか） |
| 2行目 | キーフレーム時刻（秒） |
| 3行目以降 | プロパティ名と各キーフレームの値 |

プロパティ: `X Y Z` / `Scale ScaleX ScaleY ScaleZ` / `Angle AngleX AngleY AngleZ` / `Alpha` / `R G B` / `Texture`（パラパラアニメ用）/ `Pattern`（キャラパターン切替）。
コンポーネント指定も可: `Utage.FishEye.strengthX` 等。座標系はローカル。
PlayAnimationコマンド、FadeIn/RuleFade等のキーフレーム指定（`Utage.ColorFade.strength` 等）で使用。

```csv
*揺れる,Loop
Time,0,0.5,1
Y,0,-10,0
```

**1行目は必ず`*`始まり**（`*ラベル名`。`AdvAnimationSetting.IsHeader()`が`row[0][0]=='*'`で判定）。
**2行目（キーフレーム時刻）は先頭セルが読み捨てられる**（`ParseTimeTbl()`はindex1以降のみを時刻として読む）ため、
先頭セルにはダミーの文字列（例`Time`）を入れる。3行目以降のプロパティ行も同様に先頭セルはプロパティ名で、
値は2列目以降が時刻の並びに対応する。

## EyeBlink（目パチ）/ LipSynch（口パク）

Dicing / Avatar タイプ専用。Characterシートの EyeBlink / LipSynch 列で紐づけ。

**EyeBlink**:

| 列 | 意味 | 既定 |
|---|---|---|
| Label | 識別ラベル（`*ラベル名`形式で記述。`AdvCommandParser.ParseScenarioLabel`で解析されるため先頭`*`が必須） | 必須 |
| IntervalMin / IntervalMax | 瞬きの間隔（秒・この範囲でランダム） | 2 / 6 |
| RandomDouble | 二連続瞬きする確率（0〜1） | 0.2 |
| Tag | 画像切り替えに使うタグ | eye |
| Name0/Duration0, Name1/Duration1, ... | 各コマのテクスチャ名と表示秒数のペア | — |

**LipSynch**:

| 列 | 意味 | 既定 |
|---|---|---|
| Label | 識別ラベル（`*ラベル名`形式で記述。`AdvCommandParser.ParseScenarioLabel`で解析されるため先頭`*`が必須） | 必須 |
| Type | `Text` / `Voice` / `TextAndVoice` | TextAndVoice |
| Interval | 切り替え間隔（秒） | 0.2 |
| ScaleVoiceVolume | 音量に応じた口の開き方の倍率 | 1 |
| Tag | 画像切り替えに使うタグ | lip |
| Name0/Duration0, Name1/Duration1, ... | 各コマのテクスチャ名と表示秒数のペア | — |

**Name0/Duration0以降のコマ数に上限は無い**（`MiniAnimationData.TryParse` が `Name0` 列から
右方向へ「名前・秒数」のペアを、両方空欄になるまで際限なく読み続ける実装。Name5/Duration5のように
列を追加すれば6コマ目以降も使える）。
テクスチャ名は `*_パターン名`（元画像名に付加。例 `*_e0`）または直接名指定。命名規則を統一すれば複数キャラで同一データを再利用できる。

```csv
Label,IntervalMin,IntervalMax,Name0,Duration0,Name1,Duration1
まばたき1,2,6,*_e0,0.1,*_e1,0.1
```

## Boot（起動設定・予約シート）

リソースのファイル管理・バージョン設定等のシステム用シート。通常はテンプレートのまま使用し、シナリオ側で編集することは少ない。

