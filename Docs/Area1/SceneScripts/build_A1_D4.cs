using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_D4 「ピストンの間」(D4 x270..348 y-204..-186) + 「油だまりの底」(D4s x300..321 y-219..-207) + 「整備員の休憩所」(D8 x351..375 y-204..-183)
// 西 = 歯車の胃袋 A1_D3 (x267..270, 床 y-198) / 東 = 歯車の重馬 A1_MB2 (x375..378, 床 y-198)
// D4: 床 y-198、天井 y-191 の長い廊下。天井のピストン (C) と床のピストン (F, 格子の下の溝から突き上げる) を 5.5 ごとに交互に 9 本。
//     周期はどれも 4 秒 (待ち 1.6 → 予兆 0.6 (震え + 赤) → 伸び 0.2 → 伸びたまま 0.8 → 縮み 0.8)。C と F は半周期ずれ (C が閉じている時に F が開く)。
//     ピストンの間の床 (幅 2.75) は安全地帯 = 1 本ずつ抜けて次の開きを待つ。伸びている最中だけ押しつぶしの当たり (HazardZone)。
//     中ほど (x304..314) は天井が高く、床に橙の点検口 (落下攻撃 A1_Slam_D4s_1) → D4s。
// D4s: 油だまり (WaterZone で遅い) の底にハートピン4。東の床にもう 1 枚の橙の床 (A1_Slam_D4s_2) → 奈落の倉庫 (別エリア・準備中) のヒントだけ。
//      帰りは西の岩棚と配管の足場を 3 ずつ登って点検口へ。
// D8: 中ボス前のセーブ (ベンチ) と、壁に貼られた中ボス「歯車の重馬」の設計図 (青図: 木馬 + 腹の歯車 + 床の大歯車)。
// 先に別の呼び出しで OpenOrCreate("A1_D4") を実行しておくこと。
public static class BuildD4
{
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Red = new Color(1f, 0.32f, 0.3f);
    static readonly Color Guide = new Color(0.4f, 1f, 0.78f);

    const float Ceil = -191f, Floor = -198f;
    // 周期 (全ピストン共通、合計 4.0 秒)
    const float Wait = 1.6f, Tele = 0.6f, Ext = 0.2f, Hold = 0.8f, Ret = 0.8f;

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    /// <summary>
    /// ピストン。ceiling=true: 天井から下へ叩きつける (ルート = 縮んだ時のヘッドの下面)。
    /// false: 床の格子の下の溝から上へ突き上げる (ルート = 縮んだ時のヘッドの上面, 格子の 0.7 下)。
    /// 絵は order -12 = 岩肌 (Backfill -10) の裏 → 岩の中の竿は隠れ、部屋の中に出た部分だけ見える。
    /// </summary>
    static GameObject Piston(string name, float x, bool ceiling, float delay)
    {
        var rootY = ceiling ? Ceil - 0.5f : Floor - 0.7f;
        var go = NewObject(name, new Vector2(x, rootY), LayerGround, "Gimmicks");
        var art = new GameObject("Sprite");
        art.transform.SetParent(go.transform, false);
        var sx = ceiling ? 1.25f : 1.5f;
        art.transform.localScale = new Vector3(sx, 2f, 1f);
        art.transform.localPosition = new Vector3(0, ceiling ? 4f : -4f, 0);
        var sr = AddSprite(art, Art("platforms/piston.png"), OrderBackfill - 2, null, new Color(0.92f, 0.88f, 0.95f));
        sr.flipY = !ceiling;
        var headW = sx * 2f - 0.2f;
        Box(go, new Vector2(headW, 1.5f), false, new Vector2(0, ceiling ? 0.75f : -0.75f));
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var crush = NewObject("Crush", new Vector2(x, rootY + (ceiling ? 0.12f : 0.15f)), LayerIgnoreRaycast, "Gimmicks");
        crush.transform.SetParent(go.transform, true);
        Box(crush, new Vector2(headW - 0.2f, ceiling ? 0.4f : 0.45f), true);
        var hz = AddComp<HazardZone>(crush, ("_damage", 1));
        AddComp<global::Piston>(go, ("_extendOffset", new Vector2(0, ceiling ? (Floor + 0.7f) - rootY : 5.2f)),
            ("_retractedWait", Wait), ("_telegraphTime", Tele), ("_extendDuration", Ext), ("_extendedWait", Hold),
            ("_retractDuration", Ret), ("_startDelay", delay), ("_crushHazard", hz), ("_shakeVisual", art.transform),
            ("_shakeAmplitude", 0.08f));
        if (ceiling)
        {
            // 天井の口金 (ピストンが出入りする鉄の枠)
            PipeRun(x - 1.9f, Ceil + 0.15f, x + 1.9f, Ceil + 0.15f, 0.55f, OrderProp, new Color(0.55f, 0.48f, 0.6f));
        }
        else
        {
            var x0 = Mathf.RoundToInt(x - 1.5f);
            Clear(x0, -201, x0 + 3, -198);
            Girder(x0, x0 + 3, -198, new Color(0.62f, 0.55f, 0.7f), name + "_Grate");
        }
        return go;
    }

    static void HangLamp(float x, float ceilingY, float scale = 0.6f, float glow = 1f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
        Glow(x, ceilingY - scale * 0.6f, 1.6f, new Color(1f, 0.72f, 0.45f, 0.28f * glow), -7);
    }

    static void WallLamp(float x, float y, bool flip = false, float glow = 1f)
    {
        Prop("rock/decal_lamp_wall.png", x, y, 1.1f, -6, flip);
        Glow(x + (flip ? -0.15f : 0.15f), y + 0.05f, 1.5f, new Color(1f, 0.7f, 0.4f, 0.3f * glow), -7);
    }

    static GameObject Child(GameObject parent, string name, string art, Vector2 local, Vector2 scale, int order, Color color, float rot = 0f, Vector2? tiled = null)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        g.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        g.transform.localRotation = Quaternion.Euler(0, 0, rot);
        var sr = AddSprite(g, Art(art), order, tiled, color);
        return g;
    }

    /// <summary>
    /// 中ボスの設計図 (青図)。青く染めた鉄板 + 方眼 + 回転木馬の馬 1 頭 (carousel の絵を SpriteMask で切り抜き) +
    /// 腹の歯車・足の下の大歯車 (白線) + 寸法線 + 弱点の赤丸。四隅を鋲で留める。
    /// </summary>
    static void Blueprint(float cx, float cy, float w, float h)
    {
        var bp = NewObject("Blueprint_MidBoss", new Vector2(cx, cy), LayerDefault, "Props");
        var paper = new Color(0.3f, 0.48f, 0.86f);
        Child(bp, "Paper", "ferris/plate/plate_tex_r0_c0.png", Vector2.zero, Vector2.one, -6, paper, 0f, new Vector2(w, h));
        var line = new Color(0.85f, 0.95f, 1f, 0.22f);
        for (var gx = -w * 0.5f + 0.5f; gx < w * 0.5f; gx += 0.5f)
            Child(bp, "GridV", "ferris/spoke_beam.png", new Vector2(gx, 0), Vector2.one, -5, line, 90f, new Vector2(h - 0.2f, 0.04f));
        for (var gy = -h * 0.5f + 0.5f; gy < h * 0.5f; gy += 0.5f)
            Child(bp, "GridH", "ferris/spoke_beam.png", new Vector2(0, gy), Vector2.one, -5, line, 0f, new Vector2(w - 0.2f, 0.04f));
        var ink = new Color(0.85f, 0.97f, 1f, 0.8f);
        // 左: 足 (ピストン) の部品図と小さな歯車
        Child(bp, "LegPart", "platforms/piston.png", new Vector2(-1.75f, -0.1f), new Vector2(0.32f, 0.42f), -4, new Color(0.8f, 0.92f, 1f, 0.6f));
        Child(bp, "LegPart2", "platforms/piston.png", new Vector2(-1.1f, -0.1f), new Vector2(0.32f, 0.42f), -4, new Color(0.8f, 0.92f, 1f, 0.6f));
        Child(bp, "SmallGear", "platforms/gear_platform.png", new Vector2(-1.45f, 1.25f), Vector2.one * 0.22f, -4, new Color(0.8f, 0.92f, 1f, 0.55f));
        // 右: 馬 (carousel の左端の馬を SpriteMask で切り抜く。絵は底辺中央が原点、10x8)
        const float hs = 1.15f;
        var crop = new Rect(-4.2f, 1.9f, 1.95f, 2.05f);
        var at = new Vector2(0.85f, 0.15f);
        var horse = Child(bp, "Horse", "props/carousel.png", at - crop.center * hs, Vector2.one * hs, -3, new Color(0.62f, 0.82f, 1f, 0.92f));
        horse.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        var mask = new GameObject("HorseMask");
        mask.transform.SetParent(bp.transform, false);
        mask.transform.localPosition = at;
        var m = mask.AddComponent<SpriteMask>();
        m.sprite = Art("ferris/plate/plate_tex_r0_c0.png");
        var ms = m.sprite.bounds.size;
        mask.transform.localScale = new Vector3(crop.width * hs / ms.x, crop.height * hs / ms.y, 1f);
        // 足の下の床の大歯車 (白線)
        Child(bp, "FloorGear", "platforms/gear_platform.png", new Vector2(0.85f, -1.4f), Vector2.one * 0.3f, -4, new Color(0.8f, 0.92f, 1f, 0.4f));
        // 腹の歯車 (回る心臓) と弱点の赤丸
        var belly = at + new Vector2(0.12f, -0.3f) * hs;
        Child(bp, "BellyGear", "platforms/gear_platform.png", belly, Vector2.one * 0.16f, -2, new Color(1f, 1f, 1f, 0.95f));
        Child(bp, "WeakMark", "fx/glow.png", belly, Vector2.one * 2.2f, -1, new Color(1f, 0.25f, 0.25f, 0.8f));
        // 寸法線と引き出し線
        Child(bp, "DimH", "ferris/spoke_beam.png", new Vector2(0.85f, 1.55f), Vector2.one, -3, ink, 0f, new Vector2(2.2f, 0.05f));
        Child(bp, "DimV", "ferris/spoke_beam.png", new Vector2(2.25f, 0.15f), Vector2.one, -3, ink, 90f, new Vector2(2.3f, 0.05f));
        Child(bp, "Lead", "ferris/spoke_beam.png", belly + new Vector2(-0.75f, -0.55f), Vector2.one, -3, ink, 36f, new Vector2(1.8f, 0.05f));
        // 四隅の鋲 + 紙の影
        foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            Child(bp, "Rivet", "ferris/hub_axle.png", new Vector2(c.x * (w * 0.5f - 0.2f), c.y * (h * 0.5f - 0.2f)), Vector2.one * 0.04f, -2, new Color(0.9f, 0.85f, 0.9f));
        Child(bp, "Shadow", "ferris/plate/plate_tex_r0_c0.png", new Vector2(0.12f, -0.12f), Vector2.one, -7, new Color(0f, 0f, 0f, 0.45f), 0f, new Vector2(w, h));
    }

    // 整備員のロッカー (鉄板の箱 + 細い配管の枠 + 取っ手)
    static void Locker(float x0, float x1, float y0, float y1, Color tint)
    {
        var body = NewObject("Locker", new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), LayerDefault, "Props");
        AddSprite(body, Art("ferris/machine_room_wall.png"), -6, new Vector2(x1 - x0, y1 - y0), tint);
        var frame = new Color(0.45f, 0.38f, 0.45f);
        PipeRun(x0, y1 - 0.1f, x1, y1 - 0.1f, 0.3f, -5, frame);
        PipeRun(x0 + 0.08f, y0, x0 + 0.08f, y1, 0.25f, -5, frame);
        PipeRun(x1 - 0.08f, y0, x1 - 0.08f, y1, 0.25f, -5, frame);
        PipeRun((x0 + x1) * 0.5f, y0, (x0 + x1) * 0.5f, y1, 0.2f, -5, frame);
    }

    public static string Run()
    {
        var log = BuildBase("A1_D4");
        RockDecor(0.6f);

        // ================= D4 廊下: 床 y-198 (厚さ 6)、天井 y-191
        Solid(270, -204, 348, -198);
        Solid(270, -191, 304, -186);
        Solid(314, -191, 348, -186);
        Solid(304, -188, 314, -186);       // 中ほどは天井が高い (y-188): 点検口の上で跳んで落下攻撃
        Clear(270, -191, 276, -189);       // 入口と出口の手前は通路と同じ高さ
        Clear(344, -191, 348, -189);

        // ピストン: C (天井) と F (床) を交互に。C は位相 0、F は半周期 (2 秒) ずれ。東へ行くほど少しずつ遅れる
        var cx = new[] { 281.5f, 287.5f, 293.5f, 299.5f, 318.5f, 324.5f, 330.5f, 336.5f, 342f };
        var ceil = new[] { true, false, true, false, true, false, true, false, true };
        for (var i = 0; i < cx.Length; i++)
            Piston((ceil[i] ? "PistonC" : "PistonF") + i, cx[i], ceil[i], (ceil[i] ? 0f : 2f) + i * 0.15f);

        // 点検口 (橙の床 = 落下攻撃) → D4s
        SkillBlock(306, -201, 312, -198, PlayerSkill.GroundSlam, "A1_Slam_D4s_1");
        Clear(306, -207, 312, -201);

        // ================= D4s 油だまりの底 (床 y-216, 天井 y-207)
        Solid(300, -219, 321, -216);
        Solid(300, -216, 303, -213);                  // 西の岩棚 (帰り道の 1 段目)
        Solid(318, -216, 321, -213);                  // 東の油の缶の台 = ハートピン4
        WaterBody(303, -216, 314, -214, new Color(0.78f, 0.72f, 0.7f), "Oil", WaterLook.Oil);   // polish B: 油の部品 (oil_body_tile + oil_surface_strip)
        PipePlatform(303, 306, -210, Rust);           // 帰り道: 岩棚 → 配管 3 本 → 点検口の口
        PipePlatform(307, 310, -207, Rust);
        PipePlatform(309, 312, -204, Rust);
        PipePlatform(306, 309, -201, Rust, "Pipe_Hatch");
        var pin = KeyItemAt(319.5f, -212.3f, "ハートピン4", "A1_HeartPin4", "A1_Pickup_D4s_1", "props/heart_pin.png", "拾う");
        Glow(319.5f, -212.2f, 1.4f, new Color(1f, 0.5f, 0.6f, 0.3f), -6);
        // さらに下 (奈落の倉庫, 別エリア) の橙の床と、その下の行き止まり
        SkillBlock(314, -219, 317, -216, PlayerSkill.GroundSlam, "A1_Slam_D4s_2");
        Clear(314, -222, 317, -219);
        Prop("rock/decal_chain_hanging.png", 315.5f, -219.2f, 0.5f, -7, false, new Color(0.4f, 0.38f, 0.48f));
        // 油の缶・したたる油
        Prop("props/cargo_crate.png", 301.4f, -212.5f, 0.9f, -4, false, new Color(0.55f, 0.45f, 0.35f));
        foreach (var x in new[] { 305.5f, 311.5f })
            PipeRun(x, -213.8f, x, -207f, 0.12f, -6, new Color(0.3f, 0.22f, 0.12f, 0.8f));   // 天井から垂れる油の筋
                WallLamp(320.2f, -209.5f, true, 0.7f);

        // ================= D8 整備員の休憩所 (床 y-198, 天井 y-186)
        Solid(351, -204, 375, -198);
        Solid(351, -186, 375, -183);
        SavePointAt(357.5f, -198f);
        Blueprint(366f, -192.6f, 5.2f, 4f);
        Locker(352.2f, 354.6f, -198f, -194.6f, new Color(0.8f, 0.85f, 0.9f));
        Locker(369.6f, 371.8f, -198f, -194.8f, new Color(0.9f, 0.8f, 0.78f));
        Prop("props/cargo_crate.png", 362.4f, -197.5f, 0.9f, -4, false, new Color(0.75f, 0.68f, 0.72f));   // 机がわりの木箱
        HangLamp(357.5f, -186f, 0.7f, 1.4f);
        HangLamp(366f, -186f, 0.6f, 1.1f);
        PlateWalls(351, -204, 375, -183);
        TintRock(351, -204, 375, -183, new Color(0.95f, 0.82f, 0.72f));
        // 東の出口 (重馬の部屋) の上の警告灯
        GuideLamp(374.4f, -189f, 0f, Red, true, 1.2f, "WarnLampBoss");
        GuideLamp(373.2f, -189f, 0.25f, Red, true, 1.2f, "WarnLampBoss2");

        // ================= 飾り: 廊下の配管・ピストンの機械・灯
        PipeRun(270f, -190.6f, 348f, -190.6f, 0.5f, -7, Dim(BgPipe, 0.8f));
                        foreach (var x in new[] { 284.5f, 290.5f, 296.5f, 321.5f, 327.5f, 333.5f, 339.3f })
            WallLamp(x, -193.2f, x % 2 > 1, 0.55f);
        GuideLamp(271.5f, -189f, 0f, Guide, true, 1.5f, "GuideLamp0");
        GuideLamp(346.5f, -189f, 0f, Guide, true, 1.5f, "GuideLamp1");
        WallLamp(308f, -191.2f, false, 0.9f);

        // ================= 暗さと質感
        TintRock(255, -230, 390, -170, new Color(0.66f, 0.62f, 0.76f));
        TintRock(296, -230, 325, -205, new Color(0.62f, 0.55f, 0.55f));   // 油で黒ずんだ岩
        Shade(300f, -219f, 321f, -207f, 0.5f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(310f, -212f, true, name: "OilFlyer");      // 油だまりの底 (D4s) の空中。ピストンの廊下には置かない (タイミングの部屋)

        return log + Finish();
    }
}
