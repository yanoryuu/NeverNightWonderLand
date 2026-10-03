using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W1 「観覧車の乗り場」(W1, 屋外 x3..54 y0..30)
// 東 = 観覧車の足元 A1_U4 (y6..18) / 上 = 支柱の足場 A1_W2 (天井の穴 x15..24 y30) / 西 = 非常用リフトの下の乗り口 (A1_W6 へ, 要 A1_LiftWheel)
// 乗り場のリフトは壊れていて動かない → 鉄骨の足場をジグザグに登って、支柱の付け根の穴から中へ。
// W2 の床の穴から落ちると、この部屋のいちばん上の足場 (y27, SpawnFromA1_W2) に戻る。
// 先に別の呼び出しで OpenOrCreate("A1_W1") を実行しておくこと。
public static class BuildW1
{
    static readonly Color Steel = new Color(0.9f, 0.82f, 0.95f);
    static readonly Color Rail = new Color(0.62f, 0.52f, 0.72f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_W1");
        PlaceFerrisWheel(true, true);   // 乗り場の建物 (BaseStation x44..64) がこの部屋の東端に掛かる
        // polish A: 中景の観覧車 (W1〜W4 共通の輪。ゆっくり回り、ゴンドラが通り過ぎる。当たり無し)
        FerrisMidground(1.2f);
        // polish B: 中景を通り過ぎる暗いゴンドラ (中景の輪とほぼ同じ遠さ) と、乗り場の電球の連なり
        FarGondola(new Vector2(13f, 23.5f), new Vector2(0.86f, 0.84f), new Vector2(28f, 8f), 0.62f);
        FarGondola(new Vector2(45f, 20.5f), new Vector2(0.86f, 0.84f), new Vector2(28f, 8f), 0.55f);
        BulbString(29.3f, 39f, 5.1f, 0.7f, -6);      // 街灯 → 乗り場リフトのレール
        BulbString(14.3f, 22f, 7.6f, 0.7f, -7);      // 街灯の上 (旗の上を渡す)
        RockDecor(0.6f);

        // 頭上の外周の帯 (A1_U4 と同じ遠景)。地上からは本物の外周 (下端 y162) は見えない
        ParallaxProp("ferris/far_wheel_silhouette_arc.png", new Vector2(28f, 12.2f), new Vector2(0.9f, 1f), new Vector2(28f, 8f),
            1.6f, OrderFarBg - 1, new Color(0.6f, 0.56f, 0.85f, 0.38f), new Vector2(7.033f * 3f, 1.875f));

        // ================= 東の境 (A1_U4 と見た目をそろえる): 境の壁 x54..57 は y6 以下と y18..27 だけ。その上は空
        Clear(54, 27, 57, 400);
        Clear(57, 0, 120, 400);
        Ground(57, 120, 0);
        OneWay(57, 61, 6);        // U4 側の段 (U4 のレシピと同じ)
        OneWay(61, 65, 3);

        // 乗り場の建物: 階段の上の踊り場 (y3) と日除けの屋根 (y6) → 東の出口
        OneWay(48, 54, 3);
        OneWay(50, 54, 6);

        // ================= 西: 非常用リフトの下の乗り口 (TransportInteractable → A1_W6、要 A1_LiftWheel)
        PlateWalls(-60, -10, 3, 400);           // 西の壁 = リフトの縦坑の外壁 (鉄板)
        Solid(0, 30, 3, 33);                    // 縦坑の開口 (レイアウト上の WL の通路) は塞いで、縦坑は絵で見せる
        Clear(-60, 44, -3, 400);
        Truss(3.7f, 0f, 160f, 1.1f, -46, Rail);  // 縦坑のレール
        Truss(8.3f, 0f, 160f, 1.1f, -46, Rail);
        for (var y = 6f; y < 160f; y += 6f) Brace(3.7f, 8.3f, y, 0.5f, -47, new Color(0.3f, 0.25f, 0.42f));
        var cabin = Prop("props/elevator_cabin.png", 6f, 3.45f, 2.4f, OrderProp);   // 下りてきたままのかご
        TransportAt(new Vector2(6f, 1.5f), new Vector2(3.6f, 3f), "A1_W6", "FromA1_W1", new[] { "A1_LiftWheel" },
            "乗る", "非常用リフトは止まっている…… てっぺんの機械室で動かせるらしい", name: "EmergencyLift");
        Spawn("FromA1_W6", new Vector2(10.5f, 1.5f));   // A1_W6 から非常用リフトで下りてきた時
        Prop("props/boss_door_lamp_on.png", 8.3f, 5.6f, 0.55f, OrderProp, false, new Color(1f, 0.35f, 0.4f)); // 止まっているランプ (赤)

        // ================= 上: 支柱の付け根 (A1_W2 の床) = 鉄板の塔。穴 x15..24 から中へ
        PlateWalls(6, 30, 36, 400);
        Solid(6, 30, 15, 46);
        Solid(24, 30, 36, 46);
        var shaft = NewObject("Backdrop_LegShaft", new Vector2(19.5f, 38f), LayerDefault, "Background");
        AddSprite(shaft, Art("ferris/machine_room_wall.png"), OrderRoomBackdrop, new Vector2(9f, 16f), new Color(0.85f, 0.78f, 0.95f));
        Truss(16.2f, 30f, 46f, 1.2f, -45, Rail); Truss(22.8f, 30f, 46f, 1.2f, -45, Rail);   // 支柱の中の柱 (穴の奥)
        Brace(15f, 24f, 40f, 0.8f, -44, Rail);
        Truss(9.5f, 46f, 140f, 3f, -46, Rail);    // 塔の上に続く支柱 (A1_W2 の中の柱)
        Truss(32.5f, 46f, 140f, 3f, -46, Rail);
        HangLamp(17f, 30f, 0.55f); HangLamp(22f, 30f, 0.55f);

        // ================= 乗り場のリフト (壊れて途中で止まっている) と、その骨組みに架かった鉄骨の足場
        Truss(39.4f, 0f, 30f, 1.4f, -6, Rail);    // 乗り場リフトのレール
        Truss(46.6f, 0f, 30f, 1.4f, -6, Rail);
        // 止まったかご: 傾いた台 (当たりは水平)。ケーブルが切れて垂れている
        var car = NewObject("BrokenBoardingLift", new Vector2(43f, 11.5f), LayerGround, "Terrain Objects");
        var carArt = new GameObject("Art");
        carArt.transform.SetParent(car.transform, false);
        carArt.transform.localRotation = Quaternion.Euler(0, 0, -4f);
        carArt.transform.localScale = new Vector3(2f, 1f, 1f);
        AddSprite(carArt, Art("platforms/lift_platform.png"), OrderGimmick, null, new Color(0.8f, 0.7f, 0.8f));
        Box(car, new Vector2(5.6f, 0.5f), false, new Vector2(0, 0.25f));
        var cable = NewObject("SnappedCable", new Vector2(44.6f, 15.2f), LayerDefault, "Props");
        cable.transform.rotation = Quaternion.Euler(0, 0, 80f);
        AddSprite(cable, Art("ferris/spoke_beam.png"), -6, new Vector2(7f, 0.25f), new Color(0.55f, 0.55f, 0.62f));

        // 足場を支える骨組みの柱 (足場が宙に浮いて見えないように)
        Truss(30.6f, 0f, 30f, 1.2f, -7, Rail);
        Truss(22.4f, 0f, 30f, 1.2f, -7, Rail);
        Truss(12.8f, 27f, 30f, 1.2f, -7, Rail);
        Girder(40, 47, 6, Steel);
        Girder(31, 38, 9, Steel);
        // y12 = 止まったかご (x40..46)
        Girder(31, 38, 15, Steel);
        Girder(40, 46, 18, Steel);
        Girder(30, 37, 21, Steel);
        Girder(22, 30, 24, Steel);
        Girder(12, 25, 27, Steel);     // 支柱の穴の真下 (W2 から落ちた時の受け皿)

        // ================= 地上の飾り: 待ち列の柵・街灯・旗・切符の看板
        foreach (var x in new[] { 17.5f, 20.5f, 23.5f, 26.5f })
            Prop("props/fence_gate.png", x, 0f, 0.55f, -6, false, new Color(0.6f, 0.55f, 0.7f));
        LampPost(14f, 0f, 5f); LampPost(29f, 0f, 5f);
        var bun = Prop("rock/decal_bunting.png", 21.5f, 4.9f, 1f, -6);
        bun.transform.localScale = new Vector3(5f, 1.3f, 1f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        _ = cabin;
        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(32f, 0.6f, name: "PlatformWalker");        // 乗り場の床 (x22..43)。登り始める前の地上
        Enemy(16f, 12f, true, name: "BoardingFlyer");     // 足場の間の空中 (西の縦坑寄り)

        return log + Finish();
    }
}
