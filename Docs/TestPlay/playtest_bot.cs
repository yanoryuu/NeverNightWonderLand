using System.Globalization;
using System.Linq;
using UnityEngine;

// テストプレイボットの動作確認 (プレイ中に run_script で呼ぶ。Docs/TestPlay/README.md「ボット」)。
// 例: unity command run_script --caller plugin --skill unity-cli --file Docs/TestPlay/playtest_bot.cs --entry PlayBot.Start --args '["0","3","1","1"]'
public static class PlayBot
{
    // from, to = ルート (BotRoutes.Area1Critical) の番号, speed, jev (1/0), invincible (1/0)
    public static string Start(string from, string to, string speed, string jev, string invincible = "1")
    {
        Application.runInBackground = true;
        var s = TestPlayBot.Settings;
        s.Speed = float.Parse(speed, CultureInfo.InvariantCulture);
        s.UseJev = jev == "1";
        s.Invincible = invincible == "1";
        var bot = TestPlayBot.Ensure();
        var a = int.Parse(from);
        var b = int.Parse(to);
        bot.StartRoute(BotRoutes.Area1Critical, a, b, $"Area1_{BotRoutes.Area1Critical[a].Scene}-{BotRoutes.Area1Critical[b].Scene}_x{speed}");
        return Status();
    }

    public static string AllExits(string scene, string speed, string jev)
    {
        Application.runInBackground = true;
        TestPlayBot.Settings.Speed = float.Parse(speed, CultureInfo.InvariantCulture);
        TestPlayBot.Settings.UseJev = jev == "1";
        TestPlayBot.Ensure().StartAllExits(new[] { scene }, "AllExits_" + scene);
        return Status();
    }

    public static string Set(string key, string value)
    {
        var s = TestPlayBot.Settings;
        var f = typeof(BotSettings).GetField(key);
        if (f == null) return "no field " + key;
        object v = f.FieldType == typeof(bool) ? value == "1" || value == "true" :
            f.FieldType == typeof(int) ? int.Parse(value) : (object)float.Parse(value, CultureInfo.InvariantCulture);
        f.SetValue(s, v);
        if (key == "Speed") TestPlaySpeed.SetScale((float)v);
        return $"{key}={v}";
    }

    public static string Status()
    {
        var bot = TestPlayBot.Instance;
        if (bot == null) return "no bot";
        var d = bot.LastDecision;
        var w = bot.World;
        var p = TestPlayActions.Player;
        var seg = w != null && p != null ? w.SegmentUnder(p) : null;
        return $"running={bot.Running} status='{bot.Status}' step={bot.StepIndex}/{bot.StepCount} '{bot.CurrentStepLabel}'\n" +
               $"goal={bot.GoalText} exec='{bot.ExecutorStep}'\n" +
               $"last={(d == null ? "-" : $"{d.Choice} [{d.Source} {d.Confidence:0.00}] {d.ProbText()} -> {d.Outcome}")}\n" +
               $"player={TestPlayActions.GetPlayerState()} seg={(seg != null ? seg.ToString() : "-")} cost={(w != null && seg != null ? w.Cost(seg).ToString("0.0") : "-")}\n" +
               $"world: segs={w?.Segments.Count} edges={w?.Edges.Count} scan={w?.ScanMilliseconds:0}ms graph={w?.GraphMilliseconds:0}ms\n" +
               $"jev: calls={bot.Jev.Calls} fail={bot.Jev.Failures} retry={bot.Jev.Retries} tokens={bot.Jev.InputTokens}+{bot.Jev.OutputTokens} key={bot.Jev.KeySource} err={bot.Jev.LastError}\n" +
               $"timeScale={Time.timeScale} speed={TestPlaySpeed.Scale} frozen={TestPlaySpeed.Frozen} report={bot.LastReportPath}";
    }

    public static string Summary() => TestPlayBot.Instance != null ? TestPlayBot.Instance.Summary() + "\n" + TestPlayBot.Instance.LastReportPath : "no bot";

    public static string Stop()
    {
        var bot = TestPlayBot.Instance;
        if (bot == null) return "no bot";
        bot.Stop("manual");
        return Summary();
    }

    // 今のシーンのセグメントと、今いる床からの辺を見る (調査用)
    public static string World()
    {
        var bot = TestPlayBot.Instance;
        var w = bot?.World;
        var p = TestPlayActions.Player;
        if (w == null || p == null) return "no world";
        var seg = w.SegmentUnder(p);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"scene={w.SceneName} bounds={w.Bounds} segs={w.Segments.Count} goal={w.Goal}");
        foreach (var s in w.Segments.OrderBy(s => w.Cost(s)).Take(40))
            sb.AppendLine($"  {s} cost={w.Cost(s):0.0}");
        if (seg != null)
        {
            sb.AppendLine($"cur={seg}");
            foreach (var e in w.Edges.Where(e => e.From == seg.Id))
                sb.AppendLine($"  -> {(e.To < 0 ? "GOAL" : w.Segments[e.To].ToString())} {e.Move} cost={e.Cost:0.0}");
        }

        foreach (var e in w.Entities.Take(30))
            sb.AppendLine($"  ent {e.Id} {e.Kind} {e.Label} {e.Rect} active={e.Active}");
        return sb.ToString();
    }

    public static string Shot(string name)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "TestPlay", "Reports", name + ".png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        return path;
    }
}

public static class PlayBotProbe
{
    public static string Warp(string scene, string entrance) { TestPlayActions.Warp(scene, entrance); return "warping"; }

    // 今のシーンを走査して、指定の行き先の出口をゴールにしたグラフを書き出す
    public static string Graph(string next)
    {
        var p = TestPlayActions.Player;
        var w = new BotWorld();
        w.Scan(p);
        var exit = w.Entities.FirstOrDefault(e => (e.Kind == "exit" || e.Kind == "transport") && e.Label == next);
        w.BuildGraph(exit != null ? exit.Rect : (Rect?)null);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"scene={w.SceneName} segs={w.Segments.Count} edges={w.Edges.Count} scan={w.ScanMilliseconds:0}ms goal={(exit != null ? exit.Rect.ToString() : "none")}");
        foreach (var s in w.Segments.OrderByDescending(s => s.YCenter))
        {
            sb.Append($"{s} cost={w.Cost(s):0.0} ->");
            foreach (var e in w.Edges.Where(e => e.From == s.Id))
                sb.Append($" {(e.To < 0 ? "GOAL" : w.Segments[e.To].Name)}:{e.Move}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public static class PlayBotLog
{
    public static string Recent(string n)
    {
        var bot = TestPlayBot.Instance;
        if (bot?.Report == null) return "no report";
        var list = bot.Report.Decisions;
        var k = int.Parse(n);
        return string.Join("\n", list.Skip(System.Math.Max(0, list.Count - k)).Select(d =>
            $"{d.GameTime:0.0} {d.Segment} ({d.Position.x:0.0},{d.Position.y:0.0}) {d.Choice} [{d.Source} {d.Confidence:0.00}] -> {d.Outcome}\n      opts: {d.OptionsText}"));
    }
}

public static class PlayBotPlan
{
    // 今のシーンで、床 from (名前 P12 など) の上の x から to への跳躍を計画して経路を出す
    public static string Plan(string from, string to, string x)
    {
        var p = TestPlayActions.Player;
        var w = new BotWorld();
        w.Scan(p);
        var ph = new BotPhysics(w);
        ph.ReadPlayer(p);
        var a = w.Segments.First(s => s.Name == from);
        var b = w.Segments.First(s => s.Name == to);
        var px = float.Parse(x, System.Globalization.CultureInfo.InvariantCulture);
        var plan = ph.Plan(a, new Vector2(px, a.YAt(px) + w.PivotToFeet), new BotTarget { Segment = b });
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{a} -> {b}: {plan.Describe()} sims={plan.Simulations}/{ph.LastSimulations} feetOffset={ph.LedgeFeetOffset:0.00} pivot={w.PivotToFeet:0.00} capsule={ph.CapsuleSize} off={ph.CapsuleOffset}");
        sb.AppendLine(string.Join(" ", plan.Path.Where((v, i) => i % 3 == 0).Select(v => $"({v.x:0.0},{v.y:0.0})")));
        return sb.ToString();
    }
}

public static class PlayBotEdges
{
    // 今のシーンの全ての辺 (歩き以外) を跳躍計画にかけ、計画が見つからない辺を列挙する
    public static string Check()
    {
        var p = TestPlayActions.Player;
        var w = new BotWorld();
        w.Scan(p);
        var ph = new BotPhysics(w);
        ph.ReadPlayer(p);
        w.BuildGraph(null);
        var sb = new System.Text.StringBuilder();
        int ok = 0, ng = 0;
        foreach (var e in w.Edges)
        {
            if (e.To < 0 || e.Move == BotMove.Walk) continue;
            var a = w.Segments[e.From];
            var b = w.Segments[e.To];
            if (a.Moving || b.Moving) continue;
            var plan = ph.Plan(a, new Vector2(a.CenterX, a.YCenter + w.PivotToFeet), new BotTarget { Segment = b });
            if (plan.Valid) ok++;
            else { ng++; sb.AppendLine($"NG {a} -> {b} ({e.Move})"); }
        }
        return $"ok={ok} ng={ng}\n" + sb;
    }
}

public static class PlayBotTest
{
    public static string Jump(string from, string to) => TestPlayBot.Ensure().TestJump(from, to);
    public static string Result() => TestPlayBot.Instance != null ? TestPlayBot.Instance.TestResult : "no bot";
}
