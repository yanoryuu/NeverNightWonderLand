#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>ルートの 1 区間 (1 シーンの中で何をして、どこへ抜けるか)。</summary>
public class BotRouteStep
{
    public string Scene;
    public string Entrance;          // ここから始める時の入口 (null = デフォルト / 前の区間の遷移先)
    public string Next;              // 抜ける先のシーン (null = シーン内のゴールだけ)
    public bool ViaTransport;        // 移動装置 (ジップライン・エレベーター) を使う
    public string[] Objectives = new string[0]; // "boss:<flag>" / "speaker:<flag>" / "pickup:<id>"
    public Rect? ReachRect;          // シーン内のゴール (最後の FP2 など)
    public Vector2? ExitHint;        // 同じ行き先の出口が複数ある時に選ぶ位置 (全出口の一括確認用)
    public string Label;

    public override string ToString() => Label ?? $"{Scene}→{Next}";
}

/// <summary>ボットのルート定義 (エリア1 のクリティカルパス) と、途中から始める時のフラグ・能力。</summary>
public static class BotRoutes
{
    private static BotRouteStep S(string scene, string next, string label, params string[] objectives) =>
        new() { Scene = scene, Next = next, Label = label, Objectives = objectives };

    /// <summary>
    /// エリア1 のクリティカルパス:
    /// T1→T2→T3→T4→B1 (番人+スピーカー①)→S1→H→U1→U2→U3→U4→W1→W2→W3→W4→W5→W6(W7)→W8→MB1 (中ボス上+S2 スピーカー②)→ジップライン→U1
    /// →H→D1→D2→D5→D3→D4→MB2 (中ボス下+S3 スピーカー③)→エレベーター→C1→大扉→AB (エリアボス+赤ハサミ)→ST→FP→FP2。
    /// </summary>
    public static readonly BotRouteStep[] Area1Critical =
    {
        S("A1_T1", "A1_T2", "T1→T2"),
        S("A1_T2", "A1_T3", "T2→T3"),
        S("A1_T3", "A1_T4", "T3→T4"),
        S("A1_T4", "A1_B1", "T4→B1"),
        S("A1_B1", "A1_H", "B1 番人+スピーカー①→S1→H", "boss:A1_Boss1", "speaker:A1_Speaker1"),
        S("A1_H", "A1_U1", "H→U1"),
        S("A1_U1", "A1_U2", "U1→U2"),
        S("A1_U2", "A1_U3", "U2→U3"),
        S("A1_U3", "A1_U4", "U3→U4"),
        S("A1_U4", "A1_W1", "U4→W1"),
        S("A1_W1", "A1_W2", "W1→W2"),
        S("A1_W2", "A1_W3", "W2→W3"),
        S("A1_W3", "A1_W4", "W3→W4"),
        S("A1_W4", "A1_W5", "W4→W5"),
        S("A1_W5", "A1_W6", "W5→W6"),
        S("A1_W6", "A1_W8", "W6(W7)→W8"),
        S("A1_W8", "A1_MB1", "W8→MB1"),
        new() { Scene = "A1_MB1", Next = "A1_U1", ViaTransport = true, Label = "MB1 中ボス+スピーカー②→ジップライン→U1",
            Objectives = new[] { "boss:A1_MidBossUpper", "speaker:A1_Speaker2" } },
        S("A1_U1", "A1_H", "U1→H"),
        S("A1_H", "A1_D1", "H→D1"),
        S("A1_D1", "A1_D2", "D1→D2"),
        S("A1_D2", "A1_D5", "D2→D5"),
        S("A1_D5", "A1_D3", "D5→D3"),
        S("A1_D3", "A1_D4", "D3→D4"),
        S("A1_D4", "A1_MB2", "D4→MB2"),
        new() { Scene = "A1_MB2", Next = "A1_C1", ViaTransport = true, Label = "MB2 中ボス+スピーカー③→エレベーター→C1",
            Objectives = new[] { "boss:A1_MidBossLower", "speaker:A1_Speaker3" } },
        S("A1_C1", "A1_AB", "C1 大扉→AB"),
        S("A1_AB", "A1_FP", "AB エリアボス+赤ハサミ→ST→FP", "boss:A1_AreaBoss", "pickup:A1_Pickup_AB_1"),
        new() { Scene = "A1_FP", Next = null, Label = "FP→FP2 (正門)", ReachRect = new Rect(424f, 0f, 6f, 12f) },
    };

    /// <summary>
    /// ルートの i 番目から始める時に立っているべきフラグ (前の区間の目的 + それで立つ派生フラグ)。
    /// </summary>
    public static List<string> FlagsBefore(BotRouteStep[] route, int index)
    {
        var flags = new List<string>();
        for (var i = 0; i < index && i < route.Length; i++)
        {
            foreach (var o in route[i].Objectives)
            {
                var parts = o.Split(':');
                if (parts.Length == 2) flags.Add(parts[1]);
            }
        }

        if (flags.Contains("A1_Speaker2")) flags.Add("A1_ZiplineReady");
        if (flags.Contains("A1_Speaker3")) flags.Add("A1_ElevatorDeep");
        return flags.Distinct().ToList();
    }

    /// <summary>ルートの i 番目から始める時の能力 (エリアボスの後は赤ハサミ)。</summary>
    public static List<ScissorUpgrade> UpgradesBefore(BotRouteStep[] route, int index)
    {
        var list = new List<ScissorUpgrade>();
        if (FlagsBefore(route, index).Contains("A1_Pickup_AB_1"))
            list.Add(ScissorUpgrade.Red);
        return list;
    }

    /// <summary>
    /// 1 シーンの「全入口 × 全出口」の組を作る (到達性の一括確認)。出口はシーンを読み込んだ後に分かるので、
    /// ここでは入口ごとの区間だけ作り、出口はボット側でシーンにある遷移ゾーンから展開する。
    /// </summary>
    public static List<BotRouteStep> AllExitsFromAllSpawns(string scene)
    {
        var steps = new List<BotRouteStep>();
        var spawns = TestPlayActions.SpawnIds(scene);
        if (spawns.Length == 0) spawns = new[] { "" };
        foreach (var spawn in spawns)
            steps.Add(new BotRouteStep { Scene = scene, Entrance = spawn, Next = "*", Label = $"{scene} [{(spawn == "" ? "default" : spawn)}] → 全出口" });
        return steps;
    }
}
#endif
