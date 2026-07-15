using UnityEngine;

/// <summary>
/// 1回の攻撃で与えるダメージ情報。
/// HP(赤ゲージ)と防御値(白ゲージ)の二層ダメージを持ち、
/// 二刀流は HP 寄り・両手持ちは防御値寄りの配分になる。
/// 敵からプレイヤーへの攻撃は HpDamage のみを使用する(プレイヤーに防御値はない)。
/// </summary>
public readonly struct DamageInfo
{
    /// <summary>HP(赤ゲージ)へのダメージ。</summary>
    public readonly int HpDamage;

    /// <summary>防御値(白ゲージ)へのダメージ。防御値を持たない対象は無視してよい。</summary>
    public readonly int GuardDamage;

    /// <summary>被弾位置 (ノックバックやエフェクト用)。</summary>
    public readonly Vector2 HitPoint;

    /// <summary>攻撃元の GameObject。</summary>
    public readonly GameObject Source;

    /// <summary>フィニッシャー「裁断」によるダメージか。ブレイク中の敵にのみ有効。</summary>
    public readonly bool IsFinisher;

    /// <summary>ノックバック強度の上書き (0 なら受け手のデフォルト値)。布カッターなどが使う。</summary>
    public readonly float KnockbackPower;

    public DamageInfo(int hpDamage, int guardDamage, Vector2 hitPoint, GameObject source,
        bool isFinisher = false, float knockbackPower = 0f)
    {
        HpDamage = hpDamage;
        GuardDamage = guardDamage;
        HitPoint = hitPoint;
        Source = source;
        IsFinisher = isFinisher;
        KnockbackPower = knockbackPower;
    }
}
