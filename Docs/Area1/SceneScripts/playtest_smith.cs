// 拠点 A1_H の鍛冶師 (赤ハサミ = 中ボスどちらかの撃破後) と、出口・ご褒美の確認用 (2026-10-03)。
// 例: --entry Smith.LogOn → --entry Smith.Talk (Utage 会話 A1_Smith_Locked が始まる) → Smith.Dialogue → Smith.Log
//     --entry Smith.Flag --args '["A1_MidBossLower"]' → Smith.Talk → Smith.HasRed
// 通知 (Notifier) は run_script ごとに別アセンブリになるので PlayerPrefs に貯める (LogOff で止めて消す)。
using System.Linq;
using TMPro;
using UnityEngine;

public static class Smith
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();
    const string Key = "SmithLog";

    static string State()
    {
        var p = P;
        if (p == null) return "no player";
        var inv = p.GetComponent<PlayerItemInventory>();
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) grounded={p.IsGrounded} " +
               $"stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} thread={(inv != null ? inv.Thread.CurrentValue : -1)}";
    }

    public static string Status() { Application.runInBackground = true; return State(); }

    static void OnNotify(string m)
    {
        if (PlayerPrefs.GetInt(Key + "On", 0) != 1) return;
        PlayerPrefs.SetString(Key, PlayerPrefs.GetString(Key, "") + "\n[通知] " + m);
    }

    public static string LogOn()
    {
        Application.runInBackground = true;
        PlayerPrefs.SetInt(Key + "On", 1);
        PlayerPrefs.SetString(Key, "");
        Notifier.OnNotify += OnNotify;
        return "log on";
    }

    public static string LogOff() { PlayerPrefs.SetInt(Key + "On", 0); PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + "On"); return "log off"; }
    public static string Log() => PlayerPrefs.GetString(Key, "(none)");

    /// <summary>画面上部の案内枠 (TutorialMessageView) の文字と表示状態。</summary>
    public static string Message()
    {
        var v = Object.FindAnyObjectByType<TutorialMessageView>();
        if (v == null) return "no TutorialMessageView";
        var label = v.GetComponentInChildren<TMP_Text>(true);
        return $"msg alpha={v.GetComponent<CanvasGroup>().alpha:F2} text={(label != null ? label.text : "-")}";
    }

    public static string Use(string objName)
    {
        var go = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == objName && g.scene.IsValid() && g.activeInHierarchy);
        if (go == null) return "not found (active) " + objName;
        var it = go.GetComponent<IInteractable>();
        if (it == null) return "no IInteractable on " + objName;
        it.Interact(P.gameObject);
        return $"used {objName} prompt={it.PromptText} " + State();
    }

    /// <summary>鍛冶師の前に立って話す。</summary>
    public static string Talk()
    {
        var smith = Object.FindAnyObjectByType<Blacksmith>();
        if (smith == null) return "no Blacksmith";
        var p = P;
        if (Vector2.Distance(p.transform.position, smith.transform.position) > 2f)
            p.Teleport((Vector2)smith.transform.position + new Vector2(1.2f, 1f));
        smith.Interact(p.gameObject);
        return $"talk unlocked={smith.IsUnlocked} prompt={smith.PromptText} red={HasRedBool()} | " + Message() + " | " + Dialogue();
    }

    /// <summary>Utage 会話を 1 回送る (クリックの代わり。文字送り中なら全文表示、表示済みなら次のページへ)。</summary>
    public static string Advance()
    {
        var engine = Object.FindAnyObjectByType<Utage.AdvEngine>();
        if (engine == null) return "no AdvEngine";
        engine.Page.InputSendMessage();
        return "advance | " + Dialogue();
    }

    /// <summary>Utage 会話 (DialogueService) の再生状態と、表示中のメッセージ (宴のメッセージウィンドウの TMP / Text)。</summary>
    public static string Dialogue()
    {
        var engine = Object.FindAnyObjectByType<Utage.AdvEngine>();
        var page = engine != null && engine.Page != null ? engine.Page.TextData?.OriginalText : null;
        return $"dialogue playing={DialogueService.IsPlaying} name={(engine != null && engine.Page != null ? engine.Page.NameText : "-")} text={page ?? "-"}";
    }

    static bool HasRedBool() => P.GetComponent<PlayerProgression>().Has(ScissorUpgrade.Red);
    public static string HasRed() => "Red=" + HasRedBool();
    public static string RevokeRed() { P.GetComponent<PlayerProgression>().Revoke(ScissorUpgrade.Red); return HasRed(); }
    public static string Tp(float x, float y) { Application.runInBackground = true; P.Teleport(new Vector2(x, y)); return "tp " + State(); }
    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }
    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
    public static string Has(string f) => $"{f}={GameProgress.Has(f)}";
    public static string Flags(string csv) => string.Join(" ", csv.Split(',').Select(f => $"{f}={GameProgress.Has(f)}"));
    public static string UnsetFlags(string csv) { foreach (var f in csv.Split(',')) GameProgress.Unset(f); return "unset " + csv; }

    public static string Active(string objName)
    {
        var go = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == objName && g.scene.IsValid());
        return go == null ? "missing " + objName : $"{objName} active={go.activeInHierarchy} pos={go.transform.position}";
    }

    /// <summary>横入力 dir を押したまま Space も押す (離すのは Walk(dir) / Walk(0))。段差の上り用。</summary>
    public static string WalkJump(int dir)
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var keys = dir < 0 ? new[] { UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.Space }
            : dir > 0 ? new[] { UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.Space }
            : new[] { UnityEngine.InputSystem.Key.Space };
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
        return $"walkjump {dir} " + State();
    }

    /// <summary>プレイヤーの右/左/上/下の移動 (QueueStateEvent の押しっぱなし)。dir: -1 左 / 1 右 / 0 離す。</summary>
    public static string Walk(int dir)
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var state = dir < 0 ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.A)
            : dir > 0 ? new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D)
            : new UnityEngine.InputSystem.LowLevel.KeyboardState();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, state);
        return $"walk {dir} " + State();
    }
}
