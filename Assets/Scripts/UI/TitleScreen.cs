using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// タイトル画面。はじめから / つづきから (セーブがある時のみ) / ゲーム終了。
/// UI は実行時に構築するため、シーンにはカメラとこのコンポーネントだけあればよい。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("ゲームタイトル表示")]
    [SerializeField] private string _gameTitle = "Never Night Wonderland";

    [Tooltip("「はじめから」で読み込むシーン名")]
    [SerializeField] private string _startSceneName = "TutorialScene";

    private void Awake()
    {
        GamePause.Reset();
    }

    private void Start()
    {
        // Canvas 構築
        var canvasGo = new GameObject("TitleCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // タイトルロゴ (テキスト)
        var titleGo = new GameObject("GameTitle", typeof(RectTransform));
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleRt = (RectTransform)titleGo.transform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -120f);
        titleRt.sizeDelta = new Vector2(1400f, 120f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = _gameTitle;
        title.fontSize = 84f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.9f, 0.9f, 1f);
        if (_font != null)
            title.font = _font;

        // メニュー
        var menuGo = new GameObject("TitleMenu", typeof(RectTransform));
        menuGo.transform.SetParent(canvasGo.transform, false);
        var menuRt = (RectTransform)menuGo.transform;
        menuRt.anchorMin = Vector2.zero;
        menuRt.anchorMax = Vector2.one;
        menuRt.offsetMin = Vector2.zero;
        menuRt.offsetMax = Vector2.zero;

        var menu = menuGo.AddComponent<MenuPanelView>();
        menu.Initialize(_font);
        menu.AllowCancel = false;
        menu.SetTitle("");
        menu.SetBody("");

        var hasSave = SaveSystem.Exists();
        menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("はじめから", StartNewGame),
            new("つづきから", ContinueGame, hasSave),
            new("ゲーム終了", QuitGame),
        });
        menu.Open();
    }

    private void StartNewGame()
    {
        GameSession.PendingLoad = null;
        SceneManager.LoadScene(_startSceneName);
    }

    private void ContinueGame()
    {
        var save = SaveSystem.TryLoad();
        if (save == null || string.IsNullOrEmpty(save.sceneName))
        {
            Notifier.Notify("セーブデータが読み込めなかった");
            return;
        }

        GameSession.PendingLoad = save;
        SceneManager.LoadScene(save.sceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
