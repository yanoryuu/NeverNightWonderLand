using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自動テストプレイボットのウィンドウ (メニュー: NeverNight/Test Play Bot)。
/// ルートの区間を選んで走らせる / シーンごとに「全入口 × 全出口」を確かめる。速度・無敵・Jev の有無などの設定、
/// 実行中の状態 (シーン・ゴール・最後の判断と確信度・実行中の手順)、結果のレポート (TestPlay/Reports/) を扱う。
/// 中身は TestPlayBot (実行時、エディタ/Development Build のみ)。設定は EditorPrefs に保存する。
/// </summary>
public class TestPlayBotWindow : EditorWindow
{
    private const string PrefsKey = "NeverNight.TestPlayBot.Settings";
    private const string PendingKey = "NeverNight.TestPlayBot.PendingStart";

    private enum Mode { Route, AllExits }

    private Vector2 _scroll;
    private Mode _mode;
    private int _from;
    private int _to = 3;
    private int _sceneIndex;
    private bool _allScenes;
    private bool _settingsFoldout = true;

    [MenuItem("NeverNight/Test Play Bot")]
    private static void Open()
    {
        var w = GetWindow<TestPlayBotWindow>("Test Play Bot");
        w.minSize = new Vector2(320f, 420f);
    }

    private void OnEnable()
    {
        LoadSettings();
        _mode = (Mode)EditorPrefs.GetInt(PrefsKey + ".mode", 0);
        _from = EditorPrefs.GetInt(PrefsKey + ".from", 0);
        _to = EditorPrefs.GetInt(PrefsKey + ".to", 3);
        _sceneIndex = EditorPrefs.GetInt(PrefsKey + ".scene", 0);
    }

    private void OnDisable() => SaveSettings();

    private void OnInspectorUpdate() => Repaint();

    private static void LoadSettings()
    {
        var json = EditorPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
            JsonUtility.FromJsonOverwrite(json, TestPlayBot.Settings);
    }

    private void SaveSettings()
    {
        EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(TestPlayBot.Settings));
        EditorPrefs.SetInt(PrefsKey + ".mode", (int)_mode);
        EditorPrefs.SetInt(PrefsKey + ".from", _from);
        EditorPrefs.SetInt(PrefsKey + ".to", _to);
        EditorPrefs.SetInt(PrefsKey + ".scene", _sceneIndex);
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        var bot = Application.isPlaying ? TestPlayBot.Instance : null;
        var running = bot != null && bot.Running;

        DrawKey(bot);
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(running))
        {
            _mode = (Mode)GUILayout.Toolbar((int)_mode, new[] { "ルート (クリティカルパス)", "全入口 × 全出口" });
            if (_mode == Mode.Route) DrawRoute();
            else DrawAllExits();
        }

        EditorGUILayout.Space();
        DrawSettings();
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (!running)
            {
                if (GUILayout.Button(Application.isPlaying ? "▶ 開始" : "▶ プレイして開始", GUILayout.Height(28f)))
                    StartOrEnterPlay();
            }
            else if (GUILayout.Button("■ 停止 (レポートを書く)", GUILayout.Height(28f)))
            {
                bot.Stop("manual");
            }
        }

        EditorGUILayout.Space();
        DrawStatus(bot);
        EditorGUILayout.EndScrollView();

        if (GUI.changed) SaveSettings();
    }

    private static void DrawKey(TestPlayBot bot)
    {
        var jev = bot != null ? bot.Jev : new JevClient();
        if (jev.HasKey)
        {
            EditorGUILayout.LabelField($"Jev の API キー: あり ({jev.KeySource})", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.HelpBox(JevClient.MissingKeyMessage, MessageType.Warning);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("キーファイルの場所を開く"))
            {
                var dir = Path.GetDirectoryName(JevClient.KeyFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                EditorUtility.RevealInFinder(dir);
            }

            if (GUILayout.Button("読み直す")) jev.ReloadKey();
        }
    }

    private void DrawRoute()
    {
        var route = BotRoutes.Area1Critical;
        var labels = route.Select((s, i) => $"{i:00} {s.Label}").ToArray();
        _from = Mathf.Clamp(EditorGUILayout.Popup("開始", _from, labels), 0, route.Length - 1);
        _to = Mathf.Clamp(EditorGUILayout.Popup("終了", _to, labels), _from, route.Length - 1);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("T1→T4", EditorStyles.miniButton)) { _from = 0; _to = 3; }
            if (GUILayout.Button("T1→H", EditorStyles.miniButton)) { _from = 0; _to = 4; }
            if (GUILayout.Button("U ルート", EditorStyles.miniButton)) { _from = 5; _to = 17; }
            if (GUILayout.Button("D ルート", EditorStyles.miniButton)) { _from = 19; _to = 25; }
            if (GUILayout.Button("C1→FP2", EditorStyles.miniButton)) { _from = 26; _to = route.Length - 1; }
            if (GUILayout.Button("全部", EditorStyles.miniButton)) { _from = 0; _to = route.Length - 1; }
        }

        var flags = BotRoutes.FlagsBefore(route, _from);
        var ups = BotRoutes.UpgradesBefore(route, _from);
        if (TestPlayBot.Settings.ApplyPresets && (flags.Count > 0 || ups.Count > 0))
            EditorGUILayout.LabelField($"開始時に付けるもの: {string.Join(", ", flags.Concat(ups.Select(u => "ハサミ " + u)))}",
                EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawAllExits()
    {
        var scenes = Area1Scenes();
        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox("Build Settings に Assets/Scenes/Area1/ のシーンがありません。", MessageType.Info);
            return;
        }

        _allScenes = EditorGUILayout.ToggleLeft("エリア1 の全シーン", _allScenes);
        using (new EditorGUI.DisabledScope(_allScenes))
            _sceneIndex = EditorGUILayout.Popup("シーン", Mathf.Clamp(_sceneIndex, 0, scenes.Length - 1), scenes);
        EditorGUILayout.LabelField("各入口へワープ → その入口から各出口 (遷移ゾーン・移動装置) へ向かう。フラグの要る扉は閉じたまま (届かなければ失敗として記録)。",
            EditorStyles.wordWrappedMiniLabel);
    }

    private static string[] Area1Scenes() =>
        EditorBuildSettings.scenes.Where(s => s.enabled && s.path.Contains("/Area1/"))
            .Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();

    private void DrawSettings()
    {
        _settingsFoldout = EditorGUILayout.Foldout(_settingsFoldout, "設定", true, EditorStyles.foldoutHeader);
        if (!_settingsFoldout) return;

        var s = TestPlayBot.Settings;
        // 速度 (対数スライダー)
        var log = Mathf.Log(Mathf.Clamp(s.Speed, 0.1f, 8f), 2f);
        var next = EditorGUILayout.Slider($"速度 ({s.Speed:0.##}x)", log, Mathf.Log(0.1f, 2f), Mathf.Log(8f, 2f));
        if (!Mathf.Approximately(next, log))
        {
            s.Speed = Mathf.Pow(2f, next);
            if (Application.isPlaying) TestPlaySpeed.SetScale(s.Speed);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var p in TestPlaySpeed.Presets)
            {
                if (GUILayout.Button($"{p:0.##}", EditorStyles.miniButton))
                {
                    s.Speed = p;
                    if (Application.isPlaying) TestPlaySpeed.SetScale(p);
                }
            }
        }

        var thinkOptions = new[] { "自動 (等倍以上は停止 / スローは 0.25 まで)", "停止", "0.1x", "0.25x" };
        var thinkIndex = s.ThinkSpeed < 0f ? 0 : s.ThinkSpeed == 0f ? 1 : s.ThinkSpeed <= 0.1f ? 2 : 3;
        thinkIndex = EditorGUILayout.Popup("Jev の応答待ち", thinkIndex, thinkOptions);
        s.ThinkSpeed = thinkIndex switch { 0 => -1f, 1 => 0f, 2 => 0.1f, _ => 0.25f };

        s.Invincible = EditorGUILayout.ToggleLeft("無敵", s.Invincible);
        s.UseJev = EditorGUILayout.ToggleLeft("Jev で判断する (オフ = プログラムの推奨だけで動く)", s.UseJev);
        s.AutoSkipSingle = EditorGUILayout.ToggleLeft("選択肢が 1 つなら Jev に聞かない (費用の節約)", s.AutoSkipSingle);
        s.Objectives = EditorGUILayout.ToggleLeft("ボス・スピーカー・ピックアップの目的をこなす", s.Objectives);
        s.ApplyPresets = EditorGUILayout.ToggleLeft("途中から始める時に前の区間のフラグ・能力を付ける", s.ApplyPresets);
        s.ContinueOnFail = EditorGUILayout.ToggleLeft("失敗した区間はワープで次へ進む", s.ContinueOnFail);
        s.Screenshots = EditorGUILayout.ToggleLeft("止まった/迷った場所のスクリーンショット", s.Screenshots);
        s.StuckSeconds = EditorGUILayout.Slider("止まり判定 (ゲーム秒)", s.StuckSeconds, 10f, 120f);
        s.HopTimeout = EditorGUILayout.Slider("区間の上限 (ゲーム秒)", s.HopTimeout, 60f, 900f);
        s.LowConfidence = EditorGUILayout.Slider("迷いとみなす確信度", s.LowConfidence, 0.1f, 0.9f);
    }

    private void StartOrEnterPlay()
    {
        SaveSettings();
        if (!Application.isPlaying)
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
            return;
        }

        StartNow();
    }

    /// <summary>今の選択で開始する (プレイ中のみ)。</summary>
    public void StartNow()
    {
        LoadSettings();
        Application.runInBackground = true;
        var bot = TestPlayBot.Ensure();
        TestPlaySpeed.SetScale(TestPlayBot.Settings.Speed);
        if (_mode == Mode.Route)
        {
            var route = BotRoutes.Area1Critical;
            bot.StartRoute(route, _from, _to, $"Area1_{route[_from].Scene}-{route[_to].Scene}_x{TestPlayBot.Settings.Speed:0.##}");
        }
        else
        {
            var scenes = Area1Scenes();
            if (scenes.Length == 0) return;
            var list = _allScenes ? scenes : new[] { scenes[Mathf.Clamp(_sceneIndex, 0, scenes.Length - 1)] };
            bot.StartAllExits(list, _allScenes ? "AllExits_Area1" : "AllExits_" + list[0]);
        }
    }

    [InitializeOnLoadMethod]
    private static void HookPendingStart()
    {
        EditorApplication.update += () =>
        {
            if (!Application.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
            if (TestPlayActions.Player == null || TestPlayActions.IsLoading) return;
            SessionState.SetBool(PendingKey, false);
            var w = HasOpenInstances<TestPlayBotWindow>() ? GetWindow<TestPlayBotWindow>() : CreateInstance<TestPlayBotWindow>();
            w.OnEnable();
            w.StartNow();
        };
    }

    private static void DrawStatus(TestPlayBot bot)
    {
        EditorGUILayout.LabelField("状態", EditorStyles.boldLabel);
        if (bot == null)
        {
            EditorGUILayout.LabelField(Application.isPlaying ? "待機中" : "プレイモードで動きます", EditorStyles.miniLabel);
            return;
        }

        var st = TestPlayActions.GetPlayerState();
        EditorGUILayout.LabelField($"{bot.Status}", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField($"区間 {Mathf.Min(bot.StepIndex + 1, bot.StepCount)}/{bot.StepCount}: {bot.CurrentStepLabel}", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField($"シーン {st.Scene}  位置 ({st.Position.x:F1}, {st.Position.y:F1})  {st.State}  HP {st.Hp}/{st.MaxHp}", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField($"ゴール: {bot.GoalText}", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField($"実行中: {bot.ExecutorStep}", EditorStyles.wordWrappedMiniLabel);

        var d = bot.LastDecision;
        if (d != null)
        {
            var conf = d.Confidence >= 0f ? $"確信度 {d.Confidence:0.00}" : d.Source;
            EditorGUILayout.LabelField($"最後の判断: {d.Choice} ({conf})", EditorStyles.wordWrappedMiniLabel);
            if (d.TopProbabilities.Count > 0)
                EditorGUILayout.LabelField("  " + d.ProbText(), EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(d.Outcome))
                EditorGUILayout.LabelField("  結果: " + d.Outcome, EditorStyles.wordWrappedMiniLabel);
        }

        var jev = bot.Jev;
        EditorGUILayout.LabelField($"Jev: {jev.Calls} 回 / トークン {jev.InputTokens}+{jev.OutputTokens} / 失敗 {jev.Failures} / 平均 {(jev.Calls > 0 ? jev.TotalLatency / jev.Calls : 0f):0.00} 秒" +
                                   (string.IsNullOrEmpty(jev.LastError) ? "" : $" / 直近のエラー {jev.LastError}"), EditorStyles.wordWrappedMiniLabel);

        if (!string.IsNullOrEmpty(bot.LastReportPath))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("レポート: " + Path.GetFileName(bot.LastReportPath), EditorStyles.miniLabel);
                if (GUILayout.Button("開く", EditorStyles.miniButton, GUILayout.Width(40f)))
                    EditorUtility.OpenWithDefaultApp(bot.LastReportPath);
                if (GUILayout.Button("場所", EditorStyles.miniButton, GUILayout.Width(40f)))
                    EditorUtility.RevealInFinder(bot.LastReportPath);
            }
        }
    }
}
