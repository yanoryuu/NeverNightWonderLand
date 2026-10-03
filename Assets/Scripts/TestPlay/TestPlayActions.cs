#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>テストプレイから読むプレイヤーの状態 (TestPlayActions.GetPlayerState)。</summary>
public struct TestPlayerState
{
    public bool Exists;
    public string Scene;
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Grounded;
    public int Facing;
    public string State;
    public int Hp;
    public int MaxHp;
    public int Thread;
    public bool Dead;
    public bool Paused;       // GamePause 中 (メニュー・会話・テストの一時停止)
    public bool Loading;      // ステージ読み込み中

    public override string ToString() => Exists
        ? $"{Scene} pos=({Position.x:F2},{Position.y:F2}) vel=({Velocity.x:F2},{Velocity.y:F2}) grounded={Grounded} " +
          $"state={State} hp={Hp}/{MaxHp} thread={Thread}{(Dead ? " DEAD" : "")}{(Paused ? " PAUSED" : "")}{(Loading ? " LOADING" : "")}"
        : "(player not found)";
}

/// <summary>
/// テストプレイの操作をまとめた静的 API (エディタ/Development Build のみ)。
/// 実行時オーバーレイ (TestPlayOverlay)・エディタのデバッグパネル (DebugPanelWindow)・自動テストプレイが共用する。
/// </summary>
public static class TestPlayActions
{
    /// <summary>進行フラグのプリセット (エリア1)。</summary>
    public readonly struct FlagPreset
    {
        public readonly string Name;
        public readonly string[] Flags;
        public readonly ScissorUpgrade[] Upgrades;

        public FlagPreset(string name, string[] flags, ScissorUpgrade[] upgrades = null)
        {
            Name = name;
            Flags = flags;
            Upgrades = upgrades ?? Array.Empty<ScissorUpgrade>();
        }
    }

    private static readonly string[] AfterBoss1 = { "A1_Boss1" };
    private static readonly string[] AfterMidBosses = { "A1_Boss1", "A1_MidBossUpper", "A1_MidBossLower" };

    private static readonly string[] AllSpeakers =
    {
        "A1_Boss1", "A1_MidBossUpper", "A1_MidBossLower",
        "A1_Speaker1", "A1_Speaker2", "A1_Speaker3", "A1_ZiplineReady", "A1_ElevatorDeep",
    };

    /// <summary>
    /// プリセットは「そこまで進んだ」状態を作るためのフラグの組 (足すだけ。外すのは ClearArea1Flags)。
    /// エリアボス撃破後は赤ハサミ (二段ジャンプ) も付与し、赤ハサミのピックアップを取得済みにする。
    /// </summary>
    public static readonly FlagPreset[] FlagPresets =
    {
        new("B1 改札の番人 撃破後", AfterBoss1),
        new("中ボス 2 体 撃破後", AfterMidBosses),
        new("スピーカー 3 つ 破壊後", AllSpeakers),
        new("エリアボス 撃破後", AllSpeakers.Concat(new[] { "A1_AreaBoss", "A1_Pickup_AB_1" }).ToArray(),
            new[] { ScissorUpgrade.Red }),
    };

    /// <summary>よく使う個別フラグ (デバッグパネルのボタン用)。</summary>
    public static readonly string[] KnownFlags =
    {
        "A1_Boss1", "A1_MidBossUpper", "A1_MidBossLower", "A1_AreaBoss",
        "A1_Speaker1", "A1_Speaker2", "A1_Speaker3", "A1_ZiplineReady", "A1_ElevatorDeep",
        "A1_LiftWheel", "A1_BeltReversed", "A1_LadderBackstage", "A1_FenceOpen", "A1_ArenaCups", "A1_RatNest",
    };

    // ---------------------------------------------------------------- プレイヤー

    private static PlayerController _player;

    /// <summary>現在のプレイヤー (無ければ null)。</summary>
    public static PlayerController Player
    {
        get
        {
            if (_player == null)
                _player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            return _player;
        }
    }

    /// <summary>現在のステージ名 (StageLoader が無ければアクティブシーン名)。</summary>
    public static string CurrentStageName =>
        StageLoader.Instance != null ? StageLoader.Instance.CurrentStageName : SceneManager.GetActiveScene().name;

    /// <summary>ステージを読み込み中か (遷移の暗転中を含む)。</summary>
    public static bool IsLoading => StageLoader.Instance != null && StageLoader.Instance.IsLoading;

    /// <summary>操作できる状態か (プレイヤーがいて、読み込み中でなく、ポーズしていない)。</summary>
    public static bool IsReady => Player != null && !IsLoading && !GamePause.IsPaused;

    public static TestPlayerState GetPlayerState()
    {
        var p = Player;
        if (p == null)
            return new TestPlayerState { Exists = false, Scene = CurrentStageName, Loading = IsLoading };

        return new TestPlayerState
        {
            Exists = true,
            Scene = CurrentStageName,
            Position = p.transform.position,
            Velocity = p.Rb != null ? p.Rb.linearVelocity : Vector2.zero,
            Grounded = p.IsGrounded,
            Facing = p.Facing,
            State = p.CurrentStateName,
            Hp = p.Health != null ? p.Health.Hp.CurrentValue : 0,
            MaxHp = p.Health != null ? p.Health.MaxHp : 0,
            Thread = p.Inventory != null ? p.Inventory.Thread.CurrentValue : 0,
            Dead = p.IsDead,
            Paused = GamePause.IsPaused,
            Loading = IsLoading,
        };
    }

    /// <summary>プレイヤーを指定位置へ移動し、カメラも即座に追従させる。</summary>
    public static void Teleport(Vector2 position)
    {
        var p = Player;
        if (p == null)
            return;

        p.Teleport(position);
        var follow = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
        if (follow != null)
            follow.SnapToTarget();
    }

    // ---------------------------------------------------------------- ワープ

    /// <summary>Build Settings のステージシーン名 (PlayerScene・タイトル・UI シーン・DialogueScene を除く)。folderFilter で絞る (例 "/Area1/")。</summary>
    public static List<string> StageScenes(string folderFilter = null)
    {
        var list = new List<string>();
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(path);
            if (!StageLoader.IsStageSceneName(name) || name.StartsWith("Title"))
                continue;
            if (!string.IsNullOrEmpty(folderFilter) && !path.Contains(folderFilter))
                continue;
            list.Add(name);
        }

        return list;
    }

    /// <summary>エリア1 のシーン (Assets/Scenes/Area1/)。</summary>
    public static List<string> Area1Scenes() => StageScenes("/Area1/");

    /// <summary>
    /// ステージへワープする。entranceId は遷移先の PlayerSpawnPoint の Id (null/空 = デフォルト入口)。
    /// 同じステージなら入口へテレポート。PlayerScene 方式で動いていない時 (タイトル等) は PlayerScene ごと読み直す。
    /// 読み込みは非同期 (完了は IsLoading / IsReady で待つ)。
    /// </summary>
    public static bool Warp(string sceneName, string entranceId = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        var loader = StageLoader.Instance;
        if (loader == null)
        {
            StageLoader.LoadWithPlayerScene(sceneName);
            return true;
        }

        if (sceneName == loader.CurrentStageName)
        {
            var spawn = CurrentSpawns().FirstOrDefault(s => s.Id == (entranceId ?? ""));
            if (spawn == null)
                return false;
            Teleport(spawn.transform.position);
            return true;
        }

        // 一時停止・メニュー中でも遷移できるよう止めを外す (遷移先の StageInfo も GamePause.Reset する)
        if (TestPlaySpeed.Frozen)
            TestPlaySpeed.SetFrozen(false);
        return loader.TransitionTo(sceneName, entranceId);
    }

    /// <summary>現在のステージにある入口 (PlayerSpawnPoint)。</summary>
    public static List<PlayerSpawnPoint> CurrentSpawns()
    {
        var result = new List<PlayerSpawnPoint>();
        var scene = SceneManager.GetSceneByName(CurrentStageName);
        if (!scene.IsValid() || !scene.isLoaded)
            return result;

        foreach (var root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<PlayerSpawnPoint>(true));
        return result;
    }

    private static Dictionary<string, string[]> _spawnIdCache;

    /// <summary>
    /// 指定ステージの入口 Id 一覧 ("" = デフォルト入口)。エディタではシーンファイルを読んで返す。
    /// Development Build では読み込み済みのステージ以外は分からないので空を返す (ワープ後に CurrentSpawns を使う)。
    /// </summary>
    public static string[] SpawnIds(string sceneName)
    {
        if (sceneName == CurrentStageName)
            return CurrentSpawns().Select(s => s.Id).ToArray();

#if UNITY_EDITOR
        _spawnIdCache ??= new Dictionary<string, string[]>();
        if (_spawnIdCache.TryGetValue(sceneName, out var cached))
            return cached;

        var ids = ScanSpawnIds(sceneName);
        _spawnIdCache[sceneName] = ids;
        return ids;
#else
        return Array.Empty<string>();
#endif
    }

#if UNITY_EDITOR
    /// <summary>シーンファイル (YAML) から PlayerSpawnPoint の _id を拾う (シーンを開かずに済ませるため)。</summary>
    private static string[] ScanSpawnIds(string sceneName)
    {
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:Scene {sceneName}");
        var scenePath = guids.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName);
        var scriptGuid = UnityEditor.AssetDatabase.FindAssets("PlayerSpawnPoint t:MonoScript")
            .FirstOrDefault(g => Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GUIDToAssetPath(g)) == "PlayerSpawnPoint");
        if (scenePath == null || scriptGuid == null)
            return Array.Empty<string>();

        var ids = new List<string>();
        var text = File.ReadAllText(scenePath);
        foreach (var block in text.Split(new[] { "--- !u!" }, StringSplitOptions.None))
        {
            if (!block.Contains("guid: " + scriptGuid))
                continue;

            var id = "";
            foreach (var line in block.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("_id:"))
                {
                    id = trimmed.Substring(4).Trim();
                    break;
                }
            }

            if (!ids.Contains(id))
                ids.Add(id);
        }

        ids.Sort(StringComparer.Ordinal);
        return ids.ToArray();
    }
#endif

    // ---------------------------------------------------------------- 能力・チート

    public static bool Invincible
    {
        get => DebugCheats.Invincible;
        set => DebugCheats.Invincible = value;
    }

    public static bool HasUpgrade(ScissorUpgrade upgrade) =>
        Player != null && Player.Progression != null && Player.Progression.Has(upgrade);

    public static void SetUpgrade(ScissorUpgrade upgrade, bool has)
    {
        var progression = Player != null ? Player.Progression : null;
        if (progression == null || progression.Has(upgrade) == has)
            return;
        if (has) progression.Grant(upgrade);
        else progression.Revoke(upgrade);
    }

    public static bool HasSkill(PlayerSkill skill) => Player != null && Player.HasSkill(skill);

    public static void SetSkill(PlayerSkill skill, bool has)
    {
        var progression = Player != null ? Player.Progression : null;
        if (progression == null || progression.HasSkill(skill) == has)
            return;
        if (has) progression.GrantSkill(skill);
        else progression.RevokeSkill(skill);
    }

    /// <summary>ハサミ強化・移動スキルをすべて付与 / 剥奪する。</summary>
    public static void SetAllAbilities(bool has)
    {
        foreach (ScissorUpgrade upgrade in Enum.GetValues(typeof(ScissorUpgrade)))
            SetUpgrade(upgrade, has);
        foreach (PlayerSkill skill in Enum.GetValues(typeof(PlayerSkill)))
            SetSkill(skill, has);
    }

    /// <summary>HP 全回復 + 回復ゲージ MAX。</summary>
    public static void RefillHealth()
    {
        var p = Player;
        if (p == null)
            return;
        p.Health?.Heal(9999);
        p.HealGauge?.AddCharge(9999f);
    }

    public static void AddThread(int amount) => Player?.Inventory?.AddThread(amount);

    public static void UnlockAllAttacks()
    {
        var loadout = Player != null ? Player.AttackLoadout : null;
        if (loadout == null)
            return;
        foreach (var attack in loadout.Catalog)
            loadout.Unlock(attack);
    }

    /// <summary>ロード済みの敵を全滅させる (ダメージを与えて倒すので撃破フラグ等も通常どおり立つ)。</summary>
    public static int KillAllEnemies()
    {
        var p = Player;
        var source = p != null ? p.gameObject : null;
        var count = 0;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            enemy.TakeDamage(new DamageInfo(9999, 9999, enemy.transform.position, source));
            count++;
        }

        return count;
    }

    // ---------------------------------------------------------------- 進行フラグ

    public static void ApplyPreset(FlagPreset preset)
    {
        foreach (var flag in preset.Flags)
            GameProgress.Set(flag);
        foreach (var upgrade in preset.Upgrades)
            SetUpgrade(upgrade, true);
    }

    /// <summary>エリア1 のフラグ (A1_ で始まるもの) をすべて外す。</summary>
    public static void ClearArea1Flags()
    {
        foreach (var flag in GameProgress.Collect().Where(f => f.StartsWith("A1_")).ToArray())
            GameProgress.Unset(flag);
    }
}
#endif
