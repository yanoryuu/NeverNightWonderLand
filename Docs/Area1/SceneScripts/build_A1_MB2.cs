using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_MB2 「歯車の重馬」(MB2 x378..438 y-207..-168) + 「第3スピーカー」(S3 x441..462 y-201..-180) + 搬出エレベーター (E1, 天井の縦坑 x408..417)
// 西 = 整備員の休憩所 A1_D4 (x375..378, 床 y-198) / 上 = 搬出エレベーター → A1_C1 の床下 (TransportInteractable, 要 A1_ElevatorDeep)
// MB2: 床 y-198 の真ん中に、床に半分埋まった大歯車 (中心 (408,-209), 縁 R13 = 床から 2 盛り上がったこぶ)。
//      縁は歯車と一緒に回る踏み板 20 枚 (1 つの Kinematic の剛体の子の当たり = 傾きながら東へ流れる足場)。
//      中ボス = 地上型の仮ボス (Enemy ×2 + Heavy, 真鍮色、腹に歯車) がこぶの上を行き来する。足場の傾きと流れを読みながら戦う。
//      回避用に左右の配管 (y-195 / -192) と、こぶの上の搬出エレベーターの乗り場の鉄骨 (y-189)。
//      BossArena A1_MidBossLower: 入ると西の口が閉じる。撃破で東の扉 (FlagDoor) が上がって S3 へ。
// S3: 床 y-195 の小部屋。スピーカー③ (A1_Speaker3) → FlagActivator が A1_ElevatorDeep を立てる → 縦坑のかごが乗り場まで下りてくる。
// E1: 乗り場の TransportInteractable `ElevatorDeep` → A1_C1 / FromA1_C1 (要 A1_ElevatorDeep)。C1 からの到着 `SpawnFromA1_C1` (乗り場)。
// 先に別の呼び出しで OpenOrCreate("A1_MB2") を実行しておくこと。
public static class BuildMB2
{
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Red = new Color(1f, 0.32f, 0.3f);
    static readonly Color Green = new Color(0.45f, 1f, 0.6f);

    public static readonly Vector2 GearC = new Vector2(408f, -209f);
    public const float GearR = 13f;
    public const float GearOmega = -7f;   // 時計回り (上面は東へ流れる)
    const int Treads = 20;

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    // 奥でゆっくり回る歯車 (当たり無し)
    static void BgGear(float x, float y, float scale, float speed, float k = 0.3f, int order = -46)
    {
        var g = NewObject("BgGear", new Vector2(x, y), LayerDefault, "Props");
        g.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(g, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", speed));
        var art = new GameObject("Art");
        art.transform.SetParent(g.transform, false);
        art.transform.localScale = Vector3.one * scale;
        AddSprite(art, Art("platforms/gear_platform.png"), order, null, new Color(k, k * 0.85f, k * 1.25f));
    }

    static void HangLamp(float x, float ceilingY, float scale = 0.6f, float glow = 1f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
        Glow(x, ceilingY - scale * 0.6f, 1.6f, new Color(1f, 0.72f, 0.45f, 0.28f * glow), -7);
    }

    static void StatusLamp(string name, float x, float y, string flag)
    {
        var red = Prop("props/boss_door_lamp_on.png", x, y, 0.45f, OrderProp, false, Red);
        var green = Prop("props/boss_door_lamp_on.png", x, y, 0.45f, OrderProp, false, Green);
        var gR = Glow(x, y + 0.2f, 0.9f, new Color(1f, 0.3f, 0.3f, 0.45f), -6);
        var gG = Glow(x, y + 0.2f, 0.9f, new Color(0.4f, 1f, 0.55f, 0.45f), -6);
        var act = NewObject(name, new Vector2(x, y + 0.5f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { flag }), ("_showWhenSet", new Object[] { green, gG }),
            ("_hideWhenSet", new Object[] { red, gR }));
    }

    /// <summary>
    /// 床の大歯車: 1 つの Kinematic 剛体 (MovingPlatform Orbit 半径 0 + _spinSpeed) の子に、歯車の絵 (order -12 = 床の岩肌の裏に半分隠れる) と
    /// 縁の踏み板 (接線方向の BoxCollider2D, layer 10, 絵は order -11) を並べる。踏み板は歯車と一緒に回って傾く。
    /// </summary>
    static GameObject FloorGear()
    {
        var root = NewObject("FloorGear", GearC, LayerGround, "Gimmicks");
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(root, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", GearOmega));
        var art = new GameObject("GearArt");
        art.transform.SetParent(root.transform, false);
        art.transform.localScale = Vector3.one * (GearR * 2f / 4f) * 0.98f;
        AddSprite(art, Art("platforms/gear_platform.png"), OrderBackfill - 2, null, new Color(0.55f, 0.5f, 0.66f));
        var arc = 2f * Mathf.PI * GearR / Treads;
        for (var k = 0; k < Treads; k++)
        {
            var a = k * 360f / Treads + 90f;
            var t = new GameObject("Tread" + k) { layer = LayerGround };
            t.transform.SetParent(root.transform, false);
            t.transform.localPosition = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * GearR;
            t.transform.localRotation = Quaternion.Euler(0, 0, a - 90f);
            var bc = t.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(arc + 0.15f, 0.5f);
            bc.offset = new Vector2(0, -0.25f);
            // polish B: 踏み板の絵 = gear_tooth_tread (2×1 セル, pivot bottom, 上端の明るい縁 = 歩ける面)。縁 (絵の上から 8%) を当たりの上面 (y 0) に合わせ、
            // 幅は弧の長さいっぱい (隙間を描くと落ちそうに見えるので詰める)。踏み板と一緒に回る
            var ta = new GameObject("Art");
            ta.transform.SetParent(t.transform, false);
            const float treadH = 1.25f;
            ta.transform.localPosition = new Vector3(0, -treadH * 0.918f, 0);
            ta.transform.localScale = new Vector3((arc + 0.15f) / 2f, treadH, 1f);
            AddSprite(ta, Art("polish/gear_tooth_tread.png"), OrderBackfill - 1, null, k % 2 == 0 ? new Color(1f, 0.95f, 0.88f) : new Color(0.9f, 0.84f, 0.8f));
        }
        return root;
    }

    public static string Run()
    {
        var log = BuildBase("A1_MB2");
        RockDecor(0.45f);

        // ================= MB2 闘技場: 床 y-198、天井 y-176 (縦坑 x408..417 だけ上へ)
        Solid(378, -207, 438, -198);
        Solid(378, -176, 408, -168);
        Solid(417, -176, 438, -168);
        Solid(434, -198, 438, -195);                  // S3 の口 (床 y-195) への段

        FloorGear();
        // 回避用の配管と、こぶの上の乗り場 (鉄骨)
        PipePlatform(384, 389, -195, Rust);
        PipePlatform(391, 396, -192, Rust);
        PipePlatform(420, 425, -192, Rust);
        PipePlatform(427, 432, -195, Rust);
        Girder(398, 418, -189, new Color(0.62f, 0.55f, 0.72f), "ElevatorDeck");

        // ---- 中ボス (地上型: Enemy ×2 + Heavy)。入ると西の口が閉じる
        var arena = BossRoom(new RectInt(382, -198, 52, 22), "A1_MidBossLower", new Vector2(408f, -194f), false,
            new[] { new RectInt(376, -198, 2, 9) },
            "歯車の重馬を倒した! 奥の扉が開いた……",
            // 赤ハサミを未入手なら拠点の鍛冶屋への誘導の一文 (PendingChanges 11b, 呼びかけ調にしない)
            "ハサミがかすかに震えた。……拠点の鍛冶屋なら、何か分かるかもしれない", followUpUnlessRed: true);
        var boss = GameObject.Find("Boss_A1_MidBossLower");
        if (boss != null)
        {
            boss.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.62f);
            var bg = new GameObject("BellyGear");
            bg.transform.SetParent(boss.transform, false);
            bg.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            bg.transform.localScale = Vector3.one * 0.13f;
            AddSprite(bg, Art("platforms/gear_platform.png"), 6, null, new Color(1f, 0.85f, 0.6f));
        }

        // ---- 東の扉 (撃破で上へ) → S3
        var door = GateObject("S3Door", new RectInt(438, -195, 3, 9), -1);
        AddComp<FlagDoor>(door, ("_flagId", "A1_MidBossLower"), ("_slideDistance", 9.5f), ("_slideDuration", 1.2f));

        // ================= S3 第3スピーカー (床 y-195, 天井 y-184)
        Solid(441, -201, 462, -195);
        Solid(441, -184, 462, -180);
        var speaker = SpeakerAt(455f, -195f, "A1_Speaker3");
        Set(speaker.GetComponent<BreakableSpeaker>(), "_breakMessage",
            "スピーカー③を壊した! 地下の音楽が止んだ…… ゴウン、と搬出エレベーターが動き出した (3/3)");
        var broken = Prop("props/speaker_broken.png", 455f, -195f, 1f, OrderProp);
        broken.SetActive(false);
        var brokenAct = NewObject("SpeakerBrokenFlag", new Vector2(455f, -193f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(brokenAct, ("_flags", new[] { "A1_Speaker3" }), ("_showWhenSet", new Object[] { broken }));
        var elevAct = NewObject("Speaker3Flag", new Vector2(452f, -193f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(elevAct, ("_flags", new[] { "A1_Speaker3" }), ("_setFlagsWhenSatisfied", new[] { "A1_ElevatorDeep" }));
        // スピーカーから乗り場へ這う電線 (動力がつながっている見た目)
        PipeRun(441f, -185.2f, 455f, -185.2f, 0.35f, -6, new Color(0.35f, 0.3f, 0.4f));
        PipeRun(417f, -177.2f, 438f, -177.2f, 0.35f, -6, new Color(0.35f, 0.3f, 0.4f));
        HangLamp(448f, -184f, 0.6f);

        // ================= E1 搬出エレベーター (乗り場 = こぶの上の鉄骨 y-189, かご x412.5)
        var cabinDown = Prop("props/elevator_cabin.png", 412.5f, -186.75f, 1.5f, -4);
        var cabinUp = Prop("props/elevator_cabin.png", 412.5f, -169.5f, 1.5f, -4);
        var cableDown = PipeRun(412.5f, -184.5f, 412.5f, -160f, 0.12f, -5, new Color(0.45f, 0.42f, 0.5f));
        var flagCab = NewObject("ElevatorFlag", new Vector2(410f, -180f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(flagCab, ("_flags", new[] { "A1_ElevatorDeep" }), ("_showWhenSet", new Object[] { cabinDown, cableDown }),
            ("_hideWhenSet", new Object[] { cabinUp }));
        TransportAt(new Vector2(412.5f, -187.5f), new Vector2(3f, 3f), "A1_C1", "FromA1_MB2",
            new[] { "A1_ElevatorDeep" }, "乗る", "搬出エレベーターのかごは縦坑の上で止まっている…… 奥のスピーカーが動力を食っているらしい",
            name: "ElevatorDeep");
        Spawn("FromA1_C1", new Vector2(410.5f, -187.5f));
        StatusLamp("ElevatorLamp", 416.4f, -186.6f, "A1_ElevatorDeep");
        // 縦坑のレール
        PipeRun(408.8f, -189f, 408.8f, -158f, 0.4f, -6, BgPipe);
        PipeRun(416.2f, -189f, 416.2f, -158f, 0.4f, -6, BgPipe);

        // ================= 飾り: 奥の大歯車・吊り灯・警告灯・配管
        BgGear(388f, -184f, 3.4f, 5f, 0.26f);
        BgGear(430f, -183f, 2.8f, -6f, 0.24f);
        BgGear(398.5f, -180f, 1.5f, -12f, 0.22f);
        BgGear(422f, -186.5f, 1.2f, 14f, 0.22f);
        foreach (var x in new[] { 386.5f, 399.5f, 425.5f })
            HangLamp(x, -176f, 0.65f);
        foreach (var x in new[] { 381f, 435f })
            GuideLamp(x, -176f, x > 400 ? 0.5f : 0f, Red, true, 1.6f, "WarnLamp");
        PipeRun(378f, -177.2f, 408f, -177.2f, 0.8f, -6, BgPipe);
        PipeRun(379.2f, -198f, 379.2f, -176f, 0.7f, -7, Dim(BgPipe, 0.85f));
        PipeRun(436.8f, -195f, 436.8f, -176f, 0.7f, -7, Dim(BgPipe, 0.85f));

        // ================= 暗さと質感
        TintRock(360, -225, 480, -150, new Color(0.62f, 0.58f, 0.72f));
        PlateWalls(441, -201, 462, -180);
        TintRock(441, -201, 462, -180, new Color(0.85f, 0.7f, 0.65f));

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
