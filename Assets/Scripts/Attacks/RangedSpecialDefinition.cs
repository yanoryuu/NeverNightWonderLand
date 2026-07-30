using UnityEngine;

/// <summary>
/// 遠距離型の特殊攻撃。前方へ直進する弾を発射する。
/// 消費アイテム (まち針等) との住み分けは「無限に使えるが低威力+クールダウン」。
/// ヒット時の回復ゲージ蓄積は近接の半分 (HealChargeMultiplier)。
/// </summary>
[CreateAssetMenu(fileName = "RangedSpecial", menuName = "NeverNight/Attacks/Ranged Special")]
public class RangedSpecialDefinition : SpecialAttackDefinition
{
    [Header("弾")]
    [Tooltip("発射する弾の Prefab")]
    [SerializeField] private Projectile _projectilePrefab;

    [Tooltip("弾速 (units/sec)。直進する")]
    [SerializeField] private float _speed = 14f;

    [Tooltip("弾の寿命 (sec)。速度×寿命が射程になる")]
    [SerializeField] private float _lifetime = 0.6f;

    [Tooltip("HP(赤)への与ダメージ")]
    [SerializeField] private int _hpDamage = 1;

    [Tooltip("防御値(白)への与ダメージ")]
    [SerializeField] private int _guardDamage = 2;

    [Tooltip("発射高さのオフセット (プレイヤー中心基準)。0 = 腰の高さで、床上の小型敵 (高さ1) に当たる")]
    [SerializeField] private float _fireHeightOffset = 0f;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_projectilePrefab == null)
        {
            Debug.LogError($"[{nameof(RangedSpecialDefinition)}] {name} に弾 Prefab が設定されていません。", this);
            return;
        }

        // 既定の投擲位置 (+0.3) だと地上の小型敵の頭上を抜けてしまうため、直進弾は低めに撃つ
        origin = player.ComputeThrowOrigin(_fireHeightOffset);

        var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(
            new Vector2(facing * _speed, 0f),
            gravityScale: 0f,
            lifetime: _lifetime,
            hpDamage: _hpDamage,
            guardDamage: _guardDamage,
            source: player.gameObject,
            stickAsPlatform: false,
            damageLayer: player.Consts.AttackTargetLayer,
            groundLayer: player.Consts.GroundLayer,
            onDamageDealt: () => player.HealGauge?.AddCharge(HealChargeMultiplier));
    }
}
