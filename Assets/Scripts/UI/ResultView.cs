using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// クリア/リザルト画面。GoalZone 到達で表示され、撃破数・糸・タイムを集計する。
/// </summary>
public class ResultView : MonoBehaviour
{
    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("タイトルシーン名")]
    [SerializeField] private string _titleSceneName = "TitleScene";

    private MenuPanelView _menu;

    private void Awake()
    {
        var menuGo = new GameObject("ResultMenu", typeof(RectTransform));
        menuGo.transform.SetParent(transform, false);
        var rt = (RectTransform)menuGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _menu = menuGo.AddComponent<MenuPanelView>();
        _menu.Initialize(_font);
        _menu.AllowCancel = false;
    }

    public void Show(GameObject playerGo)
    {
        if (_menu.IsOpen)
            return;

        var inventory = playerGo != null ? playerGo.GetComponent<PlayerItemInventory>() : null;
        var thread = inventory != null ? inventory.Thread.CurrentValue : 0;

        var elapsed = Mathf.Max(0f, Time.time - GameSession.RunStartTime);
        var minutes = Mathf.FloorToInt(elapsed / 60f);
        var seconds = Mathf.FloorToInt(elapsed % 60f);

        GamePause.Push();

        _menu.SetTitle("エリアクリア!");
        _menu.SetBody(
            $"撃破数: {GameSession.EnemiesDefeated}\n" +
            $"糸: {thread}\n" +
            $"タイム: {minutes:00}:{seconds:00}");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("もう一度", Replay),
            new("タイトルへ", GoTitle),
        });
        _menu.Open();
    }

    private void Replay()
    {
        GameSession.PendingLoad = null;
        GamePause.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoTitle()
    {
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }
}
