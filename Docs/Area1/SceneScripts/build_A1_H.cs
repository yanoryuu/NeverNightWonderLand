using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_H 「回転木馬の待合所」(H 拠点) + 「迷子センター」(H2) + 「屋根裏の回転灯」(Hs1, 大ジャンプ) + 「地下金庫」(Hs2, 落下攻撃)
// 十字路: 左 = 第1スピーカー室 (A1_B1) / 上 = メリーの吹き抜け (A1_U1) / 下 = 配管の縦穴 (A1_D1) / 右 = 灯の大扉の回廊 (A1_C1)
// 先に別の呼び出しで OpenOrCreate("A1_H") を実行しておくこと。
public static class BuildH
{
    // 奥の壁に半分埋まった大歯車 (背景の飾り。岩 (0) の裏、暗く沈める)
    static void BgGear(float x, float y, float scale, float rot = 0f)
    {
        // polish B: 大きく平らな歯車が手前の足場より目立っていた → さらに暗く沈め、ゆっくり回して「奥で動いている機械」に (向きは交互)
        var g = Prop("platforms/gear_platform.png", x, y, scale, -7, false, new Color(0.22f, 0.19f, 0.3f));
        g.transform.rotation = Quaternion.Euler(0, 0, rot);
        AddComp<WheelSpin>(g, ("_speed", (Mathf.RoundToInt(x + y) % 2 == 0 ? 1f : -1f) * 6f / scale));
    }

    // 天井から吊った灯
    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_H");
        // ショップ画面はステージに置かない (UI シーン ShopUI の ShopView を ShopKeeper が探して開く)

        // ================= H (x231..288, y-60..-24): 床 y-54 (左右の出口 y-54..-45 の下端)
        Ground(231, 288, -54, 6);

        // ---- 下: 地下への大穴 (x240..249 → A1_D1)。すり抜けの格子でふさぎ、↓+ジャンプで降りる
        Clear(240, -60, 249, -54);
        OneWay(240, 249, -54);
        MoveSpawn("A1_D1", 251.5f, -52.5f);

        // ---- 右下: 橙の床 (落下攻撃) → 地下金庫 Hs2
        SkillBlock(276, -57, 282, -54, PlayerSkill.GroundSlam, "A1_Slam_Hs2_1");
        Clear(276, -60, 282, -57);

        // ---- 左の登り: 衣装棚と壁の出っ張りを交互に → 迷子センター (天井の穴 x231..237)
        OneWay(236, 241, -51);
        OneWay(242, 247, -48);
        OneWay(236, 241, -45);
        Solid(231, -44, 235, -42);          // 左のバルコニー
        OneWay(236, 241, -39);
        Solid(231, -37, 235, -36);
        OneWay(236, 271, -33);              // 木馬の上をまたぐ渡り廊下
        Solid(231, -31, 235, -30);
        OneWay(236, 241, -27);
        OneWay(231, 237, -24);
        OneWay(231, 237, -21);              // 穴のふた = 迷子センターの床

        // ---- 中央: 渡り廊下から吹き抜け (x255..267 → A1_U1) へ
        OneWay(252, 258, -30);
        OneWay(259, 266, -27);              // 上 (A1_U1) から降りてきた時の着地点

        // ---- 右の登り: キャットウォーク (y-42) の上に空色の天井 (大ジャンプ) → 屋根裏 Hs1
        OneWay(278, 283, -51);
        OneWay(272, 277, -48);
        OneWay(278, 283, -45);
        OneWay(272, 288, -42);
        SkillBlock(276, -24, 285, -22, PlayerSkill.SuperJump, "A1_Super_Hs1");
        OneWay(276, 285, -21);              // 屋根裏の床 (突き抜けた後の着地点)

        // ---- 拠点の設備 (床 y-54)
        Prop("props/carousel.png", 257f, -54f, 1.8f, -4);                             // 回転木馬 (飾り)
        SavePointAt(268.3f, -54f);
        var stall = NewObject("ShopKeeper", new Vector2(273.4f, -54f), LayerInteractable, "Gimmicks");
        AddSprite(stall, Art("props/shop_stall.png"), OrderProp);
        Box(stall, new Vector2(3.6f, 3f), true, new Vector2(0, 1.5f));
        AddComp<ShopKeeper>(stall, ("_promptOffset", new Vector3(0, 4.4f, 0)));
        var smith = NewObject("Blacksmith", new Vector2(237.6f, -54f), LayerInteractable, "Gimmicks");
        AddSprite(smith, Art("props/anvil_smith.png"), OrderProp);
        Box(smith, new Vector2(2.4f, 2.4f), true, new Vector2(0, 1.2f));
        AddComp<Blacksmith>(smith, ("_grantsUpgrade", false), ("_smithName", "木馬の鍛冶師"),
            ("_forgeId", "A1_Hub"), ("_forgeCost", 30), ("_promptOffset", new Vector3(0, 2.6f, 0)));

        // 灯り (飾り)
        var warm = new Color(1f, 0.85f, 0.6f);
        foreach (var x in new[] { 270.5f, 286f })
            Prop("props/boss_door_lamp_on.png", x, -53.7f, 0.6f, OrderProp, false, warm);

        // ================= H2 (x210..237, y-21..-6): 迷子センター。床 y-21
        NpcAt(221f, -21f, "npc/npc_lost_doll.png", "迷子の人形", new[]
        {
            "……あ、おきゃくさん? ここは迷子センター。わたしも、ずっとお迎えを待ってるの。",
            "上の吹き抜けをのぼると、夜の遊園地に出られるよ。おおきな観覧車のほうで、音楽がいちばんうるさく鳴ってるの。",
            "床の大穴をおりると、地下の機械室。歯車がぐるぐる回る、そのずっと奥からも音がするんだって。",
            "迷ったら地図売りさんをさがしてね。観覧車の足元と、地下の蒸気の迷路にいるよ。",
            "これ、お迎えが来たら渡そうと思ってた糸。あげる。……気をつけてね。",
        }, new[]
        {
            "上は観覧車、下は歯車。地図売りさんは観覧車の足元と、蒸気の迷路にいるよ。",
        }, "A1_Npc_Doll", 50, flipX: true);
        KeyItemAt(213.5f, -21f, "日誌「閉園しなかった夜」その2", "A1_Lore_Journal2", "A1_Pickup_H2_1");
        OneWay(214, 219, -17);              // 棚 (飾り兼足場)
        OneWay(226, 231, -14);
        Prop("props/boss_door_lamp_on.png", 228.5f, -13.7f, 0.6f, OrderProp, false, warm);
        Prop("props/boss_door_lamp_on.png", 216.5f, -16.7f, 0.6f, OrderProp, false, warm);

        // ================= Hs1 (x273..291, y-21..-12): 屋根裏の回転灯。新技「回転斬り」
        KeyItemAt(288.5f, -21f, "新技「回転斬り」の秘伝書", "A1_Skill_SpinSlash", "A1_Pickup_Hs1_1");
        var beacon = Prop("props/boss_door_lamp_on.png", 281f, -13.2f, 1.0f, OrderProp, false, new Color(1f, 0.5f, 0.45f));
        beacon.transform.rotation = Quaternion.Euler(0, 0, 180f);   // 天井から吊った回転灯

        // ================= Hs2 (x270..288, y-75..-63): 地下金庫。ハートピン3
        Ground(270, 288, -72);
        // 金庫から拠点の床へ戻る棚 (縦穴 x276..282 の中をジグザグ)
        OneWay(271, 276, -69);
        OneWay(277, 281, -66);
        OneWay(276, 279, -63);
        OneWay(279, 282, -60);
        OneWay(276, 279, -57);
        var pin = KeyItemAt(285.5f, -70.3f, "ハートピン3", "A1_HeartPin3", "A1_Pickup_Hs2_1", "props/heart_pin.png", "拾う");
        pin.GetComponent<BoxCollider2D>().offset = Vector2.zero;
        Solid(284, -72, 287, -71);          // 金庫の台
        Prop("props/boss_door_lamp_on.png", 272.5f, -71.7f, 0.6f, OrderProp, false, warm);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // ================= 飾り (奥の大歯車・吊り灯)
        BgGear(231.5f, -50f, 2.2f, 10f); BgGear(288f, -36f, 2.6f, 22f); BgGear(250f, -24.5f, 2f);
        BgGear(274f, -24.5f, 1.6f, 30f); BgGear(210.5f, -10f, 1.8f); BgGear(288f, -66f, 1.6f, 12f);
        foreach (var x in new[] { 246f, 268f }) HangLamp(x, -33.8f, 0.6f);   // 渡り廊下から吊る
        HangLamp(283f, -42.8f, 0.55f);

        return log + Finish();
    }
}
