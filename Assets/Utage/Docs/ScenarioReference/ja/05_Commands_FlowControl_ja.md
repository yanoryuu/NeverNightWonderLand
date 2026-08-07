# コマンドリファレンス: フロー制御・ロジック

シナリオラベルの記法は [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) を参照。
パラメーター（変数）の定義と計算式の記法は [07_SettingSheets_ja.md](07_SettingSheets_ja.md) の Paramシートを参照。

## Param（パラメーター操作）

Paramシートで定義した変数を変更する。
**1つのParamコマンドで実行できる式は1つだけ**。複数の変数を変更するときは行を分ける（`;` 等での連結は不可）。
計算式の演算子仕様はParamシート側を参照。

| コマンド | Arg1 |
|---|---|
| Param | 計算式（例 `flag1=true` `point+=10`） |

```csv
Param,point+=10
Param,flag1=true
```

## Jump（シナリオジャンプ・自動分岐）

| コマンド | Arg1 | Arg2 |
|---|---|---|
| Jump | ジャンプ先シナリオラベル（`*ラベル名`） | 条件式（bool）。空欄=無条件。falseなら実行されず次の行へ |

連続記述で「最初に条件が成立したJumpだけ実行」の多分岐になる。

```csv
Jump,*GoodEnd,point>=10
Jump,*NormalEnd,point>=5
Jump,*BadEnd
```

## JumpRandom（ランダム分岐）

**JumpRandomは連続して配置することが前提**（1つだけだと必ずそこへジャンプする）。連続配置全体が1つの抽選グループになり、そのうち1つがランダムに選ばれる。

| コマンド | Arg1 | Arg2 | Arg3 |
|---|---|---|---|
| JumpRandom | ジャンプ先ラベル | 条件式（falseなら抽選対象外） | 確率の重み（空欄=1。相対値。パラメーター式も可、例 `lv/2`） |

```csv
JumpRandom,*分岐先1,,5
JumpRandom,*分岐先2,,3
JumpRandom,*分岐先3,,1
```

## Selection（選択肢）

**Selectionは連続して配置することが前提**。連続する Selection が同時に選択肢として表示される。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 | Arg5 | Arg6 | Text |
|---|---|---|---|---|---|---|---|
| Selection | 選択時のジャンプ先ラベル（必須） | 表示条件式（falseで非表示。空欄=無条件表示） | 選択時に実行する計算式（フラグ設定等）。選択された直後・ジャンプ処理の前に実行される | 選択肢UIのプレハブ名（既定 SelectionItem） | X座標（フリーレイアウト時。Arg6と両方同時指定が必須） | Y座標（フリーレイアウト時。Arg5と両方同時指定が必須） | 選択肢の表示テキスト（必須） |

```csv
Selection,*ルートA,,,,,,,選択肢A
Selection,*ルートB,flag_secret,,,,,,隠し選択肢
Selection,*ルートC,,point+=1,,,,,好感度が上がる選択肢
```

**表示条件（Arg2）が全て false で選択肢が1つも表示されない場合**、入力待ちにならず自動的に
グループ直後の行へ処理が進む（`AdvSelectionManager.TryStartWaitInputIfShowing()`が選択肢0件のとき
`false`を返すため）。1つ以上表示される場合は必ずいずれかを選択してジャンプする（未選択のまま
グループを素通りすることはない）。

## SelectionClick（表示オブジェクトクリックで分岐）

キャラ・スプライト等のクリックで分岐。Arg1〜Arg3はSelectionと同じ。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| SelectionClick | ジャンプ先ラベル | 有効化条件式 | 選択時計算式 | クリック対象オブジェクト名（キャラ名・スプライト名） |

※Arg5は未使用（コード内に残るが仕様上は機能しない廃止項目。指定しても意味を持たない）。

**SelectionClickも連続して配置することが前提**（Selectionと同様、連続配置全体が1つのグループとして扱われる）。
通常オブジェクト・ダイシング・アバターは標準対応。プレハブはUGUIベースなら自動対応（AdvClickEvent生成）、独自当たり判定は `IAdvClickEvent` 実装が必要。

```csv
,うたこ,笑い,,,,,,「クリックして話しかけてみて」
SelectionClick,*Route1,,,うたこ
SelectionClick,*Route2,,,BG
```

## If / ElseIf / Else / EndIf（条件分岐）

| コマンド | Arg1 |
|---|---|
| If | 条件式（bool） |
| ElseIf | 条件式 |
| Else | なし |
| EndIf | なし |

**重要な制限**: If〜EndIf の中にシナリオテキスト（ページ処理）を混ぜない。パラメーター操作や表示系コマンドの条件実行に使い、シナリオ自体の分岐は Jump / Selection / サブルーチンで行う。

```csv
If,point>=10
Bg,豪華な部屋
Else
Bg,普通の部屋
EndIf
```

## サブルーチン

| コマンド | Arg1 | Arg2 | Arg3 | Arg4 |
|---|---|---|---|---|
| JumpSubroutine | サブルーチンラベル | 条件式 | 終了後の復帰先ラベル（空欄=呼び出し位置に戻る） | — |
| JumpSubroutineRandom | サブルーチンラベル | 条件式 | 終了後の復帰先ラベル | 確率の重み |
| EndSubroutine | — | — | — | — |
| ExitSubroutine | — | — | — | — |

EndSubroutineはサブルーチン終了・呼び出し元へ復帰、ExitSubroutineはすべてのサブルーチンを解除して元のシナリオを継続する（いずれも引数なし）。

- **JumpSubroutineRandomも連続して配置することが前提**（JumpRandomと同様、連続配置全体が1つの抽選グループになる）。
- ネスト呼び出し可能。
- サブルーチン内でセーブする場合、呼び出し元シナリオが更新されると復帰位置がずれる可能性がある（復帰先ラベル明示で回避）。
- **サブルーチン内にテキスト表示（セリフ・地の文）を置いてよい**。サブルーチンはラベルへのジャンプ＋復帰位置の記録にすぎず
  （`AdvCommandJumpSubroutine`）、If〜EndIfのような「中にテキストを混ぜてはいけない」制限は無い。

マクロの記法は [01_ScenarioBasics_ja.md](01_ScenarioBasics_ja.md) を参照。

## EndPage / EndScenario / PauseScenario / EndSceneGallery

| コマンド | 機能 |
|---|---|
| EndPage | 改ページ位置を明示（引数なし） |
| EndScenario | シナリオ終了・タイトル画面等へ戻る（引数なし） |
| PauseScenario | シナリオを中断。プログラム側から `AdvEngine.ResumeScenario()` で再開（引数なし） |
| EndSceneGallery | シーン回想の終了位置。回想再生時はここで回想が終わる（引数なし） |

**EndScenarioはサウンドを自動停止する**（既定設定の場合）: BGM・環境音・ループ再生中の音は必ず停止し、
ボイスも既定で停止する。SE（効果音）は既定では停止しない（`AdvEngine`の`IsStopSoundOnEnd`
= 既定true、`isStopVoiceOnSoundStop`= 既定true、`isStopSeOnSoundStop`= 既定false。いずれもInspectorで変更可能）。

