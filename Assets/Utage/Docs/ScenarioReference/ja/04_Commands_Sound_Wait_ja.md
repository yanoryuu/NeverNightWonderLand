# コマンドリファレンス: サウンド・待機

ラベルは Soundシート（[07_SettingSheets_ja.md](07_SettingSheets_ja.md)）で定義したものを使う。
**注意**: フェード秒数・待機秒数は原則 **Arg6**、待機方法は **WaitType列**。

## サウンド系

### Se（効果音）/ StopSe

| コマンド | Arg1 | Arg2 | Arg3 | Arg6 |
|---|---|---|---|---|
| Se | SEラベル（必須） | ループ TRUE/FALSE（既定FALSE） | 音量 0〜1（既定1） | — |
| StopSe | SEラベル（空欄=全SE停止） | — | — | フェード秒数（既定0.2） |

同一SEを複数再生している場合、StopSeは同ラベルすべてを止める。

```csv
Se,ドアの音
StopSe,ドアの音,,,,,0.5
```

### Bgm / StopBgm

| コマンド | Arg1 | Arg2 | Arg3 | Arg5 | Arg6 |
|---|---|---|---|---|---|
| Bgm | BGMラベル（必須） | ループ TRUE/FALSE（既定TRUE） | 音量 0〜1（既定1） | 前曲のフェードアウト秒数（既定0.2） | フェードイン秒数（既定0） |
| StopBgm | — | — | — | — | フェード秒数（既定0.2） |

※StopBgmにArg1（ラベル指定）は無い。BGMは常に1トラックのみ再生されるため、停止対象を選ぶ必要がない。

```csv
Bgm,メインテーマ
StopBgm,,,,,,0.5
```

### Ambience（環境音）/ StopAmbience

| コマンド | Arg1 | Arg2 | Arg3 | Arg5 | Arg6 |
|---|---|---|---|---|---|
| Ambience | 環境音ラベル（必須） | ループ TRUE/FALSE（**既定FALSE**。Bgmと既定値が違う点に注意） | 音量 0〜1（既定1） | 前曲のフェードアウト秒数（既定0.2） | フェードイン秒数（既定0） |
| StopAmbience | — | — | — | — | フェード秒数（既定0.2） |

※StopAmbienceにArg1は無い（StopBgmと同様、常に1トラックのみ）。

```csv
Ambience,街の喧騒,TRUE
StopAmbience,,,,,,0.5
```

### Voice / StopVoice

通常のセリフ再生（Voice列）とは別に、任意タイミングで声を再生する。

| コマンド | Arg1 | Arg2 | Arg3 | Voice列 | Arg6 |
|---|---|---|---|---|---|
| Voice | キャラクターラベル（必須） | ループ TRUE/FALSE（既定FALSE） | 音量 0〜1（既定1） | ボイスファイル名（必須） | — |
| StopVoice | — | — | — | — | フェード秒数（既定0.2） |

※StopVoiceにArg1は無い（常に1トラックのみ）。

```csv
Voice,うたこ,,,,,,,,,voice001.wav
StopVoice,,,,,,0.5
```

> **注意**: オートページ送り（オートモード）はボイスの再生終了を待ってから改ページする。
> ループ再生（Arg2=TRUE）のボイスを流したままにすると、StopVoiceするまでオートのページ送りが
> 止まり続けるため、ループボイスは使いどころに注意（通常のセリフボイスはループさせない）。

### StopSound（一括停止）/ ChangeSoundVolume（グループ音量変更）

| コマンド | Arg1 | Arg2 | Arg6 |
|---|---|---|---|
| StopSound | 種類（`Bgm` `Se` `Ambience` `Voice` `All`、カンマ区切り複数可。空欄=既定 `Bgm,Ambience`） | — | フェード秒数（**既定0.15**） |
| ChangeSoundVolume | 種類（同上・必須。空欄不可） | 音量 0〜1（必須） | フェード秒数（既定0） |

**注意（ChangeSoundVolume）**: 曲を止めても設定が持続するため、明示的に音量を戻すこと。
音量は「コンフィグ設定 × 再生時Arg3 × ChangeSoundVolume」の乗算で決まる。

```csv
Bgm,メインテーマ
Se,ドアの音
ChangeSoundVolume,Bgm,0.3,,,,0.5
StopSound,All,,,,,1
```

## 待機系

### Wait / WaitInput

| コマンド | Arg6 |
|---|---|
| Wait | 待機秒数（必須） |
| WaitInput | 入力待ちのタイムアウト秒数（省略時は入力があるまで待機） |

```csv
Wait,,,,,,1.5
WaitInput,,,,,,3
```

### WaitCustom

| コマンド | 引数 |
|---|---|
| WaitCustom | なし。プログラムからの解除待ち。コード側で `AdvEngine.UiManager.IsInputTrigCustom = true;` を呼ぶと解除。カスタムUIの操作完了待ちに使用 |

```csv
WaitCustom
```

### WaitConditional

**条件式が成立している間、待機する**（式が不成立になると進む。「成立するまで待つ」ではない点に注意）。
恒真の式（常に成立する条件）を書くと永久に進まなくなる。
出典: AdvCommandWaitConditional.Wait()（待機継続条件が `最低待機時間内 || 条件式がtrue`）。

| コマンド | Arg1 | Arg6 |
|---|---|---|
| WaitConditional | 条件式（例 `flag1==true` なら flag1 がtrueの間待ち続け、falseになったら進む） | 最低待機秒数（省略可） |

```csv
WaitConditional,is_loading==true
```

### WaitFadeObjects

オブジェクトのフェード終了待ち（キャラ表示のフェードはWaitType指定不可のためこれを使う）。

| コマンド | Arg1 | WaitType列 |
|---|---|---|
| WaitFadeObjects | 対象（カンマ区切り複数可）: オブジェクト名／レイヤー名／`AllBgLayers` `AllCharacterLayers` `AllSpriteLayers`／`AllBgObjects` `AllCharacterObjects` `AllSpriteObjects`／`All`（既定=All） | 待機方法（Skippable系可） |

```csv
CharacterOff,うたこ,,,,,1
WaitFadeObjects,うたこ
```

### WaitEffectTime

ウェイトタイプ対応の時間待機。

| コマンド | Arg6 | WaitType列 |
|---|---|---|
| WaitEffectTime | 待機秒数（必須） | 待機方法 |

```csv
WaitEffectTime,,,,,,2,Skippable
```

### WaitSound

サウンド再生終了待ち。

| コマンド | Arg1 | Arg2 | WaitType列 |
|---|---|---|---|
| WaitSound | 種類（`Bgm`/`Ambience`/`Voice`/`Se`） | 対象名（Se=SEラベル・空欄で全SE、Voice=キャララベル・空欄で全キャラ、Bgm/Ambienceは不要） | 待機方法（Skippableでも音声自体は停止しない） |

```csv
Se,足音
WaitSound,Se,足音
```

### WaitVideo

ビデオオブジェクトの再生終了待ち。対象のループ設定がfalseであること。

| コマンド | Arg1 | WaitType列 |
|---|---|---|
| WaitVideo | ビデオオブジェクト名 | 待機方法 |

```csv
WaitVideo,opening_movie
```

