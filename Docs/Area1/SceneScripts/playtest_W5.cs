// プレイ中の確認 (A1_W5 / W6 / W8 / MB1)。PlayW (playtest_W.cs) と同じ流儀 + ゴンドラ・ボス・荷物の確認。
// 例: --entry PlayW5.Status / --entry PlayW5.Tp --args '[51,234]' / --entry PlayW5.OnGondola --args '["RingA_Gondola0"]'
//     --entry PlayW5.Rel --args '["RingA_Gondola0"]' (乗っているゴンドラとの相対位置 = 運ばれているか)
// 遷移テストの座標: W5→W4 (72,177) / W5→W6 (51,236) / W6→W5 (51,236) / W6→W8 (51,272) / W8→W6 (51,272)
//                  W8→MB1 (51,344) / MB1→W8 (40,336, 落下) / W6 リフト Use("EmergencyLift") / MB1 Use("StarZipline")
using System.Linq;
using UnityEngine;

public static class PlayW5
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string State()
    {
        var p = P;
        if (p == null) return "no player";
        var rb = p.GetComponent<Rigidbody2D>();
        var hp = p.GetComponent<PlayerHealth>();
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) grounded={p.IsGrounded} " +
               $"ground={(p.GroundCollider ? p.GroundCollider.name : "null")} stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} " +
               $"hp={(hp ? hp.ToString() : "-")} t={Time.time:F1}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        P.Teleport(new Vector2(x, y));
        return "tp " + State();
    }

    // ゴンドラの屋根の上に乗せる
    public static string OnGondola(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) return "not found " + name;
        P.Teleport((Vector2)g.transform.position + new Vector2(0f, 0.44f + 0.65f));
        return "on " + name + " " + State();
    }

    public static string Rel(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) return "not found " + name;
        var d = P.transform.position - g.transform.position;
        return $"rel=({d.x:F3},{d.y:F3}) g=({g.transform.position.x:F2},{g.transform.position.y:F2}) " + State();
    }

    public static string Gondolas()
    {
        var ls = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).Where(m => m.GetComponent<Collider2D>() != null)
            .OrderBy(m => m.name);
        return string.Join(" | ", ls.Select(m => $"{m.name}@({m.transform.position.x:F1},{m.transform.position.y:F1})"));
    }

    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }

    public static string Use(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var it = go.GetComponent<IInteractable>();
        if (it == null) return "no IInteractable on " + objName;
        it.Interact(P.gameObject);
        return $"used {objName} " + State();
    }

    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
    public static string Has(string f) => $"{f}={GameProgress.Has(f)}";
    public static string Active(string objName)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == objName && g.scene.IsValid()).ToList();
        return objName + ": " + (all.Count == 0 ? "none" : string.Join(",", all.Select(g => $"active={g.activeInHierarchy} pos={g.transform.position}")));
    }

    public static string Enemies() => string.Join(" | ", Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
        .Select(e => $"{e.name}@({e.transform.position.x:F1},{e.transform.position.y:F1})"));

    public static string KillBoss()
    {
        var b = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.StartsWith("Boss_"));
        if (b == null) return "no boss";
        var d = b.GetComponent<IDamageable>();
        for (var i = 0; i < 200 && b != null && b.gameObject.activeInHierarchy; i++)
            d.TakeDamage(new DamageInfo(5, 5, b.transform.position, P.gameObject));
        return "hit boss";
    }

    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    public static string BreakSpeaker(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var d = go.GetComponent<IDamageable>();
        for (var i = 0; i < 10; i++) d.TakeDamage(new DamageInfo(1, 1, go.transform.position, P.gameObject));
        return "hit " + objName;
    }

    public static string Crates() => "crates=" + Object.FindObjectsByType<FallingObject>(FindObjectsSortMode.None).Length;
}
