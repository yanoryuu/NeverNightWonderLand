using UnityEngine;

/// <summary>
/// ミシン針: 曲射で投げる。威力が高い。
/// </summary>
[CreateAssetMenu(fileName = "MishinNeedle", menuName = "NeverNight/Items/ミシン針")]
public class MishinNeedleItem : ItemDefinition
{
    [Header("ミシン針")]
    [Tooltip("弾の Prefab")]
    [SerializeField] private Projectile _projectilePrefab;

    [Tooltip("投擲初速 (units/sec)")]
    [SerializeField] private float _speed = 11f;

    [Tooltip("投擲角度 (度)")]
    [SerializeField] private float _launchAngle = 40f;

    [Tooltip("曲射の重力スケール")]
    [SerializeField] private float _gravityScale = 2.5f;

    [Tooltip("HP ダメージ (高威力)")]
    [SerializeField] private int _hpDamage = 4;

    [Tooltip("防御値ダメージ")]
    [SerializeField] private int _guardDamage = 2;

    public override ItemUseMotion Motion => ItemUseMotion.Throw;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_projectilePrefab == null)
            return;

        var consts = player.Consts;
        var needle = Instantiate(_projectilePrefab, origin, Quaternion.identity);
        needle.Launch(
            ArcVelocity(_speed, _launchAngle, facing),
            gravityScale: _gravityScale,
            lifetime: 4f,
            hpDamage: _hpDamage,
            guardDamage: _guardDamage,
            source: player.gameObject,
            stickAsPlatform: false,
            damageLayer: consts.AttackTargetLayer,
            groundLayer: consts.GroundLayer);
    }
}
