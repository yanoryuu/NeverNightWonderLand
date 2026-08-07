# コマンドリファレンス: 表示系

テキスト・キャラクター・背景・スプライト・パーティクル・レイヤー操作。
共通仕様（PageCtrl・WaitType・タグ）は [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) を参照。
**注意**: フェード秒数は本章の大半のコマンドで **Arg6**。
**例外**: Particle・ParticleOff・LayerReset にはフェード秒数の引数が無い（Particleは即時表示、ParticleOffは消し方をArg2で指定、LayerResetは即座にリセット）。

## テキスト表示（地の文）

Command・Arg1を空欄にし、Text列にテキストを書く。

| 列 | 意味 |
|---|---|
| Text | 表示テキスト |
| PageCtrl | ページ送り制御（空欄=改ページ待ち） |
| Voice | ボイスファイル名（Soundシート登録名。空欄=なし） |

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text,PageCtrl
,,,,,,,,今日も良い天気ですね。,
,,,,,,,,雨が降ってきた…,Br
```

## セリフ・キャラクター表示

Command空欄で Arg1 にキャラ名を指定。

| 引数 | 意味 | 値 |
|---|---|---|
| Arg1 | キャラの名前 | Characterシート登録ラベル。未登録名なら名前欄への表示のみ（立ち絵なし） |
| Arg2 | 表示パターン | Characterシートの Pattern（表情等）。空欄=前の表情を継続。`<Off>` で立ち絵を消してセリフのみ |
| Arg3 | レイヤー名 | 空欄=デフォルトレイヤー。同一レイヤーに表示できるキャラは1体 |
| Arg4 / Arg5 | X / Y 座標 | 数値（レイヤー位置に加算）。空欄=変更なし |
| Arg6 | フェード秒数 | 空欄=0.2秒 |
| Text | セリフ | 空欄=キャラ表示のみ |
| Voice | ボイス | Soundシート登録名 |

```csv
Command,Arg1,Arg2,Arg3,Arg4,Arg5,Arg6,WaitType,Text
,うたこ,笑い,,,,,,「こんにちは！」
,うたこ,<Off>,,,,,,「（立ち絵なしでセリフだけ）」
,太郎,通常,layer1,100,,,,「複数キャラはレイヤーを分ける」
```

## CharacterOff（キャラクター非表示）

| 引数 | 意味 | 値 |
|---|---|---|
| Arg1 | 対象 | キャラ名。レイヤー名指定でそのレイヤー以下すべて。空欄=キャラクタータイプ全レイヤー |
| Arg6 | フェード秒数 | 空欄=0.2秒 |

## Bg（背景表示）/ BgOff

Bg はイベントCG表示モードを解除する効果も持つ。背景オブジェクト名は自動で「BG」（Tween等のターゲット指定に使用）。

| コマンド | Arg1 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|
| Bg | テクスチャラベル（Textureシート登録名・必須） | レイヤー名（空欄=デフォルトBGレイヤー） | X / Y 座標（空欄=変更なし） | フェード秒数（空欄=0.2秒） |
| BgOff | — | — | — | フェード秒数（空欄=0.2秒） |

※Bgの Arg2 は未使用。

```csv
Bg,学校前
Bg,学校前,,BG,0,0,1.0
BgOff,,,,,,0.5
```

## BgEvent（イベントCG表示）/ BgEventOff

イベントCGを表示し、キャラクター表示を自動でOFFにする。解除には Bg コマンドが必要。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| BgEvent | テクスチャラベル（Textureシートの Event タイプ登録名・必須） | モード切替（FALSEで立ち絵を継続表示。既定TRUE=立ち絵OFF） | レイヤー名（空欄=デフォルトBGレイヤー） | X / Y 座標（空欄=変更なし） | フェード秒数（空欄=0.2秒） |
| BgEventOff | — | — | — | — | フェード秒数（空欄=0.2秒） |

```csv
BgEvent,回想シーン1
BgEventOff,,,,,,0.5
```

## Sprite（スプライト表示）/ SpriteOff

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 | Arg6 |
|---|---|---|---|---|---|
| Sprite | スプライト名（一意の名前・必須。「Bg」「MessageWindow」「Graphics」は予約語で使用不可） | テクスチャラベル（Textureシート登録名。空欄=Arg1と同名） | レイヤー名（空欄=デフォルト） | X / Y 座標（既定0） | フェード秒数（空欄=0.2秒） |
| SpriteOff | 対象: スプライト名／レイヤー名／`AllSpriteObjects`／空欄（=スプライトレイヤー全体） | — | — | — | フェード秒数（空欄=0.2秒） |

同じ画像を複数表示するときは Arg1 に別名を付け、Arg2 で同じラベルを指定する。後から表示したものが前面。

```csv
Sprite,ball1,ball,,100,50
Sprite,ball2,ball,,200,50
SpriteOff,ball1,,,,,0.3
```

## Particle（パーティクル表示）/ ParticleOff

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 / Arg5 |
|---|---|---|---|---|
| Particle | パーティクル名（一意の名前・必須） | パーティクルラベル（Particleシート登録名。空欄=Arg1と同名） | レイヤー名（空欄=デフォルト） | X / Y 座標（既定0） |
| ParticleOff | 対象: パーティクル名／レイヤー名（空欄=全消し） | 消し方: 空欄=プレハブ設定に従う／`Clear`=即座に削除／`StopEmitting`=発生停止して自然消滅 | — | — |

※ParticleOffにフェード秒数（Arg6）の引数は無い（本章冒頭の例外注記を参照）。

```csv
Particle,fireworks,firework1,,300,100
ParticleOff,fireworks,StopEmitting
```

## LayerOff / LayerReset / ChangeLayer（レイヤー操作）

| コマンド | Arg1 | Arg2 | Arg3 | Arg6 |
|---|---|---|---|---|
| LayerOff | レイヤー名（必須） | — | — | フェード秒数（空欄=0.2秒） |
| LayerReset | レイヤー名または `All`（必須） | — | — | — |
| ChangeLayer | 対象オブジェクト（キャラ名・Bg名など） | 座標保持方式: `KeepGlobal`（見た目位置維持・既定）／`KeepLocal`（ローカル座標維持）／`ResetLocal`（初期座標へ） | 移動先レイヤー名 | フェード秒数（空欄=0.2秒） |

- **LayerOff**: レイヤー上の全オブジェクトを非表示にする。
- **LayerReset**: Tween・Shake等で変化したレイヤーを初期状態へ戻す（フェードなし・即座に戻る）。
- **ChangeLayer**: 表示中オブジェクトを別レイヤーへ移動。移動先レイヤーに既にキャラ・背景がある場合は自動フェードアウト。移動アニメーション自体は Tween で行う。

```csv
LayerOff,キャラ中央,,,,,0.5
LayerReset,All
ChangeLayer,うたこ,KeepGlobal,サブレイヤー,,,0.3
```

