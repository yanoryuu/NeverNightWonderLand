// プレイ中の確認 (A1_AB / A1_FP)。PlayW5 と同じ流儀 + ボスの HP・段階、赤ハサミ、実入力の跳躍 (JumpSeq)。
// 例: --entry PlayA.Status / --entry PlayA.Tp --args '[352,-33]' / --entry PlayA.HitBoss --args '[9]'
//     --entry PlayA.Jump --args '[2,-1,0.38,0.1,0.5]' (二段ジャンプ: 回数, 横入力 -1/0/1, 2 回目までの秒, 横入力を押す時間帯) → 数秒後に PlayA.JumpLog
//     --entry PlayA.Red / PlayA.RevokeRed (テスト後の後始末)
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PlayA
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string State()
    {
        var p = P;
        if (p == null) return "no player";
        var rb = p.GetComponent<Rigidbody2D>();
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) grounded={p.IsGrounded} " +
               $"ground={(p.GroundCollider ? p.GroundCollider.name : "null")} stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} t={Time.time:F1}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }
    public static string Tp(float x, float y) { Application.runInBackground = true; P.Teleport(new Vector2(x, y)); return "tp " + State(); }
    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }

    public static string Use(string objName)
    {
        var go = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == objName && g.scene.IsValid() && g.activeInHierarchy);
        if (go == null) return "not found (active) " + objName;
        var it = go.GetComponent<IInteractable>();
        if (it == null) return "no IInteractable on " + objName;
        it.Interact(P.gameObject);
        return $"used {objName} " + State();
    }

    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
    public static string Has(string f) => $"{f}={GameProgress.Has(f)}";
    public static string Flags(string csv) => string.Join(" ", csv.Split(',').Select(f => $"{f}={GameProgress.Has(f)}"));
    public static string SetFlags(string csv) { foreach (var f in csv.Split(',')) GameProgress.Set(f); return "set " + csv; }
    public static string UnsetFlags(string csv) { foreach (var f in csv.Split(',')) GameProgress.Unset(f); return "unset " + csv; }

    public static string HasRed() => "Red=" + P.GetComponent<PlayerProgression>().Has(ScissorUpgrade.Red);
    public static string Red() { P.GetComponent<PlayerProgression>().Grant(ScissorUpgrade.Red); return HasRed(); }
    public static string RevokeRed() { P.GetComponent<PlayerProgression>().Revoke(ScissorUpgrade.Red); return HasRed(); }

    public static string Active(string objName)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == objName && g.scene.IsValid()).ToList();
        return objName + ": " + (all.Count == 0 ? "none" : string.Join(",", all.Select(g => $"active={g.activeInHierarchy} pos={g.transform.position}")));
    }

    static EnemyController Boss => Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.StartsWith("Boss_"));

    public static string BossInfo()
    {
        var b = Boss;
        var ph = Object.FindAnyObjectByType<BossPhaseSwitch>();
        var gates = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == "Gate" && g.scene.IsValid()).Select(g => g.activeSelf);
        return (b == null ? "no boss" : $"{b.name} hp={b.Hp.CurrentValue}/{b.Consts.MaxHp} guard={b.Guard.CurrentValue} pos=({b.transform.position.x:F1},{b.transform.position.y:F1}) scale={b.transform.localScale.x}")
               + $" phase2={(ph ? ph.InPhase2.ToString() : "-")} gates=[{string.Join(",", gates)}]";
    }

    // HP を n だけ削る (防御を無視するため大きな防御ダメージを同時に与える)
    public static string HitBoss(int n)
    {
        var b = Boss;
        if (b == null) return "no boss";
        var d = b.GetComponent<IDamageable>();
        for (var i = 0; i < n && b != null && !b.IsDead; i++)
            d.TakeDamage(new DamageInfo(1, 50, b.transform.position, P.gameObject));
        return "hit " + BossInfo();
    }

    public static string Lifts() => string.Join(" | ", Object.FindObjectsByType<Piston>(FindObjectsSortMode.None).OrderBy(p => p.name)
        .Select(p => $"{p.name}@{p.transform.position.y:F2}"));

    public static string Notes() => string.Join(" | ", Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).OrderBy(p => p.name)
        .Select(p => $"{p.name}@({p.transform.position.x:F1},{p.transform.position.y:F1})"));

    // ---- 実入力の跳躍: jumps 回 Space を押す (2 回目は delay 秒後)、横入力 dir を押し続ける
    public static string Jump(int jumps, int dir, float delay, float moveFrom, float moveTo)
    {
        Application.runInBackground = true;
        var go = new GameObject("JumpSeq");
        var s = go.AddComponent<JumpSeq>();
        s.Jumps = jumps; s.Dir = dir; s.Delay = delay; s.MoveFrom = moveFrom; s.MoveTo = moveTo;
        return "started " + State();
    }

    public static string JumpLog() => PlayerPrefs.GetString("PlayA_Jump", "(none)") + " now " + State();
}

public class JumpSeq : MonoBehaviour
{
    public int Jumps = 1, Dir;
    public float Delay = 0.3f, MoveFrom = 0f, MoveTo = 9f;
    float _maxY = float.MinValue, _startY;

    IEnumerator Start()
    {
        var p = Object.FindAnyObjectByType<PlayerController>();
        _startY = p.transform.position.y;
        var kb = Keyboard.current;
        Key? move = Dir < 0 ? Key.A : Dir > 0 ? Key.D : (Key?)null;
        KeyboardState S(params Key[] keys) => new KeyboardState(keys);
        var t = 0f;
        Key[] With(params Key[] k) => move.HasValue && t >= MoveFrom && t <= MoveTo ? k.Concat(new[] { move.Value }).ToArray() : k;

        InputSystem.QueueStateEvent(kb, S(With(Key.Space)));
        var pressed = 1;
        var spaceDown = true;
        var released = false;
        while (t < 3.0f)
        {
            t += Time.deltaTime;
            _maxY = Mathf.Max(_maxY, p.transform.position.y);
            if (!released && t > Delay - 0.06f) { spaceDown = false; released = true; }
            if (pressed < Jumps && t > Delay) { spaceDown = true; pressed++; }
            if (t > 1.6f) spaceDown = false;
            InputSystem.QueueStateEvent(kb, spaceDown ? S(With(Key.Space)) : S(With()));
            yield return null;
        }
        InputSystem.QueueStateEvent(kb, S());
        PlayerPrefs.SetString("PlayA_Jump", $"start={_startY:F2} maxY={_maxY:F2} rise={_maxY - _startY:F2} end=({p.transform.position.x:F2},{p.transform.position.y:F2}) grounded={p.IsGrounded} ground={(p.GroundCollider ? p.GroundCollider.name : "null")}");
        Destroy(gameObject);
    }
}
