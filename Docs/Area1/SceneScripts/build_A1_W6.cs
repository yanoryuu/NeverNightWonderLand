using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W6 「中心軸の機械室」(W6, 観覧車の中心軸 x36..66 y240..270) + 「逆さゴンドラの寄り道」(W7 x69..102 y252..276)
// 下 = 回るゴンドラ A1_W5 (床の口 x45..57、格子) / 上 = てっぺんへの梯子 A1_W8 (天井の口 x45..57)
// 西 = 非常用リフトの上の乗り口 (A1_W1 へ。この部屋のレバーで A1_LiftWheel を立てると往復できる)
// 部屋の奥にゆっくり回る巨大な軸 (hub_axle)。床にセーブ (ベンチ)。左の段 → リフトの踊り場 (y249) → 軸の左を登って
// 軸の上の渡り (y264) → 天井の口 (A1_W8)。渡りの右から東の棚 (y258) → W7。
// W7: 床はトゲ。逆さに吊られたゴンドラは「下のレール」と「上を向いた底」の 2 段に乗れる (すり抜け)。
// レールだけでは高さが足りず、ゴンドラの中をすり抜けて底の上に立つのが鍵。仕切りの壁を越えた奥の棚の宝箱に道具「針山」。
// 先に別の呼び出しで OpenOrCreate("A1_W6") を実行しておくこと。
public static class BuildW6
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Rail = new Color(0.62f, 0.52f, 0.72f);
    static readonly Color Chain = new Color(0.55f, 0.5f, 0.62f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // ゆっくり回る絵 (MovingPlatform Orbit 半径 0 + _spinSpeed。当たり無し)
    static GameObject Spinner(string name, Vector2 pos, float degPerSec, string group = "Props")
    {
        var go = NewObject(name, pos, LayerDefault, group);
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        AddComp<MovingPlatform>(go, ("_mode", 2), ("_radius", 0f), ("_angularSpeed", 0f), ("_spinSpeed", degPerSec));
        return go;
    }

    static GameObject Gear(Vector2 pos, float scale, float speed, Color color, int order = -46)
    {
        var g = Spinner("Gear", pos, speed);
        var art = new GameObject("Art");
        art.transform.SetParent(g.transform, false);
        art.transform.localScale = Vector3.one * scale;
        AddSprite(art, Art("platforms/gear_platform.png"), order, null, color);
        return g;
    }

    // すり抜けの当たり (子)。top = 上面
    static void OneWayBox(GameObject parent, string name, float cx, float top, float width)
    {
        var go = new GameObject(name) { layer = LayerGround };
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(cx, top - 0.15f, 0);
        var c = Box(go, new Vector2(width, 0.3f), false);
        c.usedByEffector = true;
        var e = go.AddComponent<PlatformEffector2D>();
        e.useOneWay = true;
        e.surfaceArc = 180f;
    }

    /// <summary>
    /// 逆さのゴンドラ (gondola_upside_down を上下反転, scale 1.5)。bar = 下になったレールの上面。
    /// 乗れる所: レール (上面 bar, 幅 3.7) と、上を向いた底 (上面 bar+2.48, 幅 2.6)。レールの両端から天井へ鎖。
    /// </summary>
    static void UpsideDown(string name, float cx, int bar, int ceiling)
    {
        var cy = bar + 1.18f;
        var go = NewObject(name, new Vector2(cx, cy), LayerGround, "Terrain Objects");
        var art = new GameObject("Art");
        art.transform.SetParent(go.transform, false);
        art.transform.localScale = Vector3.one * 1.5f;
        var sr = AddSprite(art, Art("platforms/gondola_upside_down.png"), OrderGimmick);
        sr.flipY = true;
        OneWayBox(go, "Rail", cx, bar, 3.7f);
        OneWayBox(go, "Bottom", cx, bar + 2.48f, 2.6f);
        foreach (var dx in new[] { -1.75f, 1.75f })
            Truss(cx + dx, bar - 0.1f, ceiling, 0.3f, -6, Chain);
    }

    public static string Run()
    {
        var log = BuildBase("A1_W6");
        SkyBackdrop();                    // W7 の奥は空 (逆さゴンドラを吊ったスポークの間)
        var wheel = PlaceFerrisWheel(true, false, new Color(0.5f, 0.46f, 0.6f));
        var prefabHub = wheel.transform.Find("Hub");
        if (prefabHub != null) prefabHub.gameObject.SetActive(false);   // 軸はこの部屋の大きな絵で描く
        var legs = wheel.transform.Find("Legs");
        if (legs != null) legs.gameObject.SetActive(false);   // 脚は観覧車の面より奥 (機械室の中を横切らないように)
        PlateWallsAll();
        RockDecor(0.6f);

        // ================= 機械室の奥の壁 (W6) と巨大な軸
        var back = NewObject("Backdrop_W6", new Vector2(51f, 255f), LayerDefault, "Background");
        AddSprite(back, Art("ferris/machine_room_wall.png"), OrderRoomBackdrop, new Vector2(30f, 30f), new Color(0.78f, 0.7f, 0.88f));
        var hub = Spinner("HubAxle", new Vector2(54f, 252f), 4f, "Background");
        var hubArt = new GameObject("Art");
        hubArt.transform.SetParent(hub.transform, false);
        hubArt.transform.localScale = Vector3.one * 2.2f;
        AddSprite(hubArt, Art("ferris/hub_axle.png"), -49, null, new Color(0.66f, 0.6f, 0.74f));
        // 軸を挟む歯車 (逆回り) と主軸の受け
        Gear(new Vector2(40.5f, 265f), 0.9f, -9f, new Color(0.5f, 0.44f, 0.58f));
        Gear(new Vector2(63.5f, 244f), 1.1f, -7f, new Color(0.5f, 0.44f, 0.58f));
        Gear(new Vector2(66f, 268f), 0.7f, 11f, new Color(0.45f, 0.4f, 0.54f));

        // ================= 床 y240。下の口 (x45..57, A1_W5 へ) は格子
        Girder(45, 57, 240, new Color(0.62f, 0.55f, 0.72f), "Grate");
        SavePointAt(40.5f, 240f);

        // ================= 西: 非常用リフトの上の乗り口 (踊り場 y249)
        Solid(30, 246, 43, 249);
        Clear(30, 249, 36, 257);                         // リフトのかごが入る縦坑の口
        Truss(30.6f, 225f, 257f, 0.9f, -46, Rail);       // 縦坑のレール (下の乗り場 A1_W1 へ続く)
        Truss(35.4f, 225f, 257f, 0.9f, -46, Rail);
        Prop("props/elevator_cabin.png", 33f, 251.4f, 1.6f, OrderProp);
        TransportAt(new Vector2(33f, 250.5f), new Vector2(4f, 3f), "A1_W1", "FromA1_W6", new[] { "A1_LiftWheel" },
            "乗る", "非常用リフトは止まっている…… 横のレバーで動かせそうだ", name: "EmergencyLift");
        Spawn("FromA1_W1", new Vector2(38.5f, 250.5f));
        // レバー (LeverSwitch → A1_LiftWheel)
        var lever = NewObject("LiftLever", new Vector2(41.6f, 250.05f), LayerInteractable, "Gimmicks");
        lever.transform.localScale = Vector3.one * 0.6f;
        AddSprite(lever, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LocalData/StageAsset/gimmick/lever/lever_01.png"), OrderGimmick);
        Box(lever, new Vector2(2.4f, 3.4f), true);
        AddComp<LeverSwitch>(lever, ("_progressFlag", "A1_LiftWheel"),
            ("_activatedMessage", "非常用リフトが動き出した! 地上の乗り場と行き来できる"), ("_promptOffset", new Vector3(0, 2.4f, 0)));
        // リフトのランプ: 止まっている間は赤、動くと緑
        var red = Prop("props/boss_door_lamp_on.png", 35.6f, 255.2f, 0.55f, OrderProp, false, new Color(1f, 0.35f, 0.4f));
        var green = Prop("props/boss_door_lamp_on.png", 35.6f, 255.2f, 0.55f, OrderProp, false, new Color(0.45f, 1f, 0.6f));
        var lamp = NewObject("LiftLampFlag", new Vector2(35.6f, 256f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(lamp, ("_flags", new[] { "A1_LiftWheel" }), ("_showWhenSet", new Object[] { green }),
            ("_hideWhenSet", new Object[] { red }));

        // ================= 登り (軸の左): 床 → 段 → 踊り場 → 軸の上の渡り → 天井の口
        Girder(46, 52, 243, Steel);
        Girder(43, 49, 246, Steel);
        // 踊り場 y249 (x30..43)
        Girder(43, 49, 252, Steel);
        Girder(37, 43, 255, Steel);
        Girder(41, 47, 258, Steel);
        Girder(37, 43, 261, Steel);
        Girder(42, 58, 264, Steel, "HubCatwalk");   // 軸の真上の渡り
        Girder(46, 56, 267, Steel);                   // 天井の口 (A1_W8) の真下。上から落ちてきた時もここ
        // 東: W7 への棚 (y258) と渡りへ戻る段
        Solid(60, 255, 66, 258);
        Girder(56, 62, 261, Steel);

        // ================= W7: 逆さゴンドラの寄り道 (床はトゲ)
        Solid(69, 252, 72, 258);                     // 入口の棚 (通路の床 y258 の続き)
        Spikes(72, 89, 252);
        Spikes(92, 99, 252);
        UpsideDown("UpsideGondola1", 75.5f, 259, 276);
        UpsideDown("UpsideGondola2", 81f, 262, 276);
        UpsideDown("UpsideGondola3", 86.5f, 266, 276);   // レール (y266) からは仕切りの上 (y270) に届かない → 底の上 (y268.5) から
        Solid(89, 252, 92, 270);                         // 仕切りの壁
        UpsideDown("UpsideGondola4", 95.5f, 263, 276);
        UpsideDown("UpsideGondola5", 98f, 267, 276);     // 帰り道: 4 の底 → 5 のレール → 5 の底 → 仕切りの上
        Solid(99, 252, 102, 261);                        // 奥の棚
        var chest = ConsumablePickupAt(100.5f, 261f, "Assets/Items/PinCushion.asset", 3, "A1_Pickup_W7_1");
        chest.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.5f);
        Set(chest.GetComponent<ConsumableItemPickup>(), "_promptText", "開ける");
        HangLamp(80f, 276f, 0.55f); HangLamp(97f, 276f, 0.55f);

        // ================= 飾り
        HangLamp(40f, 270f); HangLamp(62f, 270f);
        Prop("rock/decal_pipe_v.png", 37.5f, 244f, 1.2f, -7);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(77f, 270f, true, name: "DetourFlyer");     // W7 逆さゴンドラの寄り道の上の空中 (機械室・セーブの周りには置かない)

        return log + Finish();
    }
}
