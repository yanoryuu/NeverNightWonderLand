using UnityEngine;

/// <summary>
/// 糸玉: 曲射で投げ、当たった敵を糸で絡めて数秒拘束する (搦め手)。
/// </summary>
[CreateAssetMenu(fileName = "ThreadBall", menuName = "NeverNight/Items/糸玉")]
public class ThreadBallItem : ItemDefinition
{
    [Header("糸玉")]
    [Tooltip("弾の Prefab")]
    [SerializeField] private Projectile _projectilePrefab;

    [Tooltip("投擲初速 (units/sec)")]
    [SerializeField] private float _speed = 9f;

    [Tooltip("投擲角度 (度)")]
    [SerializeField] private float _launchAngle = 30f;

    [Tooltip("曲射の重力スケール")]
    [SerializeField] private float _gravityScale = 2f;

    [Tooltip("HP ダメージ")]
    [SerializeField] private int _hpDamage = 1;

    [Tooltip("当たった敵を拘束する時間 (sec)")]
    [SerializeField] private float _bindDuration = 2.5f;

    public override ItemUseMotion Motion => ItemUseMotion.Throw;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_projectilePrefab == null)
            return;

        var consts = player.Consts;
        var ball = Instantiate(_projectilePrefab, origin, Quaternion.identity);
        ball.Launch(
            ArcVelocity(_speed, _launchAngle, facing),
            gravityScale: _gravityScale,
            lifetime: 4f,
            hpDamage: _hpDamage,
            guardDamage: 0,
            source: player.gameObject,
            stickAsPlatform: false,
            damageLayer: consts.AttackTargetLayer,
            groundLayer: consts.GroundLayer,
            bindDuration: _bindDuration);
    }
}
