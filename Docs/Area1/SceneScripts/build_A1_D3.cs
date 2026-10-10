using System.Reflection;
using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_D3 「歯車の胃袋」(D3 x243..267 y-201..-120) + 「歯車の横穴」(D3a x270..288 y-144..-129) + 「ネズミの巣」(D7 x270..297 y-174..-153)
// 西 = 蒸気管の迷路 A1_D5 (x240..243, 床 y-141) / 東の底 = ピストンの間 A1_D4 (x267..270, 床 y-198)
// D3: エリアで一番深い縦穴。回る歯車 6 基 (歯車の絵が回り、縁の腕木に吊った足場 3 枚が円軌道で一緒に回る = MovingPlatform Orbit) を
//     乗り継いで 57 単位降りる。歯車は西 (x249) / 東 (x255) 交互、縦 8.5〜9 ごと、回る向きも交互。
//     乗ったまま待てば次の歯車へ跳び移れる角度が必ず来る (上り側に乗れば帰り道にもなる)。踏み外しても底 (y-198) へ落ちるだけ。
//     壁に張り付く敵 ×2 (浮遊敵を壁沿いにゆっくり上下させ、近づくと飛びかかる)。
//     D3a: 東の岩棚 (y-141) の 6 上 (y-135) に開いた横穴 = 二段ジャンプでだけ届く。糸の大束 ×60。
//     D7: 東の岩棚 (y-171) の奥のネズミの巣 = 任意の WaveArena (A1_RatNest)、小ネズミの群れ ×3 波。全滅で宝箱 (糸巻き3)。
// 先に別の呼び出しで OpenOrCreate("A1_D3") を実行しておくこと。
public static class BuildD3
{
    static readonly Color MossTint = new Color(0.55f, 0.62f, 0.68f, 0.85f);
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Guide = new Color(0.4f, 1f, 0.78f);
    static readonly Color Red = new Color(1f, 0.32f, 0.3f);
    const string EnemyPrefab = "Assets/Prefabs/Enemy.prefab";
    const string RatConsts = "Assets/Prefabs/Area1/EnemyConsts_A1Rat.asset";
    const string ClingerConsts = "Assets/Prefabs/Area1/EnemyConsts_A1Clinger.asset";

    public const float GearR = 3.5f;       // 足場の軌道半径 (足場の上面 = 軌道上の点)
    public const float GearOmega = 20f;    // deg/s
    const float PlateW = 2.4f;

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    /// <summary>
    /// 回る歯車の足場。歯車の絵・腕木・軸ピン (当たり無し) は回転する親 (MovingPlatform Orbit 半径 0 + _spinSpeed) の子。
    /// 足場 (lift_platform ×0.8, すり抜け) は同じ角速度の円軌道 (常に水平) で、上面 = 軌道上の点 = 腕木の先のピン。
    /// omega 正 = 反時計回り。
    /// </summary>
    public static void Gear(string name, Vector2 c, float omega, float phase0, Color tint, int count = 3, float r = GearR, float artScale = 1.35f)
    {
        var spin = NewObject(name + "_Spin", c, LayerDefault, "Gimmicks");
        spin.transform.rotation = Quaternion.Euler(0, 0, phase0);
        spin.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(spin, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", omega));
        var art = new GameObject("GearArt");
        art.transform.SetParent(spin.transform, false);
        art.transform.localScale = Vector3.one * artScale;
        AddSprite(art, Art("platforms/gear_platform.png"), -6, null, tint);
        var armCol = new Color(0.62f, 0.55f, 0.68f);
        for (var k = 0; k < count; k++)
        {
            var a = k * 360f / count;
            var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            var arm = new GameObject("Arm" + k);
            arm.transform.SetParent(spin.transform, false);
            arm.transform.localPosition = dir * (r * 0.5f + 0.6f);
            arm.transform.localRotation = Quaternion.Euler(0, 0, a);
            AddSprite(arm, Art("ferris/spoke_beam.png"), -5, new Vector2(r - 1.2f, 0.42f), armCol);
            var pin = new GameObject("Pin" + k);
            pin.transform.SetParent(spin.transform, false);
            pin.transform.localPosition = dir * r;
            pin.transform.localScale = Vector3.one * 0.07f;
            AddSprite(pin, Art("ferris/hub_axle.png"), -4, null, new Color(0.85f, 0.8f, 0.9f));
        }
        var hub = new GameObject("Hub");
        hub.transform.SetParent(spin.transform, false);
        hub.transform.localScale = Vector3.one * 0.22f;
        AddSprite(hub, Art("ferris/hub_axle.png"), -4, null, new Color(0.8f, 0.72f, 0.85f));

        var center = new GameObject(name + "_Center").transform;
        center.SetParent(Group("Gimmicks"), false);
        center.position = c;
        for (var k = 0; k < count; k++)
        {
            var ph = phase0 + k * 360f / count;
            var pos = c + new Vector2(Mathf.Cos(ph * Mathf.Deg2Rad), Mathf.Sin(ph * Mathf.Deg2Rad)) * r;
            var g = NewObject($"{name}_Plate{k}", pos, LayerGround, "Gimmicks");
            var pa = new GameObject("Art");
            pa.transform.SetParent(g.transform, false);
            pa.transform.localPosition = new Vector3(0, -0.1f, 0);
            pa.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            AddSprite(pa, Art("platforms/lift_platform.png"), OrderGimmick, null, new Color(0.95f, 0.9f, 0.95f));
            var col = Box(g, new Vector2(PlateW, 0.4f), false, new Vector2(0f, -0.2f));
            col.usedByEffector = true;
            var e = g.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 180f;
            g.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            AddComp<MovingPlatform>(g, ("_mode", 2), ("_orbitCenter", center), ("_radius", r),
                ("_angularSpeed", omega), ("_phaseDegrees", ph));
        }
    }

    // ゆっくり回る奥の歯車 (当たり無し)
    public static void BgGear(float x, float y, float scale, float speed, float k = 0.3f, int order = -46)
    {
        var g = NewObject("BgGear", new Vector2(x, y), LayerDefault, "Props");
        g.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(g, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", speed));
        var art = new GameObject("Art");
        art.transform.SetParent(g.transform, false);
        art.transform.localScale = Vector3.one * scale;
        AddSprite(art, Art("platforms/gear_platform.png"), order, null, new Color(k, k * 0.85f, k * 1.25f));
    }

    static void WallLamp(float x, float y, bool flip = false, float glow = 1f)
    {
        Prop("rock/decal_lamp_wall.png", x, y, 1.1f, -6, flip);
        Glow(x + (flip ? -0.15f : 0.15f), y + 0.05f, 1.5f, new Color(1f, 0.7f, 0.4f, 0.3f * glow), -7);
    }

    // 壁に張り付く敵 (浮遊敵を壁沿いにゆっくり上下させ、近づくと飛びかかる)
    static GameObject Clinger(string name, float x, float y, bool faceLeft)
    {
        var e = Enemy(x, y, true, 0.9f, false, name);
        Set(e.GetComponent<EnemyController>(), "_consts", AssetDatabase.LoadAssetAtPath<EnemyConsts>(ClingerConsts));
        var fp = e.GetComponent<FloatingPatrolMotion>();
        if (fp != null) { Set(fp, "_hoverAmplitude", 2.4f); Set(fp, "_hoverFrequency", 0.7f); }
        var sr = e.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.8f, 0.95f, 0.75f);
        return e;
    }

    static WaveArena.SpawnEntry Rat(float ox, float oy, float scale = 0.6f, bool big = false) =>
        new WaveArena.SpawnEntry
        {
            Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab),
            Consts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(big ? "Assets/Scripts/Enemy/EnemyConsts_Heavy.asset" : RatConsts),
            Tint = big ? new Color(0.75f, 0.62f, 0.6f) : new Color(0.72f, 0.66f, 0.6f),
            Offset = new Vector2(ox, oy),
            Scale = scale,
        };

    public static string Run()
    {
        var log = BuildBase("A1_D3");
        RockDecor(0.7f);

        // ================= D3 縦穴の骨組み
        Solid(243, -126, 267, -120);          // 天井 (上の機械室: 入口の上 y-141..-126)
        Ground(243, 250, -141);               // 入口の岩棚 (A1_D5 の出口と同じ床 y-141)
        Solid(260, -143, 267, -141);          // 東の岩棚 (D3a の横穴の 6 下)
        Solid(267, -138, 270, -135);          // 横穴の口の床を y-135 に (= 東の岩棚から 6 上: 二段ジャンプ)
        Solid(261, -173, 267, -171);          // 東の岩棚 (D7 ネズミの巣の口, 床 y-171)
        PipePlatform(258, 260, -168, Rust, "Pipe_NestStep");   // 岩棚 → 歯車 G3 へ戻る踏み台
        Ground(243, 267, -198);               // 底 (A1_D4 への出口 y-198..-189)
        PipePlatform(251, 255, -195, Rust, "Pipe_Bottom");     // 底 → 歯車 G6 (帰り道)
        Solid(243, -198, 247, -196);          // 底の西の瓦礫の段

        // ================= 回る歯車 6 基 (西 x249 / 東 x255 交互、時計回り = 負)
        var tA = new Color(0.82f, 0.8f, 0.95f);
        var tB = new Color(0.9f, 0.78f, 0.86f);
        Gear("G1", new Vector2(255f, -145.5f), -GearOmega, 90f, tA);
        Gear("G2", new Vector2(249f, -154f), GearOmega, 30f, tB);
        Gear("G3", new Vector2(255f, -162.5f), -GearOmega, 150f, tA);
        Gear("G4", new Vector2(249f, -171f), GearOmega, 60f, tB);
        Gear("G5", new Vector2(255f, -179.5f), -GearOmega, 120f, tA);
        Gear("G6", new Vector2(249f, -188.5f), GearOmega, 0f, tB);

        // ================= 壁に張り付く敵 ×2
        Clinger("WallClingerW", 244.4f, -162.5f, false);
        Clinger("WallClingerE", 265.6f, -182f, true);

        // ================= D3a 歯車の横穴 (床 y-135, 天井 y-131)
        Solid(270, -144, 288, -135);
        Solid(270, -131, 288, -129);
        Solid(285, -135, 288, -131);
        ThreadPickupAt(283.2f, -134.2f, 60, "A1_Pickup_D3a_1");
        Crate(279.5f, -135f);  // 2026-10-04 PendingChanges 28: 飾りの木箱は壊せる木箱に (見た目が同じで紛らわしいため)
        Prop("platforms/gear_platform.png", 274.6f, -133.4f, 0.55f, -6, false, new Color(0.4f, 0.36f, 0.48f)).transform.rotation = Quaternion.Euler(0, 0, 18f);
        Prop("rock/decal_moss_glow.png", 281f, -131.8f, 0.7f, -6, false, MossTint);   // polish A: 光る苔は暗く
        Glow(281f, -132f, 1.3f, new Color(0.3f, 1f, 0.85f, 0.12f), -7);

        // ================= D7 ネズミの巣 (床 y-171, 天井 y-158)。任意の闘技場
        Solid(270, -174, 297, -171);
        Solid(270, -158, 297, -153);
        Solid(294, -171, 297, -158);
        PipePlatform(274, 279, -167, Rust);
        PipePlatform(286, 292, -167, Rust);
        PipePlatform(279, 286, -163, Rust);
        var gate = GateObject("NestGate", new RectInt(268, -171, 2, 9));
        gate.SetActive(false);
        var arenaGo = NewObject("Arena_A1_RatNest", new Vector2(283f, -164.5f), LayerIgnoreRaycast, "Gimmicks");
        Box(arenaGo, new Vector2(20f, 13f), true);   // x273..293
        var arena = AddComp<WaveArena>(arenaGo, ("_flagId", "A1_RatNest"), ("_gates", new Object[] { gate }),
            ("_startMessage", "チュウチュウ…… 巣穴からネズミが湧いてきた! 口が閉じた!"),
            ("_clearMessage", "ネズミの群れを追い払った! 巣の奥で何かが光っている……"));
        var fy = -5.8f;   // 床 (y-171) の上 (闘技場の中心 y-164.5 から)
        var waves = new[]
        {
            new WaveArena.Wave { Delay = 1f, Spawns = new[] { Rat(-7f, fy), Rat(-1f, fy), Rat(5f, fy), Rat(9f, fy) } },
            new WaveArena.Wave { Delay = 1.5f, Spawns = new[] { Rat(-6.5f, -1.8f), Rat(6f, -1.8f), Rat(0f, 1.6f), Rat(-3f, fy), Rat(8f, fy) } },
            new WaveArena.Wave { Delay = 1.5f, Spawns = new[] { Rat(2f, fy, 1.1f, true), Rat(-8f, fy), Rat(-4f, fy), Rat(8f, fy),
                Rat(-6.5f, -1.8f), Rat(6f, -1.8f) } },
        };
        typeof(WaveArena).GetField("_waves", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(arena, waves);
        EditorUtility.SetDirty(arena);
        // ご褒美: 全滅で宝箱 (糸巻き3)
        var chest = KeyItemAt(291.5f, -171f, "糸巻き3", "A1_Spool3", "A1_Pickup_D7_1", "props/treasure_chest.png", "開ける");
        chest.SetActive(false);
        var chestGlow = Glow(291.5f, -170.2f, 1.8f, new Color(1f, 0.85f, 0.45f, 0.35f), -6);
        chestGlow.SetActive(false);
        var act = NewObject("RatNestReward", new Vector2(291.5f, -168f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { "A1_RatNest" }), ("_showWhenSet", new Object[] { chest, chestGlow }));
        // 巣の飾り: 巣穴 (暗いにじみ)・かじられた荷物・藁代わりの糸くず
        foreach (var p in new[] { new Vector2(272.6f, -170.2f), new Vector2(293.2f, -169.6f), new Vector2(283f, -158.9f), new Vector2(277f, -159.1f) })
            Glow(p.x, p.y, 1.1f, new Color(0.02f, 0.01f, 0.03f, 0.85f), -6);
        // かじられた荷物 = 壊せる木箱 (PendingChanges 28)。ネズミの出る位置 (中心 283 から -8,-7,-4,-1,2,5,8,9) を避ける
        Crate(273.6f, -171f);
        Crate(286.6f, -171f);
        Prop("rock/decal_chain_hanging.png", 281f, -159.6f, 0.6f, -7, false, new Color(0.4f, 0.38f, 0.48f));
        WallLamp(284.5f, -161.2f, false, 0.6f);

        // ================= 入口 (本道の灯の続き) と縦穴の飾り
        GuideLamp(244.5f, -126f, 0f, Guide, true, 1.5f, "GuideLamp0");
        GuideLamp(248.6f, -126f, -0.11f, Guide, true, 1.5f, "GuideLamp1");
        // 穴の縁の赤い警告灯 (ここから下は歯車)
        GuideLamp(249.6f, -140.4f, 0f, Red, false, 1.8f, "WarnLampEdge");
        GuideLamp(260.4f, -140.4f, 0.5f, Red, false, 1.8f, "WarnLampEast");
        // 上の機械室: 奥の大歯車と配管
        BgGear(252f, -131.5f, 3.2f, 6f, 0.3f);
        BgGear(262.5f, -129f, 2f, -10f, 0.26f);
        BgGear(245.5f, -133f, 1.4f, -14f, 0.24f);
        PipeRun(243f, -127.2f, 267f, -127.2f, 0.9f, -6, BgPipe);
        PipeRun(243f, -128.4f, 260f, -128.4f, 0.5f, -7, Dim(BgPipe, 0.75f));
        // 縦穴の壁を這う管・奥の暗い歯車 (下ほど暗い)
        PipeRun(243.9f, -198f, 243.9f, -141f, 0.9f, -6, BgPipe);
        PipeRun(266.1f, -198f, 266.1f, -143f, 0.9f, -6, BgPipe);
        PipeRun(245.2f, -196f, 245.2f, -150f, 0.5f, -7, Dim(BgPipe, 0.8f));
        PipeRun(264.8f, -170f, 264.8f, -144f, 0.5f, -7, Dim(BgPipe, 0.8f));
        BgGear(260.5f, -153f, 2.6f, 8f, 0.2f);
        BgGear(244.5f, -177f, 2.2f, -9f, 0.17f);
        BgGear(262f, -190f, 3f, 6f, 0.15f);
        BgGear(252f, -167f, 1.6f, -12f, 0.18f);
        // 吊り鎖
        foreach (var x in new[] { 246.5f, 258.5f, 264.5f })
        {
            var ch = Prop("rock/decal_chain_hanging.png", x, -127.6f, 0.6f, -7, false, new Color(0.4f, 0.38f, 0.48f));
            ch.transform.localScale = new Vector3(0.6f, 0.9f + (x % 3) * 0.2f, 1f);
        }
        // 灯: 上は暖かく、下ほど少ない
        WallLamp(244.3f, -137f, false, 1f);
        WallLamp(265.7f, -137.5f, true, 0.9f);
        WallLamp(265.7f, -167.5f, true, 0.6f);
        WallLamp(244.3f, -186f, false, 0.4f);
        // 底: 錆びた瓦礫・こぼれた油・底の灯 (赤)
        Crate(245.2f, -196f);  // 2026-10-04 PendingChanges 28: 飾りの木箱は壊せる木箱に (見た目が同じで紛らわしいため)
        Prop("platforms/gear_platform.png", 259.5f, -197.6f, 0.7f, -7, false, new Color(0.28f, 0.25f, 0.34f)).transform.rotation = Quaternion.Euler(0, 0, 70f);
        GuideLamp(266.2f, -192.5f, 0f, Guide, false, 1.5f, "GuideLampExit");

        // ================= 暗さ: 下へ行くほど沈む
        TintRockGradient(230, -205, 300, -126, new Color(0.3f, 0.27f, 0.4f), new Color(0.7f, 0.66f, 0.8f));
        TintRock(230, -126, 300, -110, new Color(0.7f, 0.66f, 0.8f));
        Shade(243f, -201f, 267f, -150f, 0.7f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(258f, -134f, true, name: "GearFlyer");     // 縦穴の上の方の空中 (壁の 2 体より上)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_D3") + "\n";
        return log + Finish();
    }
}
