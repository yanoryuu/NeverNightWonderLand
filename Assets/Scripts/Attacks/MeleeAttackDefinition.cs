using UnityEngine;

/// <summary>
/// 近接攻撃の定義 (□ボタン)。攻撃判定のパラメータは既存の
/// <see cref="PlayerConsts.AttackProfile"/> をそのまま持ち、AttackState が参照する。
/// HP(赤)寄り/防御値(白)寄りなどの性格付けはアセットごとの数値配分で表現する。
/// </summary>
[CreateAssetMenu(fileName = "MeleeAttack", menuName = "NeverNight/Attacks/Melee Attack")]
public class MeleeAttackDefinition : AttackDefinition
{
    [Header("攻撃判定")]
    [Tooltip("攻撃判定のパラメータ (継続時間・発生・ダメージ配分・判定ボックス)")]
    [SerializeField] private PlayerConsts.AttackProfile _profile =
        new(0.25f, 0.06f, 2, 1, new Vector2(0.7f, 0f), new Vector2(1.2f, 1f));

    public PlayerConsts.AttackProfile Profile => _profile;
}
