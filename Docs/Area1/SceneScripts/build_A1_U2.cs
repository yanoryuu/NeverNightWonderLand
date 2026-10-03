using System.Linq;
using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_U2 「月明かりの遊歩道」(U2, 屋外) + 「売店の屋根」(U2a, 屋外・任意の登り) + 「裏方の通用口」(U2r, 床下) + 「裏方の倉庫」(U2r2, 突進の先)
// 右 = メリーの吹き抜け A1_U1 (y3..15) / 左 = コーヒーカップ広場 A1_U3 (y6..18)
// 先に別の呼び出しで OpenOrCreate("A1_U2") を実行しておくこと。
public static class BuildU2
{
    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // 街灯 2 本の間に旗飾りを渡す
    static void BuntingBetween(float x0, float x1, float y)
    {
        var b = Prop("rock/decal_bunting.png", (x0 + x1) * 0.5f, y, 1f, -6);
        b.transform.localScale = new Vector3((x1 - x0) / 3f, 1.3f, 1f);
    }

    // 足場を支える細い支柱 (飾り)
    static void Strut(float x, float y0, float y1)
    {
        var s = NewObject("Strut", new Vector2(x, (y0 + y1) * 0.5f), LayerDefault, "Props");
        s.transform.localScale = new Vector3(0.45f, 1f, 1f);
        AddSprite(s, Art("ferris/support_leg.png"), -6, new Vector2(2f, y1 - y0), new Color(0.45f, 0.38f, 0.55f));
    }

    // 足場 [x0,x1) (上面 top) を一番近い柱 (posts) から腕木で支える: 足場の真下の梁 + 柱から足場の中ほどへの斜めの方杖
    static void PlatformArm(int x0, int x1, int top, float[] posts)
    {
        var mid = (x0 + x1) * 0.5f;
        var post = posts.OrderBy(p => Mathf.Abs(p - mid)).First();
        var col = new Color(0.5f, 0.42f, 0.6f);
        var ax0 = Mathf.Min(x0 + 0.3f, post);
        var ax1 = Mathf.Max(x1 - 0.3f, post);
        var arm = NewObject("PlatformArm", new Vector2((ax0 + ax1) * 0.5f, top - 0.95f), LayerDefault, "Props");
        arm.transform.localScale = new Vector3(1f, 0.45f, 1f);
        AddSprite(arm, Art("ferris/leg_crossbrace.png"), -6, new Vector2(ax1 - ax0, 1f), col);
        // 方杖: 柱の 2.2 下から、足場の中ほど (柱から遠い側) の腕木へ
        var far = Mathf.Abs(x0 - post) > Mathf.Abs(x1 - post) ? x0 + 1.2f : x1 - 1.2f;
        var a = new Vector2(post, top - 3.2f);
        var b = new Vector2(Mathf.Lerp(post, far, 0.7f), top - 1.0f);
        var len = Vector2.Distance(a, b);
        if (len < 1.2f) return;
        var knee = NewObject("PlatformKnee", (a + b) * 0.5f, LayerDefault, "Props");
        knee.transform.localScale = new Vector3(0.3f, 1f, 1f);
        knee.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg - 90f);
        AddSprite(knee, Art("ferris/support_leg.png"), -7, new Vector2(2f, len), col);
    }

    public static string Run()
    {
        var log = BuildBase("A1_U2");

        // ================= 地上の見た目: 東の A1_U1 の建物 (屋根 y18) の上は空、屋根に回転木馬 (A1_U1 と同じ)
        Clear(249, 18, 290, 80);
        Prop("props/carousel.png", 261f, 18f, 1.5f, -6);
        // 西の A1_U3 との境の外壁は y30 まで (その上は空)
        Clear(100, 30, 150, 80);

        // 遠景: 西の空に大観覧車 (目的地)。カメラにほぼ追従 = とても遠い。東端 (カメラ x240) で画面の左上、西へ歩くほど正面へ
        ParallaxProp("props/ferris_wheel_bg.png", new Vector2(231f, 9.5f), new Vector2(0.92f, 0.95f), new Vector2(240f, 6f),
            1.15f, OrderFarBg - 1, new Color(0.5f, 0.44f, 0.7f, 0.7f));

        // ================= U2 (x150..249, y0..27): 床 y0。西端は y6 (A1_U3 への出口)、東端は y3 (A1_U1 への出口)
        Solid(150, 0, 156, 3);
        Solid(150, 3, 153, 6);
        Solid(243, 0, 249, 3);

        // 床の切れ目 (x159..165) = 裏方の通用口への格子。↓+ジャンプで降りる
        OneWay(159, 165, 0);

        // 空中戦の足場 (花壇の縁・低い台)。浮遊する敵に跳んで攻撃するための踏み台
        OneWay(180, 185, 3);
        OneWay(188, 192, 6);
        OneWay(201, 206, 3);
        OneWay(212, 216, 6);
        Strut(182.5f, 0f, 2.6f); Strut(190f, 0f, 5.6f); Strut(203.5f, 0f, 2.6f); Strut(214f, 0f, 5.6f);

        // 浮遊する敵×3 (東から来ると 214 → 196 → 172 の順に出会う)
        Enemy(214f, 9f, flying: true, name: "FlyerWalk1");
        Enemy(196f, 8f, flying: true, name: "FlyerWalk2");
        Enemy(172f, 7f, flying: true, name: "FlyerWalk3");

        // 遊歩道の飾り: 街灯と旗飾り、閉まった屋台
        foreach (var x in new[] { 157f, 169f, 177f, 197f, 209f, 221f })
            LampPost(x, 0f, 5f);
        BuntingBetween(169f, 177f, 4.8f);
        BuntingBetween(197f, 209f, 4.8f);
        Prop("props/shop_stall.png", 194.5f, 0f, 1.1f, -6, false, new Color(0.62f, 0.56f, 0.7f));   // 閉まった屋台 (奥)
        LampPost(151.5f, 6f, 4f);   // 西の入口 (コーヒーカップ広場の外壁の前)

        // ================= 売店 (x229..239 の大きな屋台): 屋根に登って U2a へ
        Prop("props/shop_stall.png", 234.5f, 0f, 2.5f, -5);     // 10x10 (屋根の上面 ≈ y8)
        OneWay(240, 245, 6);   // 東の段 (y3) → 屋台の脇の棚
        OneWay(230, 240, 9);   // 屋根 (日除けの上)
        OneWay(224, 229, 12);
        OneWay(231, 236, 15);
        // 屋台の裏の荷揚げリフト (y15 → y33)
        Lift(new Vector2(239.5f, 14.5f), new[] { Vector2.zero, new Vector2(0, 18f) }, 2.5f, 1.5f);

        // ================= U2a (x222..249, y33..57): 屋根伝いの登り。上に道具「ボビン爆弾」
        OneWay(242, 248, 33);
        OneWay(235, 240, 36);
        OneWay(228, 233, 39);
        OneWay(222, 227, 42);
        OneWay(229, 234, 45);
        OneWay(236, 241, 48);
        OneWay(243, 249, 51);
        // 売店の裏の看板塔の骨組み (飾り): 左右の太い柱 + 足場の高さごとの筋交い。足場はこの骨組みに架かっている
        foreach (var (px, baseY) in new[] { (222.5f, 0f), (248.2f, 3f) })   // 右の柱は東の段 (y3) の上に立つ
        {
            var post = NewObject("SignTowerPost", new Vector2(px, (baseY + 53f) * 0.5f), LayerDefault, "Props");
            post.transform.localScale = new Vector3(0.55f, 1f, 1f);
            AddSprite(post, Art("ferris/support_leg.png"), -7, new Vector2(2f, 53f - baseY), new Color(0.5f, 0.42f, 0.6f));
        }
        foreach (var by in new[] { 11.5f, 20.5f, 29.5f, 38.5f, 47.5f })
        {
            // polish A: 背景の梁は細く暗く (足場と見分ける)
            var brace = NewObject("SignTowerBrace", new Vector2(235.5f, by), LayerDefault, "Props");
            brace.transform.localScale = new Vector3(1f, 0.6f, 1f);
            AddSprite(brace, Art("ferris/leg_crossbrace.png"), -8, new Vector2(26f, 1f), new Color(0.34f, 0.29f, 0.44f));
        }
        // polish A: 足場が宙に浮いて見えないよう、塔の中央に柱を 1 本足し、各足場の下に「腕木」(柱から張り出した梁) と斜めの方杖を付ける
        const float Mast = 235.5f;
        Strut(Mast, 9.5f, 53f);
        foreach (var (x0, x1, top) in new[] { (242, 248, 33), (235, 240, 36), (228, 233, 39), (222, 227, 42), (229, 234, 45), (236, 241, 48), (243, 249, 51) })
            PlatformArm(x0, x1, top, new[] { 222.5f, Mast, 248.2f });
        // 塔のてっぺんの看板 (屋台の屋根飾り)
        Prop("props/shop_stall.png", 235.5f, 53f, 1.6f, -7, false, new Color(0.8f, 0.72f, 0.88f));
        Strut(226.5f, 0f, 11.6f); Strut(233.5f, 9f, 14.6f);
        // 上の小さな屋台 (飾り) と旗
        Prop("props/shop_stall.png", 225f, 42f, 0.8f, -6, false, new Color(0.85f, 0.8f, 0.9f));
        BuntingBetween(229f, 241f, 50f);
        LampPost(229.5f, 45f, 3f); LampPost(240.5f, 48f, 3f);
        Enemy(234f, 43f, flying: true, name: "FlyerRoof");
        var bomb = ConsumablePickupAt(246.5f, 51f, "Assets/Items/BobbinBomb.asset", 3, "A1_Pickup_U2a_1");
        bomb.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.6f);

        // ================= U2r (x156..177, y-12..-3): 床下の狭い通路。床 y-12
        // 戻り道: 床 → 棚 → 格子の下の棚 → 格子
        OneWay(166, 171, -9);
        OneWay(160, 165, -6);
        OneWay(159, 165, -3);
        // 奥の紫の板壁 (突進でのみ壊れる)
        SkillBlock(177, -12, 180, -3, PlayerSkill.ChargeRush, "A1_Rush_U2r");
        HangLamp(170f, -3f, 0.55f);
        // コインの箱 (2026-10-03 PendingChanges 16: 決めた箱だけにコイン。取ったら二度と出ない。1 回の攻撃で壊れる箱は 1 つ = 17)
        // U2r の西の行き止まり (床 y-12) に積んだ木箱 2 + 横に 1。下の箱にコイン
        CrateStack(156.5f, -12f, 2, "A1_Coin_U2r_1", 0);
        Crate(157.5f, -12f);

        // ================= U2r2 (x180..195, y-12..-3): 裏方の倉庫。糸の大束×80
        Prop("props/shop_stall.png", 189f, -12f, 1.1f, -6, false, new Color(0.5f, 0.45f, 0.58f));  // しまわれた屋台
        Solid(191, -12, 194, -11);
        ThreadPickupAt(192.5f, -10.2f, 80, "A1_Pickup_U2r2_1");
        HangLamp(185f, -3f, 0.55f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(228f, 0.6f, name: "WalkWalker");           // 遊歩道の東寄りの床 (売店の前)
        Enemy(205f, 19f, true, name: "FlyerHigh");        // 遊歩道の上空 (足場の無い高さ)

        return log + Finish();
    }
}
