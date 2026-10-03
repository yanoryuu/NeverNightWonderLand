# テストプレイ基盤 (速度調整・仮想入力・入力の記録/再生・オーバーレイ)

手動テストプレイの補助と、自動テストプレイ (ボット) の土台。**エディタと Development Build だけ**に入る
(`#if UNITY_EDITOR || DEVELOPMENT_BUILD`)。リリースビルドでは下の「ゲーム側の変更」以外は存在せず、挙動も従来どおり。

## ファイル

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Player/PlayerInputSource.cs` | 入力のハブ。`PlayerInputFrame` (1 フレーム分の入力) / `IPlayerInputSource` / `PlayerInputSource` (既定 = キーボード・パッドの直読み。割り当ては旧 PlayerController.ReadInput と同一)。`Override` で差し替え (開発ビルドのみ) |
| `Assets/Scripts/TestPlay/TestPlaySpeed.cs` | 速度 0.1〜8 倍・一時停止・コマ送り |
| `Assets/Scripts/TestPlay/VirtualPlayerInput.cs` | プログラムから押す仮想入力 (`PlayerButton`) |
| `Assets/Scripts/TestPlay/TestPlayRecorder.cs` | 入力の記録 (JSON) と再生・ずれの報告 |
| `Assets/Scripts/TestPlay/TestPlayActions.cs` | ワープ・入口一覧・能力・チート・フラグのプリセット・プレイヤー状態 (オーバーレイ・デバッグパネル・ボット共用) |
| `Assets/Scripts/TestPlay/TestPlayRunner.cs` | 常駐オブジェクト `[TestPlay]` (起動時に自動生成, DontDestroyOnLoad, 実行順 -10000)。ホットキー |
| `Assets/Scripts/TestPlay/TestPlayOverlay.cs` | 実行時オーバーレイ (IMGUI) |
| `Assets/Scripts/Editor/DebugPanelWindow.cs` | `NeverNight/Debug Panel` に「速度」「入力の記録・再生」を追加。ワープは全ステージ + 入口選択に。チート等は TestPlayActions を呼ぶ形に整理 |
| `Docs/TestPlay/playtest_testplay.cs` | 動作確認用の run_script (Assets 外なのでコンパイル対象外) |
| `TestPlay/Recordings/*.json` | 記録ファイル (プロジェクト直下。Development Build では `persistentDataPath/TestPlay/Recordings`)。**git の追跡対象外** (.gitignore) |

### ゲーム側の変更 (リリースにも入る、挙動は不変)
- `PlayerController.ReadInput` は `PlayerInputSource.ReadForPlayer()` を読むだけになった (キー割り当ては PlayerInputSource.ReadDevice へ移動、内容は同じ)。
  ※ 旧コードはキーボードが無い環境で `_jumpHeld/_dashHeld` が前フレームの値を引きずっていた。新コードは毎フレーム作り直す。
- `PauseMenuView` のポーズ入力 (Esc / Start) は `PlayerInputSource.Current.PausePressed` を読む。
- アイテム画面 (`ItemMenuView`) の開閉 (Tab / Select(View)) は `PlayerInputSource.Current.ItemMenuPressed` を読む (2026-10 追加。記録・再生の対象外 = ポーズと同じ)。
  パッドの Select はアイテム画面専用 (オーバーレイの切替は R3 長押しに変更。2026-10 UIAssignment.md「3回目の決定」8)。
- `MenuInput.Read` は開発ビルドでのみ仮想入力 (`IMenuInputSource`) を重ねる。
- `GamePause` に `BaseTimeScale` / `SetBaseTimeScale` を追加。Pop / Reset は 1 ではなくこの値に戻す (既定 1)。
- `StageLoader.IsLoading` を公開。

## 使い方 (手動テスト)

### キー
| キー | 内容 |
|---|---|
| F1 (パッド: R3 を 0.6 秒長押し) | オーバーレイの表示/非表示 (長押しは実時間で測るので一時停止・メニュー中も効く。押し続けても 1 回だけ切り替わる。秒数は `TestPlayRunner.OverlayHoldSeconds`) |
| F2 / F3 | 速度を 1 段階下げる / 上げる (0.1, 0.25, 0.5, 1, 2, 4, 8) |
| F4 | 等倍に戻す |
| F5 | 一時停止 / 再開 (ポーズメニューと同じ止まり方) |
| F6 | コマ送り (一時停止中。1 コマ = 物理 1 ステップ = 0.02 秒) |
| F7 | 記録の開始 (ステージを読み直してから) / 停止して保存 |
| F8 | 直前の記録を再生 / 再生中なら停止 |

ゲームの操作 (WASD・矢印・Space・Shift・J/K/L/S/E/I/F・Esc、パッドの各ボタン) とは重ならない。
オーバーレイを閉じていても、速度が等倍でない・一時停止中・記録/再生中・無敵の時は画面右下に黄色で出る。

### オーバーレイ (F1)
- 上部: FPS / 速度 / timeScale / シーン / 位置・速度 / 接地 / ステート / HP / 糸 / 記録・再生の状態
- **速度**: プリセットのボタン・対数スライダー・一時停止・コマ送り (1 / 10)。「スロー時は物理の刻みも細かく」(下記)
- **ワープ**: 今のシーンの入口へ移動 / エリア1 のシーン → 入口 (エディタではシーンファイルから入口 Id を読む。Development Build ではデフォルト入口のみ、着いてから上の一覧で選ぶ) / その他のシーン
- **能力/フラグ**: 無敵・HP/ゲージ回復・糸 +100・敵全滅・全攻撃解放 / ハサミ強化 (Yellow/Blue/Red)・移動スキル (GroundSlam/SuperJump/ChargeRush/Parry) の個別切替と一括 / 進行フラグのプリセット (足すだけ) / A1_ フラグの全解除 / 現在のフラグ一覧
- **記録**: 記録の開始/停止、再生の停止、直前の再生結果 (ずれ)、記録ファイル一覧 (クリックで再生)

デバッグパネル (エディタの `NeverNight/Debug Panel`) にも同じ速度・記録の操作がある。

### 進行フラグのプリセット (エリア1)
| 名前 | 立てるフラグ |
|---|---|
| B1 改札の番人 撃破後 | A1_Boss1 |
| 中ボス 2 体 撃破後 | + A1_MidBossUpper, A1_MidBossLower |
| スピーカー 3 つ 破壊後 | + A1_Speaker1..3, A1_ZiplineReady, A1_ElevatorDeep |
| エリアボス 撃破後 | + A1_AreaBoss, A1_Pickup_AB_1 (+ 赤ハサミを付与) |

## 速度調整の仕組み
- 倍率は `GamePause.BaseTimeScale` (ポーズしていない時の timeScale) に入れる。ポーズメニュー・ショップ・会話の `GamePause.Push/Pop` は従来どおり 0 で止め、閉じると選んだ倍率に戻る。シーン開始の `GamePause.Reset` も倍率を保つ。
- 一時停止 = `GamePause.Push` (プレイヤーも入力を読まない)。コマ送りは LateUpdate で止めを外し、次のフレームの時間を `Time.captureDeltaTime` で固定ステップちょうどにして、終わったら止め直す。
- **物理**: `Time.fixedDeltaTime` は変えない (0.02 = 50Hz)。何倍速でも 1 ステップの計算は等倍と同一で、高速時は 1 フレームあたりのステップ数が増えるだけなので安定 (8 倍・120fps でも 1 フレーム約 3 ステップ)。`Time.maximumDeltaTime` (0.333) も変えない: 8 倍で描画が約 24fps を割ると 1 フレームのゲーム時間が上限で切られ、実効倍率が下がる (物理の暴走より遅くなる方を選択)。
- 等倍未満では物理が 50Hz のままなので、0.1 倍だと動きが 5 コマ/秒でカクつく。オーバーレイの「スロー時は物理の刻みも細かく」を入れると fixedDeltaTime を倍率に比例して縮める (滑らかだが積分の刻みが変わり、挙動はわずかに等倍と異なる)。既定はオフ。
- unscaled 時間で動く UI 演出 (ポーズメニュー・通知・部屋名・暗転) は倍率の影響を受けない。

## 入力の記録と再生
- 記録: `PlayerController` が入力を読んだフレームごとに、入力・deltaTime・fixedDeltaTime・timeScale・unscaledDeltaTime・物理の端数・遷移中か、を保存。30 フレームごとに位置を照合点として残す。
  - 「ステージを読み直してから記録」(既定, F7): 今の状態 (能力・スキル・アイテム・糸・装備・進行フラグ・位置 = セーブデータ形式) を取り、「つづきから」と同じ経路 (`GameSession.PendingLoad` + `StageLoader.LoadWithPlayerScene`) で読み直してから記録を始める。HP は満タン・倒した敵は復活した状態から始まる。
  - 「その場から記録」: 読み直さない。再生は読み直してから始まるので、敵の位置などが違う分ずれやすい。
  - 記録中はフレームレートを 60 に制限する (`TestPlayRecorder.RecordFrameRateCap`)。再生は記録の 1 フレーム = 実際の 1 フレームで進むため、記録のフレームが少ないほど速く再生できる。
  - ポーズメニュー中の操作は記録しない (再生ではポーズを開かない)。
- 再生: 記録の開始状態で読み直し、乱数の種をそろえ、最初の照合点の位置・速度に合わせてから入力を流す。
  - 各フレームのゲーム時間を `Time.captureDeltaTime` で記録と同じにし、物理の端数 (`Time.time - Time.fixedTime`) のずれを次のフレームで埋める → 物理ステップの回数と入力との前後関係が記録と一致する。
  - timeScale は記録時の値を使い、**速度 (TestPlaySpeed) は実時間の待ち** で表す (再生中に速度を変えてもずれない)。描画が追いつかない時は描画の許す限り速く進む。報告の「実効」が実際の倍率。
  - 暗転 (ScreenFader = DOTween の timeScale 非依存 tween) は実時間で進むため、記録・再生中は DOTween の 1 フレームの進みを記録した unscaledDeltaTime にそろえる。
  - ステージ遷移の読み込みは実時間で終わるため、再生の読み込みが遅ければ待ち、早ければ記録の「遷移中」のフレームを飛ばして合わせる (報告に「○フレーム飛ばした」と出る)。
  - 照合点ごとに位置のずれを測り、0.5 を超えたら警告ログ。結果は `TestPlayRecorder.LastReport` (オーバーレイ・デバッグパネル・Console)。

### 確認した結果 (2026-10-03, A1_T1 からエディタで)
| 記録 | 再生 | 結果 |
|---|---|---|
| 仮想入力の台本 13 秒 (60fps, 1 倍, T1→T2→T1 の遷移を含む) | 1 倍 / 4 倍 / 8 倍 | 最大ずれ 0.000 (3 回とも)。4 倍・8 倍の実効は約 1.85 倍 (エディタの描画が約 110〜160fps で頭打ち) |
| 同 (60fps, 1 倍, 遷移なし) | 1 倍 / 4 倍 | 最大ずれ 0.000。4 倍の実効 2.74 倍 |
| 仮想入力の台本 20 秒 (4 倍速で記録) | 4 倍 | 最大ずれ 0.000、実効 4.0 倍 |
| (参考) 遷移を含む記録、暗転・読み込みの同期を入れる前の版 | 1 倍 / 4 倍 | 最大ずれ 0.6 / 4.2 (→ 上の同期を入れて 0.000 に) |

**再生の速さの上限 = 描画できる fps ÷ 記録の fps**。エディタは描画が重いので 60fps 記録の 4 倍速は 2〜3 倍程度になる (ずれは出ない)。Development Build ならもっと速い。4 倍速以上で記録したもの (ボットなど) は 1 フレームの時間が長いので、その倍率で再生できる。

## 自動テストプレイ (ボット) 向け API

```csharp
// 1) 入力を差し込む
var input = new VirtualPlayerInput();
PlayerInputSource.Override = input;          // 以後キーボード/パッドの代わりにこれを読む (null で実デバイスへ戻す)
input.Move = 1f;                              // 右へ (-1..1)。Vertical は上 = +1 (下 + 攻撃 = 落下攻撃、上 + ジャンプ長押し = 大ジャンプ溜め 等)
input.Tap(PlayerButton.Jump);                 // 次に読まれるフレームで 1 回押して離す
input.Hold(PlayerButton.Jump);                // 押しっぱなし (押したフレームは Pressed も立つ) → input.Release(...)
input.Tap(PlayerButton.Attack | PlayerButton.Dash);   // Special / Finisher / Heal / Interact / Grapple(糸移動) / Pause も同様
input.UseItem(ItemSlot.Down);                 // アイテム (L1/[I] + 方向)
input.MenuMove(+1); input.MenuSubmit(); input.MenuCancel();  // メニュー (ポーズ・ショップ等) の操作
input.MenuMoveX(+1); input.MenuTab(+1);       // メニューの左右 / タブ切替 (Q/E・L1/R1。アイテム画面)
input.Tap(PlayerButton.ItemMenu);             // アイテム画面を開く/閉じる (Tab / Select)
input.MergeDevice = true;                     // 人の入力も重ねたい時
// 独自の入力源: IPlayerInputSource.Read() を実装して Override に入れてもよい (同一フレームは 1 回だけ読まれる)

// 2) 速度
TestPlaySpeed.SetScale(4f);                   // 0.1〜8。ポーズメニューを開いても閉じれば 4 倍に戻る
TestPlaySpeed.SetFrozen(true); TestPlaySpeed.Step(10);  // 一時停止・コマ送り

// 3) ワープ・状態の準備
TestPlayActions.Warp("A1_T2", "FromA1_T1");   // 非同期。TestPlayActions.IsLoading / IsReady で完了を待つ
TestPlayActions.SpawnIds("A1_T2");            // 入口 Id ("" = デフォルト)。エディタのみ他シーンも分かる
TestPlayActions.Teleport(new Vector2(40f, -95f));
TestPlayActions.SetUpgrade(ScissorUpgrade.Red, true); TestPlayActions.SetSkill(PlayerSkill.GroundSlam, true);
TestPlayActions.ApplyPreset(TestPlayActions.FlagPresets[2]); GameProgress.Set("A1_LiftWheel");
TestPlayActions.Invincible = true; TestPlayActions.RefillHealth(); TestPlayActions.KillAllEnemies();

// 4) 状態を読む
TestPlayerState s = TestPlayActions.GetPlayerState();
// s.Scene / Position / Velocity / Grounded / Facing / State (ステート名) / Hp / MaxHp / Thread / Dead / Paused / Loading
// PlayerController は TestPlayActions.Player (IsGrounded, GroundCollider, CurrentStateName, Rb など)

// 5) 記録・再生 (ボットの操作をそのまま回帰テストにできる)
TestPlayRecorder.StartRecording(restartScene: true);  // 読み直し後 IsCapturing が true になってから操作を始める
TestPlayRecorder.StopRecording();                     // → 保存先パス
TestPlayRecorder.StartReplay(path); TestPlayRecorder.ReplayFinished += report => { /* report.maxDrift など */ };
```

注意:
- `PlayerController` は GamePause 中 (ポーズメニュー・会話・ショップ・テストの一時停止) は入力を読まない。プレイヤー操作の Tap はポーズが明けてから行う。
- 会話 (Utage) は仮想入力の Attack / Interact / Jump のタップで読み進められる (`DialogueInputStrategy`, 2026-10-03)。ボットは `DialogueService.IsPlaying` の間 0.3 秒ごとに攻撃をタップし、区間の処理を止める (60 秒抜けなければ incident を記録して `Stop("dialogue_stuck")`)。
- `PlayerInputSource.ConsumedByPlayer` イベントで「プレイヤーがそのフレームの入力を読んだ」ことが分かる (1 フレーム単位で動くボットの拍に使える)。
- ステージ遷移は暗転 → 読み込み (数フレーム〜数秒、実時間)。`TestPlayActions.IsLoading` が false になるまで待つ。
- 闘技場などで倒されると HP 0 で止まる (ゲームオーバー画面)。無敵にするか、`StageLoader.LoadWithPlayerScene` で読み直す。

## 検証の手順 (Unity CLI)
`Docs/Area1/SceneBuildGuide.md` §2・§6 と同じ。A1_T1 を開いて `editor_play` → `run_script --file Docs/TestPlay/playtest_testplay.cs --entry PlayTP.<名前>`:
`Status` / `Speed ["4"]` / `Mark` → (待つ) → `Rate` (実測倍率) / `PauseOpen` / `PauseClose` (仮想入力で Esc とメニューの戻る) / `DeviceInput` /
`DeviceWalk` → `DeviceWalkLog` (実デバイス経路: Keyboard に D を流す。先に Game ビューへフォーカス:
`EditorWindow.FocusWindowIfItsOpen(GameView)`。InputSystem の設定が「キーボードは Game ビューのフォーカスに従う」) /
`RecordStart` → (待つ) → `RecordStop` / `Replay` → `Report` / `Overlay ["1"]` / `Spawns ["A1_T2"]` / `Warp ["A1_T2","FromA1_T1"]` / `Abilities` / `Presets` / `Cheats`。
オーバーレイ (IMGUI) は `capture_game_view` / `screenshot` には写らない。`ScreenCapture.CaptureScreenshot` (eval) で撮る。
スクリプトを書き換えると再生が止まって再コンパイルされる (このプロジェクトの設定)。

## 自動テストプレイボット (Jev による判断 + プログラムによる実行, 2026-10-03)

**判断 = Jev** (TypeSafe System One, `jev-latest`)、**実行 = プログラム**の組み合わせ。床に着くたび・行動が終わるたびに、
プログラムが物理で確かめた「次の行動の候補」と状態 (コンパクトな JSON) を Jev に送り、1 つ選ばせる。選ばれた行動は
物理予測どおりのフレーム入力に変えて `VirtualPlayerInput` から流す。エディタ / Development Build のみ (ゲーム側の挙動は不変)。

### ファイル (`Assets/Scripts/TestPlay/Bot/`)
| ファイル | 役割 |
|---|---|
| `TestPlayBot.cs` | 本体 (常駐 `[TestPlay]` に付く)。区間の進行・ゴール決め・候補作り・Jev への問い合わせ・止まり/死亡の検出・レポート。`BotSettings` |
| `BotWorld.cs` | 「目」: Ground レイヤーを 0.5 単位の格子で走査 → 立てる床のセグメント (`P12`、動く足場 `M3`)・解析的な移動グラフ・ゴールからの逆 Dijkstra。敵・危険・出口・移動装置・闘技場の一覧 |
| `BotPhysics.cs` | `PlayerController` と同じ式の空中シミュレーション (当たりは本物の CapsuleCast、崖登り判定も同じ式) と跳躍計画 (離陸点・ダッシュ・長押し時間・二段ジャンプ・空中ダッシュ)。空中の舵取り `Steer` は計画と実行で共用。`BotPlatformPredictor` = MovingPlatform の内部状態から先の位置を正確に予測 |
| `BotExecutor.cs` | 行動 → フレーム入力 (歩く・跳ぶ・すり抜ける・動く足場を待って乗る・斬る・裁断・回避・回復・調べる・大ジャンプ)。高速時は離陸直前と空中だけフレームあたりの物理ステップを 2 回までに抑える |
| `JevClient.cs` | API 呼び出し (UnityWebRequest、メインスレッドで見張る)。401/422 は即失敗、429/529/5xx/通信エラーは 0.5→1→2→4 秒で再試行。呼び出し数・トークンを集計。**費用の歯止め: 1 回の実行で送るリクエストは 2,000 回まで** (`MaxRequestsPerRun`、再試行を含む)。達するとボットは止まり、レポートに「送信 n/2000」を残す |
| `BotRoutes.cs` | エリア1 のクリティカルパス 29 区間 (目的: ボス/スピーカー/ピックアップ)、途中開始時のフラグ・能力、「全入口×全出口」 |
| `BotReport.cs` / `BotJson.cs` | レポート (Markdown + 判断の全記録 JSONL + スクリーンショット) / 最小の JSON |
| `Assets/Scripts/Editor/TestPlayBotWindow.cs` | `NeverNight/Test Play Bot` ウィンドウ |
| `Assets/Scripts/Editor/PlayFromTitle.cs` | `NeverNight/タイトルからプレイ` (Ctrl+Alt+P): 開いているシーンはそのまま、TitleScene から再生 (止めると元に戻る) |
| `Docs/TestPlay/playtest_bot.cs` | Unity CLI の run_script 用 (開始・状態・グラフ調査・単発の跳躍テスト) |

### API キー
`TYPESAFE_API_KEY` 環境変数 (プロセス → Windows のユーザー環境変数の順。エディタ起動後に設定しても読める) →
無ければ **`UserSettings/TypeSafeApiKey.txt`** (キーだけを 1 行。UserSettings は .gitignore 済み)。キーはログ・レポート・画面に出さない。
キーが無い・Jev が失敗した時は「プログラムの推奨 (ローカル方針)」で動き続ける (レポートの判断の出所が `local`)。

### 使い方
- **ウィンドウ** `NeverNight/Test Play Bot`: 「ルート」(開始/終了の区間、T1→T4・U・D などのボタン) か「全入口 × 全出口」(シーンごと/エリア1 全部) を選び、
  速度 (0.1〜8)・Jev の応答待ち (自動 = 等倍以上は停止 / スローは 0.25 まで)・無敵・Jev の有無・途中開始のフラグ付与 などを設定して **▶ 開始**
  (プレイ中でなければ「プレイして開始」)。下に状態 (区間・シーン・ゴール・実行中の手順・最後の判断と確信度・Jev の回数/トークン) とレポートへのボタン。
- **オーバーレイ** (F1 / R3 長押し) の「ボット」タブ: 区間の選択・速度・無敵・Jev・開始/停止・状態。
- **CLI**: `run_script --file Docs/TestPlay/playtest_bot.cs --entry PlayBot.Start --args '["0","3","1","1"]'` (開始区間, 終了区間, 速度, Jev 1/0[, 無敵 1/0]) →
  `PlayBot.Status` / `PlayBot.Summary` / `PlayBot.Stop` / `PlayBotLog.Recent ["20"]` / `PlayBotProbe.Graph ["A1_T4"]` / `PlayBotTest.Jump ["P9","P3"]` → `PlayBotTest.Result`。
- 途中の区間から始めると、エリア1 のフラグを外してから「前の区間まで終えた」フラグ (と AB 以降は赤ハサミ) を付ける。開始シーンにいる時は読み直す。

### 判断の仕組み
- 候補: ゴールへ直接 (`go_exit` / `use_transport` / `go_goal`)、隣の床へ (`jump_to_platform:P12` / `dash_jump:` / `drop_to:` / `drop_through:` / `walk_to:`)、
  動く足場 (`wait_for_platform:M3` / `ride_to:P5`)、`attack:E2` / `finisher:` / `dodge` / `retreat` / `heal` / `interact` / `wait`、止まった時だけ `explore_left/right` と `use_ability:super_jump`。
  各候補に「ルート上か (best / alternative / no)」「シミュレーションで着地するか」「危険」「失敗回数」と **`planner_pick` (プログラムの推奨)** を付ける。数値計算はすべてプログラム側。
- 問い: `action` (choice) と、無敵でなく敵が 5 以内の時だけ `danger` (noul)。選択肢が 1 つなら聞かない (`auto`)。
- ゴール: 区間の目的 (ボス・スピーカー・ピックアップ) → 戦闘中の闘技場 (ゲートが閉じている WaveArena/BossArena の敵) → 次のシーンへの出口 / 移動装置。
- 失敗した跳躍・シミュレーションで着地できない辺は重くして経路を引き直す。目的でない出口には歩き・跳躍とも入らない。
- **止まった**: 30 ゲーム秒進展なし、または同じ行動が 4 回続けて失敗 → スクリーンショット付きで記録。2 回目でその区間を失敗にし、次の区間の入口へワープして続ける。
  **迷った**: Jev の確信度 < 0.5 の判断 (床ごとに 1 枚スクリーンショット)。死亡は記録して読み直し (3 回で区間失敗)。穴/トゲでの戻しも記録。
- 速度: 応答待ちの間だけゲームを止める/遅くする。高速時は離陸の直前と空中だけ 1 フレームの物理を 2 ステップまでに抑える。

### 結果 (`TestPlay/Reports/<日時>_<名前>.md` + `_decisions.jsonl` + `_img/`。git の追跡対象外)
確認 (2026-10-03, エディタ, 無敵):
| 区間 | 速度 | 結果 | Jev 回数 / トークン |
|---|---|---|---|
| T1→T2→T3→T4→B1 | 1x / 4x / 8x (実効約 4.3x) / 0.25x | 4/4 (どの速度も) | 24〜36 回 / 約 2.3〜3.6 万 |
| B1 (番人+スピーカー①)→S1→H | 2x | 成功 | 13 回 / 1.2 万 |
| H→U1→…→MB1→ジップライン (U ルート 13 区間) | 4x | 8/13 | 280 回 / 27.7 万 |
U ルートで止まった所: U3 (コーヒーカップの闘技場の飛ぶ敵に届かない, (109.5,3.9))、W3 (動く足場 M5 に飛び乗れない, (52,90))、
W4→W5 (W3 側の出口へ出てしまう)、W5 (回るゴンドラから先へ跳べない, (62.5,192)/(52,184))、MB1 (中ボスが足場の 5 上にいて届かない, (55.6,351.9))。
D ルート・C1→AB→FP は未確認。

### 既知の限界
- 動く足場: 待っている床の端から歩きの速さで跳べる時だけ乗る (助走・ダッシュ併用の乗り移りは未対応)。回る足場 (Orbit) は失敗が多い。
- 飛ぶ敵・高い所のボス: 近くに来るのを待って斬るだけ。横風 (W4)・水中 (D4/D5) の補正はシミュレーションに無い (空中の舵取りで補うのみ)。
- 0.5 単位より狭い足場は床として扱わない。グラップル (青)・壁張り付き (黄)・アイテムは使わない。
