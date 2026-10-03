// プレイ中の確認 (A1_W1..W4)。PlayU (playtest_U.cs) と同じ流儀。
// 例: --entry PlayW.Status / --entry PlayW.Tp --args '[19.5,32]' / --entry PlayW.Lifts / --entry PlayW.Wind
// 遷移テストの座標: W1→W2 (19.5,32) / W2→W1 (19.5,30) / W2→W3 (35,91.5) / W3→W2 (34.5,91.5) / W3→W4 (56,157.5)
//                  W4→W3 (55,157.5) / W4→W5 (72,176) / W1→U4 (56,7.5) / U4→W1 (55.5,7.5)
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class PlayW
{
    static string State()
    {
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p == null) return "no player";
        var rb = p.GetComponent<Rigidbody2D>();
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) grounded={p.IsGrounded} " +
               $"ground={(p.GroundCollider ? p.GroundCollider.name : "null")} stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} t={Time.time:F1}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        Object.FindAnyObjectByType<PlayerController>().Teleport(new Vector2(x, y));
        return "tp " + State();
    }

    public static string Lifts()
    {
        var ls = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).OrderBy(m => m.transform.position.x).ThenBy(m => m.transform.position.y);
        return string.Join(" | ", ls.Select(m => $"{m.name}@({m.transform.position.x:F1},{m.transform.position.y:F2})")) + "\n" + State();
    }

    public static string Wind()
    {
        var ws = Object.FindObjectsByType<WindArea>(FindObjectsSortMode.None);
        return string.Join(" | ", ws.Select(w => $"{w.name} s={w.Strength:F2}")) + "\n" + State();
    }

    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }

    public static string Use(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var it = go.GetComponent<IInteractable>();
        if (it == null) return "no IInteractable on " + objName;
        it.Interact(Object.FindAnyObjectByType<PlayerController>().gameObject);
        return $"used {objName} exists={GameObject.Find(objName) != null}";
    }

    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
    public static string Has(string f) => $"{f}={GameProgress.Has(f)}";

    // 隠し壁 / スキルブロックの有無
    public static string Exists(string objName) => $"{objName} exists={GameObject.Find(objName) != null}";

    // 敵を全部消す (撮影用)
    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    // 隠し壁を壊す (BreakableBox にダメージ)
    public static string Break(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var d = go.GetComponent<IDamageable>();
        if (d == null) return "no IDamageable";
        var m = d.GetType().GetMethods().FirstOrDefault(mm => mm.Name == "TakeDamage");
        return m == null ? "no TakeDamage" : "methods: " + string.Join(",", m.GetParameters().Select(pp => pp.ParameterType.Name));
    }
}
