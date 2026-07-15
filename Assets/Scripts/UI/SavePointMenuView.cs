using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 拠点 (セーブポイント) メニュー。SavePoint のインタラクトから開かれる。
/// HP全回復 (無料) / アイテム補充 (糸を消費して再生成) / アイテム切替 / セーブ (確認あり)。
/// </summary>
public class SavePointMenuView : MonoBehaviour
{
    private enum Page { Main, Items, SaveConfirm }

    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    private MenuPanelView _menu;
    private Page _page;

    // 開いた時のコンテキスト
    private PlayerController _player;
    private PlayerSaveBridge _saveBridge;
    private SavePoint _savePoint;

    private void Awake()
    {
        var menuGo = new GameObject("SavePointMenu", typeof(RectTransform));
        menuGo.transform.SetParent(transform, false);
        var rt = (RectTransform)menuGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _menu = menuGo.AddComponent<MenuPanelView>();
        _menu.Initialize(_font);
        _menu.OnCancelled += OnCancel;
    }

    public void Open(GameObject playerGo, SavePoint savePoint)
    {
        if (_menu.IsOpen)
            return;

        _player = playerGo.GetComponent<PlayerController>();
        _saveBridge = playerGo.GetComponent<PlayerSaveBridge>();
        _savePoint = savePoint;

        GamePause.Push();
        ShowMain();
        _menu.Open();
    }

    private void CloseMenu()
    {
        _menu.Close();
        GamePause.Pop();
    }

    private void OnCancel()
    {
        if (_page == Page.Main)
            CloseMenu();
        else
            ShowMain();
    }

    #region Pages

    private void ShowMain()
    {
        _page = Page.Main;
        _menu.SetTitle("拠点");

        var inventory = _player != null ? _player.Inventory : null;
        var refillCost = _player != null ? _player.Consts.RefillThreadCost : 0;
        var thread = inventory != null ? inventory.Thread.CurrentValue : 0;
        _menu.SetBody($"糸: {thread}");

        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("HP を全回復する", HealFull),
            new($"アイテムを補充する (糸 {refillCost})", Refill, inventory != null),
            new("セーブ", ShowSaveConfirm, _saveBridge != null),
            new("閉じる", CloseMenu),
        });
    }

    private void HealFull()
    {
        if (_player != null && _player.Health != null)
        {
            _player.Health.Heal(_player.Consts.MaxHp);
            Notifier.Notify("体力が全回復した");
        }

        ShowMain();
    }

    private void Refill()
    {
        _player?.Inventory?.TryRefillAll();
        ShowMain(); // 糸の表示を更新
    }

    private void ShowSaveConfirm()
    {
        _page = Page.SaveConfirm;
        _menu.SetTitle("セーブ");
        _menu.SetBody("ここまでの進行をセーブしますか?");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("はい", DoSave),
            new("いいえ", ShowMain),
        });
    }

    private void DoSave()
    {
        var data = _saveBridge.Collect();

        // 復帰位置は拠点の位置にする
        if (_savePoint != null)
        {
            data.posX = _savePoint.transform.position.x;
            data.posY = _savePoint.transform.position.y + 0.5f;
        }

        SaveSystem.Save(data);
        Notifier.Notify("セーブしました");
        ShowMain();
    }

    #endregion
}
