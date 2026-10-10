using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー画面。DeadState から表示され、リトライ (最後に休んだ拠点から) かタイトルへ戻る。
/// </summary>
public class GameOverView : MonoBehaviour
{
    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("タイトルシーン名")]
    [SerializeField] private string _titleSceneName = "TitleScene";

    [Tooltip("メニューパネル (プレハブ上で事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(GameOverView)}] MenuPanelView が設定されていません。", this);
            return;
        }

        _menu.Initialize(_font);
        _menu.AllowCancel = false; // ゲームオーバーは選択必須
    }

    public void Show()
    {
        if (_menu == null || _menu.IsOpen)
            return;

        _menu.SetTitle("ゲームオーバー");
        _menu.SetBody(GameSession.HasRespawnBench ? "最後に休んだ拠点からやり直せる。" : "");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("リトライ", Retry),
            new("タイトルへ", GoTitle),
        });
        _menu.Open();
    }

    private void Retry() => RetryCurrentStage();

    /// <summary>
    /// 死亡からのリトライ (DeadState のゲームオーバー画面が無い時もここ)。
    /// 最後に休んだ拠点のシーンを読み、拠点から再開する (死んだ部屋に関係なく。まだ休んでいなければ今のステージの入り口から)。
    /// 状態は戻さない: 能力・持ち物・糸・コイン・進行フラグは今のまま (PlayerSaveBridge.CollectForRetry, PendingChanges 38/39)。
    /// </summary>
    public static void RetryCurrentStage()
    {
        var stageName = StageLoader.Instance != null
            ? StageLoader.Instance.CurrentStageName
            : SceneManager.GetActiveScene().name;

        var bridge = FindFirstObjectByType<PlayerSaveBridge>();
        var data = bridge != null ? bridge.CollectForRetry(stageName) : null;
        GameSession.PendingLoad = data;
        var targetStage = data != null ? data.sceneName : stageName;

        if (StageLoader.Instance != null)
        {
            // PlayerScene ごと読み直して HP・敵・木箱をリセットする (能力・持ち物は PendingLoad で戻す)
            StageLoader.LoadWithPlayerScene(targetStage);
        }
        else
        {
            GamePause.Reset();
            SceneManager.LoadScene(targetStage);
        }
    }

    private void GoTitle()
    {
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }
}
