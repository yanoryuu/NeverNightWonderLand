# エリア1 シーン間の約束ごと (後続の担当が守ること)

作り済みのシーンが前提にしている、まだ無いシーン側の要件。担当シーンを作る時にここを確認し、満たしたら ✅ を付ける。

| 担当シーン | 必要なもの | 由来 |
|---|---|---|
| A1_U1 | ✅ スポーン `FromA1_H` (268.5,-19.5, 穴の右の床) | A1_H の上の出口 |
| A1_U4 | ✅ 縦穴 Uh 上端の床の落下攻撃ブロック (SkillBreakable GroundSlam) のフラグを **`A1_Slam_Uh_1`** にする (A1_T3 の天井側と同じフラグ) | A1_T3 |
| A1_D1 | ✅ スポーン `FromA1_H` (244.5,-64.5, 格子の真下の配管の足場 y-66)。拠点の格子から落ちると着地、最上段から真上へ跳ぶと拠点の `FromA1_D1` (251.5,-52.5) へ。H→D1→H をプレイで確認 | A1_H の床の出口 |
| A1_D2 | ✅ 配電室 (D2w) のレバー② `LadderLever` (154.2,-123) で `A1_LadderBackstage`。天井の跳ね上げ戸 (x147..153 y-105, FlagActivator で消える) の下に梯子 (x150, -123→-103, フラグで出現) → 最上段で自動の `ExitToA1_T3` (一方通行) → A1_T3 の `FromA1_D2` (155.5,-94.5) に到着・接地をプレイで確認。T3 側の戸も開いた見た目になる。レバー① `BeltLever` は `A1_BeltReversed` (A1_D2 のベルト 3 本が西向き -3.5 → 東向き +3.5) | A1_T3 の T3c |
| A1_D3 | ✅ スポーン **`FromA1_D5`** (自動で OK: A1_D5 の東の出口 x240..243 y-141..-132, 床 y-141)。A1_D3 側の西の通路も同じ高さ (床 y-141) にそろえる。A1_D5 の出口の手前 (x231..240) は段 (y-135/-138) と床 y-141、天井 y-126 で、点滅する案内灯 (`GuideLamp0..8`, 緑 (0.4,1,0.78)) が出口の上 (239.4,-132) まで続いている。D3 側の入口にも同じ色の灯を 1〜2 個置くと本道がつながって見える。D5 の地図売りが「歯車の縦穴」「底の重たいやつを倒すとエレベーターで大扉の回廊のそばへ」と案内済み。**✅ D3 側**: 入口の岩棚 y-141 (x243..250)、`SpawnFromA1_D5` (245.5,-139.5)、天井に同じ緑の案内灯 `GuideLamp0/1` (244.5/248.6, -126)。D3→D5・D5→D3 をプレイで確認 | A1_D5 |
| (別エリア) 地下水路 | A1_D2 の配電室 D2w の西の床の紫の鉄扉 (SkillBlock ChargeRush `A1_Rush_D2w`, x139..141 y-123..-119) の奥 (x137..139) に「地下水路 (別エリア・準備中)」のヒントだけ (遷移なし)。地下水路のエリアを作る時はここに遷移と到着スポーンを置く | A1_D2 |
| A1_MB2 | ✅ スポーン `FromA1_C1`。A1_C1 の `FromA1_MB2` へ行く TransportInteractable (搬出エレベーター、必要フラグ `A1_ElevatorDeep`)。`A1_ElevatorDeep` はスピーカー③ (`A1_Speaker3`) 破壊で立てる (FlagActivator._setFlagsWhenSatisfied)。**✅ 配置済み**: S3 の `Speaker3Flag` (`_flags=["A1_Speaker3"]` → `["A1_ElevatorDeep"]`)、乗り場の鉄骨 (y-189) の `ElevatorDeep` (412.5,-187.5) → A1_C1 / FromA1_MB2 (要 `A1_ElevatorDeep`)、到着 `SpawnFromA1_C1` (410.5,-187.5)。MB2→C1 (301.5,-56.06 接地)・C1→MB2 (410.5,-188.06 接地) をプレイで確認 | A1_C1 |
| A1_AB | ✅ スポーン `FromA1_C1` (自動: 入口の踊り場 (320.5,-52.5), 床 y-54)。C1 の大扉 (スピーカー①②③) → AB → C1 をプレイで確認 | A1_C1 の大扉 |
| A1_MB1 | ✅ `A1_ZiplineReady` をスピーカー② (`A1_Speaker2`) 破壊で立てる。ジップラインは A1_U1 の上端のスポーンへ (一方通行): TransportInteractable(`_sceneName="A1_U1"`, `_entranceId="FromA1_MB1"`, `_requiredFlags=["A1_ZiplineReady"]`, `_promptText="滑り降りる"`)。**A1_U1 側の到着スポーン `FromA1_MB1` は配置済み ✅** (257.5,4.5, 上端の乗り場の左、ジップラインの終点の絵の横) 。**✅ MB1 側も配置済み (A1_MB1)**: S2 の `Speaker2Flag` (FlagActivator `_flags=["A1_Speaker2"]` → `_setFlagsWhenSatisfied=["A1_ZiplineReady"]`)、`StarZipline` (102.5,350.5) → A1_U1 / FromA1_MB1。プレイで到着 (257.5,3.9) を確認 | 設計 / A1_U1 |
| A1_W6 / A1_W1 | ✅ 非常用リフト: W6 のレバーで `A1_LiftWheel`、W6⇔W1 の TransportInteractable (両側、W1 側は `A1_LiftWheel` 必須)。**✅ W1 側は配置済み**: `EmergencyLift` (6,1.5) → `A1_W6` / `FromA1_W1` (要 `A1_LiftWheel`)、到着スポーン `FromA1_W6` (10.5,1.5)。A1_U4 の地図売りが「てっぺんの機械室の非常用リフトのレバー」と案内している 。**✅ W6 側も配置済み**: レバー `LiftLever` (41.6,250, 踊り場 y249)、`EmergencyLift` (33,250.5) → A1_W1 / FromA1_W6 (要 `A1_LiftWheel`)。W6→W1・W1→W6 をプレイで確認 | 設計 / A1_U4 |
| A1_FP / A1_U1 | ✅ 柵: FP 側のレバー `FenceLever` (LeverSwitch `A1_FenceOpen`, (317,4)) で開く。FP 側にも同じ位置 (x304) に柵 FlagDoor(`A1_FenceOpen`) があり、FP の `ExitToA1_U1` は柵の外側 (x300..302.5) だけに縮めた (閉じている間は触れない)。✅ U1 (U2e) 側の柵は FlagDoor(`A1_FenceOpen`) (x303..305, 当たり高さ 24)。レバー → 西へ歩いて U1 (`FromA1_FP` 300.5,3.9) → 柵の奥の `ExitToA1_FP` → FP をプレイで確認 | 設計 |
| A1_FP | ✅ スポーン **`FromA1_U1`** (308.5,4.5 = 柵の内側、小噴水の前)。✅ 柵の脇に U1 の予告と同じ小噴水 (fountain.png ×1, (309.2,6.4)) と街灯 (313.8)。大噴水 (×2) は大階段の出口 x349.5。元の要件: スポーン **`FromA1_U1`** (柵が開いた後の U2e → FP の戻り道。A1_U1 の柵の奥 x306..309 y3..15 に `ExitToA1_FP` を置いた)。FP の左端の通路 (x303..306 y3..15) の床は y3。A1_U1 の柵越しの予告として噴水 (`StageAsset/prop/fountain.png`, 中心 ≈ (309.2, 6.4)) と街灯を置いたので、FP 側も噴水を左寄り (x≈309〜) に置くと見た目がつながる | A1_U1 |
| A1_W1 | ✅ スポーン `FromA1_U4` (自動) と A1_U4 の `FromA1_W1` へ戻る出口 (自動)。A1_U4 は西の境の壁 (x51..57) の y27 より上を空にしている (W1 側も同じ高さにそろえる)。観覧車プレハブの乗り場 (BaseStation, x44..64 y0..7) が U4 の西端に掛かって見えている | A1_U4 |
| A1_W* (観覧車が見えるシーン) | ✅ (W1〜W4 で使用) 共通の観覧車プレハブ (PlaceFerrisWheel) に加え、地上近くでは本物の外周 (下端 y162) がカメラ (縦 11) に入らないので、A1_U4 と同じ頭上の外周の帯 (`ParallaxProp("ferris/far_wheel_silhouette_arc.png", ...)`) を使うと見た目がそろう | A1_U4 |
| A1_W6 | ✅ スポーン **`FromA1_W1`** (非常用リフトの上の乗り口の前) と、A1_W1 の `FromA1_W6` へ戻る TransportInteractable (`_sceneName="A1_W1"`, `_entranceId="FromA1_W6"`, `_requiredFlags=["A1_LiftWheel"]`)。レバー (LeverSwitch `_progressFlag="A1_LiftWheel"`) も W6 。配置: `SpawnFromA1_W1` (38.5,250.5) | A1_W1 |
| A1_W5 | ✅ スポーン `FromA1_W4` (自動: 下の口 x66..78 y174..180 の脇 (79.5,181.5)。下に床が要る)。W5 の下の口から落ちると A1_W4 の `SpawnFromA1_W5` (72,172.5) = 外周の上の点検路 y171 (x64..76) に着地する (W4 側は配置済み ✅)。W4 は屋外で空が開いている (W5 側の空・観覧車の見え方を合わせる)。W4 の横風は y178 まで 。W5 側は床の口を鉄骨の格子 (y180, ↓+ジャンプで降りる) で塞いだので、W5 の中で落ちても W4 には戻らない。※(解消 polish A) W4 の上の点検路を x61..78 に延ばし、追い風を x77..85 に縮めて風よけ (`WindScreen`) の西を凪にしたので、`SpawnFromA1_W5` に着いて立ち止まっても押し出されない | A1_W4 |
| A1_W2 / A1_W1 | ✅ W2 の床の穴 x15..24 → W1 の `SpawnFromA1_W2` (19.5,28.5) = 穴の真下の鉄骨 y27。W1 の上の口へ跳ぶと W2 の `FromA1_W1` (27.5,34.5) | — |
| W 区画の見た目 | ✅ (W5〜MB1 も同じ道具で作成) W2/W3 は `SkyBackdrop()` + `PlateWallsAll()` + 観覧車を暗く (`PlaceFerrisWheel(true, false, new Color(0.5f,0.46f,0.6f))`)、足場は `Girder`、背景の骨組みは暗い `Truss`/`Brace`。W5〜W8 も同じ道具で揃えると見た目がつながる | A1_W2〜W4 |
| (別エリア) 星見台 | A1_MB1 のアリーナの真上 (x50..58 y366 の鉄骨 `StarDeck`、大ジャンプでだけ届く) に「星見台への梯子 (別エリア・準備中)」の目印 (ヒントと梯子の絵だけ、遷移なし)。星見台のエリアを作る時はここに遷移 (または TransportInteractable) を足し、到着スポーンを置く | A1_MB1 |
| A1_C1 | ✅ スピーカー② = `A1_Speaker2` (A1_MB1 の S2)。大扉の灯 2/3 はこのフラグ | A1_MB1 |
| A1_W5 / A1_W6 / A1_W8 / A1_MB1 | 縦の遷移の約束 (配置済み ✅): W5 の天井の口 → W6 `FromA1_W5` (58.5,241.5, 床の口の右) / W6 の床の格子を降りる → W5 `FromA1_W6` (51,233.5, 口の真下の鉄骨 y232) / W6 の天井の口 → W8 `FromA1_W6` (59.2,277.5, 右の梯子の脇) / W8 の底へ落ちる → W6 `FromA1_W8` (51,268.5, 天井の口の真下の鉄骨 y267) / W8 の天井の口 → MB1 `FromA1_W8` (60,344.5, 外周の上の鉄骨 y343) / MB1 の足場から落ちる (y326..338 の全幅のゾーン) → W8 `FromA1_MB1` (50,340.5, 最上段 y339) | — |
| (別エリア) 奈落の倉庫 | A1_D4 の D4s (油だまりの底) の東の床の橙の床 (SkillBlock GroundSlam `A1_Slam_D4s_2`, x314..317 y-219..-216) の下は行き止まり (x314..317 y-222..-219) で「奈落の倉庫 (別エリア・準備中)」のヒントだけ (遷移なし)。奈落の倉庫のエリアを作る時はここに遷移と到着スポーンを置く | A1_D4 |
| A1_D4 / A1_D3 / A1_MB2 | ✅ 横の遷移はすべて床 y-198 の通路: D3 の東の底 x267..270 ⇔ D4 `FromA1_D3` (272.5,-196.5) / D4 → D3 `FromA1_D4` (264.5,-196.5) / D4 の東 x375..378 ⇔ MB2 `FromA1_D4` (380.5,-196.5) / MB2 → D4 `FromA1_MB2` (372.5,-196.5)。MB2 の西の口 (x376..378) は戦闘中ゲートで閉じる | — |

| A1_AB / A1_FP | ✅ 縦の遷移: AB の ST 最上段 (L7 y-5) から跳ぶ → FP `FromA1_AB` (355.5,1.5, 噴水の池の底 y0)。FP の排水口の格子 (x345..354 y0, ↓+ジャンプ) から降りる → AB `FromA1_FP` (353.5,-3.5, L7 の上) | — |
| (別エリア) エリア2 | A1_FP の FP2 東端の正門 (hall/park_gate, 扉は閉じたまま、当たり `GateBody` x431..446 y9..20) に「エリア2へ (準備中)」の看板とヒントだけ (遷移なし)。エリア2 を作る時は正門の扉 (x436..441) を開く仕掛けと遷移 (右端 x447 y15 付近) を足す | A1_FP |

仮置きのご褒美: ハートピン・糸巻きはゲーム側に仕組みがないため KeyItemPickup (フラグ `A1_HeartPin<n>` / `A1_Spool<n>`) で置く。拠点 A1_H の屋根裏 Hs1 (大ジャンプ) は 2026-10-03 から糸の大束 ×150 (ThreadPickup, `A1_Pickup_Hs1_1`。新技「回転斬り」の秘伝書 `A1_Skill_SpinSlash` は収集品カタログからも削除)。
ハートピン②は A1_W2 の W2s (`A1_HeartPin2`, `A1_Pickup_W2s_1`)。日誌その5は A1_W4 の W4r (`A1_Lore_Journal5`, `A1_Pickup_W4r_1`)。ハートピン①は A1_U3 の U3s (`A1_HeartPin1`, `A1_Pickup_U3s_1`)。ボビン爆弾は ItemDefinition (`Assets/Items/BobbinBomb.asset`) があったので ConsumableItemPickup ×3 (`A1_Pickup_U2a_1`)。
糸巻き②は A1_D5 の D6 (KeyItemPickup `A1_Spool2`, `A1_Pickup_D6_1`)。道具「糸玉」×3 は A1_D2 の D2w (ConsumableItemPickup `Assets/Items/ThreadBall.asset`, `A1_Pickup_D2w_1`)、日誌その3 (`A1_Lore_Journal3`, `A1_Pickup_D2w_2`)。D5 の下の通路の窪みに糸×15 (`A1_Pickup_D5_1`)。
道具「針山」は A1_W6 の W7 (ConsumableItemPickup `Assets/Items/PinCushion.asset` ×3, `A1_Pickup_W7_1`)。糸の大束×100 は A1_W5 の W5g (`A1_Pickup_W5g_1`)。
糸巻き③は A1_D3 の D7 (ネズミの巣のクリアで出る宝箱、KeyItemPickup `A1_Spool3`, `A1_Pickup_D7_1`)。糸の大束×60 は A1_D3 の D3a (二段ジャンプ、`A1_Pickup_D3a_1`)。ハートピン④は A1_D4 の D4s (`A1_HeartPin4`, `A1_Pickup_D4s_1`)。
日誌その4 (噴水の碑文) は A1_FP の大噴水の前 (KeyItemPickup `A1_Lore_Journal4`, `A1_Pickup_FP_1`, 絵 hall/inscription_plaque)。糸巻き①は A1_FP の FP2 の正門のアーチの上 (`A1_Spool1`, `A1_Pickup_FP2_1`, 二段ジャンプ)。**赤ハサミ (二段ジャンプ+滑空) = 最初のハサミ** は 2026-10-03 から拠点 A1_H の鍛冶師が渡す (Blacksmith `_requiredAnyFlags=["A1_MidBossUpper","A1_MidBossLower"]` = 中ボスどちらかの撃破後に話しかける。それまでは「地上か地下の中ボスを倒して来い」の案内 3 行)。A1_AB のエリアボス撃破後のご褒美は糸の大束 ×200 (ThreadPickup, `A1_Pickup_AB_1` を引き継ぎ)。

## 二段ジャンプ (赤ハサミ) の前提 (2026-10-03〜)
| 場所 | 前提 | 備考 |
|---|---|---|
| A1_W5 W5g / A1_D3 D3a / A1_FP FP2 の糸巻き① | 二段ジャンプ (寄り道のご褒美) | 中ボスどちらかを倒し、拠点の鍛冶師に話してから戻ってくる場所 |
| A1_AB ST (噴水への大階段) | 二段ジャンプで**だけ**登れる (段差 5) | AB に入るには大扉 (スピーカー①②③) = 中ボス 2 体とも撃破済み。鍛冶師に話していないと ST を登れないが、**AB → C1 → H へは二段ジャンプ無しで歩いて戻れる** (AB の入口の段 3・C1 は平らな床・C1→H も床 y-54。ボスのゲートは撃破で開く)。鍛冶師へ誘導する仕掛けは今は無い |
| 出口の見通し | 全シーンの出口は先 14 セルまで見た目で開いている (SceneBuildGuide §12)。当たりは `Grid/ExitStop` で元の位置 | 隣のシーンを作り直す時もビルダーの Carve が自動で作る |
