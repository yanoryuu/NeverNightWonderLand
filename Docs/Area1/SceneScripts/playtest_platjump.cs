// 動く足場からのジャンプの確認 (2026-10-04, PendingChanges 32)。
// 実入力 (VirtualPlayerInput) で「足場に乗せる → 接地を待つ → ジャンプ長押し」を繰り返し、
// 押した前後の接地・速度とジャンプの高さ (押した瞬間の足場上面からの最高到達点) を記録する。
// 例: --entry PlatJump.List
//     --entry PlatJump.Start --args '["Lift#0", 5, false, "down"]'   (足場名#同名の何番目, 試行回数, 二段ジャンプも押すか, 跳ぶ時の足場の向き any/up/down/left/right、drop = ↓+ジャンプで降りる)
//     --entry PlatJump.Result  (実行中なら途中経過)
//     --entry PlatJump.Stop
// 静的状態は AppDomain に置く (run_script は呼び出しごとに別アセンブリになるため)。
using System;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PlatJump
{
    const string Key = "platjump_probe";

    static PlayerController P => UnityEngine.Object.FindAnyObjectByType<PlayerController>();

    // "Lift" = 最初の 1 つ / "Lift#1" = 同名の 2 つ目 (List の並び = instanceID 順)
    static GameObject FindPlatform(string spec)
    {
        var parts = spec.Split('#');
        var index = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        var ls = UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.InstanceID)
            .Where(m => m.name == parts[0] && m.GetComponent<Collider2D>() != null).ToList();
        if (index < ls.Count) return ls[index].gameObject;

        // 動く足場以外 (コンベアなど): 同名で Collider2D を持つ物
        var others = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.InstanceID)
            .Where(c => c.name == parts[0] && !c.isTrigger).ToList();
        return index < others.Count ? others[index].gameObject : null;
    }

    public static string List()
    {
        Application.runInBackground = true;
        var ls = UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.InstanceID)
            .Where(m => m.GetComponent<Collider2D>() != null);
        return string.Join(" | ", ls.Select(m =>
        {
            var c = m.GetComponent<Collider2D>();
            var rb = m.GetComponent<Rigidbody2D>();
            return $"{m.name}@({m.transform.position.x:F1},{m.transform.position.y:F1}) v=({rb.linearVelocity.x:F1},{rb.linearVelocity.y:F1}) " +
                   $"size=({c.bounds.size.x:F1},{c.bounds.size.y:F1}) run={m.IsRunning} eff={c.usedByEffector}";
        }));
    }

    class Probe
    {
        public string Platform;
        public GameObject Target;
        public int Trials;
        public bool DoubleJump;
        public string Dir; // any / up / down / left / right (足場がその向きに動いている時に跳ぶ)
        public VirtualPlayerInput Input;
        public Action<PlayerInputFrame> Handler;
        public StringBuilder Log = new StringBuilder();
        public StringBuilder Summary = new StringBuilder();
        public int Trial;
        public int Phase; // 0 = 乗せる, 1 = 接地待ち, 2 = ジャンプ中
        public int Frame;
        public float PressY, PeakY, PressTopY;
        public bool Done;
        public bool DoubleFired;
        public Vector2 RelAt10; // 乗って 10 フレーム目の足場との相対位置 (ジャンプ直前と比べてずれ = 滑り)
    }

    // AppDomain には共通の型だけを置く (Probe の型はアセンブリごとに別物になる)
    // [0] = 結果を返す Func<string>, [1] = 止める Action
    static object[] Shared
    {
        get => AppDomain.CurrentDomain.GetData(Key) as object[];
        set => AppDomain.CurrentDomain.SetData(Key, value);
    }

    public static string Start(string platform, int trials, bool doubleJump, string dir)
    {
        Application.runInBackground = true;
        Stop();
        var go = FindPlatform(platform);
        if (go == null) return "not found " + platform;
        if (doubleJump) P.GetComponent<PlayerProgression>()?.Grant(ScissorUpgrade.Red);

        var probe = new Probe { Target = go, Platform = platform, Trials = trials, DoubleJump = doubleJump, Dir = dir, Input = new VirtualPlayerInput() };
        probe.Handler = _ => Tick(probe);
        PlayerInputSource.Override = probe.Input;
        PlayerInputSource.ConsumedByPlayer += probe.Handler;
        Func<string> result = () => $"done={probe.Done} trial={probe.Trial}/{probe.Trials}\n" + probe.Summary + "\n--- log ---\n" + probe.Log;
        Action stop = () =>
        {
            PlayerInputSource.ConsumedByPlayer -= probe.Handler;
            probe.Done = true;
        };
        Shared = new object[] { result, stop };
        return "started " + platform;
    }

    public static string Stop()
    {
        var shared = Shared;
        if (shared != null)
            ((Action)shared[1])();
        Shared = null;
        PlayerInputSource.Override = null;
        return "stopped";
    }

    public static string PauseMidJump()
    {
        AppDomain.CurrentDomain.SetData(Key + "_pause", true);
        return "will pause";
    }

    public static string Resume()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = false;
#endif
        return "resumed";
    }

    public static string Result()
    {
        var shared = Shared;
        return shared == null ? "no probe" : ((Func<string>)shared[0])();
    }

    static void Tick(Probe s)
    {
        if (s.Done) return;
        var p = P;
        var go = s.Target;
        if (p == null || go == null) return;
        var rb = p.GetComponent<Rigidbody2D>();
        var cap = p.GetComponent<CapsuleCollider2D>();
        var col = go.GetComponent<Collider2D>();
        var prb = go.GetComponentInParent<Rigidbody2D>();
        var feetOffset = p.transform.position.y - cap.bounds.min.y;
        var feet = cap.bounds.min.y;
        var top = col.bounds.max.y;
        s.Frame++;

        switch (s.Phase)
        {
            case 0:
                s.Input.ReleaseAll();
                p.Teleport(new Vector2(col.bounds.center.x, top + feetOffset + 0.02f));
                s.Phase = 1;
                s.Frame = 0;
                break;

            case 1:
                if (s.Frame == 10)
                    s.RelAt10 = (Vector2)(p.transform.position - go.transform.position);
                // 30 フレーム乗せて (運ばれているかを見る) からジャンプ
                if (s.Frame >= 30 && p.IsGrounded && p.GroundCollider == col && DirOk(s.Dir, prb ? prb.linearVelocity : Vector2.zero))
                {
                    if (s.Dir == "drop") s.Input.Vertical = -1f; // ↓+ジャンプ = すり抜け床から降りる
                    s.Input.Hold(PlayerButton.Jump);
                    s.PressY = feet;
                    s.PressTopY = top;
                    s.PeakY = feet;
                    var drift = (Vector2)(p.transform.position - go.transform.position) - s.RelAt10;
                    s.Log.AppendLine($"[trial {s.Trial}] press  ride drift since f10=({drift.x:F3},{drift.y:F3}) over {s.Frame - 10}f  feet-top={feet - top:F3} plat v=({(prb ? prb.linearVelocity.x : 0f):F2},{(prb ? prb.linearVelocity.y : 0f):F2})");
                    s.Phase = 2;
                    s.Frame = 0;
                    s.DoubleFired = false;
                }
                else if (s.Frame >= 1500)
                {
                    s.Summary.AppendLine($"trial {s.Trial}: never grounded on {s.Platform} (grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")})");
                    Next(s);
                }
                break;

            case 2:
                s.PeakY = Mathf.Max(s.PeakY, feet);
#if UNITY_EDITOR
                // スクショ用: PauseMidJump を呼んでおくと、次のジャンプの上昇中にエディタを一時停止する
                if (s.Frame == 20 && AppDomain.CurrentDomain.GetData(Key + "_pause") is true)
                {
                    AppDomain.CurrentDomain.SetData(Key + "_pause", false);
                    UnityEditor.EditorApplication.isPaused = true;
                }
#endif
                if (s.Frame <= (s.Dir == "drop" ? 200 : 8))
                    s.Log.AppendLine($"  f{s.Frame} feet-top={feet - top:F3} v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) grounded={p.IsGrounded} st={p.CurrentStateName()}");

                if (s.DoubleJump && !s.DoubleFired && s.Frame > 6 && rb.linearVelocity.y <= 0f)
                {
                    // 頂点で一度離して押し直す = 二段ジャンプ
                    s.Input.Release(PlayerButton.Jump);
                    s.DoubleFired = true;
                    s.Log.AppendLine($"  release for double at f{s.Frame} rise={feet - s.PressY:F2}");
                }
                else if (s.DoubleFired && !s.Input.IsHeld(PlayerButton.Jump))
                {
                    s.Input.Hold(PlayerButton.Jump);
                }

                // 下降に転じて (二段なら二回目の後) 十分落ちたら終了
                var falling = rb.linearVelocity.y < -0.5f && (!s.DoubleJump || (s.DoubleFired && s.Frame > 40));
                if ((falling && feet < s.PeakY - 1f) || s.Frame > 240)
                {
                    var rise = s.PeakY - s.PressY;
                    s.Summary.AppendLine($"trial {s.Trial}: rise={rise:F2} end feet-top={feet - top:F2} (peak above press-time platform top {s.PeakY - s.PressTopY:F2}) frames={s.Frame}");
                    Next(s);
                }
                break;
        }
    }

    static bool DirOk(string dir, Vector2 v) => dir switch
    {
        "up" => v.y > 0.5f,
        "down" => v.y < -0.5f,
        "left" => v.x < -0.5f,
        "right" => v.x > 0.5f,
        _ => true,
    };

    static void Next(Probe s)
    {
        s.Input.ReleaseAll();
        s.Trial++;
        s.Phase = 0;
        s.Frame = 0;
        if (s.Trial >= s.Trials)
        {
            s.Done = true;
            PlayerInputSource.ConsumedByPlayer -= s.Handler;
            PlayerInputSource.Override = null;
        }
    }

    static string CurrentStateName(this PlayerController p)
    {
        var f = typeof(PlayerController).GetField("_stateMachine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sm = f?.GetValue(p);
        var cs = sm?.GetType().GetProperty("CurrentState")?.GetValue(sm);
        return cs?.GetType().Name ?? "?";
    }
}
