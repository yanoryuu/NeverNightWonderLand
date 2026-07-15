using UnityEngine;

/// <summary>
/// 針山 (ピンクッション): 足元前方の地面に設置するトラップ。上の敵に一定間隔でダメージ。
/// </summary>
[CreateAssetMenu(fileName = "PinCushion", menuName = "NeverNight/Items/針山")]
public class PinCushionItem : ItemDefinition
{
    [Header("針山")]
    [Tooltip("トラップの Prefab")]
    [SerializeField] private PinCushionTrap _trapPrefab;

    [Tooltip("設置位置 (プレイヤーからの前方距離)")]
    [SerializeField] private float _placeDistance = 1.2f;

    [Tooltip("1ヒットあたりの HP ダメージ")]
    [SerializeField] private int _hpDamage = 1;

    [Tooltip("ダメージの間隔 (sec)")]
    [SerializeField] private float _tickInterval = 0.4f;

    [Tooltip("設置してから消えるまでの時間 (sec)")]
    [SerializeField] private float _lifetime = 6f;

    public override ItemUseMotion Motion => ItemUseMotion.Throw;

    public override void Activate(PlayerController player, Vector2 origin, int facing)
    {
        if (_trapPrefab == null)
            return;

        var consts = player.Consts;

        // 足元前方の地面の上に設置する
        var placePos = (Vector2)player.transform.position + new Vector2(facing * _placeDistance, 0.5f);
        var ground = Physics2D.Raycast(placePos, Vector2.down, 3f, consts.GroundLayer);
        if (ground.collider != null)
            placePos = ground.point + Vector2.up * 0.15f;

        var trap = Instantiate(_trapPrefab, placePos, Quaternion.identity);
        trap.Place(
            hpDamage: _hpDamage,
            tickInterval: _tickInterval,
            lifetime: _lifetime,
            source: player.gameObject,
            damageLayer: consts.AttackTargetLayer);
    }
}
