#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>1 回の判断の記録。</summary>
public class BotDecision
{
    public string Scene;
    public Vector2 Position;
    public string Segment;
    public string Goal;
    public string Choice;
    public string Source;              // jev / local / auto / fallback
    public float Confidence = -1f;
    public List<KeyValuePair<string, float>> TopProbabilities = new();
    public string Outcome;
    public float GameTime;
    public float LatencySeconds;
    public int Options;
    public string OptionsText;      // 選択肢とスコア (! = シミュレーションで着地できない)

    public string ProbText() => string.Join(", ", TopProbabilities.Select(p => $"{p.Key} {p.Value:0.00}"));
}

/// <summary>注目すべき出来事 (止まった場所・迷った場所・死亡・落下など)。</summary>
public class BotIncident
{
    public string Kind;        // stuck / hesitation / death / hazard / wrong_exit / timeout / error
    public string Scene;
    public Vector2 Position;
    public string Detail;
    public string Screenshot;  // レポートからの相対パス
    public List<BotDecision> RecentDecisions = new();
}

/// <summary>1 区間の結果。</summary>
public class BotHopResult
{
    public string Label;
    public string Scene;
    public string Goal;
    public string Result = "running"; // ok / failed / skipped
    public string Reason;
    public float GameSeconds;
    public float RealSeconds;
    public int Decisions;
    public int JevCalls;
    public long Tokens;
    public int Deaths;
    public float MinConfidence = 1f;
    public bool WarpedPast;
}

/// <summary>テストプレイボットの結果を Markdown に書く (TestPlay/Reports/)。</summary>
public class BotReport
{
    public string Title;
    public DateTime Started = DateTime.Now;
    public readonly List<BotHopResult> Hops = new();
    public readonly List<BotIncident> Incidents = new();
    public readonly List<BotDecision> Decisions = new();
    public string Settings;

    public int JevCalls, JevFailures, JevRetries;
    public long InputTokens, OutputTokens;
    public float JevLatency;
    public string Model;
    public float GameSeconds, RealSeconds;
    public int LocalDecisions, AutoDecisions;

    public static string ReportsDirectory
    {
        get
        {
#if UNITY_EDITOR
            var root = Path.GetDirectoryName(Application.dataPath) ?? ".";
#else
            var root = Application.persistentDataPath;
#endif
            return Path.Combine(root, "TestPlay", "Reports");
        }
    }

    private string _baseName;

    /// <summary>このレポートのファイル名 (拡張子なし)。</summary>
    public string BaseName
    {
        get
        {
            if (_baseName == null)
            {
                var safe = new string((Title ?? "bot").Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
                if (safe.Length > 40) safe = safe.Substring(0, 40);
                _baseName = Started.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + safe;
            }

            return _baseName;
        }
    }

    public string ImageDirectory => Path.Combine(ReportsDirectory, BaseName + "_img");

    /// <summary>スクリーンショットを撮る (フレームの終わりに保存される)。レポートからの相対パスを返す。</summary>
    public string Screenshot(string tag)
    {
        try
        {
            Directory.CreateDirectory(ImageDirectory);
            var name = $"{Incidents.Count:000}_{tag}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(ImageDirectory, name));
            return BaseName + "_img/" + name;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TestPlayBot] スクリーンショットに失敗: " + e.Message);
            return null;
        }
    }

    public string Write()
    {
        Directory.CreateDirectory(ReportsDirectory);
        var path = Path.Combine(ReportsDirectory, BaseName + ".md");
        File.WriteAllText(path, ToMarkdown(), new UTF8Encoding(false));

        // 判断の全記録 (JSON Lines)
        var jsonl = new StringBuilder();
        foreach (var d in Decisions)
        {
            jsonl.AppendLine(BotJson.Serialize(new Dictionary<string, object>
            {
                ["t"] = d.GameTime, ["scene"] = d.Scene, ["x"] = d.Position.x, ["y"] = d.Position.y, ["seg"] = d.Segment,
                ["goal"] = d.Goal, ["choice"] = d.Choice, ["source"] = d.Source, ["confidence"] = d.Confidence,
                ["probs"] = d.TopProbabilities.ToDictionary(p => p.Key, p => (object)p.Value), ["outcome"] = d.Outcome,
                ["latency"] = d.LatencySeconds, ["options"] = d.Options, ["opts"] = d.OptionsText,
            }));
        }

        File.WriteAllText(Path.Combine(ReportsDirectory, BaseName + "_decisions.jsonl"), jsonl.ToString(), new UTF8Encoding(false));
        return path;
    }

    private static string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# テストプレイボット レポート: {Title}");
        sb.AppendLine();
        sb.AppendLine($"- 開始: {Started:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- 設定: {Settings}");
        sb.AppendLine($"- ゲーム時間 {F(GameSeconds)} 秒 / 実時間 {F(RealSeconds)} 秒");
        var ok = Hops.Count(h => h.Result == "ok");
        sb.AppendLine($"- 区間: {ok}/{Hops.Count} 成功");
        sb.AppendLine($"- Jev: 呼び出し {JevCalls} 回 (失敗 {JevFailures}, 再試行 {JevRetries}), トークン 入力 {InputTokens} / 出力 {OutputTokens} (計 {InputTokens + OutputTokens}), " +
                      $"平均待ち {(JevCalls > 0 ? JevLatency / JevCalls : 0f):0.00} 秒, モデル {Model ?? "-"}");
        sb.AppendLine($"- 判断: 計 {Decisions.Count} (Jev {Decisions.Count(d => d.Source == "jev")}, 選択肢 1 つで自動 {AutoDecisions}, ローカル方針 {LocalDecisions})");
        sb.AppendLine();

        sb.AppendLine("## 区間");
        sb.AppendLine();
        sb.AppendLine("| 区間 | 結果 | 理由 | ゲーム秒 | 実秒 | 判断 | Jev | トークン | 死亡 | 最低確信度 |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var h in Hops)
        {
            sb.AppendLine($"| {h.Label} | {(h.Result == "ok" ? "✅" : h.Result == "failed" ? "❌" : h.Result)}{(h.WarpedPast ? " (ワープで先へ)" : "")} | {Esc(h.Reason)} | {F(h.GameSeconds)} | {F(h.RealSeconds)} | " +
                          $"{h.Decisions} | {h.JevCalls} | {h.Tokens} | {h.Deaths} | {(h.MinConfidence < 1f ? h.MinConfidence.ToString("0.00", CultureInfo.InvariantCulture) : "-")} |");
        }

        sb.AppendLine();
        WriteIncidents(sb, "止まった場所 (stuck / timeout / 誤った出口)", new[] { "stuck", "timeout", "wrong_exit", "error" });
        WriteIncidents(sb, "迷った場所 (Jev の確信度が低い判断)", new[] { "hesitation" });
        WriteIncidents(sb, "死亡・落下", new[] { "death", "hazard" });
        return sb.ToString();
    }

    private void WriteIncidents(StringBuilder sb, string title, string[] kinds)
    {
        var list = Incidents.Where(i => kinds.Contains(i.Kind)).ToList();
        sb.AppendLine($"## {title} ({list.Count})");
        sb.AppendLine();
        if (list.Count == 0)
        {
            sb.AppendLine("なし");
            sb.AppendLine();
            return;
        }

        foreach (var i in list)
        {
            sb.AppendLine($"### {i.Kind}: {i.Scene} ({F(i.Position.x)}, {F(i.Position.y)})");
            sb.AppendLine();
            sb.AppendLine(i.Detail);
            sb.AppendLine();
            if (i.RecentDecisions.Count > 0)
            {
                sb.AppendLine("| 時刻 | 床 | 選択 | 出所 | 確信度 | 確率 (上位) | 結果 |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (var d in i.RecentDecisions)
                    sb.AppendLine($"| {F(d.GameTime)} | {d.Segment} | {Esc(d.Choice)} | {d.Source} | {(d.Confidence >= 0 ? d.Confidence.ToString("0.00", CultureInfo.InvariantCulture) : "-")} | {Esc(d.ProbText())} | {Esc(d.Outcome)} |");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(i.Screenshot))
            {
                sb.AppendLine($"![{i.Kind}]({i.Screenshot})");
                sb.AppendLine();
            }
        }
    }

    private static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|").Replace("\n", " ");
}
#endif
