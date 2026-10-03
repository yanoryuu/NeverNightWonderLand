using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_T2 「崩れた搬入路」(T2 + 穴 T2 下段) + 「忘れ物の棚」(T2s, 隠し部屋)
// 先に別の呼び出しで OpenOrCreate("A1_T2") を実行しておくこと。
public static class BuildT2
{
    public static string Run()
    {
        var log = BuildBase("A1_T2");

        // ---- 床: 左右の出口 (y-96..-87) の下端 y=-96。中央に幅 7 の穴 (x65..72)
        //      走りジャンプ (≈5.6) では届かず、ジャンプ+回避ダッシュ (≈8.4) で越える
        Ground(48, 65, -96);
        Ground(72, 90, -96);

        // ---- 穴の底 (y-108) から歩いて左へ戻れる段 (各 +3)
        Solid(63, -108, 67, -99);   // P3: 上面 -99 (左の床へ +3)
        Solid(67, -108, 69, -102);  // P2
        Solid(69, -108, 71, -105);  // P1

        // ---- 左上: 入口側は天井の低い通路 (y-87)。その上に隠し部屋への小部屋 (アルコーブ)
        Solid(48, -87, 57, -81);    // 左上の岩
        Solid(57, -87, 65, -84);    // アルコーブの床 (上面 -84)
        HiddenWall(63, -84, 65, -81, "A1_Hidden_T2s");  // ひびの入った壁 (攻撃で壊れる, 永続化)
        // アルコーブの天井の穴 (x57..63, y-81..-75) を登って忘れ物の棚へ
        OneWay(58, 62, -81);
        OneWay(58, 62, -78);
        OneWay(57, 63, -75);        // 穴のふた = T2s の床

        // ---- 右側: 棚を登って上の渡り板へ (帰り道 + ひび壁の前に立つ足場)
        OneWay(82, 86, -93);
        OneWay(77, 81, -90);
        OneWay(72, 76, -87);
        OneWay(65, 73, -84);        // ひび壁の前の渡り板 (左からは届かない高さ)

        // ---- 飾り: 穴の底に落ちた搬入用のかご (崩れた搬入路)
        Prop("props/elevator_cabin.png", 73.3f, -106.4f, 1.1f).transform.rotation = Quaternion.Euler(0, 0, 9f);

        // ---- 弱い敵 1 体 (穴を越えた先)
        Enemy(81f, -95f, name: "WeakEnemy");

        // ---- T2s (x51..69, y-75..-60): 糸×30
        Solid(64, -75, 68, -74);    // 小さな台
        ThreadPickupAt(66f, -73.2f, 30, "A1_Pickup_T2s_1");
        // 忘れ物の棚 (壁沿いのすり抜け棚。登って遊べる)
        OneWay(51, 55, -69);
        OneWay(64, 69, -68);
        OneWay(56, 61, -65);
        Solid(51, -75, 54, -72);    // 左の段 (棚の雰囲気)

        // ---- ヒント
        Hint(55, -96, 65, -91, "[Shift] で回避ダッシュ!",
            "R2 で回避ダッシュ!", "HintDash");
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)
        Hint(72, -96, 79, -91, "[J] で攻撃!", "□ で攻撃!", "HintAttack");

        return log + Finish();
    }
}
