using UnityEngine;

/// <summary>拾うと素材「糸」をまとめて入手できるピックアップ。</summary>
public class ThreadPickup : ItemPickup
{
    [Tooltip("入手できる糸の数")]
    [SerializeField] private int _amount = 10;

    protected override bool OnPickedUp(GameObject interactor)
    {
        var inventory = interactor.GetComponent<PlayerItemInventory>();
        if (inventory == null)
            return false;

        inventory.AddThread(_amount); // 通知は AddThread が出す
        return true;
    }
}
