#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// テストプレイ用の実行時オーバーレイ (エディタ/Development Build のみ。F1 / パッド R3 長押しで開閉)。
/// ゲームの UI は「シーンに置く」規約だが、これはリリースに含めない開発用の表示なので、
/// シーン・プレハブを一切増やさずに済む IMGUI (OnGUI) で描く。timeScale 0 でも動く。
/// 操作の中身は TestPlayActions / TestPlaySpeed / TestPlayRecorder (デバッグパネルと共用)。
/// </summary>
public class TestPlayOverlay : MonoBehaviour
{
    /// <summary>オーバーレイを表示するか。</summary>
    public static bool Visible;

    private enum Tab { Speed, Warp, Ability, Record, Bot }

    private static readonly string[] TabNames = { "速度", "ワープ", "能力/フラグ", "記録", "ボット" };

    private Tab _tab;
    private Rect _window = new(10f, 120f, 480f, 480f); // HUD (左上) の下に出す
    private Vector2 _scroll;
    private string _selectedScene;
    private bool _showOtherScenes;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;
    private bool _restartOnRecord = true;
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;

    private const float BaseHeight = 720f;

    private void Update()
    {
        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }

        // 表示中はマウスで操作できるようにカーソルを出す (閉じたら元に戻す)
        if (Visible && !Cursor.visible)
        {
            _prevCursorVisible = Cursor.visible;
            _prevCursorLock = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _restoreCursor = true;
        }
        else if (!Visible && _restoreCursor)
        {
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevCursorLock;
            _restoreCursor = false;
        }
    }

    private bool _restoreCursor;
    private GUIStyle _windowStyle;

    /// <summary>後ろの HUD が透けて読みにくいので、ほぼ不透明の背景にする。</summary>
    private static GUIStyle CreateWindowStyle()
    {
        var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        tex.SetPixel(0, 0, new Color(0.07f, 0.07f, 0.1f, 0.94f));
        tex.Apply();
        var style = new GUIStyle(GUI.skin.window);
        style.normal.background = tex;
        style.onNormal.background = tex;
        return style;
    }
    private string[] _recordingList;
    private float _recordingListTime;

    private void OnGUI()
    {
        var scale = Mathf.Max(Screen.height / BaseHeight, 0.75f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        DrawBadge(scale);

        if (!Visible)
            return;

        _window.height = Mathf.Min(_window.height, Screen.height / scale - 20f);
        _windowStyle ??= CreateWindowStyle();
        _window = GUILayout.Window(0x7E57, _window, DrawWindow, "テストプレイ (F1 で閉じる)", _windowStyle);
    }

    /// <summary>閉じていても速度・停止・記録中であることは画面の隅に出す (気付かずに遊び続けないように)。</summary>
    private void DrawBadge(float scale)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!Mathf.Approximately(TestPlaySpeed.Scale, 1f)) parts.Add($"{TestPlaySpeed.Scale:0.##}x");
        if (TestPlaySpeed.Frozen) parts.Add("停止中 (F5 再開 / F6 コマ送り)");
        if (TestPlayRecorder.IsBusy) parts.Add(TestPlayRecorder.Status);
        if (DebugCheats.Invincible) parts.Add("無敵");
        if (parts.Count == 0)
            return;

        var text = "TEST " + string.Join(" | ", parts);
        var width = Screen.width / scale;
        var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleRight, fontSize = 14 };
        style.normal.textColor = Color.yellow;
        var size = style.CalcSize(new GUIContent(text));
        var height = Screen.height / scale;
        GUI.Box(new Rect(width - size.x - 10f, height - size.y - 10f, size.x, size.y), text, style);
    }

    private void DrawWindow(int id)
    {
        DrawStatus();
        _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames);

        _scroll = GUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case Tab.Speed: DrawSpeed(); break;
            case Tab.Warp: DrawWarp(); break;
            case Tab.Ability: DrawAbility(); break;
            case Tab.Record: DrawRecord(); break;
            case Tab.Bot: DrawBot(); break;
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    private void DrawStatus()
    {
        var s = TestPlayActions.GetPlayerState();
        GUILayout.Label($"FPS {_fps:F0}   速度 {TestPlaySpeed.Scale:0.##}x{(TestPlaySpeed.Frozen ? " (停止)" : "")}   timeScale {Time.timeScale:0.##}");
        GUILayout.Label($"シーン {s.Scene}{(s.Loading ? " (読み込み中)" : "")}");
        if (s.Exists)
        {
            GUILayout.Label($"位置 ({s.Position.x:F2}, {s.Position.y:F2})  速度 ({s.Velocity.x:F1}, {s.Velocity.y:F1})");
            GUILayout.Label($"接地 {(s.Grounded ? "○" : "×")}  {s.State}  HP {s.Hp}/{s.MaxHp}  糸 {s.Thread}");
        }

        if (TestPlayRecorder.IsBusy)
            GUILayout.Label(TestPlayRecorder.Status);
    }

    // ------------------------------------------------------------------ 速度

    private void DrawSpeed()
    {
        GUILayout.Label("速度 (F2 遅く / F3 速く / F4 等倍)");
        GUILayout.BeginHorizontal();
        foreach (var preset in TestPlaySpeed.Presets)
        {
            var label = Mathf.Approximately(preset, TestPlaySpeed.Scale) ? $"[{preset:0.##}]" : $"{preset:0.##}";
            if (GUILayout.Button(label))
                TestPlaySpeed.SetScale(preset);
        }

        GUILayout.EndHorizontal();

        // 対数スライダー (0.1〜8)
        var log = Mathf.Log(TestPlaySpeed.Scale, 2f);
        var next = GUILayout.HorizontalSlider(log, Mathf.Log(TestPlaySpeed.Min, 2f), Mathf.Log(TestPlaySpeed.Max, 2f));
        if (!Mathf.Approximately(next, log))
            TestPlaySpeed.SetScale(Mathf.Pow(2f, next));

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(TestPlaySpeed.Frozen ? "再開 (F5)" : "一時停止 (F5)"))
            TestPlaySpeed.ToggleFrozen();
        using (new DisabledScope(!TestPlaySpeed.Frozen))
        {
            if (GUILayout.Button("コマ送り (F6)")) TestPlaySpeed.Step();
            if (GUILayout.Button("10 コマ")) TestPlaySpeed.Step(10);
        }

        GUILayout.EndHorizontal();

        TestPlaySpeed.SmoothSlowMotion = GUILayout.Toggle(TestPlaySpeed.SmoothSlowMotion,
            "スロー時は物理の刻みも細かく (滑らか。挙動わずかに変化)");
    }

    // ------------------------------------------------------------------ ワープ

    private void DrawWarp()
    {
        GUILayout.Label("このシーンの入口へ移動");
        foreach (var spawn in TestPlayActions.CurrentSpawns())
        {
            var id = string.IsNullOrEmpty(spawn.Id) ? "(デフォルト)" : spawn.Id;
            if (GUILayout.Button($"{id}  ({spawn.transform.position.x:F1}, {spawn.transform.position.y:F1})"))
                TestPlayActions.Teleport(spawn.transform.position);
        }

        GUILayout.Space(6f);
        GUILayout.Label("エリア1 へワープ (シーン → 入口)");
        DrawSceneButtons(TestPlayActions.Area1Scenes(), 4);

        _showOtherScenes = GUILayout.Toggle(_showOtherScenes, "その他のシーン");
        if (_showOtherScenes)
            DrawSceneButtons(TestPlayActions.StageScenes().Where(s => !s.StartsWith("A1_")).ToList(), 2);

        if (string.IsNullOrEmpty(_selectedScene))
            return;

        GUILayout.Space(6f);
        GUILayout.Label($"{_selectedScene} の入口");
        var ids = TestPlayActions.SpawnIds(_selectedScene);
        if (GUILayout.Button("デフォルト入口"))
            TestPlayActions.Warp(_selectedScene);
        foreach (var id in ids.Where(i => !string.IsNullOrEmpty(i)))
        {
            if (GUILayout.Button(id))
                TestPlayActions.Warp(_selectedScene, id);
        }

        if (ids.Length == 0)
            GUILayout.Label("(入口の一覧はエディタでのみ表示。ワープ後に上の一覧から選べます)");
    }

    private void DrawSceneButtons(System.Collections.Generic.List<string> scenes, int columns)
    {
        var current = TestPlayActions.CurrentStageName;
        for (var i = 0; i < scenes.Count; i += columns)
        {
            GUILayout.BeginHorizontal();
            for (var j = i; j < Mathf.Min(i + columns, scenes.Count); j++)
            {
                var name = scenes[j];
                var label = name == current ? $"*{name}" : name == _selectedScene ? $"[{name}]" : name;
                if (GUILayout.Button(label))
                    _selectedScene = name;
            }

            GUILayout.EndHorizontal();
        }
    }

    // ------------------------------------------------------------------ 能力・フラグ

    private void DrawAbility()
    {
        TestPlayActions.Invincible = GUILayout.Toggle(TestPlayActions.Invincible, "無敵 (被ダメージ 0)");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("HP/ゲージ回復")) TestPlayActions.RefillHealth();
        if (GUILayout.Button("糸 +100")) TestPlayActions.AddThread(100);
        if (GUILayout.Button("敵を全滅")) TestPlayActions.KillAllEnemies();
        GUILayout.EndHorizontal();
        if (GUILayout.Button("全攻撃解放")) TestPlayActions.UnlockAllAttacks();

        GUILayout.Space(6f);
        GUILayout.Label("ハサミ強化");
        GUILayout.BeginHorizontal();
        foreach (ScissorUpgrade upgrade in Enum.GetValues(typeof(ScissorUpgrade)))
        {
            var has = TestPlayActions.HasUpgrade(upgrade);
            var next = GUILayout.Toggle(has, upgrade.ToString());
            if (next != has) TestPlayActions.SetUpgrade(upgrade, next);
        }

        GUILayout.EndHorizontal();

        GUILayout.Label("移動スキル");
        GUILayout.BeginHorizontal();
        foreach (PlayerSkill skill in Enum.GetValues(typeof(PlayerSkill)))
        {
            var has = TestPlayActions.HasSkill(skill);
            var next = GUILayout.Toggle(has, skill.ToString());
            if (next != has) TestPlayActions.SetSkill(skill, next);
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("全能力 付与")) TestPlayActions.SetAllAbilities(true);
        if (GUILayout.Button("全能力 剥奪")) TestPlayActions.SetAllAbilities(false);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("進行フラグのプリセット (足すだけ)");
        foreach (var preset in TestPlayActions.FlagPresets)
        {
            if (GUILayout.Button(preset.Name))
                TestPlayActions.ApplyPreset(preset);
        }

        if (GUILayout.Button("エリア1 のフラグを全部外す"))
            TestPlayActions.ClearArea1Flags();

        var flags = GameProgress.Collect().OrderBy(f => f).ToArray();
        GUILayout.Label($"現在のフラグ ({flags.Length})");
        foreach (var flag in flags)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(flag);
            if (GUILayout.Button("外す", GUILayout.Width(50f)))
                GameProgress.Unset(flag);
            GUILayout.EndHorizontal();
        }
    }

    // ------------------------------------------------------------------ 記録

    private void DrawRecord()
    {
        _restartOnRecord = GUILayout.Toggle(_restartOnRecord, "記録開始時にステージを読み直す (再生と条件をそろえる)");

        GUILayout.BeginHorizontal();
        using (new DisabledScope(TestPlayRecorder.IsBusy && !TestPlayRecorder.IsRecording))
        {
            if (GUILayout.Button(TestPlayRecorder.IsRecording ? "■ 記録を止めて保存 (F7)" : "● 記録開始 (F7)"))
            {
                if (TestPlayRecorder.IsRecording) TestPlayRecorder.StopRecording();
                else TestPlayRecorder.StartRecording(_restartOnRecord);
            }
        }

        using (new DisabledScope(!TestPlayRecorder.IsReplaying))
        {
            if (GUILayout.Button("再生を止める (F8)"))
                TestPlayRecorder.StopReplay();
        }

        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(TestPlayRecorder.LastSavedPath))
            GUILayout.Label($"保存: {Path.GetFileName(TestPlayRecorder.LastSavedPath)}");

        var report = TestPlayRecorder.LastReport;
        if (report != null)
            GUILayout.Label(report.ToString());

        GUILayout.Space(6f);
        GUILayout.Label("記録ファイル (クリックで再生。速度は「速度」タブの倍率)");
        using (new DisabledScope(TestPlayRecorder.IsBusy))
        {
            if (_recordingList == null || Time.unscaledTime - _recordingListTime > 2f)
            {
                _recordingList = TestPlayRecorder.ListRecordings().Take(20).ToArray();
                _recordingListTime = Time.unscaledTime;
            }

            foreach (var path in _recordingList)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(path)))
                    TestPlayRecorder.StartReplay(path);
            }
        }
    }

    // ------------------------------------------------------------------ ボット

    private int _botFrom;
    private int _botTo = 3;

    private void DrawBot()
    {
        var bot = TestPlayBot.Instance;
        var running = bot != null && bot.Running;
        var route = BotRoutes.Area1Critical;
        var s = TestPlayBot.Settings;

        using (new DisabledScope(running))
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("開始", GUILayout.Width(36f));
            if (GUILayout.Button("<", GUILayout.Width(24f))) _botFrom = Mathf.Max(0, _botFrom - 1);
            if (GUILayout.Button(">", GUILayout.Width(24f))) _botFrom = Mathf.Min(route.Length - 1, _botFrom + 1);
            GUILayout.Label(route[_botFrom].Label);
            GUILayout.EndHorizontal();
            _botTo = Mathf.Clamp(_botTo, _botFrom, route.Length - 1);
            GUILayout.BeginHorizontal();
            GUILayout.Label("終了", GUILayout.Width(36f));
            if (GUILayout.Button("<", GUILayout.Width(24f))) _botTo = Mathf.Max(_botFrom, _botTo - 1);
            if (GUILayout.Button(">", GUILayout.Width(24f))) _botTo = Mathf.Min(route.Length - 1, _botTo + 1);
            GUILayout.Label(route[_botTo].Label);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("T1→T4")) { _botFrom = 0; _botTo = 3; }
            if (GUILayout.Button("U")) { _botFrom = 5; _botTo = 17; }
            if (GUILayout.Button("D")) { _botFrom = 19; _botTo = 25; }
            if (GUILayout.Button("全部")) { _botFrom = 0; _botTo = route.Length - 1; }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("速度", GUILayout.Width(36f));
        foreach (var preset in TestPlaySpeed.Presets)
        {
            var label = Mathf.Approximately(preset, TestPlaySpeed.Scale) ? $"[{preset:0.##}]" : $"{preset:0.##}";
            if (GUILayout.Button(label)) { s.Speed = preset; TestPlaySpeed.SetScale(preset); }
        }

        GUILayout.EndHorizontal();
        s.Invincible = GUILayout.Toggle(s.Invincible, "無敵");
        if (running) TestPlayActions.Invincible = s.Invincible;
        s.UseJev = GUILayout.Toggle(s.UseJev, "Jev で判断 (オフ = プログラムの推奨だけ)");

        if (!running)
        {
            if (GUILayout.Button("▶ ボット開始"))
                TestPlayBot.Ensure().StartRoute(route, _botFrom, _botTo, $"Area1_{route[_botFrom].Scene}-{route[_botTo].Scene}_x{s.Speed:0.##}");
        }
        else if (GUILayout.Button("■ 停止 (レポートを書く)"))
        {
            bot.Stop("manual");
        }

        if (bot == null) return;
        GUILayout.Label(bot.Status);
        GUILayout.Label($"区間 {Mathf.Min(bot.StepIndex + 1, bot.StepCount)}/{bot.StepCount}: {bot.CurrentStepLabel}");
        GUILayout.Label("ゴール: " + bot.GoalText);
        GUILayout.Label("実行中: " + bot.ExecutorStep);
        var d = bot.LastDecision;
        if (d != null)
            GUILayout.Label($"判断: {d.Choice} ({(d.Confidence >= 0f ? "確信度 " + d.Confidence.ToString("0.00") : d.Source)})");
        GUILayout.Label($"Jev {bot.Jev.Calls} 回 / トークン {bot.Jev.InputTokens + bot.Jev.OutputTokens}" + (bot.Jev.HasKey ? "" : " (キー無し: ローカル方針)"));
        if (!string.IsNullOrEmpty(bot.LastReportPath))
            GUILayout.Label("レポート: " + Path.GetFileName(bot.LastReportPath));
    }

    /// <summary>IMGUI の GUI.enabled を一時的に切り替える。</summary>
    private readonly struct DisabledScope : IDisposable
    {
        private readonly bool _previous;

        public DisabledScope(bool disabled)
        {
            _previous = GUI.enabled;
            GUI.enabled = _previous && !disabled;
        }

        public void Dispose() => GUI.enabled = _previous;
    }
}
#endif
