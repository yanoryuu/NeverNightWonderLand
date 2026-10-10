// エリア名の表示 (初回だけ) と、Utage 会話のキー送りのプレイ確認 (2026-10-03, PendingChanges 7 / 会話のキー送り)。Play 中に run_script で呼ぶ。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_title.cs --entry PlayTitle.NewGame
//   NewGame          : 「はじめから」と同じ (PendingLoad 無し・GameProgress 全消去 → PlayerScene ごと A1_T1)
//   State            : 表示中のタイトル (StageUI の AreaTitle の enabled/alpha/文字)・フラグ・ステージ
//   Go(scene, entry) : 遷移
//   LoadSave(scene, withFlag) : セーブ復帰と同じ (Collect したデータの progressFlags にフラグを入れる/外す → PendingLoad → PlayerScene ごと)
//   Talk             : 近くの NpcTalkPoint に話しかける / Dialogue : 宴の再生状態と表示中の文
//   Key(key) / KeyUp : 実キーボードの押下/離す (InputSystem.QueueStateEvent)。例 Key("J") → KeyUp
//   VirtualTap       : テストプレイの仮想入力 (VirtualPlayerInput) で攻撃を 1 回タップ (ボットと同じ経路)
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PlayTitle
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const string Flag = StageInfo.AreaTitleFlagPrefix + "1";
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    public static string State()
    {
        Application.runInBackground = true;
        var s = $"stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} {Flag}={GameProgress.Has(Flag)} frame={Time.frameCount}";
        foreach (var v in Object.FindObjectsByType<AreaTitleView>(FindObjectsSortMode.None))
        {
            var label = v.GetType().GetField("_label", F).GetValue(v) as TMP_Text;
            s += $"\n AreaTitle[{v.gameObject.scene.name}] enabled={v.enabled} alpha={v.GetComponent<CanvasGroup>().alpha:F2} text={(label ? label.text : "-")}";
        }
        return s;
    }

    const string LogKey = "PlayTitleLog";

    /// <summary>タイトルの表示を毎フレーム見張って、ステージごとに「表示された文字と最大 alpha」を PlayerPrefs に記録する。Log で読む。</summary>
    public static string WatchOn()
    {
        PlayerPrefs.SetString(LogKey, "");
        Application.onBeforeRender -= Watch;
        Application.onBeforeRender += Watch;
        return "watch on";
    }

    public static string Log() => PlayerPrefs.GetString(LogKey, "(none)");

    public static string WatchOff()
    {
        Application.onBeforeRender -= Watch;
        PlayerPrefs.DeleteKey(LogKey);
        return "watch off";
    }

    static string _lastStage;
    static bool _shownInStage;

    static void Watch()
    {
        var stage = StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-";
        if (stage != _lastStage)
        {
            _lastStage = stage;
            _shownInStage = false;
            PlayerPrefs.SetString(LogKey, PlayerPrefs.GetString(LogKey, "") + $" [enter {stage}]");
        }
        foreach (var v in Object.FindObjectsByType<AreaTitleView>(FindObjectsSortMode.None))
        {
            if (_shownInStage || !v.enabled || v.GetComponent<CanvasGroup>().alpha < 0.5f) continue;
            _shownInStage = true;
            var label = v.GetType().GetField("_label", F).GetValue(v) as TMP_Text;
            PlayerPrefs.SetString(LogKey, PlayerPrefs.GetString(LogKey, "") + $" title shown: {(label ? label.text : "-")}");
        }
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

    public static string LoadSave(string scene, bool withFlag)
    {
        var data = P.GetComponent<PlayerSaveBridge>().Collect();
        data.sceneName = scene;
        var flags = (data.progressFlags ?? new string[0]).Where(f => f != Flag).ToList();
        if (withFlag) flags.Add(Flag);
        data.progressFlags = flags.ToArray();
        var sp = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None).FirstOrDefault();
        GameSession.PendingLoad = data;
        GameProgress.Clear();
        StageLoader.LoadWithPlayerScene(scene);
        return $"load {scene} withFlag={withFlag} flags=[{string.Join(",", data.progressFlags)}]";
    }

    public static string Talk()
    {
        var p = P;
        var n = Object.FindObjectsByType<NpcTalkPoint>(FindObjectsSortMode.None).OrderBy(t => Vector2.Distance(t.transform.position, p.transform.position)).FirstOrDefault();
        if (n == null) return "no NpcTalkPoint";
        p.Teleport(n.transform.position + Vector3.up * 0.6f);
        ((IInteractable)n).Interact(p.gameObject);
        return $"talk {n.name} | " + Dialogue();
    }

    public static string Dialogue()
    {
        var engine = Object.FindAnyObjectByType<Utage.AdvEngine>();
        var page = engine != null && engine.Page != null ? engine.Page.TextData?.OriginalText : null;
        return $"dialogue playing={DialogueService.IsPlaying} paused={GamePause.IsPaused} text={page ?? "-"}";
    }

    public static string Key(string key)
    {
        var k = (Key)System.Enum.Parse(typeof(Key), key);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(k));
        return "key down " + k;
    }

    public static string KeyUp()
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        return "key up | " + Dialogue();
    }

    static VirtualPlayerInput _virtual;

    public static string VirtualTap()
    {
        if (!(PlayerInputSource.Override is VirtualPlayerInput v))
        {
            v = new VirtualPlayerInput();
            PlayerInputSource.Override = v;
        }
        v.Tap(PlayerButton.Attack);
        return "virtual tap attack";
    }

    public static string VirtualOff()
    {
        PlayerInputSource.Override = null;
        return "virtual off";
    }
}
