using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W3 「整備用リフト」(W3, 整備用の縦坑 x36..54 y90..168)
// 左下 = 支柱の足場 A1_W2 (y90..99) / 右上 = 風の外周 A1_W4 (y156..165)
// 3 本のリフト (MovingPlatform 往復) を乗り継ぐ: 右の坑 A (y90→111) → 中の踊り場 L1 → 左の坑 B (y111→132) → 踊り場 L2
// → 右の坑 C (y132→153) → 坑の上を渡る鉄骨 (y156, すり抜け) → 出口。リフトの上で浮遊する敵 2 体が寄ってくる。
// 落ちても下の踊り場か、動いているリフト、坑の底の床が受け止める (乗り場まで落ちることはない)。
// 先に別の呼び出しで OpenOrCreate("A1_W3") を実行しておくこと。
public static class BuildW3
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Lattice = new Color(0.3f, 0.25f, 0.42f);
    static readonly Color RailCol = new Color(0.7f, 0.6f, 0.78f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // リフトの坑: 両側のレール + 上の巻き上げ機 (歯車) + ケーブル
    static void LiftShaft(float cx, float y0, float y1, bool gearTop = true)
    {
        Truss(cx - 1.75f, y0, y1 + 2f, 0.8f, -9, RailCol);
        Truss(cx + 1.75f, y0, y1 + 2f, 0.8f, -9, RailCol);
        var cable = NewObject("Cable", new Vector2(cx, (y0 + y1) * 0.5f + 1f), LayerDefault, "Props");
        cable.transform.rotation = Quaternion.Euler(0, 0, 90f);
        AddSprite(cable, Art("ferris/spoke_beam.png"), -9, new Vector2(y1 - y0 + 2f, 0.18f), new Color(0.55f, 0.55f, 0.62f));
        if (gearTop) Prop("platforms/gear_platform.png", cx, y1 + 2.4f, 0.5f, -8, false, new Color(0.8f, 0.72f, 0.85f));
    }

    static GameObject ShaftLift(float cx, int bottom, int rise, float delay)
    {
        // Lift の pos = 下端の中心 (上面 = pos.y + 0.5)
        var lift = Lift(new Vector2(cx, bottom - 0.5f), new[] { Vector2.zero, new Vector2(0, rise) }, 3f, 1.2f);
        Set(lift.GetComponent<MovingPlatform>(), "_startDelay", delay);
        // 絵の天板 (画像の上から 1/3) を当たりの上面 (pos.y+0.5) に合わせる: 絵を子に移して 0.33 上げる
        var sr = lift.GetComponent<SpriteRenderer>();
        var sprite = sr.sprite;
        Object.DestroyImmediate(sr);
        var art = new GameObject("Art");
        art.transform.SetParent(lift.transform, false);
        art.transform.localPosition = new Vector3(0, 0.33f, 0);
        AddSprite(art, sprite, OrderGimmick);
        return lift;
    }

    public static string Run()
    {
        var log = BuildBase("A1_W3");
        PlaceFerrisWheel(true, false, new Color(0.5f, 0.46f, 0.6f));   // 坑の上端 (y≈162) を外周の下端が横切る
        // polish A: 中景の観覧車 (W1〜W4 共通の輪。ゆっくり回り、ゴンドラが通り過ぎる。当たり無し)
        FerrisMidground(1.2f, new Color(0.8f, 0.76f, 0.9f));
        SkyBackdrop();
        PlateWallsAll();
        RockDecor(0.6f);

        // ================= 縦坑の骨組み (背景): 両側の柱 + 12 ごとの梁
        Truss(36.6f, 90f, 168f, 1.6f, -45, Lattice);
        Truss(53.4f, 90f, 168f, 1.6f, -45, Lattice);
        for (var y = 102f; y < 168f; y += 12f)   // 足場の高さ (111/132/156) と重ねない
            Brace(36f, 54f, y, 0.7f, -44, Lattice);
        // polish B: 坑の上端の作業用の電球 + 坑の外を通り過ぎるゴンドラ (中景, 外周に近づく前)
        BulbString(36.5f, 53.5f, 167.6f, 0.6f, -43, new Color(0.9f, 0.85f, 0.85f));
        FarGondola(new Vector2(31f, 118f), new Vector2(0.86f, 0.84f), new Vector2(45f, 118f), 0.6f, new Color(0.5f, 0.46f, 0.64f, 0.85f));

        // ================= 坑の底 (床 y90) → リフト A (右の坑 x51..54)
        LiftShaft(52.5f, 90f, 111f);
        ShaftLift(52.5f, 90, 21, 0f);

        // 踊り場 L1 (y111, 中)
        Girder(40, 51, 111, Steel);
        // リフト B (左の坑 x36..39)
        LiftShaft(37.5f, 111f, 132f);
        ShaftLift(37.5f, 111, 21, 4f);

        // 踊り場 L2 (y132, 中)
        Girder(40, 51, 132, Steel);
        // リフト C (右の坑)
        LiftShaft(52.5f, 132f, 153f, false);   // 上は渡りの鉄骨 (巻き上げ機は出口の上の外周の陰)
        ShaftLift(52.5f, 132, 21, 8f);

        // 最上段: 坑の上を渡る鉄骨 (すり抜け: リフト C の上から跳び上がって乗る) と、出口前の踊り場
        Girder(51, 54, 156, Steel, "Bridge");
        Girder(43, 51, 156, Steel);

        // 坑の底の待避所 (左の坑 B の下に入り込まないよう、坑の底の目印)
        Prop("rock/decal_pipe_v.png", 37.5f, 91f, 1f, -8);

        // ================= 浮遊する敵 (リフトの上で戦う)
        Enemy(45.5f, 122f, flying: true, name: "ShaftFlyer1");
        Enemy(45.5f, 146f, flying: true, name: "ShaftFlyer2");

        // ================= 飾り
        HangLamp(41f, 168f); HangLamp(49f, 168f);
        Prop("rock/decal_lamp_wall.png", 45.5f, 114.5f, 1f, OrderWallDecor);
        Prop("rock/decal_lamp_wall.png", 45.5f, 135.5f, 1f, OrderWallDecor);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(45f, 105f, true, name: "ShaftFlyer0");     // リフト A の上の空中 (下の区間にも 1 体)

        return log + Finish();
    }
}
