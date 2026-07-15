using UnityEngine;

/// <summary>
/// ボビン爆弾: 曲射で投げ、時限で爆発して範囲内の防御値を大きく削る (ブレイク支援)。
/// </summary>
[CreateAssetMenu(fileName = "BobbinBomb", menuName = "NeverNight/Items/ボビン爆弾")]
public class BobbinBombItem : ItemDefinition
{
    [Header("ボビン爆弾")]
    [Tooltip("爆弾の Prefab")]
    [SerializeField] private BobbinBomb _bombPrefab;

    [Tooltip("投擲初速 (units/sec)")]
    [SerializeField] private float _speed = 8f;

    [Tooltip("投擲角度 (度)")]
    [SerializeField] private float _launchAngle = 40f;

    [Tooltip("曲射の重力スケール")]
    [SerializeField] private float _gravityScale = 2.5f;

    [Tooltip("投げてから爆発するまでの時間 (sec)")]
    [SerializeField] private float _fuse = 1.5f;

    [Tooltip("HP ダメージ")]
    [SerializeField] private int _hpDamage = 2;

    [Tooltip("防御値ダメージ (ブレイク支援)")]
    [SerializeField] private int _guardDamage = 6;

    [Tooltip("爆発の半径 (units)")]
    [SerializeField] private float _radius = 2.2f;

    public override ItemUseMotion Motion => ItemUseMotion.Throw;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_bombPrefab == null)
            return;

        var bomb = Instantiate(_bombPrefab, origin, Quaternion.identity);
        bomb.Launch(
            ArcVelocity(_speed, _launchAngle, facing),
            gravityScale: _gravityScale,
            fuse: _fuse,
            hpDamage: _hpDamage,
            guardDamage: _guardDamage,
            radius: _radius,
            source: player.gameObject,
            damageLayer: player.Consts.AttackTargetLayer);
    }
}
