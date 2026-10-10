using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_C1 「灯の大扉の回廊」(C1)
// 3 つの灯の大扉 (スピーカー①②③で開く、灯は FlagActivator) / エリアボス前のベンチ /
// 床下の搬出エレベーター (E1, A1_MB2 と往復。flag A1_ElevatorDeep で動く) の到着口。
// 先に別の呼び出しで OpenOrCreate("A1_C1") を実行しておくこと。
public static class BuildC1
{
    public static string Run()
    {
        var log = BuildBase("A1_C1");

        // ---- 床 y-54 (左右の出口 y-54..-45 の下端)
        Ground(291, 315, -54);

        // ---- 床下の搬出エレベーター: 深さ 3 の乗り場 (x299..304)。レイアウトの縦穴 (x297..306, 下へ) は埋め戻す
        Solid(297, -63, 306, -57);
        Clear(299, -57, 304, -54);
        Prop("props/elevator_cabin.png", 301.5f, -54.6f, 1.6f, -4);
        TransportAt(new Vector2(301.5f, -55.5f), new Vector2(4f, 3f), "A1_MB2", "FromA1_C1",
            new[] { "A1_ElevatorDeep" }, "乗る", "搬出エレベーターは止まっている…… 地下の奥で動かせそうだ",
            name: "ElevatorDeep");
        Spawn("FromA1_MB2", new Vector2(301.5f, -55.5f));
        MoveSpawn("A1_AB", 309.6f, -52.5f);   // 大扉の手前 (扉の当たりは開くと消える)

        // ---- エリアボス前のベンチ
        SavePointAt(307f, -54f);

        // ---- 灯の大扉 (FlagDoor: スピーカー①②③)。絵は子、扉の上部の 3 つの丸穴に灯を入れる
        var door = NewObject("GreatDoor", new Vector2(313f, -54f), LayerGround, "Gimmicks");
        Box(door, new Vector2(4f, 9f), false, new Vector2(0, 4.5f));
        var art = new GameObject("Sprite");
        art.transform.SetParent(door.transform, false);
        art.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        AddSprite(art, Art("props/boss_door.png"), -3);   // 岩 (0) の裏へスライドして消える
        AddComp<FlagDoor>(door, ("_flagId", "A1_Speaker1"), ("_additionalFlags", new[] { "A1_Speaker2", "A1_Speaker3" }),
            ("_slideDistance", 9.5f), ("_slideDuration", 2f));

        var sockets = new[] { new Vector2(-0.71f, 4.26f), new Vector2(0f, 4.35f), new Vector2(0.73f, 4.26f) };
        for (var i = 0; i < 3; i++)
        {
            var off = new GameObject("LampOff" + (i + 1));
            off.transform.SetParent(art.transform, false);
            off.transform.localPosition = sockets[i];
            off.transform.localScale = Vector3.one * 0.55f;
            AddSprite(off, Art("props/boss_door_lamp_on.png"), -2, null, new Color(0.22f, 0.17f, 0.26f));
            var on = new GameObject("LampOn" + (i + 1));
            on.transform.SetParent(art.transform, false);
            on.transform.localPosition = sockets[i];
            on.transform.localScale = Vector3.one * 0.6f;
            AddSprite(on, Art("props/boss_door_lamp_on.png"), -1);
            on.SetActive(false);
            var act = NewObject("LampFlag" + (i + 1), new Vector2(313f + sockets[i].x * 1.5f, -54f + sockets[i].y * 1.5f), LayerDefault, "Gimmicks");
            AddComp<FlagActivator>(act, ("_flags", new[] { "A1_Speaker" + (i + 1) }),
                ("_showWhenSet", new Object[] { on }), ("_hideWhenSet", new Object[] { off }));
        }

        // ---- 飾り: 回廊の灯 (天井から吊る)
        foreach (var x in new[] { 294.5f, 308.5f })
        {
            var lamp = Prop("props/boss_door_lamp_on.png", x, -43.2f, 0.7f, OrderProp, false, new Color(1f, 0.8f, 0.55f));
            lamp.transform.rotation = Quaternion.Euler(0, 0, 180f);
        }

        // ---- ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_C1") + "\n";
        return log + Finish();
    }
}
