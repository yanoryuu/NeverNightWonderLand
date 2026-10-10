// プレイ中の状態確認/テレポート/闘技場の確認 (A1_U1..U4 の遷移テストに使用)。
// 例: --entry PlayU.Status / --entry PlayU.Tp --args '[262,-24]' / --entry PlayU.Arena / --entry PlayU.KillAll
// 遷移テストの座標: H→U1 (261,-22.5) / U1→U2 (247,4.5) / U2→U3 (146,7.5) / U3→U4 (103,7.5) / U4→Uh 棚 (99,-16) / Uh→T3 (99,-34)
//                  U4→U3 (104.5,7.5) / U3→U2 (149,7.5) / U2→U1 (251,4.5) / U1→H (261,-23)
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class PlayU
{
    static string State()
    {
        var sb = new System.Text.StringBuilder();
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p) sb.Append($"pos={p.transform.position} grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")}");
        else sb.Append("no player");
        sb.Append($" stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} t={Time.time:F1}");
        return sb.ToString();
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        var p = Object.FindAnyObjectByType<PlayerController>();
        p.Teleport(new Vector2(x, y));
        return "tp\n" + State();
    }

    // 闘技場の状態: 波の番号・生存敵数・ゲート・フラグ・梯子
    public static string Arena()
    {
        var a = Object.FindAnyObjectByType<WaveArena>();
        if (a == null) return "no arena\n" + State();
        var t = typeof(WaveArena);
        var idx = (int)t.GetField("_waveIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        var started = (bool)t.GetField("_started", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        var gates = (GameObject[])t.GetField("_gates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        var waves = (System.Array)t.GetField("_waves", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        var ladder = GameObject.Find("Ladder");
        return $"started={started} wave={idx}/{waves.Length} enemies={enemies.Length} gates={string.Join(",", gates.Select(g => g.activeSelf))} " +
               $"flag={GameProgress.Has("A1_ArenaCups")} ladder={(ladder != null ? ladder.activeInHierarchy.ToString() : "inactive/none")}\n" + State();
    }

    // 生きている敵を全部消す (闘技場は null を全滅として数える)
    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    // 任意のステージへ遷移 (例 ["A1_U4","FromA1_U3"])
    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }

    // 名前で探した IInteractable を、プレイヤーを近くに寄せてから Interact する (拾う/話す/休むの確認)
    public static string Use(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var p = Object.FindAnyObjectByType<PlayerController>();
        var it = go.GetComponent<IInteractable>();
        if (it == null) return "no IInteractable on " + objName;
        it.Interact(p.gameObject);
        return $"used {objName} exists={GameObject.Find(objName) != null}";
    }

    public static string Active(string objName)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == objName && g.scene.IsValid());
        return string.Join(" ", all.Select(g => $"{g.name}:{g.activeInHierarchy}"));
    }

    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
}
