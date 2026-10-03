// 「箱が壊せない」(2026-10-04) の再生中の確認。Play 中に run_script で呼ぶ。操作は仮想入力 (VirtualPlayerInput, ボットと同じ経路)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_boxfix.cs --entry PlayBox.NewGame
//   NewGame / State / Go(scene, entrance) / Tp(x, y) / Move(dir) / Attack / Shot(name)
//   Boxes(x, y, r)  : 半径 r 内の BreakableBox / SkillBreakable (名前・位置・hp・レイヤー・当たりの有無・Rigidbody の状態)
//   Probe           : いまの位置で通常攻撃を振ったら何に当たり、どの箱が選ばれるか (PerformAttackHit と同じ問い合わせ)
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayBox
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static VirtualPlayerInput Input
    {
        get
        {
            Application.runInBackground = true;
            if (PlayerInputSource.Override is VirtualPlayerInput v) return v;
            v = new VirtualPlayerInput { MergeDevice = true };
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

    public static string State()
    {
        Application.runInBackground = true;
        var p = P;
        if (p == null) return "no player stage=" + SceneManager.GetActiveScene().name;
        return $"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) grounded={p.IsGrounded} facing={p.Facing} state={StateName()} " +
               $"stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : SceneManager.GetActiveScene().name)} t={Time.time:F1}";
    }

    public static string NewGame()
    {
        Application.runInBackground = true;
        GameSession.PendingLoad = null;
        GameProgress.Clear();
        StageLoader.LoadWithPlayerScene("A1_T1");
        return "new game";
    }

    public static string Go(string scene, string entrance)
    {
        StageLoader.Instance.TransitionTo(scene, entrance);
        return "go " + scene;
    }

    public static string Entrances()
    {
        return string.Join("\n", Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name.Contains("Transition") || m.GetType().Name.Contains("SpawnPoint"))
            .Select(m => $"{m.GetType().Name} {m.name} ({m.transform.position.x:F1},{m.transform.position.y:F1})"));
    }

    public static string Tp(float x, float y)
    {
        Input.Move = 0f;
        P.Teleport(new Vector2(x, y));
        return "tp " + State();
    }

    public static string Move(float dir)
    {
        Input.Move = dir;
        return "move " + dir + " " + State();
    }

    public static string Vertical(float v)
    {
        Input.Vertical = v;
        return "vertical " + v;
    }

    public static string Jump()
    {
        Input.Tap(PlayerButton.Jump);
        return "jump " + State();
    }

    public static string Attack()
    {
        Input.Tap(PlayerButton.Attack);
        return "attack " + State();
    }

    public static string Slam()
    {
        Input.Vertical = -1f;
        Input.Tap(PlayerButton.Attack);
        DG.Tweening.DOVirtual.DelayedCall(1.5f, () => Input.Vertical = 0f, false);
        return "slam " + State();
    }

    public static string Boxes(float x, float y, float r)
    {
        var c = new Vector2(x, y);
        var sb = new StringBuilder();
        foreach (var b in Object.FindObjectsByType<BreakableBox>(FindObjectsSortMode.None).Where(b => Vector2.Distance(b.transform.position, c) <= r))
        {
            var col = b.GetComponent<Collider2D>();
            var rb = b.GetComponent<Rigidbody2D>();
            var hp = typeof(BreakableBox).GetField("_hp", F).GetValue(b);
            sb.AppendLine($"box {b.transform.parent?.name}/{b.name} ({b.transform.position.x:F2},{b.transform.position.y:F2}) hp={hp} layer={LayerMask.LayerToName(b.gameObject.layer)} " +
                          $"col={(col ? col.GetType().Name + " en=" + col.enabled + " trig=" + col.isTrigger + " b=" + col.bounds.size : "none")} " +
                          $"rb={(rb ? rb.bodyType + " sim=" + rb.simulated + " awake=" + rb.IsAwake() + " v=" + rb.linearVelocity : "none")}");
        }
        foreach (var b in Object.FindObjectsByType<SkillBreakable>(FindObjectsSortMode.None).Where(b => Vector2.Distance(b.transform.position, c) <= r))
            sb.AppendLine($"skill {b.transform.parent?.name}/{b.name} ({b.transform.position.x:F2},{b.transform.position.y:F2}) {b.RequiredSkill}");
        return sb.Length == 0 ? "none" : sb.ToString();
    }

    public static string Probe()
    {
        var p = P;
        var consts = p.Consts;
        var profile = p.CurrentMeleeProfile;
        var center = (Vector2)p.transform.position + new Vector2(profile.Offset.x * p.Facing, profile.Offset.y);
        var hits = Physics2D.OverlapBoxAll(center, profile.BoxSize, 0f, consts.AttackTargetLayer);
        var sb = new StringBuilder($"{State()}\nprofile off={profile.Offset} size={profile.BoxSize} center={center} mask={consts.AttackTargetLayer.value}\n");
        foreach (var h in hits)
            sb.AppendLine($" hit {h.transform.parent?.name}/{h.name} {h.GetType().Name} layer={LayerMask.LayerToName(h.gameObject.layer)} " +
                          $"box={(h.GetComponent<BreakableBox>() != null)} skill={(h.GetComponent<SkillBreakable>() != null)} dmg={(h.GetComponent<IDamageable>() != null)} " +
                          $"rb={(h.attachedRigidbody ? h.attachedRigidbody.name : "-")} c=({h.bounds.center.x:F2},{h.bounds.center.y:F2})");
        return sb.ToString();
    }

    public static string Shot(string name)
    {
        Application.runInBackground = true;
        System.IO.Directory.CreateDirectory("Assets/Screenshots/Area1");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/Area1/{name}.png");
        return "shot " + name;
    }

    public static string Neutral()
    {
        Input.ReleaseAll();
        return "neutral " + State();
    }

    public static string VirtualOff()
    {
        PlayerInputSource.Override = null;
        return "virtual off";
    }
}

// 2026-10-04 (2) PendingChanges 29〜31 の確認用 (playtest_boxfix の続き)。
//   Registry          : 壊れた木箱の記録 (BrokenCrateRegistry) の数とコインのフラグ
//   Coins(x, y, r)    : 半径 r 内に落ちているコイン (CoinPiece)
//   Home              : ホーム画面 (セーブポイントで休む) が開いているか / Close で閉じる
public static class PlayBox2
{
    public static string Registry() =>
        $"brokenCrates={BrokenCrateRegistry.Count} coinH2={GameProgress.Has("A1_Coin_H2_1")} " + PlayBox.State();

    public static string Coins(float x, float y, float r)
    {
        var c = new Vector2(x, y);
        var list = Object.FindObjectsByType<CoinPiece>(FindObjectsSortMode.None)
            .Where(p => Vector2.Distance(p.transform.position, c) <= r)
            .Select(p => $"coin {p.CoinId} ({p.transform.position.x:F2},{p.transform.position.y:F2})").ToArray();
        return list.Length == 0 ? "no coin" : string.Join("\n", list);
    }

    public static string Home()
    {
        var home = Object.FindFirstObjectByType<HomeUIView>();
        var menu = home == null ? null : typeof(HomeUIView).GetField("_menu", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(home) as MenuPanelView;
        return $"home={(home != null)} open={(menu != null && menu.IsOpen)} paused={GamePause.IsPaused}";
    }

    /// <summary>通常攻撃を 1 回振って、0.6 秒後に (x, y) へ離れる (コインを拾わずに部屋を出る確認用)。</summary>
    public static string HitAndRun(float x, float y)
    {
        var input = PlayerInputSource.Override as VirtualPlayerInput;
        input.Tap(PlayerButton.Attack);
        DG.Tweening.DOVirtual.DelayedCall(0.6f, () => Object.FindAnyObjectByType<PlayerController>().Teleport(new Vector2(x, y)), false);
        return "hit and run";
    }

    /// <summary>コインのフラグを取り下げる (「壊したがコインは取らずに出た」状態を作る。磁力で拾ってしまうため)。</summary>
    public static string UnsetCoin(string coinId)
    {
        GameProgress.Unset(coinId);
        return Registry();
    }

    /// <summary>インタラクトのボタンを押す (セーブポイントで休む)。</summary>
    public static string Interact()
    {
        (PlayerInputSource.Override as VirtualPlayerInput).Tap(PlayerButton.Interact);
        return "interact";
    }

    public static string Close()
    {
        var home = Object.FindFirstObjectByType<HomeUIView>();
        typeof(HomeUIView).GetMethod("CloseMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(home, null);
        return Home();
    }
}
