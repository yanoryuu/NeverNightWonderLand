using UnityEngine;

/// <summary>
/// 突進系アイテムの定義 (布カッターなど)。ItemDashState がこのパラメータを参照して突進を行う。
/// </summary>
public abstract class DashItemDefinition : ItemDefinition
{
    [Header("突進")]
    [Tooltip("突進速度 (units/sec)")]
    [SerializeField] private float _dashSpeed = 18f;

    [Tooltip("突進の継続時間 (sec)")]
    [SerializeField] private float _dashDuration = 0.25f;

    [Tooltip("HP ダメージ")]
    [SerializeField] private int _hpDamage = 1;

    [Tooltip("防御値ダメージ")]
    [SerializeField] private int _guardDamage = 1;

    [Tooltip("敵へのノックバック強度")]
    [SerializeField] private float _knockback = 9f;

    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public int HpDamage => _hpDamage;
    public int GuardDamage => _guardDamage;
    public float Knockback => _knockback;

    public sealed override ItemUseMotion Motion => ItemUseMotion.Dash;
}
