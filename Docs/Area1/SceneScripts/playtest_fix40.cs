// 2026-10-04 PendingChanges 40 (D2→T3 の跳ね上げ戸の当たりが開いた後も残る) / 41 (↓+ジャンプで下の段の床まですり抜ける) の確認。
// Play 中に run_script で呼ぶ (セーブは事前に Temp/savebak_fix40 へ退避し、後で元に戻す)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_fix40.cs --entry Fix40.State
//   Start(stage)       : はじめから相当 (このプレイのセーブ無し) で stage を PlayerScene ごと開始
//   Go(scene, entrance) / Tp(x, y) / Flag(f) / Unflag(f)
//   Hold(x, down, jump) : 仮想入力 (横 -1..1、下、ジャンプ押しっぱなし)。Release() で全部離す
//   Drop()             : ↓+ジャンプを押す (押しっぱなしにする。Release で離す)
//   Track(sec)         : sec 秒の間の位置・接地・降り抜け中の床の数を 0.1 秒ごとに記録 (TrackLog で読む)
//   Probe()            : 体と足元のすり抜け床 (Platforms) の距離 (Physics2D.Distance の向きの確認)
//   Active(name)       : その名前のオブジェクトの有効/位置、当たりの有効
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class Fix40
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static VirtualPlayerInput Input
    {
        get
        {
            Application.runInBackground = true;
            if (PlayerInputSource.Override is VirtualPlayerInput v) return v;
            v = new VirtualPlayerInput { MergeDevice = false };
            PlayerInputSource.Override = v;
            return v;
        }
    }

    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string StateName()
    {
        var sm = typeof(PlayerController).GetField("_stateMachine", F)?.GetValue(P) as PlayerStateMachine;
        return sm?.CurrentState?.GetType().Name ?? "?";
    }

    static int Dropping()
    {
        var list = typeof(PlayerController).GetField("_dropThroughPlatforms", F)?.GetValue(P) as System.Collections.IList;
        return list?.Count ?? -1;
    }

    public static string State()
    {
        Application.runInBackground = true;
        var p = P;
        if (p == null) return "no player";
        var rb = p.GetComponent<Rigidbody2D>();
        return $"stage={StageLoader.Instance?.CurrentStageName} pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) " +
               $"grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")} state={StateName()} dropping={Dropping()} t={Time.time:F2}";
    }

    public static string Start(string stage)
    {
        Application.runInBackground = true;
        GameSession.PendingLoad = null;
        GameSession.SaveBelongsToRun = false;
        GameProgress.Clear();
        GameSession.ClearRespawnBench();
        StageLoader.LoadWithPlayerScene(stage);
        return "start " + stage;
    }

    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }
    public static string Tp(float x, float y) { Application.runInBackground = true; P.Teleport(new Vector2(x, y)); return "tp " + State(); }
    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }

    public static string Hold(float x, bool down, bool jump)
    {
        var i = Input;
        i.Move = x;
        i.Vertical = down ? -1f : 0f;
        i.SetHeld(PlayerButton.Jump, jump);
        return $"hold x={x} down={down} jump={jump} " + State();
    }

    public static string Drop() => Hold(0f, true, true);

    public static string Release()
    {
        var i = Input;
        i.ReleaseAll();
        return "released " + State();
    }

    public static string ClearOverride() { PlayerInputSource.Override = null; return "override cleared"; }

    // run_script は呼び出しごとに別アセンブリなので、記録は static ではなくファイルへ書く
    static string LogPath => System.IO.Path.Combine(Application.dataPath, "../Temp/fix40_track.txt");

    class Tracker : MonoBehaviour
    {
        public IEnumerator Run(float sec)
        {
            var end = Time.time + sec;
            var log = new StringBuilder();
            while (Time.time < end)
            {
                log.AppendLine(State());
                yield return new WaitForSeconds(0.1f);
            }
            log.AppendLine("end " + State());
            System.IO.File.WriteAllText(LogPath, log.ToString());
            Destroy(gameObject);
        }
    }

    public static string Track(float sec)
    {
        if (System.IO.File.Exists(LogPath)) System.IO.File.Delete(LogPath);
        var tracker = new GameObject("Fix40Tracker").AddComponent<Tracker>();
        tracker.StartCoroutine(tracker.Run(sec));
        return "tracking " + sec;
    }

    public static string TrackLog() => System.IO.File.Exists(LogPath) ? System.IO.File.ReadAllText(LogPath) : "no log yet";

    public static string Probe()
    {
        var p = P;
        var plats = GameObject.Find("Platforms")?.GetComponent<Collider2D>();
        if (plats == null) return "no Platforms";
        var sb = new StringBuilder(State());
        foreach (var own in p.GetComponents<Collider2D>().Where(c => c.enabled && !c.isTrigger))
        {
            var d = Physics2D.Distance(own, plats);
            sb.Append($" | {own.GetType().Name} b=({own.bounds.min.y:F2}..{own.bounds.max.y:F2}) valid={d.isValid} over={d.isOverlapped} dist={d.distance:F3} n=({d.normal.x:F2},{d.normal.y:F2}) A=({d.pointA.x:F2},{d.pointA.y:F2}) B=({d.pointB.x:F2},{d.pointB.y:F2})");
        }
        return sb.ToString();
    }

    public static string Active(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == name && g.scene.IsValid()).ToList();
        return name + ": " + (all.Count == 0 ? "none" : string.Join(", ", all.Select(g =>
        {
            var c = g.GetComponent<Collider2D>();
            return $"active={g.activeInHierarchy} pos={g.transform.position} col={(c ? $"{c.GetType().Name} enabled={c.enabled} trig={c.isTrigger} b={c.bounds.min}..{c.bounds.max}" : "none")}";
        })));
    }
}
