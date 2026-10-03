using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_AB 「噴水下の大音楽堂」(AB x318..372 y-72..-30) + 「噴水への大階段」(ST x342..360 y-27..-3)
// 西 = 灯の大扉の回廊 A1_C1 (x315..318, 床 y-54) / 上 = 噴水広場 A1_FP (x345..354 y-3..0, 噴水の排水口)
// AB: 床 y-60 の大ホール (天井 y-36)。西の入口の踊り場 y-54 (段 y-57) → ホール。東に舞台 (y-57) とパイプオルガン、左右に緞帳。
//     エリアボス「指揮者ノクターン」= BossRoom A1_AreaBoss (FlyingEnemy ×2.5 + EnemyConsts_A1Conductor)。入ると西のゲートが閉じる。
//     前半 (HP 50% まで) = 浮遊する音符の足場 4 枚 (MovingPlatform, すり抜け, 段差 3 = 通常ジャンプで乗り継げる) で空中のボスを叩く。
//     後半 = 音符が点滅して消え、床の昇降台 5 本 (Piston, 上向き +3/+6, 押しつぶし無し) が 2 組交互にせり上がる (BossPhaseSwitch)。
//     撃破: 赤ハサミ (UpgradePickup Red) がホールの中央に出現 + 二段ジャンプのヒント + 天井の点検口 (ハッチ) が開き、
//     静止した音符の階段 4 段 (段差 5 = 二段ジャンプ) が天井の穴へ。
// ST: 天井の穴〜噴水の真下の縦穴。左右交互の岩棚 6 段 (段差 6 / 最後 5, 棚は横に重ねてあるので真上へ抜けられない) = 二段ジャンプでだけ登れる。
//     最上段 (y-5) から跳ぶと噴水の排水口 → A1_FP。
// 先に別の呼び出しで OpenOrCreate("A1_AB") を実行しておくこと。
public static class BuildAB
{
    const string Flag = "A1_AreaBoss";
    static readonly Color Gold = new Color(1f, 0.86f, 0.55f);
    static readonly Color WarmGlow = new Color(1f, 0.78f, 0.45f, 0.32f);

    static GameObject HallArt(string rel, float x, float y, Vector3 scale, int order, Color? color = null, bool flipX = false, string group = "Props")
    {
        var go = NewObject(System.IO.Path.GetFileNameWithoutExtension(rel), new Vector2(x, y), LayerDefault, group);
        go.transform.localScale = scale;
        var sr = AddSprite(go, Art(rel), order, null, color);
        sr.flipX = flipX;
        return go;
    }

    // 音符の足場 (すり抜け, layer 10)。pos = 足場の上面の中心。音符の頭の上面が当たりの上面に来るよう絵をずらす
    const float NoteScale = 2.0f;
    static GameObject NotePlatform(string name, Vector2 top, Transform parent, Color tint, bool moving, Vector2[] path = null,
        float speed = 1.5f, float wait = 0.6f, float delay = 0f)
    {
        var go = new GameObject(name) { layer = LayerGround };
        go.transform.SetParent(parent, false);
        go.transform.position = top;
        var c = Box(go, new Vector2(1.9f, 0.5f), false, new Vector2(0, -0.25f));
        c.usedByEffector = true;
        var e = go.AddComponent<PlatformEffector2D>();
        e.useOneWay = true;
        e.surfaceArc = 180f;
        var art = new GameObject("Note");
        art.transform.SetParent(go.transform, false);
        // 頭の中心 = 絵の中心から (-0.25, -0.30)×scale、頭の上面 = 中心から -0.13×scale
        art.transform.localPosition = new Vector3(0.25f * NoteScale, 0.10f * NoteScale, 0);
        art.transform.localScale = Vector3.one * NoteScale;
        AddSprite(art, Art("hall/music_note.png"), OrderPlatforms, null, tint);
        var glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0, -0.4f, 0);
        glow.transform.localScale = new Vector3(2.6f, 1.3f, 1f);
        AddSprite(glow, Art("fx/glow.png"), OrderPlatforms - 1, null, new Color(1f, 0.45f, 0.8f, 0.35f));
        if (moving)
        {
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            AddComp<MovingPlatform>(go, ("_mode", 0), ("_waypoints", path), ("_speed", speed), ("_waitTime", wait), ("_startDelay", delay));
        }
        return go;
    }

    // 床の昇降台 (Piston 上向き)。root = 縮んだ時の上面 (床面) の中心。押しつぶし無し (天井まで十分高い)
    static GameObject FloorLift(string name, float x, int floorY, float rise, float delay, Transform parent)
    {
        var go = new GameObject(name) { layer = LayerGround };
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector2(x, floorY);
        Box(go, new Vector2(3f, 8f), false, new Vector2(0, -4f));
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var body = new GameObject("Body");
        body.transform.SetParent(vis.transform, false);
        body.transform.localPosition = new Vector3(0, -4.5f, 0);
        AddSprite(body, Art("ferris/plate/plate_tex_r0_c0.png"), OrderBackfill - 2, new Vector2(2.8f, 8f), new Color(0.62f, 0.42f, 0.66f));
        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(vis.transform, false);
        stripe.transform.localPosition = new Vector3(0, -4.5f, 0);
        AddSprite(stripe, Art("ferris/support_leg.png"), OrderBackfill - 1, new Vector2(0.8f, 8f), new Color(1f, 0.8f, 0.45f));
        var cap = new GameObject("Cap");
        cap.transform.SetParent(vis.transform, false);
        cap.transform.localPosition = new Vector3(0, -0.5f, 0);
        AddSprite(cap, Art("platforms/lift_platform.png"), OrderGimmick, null, new Color(1f, 0.85f, 0.6f));
        AddComp<Piston>(go, ("_extendOffset", new Vector2(0, rise)), ("_retractedWait", 2.0f), ("_telegraphTime", 0.9f),
            ("_extendDuration", 0.8f), ("_extendedWait", 2.3f), ("_retractDuration", 1.0f), ("_startDelay", delay),
            ("_shakeVisual", vis.transform), ("_shakeAmplitude", 0.08f), ("_telegraphColor", new Color(1f, 0.5f, 0.75f)));
        return go;
    }

    static void Chandelier(float x, float ceilingY, float scale)
    {
        HallArt("hall/chandelier.png", x, ceilingY, Vector3.one * scale, -20, new Color(0.95f, 0.9f, 0.95f));
        Glow(x, ceilingY - 2.6f * scale / 2.5f, 3.4f * scale / 2.5f, new Color(1f, 0.8f, 0.5f, 0.35f), -21);
    }

    static void Spotlight(float x, float ceilingY, float floorY, Color beam)
    {
        HallArt("hall/spotlight.png", x, ceilingY, Vector3.one * 1.1f, -19);
        var cone = NewObject("SpotBeam", new Vector2(x, (ceilingY + floorY) * 0.5f - 0.8f), LayerDefault, "Props");
        cone.transform.localScale = new Vector3(3.2f, (ceilingY - floorY) * 1.15f, 1f);
        AddSprite(cone, Art("fx/glow.png"), -22, null, beam);
        var pool = NewObject("SpotPool", new Vector2(x, floorY + 0.15f), LayerDefault, "Props");
        pool.transform.localScale = new Vector3(5f, 1.2f, 1f);
        AddSprite(pool, Art("fx/glow.png"), -6, null, new Color(beam.r, beam.g, beam.b, 0.4f));
    }

    // ---- polish B: 音楽堂の奥の壁 (部品) ----
    static readonly Color HallBack = new Color(0.5f, 0.44f, 0.56f);       // 壁紙
    static readonly Color HallTrim = new Color(0.62f, 0.55f, 0.66f);      // 腰板・付け柱・桟敷席
    static readonly Color HallPilaster = new Color(0.42f, 0.36f, 0.46f); // 付け柱 (明るいと梯子に見えるので一段暗く)
    static readonly Color HallBalcony = new Color(0.5f, 0.44f, 0.54f);   // 桟敷席 (手すりが足場に見えないよう沈める)
    static readonly float[] Pilasters = { 326.5f, 334.5f, 342.5f, 350.5f, 358.5f };

    static void HallBackdrop()
    {
        var root = NewObject("HallBackdrop", new Vector2(345f, -48f), LayerDefault, "Background");
        var t = root.transform;
        // 壁紙 (3×3 セルの継ぎ目なし)。ホール全体 x318..372 y-60..-36
        PartSprite(t, "Wallpaper", "polish/hall_wall_panel.png", new Vector2(0, 0), 1f, OrderRoomBackdrop + 1, HallBack, new Vector2(54f, 24f));
        // 腰板 (床から 1.5)
        PartSprite(t, "Wainscot", "polish/hall_wainscot_plum.png", new Vector2(0, -12f), 1f, -62, HallTrim, new Vector2(54f / 1f, 1.5f));
        // 付け柱 (腰板の上〜飾り幕の下)
        foreach (var x in Pilasters)
            PartSprite(t, "Pilaster", "polish/hall_pilaster.png", new Vector2(x - 345f, -48.75f + 48f), 0.9f, -61, HallPilaster, new Vector2(1f, 21.5f / 0.9f));
        // 桟敷席 (柱の間、床から 12 = ボスの浮く高さより上の奥) と左右の壁灯
        foreach (var cx in new[] { 330.5f, 338.5f, 346.5f })
        {
            PartSprite(t, "Balcony", "polish/hall_balcony_box.png", new Vector2(cx - 345f, -47.6f + 48f), 1.2f, -60, HallBalcony);
            foreach (var side in new[] { -1f, 1f })
            {
                PartSprite(t, "Sconce", "polish/hall_light_sconce.png", new Vector2(cx - 345f + side * 3.05f, -45.6f + 48f), 0.8f, -59,
                    new Color(0.95f, 0.88f, 0.85f)).flipX = side > 0;
                Glow(cx + side * 3.05f, -45.4f, 1.3f, new Color(1f, 0.8f, 0.5f, 0.28f), -58);
            }
        }
        // 西の入口 (A1_C1 の機械室の通路 → 壁紙) の境目に額縁の柱を立てる (背景の切り替わりが継ぎ目に見えないように)
        PartSprite(t, "EntranceFrame", "polish/hall_pilaster.png", new Vector2(318.45f - 345f, -49.5f + 48f), 1f, -56,
            new Color(0.6f, 0.52f, 0.6f), new Vector2(1f, 9.2f));
        // 天井際の飾り幕 (舞台の手前まで。舞台の上は緞帳の幕)
        PartSprite(t, "Valance", "polish/hall_curtain_valance.png", new Vector2(337f - 345f, -36f + 48f), 1f, -57,
            new Color(0.78f, 0.7f, 0.78f), new Vector2(38f, 1.5f));
    }

    public static string Run()
    {
        var log = BuildBase("A1_AB");
        RockDecor(0.35f);
        var limit = Object.FindAnyObjectByType<CameraLimit>();
        Set(limit, "_minY", -56f);

        // ================= 地形
        Ground(318, 372, -60, 12);                 // ホールの床 y-60
        Solid(318, -60, 324, -54);                 // 入口の踊り場 y-54 (A1_C1 と同じ床)
        Solid(324, -60, 327, -57);                 // 段 y-57
        Solid(318, -45, 324, -30);                 // 踊り場の天井 (通路の上端 -45)
        Solid(324, -36, 345, -30);                 // ホールの天井 y-36 (中央 x345..354 だけ ST への穴)
        Solid(354, -36, 372, -30);
        Solid(356, -60, 372, -57);                 // 舞台 y-57

        // ================= ST: 左右交互の岩棚 7 段 (段差 5 = 二段ジャンプ。通常ジャンプ 3.5 + 崖つかみ 1.3 = 4.8 では届かない)。
        // 左の棚は x350 まで・右の棚は x351 から (隙間 1 = 棚の端で跳んでも頭を打たない。体の幅 0.7 なので大ジャンプで真上へ抜けるのはほぼ無理)
        Solid(351, -36, 354, -35);                 // L1 (穴の中、右) 上面 -35
        Solid(345, -31, 350, -30);                 // L2 (穴の上端、左) -30
        Solid(351, -26, 360, -25);                 // L3 右 -25
        Solid(342, -21, 350, -20);                 // L4 左 -20
        Solid(351, -16, 360, -15);                 // L5 右 -15
        Solid(342, -11, 350, -10);                 // L6 左 -10
        Solid(351, -6, 357, -5);                   // L7 右 -5 (ここから跳ぶと噴水の排水口)
        MoveSpawn("A1_FP", 353.5f, -3.5f);         // 噴水から降りてくると L7 の上

        // ================= ボス: 指揮者ノクターン
        var arena = BossRoom(new RectInt(324, -60, 48, 24), Flag, new Vector2(344f, -52f), true,
            new[] { new RectInt(322, -54, 2, 9) },
            "指揮者ノクターンを倒した! 鳴りやまなかった音楽が、ふっと止んだ……");
        var boss = GameObject.Find("Boss_" + Flag);
        boss.name = "Boss_Nocturne";
        boss.transform.localScale = Vector3.one * 2.5f;
        var ec = boss.GetComponent<EnemyController>();
        Set(ec, "_consts", AssetDatabase.LoadAssetAtPath<EnemyConsts>("Assets/Prefabs/Area1/EnemyConsts_A1Conductor.asset"));
        boss.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.62f, 1f);
        var fpm = boss.GetComponent<FloatingPatrolMotion>();
        if (fpm != null) { Set(fpm, "_hoverAmplitude", 1.3f); Set(fpm, "_hoverFrequency", 0.9f); }
        // 指揮棒 (飾り)
        var baton = new GameObject("Baton");
        baton.transform.SetParent(boss.transform, false);
        baton.transform.localPosition = new Vector3(0.42f, 0.18f, 0);
        baton.transform.localRotation = Quaternion.Euler(0, 0, -35f);
        AddSprite(baton, Art("ferris/support_leg.png"), 6, new Vector2(0.12f, 0.75f), Gold);
        var aura = new GameObject("Aura");
        aura.transform.SetParent(boss.transform, false);
        aura.transform.localScale = Vector3.one * 0.9f;
        AddSprite(aura, Art("fx/glow.png"), 4, null, new Color(0.75f, 0.45f, 1f, 0.4f));

        // ---- 前半: 浮遊する音符 (通常ジャンプで乗り継げる高さ: 床 -60 → -57 → -54 → -51)
        var p1 = NewObject("Phase1_Notes", new Vector2(344f, -54f), LayerDefault, "Gimmicks");
        NotePlatform("NoteA", new Vector2(330f, -57f), p1.transform, Color.white, true, new[] { Vector2.zero, new Vector2(7f, 0) }, 1.6f, 0.7f);
        NotePlatform("NoteB", new Vector2(346f, -54f), p1.transform, new Color(0.85f, 0.9f, 1f), true, new[] { Vector2.zero, new Vector2(-6f, 0) }, 1.4f, 0.7f, 1.2f);
        NotePlatform("NoteC", new Vector2(336f, -51f), p1.transform, new Color(1f, 0.85f, 0.95f), true, new[] { Vector2.zero, new Vector2(6f, 0) }, 1.2f, 0.9f, 0.5f);
        NotePlatform("NoteD", new Vector2(352.5f, -57f), p1.transform, new Color(1f, 0.95f, 0.8f), true, new[] { Vector2.zero, new Vector2(0, 6f) }, 1.3f, 1.1f);

        // ---- 後半: 床の昇降台 (2 組が交互に +3/+6 せり上がる。周期 7.0 秒、B 組は半周期遅れ)
        var p2 = NewObject("Phase2_FloorLifts", new Vector2(342f, -60f), LayerDefault, "Gimmicks");
        FloorLift("LiftA1", 330.5f, -60, 3f, 0f, p2.transform);
        FloorLift("LiftB1", 336.5f, -60, 6f, 3.5f, p2.transform);
        FloorLift("LiftA2", 342.5f, -60, 6f, 0.25f, p2.transform);
        FloorLift("LiftB2", 348.5f, -60, 3f, 3.75f, p2.transform);
        FloorLift("LiftA3", 353.5f, -60, 3f, 0.5f, p2.transform);
        p2.SetActive(false);

        var phase = NewObject("BossPhase", new Vector2(344f, -48f), LayerDefault, "Gimmicks");
        AddComp<BossPhaseSwitch>(phase, ("_boss", ec), ("_defeatedFlag", Flag), ("_threshold", 0.5f),
            ("_phase1", new Object[] { p1 }), ("_phase2", new Object[] { p2 }), ("_blinkTime", 1.6f),
            ("_phase2Message", "ノクターンが指揮棒を振り下ろした! 音符が消え、床がせり上がる……"));

        // ---- 撃破後: 赤ハサミ + 静止した音符の階段 (段差 5 = 二段ジャンプ) + 天井の点検口が開く
        var red = NewObject("RedScissors", new Vector2(342f, -58.6f), LayerInteractable, "Pickups");
        red.transform.localScale = Vector3.one * 0.85f;
        AddSprite(red, Art("hall/red_scissors.png"), OrderGimmick);
        Box(red, new Vector2(1.6f, 1.6f) / 0.85f, true);
        AddComp<UpgradePickup>(red, ("_pickupId", "A1_Pickup_AB_1"), ("_promptText", "拾う"), ("_upgrade", 2),
            ("_promptOffset", new Vector3(0, 1.3f, 0)),
            ("_hintMessage", "空中でもう一度ジャンプすると二段ジャンプ! 落ちながらジャンプを押し続けると滑空できる"));
        var redGlow = new GameObject("Glow");
        redGlow.transform.SetParent(red.transform, false);
        redGlow.transform.localScale = Vector3.one * 2.6f;
        AddSprite(redGlow, Art("fx/glow.png"), OrderGimmick - 1, null, new Color(1f, 0.3f, 0.35f, 0.55f));
        red.SetActive(false);

        var stairs = NewObject("AfterBoss_NoteStairs", new Vector2(348f, -48f), LayerDefault, "Gimmicks");
        NotePlatform("Stair1", new Vector2(350.5f, -55f), stairs.transform, new Color(1f, 0.92f, 0.75f), false);
        NotePlatform("Stair2", new Vector2(344.5f, -50f), stairs.transform, new Color(1f, 0.92f, 0.75f), false);
        NotePlatform("Stair3", new Vector2(352f, -45f), stairs.transform, new Color(1f, 0.92f, 0.75f), false);
        NotePlatform("Stair4", new Vector2(346.5f, -40f), stairs.transform, new Color(1f, 0.92f, 0.75f), false);
        stairs.SetActive(false);

        // 天井の点検口 (ST への穴 x345..354 の下端)。撃破で消える
        var hatch = NewObject("STHatch", new Vector2(349.5f, -36.5f), LayerGround, "Gimmicks");
        Box(hatch, new Vector2(9f, 1f), false);
        var hb = new GameObject("Grate");
        hb.transform.SetParent(hatch.transform, false);
        hb.transform.localPosition = new Vector3(0, -0.05f, 0);
        AddSprite(hb, Art("ferris/leg_crossbrace.png"), OrderGimmick, new Vector2(9f, 1f), new Color(0.95f, 0.75f, 0.5f));
        var lockArt = new GameObject("Lock");
        lockArt.transform.SetParent(hatch.transform, false);
        lockArt.transform.localPosition = new Vector3(0, -0.2f, 0);
        lockArt.transform.localScale = Vector3.one * 0.5f;
        AddSprite(lockArt, Art("props/boss_door_lamp_on.png"), OrderGimmick + 1, null, new Color(1f, 0.35f, 0.35f));
        // 開いた点検口 (左右に跳ね上がった格子)
        var opened = NewObject("STHatchOpen", new Vector2(349.5f, -36.5f), LayerDefault, "Props");
        foreach (var side in new[] { -1, 1 })
        {
            var flap = new GameObject(side < 0 ? "FlapL" : "FlapR");
            flap.transform.SetParent(opened.transform, false);
            flap.transform.localPosition = new Vector3(side * 4.3f, 1.8f, 0);
            flap.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            AddSprite(flap, Art("ferris/leg_crossbrace.png"), OrderProp, new Vector2(4f, 1f), new Color(0.75f, 0.6f, 0.45f));
        }
        opened.SetActive(false);

        // (撃破後の二段ジャンプの案内は削除: 案内文はチュートリアルの操作説明だけ残す。2026-10-03 ユーザー決定)

        var after = NewObject("AreaBossFlag", new Vector2(344f, -46f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(after, ("_flags", new[] { Flag }),
            ("_showWhenSet", new Object[] { red, stairs, opened }),
            ("_hideWhenSet", new Object[] { hatch, p1 }));

        // ================= 見た目: 大音楽堂
        // polish B: 奥の壁を音楽堂の部品で組む (Area1Gen/polish/hall_*)。壁紙 (Tiled) + 腰板 + 付け柱 (8 ごと) + 柱の間の桟敷席 3 +
        // 天井際の飾り幕 + 桟敷席の左右の壁灯。足場 (音符・昇降台) と見分けがつくよう、背景はすべて暗く沈める (HallBack の乗算)
        HallBackdrop();
        Shade(318f, -60f, 372f, -50f, 0.35f, false, -50);
        // パイプオルガン (舞台の奥) と緞帳
        HallArt("hall/organ_pipes.png", 364f, -57.3f, Vector3.one * 7.4f, -40);
        Glow(364f, -47f, 9f, new Color(1f, 0.75f, 0.45f, 0.22f), -41);
        HallArt("hall/stage_curtain.png", 357.4f, -36f, new Vector3(2.6f, 7.45f, 1f), -30);
        HallArt("hall/stage_curtain.png", 370.6f, -36f, new Vector3(2.6f, 7.45f, 1f), -30, null, true);
        var valance = NewObject("Valance", new Vector2(364f, -36.6f), LayerDefault, "Props");
        AddSprite(valance, Art("rock/decal_bunting.png"), -29, new Vector2(16f / 1.4f, 1f), new Color(1f, 0.55f, 0.6f)).transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        // スピーカーの山 (3 つのスピーカーから伸びた電線がここに集まる)
        HallArt("hall/speaker_stack.png", 359.6f, -57f, Vector3.one * 2.2f, -25, new Color(0.85f, 0.8f, 0.9f));
        HallArt("hall/speaker_stack.png", 368.6f, -57f, Vector3.one * 2.2f, -25, new Color(0.85f, 0.8f, 0.9f), true);
        HallArt("hall/speaker_stack.png", 327.6f, -60f, Vector3.one * 1.8f, -26, new Color(0.6f, 0.55f, 0.66f));
        for (var i = 0; i < 3; i++)
            PipeRun(318f, -37.0f - i * 0.45f, 358f, -37.0f - i * 0.45f, 0.18f, -27, new Color(0.25f, 0.2f, 0.3f));
        PipeRun(358.6f, -37.9f, 358.6f, -52f, 0.2f, -27, new Color(0.25f, 0.2f, 0.3f));

        // シャンデリアとスポットライト
        Chandelier(334.5f, -36f, 2.0f);
        Chandelier(342.5f, -36f, 1.8f);
        Spotlight(360.5f, -36f, -57f, new Color(1f, 0.9f, 0.65f, 0.12f));
        Spotlight(367.5f, -36f, -57f, new Color(1f, 0.6f, 0.85f, 0.12f));
        Spotlight(337f, -36.2f, -60f, new Color(0.7f, 0.6f, 1f, 0.09f));

        // 入口の踊り場: 吊り灯
        var lamp = Prop("props/boss_door_lamp_on.png", 320.5f, -45.3f, 0.7f, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        lamp.transform.rotation = Quaternion.Euler(0, 0, 180f);
        Glow(320.5f, -45.8f, 1.6f, WarmGlow, -7);

        // ================= ST の見た目: 噴水の真下の石の縦穴 (水が滴る配管、上から月明かり)
        TintRock(336, -40, 366, 2, new Color(0.72f, 0.78f, 0.95f));
        PipeRun(343.2f, -27f, 343.2f, -3f, 0.7f, -6, new Color(0.45f, 0.55f, 0.75f));
        PipeRun(358.8f, -27f, 358.8f, -3f, 0.7f, -6, new Color(0.45f, 0.55f, 0.75f));
        PipeRun(343.2f, -4.2f, 358.8f, -4.2f, 0.5f, -6, new Color(0.45f, 0.55f, 0.75f));
        Glow(349.5f, -1f, 6f, new Color(0.65f, 0.8f, 1f, 0.45f), -6);
        for (var i = 0; i < 4; i++)
            GuideLamp(i % 2 == 0 ? 359f : 342.6f, -25f + i * 6f, i * 0.2f, new Color(0.6f, 0.85f, 1f), false, 2.2f, "STLamp");

        // 石の井戸の壁と、上の噴水から伝い落ちる水
        var well = NewObject("STWell", new Vector2(351f, -19.5f), LayerDefault, "Background");
        AddSprite(well, Art("rock/rock_tex.png"), OrderRoomBackdrop + 1, new Vector2(18f / 2f, 33f / 2f), new Color(0.2f, 0.24f, 0.4f)).transform.localScale = new Vector3(2f, 2f, 1f);
        foreach (var (x, w) in new[] { (342.6f, 0.35f), (359.4f, 0.3f) })
        {
            var fall = NewObject("Trickle", new Vector2(x, -15f), LayerDefault, "Props");
            AddSprite(fall, Art("fx/water.png"), -6, new Vector2(w, 24f), new Color(0.6f, 0.82f, 1f, 0.2f));
        }
        Glow(349.5f, -27.5f, 4f, new Color(0.6f, 0.8f, 1f, 0.2f), -9);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
