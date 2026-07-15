using UnityEngine;

/// <summary>
/// プレイヤーの挙動を決める定数群。
/// ScriptableObject として Asset 化しておき、Editor 拡張(PlayerConstsEditor)から調整する。
/// 値の取得はプロパティ経由で行い、フィールドは Inspector からのみ書き換える想定。
/// </summary>
[CreateAssetMenu(fileName = "PlayerConsts", menuName = "NeverNight/Player Consts", order = 0)]
public class PlayerConsts : ScriptableObject
{
    /// <summary>
    /// 攻撃1種類分のパラメータ。スタイル別攻撃・切り替え攻撃・裁断が共有する形式。
    /// HP/防御値のダメージ配分で「二刀流=HP寄り、両手持ち=防御値寄り」を表現する。
    /// </summary>
    [System.Serializable]
    public class AttackProfile
    {
        [Tooltip("攻撃モーションの継続時間 (sec)")]
        [SerializeField] private float _duration = 0.35f;

        [Tooltip("攻撃モーション開始から当たり判定が出るまでの時間 (sec)")]
        [SerializeField] private float _hitDelay = 0.1f;

        [Tooltip("HP(赤ゲージ)への与ダメージ")]
        [SerializeField] private int _hpDamage = 1;

        [Tooltip("防御値(白ゲージ)への与ダメージ")]
        [SerializeField] private int _guardDamage = 1;

        [Tooltip("攻撃判定ボックスの中心オフセット (向き方向 x, 縦 y)")]
        [SerializeField] private Vector2 _offset = new Vector2(0.7f, 0f);

        [Tooltip("攻撃判定ボックスのサイズ")]
        [SerializeField] private Vector2 _boxSize = new Vector2(1.2f, 1f);

        public float Duration => _duration;
        public float HitDelay => _hitDelay;
        public int HpDamage => _hpDamage;
        public int GuardDamage => _guardDamage;
        public Vector2 Offset => _offset;
        public Vector2 BoxSize => _boxSize;

        public AttackProfile(float duration, float hitDelay, int hpDamage, int guardDamage,
            Vector2 offset, Vector2 boxSize)
        {
            _duration = duration;
            _hitDelay = hitDelay;
            _hpDamage = hpDamage;
            _guardDamage = guardDamage;
            _offset = offset;
            _boxSize = boxSize;
        }
    }

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

    [Header("攻撃 (スタイル別)")]
    [Tooltip("二刀流の通常攻撃。速い・HP(赤)削り向け")]
    [SerializeField] private AttackProfile _dualAttack =
        new AttackProfile(0.25f, 0.06f, 2, 1, new Vector2(0.7f, 0f), new Vector2(1.2f, 1f));

    [Tooltip("両手持ちの通常攻撃。遅い・防御値(白)削り向け")]
    [SerializeField] private AttackProfile _heavyAttack =
        new AttackProfile(0.5f, 0.2f, 1, 3, new Vector2(0.8f, 0f), new Vector2(1.5f, 1.4f));

    [Tooltip("切り替え攻撃: 分割時の一閃。横に広く発生が速い・HP寄り")]
    [SerializeField] private AttackProfile _splitSwitchAttack =
        new AttackProfile(0.3f, 0.05f, 2, 1, new Vector2(0.9f, 0f), new Vector2(2.2f, 0.8f));

    [Tooltip("切り替え攻撃: 合体時の振り下ろし。範囲は狭いが防御値削り向け")]
    [SerializeField] private AttackProfile _mergeSwitchAttack =
        new AttackProfile(0.4f, 0.15f, 1, 3, new Vector2(0.7f, 0.2f), new Vector2(1f, 1.6f));

    [Tooltip("攻撃が当たる対象のレイヤー")]
    [SerializeField] private LayerMask _attackTargetLayer = 0;

    [Header("フィニッシャー「裁断」")]
    [Tooltip("裁断の攻撃パラメータ。ブレイク中の敵に大ダメージ")]
    [SerializeField] private AttackProfile _finisherProfile =
        new AttackProfile(0.7f, 0.25f, 10, 0, new Vector2(0.9f, 0f), new Vector2(1.8f, 1.6f));

    [Tooltip("裁断を発動できる範囲 (プレイヤー中心のボックスサイズ)。x = 横の射程、y = 高さの許容量。" +
             "この中にいるブレイク中の敵のうち一番近いものが標的になる")]
    [SerializeField] private Vector2 _finisherRange = new Vector2(5f, 2.4f);

    [Header("体力・被弾")]
    [Tooltip("最大HP")]
    [SerializeField] private int _maxHp = 10;

    [Tooltip("被弾硬直の時間 (sec)")]
    [SerializeField] private float _hurtDuration = 0.3f;

    [Tooltip("被弾後の無敵時間 (sec)")]
    [SerializeField] private float _invincibleTime = 1f;

    [Tooltip("被弾時のノックバック初速 (x は攻撃元と反対方向に適用)")]
    [SerializeField] private Vector2 _knockbackVelocity = new Vector2(8f, 6f);

    [Tooltip("死亡からリスポーン(シーン再読込)までの時間 (sec)")]
    [SerializeField] private float _respawnDelay = 1.5f;

    [Header("回復")]
    [Tooltip("回復ゲージのメモリ数 (1ゲージ = 3メモリ)")]
    [SerializeField] private int _healGaugeMax = 3;

    [Tooltip("攻撃1ヒットで蓄積するメモリの割合 (1 でメモリ1つ分)")]
    [SerializeField] private float _healChargePerHit = 0.25f;

    [Tooltip("メモリ1消費あたりの HP 回復量")]
    [SerializeField] private int _healAmount = 3;

    [Tooltip("回復モーションの継続時間 (sec)。この間は移動不可")]
    [SerializeField] private float _healDuration = 0.5f;

    [Header("インタラクト")]
    [Tooltip("インタラクト対象を検出する半径 (units)")]
    [SerializeField] private float _interactRadius = 1.5f;

    [Tooltip("インタラクト対象のレイヤー")]
    [SerializeField] private LayerMask _interactableLayer = 0;

    // アイテムの個別パラメータは ItemDefinition (Assets/Scripts/Items/) が持つ

    [Header("拠点")]
    [Tooltip("拠点でのアイテム補充 (再生成) に必要な糸の数")]
    [SerializeField] private int _refillThreadCost = 3;

    [Header("ハサミ強化: 黄 (斬撃波)")]
    [Tooltip("斬撃波の HP ダメージ")]
    [SerializeField] private int _slashWaveHpDamage = 1;

    [Tooltip("斬撃波の防御値ダメージ")]
    [SerializeField] private int _slashWaveGuardDamage = 1;

    [Tooltip("斬撃波の速度 (units/sec)")]
    [SerializeField] private float _slashWaveSpeed = 13f;

    [Tooltip("斬撃波の寿命 (sec)。速度×寿命が射程になる")]
    [SerializeField] private float _slashWaveLifetime = 0.7f;

    [Header("ハサミ強化: 青 (糸移動)")]
    [Tooltip("グラップルの射程 (units)")]
    [SerializeField] private float _grappleRange = 9f;

    [Tooltip("グラップル移動の速度 (units/sec)")]
    [SerializeField] private float _grappleSpeed = 22f;

    [Header("ハサミ強化: 赤 (滑空)")]
    [Tooltip("滑空中の落下速度上限 (units/sec)。落下中にジャンプ長押しで滑空")]
    [SerializeField] private float _glideFallSpeed = 2.5f;

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

    public AttackProfile DualAttack => _dualAttack;
    public AttackProfile HeavyAttack => _heavyAttack;
    public AttackProfile SplitSwitchAttack => _splitSwitchAttack;
    public AttackProfile MergeSwitchAttack => _mergeSwitchAttack;
    public LayerMask AttackTargetLayer => _attackTargetLayer;

    public AttackProfile FinisherProfile => _finisherProfile;
    public Vector2 FinisherRange => _finisherRange;

    public int MaxHp => _maxHp;
    public float HurtDuration => _hurtDuration;
    public float InvincibleTime => _invincibleTime;
    public Vector2 KnockbackVelocity => _knockbackVelocity;
    public float RespawnDelay => _respawnDelay;

    public int HealGaugeMax => _healGaugeMax;
    public float HealChargePerHit => _healChargePerHit;
    public int HealAmount => _healAmount;
    public float HealDuration => _healDuration;

    public float InteractRadius => _interactRadius;
    public LayerMask InteractableLayer => _interactableLayer;

    public int RefillThreadCost => _refillThreadCost;

    public int SlashWaveHpDamage => _slashWaveHpDamage;
    public int SlashWaveGuardDamage => _slashWaveGuardDamage;
    public float SlashWaveSpeed => _slashWaveSpeed;
    public float SlashWaveLifetime => _slashWaveLifetime;

    public float GrappleRange => _grappleRange;
    public float GrappleSpeed => _grappleSpeed;

    public float GlideFallSpeed => _glideFallSpeed;

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
