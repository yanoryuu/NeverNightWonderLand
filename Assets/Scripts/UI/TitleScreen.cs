using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面。はじめから / つづきから (セーブがある時のみ) / ゲーム終了。
/// UI は実行時に生成せず、シーン上で事前配置したタイトルロゴとメニューパネルを参照する
/// (素材の差し替えはシーン/プレハブ側で行う)。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("ゲームタイトル (ロゴテキストへ反映される)")]
    [SerializeField] private string _gameTitle = "Never Night Wonderland";

    [Tooltip("「はじめから」で読み込むシーン名")]
    [SerializeField] private string _startSceneName = "TutorialScene";

    [Tooltip("タイトルロゴのテキスト (事前配置)")]
    [SerializeField] private TMP_Text _titleLabel;

    [Tooltip("メニューパネル (事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    private void Awake()
    {
        GamePause.Reset();
    }

    private void Start()
    {
        if (_titleLabel != null)
            _titleLabel.text = _gameTitle;

        if (_menu == null)
        {
            Debug.LogError($"[{nameof(TitleScreen)}] MenuPanelView が設定されていません。", this);
            return;
        }

        _menu.Initialize(_font);
        _menu.AllowCancel = false;
        _menu.SetTitle("");
        _menu.SetBody("");

        var hasSave = SaveSystem.Exists();
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("はじめから", StartNewGame),
            new("つづきから", ContinueGame, hasSave),
            new("ゲーム終了", QuitGame),
        });
        _menu.Open();
    }

    private void StartNewGame()
    {
        GameSession.PendingLoad = null;
        // PlayerScene を土台に開始ステージを Additive で重ねる (PlayerScene 未登録なら旧方式)
        StageLoader.LoadWithPlayerScene(_startSceneName);
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
        StageLoader.LoadWithPlayerScene(save.sceneName);
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
