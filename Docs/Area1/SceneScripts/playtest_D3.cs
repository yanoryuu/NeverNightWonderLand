// プレイ中の確認 (A1_D3 / D4 / MB2)。PlayD (playtest_D.cs) と同じ流儀 + 歯車・ピストン・闘技場・ボスの確認。
// 例: --entry PlayD3.Status / --entry PlayD3.Tp --args '[245.5,-139]' / --entry PlayD3.OnPlate --args '["G1_Plate0"]'
//     --entry PlayD3.Rel --args '["G1_Plate0"]' (乗っている足場との相対位置 = 運ばれているか)
//     --entry PlayD3.Pistons (全ピストンの位置と押しつぶし判定) / --entry PlayD3.Arena (WaveArena の状態)
//     --entry GearRide.Start --args '["G1_Plate0", 6]' → 7 秒後 --entry GearRide.Log (足場に乗せて放置し、相対位置を記録)
//     --entry PistonTest.Start --args '["PistonC0", 281.5, -197.4]' → 9 秒後 --entry PistonTest.Log (その場に留めて被弾の瞬間を記録)
// 遷移テストの座標: D3→D5 (241.5,-139.5) / D3→D4 (268.5,-196.5) / D4→D3 (268.5,-196.5) / D4→MB2 (376.5,-196.5)
//                  MB2→D4 (376.5,-196.5) / MB2 エレベーター Use("ElevatorDeep") / C1 エレベーター Use("ElevatorDeep")
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class PlayD3
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    public static string State()
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

    public static string OnPlate(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) return "not found " + name;
        P.Teleport((Vector2)g.transform.position + new Vector2(0f, 0.65f));
        return "on " + name + " " + State();
    }

    public static string Rel(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) return "not found " + name;
        var d = P.transform.position - g.transform.position;
        return $"rel=({d.x:F3},{d.y:F3}) g=({g.transform.position.x:F2},{g.transform.position.y:F2}) " + State();
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

    public static string Pistons() => string.Join(" | ", Object.FindObjectsByType<Piston>(FindObjectsSortMode.None).OrderBy(p => p.name)
        .Select(p => $"{p.name} y={p.transform.position.y:F2} crush={p.GetComponentInChildren<HazardZone>().IsHazardActive}")) + $" t={Time.time:F2}";

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
        return $"started={started} wave={idx}/{waves.Length} enemies={enemies.Length} gates={string.Join(",", gates.Select(g => g.activeSelf))} " +
               $"flag={GameProgress.Has("A1_RatNest")} " + Active("KeyItem_A1_Pickup_D7_1") + "\n" + State();
    }

    public static string Gates() => string.Join(" ", Resources.FindObjectsOfTypeAll<GameObject>()
        .Where(g => g.scene.IsValid() && (g.name == "Gate" || g.name.EndsWith("Door") || g.name == "NestGate"))
        .Select(g => $"{g.name}:active={g.activeInHierarchy}@({g.transform.position.x:F1},{g.transform.position.y:F1})"));

    // 闘技場の外 (壁際の敵) 以外を消す: 名前が Clinger の物は残す
    public static string KillArena()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (!e.name.StartsWith("WallClinger")) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    public static string KillBoss()
    {
        var b = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.StartsWith("Boss_"));
        if (b == null) return "no boss";
        var d = b.GetComponent<IDamageable>();
        for (var i = 0; i < 200 && b != null && b.gameObject.activeInHierarchy; i++)
            d.TakeDamage(new DamageInfo(5, 5, b.transform.position, P.gameObject));
        return "hit boss";
    }

    public static string Boss()
    {
        var b = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.StartsWith("Boss_"));
        if (b == null) return "no boss";
        var rb = b.GetComponent<Rigidbody2D>();
        return $"boss pos=({b.transform.position.x:F2},{b.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2})";
    }

    public static string Break(string objName)
    {
        var go = GameObject.Find(objName);
        if (go == null) return "not found " + objName;
        var d = go.GetComponent<IDamageable>();
        for (var i = 0; i < 10; i++) d.TakeDamage(new DamageInfo(1, 1, go.transform.position, P.gameObject));
        return "hit " + objName;
    }
}

// 足場に乗せて放置し、相対位置・接地を時刻ごとに記録 (PlayerPrefs 経由で次の呼び出しへ)
public static class GearRide
{
    static string _log { get => PlayerPrefs.GetString("d3_ride", ""); set => PlayerPrefs.SetString("d3_ride", value); }
    public static string Start(string name, float seconds)
    {
        Application.runInBackground = true;
        _log = "";
        var g = GameObject.Find(name);
        Object.FindAnyObjectByType<PlayerController>().Teleport((Vector2)g.transform.position + new Vector2(0f, 0.65f));
        new GameObject("RideRunner").AddComponent<Runner>().StartCoroutine(Run(g.transform, seconds));
        return "started";
    }
    public static string Log() => _log;
    class Runner : MonoBehaviour {}
    static System.Collections.IEnumerator Run(Transform g, float seconds)
    {
        var t0 = Time.time; var next = 0f;
        while (Time.time - t0 < seconds)
        {
            if (Time.time - t0 >= next)
            {
                var p = Object.FindAnyObjectByType<PlayerController>();
                var d = p.transform.position - g.position;
                _log += $"t={Time.time - t0:F1} rel=({d.x:F2},{d.y:F2}) plate=({g.position.x:F1},{g.position.y:F1}) grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")}\n";
                next += 1f;
            }
            yield return new WaitForFixedUpdate();
        }
        _log += "done\n";
    }
}

// ピストンの下に留めて、押しつぶしの当たりと HP の変化を記録 (9 秒)
public static class PistonTest
{
    static string _log { get => PlayerPrefs.GetString("d3_piston", ""); set => PlayerPrefs.SetString("d3_piston", value); }
    public static string Start(string name, float x, float y)
    {
        Application.runInBackground = true;
        _log = "";
        var p = Object.FindAnyObjectByType<PlayerController>();
        var pis = GameObject.Find(name).GetComponent<Piston>();
        new GameObject("PistonTestRunner").AddComponent<Runner>().StartCoroutine(Run(p, pis, x, y));
        return "started";
    }
    public static string Log() => _log;
    class Runner : MonoBehaviour {}
    static System.Collections.IEnumerator Run(PlayerController p, Piston pis, float x, float y)
    {
        var hp = p.GetComponent<PlayerHealth>();
        var hz = pis.GetComponentInChildren<HazardZone>();
        p.Teleport(new Vector2(x, y));
        int last = hp.Hp.CurrentValue; bool lastOn = hz.IsHazardActive; var t0 = Time.time;
        _log += $"t=0 start hp={last} pistonY={pis.transform.position.y:F2}\n";
        while (Time.time - t0 < 9f)
        {
            if (Time.time - t0 < 0.1f || hp.Hp.CurrentValue == last) { }
            if (hz.IsHazardActive != lastOn) { lastOn = hz.IsHazardActive; _log += $"t={Time.time - t0:F2} crush={lastOn} pistonY={pis.transform.position.y:F2} player=({p.transform.position.x:F2},{p.transform.position.y:F2})\n"; }
            if (hp.Hp.CurrentValue != last)
            {
                _log += $"t={Time.time - t0:F2} HP {last}->{hp.Hp.CurrentValue} pistonY={pis.transform.position.y:F2} player=({p.transform.position.x:F2},{p.transform.position.y:F2})\n";
                last = hp.Hp.CurrentValue;
                hp.Heal(5);
                last = hp.Hp.CurrentValue;
                p.Teleport(new Vector2(x, y));
            }
            yield return new WaitForFixedUpdate();
        }
        _log += $"done pos=({p.transform.position.x:F2},{p.transform.position.y:F2})\n";
    }
}
