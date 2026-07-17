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

    [Tooltip("メニューパネル (プレハブ上で事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(ResultView)}] MenuPanelView が設定されていません。", this);
            return;
        }

        _menu.Initialize(_font);
        _menu.AllowCancel = false;
    }

    public void Show(GameObject playerGo)
    {
        if (_menu == null || _menu.IsOpen)
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

        var stageName = StageLoader.Instance != null
            ? StageLoader.Instance.CurrentStageName
            : SceneManager.GetActiveScene().name;

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
