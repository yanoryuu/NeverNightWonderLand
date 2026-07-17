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
        _menu.SetBody(SaveSystem.Exists() ? "セーブした拠点からやり直せる。" : "");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("リトライ", Retry),
            new("タイトルへ", GoTitle),
        });
        _menu.Open();
    }

    private void Retry()
    {
        var stageName = StageLoader.Instance != null
            ? StageLoader.Instance.CurrentStageName
            : SceneManager.GetActiveScene().name;

        // セーブが現在のステージのものなら拠点から再開する
        var save = SaveSystem.TryLoad();
        GameSession.PendingLoad = (save != null && save.sceneName == stageName) ? save : null;

        if (StageLoader.Instance != null)
        {
            // PlayerScene ごと読み直して全リセットする
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
