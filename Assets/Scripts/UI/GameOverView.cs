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

    private MenuPanelView _menu;

    private void Awake()
    {
        var menuGo = new GameObject("GameOverMenu", typeof(RectTransform));
        menuGo.transform.SetParent(transform, false);
        var rt = (RectTransform)menuGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _menu = menuGo.AddComponent<MenuPanelView>();
        _menu.Initialize(_font);
        _menu.AllowCancel = false; // ゲームオーバーは選択必須
    }

    public void Show()
    {
        if (_menu.IsOpen)
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
        var activeScene = SceneManager.GetActiveScene().name;

        // セーブが現在のシーンのものなら拠点から再開する
        var save = SaveSystem.TryLoad();
        GameSession.PendingLoad = (save != null && save.sceneName == activeScene) ? save : null;

        GamePause.Reset();
        SceneManager.LoadScene(activeScene);
    }

    private void GoTitle()
    {
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }
}
