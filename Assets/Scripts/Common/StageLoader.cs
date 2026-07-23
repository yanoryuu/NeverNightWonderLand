using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージの Additive 運用を管理する (PlayerScene に置く)。
/// プレイヤーは PlayerScene に常駐し、その上へステージと UI を Additive で重ねる方式。
/// ステージ入替 (<see cref="TransitionTo"/>) ではプレイヤーが破棄されないため、
/// HP・アイテム・装備などのステータスがそのまま維持される。
/// 全リセットしたい時は <see cref="LoadWithPlayerScene"/> で PlayerScene ごと読み直す
/// (タイトルからの開始・死亡リトライ)。
/// 起動時のステージは GameSession.PendingStage → ロード済みステージ
/// (エディタでステージシーンを直接開いて再生した場合) → 既定ステージ の順で決まる。
/// UI シーンは「名前が UI で終わる」規約でステージと見分ける。
/// </summary>
public class StageLoader : MonoBehaviour
{
    public const string PlayerSceneName = "PlayerScene";

    public static StageLoader Instance { get; private set; }

    [Tooltip("開始ステージの指定が無い時に読む既定のステージ名")]
    [SerializeField] private string _defaultStageName = "PreparationRoomScene";

    /// <summary>現在のステージ名 (セーブの照合・リトライに使う)。</summary>
    public string CurrentStageName { get; private set; }

    private string _pendingEntranceId; // 次のステージで使う入り口 Id (TransitionTo で指定)
    private bool _isLoading;           // ロード中の多重遷移防止

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        var pending = GameSession.PendingStage;
        GameSession.PendingStage = null;

        if (!string.IsNullOrEmpty(pending))
        {
            LoadStage(pending);
            return;
        }

        // エディタでステージシーンを直接開いて再生した場合は、それを現在のステージとして扱う
        var existing = FindLoadedStage();
        if (existing.IsValid())
        {
            CurrentStageName = existing.name;
            SceneManager.SetActiveScene(existing);
            PlacePlayer(existing);
            return;
        }

        LoadStage(_defaultStageName);
    }

    /// <summary>
    /// ステージを入れ替える (プレイヤーは破棄されず、ステータスは維持される)。
    /// entranceId を指定すると、遷移先ステージ内の同じ Id を持つ PlayerSpawnPoint から開始する
    /// (出口⇔入り口の接続)。SceneTransitionZone から呼ばれる。
    /// ロード中・同一ステージなどで遷移しなかった場合は false を返す。
    /// </summary>
    public bool TransitionTo(string stageName, string entranceId = null)
    {
        if (_isLoading || string.IsNullOrEmpty(stageName) || stageName == CurrentStageName)
            return false;

        _pendingEntranceId = entranceId;

        // 一瞬暗転してから入れ替える (プレイヤーのワープとカメラのスナップを見せない)
        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            _isLoading = true; // 暗転中の多重発火も防ぐ
            var unloadAfter = CurrentStageName;
            fader.FadeOut(() =>
            {
                _isLoading = false; // LoadStage が改めて管理する
                LoadStage(stageName, unloadAfter);
            });
        }
        else
        {
            LoadStage(stageName, unloadAfter: CurrentStageName);
        }

        return true;
    }

    private void LoadStage(string stageName, string unloadAfter = null)
    {
        CurrentStageName = stageName;

        var load = SceneManager.LoadSceneAsync(stageName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError($"[{nameof(StageLoader)}] ステージ '{stageName}' をロードできません。Build Settings を確認してください。", this);
            _pendingEntranceId = null;
            ScreenFader.Instance?.FadeIn(); // 暗転したまま止まらないように
            return;
        }

        _isLoading = true;
        load.completed += _ =>
        {
            // 新ステージのロード完了後に旧ステージを降ろす (足場が消える瞬間を作らない)
            if (!string.IsNullOrEmpty(unloadAfter))
            {
                var old = SceneManager.GetSceneByName(unloadAfter);
                if (old.IsValid() && old.isLoaded)
                    SceneManager.UnloadSceneAsync(old);
            }

            var scene = SceneManager.GetSceneByName(stageName);
            if (scene.IsValid())
                SceneManager.SetActiveScene(scene);

            PlacePlayer(scene);

            // 暗転中にカメラをプレイヤーへ即時スナップしてから明転する
            var follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null)
                follow.SnapToTarget();

            _isLoading = false;
            ScreenFader.Instance?.FadeIn();
        };
    }

    /// <summary>
    /// PlayerScene ごと読み直して全リセットで開始する (タイトルからの開始・死亡リトライ用)。
    /// PlayerScene が Build Settings に無い場合は旧方式 (ステージ単体ロード) にフォールバックする。
    /// </summary>
    public static void LoadWithPlayerScene(string stageName)
    {
        GameSession.PendingStage = stageName;
        GamePause.Reset();
        DefeatedEnemyRegistry.Clear(); // 全リセットでは倒した敵も復活する

        if (Application.CanStreamedLevelBeLoaded(PlayerSceneName))
        {
            SceneManager.LoadScene(PlayerSceneName);
        }
        else
        {
            GameSession.PendingStage = null;
            SceneManager.LoadScene(stageName);
        }
    }

    /// <summary>ロード済みシーンからステージ (PlayerScene でも UI シーンでもないもの) を探す。</summary>
    private static Scene FindLoadedStage()
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.name == PlayerSceneName || scene.name.EndsWith("UI"))
                continue;

            return scene;
        }

        return default;
    }

    /// <summary>
    /// ステージロード後のプレイヤー配置。セーブ復帰 (PendingLoad) があればその位置を最優先し、
    /// 無ければ入り口 Id に対応する PlayerSpawnPoint へ移動する。
    /// </summary>
    private void PlacePlayer(Scene stageScene)
    {
        var entranceId = _pendingEntranceId;
        _pendingEntranceId = null;

        var player = FindAnyObjectByType<PlayerController>();
        if (player == null)
            return;

        var bridge = player.GetComponent<PlayerSaveBridge>();
        if (bridge != null && bridge.TryApplyPendingLoad())
            return;

        var spawn = FindSpawnPoint(stageScene, entranceId);
        if (spawn != null)
            player.transform.position = spawn.transform.position;
    }

    /// <summary>
    /// 入り口を解決する。優先順: entranceId 一致 → Id 空 (デフォルト入口) → 最初の1つ。
    /// </summary>
    private static PlayerSpawnPoint FindSpawnPoint(Scene stageScene, string entranceId)
    {
        if (!stageScene.IsValid())
            return null;

        PlayerSpawnPoint defaultSpawn = null;
        PlayerSpawnPoint firstSpawn = null;

        foreach (var root in stageScene.GetRootGameObjects())
        {
            foreach (var spawn in root.GetComponentsInChildren<PlayerSpawnPoint>(true))
            {
                if (!string.IsNullOrEmpty(entranceId) && spawn.Id == entranceId)
                    return spawn;

                if (defaultSpawn == null && string.IsNullOrEmpty(spawn.Id))
                    defaultSpawn = spawn;
                if (firstSpawn == null)
                    firstSpawn = spawn;
            }
        }

        if (!string.IsNullOrEmpty(entranceId))
        {
            Debug.LogWarning(
                $"[{nameof(StageLoader)}] ステージ '{stageScene.name}' に入り口 Id '{entranceId}' の " +
                "PlayerSpawnPoint がありません。デフォルト入口を使います。");
        }

        return defaultSpawn != null ? defaultSpawn : firstSpawn;
    }
}
