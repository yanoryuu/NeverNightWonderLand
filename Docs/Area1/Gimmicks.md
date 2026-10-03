# エリア1 ギミック部品リファレンス

シーンを組むエージェントはこのファイルだけを見て配置できるように書いている。
**全クラスとも名前空間なし (グローバル)**。フィールド名はすべて `_` 始まりの private `[SerializeField]`
(CLI の `set_component_properties` / `set_serialized_field` ではこの名前で指定する)。

## 0. 共通の約束

| 項目 | 内容 |
|---|---|
| レイヤー | 0 Default / 2 Ignore Raycast / 8 Player / 9 Enemy / **10 Ground** / **11 Interactable** |
| プレイヤーの地面判定 | layer 0 と 10 の **非トリガー** コライダー。乗れる物は layer 10 に置く |
| プレイヤーの攻撃が当たる | layer 9, 10 (IDamageable を持つ物) |
| インタラクト対象 | **layer 11** + **トリガー** Collider2D + `IInteractable` 実装コンポーネント (同じ GameObject か親)。プレイヤー中心から `InteractRadius` 内で最寄りの物が対象 |
| 中にいる間効くゾーン (WindArea / WaterZone / HazardZone) | **layer 2 (Ignore Raycast)** に置く。Physics2D の「クエリがトリガーに当たる」設定が ON のため、layer 0 に置くと壁判定 Raycast に引っかかり、ゾーン内で壁張り付きが誤発動しうる。isTrigger は Awake で自動で ON になる |
| 進行フラグ | `GameProgress.Has/Set` (文字列、セーブされる)。名前は Area1Build.md §6 |
| 通知 | `Notifier.Notify(文字列)` → 画面のトースト (NotificationView) |
| 予兆・見た目の子 | 「~Visual」系フィールドは子 GameObject を渡し、コンポーネントが SetActive で切り替える |

### プレイヤー側の外力フック (PlayerController, 新規追加)
PlayerController は毎物理ステップ速度を直接指定するため、`SurfaceEffector2D` / `AreaEffector2D` /
`BuoyancyEffector2D` は**効かない** (使わないこと)。代わりに以下の汎用 API を追加した。ゾーンが無ければ従来と完全に同じ挙動。

| API | 用途 |
|---|---|
| `AddExternalVelocity(Vector2)` | 次の物理ステップだけ速度を上乗せ (毎 FixedUpdate 呼ぶ)。コンベア・横風・水流が使う |
| `SetMovementModifier(Object source, PlayerMovementModifier)` / `RemoveMovementModifier(source)` | 移動倍率 (水中)。複数登録は積 |
| 自動: Kinematic Rigidbody2D の上に接地している時 | その接地点の速度 (`GetPointVelocity`) で運ぶ。MovingPlatform / Piston はこれで乗り手を運ぶ |
| `GroundCollider` | 乗っている地面のコライダー (非接地なら null) |
| `Teleport(Vector2)` | 速度・外力の持ち越しを消して瞬間移動 |

外力は「ステートが速度を決めた後に上乗せ→次ステップ頭で除去」方式なので、ステートやアニメーション (Speed パラメータ) には外力抜きの速度が見える (運ばれて立っている時に走りモーションにならない)。

---

## 1. 新規コンポーネント

### MovingPlatform (`Assets/Scripts/Gimmick/MovingPlatform.cs`)
動く足場: 整備用リフト (往復) / 観覧車のゴンドラ (円軌道・常に上向き) / 回る歯車。乗ったプレイヤーを縦横とも運ぶ (検証済み: 上下・円軌道でプレイヤーとの相対位置が誤差 0)。
- 必須: `Rigidbody2D` (Awake で Kinematic に強制) + 非トリガー Collider2D (BoxCollider2D など) を**同じ GameObject か子**に。**layer 10 (Ground)**。見た目は SpriteRenderer (子でも可)。
- すり抜け床として乗りたい場合は Collider2D の usedByEffector + `PlatformEffector2D` (oneWay) でもよい (上昇中のリフトでも接地判定は外力抜きの速度で行うので問題なし)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_mode` | `MovingPlatform.Mode` | PingPong | `PingPong`=経由点を往復 / `Loop`=最後まで行ったら最初へ / `Orbit`=円軌道 |
| `_waypoints` | Vector2[] | (0,0),(0,6) | PingPong/Loop: **配置位置からのオフセット**。先頭は通常 (0,0)。開始時に `配置位置+_waypoints[0]` へ移動 |
| `_speed` | float | 2 | 移動速度 (units/s) |
| `_waitTime` | float | 1 | 各経由点での停止 (s) |
| `_easeInOut` | bool | true | 区間の始終で減速 (SmoothStep) |
| `_orbitCenter` | Transform | null | Orbit の中心。null なら**配置位置が中心** |
| `_radius` | float | 6 | Orbit 半径 |
| `_angularSpeed` | float | 15 | Orbit 角速度 (deg/s, 正=反時計回り) |
| `_phaseDegrees` | float | 0 | Orbit 開始角 (0=中心の右, 90=真上)。ゴンドラを並べる時はこれをずらす。開始時にこの角の軌道上へ移動する |
| `_spinSpeed` | float | 0 | 足場自体の回転 (deg/s)。ゴンドラは 0 (上向き維持)。歯車の見た目用 |
| `_requiredFlag` | string | "" | このフラグが立つまで停止 (例 `A1_LiftWheel`)。同シーン内で立てば即動き出す |
| `_startDelay` | float | 0 | 動き出すまでの遅延 (位相ずらし) |

配置例:
- 整備リフト: GameObject `Lift` (layer 10) に SpriteRenderer + BoxCollider2D(3×0.5) + Rigidbody2D + MovingPlatform。`_waypoints=[(0,0),(0,9)]`, `_waitTime=1.5`。下端の床の上面に合わせて置く。
- 観覧車: 空の `WheelCenter` を置き、ゴンドラ 4 個に MovingPlatform(`_mode=Orbit`, `_orbitCenter=WheelCenter`, `_radius=12`, `_angularSpeed=10`, `_phaseDegrees=0/90/180/270`)。
- レバー連動: LeverSwitch の `_progressFlag` と MovingPlatform の `_requiredFlag` に同じフラグ。

### Conveyor (`Assets/Scripts/Gimmick/Conveyor.cs`)
ベルトコンベア。**上に接地しているプレイヤー**を横へ押し流す。判定は「プレイヤーの `GroundCollider` がこの GameObject (子を含む) のコライダーか」。
- 必須: 非トリガー Collider2D (BoxCollider2D 推奨) を**同じ GameObject か子**に、**layer 10**。地形 Tilemap とは別の GameObject にすること (Terrain に塗ると判定できない)。Rigidbody2D 不要。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_speed` | float | 3 | 押す速度 (units/s)。正=右, 負=左。歩き速度より小さめ推奨 (逆らって歩けるように) |
| `_reverseFlag` | string | "" | 立つと向きが反転 (例 `A1_BeltReversed`)。ライブで切替 |
| `_directionVisuals` | Transform[] | 空 | 矢印などの見た目。localScale.x の符号を流れの向きに合わせる (右向きの絵で作る) |

配置例: `Belt` (layer 10) に SpriteRenderer(タイル Draw Mode) + BoxCollider2D(幅10×高0.5) + Conveyor(`_speed=-3`, `_reverseFlag="A1_BeltReversed"`)。上面を整数 Y に。逆転レバーは LeverSwitch(`_progressFlag="A1_BeltReversed"`)。

### WindArea (`Assets/Scripts/Gimmick/WindArea.cs`)
横風ゾーン (UnityEngine.WindZone と名前がぶつかるので **WindArea**)。中にいるプレイヤーを押す。常時 or 間欠 (突風)。
- 必須: トリガー Collider2D (BoxCollider2D) を同じ GameObject に。**layer 2**。継承元 `PlayerAreaZone`。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_velocity` | Vector2 | (-4,0) | 風の速度。横風は (±x,0)。歩き速度 (MoveSpeed) を超えると逆らえない |
| `_groundedMultiplier` | float | 1 | 接地中の倍率 (地上で弱めるなら <1) |
| `_pattern` | `WindArea.Pattern` | Continuous | `Continuous`=常時 / `Gust`=吹く・止むを繰り返す |
| `_gustOnTime` / `_gustOffTime` | float | 2 / 2 | Gust: 吹く/止む時間 (s) |
| `_rampTime` | float | 0.3 | Gust: 強さの立ち上がり/減衰時間 |
| `_phaseOffset` | float | 0 | Gust: 周期の開始オフセット |
| `_warningTime` | float | 0.6 | Gust: 吹く何秒前から `_warningVisual` を出すか |
| `_activeVisual` | GameObject | null | 吹いている間だけ表示 (風の線) |
| `_warningVisual` | GameObject | null | 予兆 (Gust のみ) |

公開: `Strength` (0..1)。配置例: `Wind` (layer 2) に BoxCollider2D(trigger, 部屋の幅×高さ) + WindArea(`_velocity=(-5,0)`, `_pattern=Gust`)、子 `WindLines` を `_activeVisual` に。

### WaterZone (`Assets/Scripts/Gimmick/WaterZone.cs`)
水中。入ると移動倍率を登録、出ると解除 (ゾーンの無効化・シーン遷移でも解除される)。任意で水流。
- 必須: トリガー Collider2D を同じ GameObject に。**layer 2**。継承元 `PlayerAreaZone`。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_modifier` | `PlayerMovementModifier` | (0.6, 0.6, 0.4, 0.4) | `MoveSpeed` 歩き/走り倍率, `Jump` ジャンプ初速倍率 (二段ジャンプ含む), `Gravity` 重力倍率, `MaxFallSpeed` 最大落下速度倍率。ジャンプ高さ = Jump²/Gravity 倍 (既定 0.9 倍) |
| `_current` | Vector2 | (0,0) | 水流 (中にいる間ずっと足す) |

配置例: `Water` (layer 2) に BoxCollider2D(trigger, 水面〜底) + WaterZone。見た目の半透明スプライトは別の子に (sortingOrder を前面に)。

### HazardZone (`Assets/Scripts/Gimmick/HazardZone.cs`)
接触ダメージ (トゲ・蒸気・奈落・ピストンの押しつぶし面)。`PlayerHealth.TakeDamage` を通すので、無敵時間・ノックバック (ゾーン中心から離れる向き)・回避ダッシュ無効は既存仕様どおり。中にいる間は毎ステップ判定 (無敵が切れたら再被弾)。
- 必須: トリガー Collider2D を同じ GameObject に。**layer 2**。継承元 `PlayerAreaZone`。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_damage` | int | 1 | HP ダメージ。0 なら戻すだけ (奈落でダメージ無しにしたい時) |
| `_returnToSafeGround` | bool | false | 触れたら直前の安全地点へ戻す (ダメージの有無に関係なく)。シーン開始時にプレイヤーへ `PlayerSafeGround` を自動追加 |
| `_fadeOnReturn` | bool | true | 戻す時に ScreenFader で暗転 |
| `_cycle` | bool | false | 時間で ON/OFF (蒸気)。false なら常時 ON |
| `_onTime` / `_offTime` | float | 1.5 / 2 | ON/OFF の時間 |
| `_phaseOffset` | float | 0 | 周期オフセット (並んだ噴出口をずらす) |
| `_warningTime` | float | 0.5 | ON の何秒前から `_warningVisual` を表示 |
| `_activeVisual` | GameObject | null | ON の間だけ表示 (蒸気) |
| `_warningVisual` | GameObject | null | ON 直前の予兆 |

公開: `IsHazardActive`, `SetHazardEnabled(bool)` (Piston などが外部から止める。周期と AND)。
安全地点 (`PlayerSafeGround`, 自動追加): 接地中・Static な地面の上・**体の周り左右1.2/上下0.6 に HazardZone が無い**・足元の左右両端の下に地面がある時に記録。ステージ切替で破棄。記録が無ければ最寄りの PlayerSpawnPoint へ戻す。
配置例:
- トゲ床: `Spikes` (layer 2) に BoxCollider2D(trigger, トゲの上面 0.5 高) + HazardZone(`_returnToSafeGround=true`)。トゲの見た目は同じ GameObject の SpriteRenderer でよい。
- 奈落: 穴の底 (カメラ下限より下) に横長の HazardZone(`_damage=1`, `_returnToSafeGround=true`)。
- 蒸気: `Steam` に HazardZone(`_cycle=true`, `_onTime=1.2`, `_offTime=2`)、子 `SteamFx` を `_activeVisual`、子 `SteamHiss` を `_warningVisual`。

### Piston (`Assets/Scripts/Gimmick/Piston.cs`)
タイマーで伸び縮みする押しつぶしピストン。縮み待機 → 予兆 (震え+着色) → 伸びる → 伸び待機 → 縮む のループ。本体は Kinematic を速度で動かすので**動いている間も実体** (上に乗れば運ばれる)。
- 必須: `Rigidbody2D` (Kinematic に強制) + 非トリガー Collider2D、**layer 10**。押しつぶし判定は**子**の HazardZone (先端の面に薄いトリガー, layer 2) を `_crushHazard` に。**配置位置 = 縮んだ位置**。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_extendOffset` | Vector2 | (0,-3) | 伸びた位置 (配置位置からのオフセット)。下向き押しつぶしなら (0,-n)、横なら (±n,0) |
| `_retractedWait` | float | 1.5 | 縮んで待つ時間 |
| `_telegraphTime` | float | 0.5 | 予兆の時間 |
| `_extendDuration` | float | 0.15 | 伸びる時間 (短い=速く叩く) |
| `_extendedWait` | float | 0.8 | 伸びて待つ時間 |
| `_retractDuration` | float | 0.8 | 縮む時間 |
| `_startDelay` | float | 0 | 最初の周期の遅延 (並べた時の位相ずらし) |
| `_crushHazard` | HazardZone | null | 押しつぶし面。**伸びている最中だけ**有効 (伸びきって止まっている間は天井として安全) |
| `_shakeVisual` | Transform | null | 予兆で震わせる見た目 (子の SpriteRenderer の Transform)。本体は震わせない |
| `_shakeAmplitude` | float | 0.06 | 震えの振れ幅 |
| `_telegraphColor` | Color | (1,0.55,0.45) | 予兆中に子の全 SpriteRenderer に乗算する色 |

配置例: `Piston` (layer 10) に Rigidbody2D + BoxCollider2D(2×1) + Piston(`_extendOffset=(0,-3.5)`)、子 `Sprite` (SpriteRenderer, `_shakeVisual`)、子 `Crush` (layer 2, BoxCollider2D trigger 1.8×0.3 を底面に, HazardZone `_damage=1`) を `_crushHazard`。伸びきった時の底面が床上面から 1.0 以下になると確実に当たる (プレイヤーの高さ ≈1.2)。

### FallingObjectSpawner (`Assets/Scripts/Gimmick/FallingObjectSpawner.cs`) + FallingObject
上から荷物を落とす。スポナーの Y から、X は `[_minX,_maxX]` (スポナーからのオフセット) のランダム位置に生成。生成物は落ちる前に `_warningTime` 震え、プレイヤーに当たるとダメージ、地面 (`_groundMask` の非トリガー) か寿命で消える。
- 必須: 空の GameObject に FallingObjectSpawner だけ。**天井 (岩) の中ではなく空中**に置く (生成直後に地面と重なると即消える)。生成物は物理ボディ不要 (自前移動+重なり判定)。`FallingObject` は生成時に自動で付く (手で置かない)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_prefab` | GameObject | null | 落とすプレハブ (任意)。null なら `_sprite` から自動生成 |
| `_sprite` | Sprite | null | 自動生成時のスプライト (null なら見えない箱になるので設定する) |
| `_color` | Color | 茶 | 自動生成時の色 |
| `_size` | Vector2 | (1,1) | 当たり判定 (units)。自動生成時は見た目もこの大きさに拡縮 |
| `_sortingOrder` | int | 5 | 自動生成時の描画順 |
| `_interval` / `_intervalJitter` | float | 2 / 0.5 | 生成間隔 ± ばらつき |
| `_startDelay` | float | 1 | 最初の生成までの遅延 |
| `_minX` / `_maxX` | float | -3 / 3 | 生成範囲 (スポナーからの X オフセット) |
| `_activationDistance` | float | 20 | プレイヤーとの水平距離がこれ以下の時だけ生成 (0=常に) |
| `_damage` | int | 1 | ダメージ |
| `_warningTime` | float | 0.5 | 落ちる前に震える時間 |
| `_initialSpeed` / `_gravity` / `_maxFallSpeed` | float | 0 / 30 / 18 | 落下の初速・加速度・最大速度 |
| `_lifetime` | float | 6 | 寿命 (s) |
| `_groundMask` | LayerMask | Default+Ground | 当たると消えるレイヤー |
| `_stopFlag` | string | "" | 立ったら生成停止 |

配置例: 天井のすぐ下 (天井の下面 −0.6) に `CargoDrop` + FallingObjectSpawner(`_sprite`=落ちてくる物のスプライト (A1_W8 は鉄のおもり `props/falling_weight.png` = **仮素材**。木箱の絵 `cargo_crate` は「壊せる木箱」の意味なので使わない, PendingChanges 30), `_size=(1.2,1.2)`, `_minX=-4`, `_maxX=4`, `_interval=1.8`)。

### TransportInteractable (`Assets/Scripts/Gimmick/TransportInteractable.cs`)
インタラクトで別ステージへ運ぶ移動装置 (ジップライン=一方通行 / 非常用リフト=起動後は双方向 / 搬出エレベーター)。`StageLoader.Instance.TransitionTo(_sceneName, _entranceId)` を呼ぶ (StageLoader が無いテストシーンでは SceneManager.LoadScene)。
- 必須: **layer 11** + トリガー Collider2D (同じ GameObject)。見た目の SpriteRenderer は任意。遷移先に `PlayerSpawnPoint` (Id=`_entranceId`) を置くこと。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_sceneName` | string | "" | 遷移先シーン名 (Build Settings 登録必須)。空だとインタラクト不可 |
| `_entranceId` | string | "" | 遷移先の PlayerSpawnPoint の Id (慣例 `From<自シーン名>`) |
| `_requiredFlags` | string[] | 空 | 全て立つまで使えない (例 `A1_ZiplineReady`) |
| `_lockedMessage` | string | 「動かない…」 | 使えない時の通知 |
| `_activateFlag` | string | "" | 初回使用時に立てるフラグ (起動の永続化) |
| `_activateMessage` | string | "" | 初回使用時の通知 |
| `_promptText` | string | 乗る | プロンプト |
| `_lockedPromptText` | string | 調べる | 使えない時のプロンプト (空なら `_promptText`) |
| `_promptOffset` | Vector3 | (0,1.2,0) | プロンプト位置 |

公開: `IsUnlocked`。配置例:
- ジップライン (一方通行): 乗り場に TransportInteractable(`_sceneName="A1_xx"`, `_entranceId="FromA1_yy"`, `_requiredFlags=["A1_ZiplineReady"]`, `_promptText="滑り降りる"`)。到着側には置かない。
- 非常用リフト (下で起動→以後双方向): 下側 `_activateFlag="A1_LiftWL"`, 上側 `_requiredFlags=["A1_LiftWL"]`。レバー起動にするなら両側 `_requiredFlags=["A1_LiftWheel"]`。
- 搬出エレベーター: `_requiredFlags=["A1_ElevatorDeep"]`。

### FlagActivator (`Assets/Scripts/Gimmick/FlagActivator.cs`)
フラグに合わせて GameObject を表示/非表示。同シーン内でフラグが立てばその場で切替 (GameProgress.Changed 購読)。
- 必須: なし。**切り替える対象とは別の、常に有効な GameObject** に付ける (自分を無効にすると購読が止まる)。対象は初期状態 active/inactive どちらでもよい (OnEnable で正しい状態にする)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_flags` | string[] | 空 | 判定するフラグ (空なら常に不成立) |
| `_requireAll` | bool | true | true=全て立てば成立 / false=どれか1つで成立 |
| `_showWhenSet` | GameObject[] | 空 | 成立時に有効 (不成立時は無効) |
| `_hideWhenSet` | GameObject[] | 空 | 成立時に無効 (不成立時は有効) |
| `_setFlagsWhenSatisfied` | string[] | 空 | 成立時に追加で立てるフラグ (フラグの別名)。例: `_flags=["A1_Speaker2"]` → `["A1_ZiplineReady"]`。成立判定は OnEnable と Changed 時なので、元フラグが立つシーン (スピーカーの部屋) に置く。ロード時に成立済みでも立てる |

公開: `IsSatisfied`。配置例:
- 大扉のランプ: ランプごとに FlagActivator(`_flags=["A1_Speaker1"]`, `_showWhenSet=[LampLit1]`, `_hideWhenSet=[LampOff1]`)。
- 闘技場のご褒美: ピックアップ (非アクティブで配置) を `_showWhenSet` に、`_flags=["A1_ArenaCups"]`。取得済みならピックアップ自身の Awake が消す。
- フラグの別名: スピーカー②の部屋に FlagActivator(`_flags=["A1_Speaker2"]`, `_setFlagsWhenSatisfied=["A1_ZiplineReady"]`)。③も同様に `A1_ElevatorDeep`。

### WaveArena (`Assets/Scripts/Gimmick/WaveArena.cs`)
BossArena の複数ウェーブ版。入るとゲートを閉じ、ウェーブを順に出して、全滅ごとに次へ。全ウェーブ撃破でフラグ+ゲート開放+通知。クリア済み (フラグあり) なら何も起きずゲートは開いたまま。闘技場の敵は撃破記録 (DefeatedEnemyRegistry) の対象外 (`SetDefeatTracking(false)`) なので、死んでやり直すと全ウェーブ出直す。撃破以外で消えた敵 (null / IsDead) も全滅判定に数える。
- 必須: トリガー Collider2D (同じ GameObject, isTrigger 自動) をゲートより内側のエリアに。ゲートは **layer 10 の実体で非アクティブ**で配置。layer はトリガーなので 2 推奨。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_flagId` | string | "" | クリアフラグ (例 `A1_ArenaCups`, `A1_RatNest`) |
| `_waves` | `WaveArena.Wave[]` | 空 | ウェーブの並び (下表) |
| `_gates` | GameObject[] | 空 | 入場時に有効化 (閉じる) するゲート |
| `_startMessage` | string | "" | 開始時の通知 |
| `_clearMessage` | string | 扉が開いた | クリア時の通知 |

`WaveArena.Wave`: `Spawns` (`SpawnEntry[]`), `Preplaced` (GameObject[]: シーンに**非アクティブ**で置いた敵。撃破記録済みで出なかった物は自動で除外), `Delay` (float, 前ウェーブ全滅からの待ち, 既定1)。
`WaveArena.SpawnEntry`: `Prefab` (EnemyController 付き: `Assets/Prefabs/Enemy.prefab` / `FlyingEnemy.prefab`), `Consts` (EnemyConsts, null=プレハブのまま。例 `Assets/Scripts/Enemy/EnemyConsts_Heavy.asset`), `Tint` (Color, 既定白), `Point` (Transform, 出現位置), `Offset` (Vector2, Point 未設定時の闘技場からのオフセット), `Scale` (float, 既定1, 仮ボスは2)。
公開: `event Action OnCleared`。配置例: `Arena` (layer 2) に BoxCollider2D(trigger, 部屋の内側) + WaveArena、左右の入口に `GateL`/`GateR` (layer 10, BoxCollider2D+SpriteRenderer, 非アクティブ)。ご褒美は FlagActivator で `_flagId` に連動。

### UpgradePickup (`Assets/Scripts/Gimmick/Pickups/UpgradePickup.cs`)
拾うとハサミ強化を得る (`ItemPickup` 派生)。(2026-10-03: エリア1 では使っていない。赤ハサミは拠点の鍛冶師 (Blacksmith `_requiredAnyFlags`) が渡し、A1_AB の撃破報酬は糸の大束 ×200 になった。部品としては残す)。通知「赤色のハサミを手に入れた!」は PlayerProgression.Grant が出し、続けて `_hintMessage`。
- 必須: ItemPickup と同じ (SpriteRenderer 必須 + **layer 11** + トリガー Collider2D)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_pickupId` (基底) | string | "" | 取得の永続化 Id (例 `A1_Pickup_Boss_Red`)。**必ず設定** |
| `_promptText` (基底) | string | 拾う | プロンプト |
| `_promptOffset` (基底) | Vector3 | (0,1,0) | プロンプト位置 |
| `_upgrade` | ScissorUpgrade | Red | `Yellow`(0)=壁張り付き / `Blue`(1)=グラップル / `Red`(2)=二段ジャンプ+滑空 |
| `_hintMessage` | string | 「空中でもう一度…」 | 取得後の操作説明 (空なら出さない) |

配置例 (旧 A1_AB): ボス部屋に非アクティブで `RedScissors` (UpgradePickup) を置き、FlagActivator(`_flags=["A1_AreaBoss"]`, `_showWhenSet=[RedScissors]`)。現在の A1_AB は同じ方法で ThreadPickup ×200 (`ThreadBundle`) を出す。

### NpcTalk (`Assets/Scripts/Gimmick/NpcTalk.cs`)
数行のセリフを順に出す NPC (迷子の人形・地図売り)。インタラクトのたびに次の行を **TutorialMessageView** (UI シーン StageUI の `TutorialMessage`) に表示、最後の次で閉じる。`_hideDistance` 以上離れると途中で閉じる。TutorialMessageView が無いシーンでは Notifier に全行を流す。
※ Utage のシナリオで会話させたい場合は既存の `NpcTalkPoint` (下記) を使う。**エリア1 の NPC (迷子の人形・地図売り 2 人) は 2026-10-03 に NpcTalkPoint (Utage) へ移行済み** — NpcTalk は旧ステージ用に残している。
- 必須: **layer 11** + トリガー Collider2D。見た目の SpriteRenderer は任意。同じ場所に TutorialHintZone を重ねない (同じ表示枠を取り合う)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_speakerName` | string | "" | 設定すると「名前「セリフ」」で表示 |
| `_lines` | string[] (TextArea) | 空 | 初回のセリフ (1要素=1回)。空ならインタラクト不可 |
| `_repeatLines` | string[] | 空 | 2回目以降のセリフ (空なら毎回 `_lines`)。`_talkedFlag` 必須 |
| `_talkedFlag` | string | "" | 「話した」の永続化 (例 `A1_Npc_Doll`)。初回を最後まで聞くと立つ |
| `_rewardThread` | int | 0 | 初回を最後まで聞いた時に渡す糸 (`_talkedFlag` があれば一度きり) |
| `_promptText` / `_nextPromptText` | string | 話す / 次へ | プロンプト (会話前/会話中) |
| `_promptOffset` | Vector3 | (0,1.2,0) | プロンプト位置 |
| `_hideDistance` | float | 3 | これより離れたら閉じる |
| `_message` | TutorialMessageView | null | 表示先 (null なら話しかけた時に 同じシーン → UI シーン の順で検索) |

配置例: `LostDoll` (layer 11) に SpriteRenderer + BoxCollider2D(trigger 1.5×2) + NpcTalk(`_speakerName="迷子の人形"`, `_lines=[...]`, `_talkedFlag="A1_Npc_Doll"`, `_rewardThread=20`)。

### BossPhaseSwitch (`Assets/Scripts/Gimmick/BossPhaseSwitch.cs`)
ボス戦の前半/後半を地形で切り替える (A1_AB のエリアボス)。ボスの HP が最大 × `_threshold` 以下になると、`_phase1` の物を点滅させてから消し、`_phase2` の物を出す。ボスが倒れたら `_phase2` も消す。撃破済み (`_defeatedFlag`) / ボス不在なら最初から両方を消す。段階は保存しない (やり直すと前半から)。撃破後に出す物は FlagActivator で別に出す。
- 必須: なし (常に有効な GameObject に付ける)。`_phase2` は非アクティブで置いてよい (Start で消す)。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_boss` | EnemyController | null | 見張るボス |
| `_defeatedFlag` | string | "" | 撃破フラグ (BossArena の `_flagId` と同じ) |
| `_threshold` | float | 0.5 | 後半に移る HP の割合 |
| `_phase1` / `_phase2` | GameObject[] | 空 | 前半だけ / 後半だけの物 |
| `_blinkTime` | float | 1.5 | 前半の物が消える前の点滅時間 |
| `_phase2Message` | string | "" | 後半に移った時の通知 |

公開: `InPhase2`。配置例 (A1_AB): `_phase1=[Phase1_Notes (浮遊する音符の MovingPlatform の親)]`, `_phase2=[Phase2_FloorLifts (上向き Piston の親)]`。

---

## 2. 拡張した既存コンポーネント (後方互換)

### FlagDoor (`Assets/Scripts/Gimmick/FlagDoor.cs`) — 複数フラグ対応
フラグが立っていると開く扉 (ロード時に成立済みなら即開・見た目も消える / 同シーンで成立したら上へスライド)。**`_additionalFlags` を追加**: `_flagId` と合わせて全て立った時だけ開く。空なら従来どおり。
- 必須: 非トリガー Collider2D (子でも可) + SpriteRenderer、layer 10。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_flagId` | string | "" | 必要フラグ |
| `_additionalFlags` | string[] | 空 | **新規**。追加で全て必要なフラグ |
| `_slideDistance` | float | 4 | 開く時の上スライド量 |
| `_slideDuration` | float | 0.8 | スライド時間 |

配置例 (スピーカー3つの大扉): FlagDoor(`_flagId="A1_Speaker1"`, `_additionalFlags=["A1_Speaker2","A1_Speaker3"]`, `_slideDistance`=扉の高さ)。

### BreakableBox (`Assets/Scripts/Enemy/BreakableBox.cs`) — 永続化フラグ・コインの箱対応
攻撃で壊れる箱/隠し壁 (HP+防御値ダメージ合算)。**`_flagId` を追加**: 設定すると破壊時に Set、フラグ済みなら Awake で消える。空なら従来どおり再入場で復活。
- 必須: SpriteRenderer + 非トリガー Collider2D、攻撃が当たるよう **layer 10** (足場・壁として振る舞う) か 9。
- **当たり判定に入った箱は全部壊れる** (2026-10-04 PendingChanges 29): `PlayerController.PerformAttackHit` は当たった物すべて (箱・隠し壁のセル・能力ブロックのセル・敵) にダメージを与える。2026-10-03 (PendingChanges 17) に入れた「箱・ブロックは 1 回にプレイヤーに一番近い 1 つだけ」の制限はやめた。セルごとに分けたのはそのまま (当たり判定の外のセルは残る)。能力ブロックの「刃は通らない」のヒントは全ブロック共通の 2 秒の間隔で、複数セルに当たっても 1 回だけ。
- **壊した木箱は拠点で休むまで壊れたまま** (2026-10-04 PendingChanges 31): `_flagId` が空の箱 (ふつうの木箱・コインの箱) は壊すと `BrokenCrateRegistry` (静的、`DefeatedEnemyRegistry` と同じ扱い) に ID = 「シーン名:オブジェクト名:初期位置 (F1)」を記録し、次に部屋に入った時は Awake で消える。拠点 (SavePoint) で休む = ホーム画面を開いた時 (`HomeUIView.Open`, 倒した敵の記録と同時) と全リセット (`StageLoader.LoadWithPlayerScene` = タイトルから開始・セーブから復帰・死亡リトライ) で記録をクリアして全部元に戻る。**セーブファイルには入れない** (セーブは休んだ後なので、復帰時は全部の箱が元どおりで正しい)。
- **コインの箱**: コインを取った後は今まで通り箱ごと二度と出ない (GameProgress)。壊したがコインを取らずに部屋を出た場合は、次に来た時は箱は壊れたまま、**コインが箱のあった場所に置いてある** (Start で `CoinPiece` をその場に出す)。休んだ後は箱が戻り、コインは中に戻る。コインは失われず、コインの箱だけ復活して見分けがつく事も無い。
- **隠し壁・スキルブロックは 1 セル = 1 個** (ビルダー `HiddenWall` / `SkillBlock` が子 `Cell_x_y` を並べる)。全セルが同じ `_flagId` を共有し、1 セル壊れた時点でフラグが立つ (隠し部屋の幕が開く)。再訪では同じフラグのセルが全部消えている。以前は壁全体が 1 個 (Tiled の絵) で、1 発で全部消えていた = 「積んだ箱がまとめて壊れる」の原因。

| フィールド | 型 | 既定 | 意味 |
|---|---|---|---|
| `_maxHp` | int | 1 | 耐久 |
| `_flagId` | string | "" | **新規**。破壊の永続化 (例 `A1_Hidden_U2`) |
| `_coinId` | string | "" | **新規 (2026-10-03)**。コインの箱: 壊すと `CoinPiece` が飛び出す (例 `A1_Coin_T3c_1`)。コインを取った後 (GameProgress に `_coinId`) は箱ごと出ない。取らずに部屋を出たら次は箱は壊れたままでコインがその場に置いてある (2026-10-04) |
| `_coinPrefab` | CoinPiece | null | コインのプレハブ (`Assets/Prefabs/CoinPiece.prefab`) |
| `_hitFlashTime` | float | 0.1 | 被弾フラッシュ |
| `_breakFadeTime` | float | 0.3 | 破壊演出時間 |

配置例 (隠し壁): ビルダー `HiddenWall(x0,y0,x1,y1,"A1_Hidden_<部屋>", hp=1)` (セルごとの hp。以前の壁全体 HP3 から変更)。
配置例 (木箱): ビルダー `Crate(x, floorY, coinId=null)` / `CrateStack(x, floorY, count, coinId, coinIndex)`。絵 = `Area1Gen/props/cargo_crate.png` (色は全部 `CrateColor`。コインの箱も見た目・手応えはふつうの箱と同じ = PendingChanges 20)、hp 1、Rigidbody2D (動的, 横と回転を固定) なので積むと乗り、下を壊すと上が落ちる。箱そのものは永続化しない。cargo_crate の絵は壊せる木箱専用で、飾り (`Prop`) には使わない (PendingChanges 28: 同じ絵の飾りが壊せず紛らわしかった)。

### LootPiece / ThreadPiece / CoinPiece / ThreadDropSpawner (`Gimmick/Pickups/`) — 地面に散らばって拾う物 (2026-10-03 新規)
- `LootPiece` (基底): Rigidbody2D (動的, 回転固定) + CircleCollider2D + 跳ねる物理マテリアル (`Prefabs/LootBounce`: 弾み 0.35 / 摩擦 0.8)。**layer 2 (Ignore Raycast)** で、`Rigidbody2D.excludeLayers = ~Ground` なので当たるのは layer 10 だけ (プレイヤー・敵・トリガーとは接触しない。すり抜け床は上から落ちれば乗る)。出てから `_pickupDelay` 0.35 秒後、プレイヤー中心から `_magnetRadius` 1.6 以内で吸い寄せ (物理を止めて直接動かす)、`_collectRadius` 0.55 以内で取得 → ふくらんで消える。消えるのは取得とシーン遷移だけ (時間では消えない)。出た位置から `_lostFallDistance` 40 下へ落ちたら (地形の外) その場で取得扱い。`SetBounds(Rect)` (ボスの糸) を受けると、範囲の左右の端で跳ね返り (×−0.5)、上の端で落ち始める (FixedUpdate)。生成位置が地形の中なら上へずらす (`FreeSpawnPoint`)。
- `ThreadPiece` (`Prefabs/ThreadPiece.prefab`, 絵 `StageAsset/Props/thread_piece.png` = 糸巻きの 128px 縮小): 取ると `AddThread(value, notify: false)` = **入手通知の帯は出さない** (HUD の所持数が増えるだけ)。
- `ThreadDropSpawner` (Player.prefab): `EnemyController.ThreadDropped (位置, 数, 敵の大きさ)` を購読して粒を散らす (以前の「倒した瞬間に自動加算」は廃止)。粒の数 = min(糸, `_maxPieces` 12)、値は均等割り (余りは先頭)。大きさ = `_pieceScale` 0.38 × (1 + 0.15 × (値−1))。初速: 横 ±`_burstX` 3.2 × 広がり (敵の大きさ × (1 + 0.04 × 粒数))、上 `_burstY` 5〜8.5 (ボス (scale 2) は ×1.2)。プレハブが無い時は従来どおりその場で加算。**ボス** (イベントの範囲 `EnemyController.LootArea` の幅 > 0 = BossArena のボス, 2026-10-04 PendingChanges 25): `BossFountain` = 粒 = 糸 × `_bossPiecesPerThread` 0.6 を `_bossMinPieces` 12〜`_bossMaxPieces` 30 に丸める (100 → 30)、落ちる先を闘技場の端 (`_bossEdgeMargin` 1.5 内側) から端まで等分して、そこに届く速さで `_bossPiecesPerFrame` 3 粒/フレーム打ち上げる (上 `_bossBurstY` 8〜12.5、横 ±`_bossMaxSpeedX` 16 まで。遠い粒は高く打ち上げる。天井は越えない)。各粒に `SetBounds(闘技場)`。イベントは `ThreadDropped (位置, 数, 敵の大きさ, 範囲)`。
- `CoinPiece` (`Prefabs/CoinPiece.prefab`, 0.55 単位, 絵 `Props/coin_piece.png`): 取ると `GameProgress.Set(_coinId)` + `PlayerItemInventory.AddCoins(1)` + 通知「コインを手に入れた (計 n 枚)」。枚数はセーブ (`SaveData.coins`) とアイテム画面の収集品 (`CollectionCatalog` の Counter=Coins の項目) に出す。HUD には出さない。交換は未定。
- 作り直し: `Docs/Area1/SceneScripts/prep_props.cs` (`PrepProps.Run`: Props の絵のインポート / 2 つのプレハブ / Player.prefab の ThreadDropSpawner)。

---

## 3. 再利用する既存コンポーネント

### SavePoint (`Gimmick/SavePoint.cs`)
拠点 (休む = HP全回復/補充/装備入替/セーブ, HomeUI シーンの HomeUIView を開く)。必須: SpriteRenderer, **layer 11** + トリガー Collider2D。
フィールド: `_promptOffset` (Vector3, (0,1.2,0))。プロンプトは固定「休む」。
配置: ベンチのスプライトに SavePoint + BoxCollider2D(trigger)。PlayerSceneBootstrap が HomeUI をロードしていること (既定の `_uiSceneNames` に含まれる)。

### BossArena (`Gimmick/BossArena.cs`)
入るとゲートを閉じ、ボス (`EnemyController`) の撃破でフラグ+開放+通知。撃破済みならボスを消しゲート開放。必須: トリガー Collider2D (isTrigger 自動)。
フィールド: `_flagId` (string, 例 `A1_Boss1`), `_boss` (EnemyController, シーンに置いた敵), `_gates` (GameObject[], 非アクティブで配置), `_defeatMessage` (string), `_followUpMessage` (string, 2026-10-03 追加, 既定 "" = 出さない。撃破通知に続けて出す世界観の一文), `_followUpUnlessUpgrade` (bool, 既定 false。true ならプレイヤーが `_followUpUpgrade` (ScissorUpgrade, 既定 Red) を持っている時は出さない)。
エリア1 の中ボス 2 体 (A1_MB1 / A1_MB2): 赤ハサミ未入手なら撃破通知の後に「ハサミがかすかに震えた。……拠点の鍛冶屋なら、何か分かるかもしれない」(鍛冶屋に寄らない人への誘導。呼びかけ調にしない)。ビルダー `BossRoom(..., followUpMessage, followUpUnlessRed)`。
配置: 部屋内側を覆う `Arena` トリガー + BossArena。ボスは Enemy/FlyingEnemy.prefab を localScale 2 倍, `_consts=EnemyConsts_Heavy`。ボスの糸は `EnemyController._threadDropOverride` で上書き (ビルダー `BossRoom(..., threadDrop)`: B1 20 / MB1・MB2 25 / AB 100 (2026-10-04)。Start でトリガーの範囲を `_boss.LootArea` に入れ、糸の粒は闘技場いっぱいに噴き上がる (PendingChanges 25)。Heavy は普通の敵と共用なので変えない = PendingChanges 18)。ご褒美は FlagActivator で `_flagId` に連動。

### BreakableSpeaker (`Gimmick/BreakableSpeaker.cs`)
攻撃で壊せるスピーカー。破壊でフラグ+通知、破壊済みなら出現しない。必須: SpriteRenderer + Collider2D, layer 10 (攻撃が当たる)。
フィールド: `_flagId` (例 `A1_Speaker1`), `_maxHp` (3), `_breakMessage`, `_hitFlashTime` (0.1)。公開 `event Action OnBroken`。

### Door (`Gimmick/Door.cs`)
レバー等から `Open()` で開く扉 (永続化なし。永続化したい時はレバーの `_progressFlag` + FlagDoor)。フィールド: `_slideDistance` (3), `_slideDuration` (0.8)。必須: 非トリガー Collider2D (子可), layer 10。

### LeverSwitch (`Gimmick/LeverSwitch.cs`)
インタラクトで一度だけ作動。必須: SpriteRenderer, **layer 11** + トリガー Collider2D。
フィールド: `_targetDoor` (Door, 任意), `_progressFlag` (string, 任意: 作動の永続化。例 `A1_LiftWheel`, `A1_FenceOpen`, `A1_LadderBackstage`, `A1_BeltReversed`), `_activatedMessage`, `_promptText` (レバーを引く), `_promptOffset`。
配置: フラグ連動ギミック (FlagDoor / MovingPlatform `_requiredFlag` / Conveyor `_reverseFlag` / FlagActivator / TransportInteractable `_requiredFlags`) とはフラグ名だけでつなぐ (シーンをまたいでも可)。
※ 実体がインタラクト用トリガー1つだけの場合、レバーは layer 11 のトリガーのみにする (壁にしない)。

### SkillBreakable (`Gimmick/SkillBreakable.cs`)
移動スキルの衝撃でのみ壊れるブロック (通常攻撃はヒントのみ)。必須: SpriteRenderer + 非トリガー Collider2D, **layer 10**。
フィールド: `_requiredSkill` (PlayerSkill: `GroundSlam`=落下攻撃, `SuperJump`=大ジャンプ, `ChargeRush`=横突進), `_flagId` (例 `A1_Slam_<部屋>_<n>`, `A1_Super_<部屋>`, `A1_Rush_<部屋>`), `_lockedMessage` (空=自動)。色: 落下=橙 / 大ジャンプ=空色 / 突進=紫。
2026-10-03〜: ビルダー `SkillBlock` は **1 セル = 1 個** (同じ `_flagId`)。スキルは当たったセルだけ砕く = 体の通る穴 (落下攻撃・大ジャンプ = 幅 1〜2 の縦穴、突進 = 高さ 2 の横穴) が開く。落下攻撃は着地しかけた時も足元のブロックを砕いて落下を続ける (`GroundSlamState`: 積んだ段を順に突き抜ける)。ヒント通知の連発防止は全ブロック共通 (2 秒) で、スキルで砕いた直後 1 秒は出さない。

### UpgradeWall (`Gimmick/UpgradeWall.cs`)
指定のハサミ強化を持つ攻撃でのみ壊れる壁。必須: SpriteRenderer + Collider2D, layer 10。フィールド: `_required` (ScissorUpgrade, Red), `_flagId`, `_lockedMessage`。

### Ribbon (`Gimmick/Ribbon.cs`)
対応色のハサミ強化で切れるリボン (永続化なし)。必須: SpriteRenderer + Collider2D, layer 10。フィールド: `_required` (ScissorUpgrade, Yellow), `_cutFadeTime` (0.3)。

### Blacksmith (`Gimmick/Blacksmith.cs`)
鍛冶師 NPC (ハサミ強化を授ける+有償の鍛冶強化)。必須: SpriteRenderer, **layer 11** + トリガー Collider2D。
フィールド: `_upgrade` (ScissorUpgrade), `_grantsUpgrade` (bool, 既定 true。false = ハサミ強化を授けず鍛冶強化と会話だけ), `_smithName`, `_promptOffset`, `_requiredFlags` (string[], **全て**立つまで渡さない = AND), `_requiredAnyFlags` (string[], 2026-10-03 追加。空でなければ**どれか 1 つ**が立つまで渡さない = OR。`_requiredFlags` と両方あれば両方を満たす), `_lockedMessage` (解禁前のトースト), `_lockedLines` (string[], 追加。設定すると解禁前は話しかけるたびに 1 行ずつ画面上部の案内枠 (StageUI の TutorialMessageView) に「名前「行」」で出し、最後の行の次で閉じる・離れると (`_hideDistance` 3) 閉じる。空なら従来どおり `_lockedMessage`), `_grantMessage` (追加, 既定「いいハサミだ。強化してやろう」= 従来の台詞。渡す時のトースト。続けて PlayerProgression.Grant が取得通知 (操作説明の 2 行目つき) を出す), `_forgeId` (空=有償強化なし), `_forgeCost` (30)。新しいフィールドの既定値は従来の動きと同じ (旧ステージの鍛冶師はそのまま)。
流れ (`_grantsUpgrade` true): 未取得かつ未解禁 → 案内 / 未取得かつ解禁 → `_grantMessage` + Grant / 取得済み → 有償の鍛冶強化 (1 回) → 「〜の調子はどうだ?」。
**Utage 会話 (2026-10-03 追加, 全て任意・既定は空 = 従来の表示)**: `_lockedLabel` (解禁前), `_lockedRepeatLabel` + `_talkedFlag` (解禁前の 2 回目以降の短い版), `_grantLabel` (渡す時。会話の**終わりに** Grant → 取得通知), `_forgeLabel` (有償の鍛冶強化の申し出。会話の終わりに糸を払って強化、足りなければ「糸が足りない (鍛冶強化: 糸 30)」の通知), `_afterLabel` (強化も鍛冶強化も済んだ後), `_forgeBeforeUpgrade` (bool, 既定 false。true なら解禁前でも解禁前の会話に続けて鍛冶強化を受けられる)。会話中は `CanInteract=false`。
**拠点 A1_H の鍛冶師** (2026-10-03〜): `_grantsUpgrade=true`, `_upgrade=Red`, `_requiredAnyFlags=["A1_MidBossUpper","A1_MidBossLower"]`, `_forgeId=A1_Hub` 糸 30。**最初のハサミ = 赤 (二段ジャンプ・滑空) はここで受け取る**。セリフは Utage (Dialogue.xls): `_lockedLabel=A1_Smith_Locked` / `_lockedRepeatLabel=A1_Smith_LockedRepeat` (`_talkedFlag=A1_Npc_Smith`) / `_grantLabel=A1_Smith_Grant` / `_forgeLabel=A1_Smith_Forge` / `_afterLabel=A1_Smith_After`。`_forgeBeforeUpgrade=true` = **有償の鍛冶強化は赤ハサミより前 (ボス1後に拠点へ来た時) から受けられる** (PendingChanges 11a): 解禁前は 案内 → 鍛冶強化の申し出 → (糸 30 あれば) 強化。

### ShopKeeper (`Gimmick/ShopKeeper.cs`)
インタラクトで ShopView を開く。必須: SpriteRenderer, **layer 11** + トリガー Collider2D。ShopView+ShopPanel は UI シーン ShopUI にある (旧ステージは自前の SceneUI)。フィールド: `_shop` (ShopView, null=同じシーン → UI シーンの順で検索), `_promptOffset`。

### ItemPickup 系 (`Gimmick/Pickups/`)
共通 (基底 `ItemPickup`): 必須 SpriteRenderer, **layer 11** + トリガー Collider2D。フィールド `_pickupId` (永続化 Id, 例 `A1_Pickup_<部屋>_<n>`。空=再訪で復活), `_promptText` (拾う), `_promptOffset` ((0,1,0))。
- `ThreadPickup`: `_amount` (int, 10) 糸。絵は Drive の糸巻き `StageAsset/Props/thread_spool.png` (2026-10-03〜。ビルダー `ThreadPickupAt`、100 以上の大束は ×1.25)。
- `ConsumableItemPickup`: `_item` (ItemDefinition, プレイヤーのカタログ内), `_count` (1)。まち針 (A1_T3 の T3b) は絵を Drive の `Props/machi_needle.png` に (ミシン針・布カッターはステージに置いていない)。
- `EquipmentPickup`: `_attack` (AttackDefinition) 攻撃方法の解放。
- `KeyItemPickup`: `_itemName` (通知用), `_flagId` (立てるフラグ)。
- `UpgradePickup` (新規, 上記)。

### NpcTalkPoint (`Dialogue/NpcTalkPoint.cs`)
Utage シナリオを再生する会話 (`DialogueService.Play`)。必須: layer 11 + トリガー Collider2D。フィールド: `_scenarioLabel` (先頭の * 不要), `_repeatLabel` (2026-10-03 追加, 任意。2 回目以降のラベル, `_talkedFlag` 必須), `_talkedFlag` (追加, 任意。初回の会話の終わりに立つ), `_rewardThread` (追加, int 0。初回の会話の終わりに渡す糸, `_talkedFlag` があれば一度きり), `_promptText` (話す), `_promptOffset`。新しいフィールドの既定値は従来どおり (毎回 `_scenarioLabel`)。シナリオは `Assets/Dialogue/Scenarios/Dialogue.xls` (Docs/DialogueSystem.md)。
エリア1: 迷子の人形 (A1_H, `A1_Doll_First`/`A1_Doll_Repeat`, `A1_Npc_Doll`, 糸 50)、地図売り (A1_U4 `A1_MapSellerU4_First`/`_Repeat`, A1_D5 `A1_MapSellerD5_First`/`_Repeat`。どちらも拠点の鍛冶屋の話を含む = 11b)。ビルダー `NpcDialogueAt`。

### TutorialHintZone (`Tutorial/TutorialHintZone.cs`)
入ると操作ヒントを TutorialMessageView に表示。必須: トリガー Collider2D (自動)。フィールド: `_keyboardText`, `_gamepadText` (空=キーボードと同じ), `_message` (null=使う時に 同じシーン → UI シーン StageUI の順で検索), `_hideOnExit` (true)。

### SceneTransitionZone (`Common/SceneTransitionZone.cs`)
触れると別ステージへ。必須: トリガー Collider2D (自動)。フィールド: `_sceneName`, `_entranceId` (遷移先 PlayerSpawnPoint の Id, 慣例 `From<自シーン名>`)。スポーンがゾーン内でも一度出るまで発火しない (アーミング)。出口ゾーンの全幅の下に床を通す。

### PlayerSpawnPoint (`Common/PlayerSpawnPoint.cs`)
入り口マーカー。フィールド: `_id` (空=デフォルト入口)。出口ゾーンの少し内側に置く。

### GoalZone (`Common/GoalZone.cs`)
触れるとリザルト画面。必須: トリガー Collider2D。フィールドなし。

### CameraLimit (`Common/CameraLimit.cs`)
そのステージにいる間カメラの下限を上書き。フィールド: `_minY` (float, 2)。地下・y<2 を含むシーンは必須 (部屋の yMin + 2 程度)。

### CameraFollow (`Common/CameraFollow.cs`)
PlayerScene 側のカメラ (ステージには置かない)。`SnapToTarget()` は遷移/ワープ後に呼ばれる。

### StageLoader (`Common/StageLoader.cs`)
PlayerScene 常駐。`StageLoader.Instance.TransitionTo(stageName, entranceId)` でステージだけ入替 (ステータス維持)。ステージには置かない。

### UISceneBootstrap (`Common/UISceneBootstrap.cs`)
`PlayerSceneBootstrap` として各ステージに置く。フィールド: `_uiSceneNames` (string[], エリア1は `["PlayerScene"]`)。

### DamageFloaterSpawner (`UI/DamageFloaterSpawner.cs`)
各ステージに1つ。フィールド: `_floaterPrefab` (`Assets/Prefabs/DamageFloater.prefab`), `_scatterRadius` (0.3)。

### AreaTitleView / TutorialMessageView (`UI/`)
UI シーン StageUI (プレハブ `StageUI.prefab`) の子 (旧ステージは自前の SceneUI の子)。AreaTitleView: CanvasGroup 必須, `_areaName` (旧方式のみ。StageUI では空で `StageInfo.ConsumeEntryTitle()` の文字を出す = エリアのシーンはエリア名を初回だけ、フラグ `AreaTitleShown_A{n}`), `_label` (TMP_Text)。TutorialMessageView: CanvasGroup 必須, `_label` (TMP_Text), `_fadeDuration` (0.25)。NpcTalk / TutorialHintZone の表示先。

### Notifier (`Common/Notifier.cs`)
静的: `Notifier.Notify(string)`。全ギミックのメッセージはこれを通る。

### GameProgress (`Save/GameProgress.cs`)
静的: `Has(flag)`, `Set(flag)`, `event Changed(string)`, `Unset(flag)` (デバッグ, イベント無し)。

---

## 4. フラグ配線の早見表 (エリア1)

| フラグ | 立てる物 | 参照する物 |
|---|---|---|
| `A1_Speaker1..3` | BreakableSpeaker `_flagId` | FlagDoor (`_flagId` + `_additionalFlags`), FlagActivator (ランプ) |
| `A1_ZiplineReady` | スピーカー②の部屋の FlagActivator (`_flags=["A1_Speaker2"]`, `_setFlagsWhenSatisfied=["A1_ZiplineReady"]`) | TransportInteractable `_requiredFlags` (ジップライン) |
| `A1_ElevatorDeep` | スピーカー③の部屋の FlagActivator (`_flags=["A1_Speaker3"]`, `_setFlagsWhenSatisfied=["A1_ElevatorDeep"]`) | TransportInteractable `_requiredFlags` (搬出エレベーター) |
| `A1_LiftWheel`, `A1_FenceOpen`, `A1_LadderBackstage`, `A1_BeltReversed` | LeverSwitch `_progressFlag` | MovingPlatform `_requiredFlag` / FlagDoor / FlagActivator / Conveyor `_reverseFlag` |
| `A1_ArenaCups`, `A1_RatNest` | WaveArena `_flagId` | FlagActivator (ご褒美) |
| `A1_Boss1`, `A1_MidBossUpper`, `A1_MidBossLower`, `A1_AreaBoss` | BossArena `_flagId` | FlagActivator (UpgradePickup 等) / BossPhaseSwitch `_defeatedFlag` |
| `A1_Hidden_*` | BreakableBox `_flagId` (セルごと、共有) | FlagActivator / HiddenRoomCover (隠し部屋の幕) |
| `A1_Coin_*` | CoinPiece (コインの箱 BreakableBox `_coinId`) | BreakableBox (取った後は箱ごと出ない) |
| `A1_Slam_*`, `A1_Rush_*`, `A1_Super_*` | SkillBreakable `_flagId` | — |
| `A1_Pickup_*` | ItemPickup `_pickupId` | — |

