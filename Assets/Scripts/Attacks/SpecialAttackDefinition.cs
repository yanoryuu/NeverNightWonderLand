using UnityEngine;

/// <summary>
/// 特殊攻撃の定義 (△ボタン、抽象)。遠距離攻撃・パリィなどの「特殊能力」枠で、
/// コスト (クールダウン等) は攻撃ごとに定義する。
/// 効果の中身は <see cref="Activate"/> のオーバーライドが持ち、
/// SpecialAttackState は UseDelay 経過時に一度だけ呼び出す。
/// </summary>
public abstract class SpecialAttackDefinition : AttackDefinition
{
    /// <summary>特殊攻撃による回復ゲージ蓄積の倍率 (近接=1 に対して半分)。</summary>
    public const float HealChargeMultiplier = 0.5f;

    [Header("使用モーション")]
    [Tooltip("使用モーションの継続時間 (sec)")]
    [SerializeField] private float _useDuration = 0.3f;

    [Tooltip("モーション開始から効果発動までの時間 (sec)")]
    [SerializeField] private float _useDelay = 0.1f;

    [Header("コスト")]
    [Tooltip("再使用までのクールダウン (sec)。0 で連発可")]
    [SerializeField] private float _cooldown = 0.8f;

    public float UseDuration => _useDuration;
    public float UseDelay => _useDelay;
    public float Cooldown => _cooldown;

    /// <summary>
    /// 効果を発動する (SpecialAttackState が UseDelay 経過時に呼ぶ)。
    /// origin は壁めり込み補正済みの発射位置、facing は向き (1=右, -1=左)。
    /// </summary>
    public abstract void Activate(PlayerController player, Vector2 origin, int facing);
}
