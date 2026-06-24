using UnityEngine;

/// <summary>
/// ダメージを受けられるオブジェクトが実装するインターフェース。
/// プレイヤーの攻撃判定はこのインターフェースを介してダメージを与える。
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ダメージを与える。
    /// </summary>
    /// <param name="amount">ダメージ量</param>
    /// <param name="hitPoint">被弾位置 (ノックバックやエフェクト用)</param>
    /// <param name="source">攻撃元の GameObject</param>
    void TakeDamage(int amount, Vector2 hitPoint, GameObject source);
}
