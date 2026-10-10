using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_FP 「噴水広場」(FP x306..378 y0..36, 屋外) + 「正門前の並木道」(FP2 x381..447 y0..30, 屋外)
// 下 = 噴水への大階段 A1_AB (x345..354, 噴水の排水口) / 西 = 閉園の柵 → 月明かりの遊歩道 A1_U1 (x303..306 y3..15, 一方通行: FP → U1)
// FP: 床 y3。中央に沈んだ噴水の池 (x341..358, 床 y0) と大噴水 (prop/fountain ×2)。池の底の排水口 (x345..354) は鉄の格子 (↓+ジャンプで階段へ降りる)。
//     西端に柵 (FlagDoor A1_FenceOpen, A1_U1 と同じ位置 x304) と閂のレバー (LeverSwitch A1_FenceOpen)。柵の脇に U1 から見えていた小噴水 (x309.2)。
//     大噴水の前に碑文 = 日誌その4 (KeyItemPickup A1_Lore_Journal4, A1_Pickup_FP_1)。東は段 y6 / y9 → FP2。
// FP2: 床 y9 の並木道 (街灯・並木・旗)。東端に正門 (hall/park_gate ×6.87, 当たりは門の形の箱 = 上面 y20)。
//     閉じた売店 (屋根 y14 = 床から 5) → 正門のアーチの上 (y20 = 屋根から 6): どちらも二段ジャンプでだけ届く。アーチの上に糸巻き1 (A1_Spool1, A1_Pickup_FP2_1)。
//     正門の扉は閉じたまま: 「次のエリアへ (準備中)」の看板とヒント (遷移なし)。西の空に遠景の観覧車。
// 先に別の呼び出しで OpenOrCreate("A1_FP") を実行しておくこと。
public static class BuildFP
{
    static readonly Color LampWarm = new Color(1f, 0.85f, 0.6f);
    static readonly Color LampCool = new Color(0.85f, 0.9f, 1f);
    // 柵の脇の小噴水 (A1_U1 の FountainTeaser と同じ値にすること)
    public const float SmallFountainX = 309.2f, SmallFountainScale = 0.8f;
    public static readonly Color SmallFountainTint = new Color(0.86f, 0.82f, 0.98f);

    static GameObject Art2(string rel, float x, float y, float scale, int order, Color? color = null, bool flipX = false, string group = "Props")
    {
        var go = NewObject(System.IO.Path.GetFileNameWithoutExtension(rel), new Vector2(x, y), LayerDefault, group);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = AddSprite(go, Art(rel), order, null, color);
        sr.flipX = flipX;
        return go;
    }

    static void Bunting(float x0, float x1, float y)
    {
        var b = Prop("rock/decal_bunting.png", (x0 + x1) * 0.5f, y, 1.6f, -6);
        b.transform.localScale = new Vector3((x1 - x0) / 2.3f, 1.4f, 1f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_FP");
        RockDecor(0.5f);
        Set(Object.FindAnyObjectByType<CameraLimit>(), "_minY", 4.5f);   // 広場の床 (y3) が画面の下寄りに来る高さ

        // ================= 地形
        Ground(306, 341, 3, 3);                    // 西の広場 y3 (A1_U1 と同じ)
        Ground(358, 369, 3, 3);                    // 東の広場 y3
        Solid(369, 0, 372, 6);                     // 段 y6
        Solid(372, 0, 381, 9);                     // 段 y9 (FP2 への通路の床)
        Ground(381, 447, 9, 9);                    // FP2 並木道 y9
        // 池の底 y0 は岩のまま。排水口 x345..354 (A1_AB へ) は鉄の格子
        Girder(345, 354, 0, new Color(0.75f, 0.68f, 0.8f), "DrainGrate");
        // 格子の下 (階段の縦穴の口) が空に見えないよう暗い壁を敷く
        var shaft = NewObject("DrainShaft", new Vector2(349.5f, -3f), LayerDefault, "Background");
        AddSprite(shaft, Art("rock/rock_tex.png"), OrderRoomBackdrop, new Vector2(4.5f, 3f), new Color(0.3f, 0.32f, 0.45f)).transform.localScale = new Vector3(2f, 2f, 1f);
        Spawn("FromA1_U1", new Vector2(308.5f, 4.5f));      // 柵が開いた後に A1_U1 から戻ってくる所 (柵の内側)
        SetDefaultSpawn(355.5f, 1.5f);                      // 既定の入口は噴水の池 (layout の FP-U2e が双方向になっても変えない)

        // ---- 西の境: 柵の向こう (遊歩道) が見えるように空を開け、遊歩道の床を続ける (A1_U1 の U2e と同じ高さ y3)
        Clear(274, 3, 306, 60);
        Clear(300, 15, 345, 60);
        Solid(274, 0, 303, 3);
        // A1_U1 への出口は柵の外側だけ (柵が閉じている間は触れない)
        var exitU1 = GameObject.Find("ExitToA1_U1");
        exitU1.transform.position = new Vector2(301.25f, 9f);
        exitU1.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 12f);
        LampPost(297.5f, 3f, 5f);
        LampPost(281f, 3f, 5f);
        Bunting(281f, 297.5f, 7.4f);

        // ---- 東の境 (FP2 の先) の岩は門の高さ +9 程度より上を空に
        Clear(447, 30, 475, 60);

        // ================= FP: 柵とレバー
        var fence = NewObject("FenceGate", new Vector2(304f, 3f), LayerGround, "Gimmicks");
        Box(fence, new Vector2(2f, 24f), false, new Vector2(0, 12f));
        var fs = new GameObject("Sprite");
        fs.transform.SetParent(fence.transform, false);
        fs.transform.localScale = Vector3.one * 1.7f;
        AddSprite(fs, Art("props/fence_gate.png"), OrderGimmick);
        AddComp<FlagDoor>(fence, ("_flagId", "A1_FenceOpen"), ("_slideDistance", 8f), ("_slideDuration", 1f));

        // レバー = 台座 + 傾く柄 (Drive「lever」, PendingChanges 42)
        var lever = LeverAt("FenceLever", 317f, 3f);
        AddComp<LeverSwitch>(lever, ("_progressFlag", "A1_FenceOpen"), ("_promptText", "閂を外す"),
            ("_activatedMessage", "柵の閂が外れた! 月明かりの遊歩道へ戻れる (拠点へ近道)"), ("_promptOffset", new Vector3(0, 2.4f, 0)));
        SetLeverHandle(lever);
        // 閂の表示灯 (レバーの柱の上): 閉 = 赤 / 開 = 緑
        var red = LampPost(318.9f, 3f, 1.8f, new Color(1f, 0.35f, 0.4f), OrderProp);
        var green = LampPost(318.9f, 3f, 1.8f, new Color(0.45f, 1f, 0.6f), OrderProp);
        var lampFlag = NewObject("FenceLampFlag", new Vector2(318.9f, 6f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(lampFlag, ("_flags", new[] { "A1_FenceOpen" }), ("_showWhenSet", new Object[] { green }),
            ("_hideWhenSet", new Object[] { red }));

        // ================= FP: 噴水
        // 噴水はビルダーの Fountain (Drive「fountain」の 1 枚絵 2 コマ, PendingChanges 42。以前は polish B の部品)。小噴水は A1_U1 の柵越しの予告と同じ値 (SmallFountain*)
        // 柵の脇の小噴水 (A1_U1 から柵越しに見えていた物と同じ位置・大きさ)
        Fountain(SmallFountainX, 3f, SmallFountainScale, SmallFountainTint, -9, "FountainSmall");
        Glow(309.2f, 6.8f, 2.6f, new Color(0.75f, 0.85f, 1f, 0.25f), -10);
        LampPost(313.8f, 3f, 4.5f, LampCool);
        // 大噴水 (池の真ん中、排水口の上) = 大階段の出口。受け皿 9.6 幅 / 高さ ≈9.8 (カメラ下限 4.5 で画面に全部入る)
        Fountain(349.5f, 0f, 1.6f, null, -9, "FountainGrand");
        Glow(349.5f, 7.5f, 7f, new Color(1f, 0.7f, 0.9f, 0.18f), -12);
        Glow(349.5f, 1.2f, 6f, new Color(0.7f, 0.85f, 1f, 0.25f), -6);
        // 池の水 (見た目だけ、当たり無し): 半透明の模様 + 手前の薄い水 + 水面の帯 (WaterVisual)
        WaterVisual(341f, 0f, 358f, 1.4f, WaterLook.Water, new Color(0.9f, 1f, 1f), -5, 4, "PoolWater", "Props", 0.5f, 0.28f);
        // 碑文 = 日誌その4
        KeyItemAt(339.2f, 3f, "日誌「閉園しなかった夜」その4 (噴水の碑文)", "A1_Lore_Journal4", "A1_Pickup_FP_1",
            "hall/inscription_plaque.png", "読む");

        // ---- FP の飾り: 街灯・旗・ベンチ代わりの花壇は置かず静かに
        foreach (var x in new[] { 323f, 333f, 362.5f, 375.5f })
            LampPost(x, x > 370f ? 9f : 3f, 5f, LampWarm);
        Bunting(323f, 333f, 7.6f);
        Bunting(362.5f, 375.5f, 9.4f);
        foreach (var x in new[] { 327.5f, 366f })
            Art2("hall/avenue_tree.png", x, 3f, 1.9f, -9, new Color(0.85f, 0.8f, 0.9f));

        // ================= FP2: 並木道
        foreach (var x in new[] { 386f, 397f, 408f })
            Art2("hall/avenue_tree.png", x, 9f, 2.4f, -9);
        foreach (var x in new[] { 391.5f, 402.5f, 413.5f })
            LampPost(x, 9f, 5.5f, LampWarm);
        Bunting(391.5f, 402.5f, 13.6f);
        Bunting(402.5f, 413.5f, 13.6f);

        // 閉じた売店 (屋根 y14 = 床から 5、二段ジャンプで乗る)
        Art2("props/shop_stall.png", 426f, 9f, 1.5f, -6, new Color(0.8f, 0.72f, 0.85f));
        var roof = NewObject("StallRoof", new Vector2(426f, 14f), LayerGround, "Terrain Objects");   // 屋根の絵の上面に合わせた見えないすり抜け床
        Box(roof, new Vector2(6f, 0.5f), false, new Vector2(0, -0.25f)).usedByEffector = true;
        var pe = roof.AddComponent<PlatformEffector2D>();
        pe.useOneWay = true;
        pe.surfaceArc = 180f;
        // コインの箱 (2026-10-03 PendingChanges 16: 決めた箱だけにコイン。取ったら二度と出ない。1 回の攻撃で壊れる箱は 1 つ = 17)
        // 閉じた売店の屋根 (y14, 二段ジャンプで乗る) の上に木箱 2 つ (右の箱にコイン)。
        // ふつうの壊せる木箱を隣に置いて、コインの箱だけが目立たないようにする (PendingChanges 20)
        Crate(426.6f, 14f);
        Crate(427.6f, 14f, "A1_Coin_FP2_1");

        // 正門 (扉は閉じたまま)。当たり = 門の形の箱 (上面 y20、アーチの上に立てる)
        var gate = Art2("hall/park_gate.png", 438.5f, 9f, 6.87f, -12);
        var body = NewObject("GateBody", new Vector2(438.5f, 14.5f), LayerGround, "Terrain Objects");
        Box(body, new Vector2(15f, 11f), false);
        Glow(438.5f, 19.5f, 6f, new Color(1f, 0.8f, 0.5f, 0.25f), -13);
        // 看板 (アーチのプレート)
        var sign = new GameObject("GateSign");
        sign.transform.SetParent(Group("Props"), false);
        sign.transform.position = new Vector3(438.5f, 18.85f, 0);
        var tmp = sign.AddComponent<TMPro.TextMeshPro>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/Fonts/JapaneseUI SDF.asset");
        tmp.text = "エリア2へ (準備中)";
        tmp.fontSize = 7f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.36f, 0.06f, 0.24f);
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(9f, 1.6f);
        sign.GetComponent<MeshRenderer>().sortingOrder = -11;
        // アーチの上の糸巻き1
        KeyItemAt(438.5f, 20.55f, "糸巻き1", "A1_Spool1", "A1_Pickup_FP2_1", "props/thread_spool.png", "拾う");
        Glow(438.5f, 20.6f, 1.8f, new Color(1f, 0.85f, 0.55f, 0.45f), -6);

        // 遠景: 共通プレハブの大観覧車 (遠すぎて映らない)。西の空に浮いて見えた遠景の観覧車の絵 (ferris_wheel_bg) は
        // 2026-10-10 ユーザー指示で削除 (周りとなじまない)。遠景は Depth() のスカイライン・並木が受け持つ
        PlaceFerrisWheel(false, false, new Color(0.55f, 0.5f, 0.7f));

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(328f, 3.6f, name: "PlazaWalker");          // 噴水広場の床 (x319..336)
        Enemy(410f, 18f, true, name: "AvenueFlyer");      // 並木道の上空 (正門のアーチの上 = 二段ジャンプの所は避ける)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_FP") + "\n";
        return log + Finish();
    }
}
