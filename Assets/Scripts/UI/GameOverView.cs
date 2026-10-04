using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー画面。DeadState から表示され、リトライ (セーブがあれば拠点から) かタイトルへ戻る。
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
        _menu.SetBody(GameSession.SaveBelongsToRun && SaveSystem.Exists() ? "セーブした拠点からやり直せる。" : "");
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
    /// このプレイのセーブが現在のステージのものなら拠点から再開し、そうでなければ今の状態を持ってステージの入り口から再開する。
    /// どちらでも取得済みの能力は失わない (PlayerSaveBridge.CollectForRetry, PendingChanges 38)。
    /// </summary>
    public static void RetryCurrentStage()
    {
        var stageName = StageLoader.Instance != null
            ? StageLoader.Instance.CurrentStageName
            : SceneManager.GetActiveScene().name;

        var bridge = FindFirstObjectByType<PlayerSaveBridge>();
        GameSession.PendingLoad = bridge != null ? bridge.CollectForRetry(stageName) : null;

        if (StageLoader.Instance != null)
        {
            // PlayerScene ごと読み直して HP・敵・木箱をリセットする (能力・持ち物は PendingLoad で戻す)
            StageLoader.LoadWithPlayerScene(stageName);
        }
        else
        {
            GamePause.Reset();
            SceneManager.LoadScene(stageName);
        }
    }

    private void GoTitle()
    {
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }
}
