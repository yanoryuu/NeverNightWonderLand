using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 開発用デバッグパネル (メニュー: NeverNight/Debug Panel)。プレイモード中のみ操作できる。
/// ステージワープ / 能力 (ハサミ強化・スキル) の付与と剥奪 / 無敵 / 回復 / 糸 /
/// 全攻撃解放 / 敵全滅 / 会話 (Utage) の再生 / 進行フラグの確認と操作 /
/// テストプレイの速度調整と入力の記録・再生をまとめる。
/// 操作の中身は TestPlayActions / TestPlaySpeed / TestPlayRecorder (実行時オーバーレイ F1 と共用)。
/// </summary>
public class DebugPanelWindow : EditorWindow
{
    private Vector2 _scroll;
    private string _flagInput = "";
    private string _dialogueLabelInput = "";
    private bool _warpFoldout = true;
    private bool _abilityFoldout = true;
    private bool _cheatFoldout = true;
    private bool _dialogueFoldout = true;
    private bool _flagFoldout = true;
    private bool _speedFoldout = true;
    private bool _recordFoldout = true;
    private bool _restartOnRecord = true;
    private string _selectedWarpScene;

    // 主要な進行フラグ (ボタンで即セットできるようにする)。エリア1 は TestPlayActions.KnownFlags
    private static readonly string[] KnownFlags = TestPlayActions.KnownFlags.Concat(new[]
    {
        "MidBoss1", "UpperBoss", "LowerBoss",
        "SpeakerGate", "SpeakerUpper", "SpeakerLower",
        "BossDoor1", "BossDoor2", "BossDoor3",
        "SpireWall", "CarouselBoss",
    }).ToArray();

    [MenuItem("NeverNight/Debug Panel")]
    private static void Open()
    {
        var window = GetWindow<DebugPanelWindow>("Debug Panel");
        window.minSize = new Vector2(280f, 400f);
    }

    private void OnInspectorUpdate()
    {
        // プレイ中の状態変化 (フラグ・所持スキル等) を追従表示する
        Repaint();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("プレイモード中に操作できます。", MessageType.Info);
            return;
        }

        var player = FindFirstObjectByType<PlayerController>();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSpeedSection();
        EditorGUILayout.Space();
        DrawRecordSection();
        EditorGUILayout.Space();
        DrawWarpSection();
        EditorGUILayout.Space();
        DrawAbilitySection(player);
        EditorGUILayout.Space();
        DrawCheatSection(player);
        EditorGUILayout.Space();
        DrawDialogueSection();
        EditorGUILayout.Space();
        DrawFlagSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSpeedSection()
    {
        _speedFoldout = EditorGUILayout.Foldout(_speedFoldout,
            $"速度 ({TestPlaySpeed.Scale:0.##}x{(TestPlaySpeed.Frozen ? " 停止中" : "")})", true, EditorStyles.foldoutHeader);
        if (!_speedFoldout)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var preset in TestPlaySpeed.Presets)
            {
                var selected = Mathf.Approximately(preset, TestPlaySpeed.Scale);
                if (GUILayout.Toggle(selected, $"{preset:0.##}", EditorStyles.miniButton) && !selected)
                    TestPlaySpeed.SetScale(preset);
            }
        }

        var next = EditorGUILayout.Slider("倍率", TestPlaySpeed.Scale, TestPlaySpeed.Min, TestPlaySpeed.Max);
        if (!Mathf.Approximately(next, TestPlaySpeed.Scale))
            TestPlaySpeed.SetScale(next);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(TestPlaySpeed.Frozen ? "再開" : "一時停止"))
                TestPlaySpeed.ToggleFrozen();
            using (new EditorGUI.DisabledScope(!TestPlaySpeed.Frozen))
            {
                if (GUILayout.Button("コマ送り")) TestPlaySpeed.Step();
                if (GUILayout.Button("10 コマ")) TestPlaySpeed.Step(10);
            }
        }

        TestPlaySpeed.SmoothSlowMotion = EditorGUILayout.ToggleLeft(
            "スロー時は物理の刻みも細かく (滑らか。挙動はわずかに変わる)", TestPlaySpeed.SmoothSlowMotion);
        EditorGUILayout.LabelField("ゲーム中のキー: F1 オーバーレイ / F2・F3 速度 / F4 等倍 / F5 停止 / F6 コマ送り / F7 記録 / F8 再生",
            EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawRecordSection()
    {
        _recordFoldout = EditorGUILayout.Foldout(_recordFoldout, "入力の記録・再生", true, EditorStyles.foldoutHeader);
        if (!_recordFoldout)
            return;

        if (TestPlayRecorder.IsBusy)
            EditorGUILayout.HelpBox(TestPlayRecorder.Status, MessageType.Info);

        _restartOnRecord = EditorGUILayout.ToggleLeft("記録開始時にステージを読み直す (再生と条件をそろえる)", _restartOnRecord);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(TestPlayRecorder.IsBusy && !TestPlayRecorder.IsRecording))
            {
                if (GUILayout.Button(TestPlayRecorder.IsRecording ? "■ 記録を止めて保存" : "● 記録開始"))
                {
                    if (TestPlayRecorder.IsRecording) TestPlayRecorder.StopRecording();
                    else TestPlayRecorder.StartRecording(_restartOnRecord);
                }
            }

            using (new EditorGUI.DisabledScope(!TestPlayRecorder.IsReplaying))
            {
                if (GUILayout.Button("再生を止める"))
                    TestPlayRecorder.StopReplay();
            }
        }

        if (TestPlayRecorder.LastReport != null)
            EditorGUILayout.LabelField(TestPlayRecorder.LastReport.ToString(), EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.LabelField($"記録ファイル ({TestPlayRecorder.RecordingsDirectory})", EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(TestPlayRecorder.IsBusy))
        {
            foreach (var path in TestPlayRecorder.ListRecordings().Take(10))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(path));
                    if (GUILayout.Button("再生", GUILayout.Width(50f)))
                        TestPlayRecorder.StartReplay(path);
                }
            }
        }
    }

    private void DrawWarpSection()
    {
        _warpFoldout = EditorGUILayout.Foldout(_warpFoldout, "ステージワープ", true, EditorStyles.foldoutHeader);
        if (!_warpFoldout)
            return;

        if (StageLoader.Instance == null)
        {
            EditorGUILayout.HelpBox("StageLoader が見つかりません (PlayerScene 方式で起動していますか)。", MessageType.Warning);
            return;
        }

        var current = StageLoader.Instance.CurrentStageName;
        EditorGUILayout.LabelField($"現在: {current}");

        // シーンを選ぶと、その入口 (PlayerSpawnPoint の Id) を選べる
        var scenes = TestPlayActions.StageScenes();
        const int columns = 4;
        for (var i = 0; i < scenes.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var j = i; j < Mathf.Min(i + columns, scenes.Count); j++)
                {
                    var selected = scenes[j] == _selectedWarpScene;
                    if (GUILayout.Toggle(selected, scenes[j] == current ? $"*{scenes[j]}" : scenes[j], EditorStyles.miniButton) && !selected)
                        _selectedWarpScene = scenes[j];
                }
            }
        }

        if (string.IsNullOrEmpty(_selectedWarpScene))
            return;

        EditorGUILayout.LabelField($"{_selectedWarpScene} の入口", EditorStyles.miniBoldLabel);
        foreach (var id in TestPlayActions.SpawnIds(_selectedWarpScene))
        {
            if (GUILayout.Button(string.IsNullOrEmpty(id) ? "(デフォルト入口)" : id))
                TestPlayActions.Warp(_selectedWarpScene, id);
        }
    }

    private void DrawAbilitySection(PlayerController player)
    {
        _abilityFoldout = EditorGUILayout.Foldout(_abilityFoldout, "能力 (付与/剥奪)", true, EditorStyles.foldoutHeader);
        if (!_abilityFoldout)
            return;

        if (player == null || player.Progression == null)
        {
            EditorGUILayout.HelpBox("プレイヤーが見つかりません。", MessageType.Warning);
            return;
        }

        var progression = player.Progression;

        EditorGUILayout.LabelField("ハサミ強化", EditorStyles.miniBoldLabel);
        foreach (ScissorUpgrade upgrade in Enum.GetValues(typeof(ScissorUpgrade)))
        {
            var has = progression.Has(upgrade);
            var next = EditorGUILayout.ToggleLeft($"{upgrade.DisplayName()} ({upgrade})", has);
            if (next == has)
                continue;
            if (next) progression.Grant(upgrade);
            else progression.Revoke(upgrade);
        }

        EditorGUILayout.LabelField("移動スキル", EditorStyles.miniBoldLabel);
        foreach (PlayerSkill skill in Enum.GetValues(typeof(PlayerSkill)))
        {
            var has = progression.HasSkill(skill);
            var next = EditorGUILayout.ToggleLeft($"{skill.DisplayName()} ({skill})", has);
            if (next == has)
                continue;
            if (next) progression.GrantSkill(skill);
            else progression.RevokeSkill(skill);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全能力を付与")) TestPlayActions.SetAllAbilities(true);
            if (GUILayout.Button("全能力を剥奪")) TestPlayActions.SetAllAbilities(false);
        }

        EditorGUILayout.LabelField("鍛冶強化 (攻撃力+コンボ)", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(
                $"回数 {progression.ForgeLevel} / 最大コンボ {player.MaxCombo}", GUILayout.Width(160f));
            if (GUILayout.Button("-1", GUILayout.Width(36f)))
                progression.SetForgeLevel(progression.ForgeLevel - 1);
            if (GUILayout.Button("+1", GUILayout.Width(36f)))
                progression.AddForgeLevel();
        }
    }

    private void DrawCheatSection(PlayerController player)
    {
        _cheatFoldout = EditorGUILayout.Foldout(_cheatFoldout, "チート", true, EditorStyles.foldoutHeader);
        if (!_cheatFoldout)
            return;

        DebugCheats.Invincible = EditorGUILayout.ToggleLeft("無敵 (被ダメージ0)", DebugCheats.Invincible);

        if (player == null)
        {
            EditorGUILayout.HelpBox("プレイヤーが見つかりません。", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全回復 + ゲージMAX"))
                TestPlayActions.RefillHealth();

            if (GUILayout.Button("糸 +100"))
                TestPlayActions.AddThread(100);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全攻撃解放"))
                TestPlayActions.UnlockAllAttacks();

            if (GUILayout.Button("シーン内の敵を全滅"))
                TestPlayActions.KillAllEnemies();
        }
    }

    private void DrawDialogueSection()
    {
        _dialogueFoldout = EditorGUILayout.Foldout(_dialogueFoldout, "会話 (Utage)", true, EditorStyles.foldoutHeader);
        if (!_dialogueFoldout)
            return;

        var labels = DialogueLabelScanner.Scan();
        if (labels.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "シナリオラベルが見つかりません。\nAssets/Dialogue 内の .tsv/.xls にラベル (*ラベル名) を追加してください。",
                MessageType.Warning);
        }

        if (DialogueService.IsPlaying)
            EditorGUILayout.HelpBox("会話を再生中です。", MessageType.Info);

        using (new EditorGUI.DisabledScope(DialogueService.IsPlaying))
        {
            foreach (var label in labels)
            {
                if (GUILayout.Button(label))
                    DialogueService.Play(label);
            }

            // ラベル直接入力での再生 (Excel 保存直後などボタン一覧に無いものを試す用)
            using (new EditorGUILayout.HorizontalScope())
            {
                _dialogueLabelInput = EditorGUILayout.TextField(_dialogueLabelInput);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_dialogueLabelInput)))
                {
                    if (GUILayout.Button("再生", GUILayout.Width(60f)))
                        DialogueService.Play(_dialogueLabelInput.TrimStart('*').Trim());
                }
            }
        }

        if (GUILayout.Button("ラベル一覧を再読み込み"))
            DialogueLabelScanner.Scan(forceReload: true);
    }

    private void DrawFlagSection()
    {
        _flagFoldout = EditorGUILayout.Foldout(_flagFoldout, "進行フラグ (GameProgress)", true, EditorStyles.foldoutHeader);
        if (!_flagFoldout)
            return;

        var current = GameProgress.Collect();
        EditorGUILayout.LabelField($"現在のフラグ ({current.Length})", EditorStyles.miniBoldLabel);
        foreach (var flag in current.OrderBy(f => f))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(flag);
                if (GUILayout.Button("解除", GUILayout.Width(44f)))
                    GameProgress.Unset(flag);
            }
        }

        EditorGUILayout.LabelField("プリセット (足すだけ)", EditorStyles.miniBoldLabel);
        foreach (var preset in TestPlayActions.FlagPresets)
        {
            if (GUILayout.Button(preset.Name))
                TestPlayActions.ApplyPreset(preset);
        }

        if (GUILayout.Button("エリア1 のフラグを全部外す"))
            TestPlayActions.ClearArea1Flags();

        EditorGUILayout.LabelField("主要フラグ", EditorStyles.miniBoldLabel);
        foreach (var flag in KnownFlags)
        {
            if (GameProgress.Has(flag))
                continue;
            if (GUILayout.Button($"セット: {flag}"))
                GameProgress.Set(flag);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _flagInput = EditorGUILayout.TextField(_flagInput);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_flagInput)))
            {
                if (GUILayout.Button("セット", GUILayout.Width(60f)))
                {
                    GameProgress.Set(_flagInput);
                    _flagInput = "";
                }
            }
        }

        if (GUILayout.Button("フラグ全クリア"))
            GameProgress.Clear();
    }
}
