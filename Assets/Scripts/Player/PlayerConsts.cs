　using UnityEngine;

/// <summary>
/// プレイヤーの挙動を決める定数群。
/// ScriptableObject として Asset 化しておき、Editor 拡張(PlayerConstsEditor)から調整する。
/// 値の取得はプロパティ経由で行い、フィールドは Inspector からのみ書き換える想定。
/// </summary>
[CreateAssetMenu(fileName = "PlayerConsts", menuName = "NeverNight/Player Consts", order = 0)]
public class PlayerConsts : ScriptableObject
{
    #region Serialized Fields

    [Header("水平移動")]
    [Tooltip("通常移動速度 (units/sec)。慣性・補間なしで即時反映される")]
    [SerializeField] private float _moveSpeed = 8f;

    [Tooltip("ダッシュ後に維持される走り速度 (units/sec)")]
    [SerializeField] private float _runSpeed = 12f;

    [Header("ダッシュ")]
    [Tooltip("ダッシュ中の速度 (units/sec)。向いている方向へ飛び出す")]
    [SerializeField] private float _dashSpeed = 22f;

    [Tooltip("ダッシュの継続時間 (sec)")]
    [SerializeField] private float _dashDuration = 0.18f;

    [Tooltip("ダッシュの再使用までのクールダウン (sec)")]
    [SerializeField] private float _dashCooldown = 0.4f;

    [Header("攻撃")]
    [Tooltip("攻撃モーションの継続時間 (sec)")]
    [SerializeField] private float _attackDuration = 0.35f;

    [Tooltip("攻撃モーション開始から当たり判定が出るまでの時間 (sec)")]
    [SerializeField] private float _attackHitDelay = 0.1f;

    [Tooltip("攻撃の与ダメージ")]
    [SerializeField] private int _attackDamage = 1;

    [Tooltip("攻撃判定ボックスの中心オフセット (向き方向 x, 縦 y)")]
    [SerializeField] private Vector2 _attackOffset = new Vector2(0.7f, 0f);

    [Tooltip("攻撃判定ボックスのサイズ")]
    [SerializeField] private Vector2 _attackBoxSize = new Vector2(1.2f, 1f);

    [Tooltip("攻撃が当たる対象のレイヤー")]
    [SerializeField] private LayerMask _attackTargetLayer = 0;

    [Header("ジャンプ")]
    [Tooltip("ジャンプの最高到達高さ (units)")]
    [SerializeField] private float _jumpHeight = 3.5f;

    [Tooltip("ジャンプ開始から頂点に達するまでの時間 (sec)")]
    [SerializeField] private float _timeToApex = 0.4f;

    [Tooltip("落下中に重力へ掛ける倍率 (1 より大きいほどキビキビ落ちる)")]
    [SerializeField] private float _fallGravityMultiplier = 1.8f;

    [Tooltip("ジャンプボタンを離した時に重力へ掛ける倍率 (可変ジャンプ高さ)")]
    [SerializeField] private float _lowJumpMultiplier = 2.5f;

    [Tooltip("落下速度の上限 (units/sec)")]
    [SerializeField] private float _maxFallSpeed = 20f;

    [Header("ジャンプ補助")]
    [Tooltip("地面を離れてからジャンプ入力を受け付ける猶予 (sec)")]
    [SerializeField] private float _coyoteTime = 0.1f;

    [Tooltip("着地前にジャンプ入力を先行入力として保持する時間 (sec)")]
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("接地判定")]
    [Tooltip("接地判定を行う円の半径 (units)")]
    [SerializeField] private float _groundCheckRadius = 0.2f;

    [Tooltip("接地と見なすレイヤー")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    #endregion

    #region Properties

    public float MoveSpeed => _moveSpeed;
    public float RunSpeed => _runSpeed;

    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float DashCooldown => _dashCooldown;

    public float AttackDuration => _attackDuration;
    public float AttackHitDelay => _attackHitDelay;
    public int AttackDamage => _attackDamage;
    public Vector2 AttackOffset => _attackOffset;
    public Vector2 AttackBoxSize => _attackBoxSize;
    public LayerMask AttackTargetLayer => _attackTargetLayer;

    public float JumpHeight => _jumpHeight;
    public float TimeToApex => _timeToApex;
    public float FallGravityMultiplier => _fallGravityMultiplier;
    public float LowJumpMultiplier => _lowJumpMultiplier;
    public float MaxFallSpeed => _maxFallSpeed;

    public float CoyoteTime => _coyoteTime;
    public float JumpBufferTime => _jumpBufferTime;

    public float GroundCheckRadius => _groundCheckRadius;
    public LayerMask GroundLayer => _groundLayer;

    /// <summary>
    /// jumpHeight と timeToApex から逆算した基準重力加速度 (正の値)。
    /// gravity = 2 * height / apex^2
    /// </summary>
    public float Gravity
    {
        get
        {
            // ゼロ除算を避ける
            var apex = Mathf.Max(_timeToApex, 0.0001f);
            return (2f * _jumpHeight) / (apex * apex);
        }
    }

    /// <summary>
    /// 頂点に到達させるための初速 (正の値)。
    /// v0 = gravity * timeToApex
    /// </summary>
    public float JumpVelocity => Gravity * _timeToApex;

    #endregion
}
