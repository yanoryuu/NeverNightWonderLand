// 2026-10-08 PendingChanges 43〜47 の確認 (歯車の重馬の絵 / 梯子 / 立ち止まって会話 / 宴の先読み / 休んだらセーブ)。
// Play 中に run_script で呼ぶ。セーブは事前に退避し、後でバイト一致で戻すこと。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_batch6.cs --entry Batch6.State
//   Start(stage) / Go(scene, entrance) / Tp(x, y) / Flag(f) / Title() / TitlePick(method, slot) / Continue(slot)
//   Seq(spec)  : 仮想入力の手順を実時間で流し、状態を記録する (SeqLog で読む)。spec は ; 区切り:
//                x=<横> v=<縦> hold=jump rel=jump tap=jump|interact|attack w=<秒> mark=<名前> dmg (左から 1 ダメージ)
//   Dialogue() : 宴の常駐状態 (DontDestroyOnLoad・起動済み・ロード中)
//   Rest() / SaveInfo(slot) / Kill() / Retry() / Shot(name) / Off()
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Batch6
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    static PlayerController P => UnityEngine.Object.FindAnyObjectByType<PlayerController>();

    static VirtualPlayerInput In
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

    public static string State()
    {
        Application.runInBackground = true;
        var p = P;
        if (p == null) return "no player " + Dialogue();
        var rb = p.GetComponent<Rigidbody2D>();
        return $"stage={StageLoader.Instance?.CurrentStageName} pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) feet={p.FeetY:F2} " +
               $"v=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) st={p.CurrentStateName} ladder={p.IsOnLadder} gr={p.IsGrounded} " +
               $"canTalk={p.CanInteractNow} talk={DialogueService.IsPlaying} t={Time.unscaledTime:F2}";
    }

    public static string Dialogue()
    {
        var c = UnityEngine.Object.FindFirstObjectByType<UtageDialogueScene>(FindObjectsInactive.Include);
        var dlgScene = SceneManager.GetSceneByName(DialogueService.SceneName);
        var es = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        return $"dialogue: controller={(c != null ? c.gameObject.scene.name : "none")} ready={DialogueService.IsReady} loading={DialogueService.IsLoading} " +
               $"playing={DialogueService.IsPlaying} sceneLoaded={dlgScene.IsValid() && dlgScene.isLoaded} eventSystems={es.Length} " +
               $"[{string.Join(",", es.Select(e => e.gameObject.scene.name))}] boot={AppDomain.CurrentDomain.GetData("b6_boot")}";
    }

    // 起動からの宴の起動時間を測る (Play 直後に呼ぶ)
    class BootTimer : MonoBehaviour
    {
        float _t0;
        void Awake() { _t0 = Time.realtimeSinceStartup; DontDestroyOnLoad(gameObject); }
        void Update()
        {
            if (!DialogueService.IsReady) return;
            AppDomain.CurrentDomain.SetData("b6_boot", $"ready +{Time.realtimeSinceStartup - _t0:F2}s (since timer) frame={Time.frameCount} realtime={Time.realtimeSinceStartup:F2}");
            Destroy(gameObject);
        }
    }

    public static string TimeBoot()
    {
        AppDomain.CurrentDomain.SetData("b6_boot", "waiting");
        new GameObject("B6BootTimer").AddComponent<BootTimer>();
        return "timing " + Dialogue();
    }

    public static string Start(string stage)
    {
        Application.runInBackground = true;
        GameSession.PendingLoad = null;
        GameSession.SaveBelongsToRun = false;
        GameSession.RunSlotChosen = false;
        GameProgress.Clear();
        GameSession.ClearRespawnBench();
        StageLoader.LoadWithPlayerScene(stage);
        return "start " + stage;
    }

    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }
    public static string Tp(float x, float y) { P.Teleport(new Vector2(x, y)); return "tp " + State(); }
    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Skill(string s) { P.Progression.GrantSkill((PlayerSkill)Enum.Parse(typeof(PlayerSkill), s)); return "skill " + s; }

    public static string Title()
    {
        GamePause.Reset();
        SceneManager.LoadScene("TitleScene");
        return "title";
    }

    public static string TitlePick(string method, int slot)
    {
        var title = UnityEngine.Object.FindFirstObjectByType<TitleScreen>();
        var slots = UnityEngine.Object.FindFirstObjectByType<SaveSlotView>();
        if (title == null || slots == null) return "no title/slots";
        typeof(TitleScreen).GetMethod(method, F).Invoke(title, null);
        var chosen = typeof(SaveSlotView).GetField("_onChosen", F).GetValue(slots) as Action<int>;
        if (chosen == null) return "no callback";
        chosen(slot - 1);
        return $"{method} slot{slot}";
    }

    public static string Rest()
    {
        var sp = UnityEngine.Object.FindAnyObjectByType<SavePoint>();
        var home = UnityEngine.Object.FindFirstObjectByType<HomeUIView>();
        if (sp == null || home == null) return "no savepoint/home";
        home.Open(P.gameObject, sp);
        typeof(HomeUIView).GetMethod("CloseMenu", F).Invoke(home, null);
        return $"rested at {sp.transform.position} respawn={GameSession.RespawnScene} belongs={GameSession.SaveBelongsToRun} chosen={GameSession.RunSlotChosen} playTime={GameSession.PlayTimeSeconds:F1}";
    }

    public static string SaveInfo(int slot)
    {
        var path = SaveSystem.PathOf(slot - 1);
        if (!File.Exists(path)) return "no file " + slot;
        var d = SaveSystem.TryLoad(slot - 1);
        return $"save{slot}: scene={d.sceneName} pos=({d.posX:F2},{d.posY:F2}) place={d.placeName} savedAt={d.savedAt} playTime={d.playTimeSeconds:F1} " +
               $"md5={BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant()}";
    }

    public static string Kill()
    {
        var p = P;
        typeof(PlayerHealth).GetField("_invincibleTimer", F)?.SetValue(p.GetComponent<PlayerHealth>(), 0f);
        p.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo(999, 0, p.transform.position, null, false, 0f));
        return "kill " + State();
    }

    public static string Retry()
    {
        var go = UnityEngine.Object.FindFirstObjectByType<GameOverView>();
        if (go == null) return "no GameOverView";
        typeof(GameOverView).GetMethod("Retry", F).Invoke(go, null);
        return "retry";
    }

    public static string Shot(string name)
    {
        Directory.CreateDirectory("Assets/Screenshots/Area1");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/Area1/{name}.png");
        return "shot " + name;
    }

    public static string Off()
    {
        PlayerInputSource.Override = null;
        return "off";
    }

    // ------------------------------------------------------------------ 手順の実行

    class Runner : MonoBehaviour
    {
        public string[] Steps;
        public StringBuilder Log = new StringBuilder();

        public IEnumerator Run()
        {
            var input = In;
            input.ReleaseAll();
            foreach (var raw in Steps)
            {
                var step = raw.Trim();
                if (step.Length == 0) continue;
                var kv = step.Split('=');
                var key = kv[0];
                var val = kv.Length > 1 ? kv[1] : "";
                switch (key)
                {
                    case "x": input.Move = float.Parse(val, CultureInfo.InvariantCulture); break;
                    case "v": input.Vertical = float.Parse(val, CultureInfo.InvariantCulture); break;
                    case "hold": input.Hold(Button(val)); break;
                    case "rel": input.Release(Button(val)); break;
                    case "tap": input.Tap(Button(val)); break;
                    case "mark": Log.AppendLine($"[{val}] {State()}"); break;
                    case "tp":
                    {
                        var xy = val.Split(',');
                        P.Teleport(new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture)));
                        break;
                    }
                    case "dmg":
                    {
                        var p = P;
                        typeof(PlayerHealth).GetField("_invincibleTimer", F)?.SetValue(p.GetComponent<PlayerHealth>(), 0f);
                        p.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo(1, 0, (Vector2)p.transform.position + Vector2.left, null, false, 0f));
                        Log.AppendLine("[dmg] " + State());
                        break;
                    }
                    case "w":
                    {
                        var end = Time.realtimeSinceStartup + float.Parse(val, CultureInfo.InvariantCulture);
                        var next = 0f;
                        while (Time.realtimeSinceStartup < end)
                        {
                            if (Time.realtimeSinceStartup >= next && val != "0" && float.Parse(val, CultureInfo.InvariantCulture) >= 0.3f)
                            {
                                Log.AppendLine("  " + State());
                                next = Time.realtimeSinceStartup + 0.25f;
                            }
                            yield return null;
                        }
                        break;
                    }
                }
            }

            input.ReleaseAll();
            Log.AppendLine("end " + State());
            AppDomain.CurrentDomain.SetData("b6_seq", Log.ToString());
            Destroy(gameObject);
        }

        static PlayerButton Button(string s) => s switch
        {
            "jump" => PlayerButton.Jump,
            "interact" => PlayerButton.Interact,
            "attack" => PlayerButton.Attack,
            "dash" => PlayerButton.Dash,
            _ => PlayerButton.None,
        };
    }

    public static string Seq(string spec)
    {
        AppDomain.CurrentDomain.SetData("b6_seq", "running");
        var r = new GameObject("B6Runner").AddComponent<Runner>();
        UnityEngine.Object.DontDestroyOnLoad(r.gameObject);
        r.Steps = spec.Split(';');
        r.StartCoroutine(r.Run());
        return "seq started " + State();
    }

    public static string SeqLog() => AppDomain.CurrentDomain.GetData("b6_seq") as string ?? "none";

    // ボット (ローカル方針, Jev なし) で区間を走らせる: label を含む Area1Critical の区間
    public static string Bot(string label, float speed)
    {
        var route = BotRoutes.Area1Critical;
        var i = Array.FindIndex(route, s => s.ToString().Contains(label));
        if (i < 0) return "no step " + label;
        PlayerInputSource.Override = null;
        TestPlayBot.Settings.UseJev = false;
        TestPlayBot.Settings.Speed = speed;
        TestPlayBot.Settings.Screenshots = false;
        TestPlayBot.Settings.StuckSeconds = 30f;
        TestPlayBot.Ensure().StartRoute(route, i, i, "batch6 " + label);
        return $"bot step {i} {route[i]}";
    }

    public static string BotStatus()
    {
        var b = TestPlayBot.Instance;
        if (b == null) return "no bot";
        return $"running={b.Running} status={b.Status}\n{(b.Running ? "" : b.Summary())}\n{State()}";
    }

    public static string BotDecisions(int last)
    {
        var b = TestPlayBot.Instance;
        if (b == null || b.Report == null) return "no report";
        var d = b.Report.Decisions;
        return string.Join("\n", d.Skip(Math.Max(0, d.Count - last)).Select(x =>
            $"{x.GameTime:F1} {x.Scene} ({x.Position.x:F1},{x.Position.y:F1}) seg={x.Segment} -> {x.Choice} [{x.Source}] = {x.Outcome}"));
    }

    public static string BotStop()
    {
        var b = TestPlayBot.Instance;
        if (b != null && b.Running) b.GetType().GetMethod("Stop", F)?.Invoke(b, new object[] { "batch6 stop" });
        TestPlaySpeedReset();
        return "stopped";
    }

    static void TestPlaySpeedReset() { Time.timeScale = 1f; }

    public static string Ladders()
    {
        return string.Join("\n", ClimbableLadder.Active.Select(l =>
            $"{l.name} x={l.CenterX:F2} y{l.Bottom:F2}..{l.Top:F2} top={(l.TopPlatform ? l.TopPlatform.name : "none")} scene={l.gameObject.scene.name}"));
    }

    public static string Find(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == name && g.scene.IsValid()).ToList();
        return name + ": " + (all.Count == 0 ? "none" : string.Join(", ", all.Select(g => $"{g.scene.name} active={g.activeInHierarchy} pos={g.transform.position}")));
    }
}
