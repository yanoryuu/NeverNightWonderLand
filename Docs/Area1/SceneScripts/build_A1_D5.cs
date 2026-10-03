using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_D5 「蒸気管の迷路」(D5 x180..240 y-150..-123) + 「水没した排水溝」(D6 x132..177 y-153..-132)
// 上 = 搬送ベルトの通路 A1_D2 (天井の口 x186..195) / 東 = 歯車の胃袋 A1_D3 (x240..243, 床 y-141) / 西 = D6 (同じシーン, 床 y-147)
// D5: 部屋を岩で埋めて配管の通路を掘った迷路。到着の小部屋 (床 y-132) から 3 方向:
//     上 = 来た道 (棚 → 天井の口) / 西 = 縦穴 (配管の棚) を降りて排水溝 D6 / 東 = 本道 (天井の蒸気管 3 本の下を抜け、段を降りて A1_D3)。
//     本道にだけ点滅する案内灯 (東へ流れる)。縦穴の底から東へ下の通路 (蒸気 3 + 窪みの糸) が延び、東の縦穴で本道の床 (すり抜けの蓋) に戻る輪。
//     到着の小部屋に地図売り (歯車の縦穴と、ボスの後のエレベーターが大扉のそばへ上がること)。
// D6: 水没した排水溝 (水面 y-146)。水の中は遅いが、天井の蒸気は水面で止まる = 沈んで歩けば安全。
//     水面の上の配管 (近道) と、西の岸へ上がる段は蒸気にさらされる。慌てず止むのを待つ人が得をする。西の岸に糸巻き2。
// 先に別の呼び出しで OpenOrCreate("A1_D5") を実行しておくこと。
public static class BuildD5
{
    static readonly Color Rust = new Color(0.95f, 0.88f, 0.95f);
    static readonly Color BgPipe = new Color(0.42f, 0.37f, 0.5f);
    static readonly Color Guide = new Color(0.4f, 1f, 0.78f);

    static Color Dim(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    public static string Run()
    {
        var log = BuildBase("A1_D5");

        // ================= D5: いったん部屋を岩で埋め、通路を掘る
        Solid(180, -150, 240, -123);
        Clear(180, -132, 201, -123);     // A 到着の小部屋 (床 y-132)
        Clear(180, -147, 187, -132);     // B 西の縦穴 (底 y-147 → 西の通路で D6)
        Clear(201, -132, 228, -126);     // C 本道 (床 y-132, 天井 y-126)
        Clear(228, -141, 240, -126);     // D 出口の段 (床 y-141 → A1_D3)
        Clear(187, -150, 228, -144);     // E 下の通路 (床 y-150, 天井 y-144)
        Clear(222, -150, 228, -132);     // F 東の縦穴 (E → C の床の蓋)
        Clear(205, -153, 211, -150);     // E の窪み (糸)

        // A: 天井の口 (A1_D2) の真下の棚と、そこへ戻る段
        PipePlatform(185, 196, -126, Rust, "Pipe_Landing");   // SpawnFromA1_D2 (190.5,-124.5)
        PipePlatform(197, 201, -129, Rust);

        // B: 西の縦穴の棚 (左右交互)
        PipePlatform(180, 184, -135, Rust);
        PipePlatform(183, 187, -138, Rust);
        PipePlatform(180, 184, -141, Rust);
        PipePlatform(183, 187, -144, Rust);

        // D: 出口へ降りる段
        Solid(228, -141, 231, -135);
        Solid(231, -141, 234, -138);

        // F: 東の縦穴の棚 + 本道の床の蓋 (上からは床、下からはすり抜けて上がれる)
        PipePlatform(222, 225, -147, Rust);
        PipePlatform(225, 228, -144, Rust);
        PipePlatform(222, 225, -141, Rust);
        PipePlatform(225, 228, -138, Rust);
        PipePlatform(222, 225, -135, Rust);
        PipePlatform(222, 228, -132, Rust, "Pipe_Lid");

        // ---- 蒸気 (本道 C: 天井から床へ。止む順番が東へずれていく = 波に乗って走り抜けられる)
        SteamVent(206.5f, -126f, 6f, 1.3f, 2.1f, 0f, true, name: "SteamC1");
        SteamVent(212.5f, -126f, 6f, 1.3f, 2.1f, 0.7f, true, name: "SteamC2");
        SteamVent(218.5f, -126f, 6f, 1.3f, 2.1f, 1.4f, true, name: "SteamC3");
        SteamVent(232.5f, -138f, 4.5f, 1.2f, 2.2f, 2.0f, false, name: "SteamD1");   // 段の上の床の口
        // ---- 蒸気 (下の通路 E)
        SteamVent(194.5f, -144f, 6f, 1.2f, 2.0f, 0.3f, true, name: "SteamE1");
        SteamVent(201f, -150f, 4.5f, 1.2f, 2.0f, 1.3f, false, name: "SteamE2");
        SteamVent(208f, -144f, 9f, 1.4f, 1.8f, 0.8f, true, name: "SteamE3");   // 窪みの真上
        SteamVent(216.5f, -150f, 4.5f, 1.2f, 2.0f, 2.2f, false, name: "SteamE4");
        ThreadPickupAt(209.6f, -152.4f, 15, "A1_Pickup_D5_1");

        // ---- 本道の目印: 点滅する案内灯 (到着の小部屋 → 本道 → 出口。点滅が東へ流れる)
        var lamps = new (float x, float y)[]
        {
            (199f, -123f), (203.5f, -126f), (209.5f, -126f), (215.5f, -126f), (221f, -126f), (226f, -126f),
            (231f, -126f), (236f, -126f), (239.4f, -132f),
        };
        for (var i = 0; i < lamps.Length; i++)
            GuideLamp(lamps[i].x, lamps[i].y, -i * 0.11f, Guide, true, 1.5f, "GuideLamp" + i);

        // ---- 地図売り (到着の小部屋、西の縦穴の縁)
        NpcAt(189.6f, -132f, "npc/npc_map_seller.png", "地図売り", new[]
        {
            "おや、地上からのお客さんかい。こんな蒸気の中まで、ご苦労さん。",
            "蒸気は決まった間隔で噴く。シューッと鳴って口が赤く光ったら、すぐに噴き出す合図だ。止んだ隙に走り抜けな。",
            "道は三つ。上は来た道、西の縦穴の先は水に沈んだ排水溝。東の、灯りが点滅してる方が本道だよ。",
            "本道の先は「歯車の胃袋」。回る歯車を足場にして、うんと深くまで降りていく縦穴さ。踏み外しても、下へ下へ行くだけだがね。",
            "その底の奥に居座ってる重たいやつを片付けりゃ、搬出用のエレベーターが動き出す。あれは地上の大扉の回廊のすぐそばまで上がっていくから、帰りはそいつを使いな。",
            "地図? ……この迷路のは、わたしの頭の中にしかないのさ。",
        }, new[]
        {
            "東の点滅する灯りが本道。歯車の縦穴の底で重たいやつを片付けりゃ、エレベーターで大扉の回廊のそばへ戻れるよ。",
        }, "A1_Npc_MapSellerD5", 0, flipX: true);

        // ================= D6 水没した排水溝 (水面 y-148)
        Solid(172, -153, 177, -147);     // 入口の岸 (D5 の縦穴の底と同じ高さ、水面より上)
        Solid(169, -153, 172, -149);     // 水に入る段 (2 ずつ)
        Solid(166, -153, 169, -151);
        Solid(155, -153, 158, -151);     // 底の出っ張り (沈んだ配管の継ぎ手。上で跳ぶと頭が水面から出る)
        Solid(142, -153, 145, -151);     // 西の岸へ上がる段
        Solid(139, -153, 142, -149);
        Solid(136, -153, 139, -147);
        Solid(132, -153, 136, -145);     // 西の岸 (水面より上) = 糸巻き2
        WaterBody(136, -153, 172, -148);
        // 水面の上の配管 (濡れずに渡れる近道。ただし真上に蒸気)
        PipePlatform(165, 170, -144, Rust);
        PipePlatform(157, 162, -144, Rust);
        PipePlatform(149, 154, -144, Rust);
        PipePlatform(141, 146, -144, Rust);
        // 天井から下がった蒸気管 (口 y-138)。蒸気は水面 y-148 で止まる = 沈んでいれば当たらない
        foreach (var x in new[] { 167.5f, 159.5f, 151.5f, 143.5f, 138.5f })
            PipeRun(x, -138.3f, x, -132f, 0.6f, -5, new Color(0.55f, 0.5f, 0.62f));
        SteamVent(167.5f, -138f, 10f, 1.4f, 2.0f, 0f, true, name: "SteamW1");
        SteamVent(159.5f, -138f, 10f, 1.4f, 2.0f, 1.1f, true, name: "SteamW2");
        SteamVent(151.5f, -138f, 10f, 1.4f, 2.0f, 2.2f, true, name: "SteamW3");
        SteamVent(143.5f, -138f, 10f, 1.4f, 2.0f, 0.4f, true, name: "SteamW4");
        SteamVent(138.5f, -138f, 10f, 1.4f, 2.0f, 1.9f, true, name: "SteamW5");   // 西の岸へ上がる段の上
        KeyItemAt(133.8f, -144.45f, "糸巻き2", "A1_Spool2", "A1_Pickup_D6_1", "props/thread_spool.png", "拾う");
        Enemy(153f, -138f, true, name: "DrainFlyer");

        // ================= 飾り: 迷路の配管・湯気・水の滴り
        PipeRun(180f, -124.2f, 201f, -124.2f, 0.7f, -6, BgPipe);
        PipeRun(201f, -127.0f, 240f, -127.0f, 0.75f, -6, BgPipe);
        PipeRun(187f, -145.0f, 228f, -145.0f, 0.7f, -6, BgPipe);
        PipeRun(181.2f, -147f, 181.2f, -123f, 0.6f, -7, Dim(BgPipe, 0.85f));
        PipeRun(226.8f, -150f, 226.8f, -127f, 0.55f, -7, Dim(BgPipe, 0.85f));
        PipeRun(238.6f, -141f, 238.6f, -127f, 0.6f, -7, Dim(BgPipe, 0.85f));
        PipeRun(132f, -133.2f, 177f, -133.2f, 0.8f, -6, BgPipe);
        PipeRun(175.5f, -147f, 175.5f, -133f, 0.6f, -7, Dim(BgPipe, 0.8f));
        // 迷路の壁 (通路の間の岩の塊) = 太い配管の束に見せる (岩肌の前・縁タイルの後ろ)
        var bundle = new Color(0.5f, 0.44f, 0.56f);
        for (var i = 0; i < 6; i++)
            PipeRun(187f, -133.4f - i * 1.92f, 222f, -133.4f - i * 1.92f, 2.4f, OrderBackfill + 2, Dim(bundle, 0.72f + 0.07f * (i % 3)));
        foreach (var x in new[] { 193.5f, 205.5f, 216.5f })
            PipeRun(x, -144f, x, -132f, 1.2f, OrderBackfill + 3, Dim(bundle, 0.6f));
        foreach (var y in new[] { -142.6f, -145.2f, -147.8f })
            PipeRun(228f, y, 240f, y, 2.2f, OrderBackfill + 2, Dim(bundle, 0.7f));
        PipeRun(201f, -124.5f, 240f, -124.5f, 2f, OrderBackfill + 2, Dim(bundle, 0.65f));
        // 排水溝: 天井から垂れる鎖と配管、壁の光る苔、水面の照り返し
        foreach (var x in new[] { 146f, 154f, 163f, 170f })
        {
            var ch = Prop("rock/decal_chain_hanging.png", x, -133.8f, 0.6f, -7, false, new Color(0.4f, 0.38f, 0.48f));
            ch.transform.localScale = new Vector3(0.6f, 0.5f + (x % 3) * 0.12f, 1f);
        }
        // polish A: 光る苔は 2 つに減らして暗く (明るい水色が岩の上で浮かないように)
        foreach (var p in new[] { new Vector2(134.5f, -140f), new Vector2(175.6f, -141.5f) })
        {
            Prop("rock/decal_moss_glow.png", p.x, p.y, 0.75f, -6, false, new Color(0.55f, 0.62f, 0.68f, 0.85f));
            Glow(p.x, p.y, 1.3f, new Color(0.3f, 1f, 0.85f, 0.12f), -7);
        }
        Prop("platforms/gear_platform.png", 183.6f, -139.5f, 0.6f, -7, false, new Color(0.22f, 0.2f, 0.28f));
        Prop("platforms/gear_platform.png", 236f, -133.5f, 0.75f, -7, false, new Color(0.22f, 0.2f, 0.28f));
        Glow(189.6f, -129.5f, 2.2f, new Color(1f, 0.75f, 0.45f, 0.22f), -7);   // 地図売りの手元の灯り
        Prop("rock/decal_lamp_wall.png", 186.4f, -128.4f, 1f, -6);

        // ================= 暗さと質感
        TintRock(100, -170, 280, -110, new Color(0.66f, 0.62f, 0.76f));
        TintRock(100, -170, 178, -110, new Color(0.85f, 0.95f, 1.05f));   // 排水溝は青く
        Shade(132f, -154f, 177f, -140f, 0.55f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
