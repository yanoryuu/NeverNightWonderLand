using UnityEditor;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_U1 「メリーの吹き抜け」(U1, 拠点の真上の縦穴) + 「閉園の柵」(U2e, 屋外)
// 下 = 拠点 A1_H (床の穴 x255..267) / 左 = 月明かりの遊歩道 A1_U2 (y3..15) / 右 = 柵 (FlagDoor A1_FenceOpen) の向こうに噴水広場 A1_FP
// 上端の左の乗り場 = ジップラインの到着点 (A1_MB1 から一方通行, SpawnFromA1_MB1)
// 先に別の呼び出しで OpenOrCreate("A1_U1") を実行しておくこと。
public static class BuildU1
{
    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_U1");

        // ================= 地上の見た目: U1 の屋根 (y12..18) より上の岩を消して空を見せる (A1_U2 側と同じ高さ)
        Clear(236, 18, 276, 60);
        // 屋根の上の回転木馬 (= この縦穴は木馬の真下の支柱の点検路)
        Prop("props/carousel.png", 261f, 18f, 1.5f, -6);

        // ================= U1 (x252..270, y-21..12): 縦穴。床 y-21 (中央 x255..267 は拠点への穴)
        // 支柱 (飾り): 拠点の回転木馬の芯棒が屋根まで貫く
        var pole = NewObject("CarouselPole", new Vector2(261f, -4.5f), LayerDefault, "Props");
        pole.transform.localScale = new Vector3(0.9f, 1f, 1f);
        AddSprite(pole, Art("ferris/support_leg.png"), -6, new Vector2(2f, 33f), new Color(0.95f, 0.72f, 0.85f));

        // 支柱に据わった馬の台座 = すり抜け足場 (左右交互, 段差 3)
        OneWay(261, 266, -18);
        OneWay(255, 260, -15);
        OneWay(262, 267, -12);
        OneWay(255, 260, -9);
        OneWay(262, 267, -6);
        OneWay(255, 260, -3);
        OneWay(262, 267, 0);
        // 壁の出っ張り (休憩所)
        Solid(252, -12, 254, -11);
        Solid(268, -3, 270, -2);

        // 上端の乗り場 y3: 左は岩の床 (遊歩道への出口)、中〜右はすり抜けの床 (下から登れる・↓+ジャンプで降りる)
        Solid(252, 0, 256, 3);
        OneWay(256, 270, 3);

        // ジップラインの到着点 (A1_MB1 から一方通行)。到着側には移動装置を置かない
        Prop("props/zipline_station.png", 253.6f, 3f, 1f, OrderProp);
        // 到着してくるケーブル (遊歩道側の出口の上から差し込む)
        var cable = NewObject("ZiplineCable", new Vector2(251.6f, 9.7f), LayerDefault, "Props");
        cable.transform.rotation = Quaternion.Euler(0, 0, -58f);
        AddSprite(cable, Art("ferris/spoke_beam.png"), -6, new Vector2(9f, 0.25f), new Color(0.55f, 0.55f, 0.62f));
        Spawn("FromA1_MB1", new Vector2(257.5f, 4.5f));
        HangLamp(259f, 12f); HangLamp(267f, 12f);

        // ================= U2e (x276..303, y0..15): 閉園の柵。床 y3
        Solid(270, 0, 306, 3);
        LampPost(281f, 3f, 5f);
        LampPost(297.5f, 3f, 5f);
        var bunting = Prop("rock/decal_bunting.png", 289.2f, 7.4f, 1.6f, -6);
        bunting.transform.localScale = new Vector3(5.2f, 1.4f, 1f);

        // 柵 (FlagDoor A1_FenceOpen): 噴水広場の側のレバーで開く。開くまで柵越しに噴水が見えるだけ
        var fence = NewObject("FenceGate", new Vector2(304f, 3f), LayerGround, "Gimmicks");
        Box(fence, new Vector2(2f, 24f), false, new Vector2(0, 12f));   // 上を跳び越えられない高さ (絵より高い)
        var fs = new GameObject("Sprite");
        fs.transform.SetParent(fence.transform, false);
        fs.transform.localScale = Vector3.one * 1.7f;
        AddSprite(fs, Art("props/fence_gate.png"), OrderGimmick);
        AddComp<FlagDoor>(fence, ("_flagId", "A1_FenceOpen"), ("_slideDistance", 8f), ("_slideDuration", 1f));

        // 柵が開いた後の戻り道: 柵の奥に A1_FP への出口 (A1_FP 側に SpawnFromA1_U1 が要る → CrossSceneContracts)
        // (layout の FP-U2e は双方向 = 自動の ExitToA1_FP が通路 x303.5..309 に出来るので、消して柵の奥だけに置き直す)
        var autoExit = GameObject.Find("ExitToA1_FP");
        if (autoExit != null) Object.DestroyImmediate(autoExit);
        var exit = NewObject("ExitToA1_FP", new Vector2(307.5f, 9f), LayerIgnoreRaycast, "Transitions");
        Box(exit, new Vector2(3f, 12f), true);
        AddComp<SceneTransitionZone>(exit, ("_sceneName", "A1_FP"), ("_entranceId", "FromA1_U1"));

        // 柵の向こう = 噴水広場の予告 (見えるだけ): 空を開け、床と噴水を置く
        Clear(303, 15, 345, 60);
        Clear(306, 3, 345, 15);
        Solid(306, -3, 316, 3);
        // polish B: A1_FP の小噴水 (FountainSmall) と同じ部品・値 (x 309.2 / 床 y3 / 倍率 0.8 / 色) — 遷移しても同じ物に見える
        Fountain(309.2f, 3f, 0.8f, new Color(0.86f, 0.82f, 0.98f), -9, "FountainTeaser");
        LampPost(313.8f, 3f, 4.5f, new Color(0.85f, 0.9f, 1f));

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(265f, -9f, true, name: "ShaftFlyer");      // 吹き抜けの途中 (支柱を登る間の空中戦)
        Enemy(285f, 3.6f, name: "FenceWalker");          // 閉園の柵の前の床 (x281..290)

        return log + Finish();
    }
}
