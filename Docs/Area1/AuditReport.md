# エリア1 QA 監査レポート (2026-10-03)

対象: `Assets/Scenes/Area1/A1_*.unity` 27 枚。方法: 全シーンを順に開いて (保存せず) 構造を収集するエディタスクリプト `Docs/Area1/SceneScripts/audit_scan.cs` (再実行可) + プレイ中のテレポート遷移テスト。旧シーン (GameField_old) は未接触。**修正を要する配線バグは見つからなかったため、シーン・ビルダー・ビルド設定への変更は無し。**

## 1. Build Settings / Validate

| 項目 | 結果 |
|---|---|
| Area1 の 27 シーンが Build Settings にある | OK (27/27、全て enabled、パスは `Assets/Scenes/Area1/A1_*.unity`) |
| ビルダー `Validate()` 27 枚 | **全て 0 error / 0 warning** |
| `SpawnDefault` (Id 空) | 全シーンにちょうど 1 つ |
| スポーン Id の重複 | 無し |

## 2. 遷移グラフ (SceneTransitionZone 56 + TransportInteractable 5 = 61 辺)

- 全ての辺で 先シーンが存在し、`_entranceId` と同じ Id の PlayerSpawnPoint が先にある。`_entranceId` は全て `From<自シーン>` 規約どおり。受け側に出口の無い孤立スポーンは無し。
- layout.json の scene-transition リンク (WL=W6⇔W1、E1=MB2⇔C1 は移動装置として換算) と比較: **欠けている辺 0**。

| 種別 | 内容 | 判定 |
|---|---|---|
| 一方通行 | A1_U4(Uh)→A1_T3 / A1_D2(D2w)→A1_T3 / A1_MB1(S2)→A1_U1 (ジップライン) | layout どおり。逆辺は無く、受け側はスポーンのみ |
| 余分な辺 | A1_U1→A1_FP (U1 の `ExitToA1_FP`、柵の奥) | layout は `FP→U2e oneWay` だが、CrossSceneContracts の設計 (柵が開けば U1⇔FP) どおり実装。**意図どおり。layout 側の oneWay 表記が実装と食い違う** (情報) |
| 移動装置 | W6⇔W1 (`EmergencyLift`、要 `A1_LiftWheel` 両側) / MB2⇔C1 (`ElevatorDeep`、要 `A1_ElevatorDeep` 両側) / MB1→U1 (`StarZipline`、要 `A1_ZiplineReady`) | layout は WL を `W6→WL oneWay` と記すが、契約書どおり双方向で実装。意図どおり |
| 縦の出口 (床の無い幅広ゾーン) | AB→FP, D1→H, D2→D1, D5→D2, H→U1, MB1→W8 (落下), W1→W2, W4→W5, W5→W6, W6→W8, W8→MB1 | ゾーン全幅の下に床が無いのは天井口/穴の縦出口のため仕様どおり。横出口は全て床あり |
| 別エリア行き (X1 星見台 / X2 地下水路 / X3 奈落の倉庫 / NX エリア2) | 遷移無し (ヒント・看板のみ) | 契約書どおり準備中 |

## 3. 到達性 (部屋単位、layout の gate を使用)

| 条件 | 到達 | 未到達 |
|---|---|---|
| (a) 能力なし・フラグなし (T1 から) | **9/57 部屋** (T1 T1s T2 T2s T3 T3b T3c T4 B1。隠し壁は通常攻撃で壊せる) | B1 のボス (`A1_Boss1`) で S1 以降が止まる |
| (b1) 進行フラグ (Boss1 / Speaker1-3 / MidBossUpper / MidBossLower) のみ、赤ハサミ無し | 46/57 | Hs1 Hs2 U2r2 Uh W4r W5g D3a D4s ST FP FP2 |
| (b2) + AreaBoss + 赤ハサミ (二段ジャンプ) | 51/57。**ストーリー上の全シーン (FP 含む) に到達可** | Hs1 Hs2 U2r2 Uh W4r D4s (全て技能の寄り道) |
| (c) + 落下/突進/大ジャンプ | 57/57 | - |

- 技能が要る部屋: **落下 (Slam)**: Hs2 (H)、Uh (U4、T3 天井側と共有ブロック)、D4s (D4)。**突進 (Rush)**: U2r2 (U2)、W4r (W4)、(D2w の鉄扉の奥は準備中ヒントのみ)。**大ジャンプ (Super)**: Hs1 (H)。
- 二段ジャンプ (赤ハサミ) が要る部屋: W5g、D3a、FP2 (糸巻き①)、ST→FP の遷移。赤ハサミは A1_AB のエリアボス撃破で出現する (`UpgradePickup`、初期は非アクティブ)。つまり W5g / D3a は終盤の戻り道。
- ストーリー必須経路に技能は不要 (SkillBreakable の実配置 9 個は全て寄り道/準備中スタブ側)。
- 注意: ここでの到達は layout の gate と実フラグ連鎖 (Boss1 → S1 扉、Speaker1/2/3 → C1 大扉、MidBossLower → S3、AreaBoss → ST) で見ている。部屋内の跳躍可否はプレイテスト済みの記録に依存。

## 4. フラグ

収集: BossArena / BreakableSpeaker / FlagDoor (+additional) / FlagActivator / LeverSwitch / SkillBreakable / BreakableBox / WaveArena / KeyItemPickup / NpcTalk / TransportInteractable / Conveyor / BossPhaseSwitch の全フラグ系フィールド (107 件) と pickupId。

| 項目 | 結果 |
|---|---|
| 要求されるが誰も立てないフラグ | **0** |
| §6 の命名どおりか (`A1_Boss1`/`Speaker1-3`/`MidBossUpper`/`MidBossLower`/`AreaBoss`/`ZiplineReady`/`LiftWheel`/`ElevatorDeep`/`FenceOpen`/`LadderBackstage`/`BeltReversed`/`ArenaCups`/`RatNest`) | 全て一致、typo 無し |
| 立てるだけで読まれないフラグ (問題なし) | `A1_HeartPin1-4` `A1_Lore_Journal1-5` `A1_Spool1-3` `A1_Skill_SpinSlash` (ピックアップ)、`A1_Npc_Doll/MapSellerD5/MapSellerU4` (NPC)、`A1_Hidden_T2s` (T2s に目隠し無し)、`A1_Rush_*` `A1_Slam_*` `A1_Super_Hs1` (ブロックの永続化) |
| 複数箇所で立てるフラグ | `A1_Slam_Uh_1` (A1_T3 と A1_U4 の SkillBreakable。契約書どおり意図的) |
| 近似名 | `A1_Hidden_{T2s,U3s,W2s}` / `A1_Rush_{D2w,U2r,W4r}` は命名規則による類似のみで typo ではない |
| pickupId | 24 個 + 赤ハサミ 1 個 = 全て一意、衝突無し、空 Id 無し |

## 5. ご褒美 (layout `rewards` 21 件 vs 配置)

| 部屋 | 種別 | 要件 | 配置 |
|---|---|---|---|
| T1s / H2 / W4r / FP | 日誌 1/2/5/4 | -, -, rush, - | OK (`A1_Pickup_T1s_1` / `H2_1` / `W4r_1` (突進ブロック) / `FP_1`) |
| T2s | 糸×30 | 隠し | OK (隠し壁 `A1_Hidden_T2s`) |
| T3b / U2a / W7 / D2w | 道具 まち針 / ボビン爆弾 / 針山 / 糸玉 | - | OK (各 ×3、D2w は日誌その3 も) |
| U3s / W2s | ハートピン①② | 隠し | OK (隠し壁) |
| Hs2 / D4s | ハートピン③④ | slam | OK (落下ブロック) |
| Hs1 | 新技 回転斬り | super | OK (大ジャンプブロック、仮の KeyItem) |
| Uh / U2r2 | 糸×40 / ×80 | slam / rush | OK (量も一致) |
| W5g / D3a | 糸×100 / ×60 | dj | OK (量一致) |
| D6 / D7 / FP2 | 糸巻き②③① | -, -, dj | OK |
| AB | 赤ハサミ | エリアボス | OK (UpgradePickup Red、撃破後に出現) |
| **H2 の「糸×50」** | 糸 | - | **欠落**: 日誌のみ。ThreadPickup が無い (下の findings) |
| 余分: D5 糸×15 (`A1_Pickup_D5_1`) | 糸 | - | layout に無いが CrossSceneContracts に記載ある意図的な追加 |

## 6. スポーン / 出口の床

- 全 88 スポーン: 岩/床に埋まっている物 0、真下 3 単位以内に接地面が無い物 0。
- 横出口のゾーンは全幅の下に床あり (Validate と自前チェックの両方)。縦出口は §2 参照。

## 7. プレイ スモークテスト (A1_T1 から再生、テレポートで出口ゾーンへ)

全ホップで正しい `stage` に遷移し、到着後 `grounded=True`、コンソールのエラーは Unity MCP の接続エラー (MCP-FOR-UNITY、無関係) のみ。

T1→T2→T3→T4→B1→H→U1→H→D1→H→C1→AB→C1 (13 ホップ、フラグ Boss1/Speaker1-3 を設定) / C1→MB2→C1 (`ElevatorDeep` を Use) / MB1→U1 (`StarZipline`、(257.5,3.94) 着地) / W1→W6→W1 (`EmergencyLift`) / FP→U1→FP (`A1_FenceOpen` を設定してゾーン通過) / FP 到着 (355.5,0.94)。

後始末: 設定した全フラグ (`A1_Boss1 A1_Speaker1-3 A1_ElevatorDeep A1_ZiplineReady A1_LiftWheel A1_FenceOpen`) を `Unset` して全て False を確認、`Red` を Revoke、再生停止。シーンは未保存 (isDirty=false)、git の変更は監査開始前と同一 (save.json はプレイ中に書かれない)。

## 8. 指摘事項 (重大度順)

| 重大度 | 内容 | 対応 |
|---|---|---|
| 低 | **A1_H の H2 に「糸×50」が無い** (日誌その2 のみ。layout `rewards` は「日誌その2+糸×50」) | 未修正 (報酬の追加は設計判断。必要なら `build_A1_H.cs` に `ThreadPickupAt(…, 50, "A1_Pickup_H2_2")` を足して再実行) |
| 低 | layout.json の oneWay 表記と実装の食い違い: `FP→U2e` (実装は U1⇔FP の双方向)、`W6→WL` (実装は W6⇔W1 双方向) | **polish A で layout を `oneWay: false` に更新** (U1/FP のレシピを合わせ、遷移・スポーンは不変) |
| 情報 | D5 の糸×15 は layout に無い追加報酬 | 意図的 |
| 情報 | CrossSceneContracts の W5 の注記 (W4 `SpawnFromA1_W5` に着くと追い風で点検路から押し出される) | **polish A で解消** (W4 の風よけ) |
| 情報 | `A1_Slam_Uh_1` の共有 (T3 と U4) | 意図的 |
| 情報 | 能力ゲート (dj) の W5g / D3a / FP2 は静的には検証できず、過去のプレイテスト記録に依存 | - |

配線バグ (遷移先名・スポーン Id・Build Settings・フラグ typo) は 0 件。
