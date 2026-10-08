using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utage を使った会話シーン (DialogueScene, Additive) の静的な入口。
/// ゲーム起動時 (最初のシーンのロード直後) に先読みして宴のエンジンまで起動しておき (<see cref="Preload"/>)、
/// 中身を DontDestroyOnLoad へ移して常駐させる (死亡リトライの PlayerScene 読み直し・タイトルへ戻るでも消えない)。
/// 会話を始める時にロード待ちが無い (PendingChanges 46)。先読みが間に合わない・無効な場合は従来どおり初回の呼び出しでロードする。
/// 呼び出し側は <see cref="Play"/> にシナリオラベルを渡すだけでよい。
/// 会話は同時に 1 つだけ: 再生中 (<see cref="IsPlaying"/>) とシーンのロード中 (<see cref="IsLoading"/>) は
/// 新しい再生要求を無視し、DialogueScene を二重にロードしない (2026-10-03 バグ修正, Docs/DialogueSystem.md §7)。
/// </summary>
public static class DialogueService
{
    public const string SceneName = "DialogueScene";

    /// <summary>
    /// 会話が終わってから、インタラクト (NPC に話しかける) で次の会話を始められるまでの時間 (秒, unscaled)。
    /// 会話を送るボタン (E / J / Space / パッド) の連打で、閉じた直後に同じ NPC の会話がまた開くのを防ぐ。
    /// </summary>
    public const float ReopenCooldown = 0.35f;

    private static UtageDialogueScene _controller;
    private static AsyncOperation _loadOp;
    private static bool _playRequestedWhileLoading;
    private static readonly List<Action<UtageDialogueScene>> _pending = new();
    private static float _lastEndTime = float.NegativeInfinity;

    /// <summary>会話を再生中か。</summary>
    public static bool IsPlaying => _controller != null && _controller.IsPlaying;

    /// <summary>DialogueScene をロード中か (最初の会話の開始待ち)。</summary>
    public static bool IsLoading => _loadOp != null;

    /// <summary>会話の再生中またはその準備中 (新しい会話は始められない)。</summary>
    public static bool IsBusy => IsPlaying || IsLoading;

    /// <summary>
    /// プレイヤーのインタラクトで会話を始めてよいか (NPC などの CanInteract 用)。
    /// 再生中・ロード中に加えて、会話が終わった直後 (<see cref="ReopenCooldown"/>) も false。
    /// </summary>
    public static bool CanStartFromInteract =>
        !IsBusy && Time.unscaledTime - _lastEndTime >= ReopenCooldown;

    /// <summary>ゲーム起動時に先読みするか (テスト等で止めたい時に false。プレイモードごとに true へ戻る)。</summary>
    public static bool PreloadOnBoot = true;

    /// <summary>宴のエンジンの起動 (シナリオ・設定の読み込み) まで済んでいるか。</summary>
    public static bool IsReady => _controller != null && _controller.IsBooted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PreloadOnBoot = true;
        _controller = null;
        _loadOp = null;
        _playRequestedWhileLoading = false;
        _pending.Clear();
        _lastEndTime = float.NegativeInfinity;
        StartedFrame = -1;
    }

    // ゲーム起動時 (最初のシーンの Awake 後) に会話シーンを先読みする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PreloadAtBoot()
    {
        if (PreloadOnBoot)
            Preload();
    }

    /// <summary>
    /// DialogueScene をロードして宴のエンジンを起動しておく (未ロード時のみ。表示はしない)。
    /// 何度呼んでもよい。ロード中・起動済みなら何もしない。
    /// </summary>
    public static void Preload()
    {
        WithController(controller => controller.Boot());
    }

    /// <summary>直近の会話を始めたフレーム (Time.frameCount)。会話を開いたボタンでそのまま 1 ページ目を送らないために使う。</summary>
    public static int StartedFrame { get; private set; } = -1;

    /// <summary>UtageDialogueScene が会話の始めに呼ぶ。</summary>
    internal static void NotifyStarted()
    {
        StartedFrame = Time.frameCount;
    }

    /// <summary>UtageDialogueScene が会話の終わりに呼ぶ (onComplete より前)。</summary>
    internal static void NotifyEnded()
    {
        _lastEndTime = Time.unscaledTime;
    }

    /// <summary>
    /// 指定ラベルのシナリオを再生する。DialogueScene が未ロードならロードしてから再生する。
    /// 会話の再生中・ロード中は無視する (警告ログ)。
    /// </summary>
    /// <param name="scenarioLabel">シナリオラベル (先頭の * は不要)</param>
    /// <param name="onComplete">会話終了時に呼ばれるコールバック</param>
    public static void Play(string scenarioLabel, Action onComplete = null)
    {
        if (string.IsNullOrEmpty(scenarioLabel))
        {
            Debug.LogWarning("[DialogueService] シナリオラベルが空です。");
            return;
        }

        if (IsPlaying || _playRequestedWhileLoading)
        {
            Debug.LogWarning($"[DialogueService] 会話の再生中 (または開始待ち) のため '{scenarioLabel}' の再生要求を無視しました。");
            return;
        }

        WithController(controller =>
        {
            _playRequestedWhileLoading = false;
            controller.Play(scenarioLabel, onComplete);
        });
        // シーンのロード待ちに入った (この再生要求は保留中)。ロードが終わるまで次の再生要求は受けない
        if (IsLoading)
            _playRequestedWhileLoading = true;
    }

    /// <summary>
    /// DialogueScene をロード (未ロード時) してシナリオラベル一覧を取得する (デバッグ用)。
    /// エンジンのブート完了後にコールバックされる。
    /// </summary>
    public static void LoadScenarioLabels(Action<string[]> onLoaded)
    {
        WithController(controller => controller.CollectScenarioLabels(onLoaded));
    }

    /// <summary>コントローラを用意して action を実行する。シーン未ロードなら Additive ロードしてから実行。</summary>
    private static void WithController(Action<UtageDialogueScene> action)
    {
        // 常駐済みならそのまま (シーン遷移で破棄されていれば Unity null になる)
        if (_controller != null)
        {
            action(_controller);
            return;
        }

        // ロード中なら完了後にまとめて実行する (2 回目の LoadSceneAsync はしない)
        if (_loadOp != null)
        {
            _pending.Add(action);
            return;
        }

        var scene = SceneManager.GetSceneByName(SceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            FindControllerAndRun(action);
            return;
        }

        if (scene.IsValid())
        {
            // 他所 (エディタでの手動ロードなど) がロード中。二重ロードはしない
            Debug.LogWarning($"[DialogueService] '{SceneName}' が別の経路でロード中のため要求を無視しました。");
            _playRequestedWhileLoading = false;
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogWarning($"[DialogueService] シーン '{SceneName}' が Build Settings に見つかりません。");
            _playRequestedWhileLoading = false;
            return;
        }

        _pending.Add(action);
        _loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        _loadOp.completed += OnLoaded;
    }

    private static void OnLoaded(AsyncOperation op)
    {
        if (op != _loadOp)
            return; // プレイモードをまたいだ古いロード
        _loadOp = null;
        MakeResident();
        var actions = _pending.ToArray();
        _pending.Clear();
        foreach (var action in actions)
            FindControllerAndRun(action);
    }

    /// <summary>
    /// 自分でロードした DialogueScene の中身を DontDestroyOnLoad へ移し、空になったシーンは降ろす。
    /// PlayerScene の読み直し (死亡リトライ・タイトルからの開始) やタイトルへ戻る LoadScene (Single) で消えないように。
    /// </summary>
    private static void MakeResident()
    {
        var scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var controller = UnityEngine.Object.FindFirstObjectByType<UtageDialogueScene>(FindObjectsInactive.Include);
        if (controller == null || controller.gameObject.scene != scene)
            return;

        controller.RememberOwnRoots(scene.GetRootGameObjects());
        foreach (var root in scene.GetRootGameObjects())
            UnityEngine.Object.DontDestroyOnLoad(root);
        SceneManager.UnloadSceneAsync(scene);
        _controller = controller;
    }

    private static void FindControllerAndRun(Action<UtageDialogueScene> action)
    {
        if (_controller == null)
            _controller = UnityEngine.Object.FindFirstObjectByType<UtageDialogueScene>(FindObjectsInactive.Include);
        if (_controller == null)
        {
            _playRequestedWhileLoading = false;
            Debug.LogWarning($"[DialogueService] {nameof(UtageDialogueScene)} が '{SceneName}' 内に見つかりません。");
            return;
        }

        action(_controller);
    }
}
