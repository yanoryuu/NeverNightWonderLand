using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W4 「風の外周」(W4, 屋外 x57..96 y150..174) + 「外周の物置」(W4r x99..114 y156..171, 紫の錆びた扉 = 突進)
// 左 = 整備用リフト A1_W3 (y156..165) / 上 = 回るゴンドラ A1_W5 (x66..78 y174)
// 観覧車の外周 (下端 y≈162..172) の外に吊られた点検路。突風 (左向き) に押し戻されながら東へ → 東端で外周の上へ登り、
// 今度は追い風で西へ戻って上の口 (A1_W5) へ。落ちても外周の下に張った安全網 (y150) が受け止める。
// 先に別の呼び出しで OpenOrCreate("A1_W4") を実行しておくこと。
public static class BuildW4
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Rod = new Color(0.62f, 0.52f, 0.72f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // 外周の中心線の高さ (共通の観覧車: 中心 (54,252) 半径 90)
    static float RimY(float x) => 252f - Mathf.Sqrt(90f * 90f - (x - 54f) * (x - 54f));

    // 点検路を外周から吊る棒 (足場の両端から外周の下端まで)
    static void Hangers(int x0, int x1, int top)
    {
        foreach (var x in new[] { x0 + 0.6f, x1 - 0.6f })
        {
            var rimBottom = RimY(x) - 1.2f;
            if (rimBottom > top + 0.5f)
                Truss(x, top - 0.3f, rimBottom, 0.45f, -6, Rod);
        }
    }

    static void Walk(int x0, int x1, int top, bool hang = true)
    {
        Girder(x0, x1, top, Steel);
        if (hang) Hangers(x0, x1, top);
    }

    // 風よけ: 点検路の上に立てた鉄板のついたて (当たり無し, プレイヤーの奥)。上半分は外周の上の口 (A1_W5) の縁から吊った板
    static void WindScreen(float x, float floorY)
    {
        var go = NewObject("WindScreen", new Vector2(x, floorY), LayerDefault, "Props");
        var panel = new GameObject("Panel");
        panel.transform.SetParent(go.transform, false);
        panel.transform.localPosition = new Vector3(0, 2.1f, 0);
        AddSprite(panel, Art("ferris/plate/plate_tex_r0_c0.png"), -6, new Vector2(1.1f, 4.2f), new Color(0.55f, 0.48f, 0.66f));
        // 板の縁 (柱) と斜めの控え
        Truss(x - 0.6f, floorY - 0.3f, floorY + 4.6f, 0.4f, -5, Rod);
        Truss(x + 0.6f, floorY - 0.3f, floorY + 4.6f, 0.4f, -5, Rod);
        Truss(x - 1.4f, floorY - 0.2f, floorY + 2.6f, 0.35f, -5, Rod, -28f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_W4");
        PlaceFerrisWheel(true, false, new Color(0.82f, 0.8f, 0.9f));   // 外周・スポーク・吊られたゴンドラ (飾り) がすぐそこに見える
        // polish B: 中景の観覧車 (FerrisMidground) は置かない。ここは本物の外周の上なので、縮めた輪のハブが頭上に見えると矛盾する
        // (W1〜W3 の中景の輪もカメラ y135→152 で消える)。代わりに輪の向こう側を回る本物のゴンドラを遠くに小さく
        FarGondola(new Vector2(66f, 167f), new Vector2(0.94f, 0.95f), new Vector2(77f, 160f), 0.45f, new Color(0.46f, 0.42f, 0.62f, 0.8f));
        FarGondola(new Vector2(91f, 169.5f), new Vector2(0.94f, 0.95f), new Vector2(77f, 160f), 0.4f, new Color(0.42f, 0.38f, 0.58f, 0.75f));
        PlateWallsAll();                 // 西 = 整備用の縦坑 (A1_W3) の外壁、東 = 外周の物置 (鉄板)
        RockDecor(0.5f);

        // ================= 空を開ける: 足元 (y150 より下) は眼下の園内まで何も無い。縦坑の外壁と物置だけを残す
        Clear(57, 100, 99, 150);          // 部屋の下
        Clear(96, 100, 140, 153);         // 物置の下
        Clear(117, 100, 160, 260);        // 物置の東
        Clear(96, 174, 160, 260);         // 物置の上
        Clear(20, 172, 57, 260);          // 縦坑の外壁は y172 まで (その上は外周)
        Clear(0, 100, 30, 260);           // 縦坑の外壁の西
        // 眼下の園内 (画面の下端にほぼ張り付く遠景: 高い所から見下ろした小さなシルエット)
        ParallaxProp("bg/bg_park_silhouette.png", new Vector2(77f, 150.3f), new Vector2(0.97f, 1f), new Vector2(77f, 156f),
            1.1f, OrderFarBg, new Color(0.55f, 0.45f, 0.75f, 0.9f), new Vector2(60f, 1.98f));
        ParallaxProp("bg/bg_park_silhouette.png", new Vector2(77f, 150.0f), new Vector2(0.985f, 1f), new Vector2(77f, 156f),
            0.7f, OrderFarBg - 1, new Color(0.45f, 0.38f, 0.65f, 0.7f), new Vector2(90f, 1.98f));

        // ================= 入口 (縦坑の扉の外の踊り場, y156)
        Ground(57, 62, 156, 3);
        // 安全網 (y150): 外周の下に張った作業用の網。ここから踊り場へ戻る段
        var net = Girder(57, 99, 150, new Color(0.45f, 0.72f, 0.75f), "SafetyNet");
        Girder(58, 63, 153, Steel);
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // ================= 下の点検路 (外周の外, 向かい風の中を東へ)
        Walk(63, 69, 156);
        Walk(71, 77, 158);
        Walk(81, 86, 158);          // 幅 4 の隙間: 風が止んだ隙に跳ぶ (ダッシュなら風の中でも届く)
        Walk(88, 96, 159);          // 物置の扉の前
        // 紫の錆びた扉 (横突進で壊れる) → W4r
        SkillBlock(96, 159, 99, 168, PlayerSkill.ChargeRush, "A1_Rush_W4r");

        // ================= 外周をくぐる梯子 (物置の陰で風が当たらない) → 外周の上の点検路を追い風で西へ
        Ladder(86, 159, 172);
        Walk(78, 85, 172, false);
        Walk(61, 78, 171, false);   // A1_W5 の口の真下 (上から落ちてきた時の受け皿)。polish A: 西へ 3・東へ 2 延長 (着地点を広く、東の点検路 y172 と隙間なし)
        // 上の点検路を外周に留める短い支柱
        foreach (var x in new[] { 80f, 83.5f, 62f, 68f, 74f })
        {
            var top = x > 77f ? 172f : 171f;
            Truss(x, RimY(x), top - 0.3f, 0.45f, -6, Rod);
        }

        // ================= 横風 (突風: 吹く 2.6s / 止む 2s、左向き)。外周の下 = 向かい風 / 外周の上 = 追い風。
        // 梯子のまわり (x85..96) は物置の陰で風が当たらない
        Wind(62, 150, 85, 164, new Vector2(-5.5f, 0f), true, 2.6f, 2f, 0f, 0.45f, "WindLow");   // 入口の踊り場 (x57..62) は縦坑の陰
        // polish A: 上の点検路は風よけ (WindScreen, x77) の西が凪。A1_W5 から落ちてきた着地点 (72,171) で立ち止まっても押し出されない。
        // 追い風は東の点検路 (x78..85) と、そこから風よけまでの隙間だけ
        Wind(77, 164, 85, 178, new Vector2(-5.5f, 0f), true, 2.6f, 2f, 0f, 0.45f, "WindHigh");
        WindScreen(77.4f, 171f);
        Wind(85, 150, 96, 159, new Vector2(-5.5f, 0f), true, 2.6f, 2f, 0f, 0.45f, "WindDoor");

        // ================= W4r: 外周の物置 (床 y159)。日誌その5
        Ground(99, 114, 159, 3);
        KeyItemAt(109.5f, 159f, "日誌「閉園しなかった夜」その5 (整備員の手記)", "A1_Lore_Journal5", "A1_Pickup_W4r_1");
        HangLamp(104f, 171f, 0.6f); HangLamp(111f, 171f, 0.6f);
        Prop("platforms/gear_platform.png", 102.5f, 161.2f, 0.5f, -6, false, new Color(0.75f, 0.68f, 0.8f));
        Prop("rock/decal_pipe_h.png", 106f, 166f, 1.2f, -6);

        // ================= 飾り: 入口の作業灯
        HangLamp(59.5f, 165f, 0.6f);

        // ================= ヒント

        _ = net;
        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(75f, 163f, true, name: "WindFlyer");       // 外周の踊り場の間の空中 (風の中の空中戦)

        return log + Finish();
    }
}
