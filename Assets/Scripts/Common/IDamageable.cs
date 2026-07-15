/// <summary>
/// ダメージを受けられるオブジェクトが実装するインターフェース。
/// プレイヤーの攻撃判定・敵の接触攻撃はこのインターフェースを介してダメージを与える。
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ダメージを与える。HP/防御値の配分は <see cref="DamageInfo"/> を参照。
    /// </summary>
    void TakeDamage(in DamageInfo info);
}
