using UnityEngine;

/// <summary>拾うと攻撃方法 (装備品) が解放されるピックアップ。装備の入れ替えは拠点で行う。</summary>
public class EquipmentPickup : ItemPickup
{
    [Tooltip("解放する攻撃方法")]
    [SerializeField] private AttackDefinition _attack;

    protected override bool OnPickedUp(GameObject interactor)
    {
        var loadout = interactor.GetComponent<PlayerAttackLoadout>();
        if (loadout == null || _attack == null)
            return false;

        if (loadout.IsUnlocked(_attack))
        {
            Notifier.Notify($"「{_attack.DisplayName}」は習得済みだ");
            return true; // 拾ったことにして消す
        }

        loadout.Unlock(_attack);
        Notifier.Notify($"装備品「{_attack.DisplayName}」を手に入れた! (拠点で装備できる)");
        return true;
    }
}
