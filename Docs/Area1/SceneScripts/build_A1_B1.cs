using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_B1 「改札の番人」(B1 ボス部屋) + 「第1スピーカー室」(S1)
// 先に別の呼び出しで OpenOrCreate("A1_B1") を実行しておくこと。
public static class BuildB1
{
    // 奥の壁に半分埋まった大歯車 (背景の飾り。岩 (0) の裏、暗く沈める)
    static void BgGear(float x, float y, float scale, float rot = 0f)
    {
        var g = Prop("platforms/gear_platform.png", x, y, scale, -7, false, new Color(0.3f, 0.26f, 0.4f));
        g.transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    // 天井から吊った灯
    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_B1");

        // ================= B1 (x165..207, y-63..-30): 平らで広い四角い部屋。床 y-57 (左の通路 y-57..-48 の下端)
        Ground(165, 207, -57, 6);
        Solid(204, -57, 207, -54);      // 右奥の段 (S1 への通路 y-54..-45 の床)

        // 背景: 閉ざされた改札の列 (奥の飾り、当たり無し)
        var gateTint = new Color(0.42f, 0.36f, 0.5f);
        for (var x = 174f; x <= 198f; x += 6f)
            Prop("props/fence_gate.png", x, -57f, 0.9f, -6, false, gateTint);
        foreach (var x in new[] { 171f, 186f, 201f })
            Prop("props/boss_door_lamp_on.png", x, -37.5f, 0.7f, OrderProp, false, new Color(1f, 0.6f, 0.55f));

        // ボス: 改札の番人 (地上型の仮ボス = Enemy ×2 + Heavy)。入ると左の扉が閉まる
        BossRoom(new RectInt(168, -57, 35, 24), "A1_Boss1", new Vector2(192f, -55f), false,
            new[] { new RectInt(165, -57, 2, 9) },
            "改札の番人を倒した! 奥のスピーカー室の扉が開いた",
            threadDrop: 20);   // ボスの糸 20 本 → 12 粒 (PendingChanges 18。Heavy の定数は普通の敵と共用なので上書き)

        // 撃破でスピーカー室への扉 (シャッター) が開く
        var door = GateObject("S1Door", new RectInt(207, -54, 3, 9), -1); // 岩 (0) の裏へスライドして消える
        AddComp<FlagDoor>(door, ("_flagId", "A1_Boss1"), ("_slideDistance", 9.5f), ("_slideDuration", 1.2f));

        // ================= S1 (x210..228, y-57..-36): 第1スピーカー室。床 y-54
        Ground(210, 228, -54);
        // 壁沿いの棚を登ると、スピーカーの据わった高い渡り廊下 (下は通り抜けられる)
        OneWay(211, 215, -51);
        OneWay(216, 220, -48);
        OneWay(213, 226, -45);
        var speaker = SpeakerAt(221.5f, -45f, "A1_Speaker1");
        Set(speaker.GetComponent<BreakableSpeaker>(), "_breakMessage",
            "スピーカー①を壊した! 音楽の一部が止んだ…… 拠点の奥の大扉に灯がひとつ点いた (1/3)");
        var broken = Prop("props/speaker_broken.png", 221.5f, -45f, 1f, OrderProp);
        broken.SetActive(false);
        var act = NewObject("SpeakerBrokenFlag", new Vector2(221.5f, -43f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { "A1_Speaker1" }), ("_showWhenSet", new Object[] { broken }));
        // 渡り廊下の支柱 (飾り)
        var post = NewObject("CatwalkPost", new Vector2(224.5f, -49.5f), LayerDefault, "Props");
        AddSprite(post, Art("tiles/tile_ladder.png"), OrderProp, new Vector2(1f, 9f), new Color(0.7f, 0.65f, 0.8f));

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // ---- 飾り (奥の大歯車・吊り灯)
        BgGear(165.5f, -36f, 3f, 8f); BgGear(207f, -33f, 2.4f, 20f); BgGear(186f, -30.5f, 2f);
        HangLamp(215f, -36f); HangLamp(224.5f, -36f);

        return log + Finish();
    }
}
