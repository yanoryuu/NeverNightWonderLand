using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 移動部品: ジャンプ踏みつけ (ウマ2 / Stomper)。
/// 距離内でプレイヤーを見つけると しゃがむ (着地点に影で予告。影はプレイヤーの足元を追う)
/// → 跳ぶ瞬間のプレイヤー位置へ放物線で跳ぶ (重力を切って軌道をなぞる) → 着地で踏みつけ判定
/// → 硬直 (反撃のチャンス) → クールダウン、の順に動く。
/// クールダウン中・範囲外の間は IsActive が false になり、下に置いた巡回 (PatrolMotion) に切り替わる。
/// 被弾・拘束・ブレイクで中断されると重力・影・点滅を元に戻し、クールダウンから再開する。
/// 影は実行時に SpriteRenderer で作る (素材が無くても動く。_shadowSprite で差し替え可)。
/// Animator トリガー (任意・無ければ無視): しゃがみ / 跳躍 / 着地。
/// </summary>
public class StompJumpMotion : EnemyMotionBase
{
    private enum State
    {
        Idle,
        Crouch,  // しゃがみ (影で着地点を予告)
        Jump,    // 放物線で跳ぶ
        Fall,    // 軌道の終わりで足場が無かった → 重力で落ちて着地を待つ
        Recover, // 着地後の硬直
    }

    #region Serialized Fields

    [Header("発見")]
    [Tooltip("跳びかかる距離 (units)")]
    [SerializeField] private float _detectRange = 7f;

    [Tooltip("地形に遮られていない時だけ跳ぶ")]
    [SerializeField] private bool _requireLineOfSight = false;

    [Header("跳躍")]
    [Tooltip("しゃがみ (予告) の時間 (sec)")]
    [SerializeField] private float _crouchTime = 0.5f;

    [Tooltip("放物線の頂点の高さ (始点と着地点を結ぶ線からの高さ, units)")]
    [SerializeField] private float _jumpHeight = 5f;

    [Tooltip("滞空時間 (sec)")]
    [SerializeField] private float _airTime = 0.8f;

    [Tooltip("1 回の跳躍で進む横の距離の上限 (units)")]
    [SerializeField] private float _maxJumpDistance = 9f;

    [Tooltip("軌道の終わりで足場が無かった時、落ちて着地するまで待つ上限 (sec)")]
    [SerializeField] private float _maxFallTime = 2f;

    [Header("踏みつけ")]
    [Tooltip("着地の踏みつけ判定の幅 (units)")]
    [SerializeField] private float _stompWidth = 1.6f;

    [Tooltip("着地の踏みつけ判定の高さ (足元から上, units)")]
    [SerializeField] private float _stompHeight = 1.0f;

    [Tooltip("踏みつけのダメージ")]
    [SerializeField] private int _stompDamage = 2;

    [Tooltip("着地後の硬直 (sec)")]
    [SerializeField] private float _recoverTime = 0.8f;

    [Tooltip("次に跳べるまでの時間 (sec)。硬直明け または中断から数える")]
    [SerializeField] private float _cooldown = 2f;

    [Header("地形")]
    [Tooltip("地面・壁のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("見た目 (予告)")]
    [Tooltip("着地点の影の絵。未設定なら実行時に作るぼけた楕円")]
    [SerializeField] private Sprite _shadowSprite;

    [Tooltip("影の色 (暗い背景でも床の上で見えるよう濃いめ・少し赤み)")]
    [SerializeField] private Color _shadowColor = new Color(0.12f, 0f, 0.04f, 0.7f);

    [Tooltip("影の描画順 (本体の絵と同じ sortingLayer)。床 (地形 0〜3) の後ろに隠れないよう、ボスの予告 (BossKit.MarkerSortingOrder) と同じく手前に出す")]
    [SerializeField] private int _shadowSortingOrder = BossKit.MarkerSortingOrder;

    [Tooltip("しゃがみ中に本体を点滅させるか")]
    [SerializeField] private bool _blinkOnCrouch = true;

    [Tooltip("点滅させる SpriteRenderer。未設定なら本体の絵を自動で探す")]
    [SerializeField] private SpriteRenderer _blinkRenderer;

    [Tooltip("点滅の色")]
    [SerializeField] private Color _blinkColor = new Color(1f, 0.45f, 0.45f);

    [Tooltip("点滅の間隔 (sec)")]
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Animator トリガー (無ければ無視される)")]
    [SerializeField] private string _crouchTrigger = "StompCrouch";
    [SerializeField] private string _jumpTrigger = "StompJump";
    [SerializeField] private string _landTrigger = "StompLand";

    #endregion

    private Rigidbody2D _rb;
    private Collider2D _body;
    private SpriteRenderer _visual;
    private EnemyTelegraphBlink _blink;
    private readonly PlayerHitProbe _hitProbe = new();
    private SpriteRenderer _shadow;

    private State _state = State.Idle;
    private float _timer;
    private float _readyTime;
    private float _baseGravity;
    private Vector2 _jumpStart;
    private Vector2 _jumpTarget;   // 着地点 (Transform の位置)
    private Vector2 _landingPoint; // 着地点 (足元 = 影の位置)

    /// <summary>しゃがみ・跳躍・硬直のいずれかの最中か。</summary>
    public bool IsBusy => _state != State.Idle;

    public override bool IsActive => _state != State.Idle || CanStart();

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _body = GetComponent<Collider2D>();
        _baseGravity = _rb.gravityScale;
        _visual = EnemyTelegraphBlink.ResolveVisual(this, _blinkRenderer);
        _blink = new EnemyTelegraphBlink(_visual, _blinkColor, _blinkInterval);
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
        if (_shadow != null)
            Destroy(_shadow.gameObject);
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
                BeginCrouch();
                break;

            case State.Crouch:
                FacePlayer();
                Enemy.Move(0f);
                if (_blinkOnCrouch)
                    _blink.Tick(dt);
                UpdateLandingPoint(ground);
                if (_timer >= _crouchTime)
                    BeginJump();
                break;

            case State.Jump:
                TickJump(dt, ground);
                break;

            case State.Fall:
                Enemy.Move(0f);
                if (EnemyMotionUtil.IsGrounded(_body, ground) || _timer >= _maxFallTime)
                    Land();
                break;

            case State.Recover:
                Enemy.Move(0f);
                if (_timer >= _recoverTime)
                    Finish();
                break;
        }
    }

    public override void OnInterrupted()
    {
        if (_state != State.Idle)
            ResetState(true);
    }

    #region State

    private bool CanStart()
    {
        if (Player == null || Enemy.IsDead || Time.time < _readyTime)
            return false;

        var self = (Vector2)transform.position;
        var target = _hitProbe.GetCenter(Player);
        if (Vector2.Distance(self, target) > _detectRange)
            return false;

        var ground = EnemyMotionUtil.GroundOrDefault(_groundLayer);
        if (!EnemyMotionUtil.IsGrounded(_body, ground))
            return false;

        if (_requireLineOfSight && !EnemyMotionUtil.HasLineOfSight(_body.bounds.center, target, ground))
            return false;

        return true;
    }

    private void BeginCrouch()
    {
        _state = State.Crouch;
        _timer = 0f;
        FacePlayer();
        Enemy.Move(0f);
        if (_blinkOnCrouch)
            _blink.Start();
        Animation?.SetTrigger(_crouchTrigger);
        UpdateLandingPoint(EnemyMotionUtil.GroundOrDefault(_groundLayer));
        ShowShadow(true);
    }

    /// <summary>プレイヤーの足元の地面を着地点にする (しゃがみ中は毎ステップ追う)。</summary>
    private void UpdateLandingPoint(LayerMask ground)
    {
        if (Player == null)
            return;

        var self = (Vector2)transform.position;
        var dx = Mathf.Clamp(Player.position.x - self.x, -_maxJumpDistance, _maxJumpDistance);
        var x = self.x + dx;

        // 足元からの Transform の高さ (ピボットが中心でも足元でも合うように)
        var feetOffset = self.y - _body.bounds.min.y;

        var probeFrom = new Vector2(x, _hitProbe.GetCenter(Player).y);
        var groundY = EnemyMotionUtil.TryGetGroundBelow(probeFrom, 20f, ground, out var y)
            ? y
            : _body.bounds.min.y; // 足場が見つからなければ今の高さ (着地後に落ちる)

        _landingPoint = new Vector2(x, groundY);
        _jumpTarget = new Vector2(x, groundY + feetOffset);

        if (_shadow != null)
            _shadow.transform.position = _landingPoint;
    }

    private void BeginJump()
    {
        _state = State.Jump;
        _timer = 0f;
        _blink.Stop();
        _jumpStart = _rb.position;
        _rb.gravityScale = 0f;
        Animation?.SetTrigger(_jumpTrigger);
    }

    private void TickJump(float dt, LayerMask ground)
    {
        var s = _airTime > 0f ? Mathf.Clamp01(_timer / _airTime) : 1f;

        // 始点→着地点の直線 + 4h·s(1-s) の山 (s = 0.5 で頂点)
        var desired = Vector2.Lerp(_jumpStart, _jumpTarget, s);
        desired.y += 4f * _jumpHeight * s * (1f - s);

        // 速度で軌道をなぞる (壁・天井には物理で止められる)
        var v = (desired - _rb.position) / dt;
        v = Vector2.ClampMagnitude(v, 60f);
        Enemy.Move(v.x);
        _rb.linearVelocity = new Vector2(v.x, v.y);

        // 下りで体がプレイヤーに重なったら踏みつけのダメージ (コアの接触ダメージ 1 より先に入れる。
        // 着地の瞬間だけ判定すると、降りてくる途中の接触ダメージで無敵になって踏みつけが弾かれる)
        var descending = s > 0.5f;
        if (descending)
            _hitProbe.TryHit(Player, _body.bounds, _stompDamage, _body.bounds.center, gameObject);

        // 下りで先に足場に着いたら着地 (段差の上などに降りた時)
        if (descending && EnemyMotionUtil.IsGrounded(_body, ground))
        {
            Land();
            return;
        }

        if (s >= 1f)
        {
            _rb.gravityScale = _baseGravity;
            if (EnemyMotionUtil.IsGrounded(_body, ground))
            {
                Land();
            }
            else
            {
                _state = State.Fall;
                _timer = 0f;
            }
        }
    }

    private void Land()
    {
        _rb.gravityScale = _baseGravity;
        _rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, _rb.linearVelocity.y));
        Enemy.Move(0f);
        ShowShadow(false);
        Animation?.SetTrigger(_landTrigger);

        // 踏みつけ判定: 足元を中心に幅 _stompWidth・高さ _stompHeight
        var feet = new Vector2(_body.bounds.center.x, _body.bounds.min.y);
        var area = new Bounds(feet + Vector2.up * (_stompHeight * 0.5f), new Vector3(_stompWidth, _stompHeight, 1f));
        _hitProbe.TryHit(Player, area, _stompDamage, feet, gameObject);

        _state = State.Recover;
        _timer = 0f;
    }

    private void Finish()
    {
        _state = State.Idle;
        _timer = 0f;
        _readyTime = Time.time + _cooldown;
    }

    private void ResetState(bool startCooldown)
    {
        _rb.gravityScale = _baseGravity;
        _blink.Stop();
        ShowShadow(false);
        _state = State.Idle;
        _timer = 0f;
        if (startCooldown)
            _readyTime = Time.time + _cooldown;
    }

    private void HandleDied(EnemyController _) => ResetState(false);

    private void FacePlayer()
    {
        if (Player == null)
            return;
        var dx = Player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
            EnemyMotionUtil.FaceToward(Enemy, dx > 0f ? 1 : -1);
    }

    #endregion

    #region Shadow

    private void ShowShadow(bool visible)
    {
        if (!visible)
        {
            if (_shadow != null)
                _shadow.enabled = false;
            return;
        }

        if (_shadow == null)
            CreateShadow();

        _shadow.transform.position = _landingPoint;
        _shadow.enabled = true;
    }

    /// <summary>着地点の影を作る。本体の子にはしない (本体と一緒に動かないように)。</summary>
    private void CreateShadow()
    {
        var go = new GameObject($"{name}_StompShadow");
        if (gameObject.scene.IsValid())
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);

        var sprite = _shadowSprite != null ? _shadowSprite : EnemyFxSprites.SoftEllipse;
        _shadow = go.AddComponent<SpriteRenderer>();
        _shadow.sprite = sprite;
        _shadow.color = _shadowColor;

        if (_visual != null)
            _shadow.sortingLayerID = _visual.sortingLayerID;
        _shadow.sortingOrder = _shadowSortingOrder;

        // 絵の幅を踏みつけ判定の幅に合わせる
        var spriteWidth = Mathf.Max(0.01f, sprite.bounds.size.x);
        var scale = _stompWidth / spriteWidth;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        _shadow.enabled = false;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _detectRange);
    }
}
