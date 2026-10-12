using System.Linq;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_MB1 「ゴンドラの道化」(MB1, 観覧車のてっぺん x18..84 y348..372。足場は外周の上 y340..344 に下げた) + 「第2スピーカー」(S2 x87..105 y354..372)
// 下 = てっぺんへの梯子 A1_W8 (中央の格子 x45..57)。外周の上に架けた細い鉄骨の上で、浮遊する中ボスと戦う (空に囲まれている)。
// 足場の隙間・両端から落ちると A1_W8 の最上段 (FromA1_MB1) に戻る = 梯子の上から再挑戦。
// 中ボス撃破 (A1_MidBossUpper) で東の扉が開く → S2: スピーカー② (A1_Speaker2)。壊すと A1_ZiplineReady が立ち、
// 星空ジップライン (TransportInteractable → A1_U1 / FromA1_MB1, 一方通行) で拠点の真上まで滑り降りる。
// アリーナの真上 (y370) に星見台への足場: 大ジャンプ (24.5) でだけ届く。先は別エリア (準備中) の目印だけ。
// 先に別の呼び出しで OpenOrCreate("A1_MB1") を実行しておくこと。
public static class BuildMB1
{
    /// <summary>中ボス「カップ」のプレハブ (SceneScripts/prep_midboss_cups.cs で作成)。</summary>
    const string CupsBossPrefabPath = "Assets/Prefabs/Enemies/Boss_Cups.prefab";

    /// <summary>
    /// BossRoom が置いた仮ボスを中ボス「カップ」(Boss_Cups.prefab) に差し替える (Area1StageBuilder.UseGramineBoss と同じ作法)。
    /// 位置・糸の上書き (_threadDropOverride) は仮ボスから引き継ぎ、BossArena._boss = ルート (合体後のカップ) の EnemyController、
    /// カップの闘技場の範囲 (CupBossBehaviour._arenaArea) = BossArena のトリガー。返り値 = 新しいボス。
    /// </summary>
    static GameObject UseCupsBoss(GameObject arena, string name = "Boss_Cups")
    {
        var ba = arena.GetComponent<BossArena>() ?? throw new System.ArgumentException("BossArena が無い: " + arena.name);
        var so = new UnityEditor.SerializedObject(ba);
        var old = so.FindProperty("_boss").objectReferenceValue as EnemyController;
        var pos = old != null ? old.transform.position : arena.transform.position;
        var threadDrop = old != null ? new UnityEditor.SerializedObject(old).FindProperty("_threadDropOverride").intValue : 0;
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CupsBossPrefabPath)
                     ?? throw new System.InvalidOperationException(CupsBossPrefabPath + " がありません (SceneScripts/prep_midboss_cups.cs)");
        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        go.transform.SetParent(Group("Enemies"), true);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.name = name;
        var ec = go.GetComponent<EnemyController>();
        if (threadDrop > 0)
            Set(ec, "_threadDropOverride", threadDrop);
        Set(go.GetComponent<CupBossBehaviour>(), "_arenaArea", arena.GetComponent<Collider2D>());
        Set(ba, "_boss", ec);
        return go;
    }

    static readonly Color Steel = new Color(0.95f, 0.86f, 0.98f);
    static readonly Color Post = new Color(0.55f, 0.48f, 0.66f);

    // 外周の中心線の高さ (共通の観覧車: 中心 (54,252) 半径 90)
    static float RimY(float x) => 252f + Mathf.Sqrt(90f * 90f - (x - 54f) * (x - 54f));

    // 外周の上に架けた鉄骨 (外周から立てた支柱つき)
    static GameObject Walk(int x0, int x1, int top, string name = "Girder")
    {
        var g = Girder(x0, x1, top, Steel, name);
        foreach (var x in new[] { x0 + 0.7f, x1 - 0.7f })
        {
            var rim = RimY(x) + 1f;
            if (rim < top - 0.6f)
                Truss(x, rim, top - 0.3f, 0.45f, -6, Post);
        }
        return g;
    }

    public static string Run()
    {
        var log = BuildBase("A1_MB1");
        SkyBackdrop();
        PlaceFerrisWheel(true, false);    // 外周の上端 (y≈342) と電球、吊られたゴンドラが足元に
        PlateWallsAll();
        RockDecor(0.3f);

        // ================= 空を開ける: アリーナの下・上・西、S2 の下・上・東。東の扉の柱 (x84..87) だけ天まで残す
        Clear(-10, 290, 84, 348);     // アリーナの下 (W8 への口の掘削も含めて空)
        Clear(-10, 348, 18, 430);     // 西
        Clear(-10, 372, 84, 430);     // アリーナの上
        Clear(84, 290, 140, 346);     // S2 の下
        Clear(87, 372, 140, 430);     // S2 の上
        Clear(105, 346, 140, 430);    // S2 の東 (ジップラインが空へ伸びる)
        Solid(84, 361, 87, 404);      // 扉の柱 (大ジャンプでも越えられない高さまで)
        // 眼下の遠い園内 (画面の下端に張り付く遠景)
        ParallaxProp("bg/bg_park_silhouette.png", new Vector2(51f, 340.3f), new Vector2(0.98f, 1f), new Vector2(51f, 346f),
            0.9f, OrderFarBg, new Color(0.5f, 0.42f, 0.72f, 0.85f), new Vector2(80f, 1.9f));
        // カメラの下限を下げて足元の外周を見せる
        var limit = Object.FindAnyObjectByType<CameraLimit>();
        if (limit != null) Set(limit, "_minY", 341f);

        // ================= 足場 (外周の上の細い鉄骨)。隙間 x30..33 / x71..73 から落ちると A1_W8 へ
        Walk(21, 29, 340, "WestEnd");
        Walk(32, 44, 343);
        Girder(44, 58, 344, new Color(0.62f, 0.55f, 0.72f), "Grate");   // A1_W8 への口 (↓+ジャンプで降りる)
        Walk(58, 71, 343);
        Walk(74, 83, 340, "EastEnd");
        // 空中戦用の浮き足場
        Girder(24, 30, 345, Steel);
        Girder(36, 42, 348, Steel);
        Girder(48, 56, 351, Steel, "MidHigh");
        Girder(62, 68, 348, Steel);
        // 扉の前の段 (東端 340 → 343 → 346 → 扉の床 349)
        Girder(74, 80, 343, Steel);
        Girder(78, 84, 346, Steel);
        MoveSpawn("A1_W8", 60f, 344.5f);
        SetDefaultSpawn(60f, 344.5f);

        // ================= 中ボス (BossRoom が置いた浮遊型の仮ボスを、下の UseCupsBoss で「カップ」に差し替える)
        var arenaGo = BossRoom(new RectInt(20, 340, 63, 32), "A1_MidBossUpper", new Vector2(54f, 357f), true, new RectInt[0],
            "カップを倒した! 東のスピーカー塔の扉が開いた",
            // 赤ハサミを未入手なら拠点の鍛冶屋への誘導の一文 (PendingChanges 11b, 呼びかけ調にしない)
            "ハサミがかすかに震えた。……拠点の鍛冶屋なら、何か分かるかもしれない", followUpUnlessRed: true,
            threadDrop: 25);   // 中ボスの糸 25 本 → 12 粒 (PendingChanges 18)
        // 仮ボスを中ボス「カップ」(Boss_Cups.prefab, Docs/EnemySpec.md「上の中ボス「カップ」」) に差し替える
        UseCupsBoss(arenaGo);
        // ゲート: 戦闘中は格子に錠 (↓+ジャンプで逃げられない。落ちれば W8 へ戻る)
        var lockGo = NewObject("GrateLock", new Vector2(51f, 344f), LayerGround, "Gimmicks");
        var lockArt = new GameObject("Beam");
        lockArt.transform.SetParent(lockGo.transform, false);
        lockArt.transform.localPosition = new Vector3(0, -0.266f, 0);
        AddSprite(lockArt, Art("ferris/leg_crossbrace.png"), OrderGimmick + 1, new Vector2(14f, 1f), new Color(1f, 0.45f, 0.5f));
        Box(lockGo, new Vector2(14f, 0.6f), false, new Vector2(0, -0.3f));
        lockGo.SetActive(false);
        Set(arenaGo.GetComponent<BossArena>(), "_gates", new[] { lockGo });

        // ================= 東の扉 (撃破で開く) と S2 (床 y349)
        Clear(84, 349, 108, 357);
        Ground(84, 108, 349, 3);
        // 開き戸 (PendingChanges 42): 扉は高さ 4、その上 (353..361) は FlagSwingDoor が壁で塞ぐ
        // (= 闘技場の東の壁 (361 より上) が扉の上まで下りてきて、壁に扉が付いた形になる)
        FlagSwingDoor("S2Door", new RectInt(84, 349, 3, 12), "A1_MidBossUpper", -1);

        var speaker = SpeakerAt(93.5f, 349f, "A1_Speaker2", 2);
        Set(speaker.GetComponent<BreakableSpeaker>(), "_breakMessage",
            "スピーカー②を壊した! 音楽の一部が止んだ…… 拠点の奥の大扉に灯がもうひとつ (2/3)。ジップラインに動力が戻った");
        var broken = Prop(SpeakerBrokenArt(2), 93.5f, 349f, 1f, OrderProp);
        broken.SetActive(false);
        var act = NewObject("Speaker2Flag", new Vector2(93.5f, 351f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { "A1_Speaker2" }), ("_showWhenSet", new Object[] { broken }),
            ("_setFlagsWhenSatisfied", new[] { "A1_ZiplineReady" }));

        // 星空ジップラインの乗り場 (東端)。ケーブルは東の空へ、拠点 (A1_U1) の真上まで
        Prop("props/zipline_station.png", 101.5f, 349f, 1.4f, OrderProp);
        var cable = NewObject("ZiplineCable", new Vector2(125.2f, 347.4f), LayerDefault, "Props");
        cable.transform.rotation = Quaternion.Euler(0, 0, -13f);
        AddSprite(cable, Art("ferris/spoke_beam.png"), -6, new Vector2(46f, 0.18f), new Color(0.75f, 0.75f, 0.85f));
        TransportAt(new Vector2(102.5f, 350.5f), new Vector2(3f, 3f), "A1_U1", "FromA1_MB1", new[] { "A1_ZiplineReady" },
            "滑り降りる", "ジップラインは止まっている…… スピーカーに動力を取られているようだ", name: "StarZipline");
        var lampOff = Prop("props/boss_door_lamp_on.png", 100.95f, 353.7f, 0.4f, OrderProp, false, new Color(1f, 0.35f, 0.4f));
        var lampOn = Prop("props/boss_door_lamp_on.png", 100.95f, 353.7f, 0.4f, OrderProp, false, new Color(0.45f, 1f, 0.6f));
        var lamp = NewObject("ZiplineLampFlag", new Vector2(100.95f, 354.5f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(lamp, ("_flags", new[] { "A1_ZiplineReady" }), ("_showWhenSet", new Object[] { lampOn }),
            ("_hideWhenSet", new Object[] { lampOff }));

        // ================= 星見台 (大ジャンプでだけ届く。先は別エリア: 準備中)
        Girder(50, 58, 366, new Color(1f, 0.95f, 0.75f), "StarDeck");
        var ladder = NewObject("StarLadder", new Vector2(54.5f, 371f), LayerDefault, "Props");
        AddSprite(ladder, Art("tiles/tile_ladder.png"), OrderProp, new Vector2(1f, 10f), new Color(1f, 0.95f, 0.8f, 0.85f));
        foreach (var (x, y, s) in new[] { (51f, 367.6f, 0.5f), (57f, 368.2f, 0.45f), (53f, 374.5f, 0.6f), (56.5f, 372f, 0.4f) })
            Prop("ferris/rim_bulb.png", x, y, s, OrderProp, false, new Color(1f, 0.95f, 0.7f));
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)
        // (撃破後の大ジャンプの案内 HintSuper とその表示切替 SuperHintFlag は削除)

        // ================= 落下: アリーナの下の全幅 → A1_W8 (FromA1_MB1)。自動の出口ゾーン (口の下) を広げる
        var fall = Group("Transitions").Find("ExitToA1_W8");
        if (fall != null)
        {
            fall.position = new Vector3(63f, 332f, 0f);
            fall.GetComponent<BoxCollider2D>().size = new Vector2(110f, 12f);
        }

        // ================= ヒント

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_MB1") + "\n";
        // 雰囲気 (§15): 作り直しで消えないよう StageAtmosphere をレシピで置く (2026-10-12)
        log += Atmosphere("A1_MB1") + "\n";
        return log + Finish();
    }
}
