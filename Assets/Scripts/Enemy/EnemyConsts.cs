using UnityEngine;

/// <summary>
/// 敵の挙動を決める定数群。PlayerConsts と同様に ScriptableObject として Asset 化して使う。
/// 敵は「防御値(白ゲージ)」と「HP(赤ゲージ)」の二層構造を持ち、
/// 防御値を削り切ると一定時間ブレイク(スタン・無防備)する。
/// </summary>
[CreateAssetMenu(fileName = "EnemyConsts", menuName = "NeverNight/Enemy Consts", order = 1)]
public class EnemyConsts : ScriptableObject
{
    #region Serialized Fields

    [Header("体力")]
    [Tooltip("最大HP (赤ゲージ)")]
    [SerializeField] private int _maxHp = 10;

    [Tooltip("最大防御値 (白ゲージ)。削り切るとブレイク")]
    [SerializeField] private int _maxGuard = 6;

    [Header("ブレイク")]
    [Tooltip("ブレイク(スタン)の継続時間 (sec)。明けると防御値は全回復する")]
    [SerializeField] private float _breakDuration = 3f;

    [Tooltip("ブレイク中に受ける HP ダメージの倍率")]
    [SerializeField] private float _breakHpDamageMultiplier = 1.5f;

    [Header("移動")]
    [Tooltip("巡回の移動速度 (units/sec)")]
    [SerializeField] private float _moveSpeed = 2f;

    [Tooltip("初期位置から左右へ巡回する幅 (units)")]
    [SerializeField] private float _patrolHalfWidth = 3f;

    [Header("攻撃 (接触)")]
    [Tooltip("プレイヤーに接触した時の与ダメージ")]
    [SerializeField] private int _contactDamage = 1;

    [Header("追跡 (俊敏型)")]
    [Tooltip("プレイヤーを追跡する検知範囲 (units)。0 で追跡しない (重装甲型など)")]
    [SerializeField] private float _chaseRange = 0f;

    [Tooltip("追跡時の移動速度 (units/sec)")]
    [SerializeField] private float _chaseSpeed = 0f;

    [Header("ドロップ")]
    [Tooltip("撃破時にドロップする素材「糸」の数")]
    [SerializeField] private int _threadDrop = 2;

    [Header("被弾")]
    [Tooltip("被弾時のノックバック初速 (units/sec)。攻撃元と反対方向へ")]
    [SerializeField] private float _knockbackOnHit = 3f;

    [Tooltip("被弾硬直の時間 (sec)")]
    [SerializeField] private float _hitStunTime = 0.15f;

    [Tooltip("被弾フラッシュの時間 (sec)")]
    [SerializeField] private float _hitFlashTime = 0.1f;

    [Header("死亡")]
    [Tooltip("死亡演出(フェード)の時間 (sec)")]
    [SerializeField] private float _deathFadeTime = 0.4f;

    #endregion

    #region Properties

    public int MaxHp => _maxHp;
    public int MaxGuard => _maxGuard;

    public float BreakDuration => _breakDuration;
    public float BreakHpDamageMultiplier => _breakHpDamageMultiplier;

    public float MoveSpeed => _moveSpeed;
    public float PatrolHalfWidth => _patrolHalfWidth;

    public int ContactDamage => _contactDamage;

    public float ChaseRange => _chaseRange;
    public float ChaseSpeed => _chaseSpeed;

    public int ThreadDrop => _threadDrop;

    public float KnockbackOnHit => _knockbackOnHit;
    public float HitStunTime => _hitStunTime;
    public float HitFlashTime => _hitFlashTime;

    public float DeathFadeTime => _deathFadeTime;

    #endregion
}
