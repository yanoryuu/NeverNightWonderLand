using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_T4 「チケット売り場の廊下」(T4)
// ここまでの操作の総ざらい (段差 / 溝のダッシュ越え / 棚) + 重装甲の敵でブレイク→裁断 + ボス前の仮眠ベンチ (セーブ)。
// 先に別の呼び出しで OpenOrCreate("A1_T4") を実行しておくこと。
public static class BuildT4
{
    // 奥の壁に半分埋まった大歯車 (背景の飾り。岩 (0) の裏、暗く沈める)
    static void BgGear(float x, float y, float scale, float rot = 0f)
    {
        var g = Prop("platforms/gear_platform.png", x, y, scale, -7, false, new Color(0.3f, 0.26f, 0.4f));
        g.transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    // 天井から吊った灯
    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_T4");

        // ---- 床: 左 (A1_T3 から) y-54、右 (A1_B1 へ) y-57。途中に幅 6 の溝 (ダッシュ越え、底 y-57 から両側へ登れる)
        Solid(117, -60, 128, -54);
        Solid(128, -60, 134, -57);     // 溝の底
        Solid(134, -60, 146, -54);     // 重装甲の敵の床
        Ground(146, 162, -57);         // 一段下りてベンチと番人の扉

        // ---- 天井: 梁が低く垂れた区間 (単調さを崩す)
        Solid(136, -44, 146, -42);
        Solid(150, -45, 154, -42);

        // ---- チケット売り場 (閉まった窓口) と、その上の棚
        Prop("props/shop_stall.png", 122.5f, -54f, 1f, OrderProp, false, new Color(0.62f, 0.58f, 0.72f));
        OneWay(119, 125, -50);
        OneWay(126, 131, -47);

        // ---- 重装甲の敵 (ブレイク→裁断の練習)
        Enemy(140.5f, -52.7f, scale: 1.3f, heavy: true, name: "HeavyEnemy");

        // ---- ボス前の仮眠ベンチ (セーブ)
        SavePointAt(153.5f, -57f);
        Prop("props/boss_door_lamp_on.png", 151.2f, -56.7f, 0.6f, OrderProp, false, new Color(1f, 0.85f, 0.6f));
        Prop("props/boss_door_lamp_on.png", 155.8f, -56.7f, 0.6f, OrderProp, false, new Color(1f, 0.85f, 0.6f));

        // ---- ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)
        Hint(134, -54, 138, -49, "重い敵は白い防御ゲージを削るとブレイク! ブレイク中に [L] で裁断できる",
            "重い敵は白い防御ゲージを削るとブレイク! ブレイク中に R1 で裁断できる", "HintBreak");

        // ---- 飾り (奥の歯車・吊り灯)
        BgGear(131f, -42.5f, 1.8f, 12f); BgGear(160.5f, -44f, 1.4f);
        HangLamp(123f, -42f); HangLamp(141f, -44f); HangLamp(158f, -42f);

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(151.5f, -48f, true, name: "CorridorFlyer");   // 重装甲の敵の先・ベンチの手前の空中 (ブレイク練習の後の総ざらい)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_T4") + "\n";
        return log + Finish();
    }
}
