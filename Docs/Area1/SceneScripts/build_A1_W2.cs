using UnityEngine;
using Foe = NeverNight.StageBuild.GroundFoe;   // 種類 (GroundFoe() と同名なので別名で)
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_W2 「支柱の足場」(W2, 観覧車の支柱の中 x12..33 y33..102) + 「支柱の中の隠し部屋」(W2s x36..51 y57..72)
// 下 = 乗り場 A1_W1 (床の穴 x15..24。落ちると W1 のいちばん上の足場に戻る) / 右上 = 整備用リフト A1_W3 (y90..99)
// 基本の縦登り: 広めの鉄骨の足場を左右交互に 3 ずつ登る (浮遊する敵 1)。y63 の右の足場の先の継ぎ目にひび (A1_Hidden_W2s)。
// 先に別の呼び出しで OpenOrCreate("A1_W2") を実行しておくこと。
public static class BuildW2
{
    static readonly Color Steel = new Color(0.92f, 0.84f, 0.97f);
    static readonly Color Lattice = new Color(0.3f, 0.25f, 0.42f);

    static void HangLamp(float x, float ceilingY, float scale = 0.7f)
    {
        var l = Prop("props/boss_door_lamp_on.png", x, ceilingY - scale * 0.45f, scale, OrderProp, false, new Color(1f, 0.82f, 0.58f));
        l.transform.rotation = Quaternion.Euler(0, 0, 180f);
    }

    // 壁から張り出した鉄板の棚 (上面 top, 厚み 1)
    static void Shelf(int x0, int x1, int top) => Solid(x0, top - 1, x1, top);

    public static string Run()
    {
        var log = BuildBase("A1_W2");
        PlaceFerrisWheel(true, false, new Color(0.5f, 0.46f, 0.6f));   // 本物の左の脚 (斜め) が支柱の奥に見える (暗く沈めて足場と見分ける)
        // polish A: 中景の観覧車 (W1〜W4 共通の輪。ゆっくり回り、ゴンドラが通り過ぎる。当たり無し)
        FerrisMidground(1.2f, new Color(0.8f, 0.76f, 0.9f));
        SkyBackdrop();
        // polish B: 鉄骨の隙間の向こうを通り過ぎるゴンドラ (中景)
        FarGondola(new Vector2(37f, 74f), new Vector2(0.86f, 0.84f), new Vector2(22f, 66f), 0.6f, new Color(0.5f, 0.46f, 0.64f, 0.85f));                   // 鉄骨の隙間から夜空
        PlateWallsAll();                 // 壁はすべて鉄板
        RockDecor(0.6f);

        // ================= 支柱の骨組み (背景): 両側の太い柱 + 9 ごとの梁 + X の筋交い
        Truss(14f, 30f, 102f, 2.6f, -45, Lattice);
        Truss(31f, 30f, 102f, 2.6f, -45, Lattice);
        for (var y = 40f; y < 102f; y += 12f)
        {
            Brace(12f, 33f, y, 0.7f, -44, Lattice);
            // polish B: 梁の下に作業用の電球の連なり (1 本おき)。足場と見分けがつくよう小さく暗め
            if (Mathf.RoundToInt((y - 40f) / 12f) % 2 == 0)
                BulbString(12.5f, 32.5f, y - 0.25f, 0.6f, -43, new Color(0.9f, 0.85f, 0.85f));
            var len = Mathf.Sqrt(17f * 17f + 12f * 12f);
            var ang = Mathf.Atan2(12f, 17f) * Mathf.Rad2Deg;
            Truss(22.5f, y + 6f - len * 0.5f, y + 6f + len * 0.5f, 0.9f, -46, Lattice, 90f - ang);
            Truss(22.5f, y + 6f - len * 0.5f, y + 6f + len * 0.5f, 0.9f, -46, Lattice, ang - 90f);
        }

        // ================= W2: 床 y33 (中央 x15..24 は乗り場への穴)。左右交互の鉄骨の足場
        Girder(16, 23, 36, Steel);     // 穴の上 (落ちたら乗り場の上の足場へ)
        Girder(12, 17, 39, Steel);
        Girder(19, 26, 42, Steel);
        Shelf(27, 33, 45);
        Girder(20, 26, 48, Steel);
        Shelf(12, 19, 51);             // 踊り場 (広め)
        Girder(19, 25, 54, Steel);
        Shelf(27, 33, 57);
        Girder(19, 25, 60, Steel);
        Girder(26, 33, 63, Steel);     // ひびの継ぎ目の前に立つ足場
        Girder(18, 24, 66, Steel);
        Shelf(12, 18, 69);
        Girder(19, 26, 72, Steel);
        Shelf(27, 33, 75);
        Girder(20, 26, 78, Steel);
        Shelf(12, 18, 81);
        Girder(19, 25, 84, Steel);
        Girder(13, 20, 87, Steel);
        Ground(22, 33, 90, 2);         // 整備用リフト (A1_W3) への出口の床

        Enemy(22.5f, 58f, flying: true, name: "LegFlyer");
        MoveSpawn("A1_W1", 27.5f, 34.5f);   // 乗り場から上がってきた時: 穴の縁から少し離す

        // ================= W2s: 支柱の中の隠し部屋 (床 y63)。ハートピン2
        HiddenWall(33, 63, 36, 69, "A1_Hidden_W2s");
        Ground(36, 51, 63, 6);
        var back = NewObject("Backdrop_W2s", new Vector2(42f, 66f), LayerDefault, "Background");
        AddSprite(back, Art("ferris/machine_room_wall.png"), OrderRoomBackdrop, new Vector2(18f, 12f), new Color(1f, 0.9f, 1f));
        Prop("platforms/gear_platform.png", 41.5f, 66.5f, 0.45f, -6, false, new Color(0.7f, 0.62f, 0.75f));   // 支柱の中の歯車 (止まっている)
        Prop("rock/decal_pipe_h.png", 44f, 70.5f, 1.4f, -6);
        Solid(46, 63, 49, 64);   // 台
        var pin = KeyItemAt(47.5f, 64.7f, "ハートピン2", "A1_HeartPin2", "A1_Pickup_W2s_1", "props/heart_pin.png", "拾う");
        pin.GetComponent<BoxCollider2D>().offset = Vector2.zero;
        HangLamp(40f, 72f, 0.6f); HangLamp(47.5f, 72f, 0.6f);
        Truss(37.5f, 63f, 72f, 1f, -45, Lattice);
        // 壁を壊すまで中は鉄板の幕で隠す (部屋 + 内側の縁 1 セル。ひび壁 x33..36 には掛けない)
        HiddenRoomCover("A1_Hidden_W2s", 0.85f, new RectInt(36, 62, 16, 11), new RectInt(35, 69, 1, 4));

        // ================= 飾り: 支柱の中の作業灯
        HangLamp(17f, 102f); HangLamp(28f, 102f);

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        GroundFoe(Foe.Bundled, 41f, 63f, "HiddenBundled");  // 2026-10-10 (HiddenRoomWalker を置換)。隠し部屋 W2s の床 (ご褒美の番。浮遊する敵は 1 体のまま)

        // 奥行き (§16): 視差の層・霧・前景・光の筋 (Background/Depth を作り直す)
        log += Depth("A1_W2") + "\n";
        // 雰囲気 (§15): 作り直しで消えないよう StageAtmosphere をレシピで置く (2026-10-10)
        log += Atmosphere("A1_W2") + "\n";
        return log + Finish();
    }
}
