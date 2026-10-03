# 保留中の変更 (エディタが空いたら反映する)

## 2026-10-03 ユーザー決定
1. ✅ (2026-10-03 反映済み: A1_H 作り直し・CollectionCatalog 7 項目・layout/ドキュメント/Desktop の設計図 html。artifact の再公開は未) **拠点の屋根裏 (A1_H の Hs1、大ジャンプで入る部屋) のご褒美を「新技『回転斬り』の秘伝書」から「糸の大束×150」に変更**。新技は想定していない。
   - `Docs/Area1/SceneScripts/build_A1_H.cs`: KeyItemPickup (`A1_Skill_SpinSlash`) を ThreadPickup ×150 (id `A1_Pickup_Hs1_1`) に置き換えて A1_H を作り直す (Validate 0 error)。
   - `Assets/Items/CollectionCatalog.asset`: 秘伝書の項目を削除 (収集品は ハートピン1〜4・糸巻き1〜3 の7つ)。
   - `Docs/Area1/area1_layout.json` の rewards と、`CrossSceneContracts.md` / `SceneBuildGuide.md` / 設計図 (artifact・Desktop/nevernight-map.html) の「新技」記述も更新。
2. UI 追加質問 (おすすめで回答待ち): 秘伝書の絵 → 項目ごと削除になったので不要。
4. ✅ (2026-10-03 反映済み: Blacksmith に `_requiredAnyFlags` (OR)・`_lockedLines`・`_grantMessage` を後方互換で追加、A1_H の鍛冶師が赤を渡す、A1_AB は糸の大束×200 (`A1_Pickup_AB_1`)。セリフは今は Blacksmith のフィールド = 9 の Utage 化で置き換える) **二段ジャンプ (赤ハサミ) の入手を変更** (2026-10-03 ユーザー決定): 最初に解放されるハサミは赤 (二段ジャンプ)。チュートリアルのボス (改札の番人) を倒した後、エリア1 の中ボス2体 (A1_MidBossUpper / A1_MidBossLower) の**どちらか**を倒したら獲得。**案内と受け渡しは拠点の鍛冶屋**: ボス1後に拠点で話すと「地上か地下の中ボスどちらかを倒したら来い、ハサミを鍛えてやる」、どちらかを倒して話しかけると赤ハサミを渡す。エリアボス (A1_AB) の撃破報酬は赤ハサミをやめて**糸の大束×200** (ハートピンは4つで区切りのため糸にした)。
5. ✅ (2026-10-03 反映済み: ビルダーの「出口の見通し」 = SceneBuildGuide §12、27 シーン作り直し・Validate 全 0 error、before/after は Assets/Screenshots/Area1/sheets/exits_sheet_*) **ステージの出入り口を開ける** (ユーザー指示「出入り口はブロックで埋めずに開けて」): シーン間の遷移の開口がブロック (岩・Backfill・地形タイル) で埋まって分かりにくい → 全27シーンの出入り口を見た目でも開いた状態にする。
6. ✅ (2026-10-03 反映済み: 回転木馬の Prop を撤去、空いた床に荷物の木箱 3 + 床置きの灯、奥の壁は自動の壁飾り) **拠点 (A1_H) の大きなメリーゴーランドを削除** (ユーザー指示)。
7. **部屋の名前は出さない。エリアに初めて入った時だけ「鳴りやまぬ遊園地」を1回出す** (ユーザー決定: 単位はエリア全体)。表示済みかはセーブに残す。
8. **敵が少ないので少し増やす** (ユーザー指示)。各部屋の設計意図に合わせて増やす (チュートリアルは控えめ、闘技場・ボス部屋は対象外)。
9. **会話は Utage を使う** (ユーザー指示「すでに画面はできているのでそれを使う」): NpcTalk (迷子の人形・地図売り2人) と鍛冶屋の会話 (二段ジャンプの案内・受け渡し・鍛冶強化) を DialogueService.Play(ラベル) に切り替える (既存の NpcTalkPoint / Docs/DialogueSystem.md を流用)。セリフは **Claude が Assets/Dialogue/Scenarios/Dialogue.xls に書き込む** (ユーザー決定。必ずバックアップ→書き込み→読み戻し検証 (Text 列=I列、Arg2 に入れない)→NeverNight/会話シナリオを再インポート)。
10. **ボットの確認事項 (おすすめで決定)**: 区間失敗時は次区間の入口へワープして継続 (現状維持)、迷いの閾値は確信度0.5未満 (現状維持)、**費用上限: 1回の実行あたり Jev 呼び出し 2,000 回で停止** (追加)、「タイトルからプレイ」は現状の解釈で OK。
3. UI 4回目の決定 (Docs/UI/UIAssignment.md「4回目の決定」) の反映: 仮素材 `Assets/LocalData/UI/_temp/` (picker_row/selected/disabled/header, icon_<id>/_gray) を装備品タブの付け替え一覧に組み込む、0個のアイテムも灰色+アイコンで表示、同じアイテムは2スロットに入れない (別スロットの物を選んだら中身を入れ替える)、入手通知の帯を2行対応・2行時3.5秒。
