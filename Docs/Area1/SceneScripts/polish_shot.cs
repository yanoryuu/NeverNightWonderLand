// 磨き込み (polish) 用: プレイ中にプレイヤーを任意の位置へ置いて止め、カメラを落ち着かせてから capture_game_view で撮る。
// 例: --entry Shot.Freeze --args '[72,172.5]' → (1〜2 秒待つ) → capture_game_view → --entry Shot.Unfreeze
//     --entry Shot.Go --args '["A1_W4","FromA1_W5"]' (遷移で到着させる)
using UnityEngine;

public static class Shot
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string State()
    {
        var p = P;
        if (p == null) return "no player";
        var rb = p.GetComponent<Rigidbody2D>();
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) " +
               $"grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")} " +
               $"stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} t={Time.time:F1}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    /// <summary>プレイヤーを (x,y) に置いて物理を止める (空中でも落ちない)。HUD 以外の見た目の確認用。</summary>
    public static string Freeze(float x, float y)
    {
        Application.runInBackground = true;
        var p = P;
        p.Teleport(new Vector2(x, y));
        p.GetComponent<Rigidbody2D>().simulated = false;
        return "frozen " + State();
    }

    public static string Unfreeze()
    {
        var p = P;
        p.GetComponent<Rigidbody2D>().simulated = true;
        return "unfrozen " + State();
    }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        var p = P;
        p.GetComponent<Rigidbody2D>().simulated = true;
        p.Teleport(new Vector2(x, y));
        return "tp " + State();
    }

    public static string Go(string scene, string entrance)
    {
        Application.runInBackground = true;
        var p = P;
        if (p != null) p.GetComponent<Rigidbody2D>().simulated = true;
        StageLoader.Instance.TransitionTo(scene, entrance);
        return "go " + scene;
    }

    /// <summary>プレイヤーの速度を与えてから遷移する (縦の出口に跳び込んだ時の再現)。</summary>
    public static string GoWithVelocity(string scene, string entrance, float vx, float vy)
    {
        Application.runInBackground = true;
        var rb = P.GetComponent<Rigidbody2D>();
        rb.simulated = true;
        rb.linearVelocity = new Vector2(vx, vy);
        StageLoader.Instance.TransitionTo(scene, entrance);
        return $"go {scene} with v=({vx},{vy})";
    }

    public static string KillAll()
    {
        var n = 0;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) { Object.Destroy(e.gameObject); n++; }
        return $"killed {n}";
    }

    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }

    /// <summary>name の子 child (例 "SteamC2","SteamFx") が表示された瞬間に Time.timeScale=0 にする (撮影用)。Resume で戻す。</summary>
    public static string PauseWhenActive(string name, string child)
    {
        var go = GameObject.Find(name);
        if (go == null) return "not found " + name;
        var t = go.transform.Find(child);
        var w = new GameObject("PauseWatcher").AddComponent<PauseWatcher>();
        w.Target = t.gameObject;
        return "watching " + name + "/" + child;
    }

    public static string Resume() { Time.timeScale = 1f; return "resumed"; }
}

public class PauseWatcher : MonoBehaviour
{
    public GameObject Target;
    void Update()
    {
        if (Target != null && Target.activeInHierarchy) { Time.timeScale = 0f; Destroy(gameObject); }
    }
}
