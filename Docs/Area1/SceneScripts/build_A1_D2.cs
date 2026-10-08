using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_D2 「搬送ベルトの通路」(D2 x168..252 y-117..-99) + 「錆びた配電室」(D2w x141..165 y-123..-105)
// 上 = 配管の縦穴 A1_D1 (天井の口 x240..249) / 下 = 蒸気管の迷路 A1_D5 (床の格子 x186..195)
// 上 (D2w の天井 x147..153) = 楽屋裏の勝手口 A1_T3 (梯子を下ろすと登れる。開いた後は T3 の戸の穴から降りても来られる = SpawnFromA1_T3 (150,-106.5) から梯子へ落ちる)
// D2: 床 y-117 の大半がベルトコンベア (既定は西向き = 奥へ進む向き)。立ち止まると流される。
//     ベルト 3 本の間に固定の床 (敵が立つ島)、ベルトの上に配管の足場 (y-114, 流れを避ける道)。敵 3 (地上 2 + 浮遊 1)。
//     東端は縦穴の真下の着地塔 (配管を左右交互に y-102 まで)。
// D2w: 配電盤のレバー① (A1_BeltReversed: ベルトが東向きに = 帰り道が楽)、跳ね上げ戸のレバー② (A1_LadderBackstage: 梯子が下りて A1_T3 へ)。
//      棚に道具「糸玉」×3 と日誌その3。西の床の紫の鉄扉 (突進 A1_Rush_D2w) の奥は地下水路 (別エリア・準備中) の目印だけ。
// 先に別の呼び出しで OpenOrCreate("A1_D2") を実行しておくこと。
public static class BuildD2
{
    static readonly Color BeltTint = new Color(1f, 0.5f, 0.42f);     // レバー① ベルト = 赤 (柄 gimmick/lever/lever_01_red)
    static readonly Color HatchTint = new Color(0.5f, 0.95f, 0.88f);  // レバー② 跳ね上げ戸 = 青緑
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Red = new Color(1f, 0.32f, 0.3f);
    static readonly Color Green = new Color(0.45f, 1f, 0.6f);
    const float BeltSpeed = -3.5f;

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    static void HangLamp(float x, float ceilingY, float scale = 0.6f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
        Glow(x, ceilingY - scale * 0.6f, 1.6f, new Color(1f, 0.72f, 0.45f, 0.28f), -7);
    }

    // ゆっくり回る奥の歯車 (当たり無し)
    static void BgGear(float x, float y, float scale, float speed, float k = 0.32f)
    {
        var g = NewObject("BgGear", new Vector2(x, y), LayerDefault, "Props");
        g.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(g, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", speed));
        var art = new GameObject("Art");
        art.transform.SetParent(g.transform, false);
        art.transform.localScale = Vector3.one * scale;
        AddSprite(art, Art("platforms/gear_platform.png"), -46, null, new Color(k, k * 0.85f, k * 1.25f));
    }

    // 状態ランプ (フラグで赤→緑)
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

    // 配電盤 (奥の壁の前に立つ箱。鉄板の絵 + 細い配管の枠 + ちらつく小さな表示灯の列 + 天井からの電線)
    static void Cabinet(float x0, float y0, float x1, float y1, int seed, bool cable = true)
    {
        var w = x1 - x0;
        var h = y1 - y0;
        var body = NewObject("Cabinet", new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), LayerDefault, "Props");
        AddSprite(body, Art("ferris/machine_room_wall.png"), -6, new Vector2(w, h), new Color(0.62f, 0.6f, 0.62f));
        var frame = new Color(0.5f, 0.4f, 0.45f);
        PipeRun(x0, y1 - 0.1f, x1, y1 - 0.1f, 0.35f, -5, frame);
        PipeRun(x0 + 0.1f, y0, x0 + 0.1f, y1, 0.3f, -5, frame);
        PipeRun(x1 - 0.1f, y0, x1 - 0.1f, y1, 0.3f, -5, frame);
        var rnd = new System.Random(seed * 7919);
        var cols = Mathf.Max(2, Mathf.FloorToInt(w / 0.8f) - 1);
        for (var i = 0; i < cols; i++)
        {
            var lx = x0 + 0.6f + i * (w - 1.2f) / Mathf.Max(1, cols - 1);
            var ly = y1 - 0.75f;
            var c = rnd.NextDouble() < 0.35 ? new Color(1f, 0.35f, 0.3f) : rnd.NextDouble() < 0.5 ? new Color(0.45f, 1f, 0.6f) : new Color(1f, 0.85f, 0.4f);
            var dot = NewObject("PanelLight", new Vector2(lx, ly), LayerDefault, "Props");
            var g = new GameObject("Dot");
            g.transform.SetParent(dot.transform, false);
            g.transform.localScale = Vector3.one * (0.42f / (256f / 546f));
            AddSprite(g, Art("fx/glow.png"), -4, null, c);
            AddComp<BlinkingLight>(dot, ("_period", 0.6f + (float)rnd.NextDouble() * 1.8f), ("_phase", (float)rnd.NextDouble()),
                ("_minAlpha", 0.25f), ("_onRatio", 0.3f + (float)rnd.NextDouble() * 0.5f));
        }
        if (cable)
            foreach (var cx in new[] { x0 + w * 0.3f, x0 + w * 0.72f })
                PipeRun(cx, y1, cx, -105f, 0.18f, -7, new Color(0.3f, 0.26f, 0.34f));
    }

    // レバー = 台座 + 傾く柄 (Drive「lever」, 2026-10-08 PendingChanges 42)。柄の色で役目を分ける (gimmick/lever/lever_01_red / _teal =
    // polish B の色違いの柄を新しい台座の位置に合わせた物)。高さ ≈2.05
    static GameObject Lever(string name, float x, float floorY, string flag, string message, string handle, string prompt)
    {
        var lever = LeverAt(name, x, floorY, 0.6f, handle);
        AddComp<LeverSwitch>(lever, ("_progressFlag", flag), ("_activatedMessage", message), ("_promptText", prompt),
            ("_promptOffset", new Vector3(0, 3.45f, 0)));
        SetLeverHandle(lever);
        return lever;
    }

    // polish B: 札 = 専用の看板の絵 (sign_belt / sign_ladder: 金の縁 + 作用先の絵) + 下に短い文字 (レバーの柄と同じ色)
    static void LeverPlaque(float x, float y, Color tint, string sign, string label)
    {
        var go = NewObject("Plaque_" + sign, new Vector2(x, y), LayerDefault, "Props");
        PartSprite(go.transform, "Sign", "polish/" + sign + ".png", Vector2.zero, 1.3f, OrderGimmick + 1);   // 配電盤 (Cabinet) の筋交いより手前
        var text = new GameObject("Label");
        text.transform.SetParent(go.transform, false);
        text.transform.localPosition = new Vector3(0, -0.98f, 0);
        var tmp = text.AddComponent<TMPro.TextMeshPro>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/Fonts/JapaneseUI SDF.asset");
        tmp.text = label;
        tmp.fontSize = 3.2f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = tint;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(1.6f, 0.5f);
        text.GetComponent<MeshRenderer>().sortingOrder = OrderGimmick;
    }

    public static string Run()
    {
        var log = BuildBase("A1_D2");

        // ================= D2 床 y-117 (岩)。ベルト 3 本 (西向き) + 固定の島 + 床の格子 (A1_D5 へ)
        Belt(172, 186, -117, BeltSpeed, "A1_BeltReversed", "BeltWest");
        Girder(186, 195, -117, new Color(0.55f, 0.48f, 0.62f), "Grate");   // ↓+ジャンプで蒸気管の迷路へ
        Belt(198, 214, -117, BeltSpeed, "A1_BeltReversed", "BeltMid");
        Belt(222, 238, -117, BeltSpeed, "A1_BeltReversed", "BeltEast");
        MoveSpawn("A1_D5", 196.6f, -115.5f);                              // 格子の右の固定の床

        // ベルトの上の配管 (流れを避けて渡れる道。跳び移りで敵をかわす)
        PipePlatform(175, 182, -114, Rust);
        PipePlatform(203, 210, -114, Rust);
        PipePlatform(226, 233, -114, Rust);
        PipePlatform(213, 223, -111, Rust);   // 島の上 (浮遊する敵の止まり木。左右の配管から跳び移れる)
        // 着地塔 (A1_D1 の縦穴の真下): 左右交互に 3 ずつ
        PipePlatform(241, 248, -102, Rust, "Pipe_Landing");   // SpawnFromA1_D1 (244.5,-100.5)
        PipePlatform(247, 252, -105, Rust);
        PipePlatform(240, 245, -108, Rust);
        PipePlatform(246, 251, -111, Rust);
        PipePlatform(239, 244, -114, Rust);

        // 敵 3: 島の上 / 中のベルトの上 / 浮遊 (東のベルトの上)
        Enemy(218f, -116f, name: "IslandEnemy");
        Enemy(206f, -116f, name: "BeltEnemy");
        Enemy(229f, -108f, true, name: "FlyingEnemy");

        // ================= D2w 錆びた配電室 (床 y-123)
        Solid(161, -123, 165, -120);                          // 東の段 (通路 y-117 へ +3)
        PipePlatform(142, 148, -120, Rust);                   // 棚 A
        PipePlatform(142, 146, -117, Rust);                   // 棚 B
        ConsumablePickupAt(146.2f, -120f, "Assets/Items/ThreadBall.asset", 3, "A1_Pickup_D2w_1");
        KeyItemAt(143.6f, -117f, "日誌「閉園しなかった夜」その3", "A1_Lore_Journal3", "A1_Pickup_D2w_2");

        // レバー①: 配電盤 (ベルトの向き)。盤の上にベルトの向きの表示 (矢印の帯が流れる)
        Lever("BeltLever", 157.6f, -123f, "A1_BeltReversed", "ゴウン…… 搬送ベルトが逆向きに回りはじめた!", "lever_01_red.png", "ベルトの向きを変える");
        LeverPlaque(157.6f, -118.75f, BeltTint, "sign_belt", "ベルト");
        StatusLamp("BeltLamp", 157.6f, -116.4f, "A1_BeltReversed");
        Cabinet(155.2f, -123f, 160.4f, -117.6f, 1);
        Cabinet(161.4f, -120f, 164.6f, -113.5f, 2);
        var mini = Belt(156, 160, -117, BeltSpeed, "A1_BeltReversed", "BeltIndicator");   // 見た目だけ (盤の上の小さな帯。当たりを外す)
        Object.DestroyImmediate(mini.GetComponent<BoxCollider2D>());

        // レバー②: 跳ね上げ戸 (梯子を下ろす → A1_T3 の楽屋裏へ。開くまでは T3 側から降りられない)
        Lever("LadderLever", 154.2f, -123f, "A1_LadderBackstage", "ガラガラ…… 天井の跳ね上げ戸が開いて梯子が下りてきた!", "lever_01_teal.png", "跳ね上げ戸を開ける");
        LeverPlaque(154.2f, -118.75f, HatchTint, "sign_ladder", "はね戸");
        // レバー②から天井の跳ね上げ戸へ引き綱 (作用先が見える)
        PipeRun(153.85f, -118.8f, 153.85f, -105.4f, 0.12f, OrderProp - 1, HatchTint * new Color(0.75f, 0.75f, 0.75f, 1f));
        PipeRun(152.9f, -105.4f, 153.91f, -105.4f, 0.12f, OrderProp - 1, HatchTint * new Color(0.75f, 0.75f, 0.75f, 1f));
        StatusLamp("HatchLamp", 154.2f, -106.6f, "A1_LadderBackstage");
        var hatch = NewObject("Hatch", new Vector2(150f, -104.75f), LayerGround, "Terrain Objects");
        Box(hatch, new Vector2(6f, 0.5f), false);
        var plank = (Tile("stagetile_floor_08_m") as UnityEngine.Tilemaps.Tile)?.sprite;
        var lid = new GameObject("Lid");
        lid.transform.SetParent(hatch.transform, false);
        AddSprite(lid, plank, OrderGimmick, new Vector2(6f, 0.8f), new Color(0.62f, 0.5f, 0.48f));
        var folded = NewObject("FoldedLadder", new Vector2(150.5f, -106.1f), LayerDefault, "Props");   // 戸の裏に畳まれた梯子
        AddSprite(folded, Art("tiles/tile_ladder.png"), OrderProp, new Vector2(1f, 1.8f), new Color(0.7f, 0.65f, 0.75f));
        var openLid = NewObject("LidOpen", new Vector2(152.55f, -102.6f), LayerDefault, "Props");   // 開いた戸は縦坑の右の壁に沿って下がる
        openLid.transform.rotation = Quaternion.Euler(0, 0, 90f);
        AddSprite(openLid, plank, OrderProp, new Vector2(4.6f, 0.8f), new Color(0.62f, 0.5f, 0.48f));
        var ladder = Ladder(150, -123, -103);
        var hatchAct = NewObject("HatchFlag", new Vector2(150f, -110f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(hatchAct, ("_flags", new[] { "A1_LadderBackstage" }),
            ("_showWhenSet", new Object[] { ladder, openLid }), ("_hideWhenSet", new Object[] { hatch, folded }));

        // 西の紫の鉄扉 (突進でしか破れない) → 奥は地下水路 (別エリア・準備中) の目印だけ
        SkillBlock(139, -123, 141, -119, PlayerSkill.ChargeRush, "A1_Rush_D2w");
        Clear(137, -123, 139, -119);
        Prop("props/fence_gate.png", 137.6f, -123f, 0.55f, OrderProp, false, new Color(0.5f, 0.6f, 0.75f));
        var drip = Prop("platforms/steam_puff.png", 138f, -123f, 1f, -6, false, new Color(0.7f, 0.85f, 1f, 0.12f));
        drip.transform.localScale = new Vector3(0.85f, 0.85f, 1f);   // polish A: 蒸気の絵は縦横比を保つ (潰すと針葉樹に見える)

        // ================= 飾り: 天井の配管・吊り灯・奥の歯車・配電室の明滅する電球
        PipeRun(168f, -99.55f, 252f, -99.55f, 0.9f, -6, BgPipe);   // 天井に半分埋まった管
        PipeRun(168f, -100.6f, 236f, -100.6f, 0.5f, -7, Dim(BgPipe, 0.7f));
        foreach (var x in new[] { 172.5f, 199.5f, 221.5f })
            PipeRun(x, -116f, x, -100f, 0.55f, -7, Dim(BgPipe, 0.85f));
        PipeRun(141f, -106.2f, 147f, -106.2f, 0.6f, -6, BgPipe);
        PipeRun(153f, -106.2f, 165f, -106.2f, 0.6f, -6, BgPipe);
        PipeRun(163.8f, -120f, 163.8f, -106f, 0.5f, -7, Dim(BgPipe, 0.9f));
        foreach (var x in new[] { 180f, 207f, 230f })
            HangLamp(x, -99.4f);
        BgGear(190.5f, -106f, 1.6f, 9f, 0.17f);
        BgGear(184f, -103f, 0.9f, -16f, 0.15f);
        BgGear(218f, -104.5f, 1.3f, -7f, 0.17f);
        BgGear(236f, -108f, 1.1f, 11f, 0.14f);
        BgGear(158f, -109.5f, 1.0f, 6f, 0.15f);
        // 配電室の裸電球 (ちらつく)
        var bulb = NewObject("FlickerBulb", new Vector2(160.5f, -105f), LayerDefault, "Props");
        var bl = new GameObject("Lamp");
        bl.transform.SetParent(bulb.transform, false);
        bl.transform.localPosition = new Vector3(0, -0.5f, 0);
        bl.transform.localScale = Vector3.one * 0.4f;
        bl.transform.localRotation = Quaternion.Euler(0, 0, 180f);
        AddSprite(bl, Art("props/boss_door_lamp_on.png"), OrderProp, null, new Color(1f, 0.85f, 0.6f));
        var bg = new GameObject("Halo");
        bg.transform.SetParent(bulb.transform, false);
        bg.transform.localPosition = new Vector3(0, -0.5f, 0);
        bg.transform.localScale = Vector3.one * (2.4f / (256f / 546f));
        AddSprite(bg, Art("fx/glow.png"), -7, null, new Color(1f, 0.75f, 0.45f, 0.4f));
        AddComp<BlinkingLight>(bulb, ("_period", 0.37f), ("_minAlpha", 0.45f), ("_onRatio", 0.8f));
        // 置き去りの荷物 (島と配電室)
        Crate(216f, -117f);  // 2026-10-04 PendingChanges 28: 飾りの木箱は壊せる木箱に (見た目が同じで紛らわしいため)
        CrateStack(239.6f, -117f, 2);

        // ================= 暗さと質感
        TintRock(100, -160, 300, -60, new Color(0.72f, 0.68f, 0.8f));
        PlateWalls(131, -133, 165, -95);
        TintRock(131, -133, 165, -95, new Color(0.85f, 0.7f, 0.65f));   // 錆びた鉄板

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(180f, -106f, true, name: "BeltFlyerWest");  // 西寄りのベルトの上空 (配電室への分かれ道の手前)

        return log + Finish();
    }
}
