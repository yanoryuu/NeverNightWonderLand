using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 開発用デバッグパネル (メニュー: NeverNight/Debug Panel)。プレイモード中のみ操作できる。
/// ステージワープ / 能力 (ハサミ強化・スキル) の付与と剥奪 / 無敵 / 回復 / 糸 /
/// 全攻撃解放 / 敵全滅 / 会話 (Utage) の再生 / 進行フラグの確認と操作をまとめる。
/// ランタイム側への影響は DebugCheats.Invincible の参照のみで、ビルドには含まれない。
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

    // 主要な進行フラグ (ボタンで即セットできるようにする)
    private static readonly string[] KnownFlags =
    {
        "MidBoss1", "UpperBoss", "LowerBoss",
        "SpeakerGate", "SpeakerUpper", "SpeakerLower",
        "BossDoor1", "BossDoor2", "BossDoor3",
        "SpireWall", "CarouselBoss",
    };

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

        EditorGUILayout.LabelField($"現在: {StageLoader.Instance.CurrentStageName}");

        foreach (var sceneName in EnumerateGameFieldScenes())
        {
            using (new EditorGUI.DisabledScope(sceneName == StageLoader.Instance.CurrentStageName))
            {
                if (GUILayout.Button(sceneName))
                    StageLoader.Instance.TransitionTo(sceneName);
            }
        }
    }

    private static IEnumerable<string> EnumerateGameFieldScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled && s.path.Contains("/GameField/"))
            .Select(s => Path.GetFileNameWithoutExtension(s.path));
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

        if (GUILayout.Button("全能力を付与"))
        {
            foreach (ScissorUpgrade upgrade in Enum.GetValues(typeof(ScissorUpgrade)))
                progression.Grant(upgrade);
            foreach (PlayerSkill skill in Enum.GetValues(typeof(PlayerSkill)))
                progression.GrantSkill(skill);
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
            {
                player.Health?.Heal(9999);
                player.HealGauge?.AddCharge(9999f);
            }

            if (GUILayout.Button("糸 +100"))
                player.Inventory?.AddThread(100);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全攻撃解放"))
            {
                var loadout = player.AttackLoadout;
                if (loadout != null)
                {
                    foreach (var attack in loadout.Catalog)
                        loadout.Unlock(attack);
                }
            }

            if (GUILayout.Button("シーン内の敵を全滅"))
            {
                foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    enemy.TakeDamage(new DamageInfo(9999, 9999, enemy.transform.position, player.gameObject));
            }
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
