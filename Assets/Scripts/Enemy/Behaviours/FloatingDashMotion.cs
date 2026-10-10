using UnityEngine;

/// <summary>
/// 移動部品: 浮遊しながらの突進 (飛んで突進してくる敵 / FlyingDasher)。
/// 発見中・距離内・視線が通っていてクールダウンが明けていると
/// 予備動作 (その場で少し後ろへ下がる+点滅) → 予備動作を始めた時のプレイヤー位置へ一直線に突進 (斜めも可)
/// → 減速して漂う (反撃のチャンス) / 壁・床に当たったらふらつく → クールダウン、の順に動く。
/// それ以外の間は IsActive が false になり、下に置いた FloatingChaseMotion (間合いを取る) や
/// FloatingPatrolMotion に切り替わる。
/// 突進中はプレイヤーへ <see cref="_dashDamage"/> を直接与える (コアの接触ダメージ 1 は無敵時間で弾かれる)。
/// 被弾・拘束・ブレイクで中断され、状態はリセットされる (クールダウンから再開)。
/// </summary>
public class FloatingDashMotion : FloatingMotionBase
{
    private enum State
    {
        Idle,
        Windup,  // 予備動作 (後ろへ下がる+点滅)
        Dash,    // 突進
        Drift,   // 突進後に減速して漂う
        Stagger, // 壁・床に当たってふらつく
    }

    #region Serialized Fields

    [Header("発動")]
    [Tooltip("突進を始める距離 (units)。発見中かつこの距離内で突進する")]
    [SerializeField] private float _triggerRange = 7f;

    [Tooltip("地形に遮られていない時だけ突進する")]
    [SerializeField] private bool _requireLineOfSight = true;

    [Header("予備動作")]
    [Tooltip("予備動作の時間 (sec)")]
    [SerializeField] private float _windupTime = 0.5f;

    [Tooltip("予備動作中に後ろへ下がる速さ (units/sec)")]
    [SerializeField] private float _windupBackSpeed = 1.5f;

    [Header("突進")]
    [Tooltip("突進の速さ (units/sec)")]
    [SerializeField] private float _dashSpeed = 14f;

    [Tooltip("突進の最大時間 (sec)")]
    [SerializeField] private float _maxDashTime = 0.6f;

    [Tooltip("突進中にプレイヤーへ与えるダメージ (通常の接触ダメージの代わり)")]
    [SerializeField] private int _dashDamage = 2;

    [Header("突進のあと")]
    [Tooltip("突進後に減速して漂う時間 (sec)")]
    [SerializeField] private float _driftTime = 0.8f;

    [Tooltip("壁・床に当たった時にふらつく時間 (sec)")]
    [SerializeField] private float _staggerTime = 1.0f;

    [Tooltip("次に突進できるまでの時間 (sec)。漂い・ふらつきの後 または中断から数える")]
    [SerializeField] private float _cooldown = 2f;

    [Header("地形")]
    [Tooltip("視線を遮る地形のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("見た目 (予備動作の点滅)")]
    [Tooltip("点滅させる SpriteRenderer。未設定なら本体の絵を自動で探す")]
    [SerializeField] private SpriteRenderer _blinkRenderer;

    [Tooltip("点滅の色")]
    [SerializeField] private Color _blinkColor = new Color(1f, 0.45f, 0.45f);

    [Tooltip("点滅の間隔 (sec)")]
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Animator トリガー (無ければ無視される)")]
    [SerializeField] private string _windupTrigger = "DashWindup";
    [SerializeField] private string _dashTrigger = "Dash";
    [SerializeField] private string _staggerTrigger = "DashStagger";
    [SerializeField] private string _endTrigger = "DashEnd";

    #endregion

    private Collider2D _body;
    private EnemyTelegraphBlink _blink;
    private readonly PlayerHitProbe _hitProbe = new();

    private State _state = State.Idle;
    private float _timer;
    private float _readyTime;
    private Vector2 _dashDir;
    private Vector2 _driftFrom;

    /// <summary>予備動作・突進・漂い・ふらつきのいずれかの最中か。</summary>
    public bool IsBusy => _state != State.Idle;

    public override bool IsActive => _state != State.Idle || CanStart();

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
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

        switch (_state)
        {
            case State.Idle:
                // CanStart が通った最初のステップ
                BeginWindup();
                break;

            case State.Windup:
                // 向きは変えずに、突進方向の逆へ少し下がる
                Rb.linearVelocity = -_dashDir * _windupBackSpeed;
                _blink.Tick(dt);
                if (_timer >= _windupTime)
                    BeginDash();
                break;

            case State.Dash:
                // 壁・床に当たった (進行方向の速度が削られた)。浅い角度で床をこすった時も
                // 進行方向の成分が 1 割以上落ちるので、0.9 倍をしきい値にする (0.5 倍だと床を滑り続けた)
                if (_timer > dt * 1.5f && Vector2.Dot(Rb.linearVelocity, _dashDir) < _dashSpeed * 0.9f)
                {
                    BeginStagger();
                    break;
                }

                if (_timer >= _maxDashTime)
                {
                    BeginDrift();
                    break;
                }

                MoveFloating(_dashDir.x * _dashSpeed, _dashDir.y * _dashSpeed);
                _hitProbe.TryHit(Player, _body.bounds, _dashDamage, transform.position, gameObject);
                break;

            case State.Drift:
                var t = _driftTime > 0f ? Mathf.Clamp01(_timer / _driftTime) : 1f;
                Rb.linearVelocity = Vector2.Lerp(_driftFrom, Vector2.zero, t);
                if (t >= 1f)
                    Finish();
                break;

            case State.Stagger:
                Rb.linearVelocity = Vector2.zero;
                if (_timer >= _staggerTime)
                    Finish();
                break;
        }
    }

    public override void OnInterrupted()
    {
        base.OnInterrupted();
        if (_state != State.Idle)
            ResetState(true);
    }

    #region State

    private bool CanStart()
    {
        if (Player == null || Enemy.IsDead || !Enemy.IsPlayerDetected || Time.time < _readyTime)
            return false;

        var self = (Vector2)transform.position;
        var target = _hitProbe.GetCenter(Player);
        if (Vector2.Distance(self, target) > _triggerRange)
            return false;

        if (_requireLineOfSight
            && !EnemyMotionUtil.HasLineOfSight(_body.bounds.center, target, EnemyMotionUtil.GroundOrDefault(_groundLayer)))
            return false;

        return true;
    }

    private void BeginWindup()
    {
        _state = State.Windup;
        _timer = 0f;

        // 予備動作を始めた時のプレイヤー位置へ突っ込む
        var to = _hitProbe.GetCenter(Player) - (Vector2)_body.bounds.center;
        _dashDir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;

        EnemyMotionUtil.FaceToward(Enemy, _dashDir.x >= 0f ? 1 : -1);
        _blink.Start();
        Animation?.SetTrigger(_windupTrigger);
    }

    private void BeginDash()
    {
        _state = State.Dash;
        _timer = 0f;
        _blink.Stop();
        Animation?.SetTrigger(_dashTrigger);
        MoveFloating(_dashDir.x * _dashSpeed, _dashDir.y * _dashSpeed);
    }

    private void BeginDrift()
    {
        _state = State.Drift;
        _timer = 0f;
        _driftFrom = Rb.linearVelocity;
        Animation?.SetTrigger(_endTrigger);
    }

    private void BeginStagger()
    {
        _state = State.Stagger;
        _timer = 0f;
        Rb.linearVelocity = Vector2.zero;
        Animation?.SetTrigger(_staggerTrigger);
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
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _triggerRange);
    }
}
