using UnityEngine;
using static NeverNight.StageBuild.Area1StageBuilder;

// A1_T3 「縦の衣装ラック」(T3) + 「針だまりの小部屋」(T3b) + 「楽屋裏の勝手口」(T3c)
// 先に別の呼び出しで OpenOrCreate("A1_T3") を実行しておくこと。
public static class BuildT3
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
        var log = BuildBase("A1_T3");

        // ================= T3 (x93..114, y-99..-39): 縦の衣装ラック
        // 床 y-96 (左 = A1_T2 からの通路 y-96..-87、右下 = T3b への通路 y-96..-87)。
        // 右上 (y-54..-45) が A1_T4 への出口。天井 (x96..102) が上 (A1_U4 の点検口の縦穴) からの到着口。
        Ground(93, 114, -96);

        // 左右交互の足場 (段差 3、横の隙間 ≤2)。すり抜けの衣装ラックと壁から突き出た岩を混ぜる
        OneWay(98, 103, -93);              // 1
        OneWay(105, 110, -90);             // 2
        Solid(110, -88, 114, -87);         // 3 右壁の出っ張り (T3b への通路の真上)
        OneWay(103, 108, -84);             // 4
        Solid(93, -82, 101, -81);          // 5 左壁の出っ張り
        OneWay(100, 105, -78);             // 6
        Solid(106, -77, 114, -75);         // 7 中段の踊り場 (敵がいる)
        OneWay(99, 104, -72);              // 8
        Solid(93, -70, 98, -69);           // 9 左壁の出っ張り
        OneWay(99, 104, -66);              // 10
        OneWay(106, 111, -63);             // 11
        OneWay(100, 105, -60);             // 12
        OneWay(106, 111, -57);             // 13
        // 上段: 出口の高さ (y-54) の渡り板 + 出口手前の岩
        OneWay(96, 111, -54);
        Solid(111, -56, 114, -54);
        // 天井の点検口を見上げる棚 (上から来た時の着地点)
        OneWay(103, 108, -51);
        OneWay(97, 102, -48);
        OneWay(95, 103, -45);

        // 天井の点検口 (橙 = 落下攻撃で上から壊す)。A1_U4 側 (縦穴 Uh) の床と同じフラグ
        SkillBlock(96, -39, 102, -36, PlayerSkill.GroundSlam, "A1_Slam_Uh_1");
        MoveSpawn("A1_U4", 99.5f, -43.5f);

        // 敵×1 (中段の踊り場の上)
        Enemy(110.5f, -74f, name: "RackEnemy");

        // ================= T3b (x117..138, y-99..-81): 針だまりの小部屋
        Ground(117, 138, -96);
        // トゲの溝 (深さ 1、トゲの先が床と同じ高さ)
        Clear(121, -97, 124, -96); Spikes(121, 124, -97);       // 3 マス: 小ジャンプ
        Clear(127, -97, 131, -96); Spikes(127, 131, -97);       // 4 マス: 助走ジャンプ
        Solid(132, -96, 134, -94);                               // 段 (高さ 2)
        Clear(134, -97, 137, -96); Spikes(134, 137, -97);       // 段から跳ぶ 3 マス
        // 段から登る棚の上に道具「まち針」(落ちるとトゲ)
        OneWay(127, 131, -91);
        var needle = ConsumablePickupAt(129f, -91f, "Assets/Items/MachiNeedle.asset", 3, "A1_Pickup_T3b_1");
        needle.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.6f);

        // ================= T3c (x141..159, y-99..-84): 楽屋裏の勝手口 (行き止まり)
        Ground(141, 159, -96);
        // 床の跳ね上げ戸 (x148..152)。地下 (D2w) のレバーで梯子が下りると、下から上がってこられる (一方通行)
        Clear(148, -99, 152, -96);
        var hatch = NewObject("Hatch", new Vector2(150f, -96.25f), LayerGround, "Terrain Objects");
        Box(hatch, new Vector2(4f, 0.5f), false);
        var plank = (Map("Platforms") != null ? Tile("stagetile_floor_08_m") as UnityEngine.Tilemaps.Tile : null)?.sprite;
        var lidClosed = new GameObject("LidClosed");
        lidClosed.transform.SetParent(hatch.transform, false);
        lidClosed.transform.localPosition = new Vector3(0, -0.15f, 0);
        AddSprite(lidClosed, plank, OrderGimmick, new Vector2(4f, 0.8f), new Color(0.75f, 0.6f, 0.55f));
        var lidOpen = new GameObject("LidOpen");
        lidOpen.transform.SetParent(hatch.transform, false);
        lidOpen.transform.localPosition = new Vector3(2.3f, 1.9f, 0);
        lidOpen.transform.localRotation = Quaternion.Euler(0, 0, 80f);
        AddSprite(lidOpen, plank, OrderProp, new Vector2(4f, 0.8f), new Color(0.75f, 0.6f, 0.55f));
        var ladderTop = new GameObject("LadderTop");
        ladderTop.transform.SetParent(hatch.transform, false);
        ladderTop.transform.localPosition = new Vector3(0, 1.0f, 0);
        AddSprite(ladderTop, Art("tiles/tile_ladder.png"), OrderProp, new Vector2(1f, 2f));
        // 戸の下の縦穴 (見えるだけ): 梯子が下がっている
        var shaftLadder = NewObject("ShaftLadder", new Vector2(150.5f, -102f), LayerDefault, "Props");
        AddSprite(shaftLadder, Art("tiles/tile_ladder.png"), OrderBackfill + 1, new Vector2(1f, 12f), new Color(0.6f, 0.6f, 0.7f));
        var act = NewObject("HatchFlag", new Vector2(150f, -94f), LayerDefault, "Gimmicks");
        AddComp<FlagActivator>(act, ("_flags", new[] { "A1_LadderBackstage" }),
            ("_showWhenSet", new Object[] { lidOpen, ladderTop }), ("_hideWhenSet", new Object[] { lidClosed }));
        MoveSpawn("A1_D2", 155.5f, -94.5f);

        // 開かない勝手口 (右の壁)
        Prop("props/fence_gate.png", 157.6f, -96f, 1f, OrderProp, false, new Color(0.8f, 0.75f, 0.85f));

        // ================= ヒント
        // 案内文はチュートリアルの操作説明 (キーの説明) だけ残す。呼びかけ・攻略助言・場所の説明は削除 (2026-10-03 ユーザー決定, Docs/UI/UIAssignment.md「案内文」)
        Hint(124, -96, 127, -92, "ダメージを受けたら [S] で回復! 回復ゲージは敵に攻撃を当てるとたまる",
            "ダメージを受けたら ○ で回復! 回復ゲージは敵に攻撃を当てるとたまる", "HintHeal");
        Hint(131, -94, 134, -89, "まち針は道具。[I] を押しながら方向キーで使う",
            "まち針は道具。L1 を押しながら十字キーで使う", "HintItem");

        // ================= 飾り (奥の歯車・吊り灯)
        BgGear(93.5f, -84f, 2.2f, 10f); BgGear(114f, -66f, 1.8f, 25f); BgGear(94f, -58f, 1.5f);
        BgGear(126f, -82f, 1.6f, 15f); BgGear(151f, -86f, 2f, 5f);
        HangLamp(104f, -39f); HangLamp(124.5f, -81f); HangLamp(135.5f, -81f); HangLamp(146f, -84f);

        // 敵を少し増やす (2026-10-03 PendingChanges 8: 部屋の意図に合わせる。位置は enemy_probe.cs の候補から)
        Enemy(108.5f, -95.4f, name: "FloorEnemy");   // 一番下の床 (x104..113)。チュートリアルなので控えめに 1 体だけ足す

        return log + Finish();
    }
}
