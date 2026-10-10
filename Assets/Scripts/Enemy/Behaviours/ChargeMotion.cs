using UnityEngine;

/// <summary>
/// 移動部品: 地上の突進 (ウマ / Charger)。
/// 正面・ほぼ同じ高さ・一定距離内でプレイヤーを見つけると
/// 予備動作 (その場で点滅) → 向いている方向へ突進 → 壁に当たったらふらつく / 当たらず終わったら減速して停止
/// → クールダウン、の順に動く。クールダウン中・未発見の間は IsActive が false になり、
/// 下に置いた巡回 (PatrolMotion) に切り替わる。発見は自前の条件で行うので EnemyPerception は不要。
/// 突進中はプレイヤーへ <see cref="_chargeDamage"/> を直接与える (コアの接触ダメージより先に入り、
/// プレイヤーの無敵時間で接触ダメージ 1 は弾かれる → 突進中だけ実質 2)。
/// 被弾・拘束・ブレイクで中断され、状態はリセットされる (クールダウンから再開)。
/// Animator トリガー (任意・無ければ無視): 予備動作 / 突進 / ふらつき / 突進の終わり。
/// </summary>
public class ChargeMotion : EnemyMotionBase
{
    private enum State
    {
        Idle,
        Windup,  // 予備動作 (前かき・点滅)
        Charge,  // 突進
        Stagger, // 壁に当たってふらつく (無防備)
        Brake,   // 壁に当たらず終わった → 減速して停止
    }

    #region Serialized Fields

    [Header("発見")]
    [Tooltip("突進を始める距離 (正面方向の水平距離, units)")]
    [SerializeField] private float _detectRange = 8f;

    [Tooltip("突進を始める縦の差の上限 (units)。ほぼ同じ高さの時だけ突進する")]
    [SerializeField] private float _maxHeightDiff = 1.5f;

    [Tooltip("地形に遮られていない時だけ突進する")]
    [SerializeField] private bool _requireLineOfSight = true;

    [Header("突進")]
    [Tooltip("予備動作の時間 (sec)")]
    [SerializeField] private float _windupTime = 0.6f;

    [Tooltip("突進の速さ (units/sec)")]
    [SerializeField] private float _chargeSpeed = 12f;

    [Tooltip("突進の最大時間 (sec)")]
    [SerializeField] private float _maxChargeTime = 1.2f;

    [Tooltip("突進中にプレイヤーへ与えるダメージ (通常の接触ダメージの代わり)")]
    [SerializeField] private int _chargeDamage = 2;

    [Tooltip("足場の端で突進を止める (落ちない)")]
    [SerializeField] private bool _stopAtLedge = true;

    [Header("突進のあと")]
    [Tooltip("壁に当たった時にふらつく時間 (sec)")]
    [SerializeField] private float _staggerTime = 1.0f;

    [Tooltip("壁に当たらず終わった時の減速時間 (sec)")]
    [SerializeField] private float _brakeTime = 0.4f;

    [Tooltip("次に突進できるまでの時間 (sec)。突進の終わり (ふらつき・減速の後) または中断から数える")]
    [SerializeField] private float _cooldown = 1.5f;

    [Header("地形")]
    [Tooltip("地面・壁のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("見た目 (予備動作の点滅)")]
    [Tooltip("点滅させる SpriteRenderer。未設定なら本体の絵を自動で探す")]
    [SerializeField] private SpriteRenderer _blinkRenderer;

    [Tooltip("点滅の色")]
    [SerializeField] private Color _blinkColor = new Color(1f, 0.45f, 0.45f);

    [Tooltip("点滅の間隔 (sec)")]
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Animator トリガー (無ければ無視される)")]
    [SerializeField] private string _windupTrigger = "ChargeWindup";
    [SerializeField] private string _chargeTrigger = "Charge";
    [SerializeField] private string _staggerTrigger = "ChargeStagger";
    [SerializeField] private string _endTrigger = "ChargeEnd";

    #endregion

    private Rigidbody2D _rb;
    private Collider2D _body;
    private EnemyTelegraphBlink _blink;
    private readonly PlayerHitProbe _hitProbe = new();

    private State _state = State.Idle;
    private float _timer;
    private float _readyTime;
    private int _facing = 1;
    private float _brakeFromSpeed;

    /// <summary>予備動作・突進・ふらつき・減速のいずれかの最中か。</summary>
    public bool IsBusy => _state != State.Idle;

    public override bool IsActive => _state != State.Idle || CanStart();

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _body = GetComponent<Collider2D>();
        _blink = new EnemyTelegraphBlink(EnemyTelegraphBlink.ResolveVisual(this, _blinkRenderer), _blinkColor, _blinkInterval);
    }

    private void Start()
    {
        if (Enemy != null)
            Enemy.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (Enemy != null)
            Enemy.OnDied -= HandleDied;
    }

    #endregion

    public override void PhysicsTick()
    {
        var dt = Time.fixedDeltaTime;
        _timer += dt;
        var ground = EnemyMotionUtil.GroundOrDefault(_groundLayer);

        switch (_state)
        {
            case State.Idle:
                // CanStart が通った最初のステップ
                BeginWindup();
                Enemy.Move(0f);
                break;

            case State.Windup:
                Enemy.Move(0f);
                _blink.Tick(dt);
                if (_timer >= _windupTime)
                    BeginCharge();
                break;

            case State.Charge:
                if (_timer > dt * 1.5f && Enemy.HitWallLastStep)
                {
                    BeginStagger();
                    break;
                }

                if (_stopAtLedge && EnemyMotionUtil.IsGrounded(_body, ground)
                    && !EnemyMotionUtil.HasGroundAhead(_body, _facing, ground))
                {
                    // 足場の端: その場で止まって減速の間を置く
                    Enemy.Move(0f);
                    BeginBrake(0f);
                    break;
                }

                if (_timer >= _maxChargeTime)
                {
                    BeginBrake(_chargeSpeed);
                    break;
                }

                Enemy.Move(_facing * _chargeSpeed);
                _hitProbe.TryHit(Player, _body.bounds, _chargeDamage, transform.position, gameObject);
                break;

            case State.Stagger:
                Enemy.Move(0f);
                if (_timer >= _staggerTime)
                    Finish();
                break;

            case State.Brake:
                var t = _brakeTime > 0f ? Mathf.Clamp01(_timer / _brakeTime) : 1f;
                var speed = Mathf.Lerp(_brakeFromSpeed, 0f, t);
                if (_stopAtLedge && EnemyMotionUtil.IsGrounded(_body, ground)
                    && !EnemyMotionUtil.HasGroundAhead(_body, _facing, ground))
                    speed = 0f;
                Enemy.Move(_facing * speed);
                if (t >= 1f)
                    Finish();
                break;
        }
    }

    public override void OnInterrupted()
    {
        // 被弾で予備動作・突進は中断。クールダウンから再開する
        if (_state != State.Idle)
            ResetState(true);
    }

    #region State

    /// <summary>正面・ほぼ同じ高さ・距離内にプレイヤーがいて、クールダウンが明けているか。</summary>
    private bool CanStart()
    {
        // 巡回中の進行方向を「正面」として覚えておく
        if (_rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
            _facing = _rb.linearVelocity.x > 0f ? 1 : -1;

        if (Player == null || Enemy.IsDead || Time.time < _readyTime)
            return false;

        var self = (Vector2)transform.position;
        var target = _hitProbe.GetCenter(Player);
        var dx = target.x - self.x;
        var feetDy = Player.position.y - self.y;

        if (dx * _facing <= 0f || Mathf.Abs(dx) > _detectRange || Mathf.Abs(feetDy) > _maxHeightDiff)
            return false;

        var ground = EnemyMotionUtil.GroundOrDefault(_groundLayer);
        if (!EnemyMotionUtil.IsGrounded(_body, ground))
            return false;

        if (_requireLineOfSight && !EnemyMotionUtil.HasLineOfSight(_body.bounds.center, target, ground))
            return false;

        return true;
    }

    private void BeginWindup()
    {
        _state = State.Windup;
        _timer = 0f;
        EnemyMotionUtil.FaceToward(Enemy, _facing);
        _blink.Start();
        Animation?.SetTrigger(_windupTrigger);
    }

    private void BeginCharge()
    {
        _state = State.Charge;
        _timer = 0f;
        _blink.Stop();
        Animation?.SetTrigger(_chargeTrigger);
        Enemy.Move(_facing * _chargeSpeed);
    }

    private void BeginStagger()
    {
        _state = State.Stagger;
        _timer = 0f;
        Enemy.Move(0f);
        Animation?.SetTrigger(_staggerTrigger);
    }

    private void BeginBrake(float fromSpeed)
    {
        _state = State.Brake;
        _timer = 0f;
        _brakeFromSpeed = fromSpeed;
        Animation?.SetTrigger(_endTrigger);
    }

    private void Finish()
    {
        _state = State.Idle;
        _timer = 0f;
        _readyTime = Time.time + _cooldown;
    }

    private void ResetState(bool startCooldown)
    {
        _blink.Stop();
        _state = State.Idle;
        _timer = 0f;
        if (startCooldown)
            _readyTime = Time.time + _cooldown;
    }

    private void HandleDied(EnemyController _) => ResetState(false);

    #endregion

    private void OnDrawGizmosSelected()
    {
        var origin = (Vector2)transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(origin, new Vector2(_detectRange * 2f, _maxHeightDiff * 2f));
    }
}
