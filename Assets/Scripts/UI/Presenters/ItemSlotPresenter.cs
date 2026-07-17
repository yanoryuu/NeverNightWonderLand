using System;
using R3;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// アイテムスロット表示の Presenter (MonoBehaviour 非依存)。
/// PlayerItemInventoryModel の変更を購読し、行テキスト (方向グリフ・名前・残数) と
/// 色 (残数0や空スロットは薄く) を整形して View に反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class ItemSlotPresenter : IStartable, IDisposable
{
    private static readonly Color EmptyColor = new(1f, 1f, 1f, 0.3f);

    private readonly PlayerItemInventoryModel _inventory;
    private readonly IItemSlotView _view;

    private IDisposable _subscription;

    public ItemSlotPresenter(PlayerItemInventoryModel inventory, IItemSlotView view)
    {
        _inventory = inventory;
        _view = view;
    }

    public void Start()
    {
        _subscription = _inventory.Changed.Subscribe(_ => RefreshAll());
        RefreshAll();
    }

    private void RefreshAll()
    {
        var count = Mathf.Min(ItemSlotExtensions.SlotCount, _view.SlotCount);
        for (var i = 0; i < count; i++)
            RefreshSlot((ItemSlot)i);
    }

    private void RefreshSlot(ItemSlot slot)
    {
        var item = _inventory.GetSlotItem(slot);
        if (item == null)
        {
            _view.SetSlotLabel((int)slot, $"{slot.Glyph()} ---", EmptyColor);
            return;
        }

        var count = _inventory.GetCount(item);

        var color = item.IconColor;
        color.a = count > 0 ? 1f : 0.35f; // 使い切ったら薄く

        _view.SetSlotLabel((int)slot, $"{slot.Glyph()} {item.DisplayName}  x{count}", color);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
