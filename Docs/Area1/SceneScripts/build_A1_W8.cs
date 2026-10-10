using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W8 「てっぺんへの梯子」(W8, 真上のスポークの点検坑 x42..60 y276..342)
// 下 = 中心軸の機械室 A1_W6 (床の口 x45..57。落ちると機械室の天井の渡り y267 へ戻る) / 上 = ゴンドラの道化 A1_MB1 (天井の口 x45..57)
// 最後の一直線: 右の梯子 → 細い鉄骨 2 枚 → 左の梯子 → 細い鉄骨 2 枚 → 右の梯子 → … を 3 区画 (y276→297→318→342)。
// 区画ごとに上から鉄のおもりが落ちてくる (FallingObjectSpawner、揺れてから落ちる。鉄骨に当たると砕ける)。浮遊する敵 2。
// 坑の中心を真上のスポーク (x54, 90 度) が貫いている (奥の飾り)。
// 先に別の呼び出しで OpenOrCreate("A1_W8") を実行しておくこと。
public static class BuildW8
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Lattice = new Color(0.3f, 0.25f, 0.42f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    static GameObject CargoDrop(string name, float x, float y, float minX, float maxX, float interval, float delay)
    {
        var go = NewObject(name, new Vector2(x, y), LayerDefault, "Gimmicks");
        // 落ちてくるのは鉄のおもり (警告の縞入り)。木箱の絵 (cargo_crate) は「壊せる木箱」の意味なので使わない (PendingChanges 30, 仮素材)
        AddComp<FallingObjectSpawner>(go, ("_sprite", Art("props/falling_weight.png")), ("_color", Color.white), ("_size", new Vector2(1.2f, 1.2f)),
            ("_minX", minX), ("_maxX", maxX), ("_interval", interval), ("_intervalJitter", 0.6f), ("_startDelay", delay),
            ("_activationDistance", 0f), ("_warningTime", 0.7f), ("_sortingOrder", 5));
        // 荷物を吊っている滑車 (落ちてくる場所の目印)
        var hook = Prop("ferris/gondola_hanger.png", x, y + 1.9f, 0.6f, -6, false, new Color(0.7f, 0.62f, 0.78f));
        Brace(x + minX - 0.6f, x + maxX + 0.6f, y + 1.9f, 0.4f, -7, new Color(0.5f, 0.44f, 0.6f));
        _ = hook;
        return go;
    }

    public static string Run()
    {
        var log = BuildBase("A1_W8");
        PlaceFerrisWheel(true, false, new Color(0.5f, 0.46f, 0.6f));   // 真上のスポーク (x54) と外周の上端 (y342) が奥に
        SkyBackdrop();
        PlateWallsAll();
        RockDecor(0.5f);
        // 坑の外の岩を削る (左右の壁は厚さ 3 の鉄板だけ。外は夜空と観覧車)
        Clear(-10, 279, 39, 400);
        Clear(63, 279, 140, 400);
        // polish B: 到着点 (59,277) の東に広がる平らな鉄板の塊 (W6 の天井) を、下ほど暗く沈め、太い配管 2 本で面を割る
        TintRockGradient(63, 250, 120, 279, new Color(0.5f, 0.46f, 0.58f), new Color(0.88f, 0.84f, 0.92f));
        PipeRun(64f, 276.2f, 92f, 276.2f, 0.9f, -6, new Color(0.5f, 0.42f, 0.55f));
        PipeRun(70.5f, 262f, 70.5f, 276.2f, 0.7f, -6, new Color(0.46f, 0.4f, 0.52f));

        // ================= 背景の骨組み: 坑の両側の柱 + 12 ごとの梁 (足場の高さと重ねない)
        Truss(42.6f, 270f, 348f, 1.4f, -45, Lattice);
        Truss(59.4f, 270f, 348f, 1.4f, -45, Lattice);
        for (var y = 283.5f; y < 342f; y += 12f)
            Brace(42f, 60f, y, 0.6f, -44, Lattice);

        // ================= 区画 1 (y276→297)
        // 床 = 口の左右 (x42..45 / x57..60)。到着は右
        MoveSpawn("A1_W6", 59.2f, 277.5f);
        SetDefaultSpawn(59.2f, 277.5f);
        Ladder(57, 276, 288);
        Girder(51, 55, 290, Steel);
        Girder(43, 49, 292, Steel);
        // ================= 区画 2 (y292→318)
        Ladder(43, 292, 304);
        Girder(46, 50, 306, Steel);
        Girder(52, 58, 308, Steel);
        Ladder(57, 308, 320);
        // ================= 区画 3 (y320→342)
        Girder(51, 55, 322, Steel);
        Girder(43, 49, 324, Steel);
        Ladder(43, 324, 336);
        Girder(46, 54, 339, Steel, "TopLanding");   // 天井の口 (A1_MB1) の真下。上から落ちてきた時もここ
        MoveSpawn("A1_MB1", 50f, 340.5f);

        // ================= 落ちてくる荷物 (区画ごと。鉄骨の渡りの上に落ちる)
        CargoDrop("CargoDrop1", 51f, 302.5f, -4f, 4f, 2.4f, 1f);
        CargoDrop("CargoDrop2", 51f, 318.5f, -4f, 4f, 2.2f, 1.8f);
        CargoDrop("CargoDrop3", 49.5f, 334f, -3.5f, 3.5f, 2.0f, 0.6f);

        // ================= 浮遊する敵 2
        Enemy(50f, 299f, flying: true, name: "LadderFlyer1");
        Enemy(51f, 329f, flying: true, name: "LadderFlyer2");

        // ================= 飾り
        HangLamp(44f, 342f, 0.6f); HangLamp(58f, 342f, 0.6f);
        Prop("rock/decal_lamp_wall.png", 59f, 296f, 0.9f, OrderWallDecor);
        Prop("rock/decal_lamp_wall.png", 43f, 314f, 0.9f, OrderWallDecor);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(49f, 285f, true, name: "LadderFlyer0");    // 梯子の下の区間 (上の 2 体と間をあける)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_W8") + "\n";
        return log + Finish();
    }
}
