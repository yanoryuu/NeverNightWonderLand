using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_D1 「配管の縦穴」(D1 x237..255 y-96..-63)
// 上 = 拠点 A1_H の床の格子 (x240..249) から落ちてくる / 下 = 搬送ベルトの通路 A1_D2 (床の穴 x240..249)
// 地上とは逆に「暗く狭い所へ降りていく」合図の縦穴。足場は錆びた配管 (PipePlatform, すり抜け) と壁の岩棚を左右交互に 3 ずつ。
// 上ほど暖かい灯が多く、下へ行くほど岩肌が暗く沈み、灯は消える。底の穴の縁に赤い警告灯 (点滅)。
// 帰り道: 底の右の床 → 配管を交互に登って最上段 (y-66) → 真上へ跳ぶと拠点へ。
// 先に別の呼び出しで OpenOrCreate("A1_D1") を実行しておくこと。
public static class BuildD1
{
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Warm = new Color(1f, 0.78f, 0.5f);

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    static void WallLamp(float x, float y, bool flip = false, float glow = 1f)
    {
        Prop("rock/decal_lamp_wall.png", x, y, 1.1f, -6, flip);
        Glow(x + (flip ? -0.15f : 0.15f), y + 0.05f, 1.5f, new Color(1f, 0.7f, 0.4f, 0.3f * glow), -7);
    }

    public static string Run()
    {
        var log = BuildBase("A1_D1");

        // ================= 足場: 上 (拠点からの着地点) から底へ、左右交互に 3 ずつ
        PipePlatform(241, 248, -66, Rust, "Pipe_Landing");   // 拠点の格子の真下 = 到着点 (SpawnFromA1_H 244.5,-64.5)
        PipePlatform(249, 254, -69, Rust);
        PipePlatform(243, 248, -72, Rust);
        Solid(237, -77, 241, -75);                            // 左の岩棚
        PipePlatform(242, 247, -78, Rust);
        PipePlatform(248, 254, -81, Rust);
        PipePlatform(242, 247, -84, Rust);
        Solid(237, -89, 240, -87);                            // 左の岩棚 (細い)
        PipePlatform(241, 246, -87, Rust);
        PipePlatform(249, 254, -90, Rust);
        PipePlatform(244, 249, -93, Rust);                    // 穴の右半分の上 (左半分から落ちる)
        // 底: 左右の床 y-96 (岩) の間が A1_D2 への穴 (x240..249)
        MoveSpawn("A1_D2", 252f, -94.5f);

        // ================= 奥の配管 (壁を這う縦管と、横に渡る管)
        PipeRun(238.3f, -100f, 238.3f, -60f, 0.9f, -6, BgPipe);
        PipeRun(253.9f, -100f, 253.9f, -60f, 0.9f, -6, BgPipe);
        PipeRun(239.6f, -100f, 239.6f, -74f, 0.55f, -7, Dim(BgPipe, 0.85f));
        PipeRun(237f, -79.6f, 255f, -79.6f, 0.6f, -7, Dim(BgPipe, 0.8f));
        PipeRun(237f, -91.3f, 255f, -91.3f, 0.5f, -7, Dim(BgPipe, 0.75f));
        PipeRun(251.4f, -96f, 251.4f, -83f, 0.5f, -7, Dim(BgPipe, 0.8f));
        // 足場の配管を支える縦の継ぎ手 (足場の端から壁の管へ)
        PipeRun(253.3f, -69.4f, 253.3f, -66f, 0.5f, -5, Dim(BgPipe, 1.2f));
        PipeRun(241.6f, -84.4f, 241.6f, -80f, 0.5f, -5, Dim(BgPipe, 1.2f));
        PipeRun(253.3f, -81.4f, 253.3f, -79.6f, 0.5f, -5, Dim(BgPipe, 1.2f));

        // ================= 灯: 上は暖かく、下へ行くほど少なく暗い
        WallLamp(239.2f, -64.5f, false, 1.1f);
        WallLamp(253.8f, -70.5f, true);
        WallLamp(238.6f, -73.2f, false, 0.7f);
        WallLamp(253.6f, -84.5f, true, 0.45f);
        // 底の穴の縁に赤い警告灯 (点滅)
        GuideLamp(240.6f, -95.2f, 0f, new Color(1f, 0.32f, 0.3f), false, 1.8f, "WarnLampL");
        GuideLamp(248.4f, -95.2f, 0.5f, new Color(1f, 0.32f, 0.3f), false, 1.8f, "WarnLampR");

        // ================= 暗さ: 岩肌を下ほど沈め、奥に暗がりの帯
        TintRockGradient(205, -102, 290, -62, new Color(0.34f, 0.3f, 0.44f), new Color(1.1f, 1.08f, 1.1f));
        TintRock(205, -140, 290, -102, new Color(0.34f, 0.3f, 0.44f));
        Shade(237f, -100f, 255f, -74f, 0.75f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(248f, -75f, true, name: "PipeFlyer");      // 縦穴の上の空中 (降りる途中で 1 体)

        return log + Finish();
    }
}
