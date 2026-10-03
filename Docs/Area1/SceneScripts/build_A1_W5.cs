using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W5 「回るゴンドラ」(W5, 観覧車の内側 x24..90 y180..234) + 「外れたゴンドラ」(W5g x93..108 y210..222, 二段ジャンプ)
// 下 = 風の外周 A1_W4 (床の口 x66..78、格子で塞いで ↓+ジャンプで降りる) / 上 = 中心軸の機械室 A1_W6 (天井の口 x45..57)
// 大観覧車のスポークに取り付けた「整備用の小さな回転輪」3 基 (A/B/C) に吊られたゴンドラ 8 台 (MovingPlatform Orbit) を乗り継ぐ。
// 輪 A (3 台, 反時計回り) → 踊り場 L1 → 輪 B (3 台, 時計回り) → L2 → 輪 C (2 台, 反時計回り) → L3..L6 → 天井の口。
// ゴンドラの屋根に乗る (すり抜け)。飛び移れる角度は限られるので回転のリズムを読む。落ちても下の踊り場か下の輪が受ける。
// 右の継ぎ目の上に外れたゴンドラ (W5g): 受け皿の鉄骨 (y208) から口 (y213) まで 5 = 二段ジャンプでだけ届く。糸の大束×100。
// 先に別の呼び出しで OpenOrCreate("A1_W5") を実行しておくこと。
public static class BuildW5
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Arm = new Color(0.78f, 0.62f, 0.86f);
    static readonly Color Bracket = new Color(0.45f, 0.38f, 0.58f);

    const float Hang = 1.5f;      // ゴンドラの絵の上端 (吊り金具) = 中心 +1.5 (scale 1.5)
    const float RoofTop = 0.44f;  // 屋根の上面 = 中心 +0.44

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // 足場を吊るワイヤー: p から観覧車の中心 (54,252) の方へ、天井 (y234) か長さ 10 まで
    static void HubWire(Vector2 p)
    {
        var dir = (new Vector2(54f, 252f) - p).normalized;
        var len = Mathf.Min(10f, (234f - p.y) / Mathf.Max(0.2f, dir.y));
        var w = NewObject("HubWire", p + dir * (len * 0.5f), LayerDefault, "Props");
        w.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        AddSprite(w, Art("ferris/spoke_beam.png"), -6, new Vector2(len, 0.14f), new Color(0.7f, 0.66f, 0.8f, 0.9f));
    }

    /// <summary>
    /// 回転輪:hub = 輪の軸 (腕の付け根)。ゴンドラ (足場) の軌道中心 = hub - (0, Hang)、半径 r。
    /// 腕・電球・軸の絵は回転する親 (MovingPlatform Orbit 半径 0 + _spinSpeed) の子で、ゴンドラと同じ角速度で回る。
    /// </summary>
    static void Ring(string name, Vector2 hub, float r, float omega, int count, float phase0, Color tint)
    {
        // ---- 回る絵 (当たり無し)
        var spin = NewObject(name + "_Spin", hub, LayerDefault, "Gimmicks");
        spin.transform.rotation = Quaternion.Euler(0, 0, phase0);
        spin.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(spin, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", omega));
        for (var k = 0; k < count; k++)
        {
            var a = k * 360f / count;
            var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            var arm = new GameObject("Arm" + k);
            arm.transform.SetParent(spin.transform, false);
            arm.transform.localPosition = dir * (r * 0.5f);
            arm.transform.localRotation = Quaternion.Euler(0, 0, a);
            AddSprite(arm, Art("ferris/spoke_beam.png"), -30, new Vector2(r, 0.5f), Arm);
        }
        // polish B: 輪の縁 = rim_segment_near (電球付きの外周の弧, 弧の半径 4841px・約 54°、両端は細い) を 8 枚 (45° ごと, 端を重ねて隙間なく)、半径 r に合わせて回る親に並べる
        // (弧の中心は絵の中心から 4619px 下 → 倍率 k で絵の中心は軸から 4619/546·k)
        var rimK = r / (4841f / 546f);
        var rimOff = 4619f / 546f * rimK;
        for (var k = 0; k < 8; k++)
        {
            var am = (k + 0.5f) * 360f / 8f;
            var seg = new GameObject("RimSeg" + k);
            seg.transform.SetParent(spin.transform, false);
            seg.transform.localPosition = new Vector2(Mathf.Cos(am * Mathf.Deg2Rad), Mathf.Sin(am * Mathf.Deg2Rad)) * rimOff;
            seg.transform.localRotation = Quaternion.Euler(0, 0, am - 90f);
            seg.transform.localScale = Vector3.one * rimK;
            AddSprite(seg, Art("polish/rim_segment_near.png"), -31, null, new Color(0.88f, 0.84f, 0.95f));
        }
        var axle = new GameObject("Axle");
        axle.transform.SetParent(spin.transform, false);
        axle.transform.localScale = Vector3.one * 0.36f;
        AddSprite(axle, Art("ferris/hub_axle.png"), -28, null, new Color(0.9f, 0.85f, 0.95f));

        // ---- 輪をいちばん近い本物のスポークに留める腕木 (大観覧車の中心 (54,252)、スポーク 16 本)
        var c = new Vector2(54f, 252f);
        var d = hub - c;
        var ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        var snapped = Mathf.Round(ang / 22.5f) * 22.5f;
        var sd = new Vector2(Mathf.Cos(snapped * Mathf.Deg2Rad), Mathf.Sin(snapped * Mathf.Deg2Rad));
        var foot = c + sd * Vector2.Dot(d, sd);
        var len = Vector2.Distance(hub, foot);
        if (len > 0.5f)
        {
            var mid = (hub + foot) * 0.5f;
            var br = Truss(mid.x, mid.y - len * 0.5f, mid.y + len * 0.5f, 0.8f, -31, Bracket,
                Mathf.Atan2(foot.y - hub.y, foot.x - hub.x) * Mathf.Rad2Deg - 90f);
            br.name = name + "_Bracket";
        }

        // ---- ゴンドラ (足場)
        var center = new GameObject(name + "_Center").transform;
        center.SetParent(Group("Gimmicks"), false);
        center.position = hub - new Vector2(0, Hang);
        for (var k = 0; k < count; k++)
        {
            var ph = phase0 + k * 360f / count;
            var pos = (Vector2)center.position + new Vector2(Mathf.Cos(ph * Mathf.Deg2Rad), Mathf.Sin(ph * Mathf.Deg2Rad)) * r;
            var g = NewObject($"{name}_Gondola{k}", pos, LayerGround, "Gimmicks");
            var art = new GameObject("Art");
            art.transform.SetParent(g.transform, false);
            art.transform.localScale = Vector3.one * 1.5f;
            AddSprite(art, Art("platforms/gondola_platform.png"), OrderGimmick, null, tint);
            var col = Box(g, new Vector2(3.1f, 0.4f), false, new Vector2(0f, RoofTop - 0.2f));
            col.usedByEffector = true;
            var e = g.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 180f;
            g.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            AddComp<MovingPlatform>(g, ("_mode", 2), ("_orbitCenter", center), ("_radius", r),
                ("_angularSpeed", omega), ("_phaseDegrees", ph));
        }
    }

    public static string Run()
    {
        var log = BuildBase("A1_W5");
        var wheel = PlaceFerrisWheel(true, false, new Color(0.72f, 0.68f, 0.82f));   // 本物のスポーク (247.5/270/292.5 度) が奥を貫く
        // 脚 (A フレーム) は観覧車の面より奥にあるので、さらに暗く沈める (手前の輪・足場と見分ける)
        var legs = wheel.transform.Find("Legs");
        if (legs != null)
            foreach (var sr in legs.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color *= new Color(0.5f, 0.46f, 0.6f);
        PlateWallsAll();
        RockDecor(0.4f);

        // ================= 外の岩を削って空を見せる: 左右の壁は厚さ 3 の鉄板の柱だけ残す
        Clear(-10, 177, 21, 300);
        Clear(93, 177, 150, 207);
        Clear(111, 207, 150, 300);
        Clear(93, 225, 111, 300);
        Clear(0, 100, 150, 165);
        // 天井 = 中心軸の機械室 (A1_W6) の床下。口 x45..57 の左右だけ鉄板の床下
        Solid(30, 234, 45, 238);
        Solid(57, 234, 72, 238);

        // ================= 床 y180。下の口 (x66..78, A1_W4 へ) には格子 (↓+ジャンプで降りる)
        Girder(66, 78, 180, new Color(0.62f, 0.55f, 0.72f), "Grate");

        // ================= 回転輪 (ゴンドラ 8 台)
        var pink = Color.white;
        var mint = new Color(0.78f, 1f, 0.95f);
        var lilac = new Color(0.9f, 0.82f, 1f);
        Ring("RingA", new Vector2(58f, 191.5f), 6f, 14f, 3, 270f, pink);    // 反時計回り
        Ring("RingB", new Vector2(38f, 205.5f), 6f, -12f, 3, 270f, mint);   // 時計回り
        Ring("RingC", new Vector2(62f, 217.5f), 6f, 14f, 2, 270f, lilac);   // 反時計回り

        // ================= 固定の踊り場 (鉄骨, すり抜け)
        Girder(46, 53, 183, Steel, "L0");     // 乗り場: 輪 A の真下を通るゴンドラへ
        Girder(40, 48, 196, Steel, "L1");     // 輪 A の左上 (135 度前後で飛び移る) / 輪 B の乗り場
        Girder(47, 53, 203, Steel, "C1");     // L2 から落ちた時の受け皿 (輪 B の右へ乗り直せる)
        Girder(46, 57, 211, Steel, "L2");     // 輪 B の右上 (45 度前後) / 輪 C の乗り場
        Girder(66, 86, 208, Steel, "R");      // 輪 C の右下の受け皿。右端が外れたゴンドラの真下
        Girder(40, 53, 224, Steel, "L3");     // 輪 C の左上 (110 度前後)
        Girder(48, 56, 226, Steel, "L4");
        Girder(44, 50, 229, Steel, "L5");
        Girder(47, 55, 232, Steel, "L6");     // 天井の口の真下
        MoveSpawn("A1_W6", 51f, 233.5f);       // 機械室から降りてきた時は L6 の上

        // 踊り場を吊るワイヤー: 両端から観覧車の中心軸 (54,252) の方へ張る (宙に浮いて見えないように)
        foreach (var (x, top) in new[] { (46.6f, 183f), (52.4f, 183f), (40.6f, 196f), (47.4f, 196f), (47.6f, 203f), (52.4f, 203f),
                     (46.6f, 211f), (56.4f, 211f), (66.6f, 208f), (85.4f, 208f), (40.6f, 224f), (52.4f, 224f),
                     (48.6f, 226f), (55.4f, 226f), (44.6f, 229f), (49.4f, 229f), (47.6f, 232f), (54.4f, 232f) })
            HubWire(new Vector2(x, top - 0.2f));

        // ================= W5g: 外れたゴンドラ (床 y213)。糸の大束×100
        Ground(93, 108, 213, 3);
        var stuck = Prop("platforms/gondola_platform.png", 97.5f, 216.2f, 1.5f, OrderProp, false, new Color(0.85f, 0.8f, 0.9f));
        stuck.transform.rotation = Quaternion.Euler(0, 0, -14f);
        var hanger = Prop("ferris/gondola_hanger.png", 92.6f, 214.9f, 1.3f, OrderProp, false, new Color(0.8f, 0.72f, 0.88f));
        hanger.transform.rotation = Quaternion.Euler(0, 0, 100f);  // 口の縁に引っかかった吊り金具 (下の鉄骨から見える目印)
        ThreadPickupAt(104f, 214.2f, 100, "A1_Pickup_W5g_1");
        HangLamp(97f, 222f, 0.55f);

        // ================= 飾り: 天井 (機械室の床下) の作業灯
        HangLamp(36f, 234f); HangLamp(42f, 234f); HangLamp(60f, 234f); HangLamp(68f, 234f);
        LampPost(86f, 180f, 4.5f);
        LampPost(27f, 180f, 4.5f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
