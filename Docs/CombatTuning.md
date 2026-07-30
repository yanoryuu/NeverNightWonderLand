# 戦闘の調整ガイド (フレーム・当たり判定・攻撃範囲・パリィ・コンボ)

戦闘まわりの数値は**すべてコードの外**にあり、以下の2箇所を Unity のインスペクタで書き換えるだけで調整できる。
コードの変更・再コンパイルは不要 (プレイモード中の変更は再生終了で戻るので、非再生中に変更して保存する)。

| 置き場所 | 中身 |
|---|---|
| `Assets/Scripts/Player/PlayerConsts.asset` | プレイヤー全体の共通値 (移動/ジャンプ/基本攻撃/パリィ/コンボ/スキル) |
| `Assets/Attacks/*.asset` | 装備攻撃1つごとの定義 (Slash / HeavyCut / WideSlash / NeedleShot など) |

## 1. 攻撃のフレーム (時間) の意味

近接攻撃は `AttackProfile` という共通の入れ物で定義される。時間は秒指定 (60fps換算: 0.1s = 6F)。

| 項目 | 意味 |
|---|---|
| `Duration` | モーション全体の長さ。この間は攻撃状態 (終わると行動可能)。コンボの1段ぶんの長さでもある |
| `HitDelay` | ボタンを押してから当たり判定が出るまでの発生時間。`Duration - HitDelay` が持続後の硬直にあたる (判定は1フレームのみ) |
| `HpDamage` / `GuardDamage` | HP(赤)/防御値(白)への与ダメージ |

例: 二刀流 (DualAttack) = Duration 0.25 / HitDelay 0.06 → 発生4F・全体15F。

## 2. 当たり判定 (攻撃範囲) の修正方法

攻撃範囲は `AttackProfile` の2つの値で決まる:

- **`Offset`** — 判定ボックスの中心位置。x は**向いている方向へ自動で反転**する (右向きで +x が前方)。y は上下
- **`BoxSize`** — 判定ボックスの大きさ (幅×高さ)

**手順**:
1. 非再生中に `PlayerConsts.asset` (または `Assets/Attacks/` の装備攻撃アセット) を選択
2. 対象プロファイルの `Offset` / `BoxSize` を変更
3. **Hierarchy で Player を選択すると、Scene ビューに現在装備中の近接攻撃の判定ボックスがワイヤー表示される** (PlayerController の Gizmo)。これを見ながら合わせるのが早い

どのプロファイルがどこにあるか:
- 基本攻撃 (未装備時のフォールバック含む): `PlayerConsts.asset` → 「攻撃 (スタイル別)」の DualAttack / HeavyAttack など
- 装備攻撃 (□で振る技): `Assets/Attacks/Slash.asset` などの `Profile`
- 裁断: `PlayerConsts.asset` → FinisherProfile / FinisherRange
- 落下攻撃の着地衝撃: `PlayerConsts.asset` → 「移動スキル」の SlamAttack
- 遠距離 (針弾): `Assets/Attacks/NeedleShot.asset` (速度/寿命=射程/ダメージ/発射高さ) + 弾の当たりは `Assets/Prefabs/NeedleShot.prefab` の BoxCollider2D

攻撃が当たる対象は `PlayerConsts.asset` の `AttackTargetLayer` (現在 Enemy+Ground) で決まる。
壊せるオブジェクト (箱/スピーカー等) は Ground レイヤーに置くのが作法。

## 3. パリィの調整 (`PlayerConsts.asset` → 「パリィ (スキル)」)

発動: **方向入力なしでダッシュボタン** (スキル `Parry` 所持時のみ。未所持なら通常ダッシュ)。

| 項目 | 既定 | 意味 |
|---|---|---|
| `ParryWindow` | 0.18 | 受付時間。この間に受けた攻撃を無効化 (成功すると硬直なしで即行動可能) |
| `ParryRecovery` | 0.25 | 受付終了後の硬直。この間は無防備 |
| `ParrySuccessInvincible` | 0.6 | 成功時に得る無敵時間 |

- 連打防止はダッシュと共通のクールダウン (`DashCooldown` = 0.4)
- 見た目: 受付中=水色 / 硬直中=灰色 / 成功=白 (ParryState 内の色定数)
- 成功時の追加効果 (カウンター攻撃・ゲージ回収など) を足す場合は `ParryState.TryAbsorb` に書く

## 4. コンボと鍛冶強化 (`PlayerConsts.asset` → 「近接コンボ / 鍛冶強化」)

- スイング中に攻撃を先行入力すると次の段につながる (各段のフレームは同じプロファイル)
- 段数を伸ばす = `Duration` の調整、または `BaseMaxCombo` の変更

| 項目 | 既定 | 意味 |
|---|---|---|
| `BaseMaxCombo` | 3 | 初期状態の最大コンボ数 |
| `MaxComboCap` | 5 | 鍛冶強化を重ねても超えない上限 |
| `ForgeAttackBonus` | 1 | 鍛冶強化1回ごとの近接HPダメージ加算 |

**鍛冶強化 (有償)**: 能力 (ハサミの色) とは別で、各鍛冶師が**1人1回**、糸と引き換えに
「攻撃力 +ForgeAttackBonus / 最大コンボ +1 (上限 MaxComboCap)」を行う。
費用は各鍛冶師の `Blacksmith` コンポーネントの `_forgeCost` (大広間=30)。
新しい鍛冶師に強化を持たせるには `_forgeId` に一意な名前を入れるだけ (空なら有償強化なし)。
実施済みは進行フラグ `Forge_<forgeId>`、回数は `SaveData.forgeLevel` で永続化される。

## 5. スキルの調整 (`PlayerConsts.asset` → 「移動スキル」)

`SlamFallSpeed` (落下攻撃の降下速度) / `SlamAttack` (着地衝撃) / `SkillChargeTime` (大ジャンプ・突進の溜め時間) /
`SuperJumpHeight` (大ジャンプ高さ=24.5) / `ChargeRushSpeed` (突進速度。障害物まで継続)。
被弾関連は「体力・被弾」の `InvincibleTime` / `KnockbackVelocity` / `HurtDuration`。
