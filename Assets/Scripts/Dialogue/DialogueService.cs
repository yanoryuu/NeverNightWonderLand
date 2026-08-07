using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utage を使った会話シーン (DialogueScene, Additive) の静的な入口。
/// 初回呼び出しでシーンを Additive ロードし、以後は常駐させて使い回す。
/// 呼び出し側は <see cref="Play"/> にシナリオラベルを渡すだけでよい。
/// </summary>
public static class DialogueService
{
    public const string SceneName = "DialogueScene";

    private static UtageDialogueScene _controller;

    /// <summary>会話を再生中か。</summary>
    public static bool IsPlaying => _controller != null && _controller.IsPlaying;

    /// <summary>
    /// 指定ラベルのシナリオを再生する。DialogueScene が未ロードならロードしてから再生する。
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

        if (IsPlaying)
        {
            Debug.LogWarning($"[DialogueService] 会話再生中のため '{scenarioLabel}' の再生要求を無視しました。");
            return;
        }

        WithController(controller => controller.Play(scenarioLabel, onComplete));
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

        var scene = SceneManager.GetSceneByName(SceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            FindControllerAndRun(action);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogWarning($"[DialogueService] シーン '{SceneName}' が Build Settings に見つかりません。");
            return;
        }

        var op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        op.completed += _ => FindControllerAndRun(action);
    }

    private static void FindControllerAndRun(Action<UtageDialogueScene> action)
    {
        _controller = UnityEngine.Object.FindFirstObjectByType<UtageDialogueScene>(FindObjectsInactive.Include);
        if (_controller == null)
        {
            Debug.LogWarning($"[DialogueService] {nameof(UtageDialogueScene)} が '{SceneName}' 内に見つかりません。");
            return;
        }

        action(_controller);
    }
}
