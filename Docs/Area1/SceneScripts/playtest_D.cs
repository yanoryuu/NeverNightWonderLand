// プレイ中の確認 (A1_D1 / D2 / D5)。PlayW5 と同じ流儀 + ベルト・水・蒸気の確認。
// 例: --entry PlayD.Status / --entry PlayD.Tp --args '[244.5,-100]' / --entry PlayD.Use --args '["BeltLever"]'
//     --entry PlayD.Drift --args '[2]' (2 秒待たずに即座の位置を返す。速度と外力の確認は Status を繰り返す)
//     --entry PlayD.Steam --args '["SteamC1"]' (蒸気の ON/OFF) / --entry PlayD.Water (水中の倍率)
// 遷移テストの座標: D1→H (244.5,-63) / D1→D2 (244.5,-100) / D2→D1 (244.5,-97) / D2→D5 (190.5,-121)
//                  D5→D2 (190.5,-121) / D2w→T3 (150.5,-103.5) / D5→D3 (241.5,-139.5)
using System.Linq;
using UnityEngine;

public static class PlayD
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
               $"hp={(hp ? hp.Hp.CurrentValue.ToString() : "-")} t={Time.time:F2}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        P.Teleport(new Vector2(x, y));
        return "tp " + State();
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

    public static string Belts() => string.Join(" | ", Object.FindObjectsByType<Conveyor>(FindObjectsSortMode.None)
        .Select(c => $"{c.name} speed={c.CurrentSpeed:F1}"));

    public static string Steam(string name)
    {
        var go = GameObject.Find(name);
        var hz = go ? go.GetComponent<HazardZone>() : null;
        if (hz == null) return "not found " + name;
        return $"{name} active={hz.IsHazardActive} t={Time.time:F2} " + State();
    }

    public static string SteamAll() => string.Join(" ", Object.FindObjectsByType<HazardZone>(FindObjectsSortMode.None)
        .OrderBy(h => h.name).Select(h => $"{h.name}:{(h.IsHazardActive ? "ON" : "off")}")) + $" t={Time.time:F2}";

    public static string Hp(int v)
    {
        var hp = P.GetComponent<PlayerHealth>();
        hp.Heal(v);
        return State();
    }

    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }
}
