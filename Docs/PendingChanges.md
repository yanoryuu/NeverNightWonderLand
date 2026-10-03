# 保留中の変更 (エディタが空いたら反映する)

## 2026-10-03 ユーザー決定
1. **拠点の屋根裏 (A1_H の Hs1、大ジャンプで入る部屋) のご褒美を「新技『回転斬り』の秘伝書」から「糸の大束×150」に変更**。新技は想定していない。
   - `Docs/Area1/SceneScripts/build_A1_H.cs`: KeyItemPickup (`A1_Skill_SpinSlash`) を ThreadPickup ×150 (id `A1_Pickup_Hs1_1`) に置き換えて A1_H を作り直す (Validate 0 error)。
   - `Assets/Items/CollectionCatalog.asset`: 秘伝書の項目を削除 (収集品は ハートピン1〜4・糸巻き1〜3 の7つ)。
   - `Docs/Area1/area1_layout.json` の rewards と、`CrossSceneContracts.md` / `SceneBuildGuide.md` / 設計図 (artifact・Desktop/nevernight-map.html) の「新技」記述も更新。
2. UI 追加質問 (おすすめで回答待ち): 秘伝書の絵 → 項目ごと削除になったので不要。
3. UI 4回目の決定 (Docs/UI/UIAssignment.md「4回目の決定」) の反映: 仮素材 `Assets/LocalData/UI/_temp/` (picker_row/selected/disabled/header, icon_<id>/_gray) を装備品タブの付け替え一覧に組み込む、0個のアイテムも灰色+アイコンで表示、同じアイテムは2スロットに入れない (別スロットの物を選んだら中身を入れ替える)、入手通知の帯を2行対応・2行時3.5秒。
