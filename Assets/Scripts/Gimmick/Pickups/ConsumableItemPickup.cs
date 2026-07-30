using UnityEngine;

/// <summary>拾うと消耗品アイテム (まち針・ボビン爆弾など) を入手できるピックアップ。</summary>
public class ConsumableItemPickup : ItemPickup
{
    [Tooltip("入手できるアイテム (プレイヤーのカタログに含まれていること)")]
    [SerializeField] private ItemDefinition _item;

    [Tooltip("入手できる個数")]
    [SerializeField] private int _count = 1;

    protected override bool OnPickedUp(GameObject interactor)
    {
        var inventory = interactor.GetComponent<PlayerItemInventory>();
        if (inventory == null || _item == null)
            return false;

        var added = 0;
        for (var i = 0; i < _count; i++)
        {
            if (!inventory.TryAddItem(_item))
                break;
            added++;
        }

        if (added <= 0)
        {
            Notifier.Notify($"{_item.DisplayName}はこれ以上持てない");
            return false; // 持ちきれないので残しておく
        }

        Notifier.Notify($"{_item.DisplayName}を {added} 個手に入れた");
        return true;
    }
}
