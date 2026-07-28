using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ショップ画面 (拠点)。ShopKeeper のインタラクトから開き、糸を消費してアイテムを購入する。
/// UI はシーン上で事前配置した MenuPanelView を参照する (実行時生成なし)。
/// 品揃えと価格は _lineup で設定する。所持数は各アイテムの最大数まで。
/// </summary>
public class ShopView : MonoBehaviour
{
    [System.Serializable]
    public class ShopEntry
    {
        [Tooltip("販売するアイテム (プレイヤーのカタログに含まれていること)")]
        public ItemDefinition Item;

        [Tooltip("価格 (糸)")]
        public int Price = 3;
    }

    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("メニューパネル (シーン上で事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    [Tooltip("品揃え")]
    [SerializeField] private ShopEntry[] _lineup;

    private PlayerItemInventory _inventory;

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(ShopView)}] MenuPanelView が設定されていません。", this);
            return;
        }

        _menu.Initialize(_font);
        _menu.OnCancelled += CloseMenu;
    }

    public void Open(GameObject playerGo)
    {
        if (_menu == null || _menu.IsOpen)
            return;

        _inventory = playerGo.GetComponent<PlayerItemInventory>();
        if (_inventory == null)
            return;

        GamePause.Push();
        Rebuild();
        _menu.Open();
    }

    private void CloseMenu()
    {
        _menu.Close();
        GamePause.Pop();
    }

    private void Rebuild()
    {
        _menu.SetTitle("ショップ");
        _menu.SetBody($"糸を消費してアイテムを購入する\n所持している糸: {_inventory.Thread.CurrentValue}");

        var entries = new List<MenuPanelView.Entry>();
        foreach (var sale in _lineup)
        {
            if (sale == null || sale.Item == null)
                continue;

            var item = sale.Item;
            var count = _inventory.GetCount(item);
            var label = $"{item.DisplayName}  糸{sale.Price}  (所持 {count}/{item.MaxCount})";
            var canBuy = count < item.MaxCount && _inventory.Thread.CurrentValue >= sale.Price;
            entries.Add(new MenuPanelView.Entry(label, () => Purchase(sale), canBuy));
        }

        entries.Add(new MenuPanelView.Entry("やめる", CloseMenu));
        _menu.SetEntries(entries);
    }

    private void Purchase(ShopEntry sale)
    {
        if (_inventory.GetCount(sale.Item) >= sale.Item.MaxCount)
        {
            Notifier.Notify("これ以上は持てない");
        }
        else if (!_inventory.TrySpendThread(sale.Price))
        {
            Notifier.Notify($"糸が足りない… ({sale.Item.DisplayName}には糸 {sale.Price} が必要)");
        }
        else
        {
            _inventory.TryAddItem(sale.Item);
            Notifier.Notify($"{sale.Item.DisplayName}を購入した");
        }

        Rebuild(); // 所持数・購入可否の表示を更新する
    }
}
