using System.Reflection;
using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_U3 「コーヒーカップ広場」(U3, 屋外の闘技場 WaveArena A1_ArenaCups ×3 波) + 「砂糖菓子の隠し部屋」(U3s, 高床の売店の中)
// 左 = 観覧車の足元 A1_U4 (y6..18) / 右 = 月明かりの遊歩道 A1_U2 (y6..18)
// クリアすると左の壁沿いに梯子が下りる → 高床の売店のひび壁 (A1_Hidden_U3s) → ハートピン1
// 先に別の呼び出しで OpenOrCreate("A1_U3") を実行しておくこと。
public static class BuildU3
{
    const string EnemyPrefab = "Assets/Prefabs/Enemy.prefab";
    const string FlyerPrefab = "Assets/Prefabs/FlyingEnemy.prefab";

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // 回るコーヒーカップの足場 (MovingPlatform, layer 10, すり抜け)。縁の上面 ≈ 位置 +0.85
    static GameObject Cup(string name, Vector2 pos, Color tint)
    {
        var go = NewObject(name, pos, LayerGround, "Gimmicks");
        AddSprite(go, Art("platforms/coffee_cup_platform.png"), OrderGimmick, null, tint);
        var c = Box(go, new Vector2(3.2f, 0.4f), false, new Vector2(0f, 0.65f));
        c.usedByEffector = true;
        var e = go.AddComponent<PlatformEffector2D>();
        e.useOneWay = true;
        e.surfaceArc = 180f;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        return go;
    }

    static WaveArena.SpawnEntry E(bool flying, float ox, float oy, float scale = 1f, bool heavy = false, Color? tint = null) =>
        new WaveArena.SpawnEntry
        {
            Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(flying ? FlyerPrefab : EnemyPrefab),
            Consts = heavy ? AssetDatabase.LoadAssetAtPath<EnemyConsts>("Assets/Scripts/Enemy/EnemyConsts_Heavy.asset") : null,
            Tint = tint ?? Color.white,
            Offset = new Vector2(ox, oy),
            Scale = scale,
        };

    public static string Run()
    {
        var log = BuildBase("A1_U3");
        PlaceFerrisWheel(false, false);   // 観覧車の東の脚 (足元 x116) が広場の奥にそびえる
        // 隠し部屋への通路はレイアウト上の位置 (売店の床下) を岩で塞ぎ、横のひび壁から入るので背景板は要らない (空に浮いて見える)
        var bd = Group("Background").Find("Backdrop_U3-U3s");
        if (bd != null) Object.DestroyImmediate(bd.gameObject);

        // 東の A1_U2 との境の外壁は y30 まで (その上は空)。西 (A1_U4 側) は梯子の上から越えられないよう高いまま
        Clear(150, 30, 200, 90);

        // ================= U3 (x105..147, y0..33): 床 y0。左右の出口 (y6) へ 2 段
        Solid(105, 0, 108, 6); Solid(108, 0, 110, 3);
        Solid(144, 0, 147, 6); Solid(142, 0, 144, 3);

        // 固定の足場 (左右の縁と、中央の高い受け皿)
        OneWay(110, 115, 9);
        OneWay(137, 142, 9);
        OneWay(121, 131, 19);

        // 回るカップ: 中央の 2 客は円軌道 (中心 126,11 半径 5)、左右の 2 客は上下に往復
        var pink = new Color(1f, 1f, 1f);
        var mint = new Color(0.75f, 1f, 0.95f);
        var c1 = Cup("CupOrbitA", new Vector2(126f, 11f), pink);
        AddComp<MovingPlatform>(c1, ("_mode", 2), ("_radius", 5f), ("_angularSpeed", 22f), ("_phaseDegrees", 0f));
        var c2 = Cup("CupOrbitB", new Vector2(126f, 11f), mint);
        AddComp<MovingPlatform>(c2, ("_mode", 2), ("_radius", 5f), ("_angularSpeed", 22f), ("_phaseDegrees", 180f));
        var c3 = Cup("CupLiftL", new Vector2(117f, 4f), mint);
        AddComp<MovingPlatform>(c3, ("_mode", 0), ("_waypoints", new[] { Vector2.zero, new Vector2(0, 9f) }), ("_speed", 2.2f), ("_waitTime", 1.2f));
        var c4 = Cup("CupLiftR", new Vector2(135f, 13f), pink);
        AddComp<MovingPlatform>(c4, ("_mode", 0), ("_waypoints", new[] { Vector2.zero, new Vector2(0, -9f) }), ("_speed", 2.2f), ("_waitTime", 1.2f));
        // 中央の大きなティーポット台座 (飾り) = カップの回転軸
        Prop("platforms/gear_platform.png", 126f, 11f, 1.6f, -6, false, new Color(0.55f, 0.45f, 0.65f));

        // ---- 闘技場 (WaveArena A1_ArenaCups): 入ると両出口が閉じ、3 波
        var gateW = GateObject("GateW", new RectInt(103, 6, 2, 12)); gateW.SetActive(false);
        var gateE = GateObject("GateE", new RectInt(147, 6, 2, 12)); gateE.SetActive(false);
        var arenaGo = NewObject("Arena_A1_ArenaCups", new Vector2(126f, 15f), LayerIgnoreRaycast, "Gimmicks");
        Box(arenaGo, new Vector2(30f, 30f), true);   // x111..141 (スポーンの位置ではまだ閉じない)
        var arena = AddComp<WaveArena>(arenaGo, ("_flagId", "A1_ArenaCups"),
            ("_gates", new Object[] { gateW, gateE }),
            ("_startMessage", "コーヒーカップが回りだした! 出口が閉じた…… 敵の波をしのげ!"),
            ("_clearMessage", "敵の波をしのいだ! 出口が開いた…… 左の壁で何かが下りる音がした"));
        var waves = new[]
        {
            new WaveArena.Wave { Delay = 1f, Spawns = new[] { E(false, -10f, -14f), E(false, 0f, -14f), E(false, 10f, -14f) } },
            new WaveArena.Wave { Delay = 1.5f, Spawns = new[] { E(true, -8f, -5f), E(true, 8f, -5f), E(false, -14f, -14f), E(false, 14f, -14f) } },
            new WaveArena.Wave { Delay = 1.5f, Spawns = new[] { E(false, 0f, -13.6f, 1.4f, true, new Color(1f, 0.8f, 0.85f)),
                E(true, -12f, -1f), E(true, 12f, -1f) } },
        };
        typeof(WaveArena).GetField("_waves", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(arena, waves);
        EditorUtility.SetDirty(arena);

        // ---- クリアのご褒美: 左の壁沿いに梯子が下りる (y6 → y36) → 高床の売店の前の踊り場 (y39)
        var ladder = Ladder(106, 6, 36);
        ladder.SetActive(false);
        var act = NewObject("LadderFlag", new Vector2(106.5f, 4f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { "A1_ArenaCups" }), ("_showWhenSet", new Object[] { ladder }));

        // ================= U3s (x114..135, y39..57): 砂糖菓子の隠し部屋 = 高床の売店の中
        Solid(111, 36, 138, 60);      // 売店の外壁 (空に浮かぶ高床。下は支柱)
        Clear(114, 39, 135, 57);
        Solid(107, 36, 111, 39);      // 梯子の上の踊り場
        HiddenWall(111, 39, 114, 42, "A1_Hidden_U3s");
        // 壁を壊すまでは中が見えないよう、売店の外壁と同じ岩肌の幕で覆う (壊すとその場で消える)
        // (部屋の内側の縁タイルも隠すよう 1 セル広く。ひび壁 x111..114 y39..42 には掛けない)
        var cover = NewObject("U3sCover", new Vector2(124.5f, 48f), LayerDefault, "Props");
        var coverColor = new Color(0.72f, 0.56f, 0.68f);
        var cu = new GameObject("Upper"); cu.transform.SetParent(cover.transform, false);
        cu.transform.position = new Vector3(124.75f, 50f, 0);
        AddSprite(cu, Art("rock/rock_tex.png"), 5, new Vector2(22.5f, 16f), coverColor);
        var cl = new GameObject("Lower"); cl.transform.SetParent(cover.transform, false);
        cl.transform.position = new Vector3(125f, 40.3f, 0);
        AddSprite(cl, Art("rock/rock_tex.png"), 5, new Vector2(22f, 3.4f), coverColor);
        var coverAct = NewObject("U3sCoverFlag", new Vector2(124.5f, 46f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(coverAct, ("_flags", new[] { "A1_Hidden_U3s" }), ("_hideWhenSet", new Object[] { cover }));
        TintRock(111, 33, 138, 60, new Color(1f, 0.78f, 0.92f));   // 砂糖菓子色の外壁
        // 高床の支柱 (飾り)
        foreach (var x in new[] { 112.5f, 123f, 136.5f })
        {
            var s = NewObject("Stilt", new Vector2(x, 18f), LayerDefault, "Props");
            s.transform.localScale = new Vector3(0.6f, 1f, 1f);
            AddSprite(s, Art("ferris/support_leg.png"), -7, new Vector2(2f, 36f), new Color(0.75f, 0.55f, 0.75f));
        }

        // 中: 棚とハートピン1
        Prop("props/shop_stall.png", 121f, 39f, 1.3f, -6, false, new Color(0.9f, 0.82f, 0.95f));
        OneWay(115, 119, 43);
        OneWay(127, 132, 45);
        Solid(130, 39, 133, 40);      // 台
        var pin = KeyItemAt(131.5f, 40.7f, "ハートピン1", "A1_HeartPin1", "A1_Pickup_U3s_1", "props/heart_pin.png", "拾う");
        pin.GetComponent<BoxCollider2D>().offset = Vector2.zero;
        HangLamp(118f, 57f); HangLamp(126f, 57f); HangLamp(133f, 57f);

        // ---- polish A: 隠し部屋の中を「閉店した砂糖菓子の売店の倉庫」に。既存の絵だけで飾る (当たり無し)
        var u3sBack = Group("Background").Find("Backdrop_U3s");
        if (u3sBack != null) u3sBack.GetComponent<SpriteRenderer>().color = new Color(0.72f, 0.5f, 0.66f);   // 奥の壁を砂糖菓子色に
        foreach (var (cx, flip) in new[] { (115.4f, false), (133.6f, true) })   // 左右の壁の緞帳 (ピンク)
            Prop("hall/stage_curtain.png", cx, 57f, 2.2f, -8, flip, new Color(1f, 0.62f, 0.82f));
        Prop("hall/chandelier.png", 124.5f, 57f, 1.5f, -7, false, new Color(1f, 0.85f, 0.95f));
        Glow(124.5f, 54.8f, 3.2f, new Color(1f, 0.75f, 0.9f, 0.14f), -9);
        foreach (var (bx, by) in new[] { (119.6f, 55.7f), (129.4f, 55.7f) })
        {
            var b = Prop("rock/decal_bunting.png", bx, by, 1f, -6);
            b.transform.localScale = new Vector3(1.9f, 1.1f, 1f);
        }
        foreach (var (px, py) in new[] { (118.5f, 49.5f), (129.8f, 50.5f) })
            Prop("rock/decal_poster.png", px, py, 1.1f, OrderWallDecor, false, new Color(1f, 0.85f, 0.95f));
        // 在庫の木箱 (入口の脇と棚の下)
        // 壊せる木箱 (PendingChanges 28)。下 2 個の上に 1 個
        Crate(116.1f, 39f);
        Crate(117.1f, 39f);
        Crate(116.6f, 40f);
        Crate(127.6f, 39f);
        // ハートピン1を照らす舞台照明
        Prop("hall/spotlight.png", 131.5f, 57f, 1.1f, -7, false, new Color(1f, 0.9f, 0.95f));
        Glow(131.5f, 41f, 2.2f, new Color(1f, 0.55f, 0.75f, 0.3f), -7);

        // ================= 飾り: 外周の街灯と旗
        LampPost(108.5f, 6f, 4f); LampPost(143.5f, 6f, 4f);
        var bun = Prop("rock/decal_bunting.png", 126f, 25f, 1f, -6);
        bun.transform.localScale = new Vector3(6f, 1.5f, 1f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_U3") + "\n";
        return log + Finish();
    }
}
