using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_U4 「観覧車の足元」(U4, 屋外: セーブ + 地図売り + 橙の点検口) + 「点検口の縦穴」(Uh, 落下攻撃の先 → A1_T3 へ一方通行)
// 左 = 観覧車の乗り場 A1_W1 (y6..18) / 右 = コーヒーカップ広場 A1_U3 (y6..18) / 床の点検口 (x96..102) → 縦穴 → A1_T3 の天井 (同じフラグ A1_Slam_Uh_1)
// 先に別の呼び出しで OpenOrCreate("A1_U4") を実行しておくこと。
public static class BuildU4
{
    static void Strut(float x, float y0, float y1)
    {
        var s = NewObject("Strut", new Vector2(x, (y0 + y1) * 0.5f), LayerDefault, "Props");
        s.transform.localScale = new Vector3(0.45f, 1f, 1f);
        AddSprite(s, Art("ferris/support_leg.png"), -6, new Vector2(2f, y1 - y0), new Color(0.45f, 0.38f, 0.55f));
    }

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    public static string Run()
    {
        var log = BuildBase("A1_U4");
        // 大観覧車 (全シーン共通の中心 (54,252) 半径 90)。足元の乗り場がこの部屋の西端に掛かる
        PlaceFerrisWheel(true, true);
        // 西 (A1_W1 = 観覧車の乗り場, 屋外) との境の壁は y27 まで。その上は空 (観覧車の脚と乗り場が続いて見える)
        Clear(20, 27, 57, 90);
        // 頭上を覆う観覧車の外周 (遠景の帯, カメラにほぼ追従)。足元から見上げた巨大な輪
        ParallaxProp("ferris/far_wheel_silhouette_arc.png", new Vector2(80f, 12.2f), new Vector2(0.9f, 1f), new Vector2(80f, 8f),
            1.6f, OrderFarBg - 1, new Color(0.6f, 0.56f, 0.85f, 0.38f), new Vector2(7.033f * 3f, 1.875f));

        // ================= U4 (x57..102, y0..27): 床 y0。西の出口 (y6) へ 2 段、東の出口 (y6) へは渡り板
        OneWay(61, 65, 3);       // 乗り場の切符売り場の前の段 (すり抜け: 奥の乗り場の建物が見える)
        OneWay(57, 61, 6);
        OneWay(86, 92, 3);
        OneWay(92, 102, 6);            // 東の渡り板 (下に点検口)
        Strut(89f, 0f, 2.6f); Strut(94.5f, 0f, 5.6f);

        // 床の橙の点検口 (落下攻撃で割れる)。A1_T3 の天井の点検口と同じフラグ
        SkillBlock(96, -3, 102, 0, PlayerSkill.GroundSlam, "A1_Slam_Uh_1");

        // セーブ (中ボス前の休憩所) と地図売り
        SavePointAt(73f, 0f);
        NpcAt(81.5f, 0f, "npc/npc_map_seller.png", "地図売り", new[]
        {
            "いらっしゃい。……おや、こんな夜更けに観覧車の見物かい?",
            "あの観覧車のてっぺんまで行くなら、西の乗り場からゴンドラを乗り継いでいくしかない。ぐるぐる回る足場に置いていかれないようにな。",
            "てっぺんの機械室には非常用のリフトがある。上でレバーを引いておけば、次からは乗り場から一気に上がれるはずだ。",
            "地図かい? 生憎ここらの分は売り切れでね。地下の蒸気の迷路にも同業者がいるから、あっちで聞いてみな。",
            "それと、足元の橙色の点検口。上から思いきり踏みつければ、楽屋裏まで真っ逆さまの近道さ。",
        }, new[]
        {
            "観覧車はゴンドラを乗り継いで、てっぺんの機械室へ。非常用リフトのレバーを忘れずにな。地図なら地下の蒸気の迷路の同業者に聞きな。",
        }, "A1_Npc_MapSellerU4", 0, flipX: true);

        // 飾り: 街灯・旗・行列の柵 (観覧車の待ち列)
        LampPost(66f, 0f, 5f); LampPost(78f, 0f, 5f); LampPost(88f, 3f, 4f);
        var bun = Prop("rock/decal_bunting.png", 72f, 4.9f, 1f, -6);
        bun.transform.localScale = new Vector3(4f, 1.3f, 1f);
        foreach (var x in new[] { 64.5f, 67.5f, 70.5f })
            Prop("props/fence_gate.png", x, 0f, 0.55f, -6, false, new Color(0.6f, 0.55f, 0.7f));

        // ================= Uh (x96..102, y-36..-3): 縦穴。途中の棚に糸×40。下端 → A1_T3 (一方通行)
        OneWay(96, 102, -18);
        ThreadPickupAt(99f, -17.2f, 40, "A1_Pickup_Uh_1");
        Solid(96, -27, 97, -26); Solid(101, -10, 102, -9);   // 壁の出っ張り (飾り兼目印)
        HangLamp(98f, -3f, 0.55f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        return log + Finish();
    }
}
