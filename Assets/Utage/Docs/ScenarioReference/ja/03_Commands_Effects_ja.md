# コマンドリファレンス: 演出・エフェクト

WaitType列の仕様は [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) を参照。
**注意**: 演出のフェード秒数・時間指定は原則 **Arg6**、待機方法は **WaitType列**。

## 実践パターン: 演出をテキスト表示と同時に進行させる

メッセージウィンドウは、ページ切り替え後に次のテキスト系コマンドが実行されるまで表示されない仕様。
そのため、ウィンドウを対象にした演出（`Shake,MessageWindow` 等）をテキストの合間に単独で置くと、
**非表示のウィンドウに演出がかかり、画面上は何も起きていないように見える**。

演出コマンドの WaitType に `PageWait` を指定して完了待ちをやめ、直後のテキスト表示と並行実行させるのが定石。
`NoWait` でも並行実行になるが、ページ送り時に演出完了を待つ `PageWait` のほうが安全（演出中のセーブも問題にならない）。

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text
Shake,MessageWindow,,time=0.5 x=10 y=10,,,,PageWait
,うたこ,,,,,,,「テキスト表示と同時にウィンドウが揺れる」
```

同様に、演出の対象物（背景・キャラ等）が画面に表示されていない状態で演出コマンドを実行しても、見た目には何も起きない。
演出は「対象が表示されているタイミング」で実行されるように組むこと。

## Tween（汎用アニメーション）

列: Arg1=対象名, Arg2=TweenType, Arg3=パラメーター, Arg4=EaseType, Arg5=LoopType ＋ WaitType列

**Arg1（対象）**: `MessageWindow`（メッセージウィンドウ）／`Graphics`（グラフィック全体）／`Camera`（カメラ。空欄相当でメインカメラ、名前指定で特定カメラ）／それ以外はオブジェクト名またはレイヤー名として解決（キャラ名・スプライト名・`BG`・レイヤー名等。レイヤーの場合スケール値は1/100で指定）。
Shakeコマンドも同じ解析ロジックを共有するため、指定できる値はTweenと共通。

**Arg2（TweenType）**:

| 系統 | TweenType | 説明 |
|---|---|---|
| 移動 | MoveTo / MoveFrom / MoveBy | 指定座標へ／から／分だけ移動 |
| 移動 | PunchPosition / ShakePosition | 弾んで戻る／揺れて戻る |
| 回転 | RotateTo / RotateFrom / RotateBy | 指定角度へ／から／分だけ回転 |
| 回転 | PunchRotation / ShakeRotation | 弾む回転／ブレる回転 |
| 拡縮 | ScaleTo / ScaleFrom / ScaleBy | 指定スケールへ／から／分だけ |
| 拡縮 | PunchScale / ShakeScale | 弾む拡縮／ブレる拡縮 |
| 色 | ColorTo / ColorFrom | 指定色へ／から変化 |

**Arg3（パラメーター）**: `名前=値` をスペース区切りで複数指定。

| パラメーター | 意味 |
|---|---|
| time | 秒数（未記入=1秒、0=即時） |
| speed | timeの代わりに速度指定 |
| delay | 開始遅延秒数（既定0） |
| x, y, z | 変化量（TweenTypeにより意味が変わる） |
| islocal | true=ローカル座標／false=グローバル（既定） |
| alpha / r,g,b,a / color | 色・透明度（0.0〜1.0） |

**Arg4（EaseType）**: `linear` `spring` および `easeIn/Out/InOut` × `Quad/Cubic/Quart/Quint/Sine/Expo/Circ/Bounce/Back/Elastic`（例 `easeOutQuad`）。
**空欄時の既定値は `easeOutExpo`**（最初速く動き、後半急激に減速するカーブ。`linear`ではない点に注意）。
例外: `ColorTo`/`ColorFrom` だけ空欄時に `linear` になる（色変化にイージングをかけるとほぼ体感できないための特例。iTween側コード実装で確認）。
**さらに注意**: `Punch*`系（PunchPosition/PunchRotation/PunchScale）と`Shake*`系（ShakePosition/ShakeRotation/ShakeScale）は、
Move/Scale/Rotate/Colorが使う汎用のイージング関数`ease()`を呼ばず、専用の固定カーブ関数（`punch()`、または
Shakeは`percentage`をそのまま使う線形の減衰）で動くため、**Arg4に何を指定しても効果が無い**（詳細はShakeコマンドの項を参照）。

**Arg5（LoopType）**: `loop=回数`（0=無限）／`pingPong=回数`（往復）。**空欄時は既定でループなし（1回再生のみ）**。

```csv
Tween,うたこ,MoveTo,time=2 x=400 y=300,easeOutQuad
Tween,BG,ColorTo,time=1.5 alpha=0.5
Tween,sprite1,ScaleTo,time=1 x=1.5 y=1.5,,loop=2
```

## Shake（揺らし）

Tweenの簡易版（`AdvCommandTween` を継承し、TweenTypeを `ShakePosition` に固定したもの）。

| Arg1 | Arg2 | Arg3 | Arg4 | Arg5 |
|---|---|---|---|---|
| 対象（Tweenと共通の仕様。上記参照） | 未使用（TweenTypeは`ShakePosition`固定） | パラメーター（`名前=値`をスペース区切り。**既定 `x=30 y=30`**。time/delay等の意味はTweenのArg3表を参照） | **指定しても効果なし**（下記注意） | LoopType（Tweenと共通と思われる。loop/pingPongでの繰り返しに対応） |

**Arg4（EaseType）に関する注意**: 値としては受け付けてエラーにならないが、揺れの挙動には反映されない。
iTween本体の実装では、進行度`percentage`は常に線形（`runningTime/time`）で計算され、
イージングは各Tweenタイプの`Apply*Targets()`関数が個別に`ease(start,end,percentage)`を
呼び出して初めて反映される。ところがShakeの`ApplyShakePositionTargets()`はこの`ease()`呼び出しを
行わず、線形の`percentage`をそのまま「揺れ幅の減衰（`1-percentage`）」にしか使っていない
（`UnityEngine.Random.Range`で毎フレームランダムにジャンプさせる方式）。
そのためEaseTypeを何に設定しても、揺れの収まり方は常に線形になる。

```csv
Shake,MessageWindow,,time=0.5 x=10 y=10
Shake,Camera,,time=0.3 x=5
```

## FadeOut / FadeIn（画面全体フェード）

カメラに対するカラーフェード。**FadeInはFadeOut後にのみ有効**（冒頭で暗転から始めたい場合は先に0秒のFadeOutを入れる）。
既定の対象は SpriteCamera（背景・キャラ・スプライト）のため、背景等が何も表示されていない場面ではフェードしても見た目の変化がない点に注意。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 | Arg6 |
|---|---|---|---|---|---|
| FadeOut | フェードカラー（カラー名または`#RRGGBB`/`#RRGGBBAA`。既定white） | カメラ名（既定SpriteCamera。`UICamera`でUI層にも適用） | ルール画像ファイル名（省略可） | ルール境界値 0.01〜1.0（既定0.2） | フェード秒数（数値またはAnimationシートのキーフレーム名。既定0.2秒） |
| FadeIn | 同上 | 同上 | 同上 | 同上 | 同上 |

※Arg5は両コマンドとも未使用。

```csv
FadeOut,black,,,,,1
FadeIn,black,,,,,1
```

## RuleFadeIn / RuleFadeOut（オブジェクト単位のルール画像フェード）

| コマンド | Arg1 | Arg2 | Arg3 | Arg6 | WaitType列 |
|---|---|---|---|---|---|
| RuleFadeIn | 対象オブジェクト名（必須） | ルール画像名（必須） | 中間領域の大きさ 0.01〜1.0（既定0.2） | フェード秒数またはキーフレームアニメ名（既定0.2秒） | 待機方法 |
| RuleFadeOut | 同上 | 同上 | 同上 | 同上 | 同上 |

```csv
RuleFadeIn,BG,ルール画像1,0.3,,,1
RuleFadeOut,BG,ルール画像1,0.3,,,1
```

## CaptureImage（画面キャプチャ）

現在の画面をキャプチャしてオブジェクト化（ルールフェードと組み合わせた場面転換等に使用）。

| コマンド | Arg1 | Arg2 | Arg3 |
|---|---|---|---|
| CaptureImage | 作成オブジェクト名（必須） | キャプチャ対象カメラ名（必須） | 表示レイヤー名（必須） |

```csv
CaptureImage,capture1,SpriteCamera,サブレイヤー
RuleFadeIn,capture1,ルール画像1,0.3,,,1
```

## PlayAnimation（キーフレームアニメーション再生）

事前に Animationシート で定義が必要。

| 引数 | 意味 |
|---|---|
| Arg1 | 対象（キャラ名・レイヤー名） |
| Arg2 | アニメーション名（Animationシート定義名） |
| Arg3 | セーブデータに含めるか TRUE/FALSE（既定TRUE） |
| WaitType列 | 待機方法 |

```csv
PlayAnimation,うたこ,揺れ
```

## ImageEffect / ImageEffectOff（イメージエフェクト・ビルトインRP用）

| コマンド | Arg1 | Arg2 | Arg3 | Arg6 | WaitType列 |
|---|---|---|---|---|---|
| ImageEffect | カメラ名（SpriteCamera=背景キャラのみ／UICamera=UI含む全体） | エフェクト名: GrayScale / Sepia / NegaPosi / Blur / MotionBlur / Bloom / Mosaic / FishEye / Twirl / Vortex | キーフレームアニメーション名（任意） | フェード秒数（空欄=0） | 待機方法 |
| ImageEffectOff | 同上 | エフェクト名または `All` | キーフレームアニメーション名（任意。ImageEffectと同じ引数構成） | フェード秒数（空欄=0） | 待機方法 |

※URPプロジェクトではURP対応パッケージが必要（PostEffect推奨）。

```csv
ImageEffect,SpriteCamera,Sepia,,,,1
ImageEffectOff,SpriteCamera,Sepia,,,,1
```

## PostEffect / PostEffectOff（ポストエフェクト・URP用）

URP版のみ。カメラのVolume（AdvPostEffectVolume）単位でエフェクトを制御。

| コマンド | Arg1 | Arg2 | Arg3 | Arg6 | WaitType列 |
|---|---|---|---|---|---|
| PostEffect | カメラ名（必須） | ボリューム名（必須。シーン内Volumes以下のオブジェクト名） | エフェクト名（カンマ区切り複数可。空欄=ボリューム内全エフェクト） | フェード秒数（空欄=0） | 待機方法 |
| PostEffectOff | 同上 | ボリューム名（空欄=全ボリューム。CaptureVolume/FadeVolume除く） | — | フェード秒数（空欄=0） | 待機方法 |

注意: 同種エフェクトを複数ボリュームで同時使用すると優先順位が不定。

```csv
PostEffect,SpriteCamera,MainVolume,Bloom,,,1
PostEffectOff,SpriteCamera,MainVolume,,,,1
```

## ZoomCamera（カメラズーム）

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 | Arg6 | WaitType列 |
|---|---|---|---|---|---|---|
| ZoomCamera | カメラ名 | ズーム倍率（空欄=1） | ズーム中心X（Arg4と両方空欄=現在の中心維持） | ズーム中心Y | アニメーション秒数（既定0.2） | 待機方法 |

演出後は倍率を必ず1に戻す（1に戻すと中心点は自動で0,0にリセット）。

```csv
ZoomCamera,SpriteCamera,1.5,0,0,,1
ZoomCamera,SpriteCamera,1,,,,1
```

## SetPivot / ResetPivot（ピボット操作）

回転・拡大の中心点を変更する。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| SetPivot | オブジェクト名 | ピボットX: 0〜1.0 または Left/Center/Right | ピボットY: 0〜1.0 または Bottom/Center/Top | オフセット X / Y（既定0） | タイプ: SpritePos（既定）/ SpritePosLocal / SpritePosNoSize / WorldSpace / Direct |
| ResetPivot | オブジェクト名 | — | — | — | — |

注意: ピボット変更は見た目位置を変えないが座標は変わるため、後続アニメーションに影響し得る。

```csv
SetPivot,うたこ,Center,Bottom,,,SpritePos
Tween,うたこ,RotateBy,time=1 z=360
ResetPivot,うたこ
```

## Vibrate（バイブレーション）

| コマンド | 機能 |
|---|---|
| Vibrate | Android/iOSで端末を振動させる（引数なし・時間指定不可） |

```csv
Vibrate
```

使わない場合は Scripting Define Symbols に `UTAGE_IGNORE_VIBRATE` を追加するとAndroidのVIBRATE Permission付与を回避できる。

## Video（動画再生）

全画面ムービー再生（指定カメラの背景として再生する。[07_SettingSheets_ja.md](07_SettingSheets_ja.md) の
Characterシート「FileType=Video」＝表示オブジェクトとしての動画再生とは別物。混同注意）。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| Video | 動画ファイル名（`Resources/<プロジェクト名>/Video/` 配下。DL運用時は拡張子込み） | カメラ名（必須） | ループ TRUE/FALSE（既定FALSE） | クリックスキップ可否 TRUE/FALSE（既定TRUE） |

再生完了はコマンド自身が待つため、**WaitVideoコマンドは不要**（WaitVideoは07章のFileType=Video用）。
出典: `AdvCommandVideo.Wait()`。

```csv
Video,opening,SpriteCamera,FALSE,TRUE
```

※旧 `Movie` コマンドは `AdvCommandParser` に定数（`IdMovie`）だけ残っているが、コマンド生成の
switch文に対応するcaseが無く機能しない（レガシーの未実装コマンド。使用不可）。

## Thread / WaitThread / EndThread（演出用スレッド）

テキスト表示と非同期に演出を動かす。スレッド内で使えるのはTween・エフェクト系のみ（テキスト・ページ操作系は不可）。

| コマンド | Arg1 | Arg2 |
|---|---|---|
| Thread | スレッドのシナリオラベル | — |
| WaitThread | シナリオラベル | キャンセル可否 TRUE/FALSE（既定FALSE） |
| EndThread | — | — |

`EndThread` はスレッドの末尾に置く。

```csv
Thread,*揺れ演出
WaitThread,*揺れ演出
,うたこ,,,,,,,「スレッドの完了を待ってから続く」

*揺れ演出
Shake,うたこ,,time=2 x=10 y=10
EndThread
```

ページをまたぐ演出をセーブ対応するには AdvSaveManager の「Restart Sub Thread」をオン（ロード時はスレッド先頭から再開）。

## SkipEffect（演出の強制スキップ）

再生中の演出を強制終了する。

| コマンド | Arg1 | Arg2 |
|---|---|---|
| SkipEffect | スキップ対象タイプ: `All`（既定・待機中の演出すべて）／`NoWait`（NoWait指定の演出のみ） | ループ演出もスキップするか TRUE/FALSE（既定FALSE） |

```csv
SkipEffect
SkipEffect,All,TRUE
```

