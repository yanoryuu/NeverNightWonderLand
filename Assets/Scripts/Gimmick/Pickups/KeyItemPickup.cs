using UnityEngine;

/// <summary>
/// 拾うと進行フラグが立つキーアイテムのピックアップ。
/// 扉の解錠条件などは立てたフラグを FlagDoor 等が参照する。
/// </summary>
public class KeyItemPickup : ItemPickup
{
    [Tooltip("キーアイテムの名前 (通知表示用)")]
    [SerializeField] private string _itemName = "鍵";

    [Tooltip("立てる進行フラグ (GameProgress)")]
    [SerializeField] private string _flagId = "";

    protected override bool OnPickedUp(GameObject interactor)
    {
        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        Notifier.Notify($"{_itemName}を手に入れた!");
        return true;
    }
}
