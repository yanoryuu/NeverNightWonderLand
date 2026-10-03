using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_T1 「目覚めの楽屋」(T1) + 「衣装棚の奥」(T1s)
// 先に別の呼び出しで OpenOrCreate("A1_T1") を実行しておくこと。
public static class BuildT1
{
    public static string Run()
    {
        var log = BuildBase("A1_T1");

        // ---- T1 (x9..45, y-99..-78): 床は通路 (y-96..-87) の下端に合わせて y=-96
        Ground(9, 45, -96);
        // 低い段差が2つ (1 段 → 2 段、その先で下りて出口へ)
        Solid(26, -96, 30, -95);
        Solid(30, -96, 37, -94);

        // 左上の衣装棚: すり抜けの棚を 3 単位ずつジグザグに登り、天井の穴 (x18..24) から衣装棚の奥へ
        OneWay(19, 23, -93);
        OneWay(13, 17, -90);
        OneWay(19, 23, -87);
        OneWay(13, 17, -84);
        OneWay(19, 23, -81);
        OneWay(19, 23, -78);   // 穴の入口
        OneWay(19, 23, -75);   // 穴の中
        OneWay(18, 24, -72);   // 穴のふた = 衣装棚の奥の床

        // ---- T1s (x12..27, y-72..-57): 床は岩 (y=-72)。日誌を奥に
        Solid(12, -72, 15, -70);   // 左奥の小さな台 (単調さを崩す)
        KeyItemAt(25.8f, -72f, "日誌「閉園しなかった夜」その1", "A1_Lore_Journal1", "A1_Pickup_T1s_1");

        // ---- スポーン
        OneWay(15, 19, -68);       // 奥の棚 (飾り兼ちょっとした足場)
        OneWay(21, 26, -65);

        SetDefaultSpawn(12.5f, -94.5f);

        // ---- 操作ヒント
        Hint(9, -96, 22, -92, "[A] / [D] で移動", "左スティックで移動", "HintMove");
        Hint(22, -96, 38, -92, "[Space] でジャンプ! 長押しで高く跳べる", "× でジャンプ! 長押しで高く跳べる", "HintJump");
        Hint(9, -92, 25, -78, "棚には下から跳び乗れる。[↓]+[Space] で降りる", "棚には下から跳び乗れる。↓+× で降りる", "HintShelf");
        Hint(19, -72, 27, -67, "[E] で調べる", "スティック/十字キーの ↑ で調べる", "HintInteract");
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
