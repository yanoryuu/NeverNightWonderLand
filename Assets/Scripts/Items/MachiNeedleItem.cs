using UnityEngine;

/// <summary>
/// まち針: 平行に飛び、壁に刺さると1回限りの足場になる。敵に当たるとダメージ。
/// </summary>
[CreateAssetMenu(fileName = "MachiNeedle", menuName = "NeverNight/Items/まち針")]
public class MachiNeedleItem : ItemDefinition
{
    [Header("まち針")]
    [Tooltip("弾の Prefab")]
    [SerializeField] private Projectile _projectilePrefab;

    [Tooltip("飛行速度 (units/sec)")]
    [SerializeField] private float _speed = 14f;

    [Tooltip("寿命 (sec)。壁に刺さった場合は踏まれるまで残る")]
    [SerializeField] private float _lifetime = 3f;

    [Tooltip("HP ダメージ")]
    [SerializeField] private int _hpDamage = 1;

    public override ItemUseMotion Motion => ItemUseMotion.Throw;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_projectilePrefab == null)
            return;

        var consts = player.Consts;
        var needle = Instantiate(_projectilePrefab, origin, Quaternion.identity);
        needle.Launch(
            new Vector2(facing * _speed, 0f),
            gravityScale: 0f,
            lifetime: _lifetime,
            hpDamage: _hpDamage,
            guardDamage: 0,
            source: player.gameObject,
            stickAsPlatform: true,
            damageLayer: consts.AttackTargetLayer,
            groundLayer: consts.GroundLayer);
    }
}
