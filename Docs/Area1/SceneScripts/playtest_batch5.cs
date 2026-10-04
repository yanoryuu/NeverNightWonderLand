// 2026-10-04 (3) PendingChanges 33〜36 の再生中の確認。Play 中に run_script で呼ぶ。操作は仮想入力 (VirtualPlayerInput, ボットと同じ経路)。
// シーンの移動・スクショは playtest_boxfix.cs (PlayBox.NewGame / Go / Tp / Shot) を使う。
//   Batch5.Carry(platform, dir, move, trials) : 足場に乗せて (dir = 足場がその向きに動いている時 any/up/down/left/right) ジャンプ長押し、
//                                               跳んだ後は move (-1..1) を入れ続ける。各フレームの横速度・引き継ぎの勢い・高さを記録 (33)
//   Batch5.Drop(platform, trials)             : 赤ハサミを付けて ↓+ジャンプ長押しで降りる。押しっぱなしで滑空しないか・乗り直さないか、
//                                               60 フレーム目で離して 66 で押し直す (= 二段ジャンプ。その後の下降で押しっぱなしなら滑空) (34)
//   Batch5.Result / Batch5.Stop
//   Batch5.SlowBreak(x, y, dir, slam, timeScale, pauseAfter) : (x, y) で dir を向いて攻撃 (slam = 落下攻撃) → スロー再生 → pauseAfter 秒 (実時間) で一時停止 (破片の途中のスクショ用, 36)
//   Batch5.Debris : いま飛んでいる破片の数 / Batch5.SetPaused(bool) / Batch5.TimeScale(s) / Batch5.Normal / Batch5.Isolate (実パッドの入力を混ぜない)
// 静的状態は AppDomain に置く (run_script は呼び出しごとに別アセンブリになるため)。
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class Batch5
{
    const string Key = "batch5_probe";
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static PlayerController P => UnityEngine.Object.FindAnyObjectByType<PlayerController>();

    static GameObject FindPlatform(string spec)
    {
        var parts = spec.Split('#');
        var index = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        var ls = UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.InstanceID)
            .Where(m => m.name == parts[0] && m.GetComponent<Collider2D>() != null).ToList();
        return index < ls.Count ? ls[index].gameObject : null;
    }

    static string StateName(PlayerController p)
    {
        var sm = typeof(PlayerController).GetField("_stateMachine", F)?.GetValue(p);
        var cs = sm?.GetType().GetProperty("CurrentState")?.GetValue(sm);
        return cs?.GetType().Name ?? "?";
    }

    class Probe
    {
        public string Mode; // carry / drop
        public string Platform;
        public GameObject Target;
        public int Trials;
        public string Dir;
        public float MoveAfter;
        public VirtualPlayerInput Input;
        public Action<PlayerInputFrame> Handler;
        public StringBuilder Log = new StringBuilder();
        public StringBuilder Summary = new StringBuilder();
        public int Trial, Phase, Frame;
        public float PressFeet, PeakFeet, PressX, PlatVx, MaxCarry;
        public int CarryEndFrame;
        public bool Done, Relanded, GlideWhileHeld, GlideAfterRepress, Repressed;
        public float MinVyHeld, MinVyRepress;
    }

    static object[] Shared
    {
        get => AppDomain.CurrentDomain.GetData(Key) as object[];
        set => AppDomain.CurrentDomain.SetData(Key, value);
    }

    public static string Carry(string platform, string dir, float move, int trials) => Begin("carry", platform, dir, move, trials);

    public static string Drop(string platform, int trials) => Begin("drop", platform, "down", 0f, trials); // 降りていく時に降りる (不具合の起きた状況)

    static string Begin(string mode, string platform, string dir, float move, int trials)
    {
        Application.runInBackground = true;
        Stop();
        var go = FindPlatform(platform);
        if (go == null) return "not found " + platform;
        if (mode == "drop") P.GetComponent<PlayerProgression>()?.Grant(ScissorUpgrade.Red);

        var probe = new Probe { Mode = mode, Target = go, Platform = platform, Trials = trials, Dir = dir, MoveAfter = move, Input = new VirtualPlayerInput() };
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
        return $"started {mode} {platform}";
    }

    public static string Stop()
    {
        var shared = Shared;
        if (shared != null) ((Action)shared[1])();
        Shared = null;
        PlayerInputSource.Override = null;
        return "stopped";
    }

    public static string Result()
    {
        var shared = Shared;
        return shared == null ? "no probe" : ((Func<string>)shared[0])();
    }

    public static string PauseAt(int frame)
    {
        AppDomain.CurrentDomain.SetData(Key + "_pause", frame);
        return "will pause at f" + frame;
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
        var pv = prb ? prb.linearVelocity : Vector2.zero;
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
                if (s.Frame >= 30 && p.IsGrounded && p.GroundCollider == col && DirOk(s.Dir, pv))
                {
                    if (s.Mode == "drop") s.Input.Vertical = -1f;
                    s.Input.Hold(PlayerButton.Jump);
                    s.PressFeet = feet;
                    s.PeakFeet = feet;
                    s.PressX = p.transform.position.x;
                    s.PlatVx = pv.x;
                    s.MaxCarry = 0f;
                    s.CarryEndFrame = -1;
                    s.Relanded = s.GlideWhileHeld = s.GlideAfterRepress = s.Repressed = false;
                    s.MinVyHeld = s.MinVyRepress = 0f;
                    s.Log.AppendLine($"[trial {s.Trial}] press plat v=({pv.x:F2},{pv.y:F2}) feet-top={feet - top:F3}");
                    s.Phase = 2;
                    s.Frame = 0;
                }
                else if (s.Frame >= 6000)
                {
                    s.Summary.AppendLine($"trial {s.Trial}: never grounded on {s.Platform}");
                    Next(s);
                }
                break;

            case 2:
#if UNITY_EDITOR
                if (AppDomain.CurrentDomain.GetData(Key + "_pause") is int pf && pf == s.Frame)
                {
                    AppDomain.CurrentDomain.SetData(Key + "_pause", -1);
                    UnityEditor.EditorApplication.isPaused = true;
                }
#endif
                s.PeakFeet = Mathf.Max(s.PeakFeet, feet);
                var carry = p.CarryMomentumX;
                s.MaxCarry = Mathf.Abs(carry) > Mathf.Abs(s.MaxCarry) ? carry : s.MaxCarry;
                if (s.CarryEndFrame < 0 && s.Frame > 2 && carry == 0f) s.CarryEndFrame = s.Frame;

                if (s.Mode == "carry")
                {
                    if (s.Frame == 2) s.Input.Move = s.MoveAfter;
                    if (s.Frame <= 45 && (s.Frame <= 10 || s.Frame % 3 == 0))
                        s.Log.AppendLine($"  f{s.Frame} dx={p.transform.position.x - s.PressX:F2} vx={rb.linearVelocity.x:F2} carry={carry:F2} rise={feet - s.PressFeet:F2} st={StateName(p)}");
                    var falling = rb.linearVelocity.y < -0.5f;
                    if ((falling && feet < s.PeakFeet - 1f) || p.IsGrounded && s.Frame > 15 || s.Frame > 240)
                    {
                        s.Summary.AppendLine($"trial {s.Trial}: platVx={s.PlatVx:F2} move={s.MoveAfter} rise={s.PeakFeet - s.PressFeet:F2} maxCarry={s.MaxCarry:F2} carryEnd=f{s.CarryEndFrame} dx={p.transform.position.x - s.PressX:F2} frames={s.Frame}");
                        Next(s);
                    }
                }
                else
                {
                    // ↓+ジャンプ押しっぱなし → 60 フレーム目で離す → 66 フレーム目で押し直す
                    if (s.Frame == 8) s.Input.Vertical = 0f;
                    if (s.Frame == 60) s.Input.Release(PlayerButton.Jump);
                    if (s.Frame == 66) { s.Input.Hold(PlayerButton.Jump); s.Repressed = true; }
                    var vy = rb.linearVelocity.y;
                    if (!s.Repressed) s.MinVyHeld = Mathf.Min(s.MinVyHeld, vy); else s.MinVyRepress = Mathf.Min(s.MinVyRepress, vy);
                    var gliding = vy < 0f && Mathf.Abs(vy + p.Consts.GlideFallSpeed) < 0.05f;
                    if (gliding && !s.Repressed && s.Frame < 60) s.GlideWhileHeld = true;
                    if (gliding && s.Repressed) s.GlideAfterRepress = true;
                    if (p.IsGrounded && p.GroundCollider == col && s.Frame > 3) s.Relanded = true;
                    if (s.Frame <= 40 || s.Frame % 5 == 0)
                        s.Log.AppendLine($"  f{s.Frame} feet-top={feet - top:F2} vy={vy:F2} grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "-")} st={StateName(p)}");
                    if (s.Frame >= 260 || (p.IsGrounded && s.Frame > 20))
                    {
                        s.Summary.AppendLine($"trial {s.Trial}: glideWhileHeld={s.GlideWhileHeld} minVyHeld={s.MinVyHeld:F2} relandedOnSame={s.Relanded} " +
                                             $"glideAfterRepress={s.GlideAfterRepress} minVyRepress={s.MinVyRepress:F2} endFeet-top={feet - top:F2} frames={s.Frame}");
                        Next(s);
                    }
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

    // ---- 破片 (36)
    // (x, y) へ移って dir の向きを向き、攻撃 (slam = 空中で↓+攻撃 = 落下攻撃) → timeScale のスローにして、pauseAfter 秒 (実時間) で一時停止
    public static string SlowBreak(float x, float y, float dir, bool slam, float timeScale, float pauseAfter)
    {
        Application.runInBackground = true;
        var input = PlayerInputSource.Override as VirtualPlayerInput;
        if (input == null) { input = new VirtualPlayerInput { MergeDevice = false }; PlayerInputSource.Override = input; }
        input.ReleaseAll();
        var p = P;
        if (slam) p.GetComponent<PlayerProgression>()?.GrantSkill(PlayerSkill.GroundSlam);
        p.Teleport(new Vector2(x, y));
        input.Move = dir;
        DG.Tweening.DOVirtual.DelayedCall(0.06f, () => input.Move = 0f, true);
        var wait = slam ? 0.15f : 0.4f;
        DG.Tweening.DOVirtual.DelayedCall(wait, () =>
        {
            if (slam) input.Vertical = -1f;
            input.Tap(PlayerButton.Attack);
            DG.Tweening.DOVirtual.DelayedCall(slam ? 0.25f : 0.12f, () =>
            {
                input.Vertical = 0f;
                Time.timeScale = timeScale;
#if UNITY_EDITOR
                if (pauseAfter > 0f)
                    DG.Tweening.DOVirtual.DelayedCall(pauseAfter, () => UnityEditor.EditorApplication.isPaused = true, true);
#endif
            }, true);
        }, true);
        return "slow break at " + x + "," + y;
    }

    public static string Normal()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = false;
#endif
        return "normal";
    }

    public static string Debris()
    {
        var ps = UnityEngine.Object.FindObjectsByType<DebrisPiece>(FindObjectsSortMode.None);
        return $"debris active={ps.Length} " + string.Join(" ", ps.Take(8).Select(d => $"({d.transform.position.x:F2},{d.transform.position.y:F2})"));
    }

    public static string Unpause()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = false;
#endif
        return "unpaused";
    }

    /// <summary>実デバイス (つながっているパッド) の入力を混ぜない仮想入力にする (スティックのゆらぎで勝手に動かないように)。</summary>
    public static string Isolate()
    {
        PlayerInputSource.Override = new VirtualPlayerInput { MergeDevice = false };
        return "isolated";
    }

    /// <summary>エディタの一時停止を切り替える (スロー中に少しだけ進めてスクショ: SetPaused false → 次の呼び出しで true)。</summary>
    public static string SetPaused(bool paused)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = paused;
#endif
        return $"paused={paused} t={Time.time:F2}";
    }

    public static string TimeScale(float scale)
    {
        Time.timeScale = scale;
        return "timeScale " + scale;
    }
}
