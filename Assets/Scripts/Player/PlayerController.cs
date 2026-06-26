using R3;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D メトロイドヴァニア風のプレイヤー制御(ステートパターンのコンテキスト)。
/// 入力の取得・物理ヘルパー・Animator パラメータ反映を担い、状態ごとの挙動は
/// <see cref="PlayerState"/> 派生クラスに委譲する。状態遷移は <see cref="PlayerStateMachine"/> が管理する。
/// 見た目は SpriteRenderer + Animator(Warrior.controller、パラメータ駆動)、物理は Rigidbody2D。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    #region Constants

    // スティックの遊び(デッドゾーン)。これを超えたら入力ありとみなす
    private const float StickDeadZone = 0.3f;

    // Animator のパラメータ名 (Animator に同名で追加すること)
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int ParamYVelocity = Animator.StringToHash("YVelocity");
    private static readonly int ParamIsDashing = Animator.StringToHash("IsDashing");
    private static readonly int ParamIsAttacking = Animator.StringToHash("IsAttacking");
    
    private CompositeDisposable _playerDisposables = new CompositeDisposable();
    #endregion

    #region Serialized Fields

    [Tooltip("挙動を定義する定数アセット")]
    [SerializeField] private PlayerConsts _consts;

    [Tooltip("接地判定の中心となる足元の Transform")]
    [SerializeField] private Transform _groundCheck;

    #endregion

    #region Components

    private Rigidbody2D _rb;
    private Animator _animator;

    #endregion

    #region State Machine

    private PlayerStateMachine _stateMachine;

    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }
    public DashState DashState { get; private set; }
    public AttackState AttackState { get; private set; }

    #endregion

    #region Input State

    private float _moveInput;        // -1..1 の水平入力
    private bool _jumpHeld;          // ジャンプボタン押しっぱなし
    private bool _dashPressed;       // このフレームにダッシュ入力されたか
    private bool _attackPressed;     // このフレームに攻撃入力されたか

    #endregion

    #region Runtime State

    private bool _isGrounded;
    private readonly ReactiveProperty<int> _facing = new(1);  // 1 = 右, -1 = 左

    private float _originalScaleX;
    private float _originalScaleY;
    private float _originalScaleZ;

    private float _coyoteTimer;      // 地面を離れてからの残り猶予
    private float _jumpBufferTimer;  // 先行入力の残り時間
    private float _dashCooldownTimer;

    private bool _isDashing;
    private bool _isAttacking;
    private bool _isRunning;         // ダッシュ後に維持される走り状態

    #endregion

    #region Public Accessors (states 用)

    public PlayerConsts Consts => _consts;
    public Rigidbody2D Rb => _rb;
    public float MoveInput => _moveInput;
    public bool IsGrounded => _isGrounded;
    public int Facing => _facing.Value;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        _originalScaleX = transform.localScale.x;
        _originalScaleY = transform.localScale.y;
        _originalScaleZ = transform.localScale.z;

        _facing.Subscribe(f =>
            transform.localScale = new Vector3(_originalScaleX * f, _originalScaleY, _originalScaleZ))
            .AddTo(_playerDisposables);

        // 重力は自前で適用するので Rigidbody2D 側の重力は切る
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        if (_consts == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerConsts が設定されていません。", this);

        // ステート生成
        _stateMachine = new PlayerStateMachine();
        IdleState = new IdleState(this, _stateMachine);
        MoveState = new MoveState(this, _stateMachine);
        JumpState = new JumpState(this, _stateMachine);
        FallState = new FallState(this, _stateMachine);
        DashState = new DashState(this, _stateMachine);
        AttackState = new AttackState(this, _stateMachine);
    }

    private void Start()
    {
        _stateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        if (_consts == null)
            return;

        ReadInput();
        UpdateTimers();
        _stateMachine.CurrentState.LogicUpdate();
        UpdateFacing();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (_consts == null)
            return;

        UpdateGrounded();
        _stateMachine.CurrentState.PhysicsUpdate();
    }

    private void OnDrawGizmosSelected()
    {
        if (_consts == null)
            return;

        if (_groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _consts.GroundCheckRadius);
        }

        // 攻撃判定ボックス
        Gizmos.color = Color.red;
        var dir = Application.isPlaying ? _facing.Value : 1;
        var center = (Vector2)transform.position
                     + new Vector2(_consts.AttackOffset.x * dir, _consts.AttackOffset.y);
        Gizmos.DrawWireCube(center, _consts.AttackBoxSize);
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        _moveInput = 0f;
        var jumpPressedThisFrame = false;
        var dashPressedThisFrame = false;
        var attackPressedThisFrame = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) _moveInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) _moveInput += 1f;

            _jumpHeld = keyboard.spaceKey.isPressed;
            jumpPressedThisFrame = keyboard.spaceKey.wasPressedThisFrame;

            // キーボードでの代替: Shift = ダッシュ, J = 攻撃
            dashPressedThisFrame = keyboard.leftShiftKey.wasPressedThisFrame;
            attackPressedThisFrame = keyboard.jKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // 慣性なしの移動に合わせ、スティックはデッドゾーンを超えたら最大入力として扱う
            var stickX = gamepad.leftStick.x.ReadValue();
            if (stickX < -StickDeadZone || gamepad.dpad.left.isPressed) _moveInput = -1f;
            else if (stickX > StickDeadZone || gamepad.dpad.right.isPressed) _moveInput = 1f;

            // ジャンプ = 南(A/×), ダッシュ = R2(右トリガー), 攻撃 = 西(X/□)
            _jumpHeld |= gamepad.buttonSouth.isPressed;
            jumpPressedThisFrame |= gamepad.buttonSouth.wasPressedThisFrame;
            dashPressedThisFrame |= gamepad.rightTrigger.wasPressedThisFrame;
            attackPressedThisFrame |= gamepad.buttonWest.wasPressedThisFrame;
        }

        _moveInput = Mathf.Clamp(_moveInput, -1f, 1f);

        // 先行入力(ジャンプバッファ)を更新
        if (jumpPressedThisFrame)
            _jumpBufferTimer = _consts.JumpBufferTime;

        // ダッシュ・攻撃は押されたフレームのみ true。各ステートが TryConsume で消費する
        _dashPressed = dashPressedThisFrame;
        _attackPressed = attackPressedThisFrame;
    }

    private void UpdateTimers()
    {
        var dt = Time.deltaTime;
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;
        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= dt;
    }

    #endregion

    #region Input Queries (states 用)

    /// <summary>ジャンプ可能(コヨーテ中 かつ 先行入力あり)か。</summary>
    public bool HasBufferedJump() => _coyoteTimer > 0f && _jumpBufferTimer > 0f;

    /// <summary>攻撃入力を消費する。攻撃すべきなら true。</summary>
    public bool TryConsumeAttack()
    {
        if (!_attackPressed)
            return false;

        _attackPressed = false;
        return true;
    }

    /// <summary>ダッシュ入力を消費する。クールダウンが明けていて入力があれば true。</summary>
    public bool TryConsumeDash()
    {
        if (!_dashPressed || _dashCooldownTimer > 0f)
            return false;

        _dashPressed = false;
        return true;
    }

    #endregion

    #region Movement Helpers (states 用)

    private void UpdateGrounded()
    {
        var wasGrounded = _isGrounded;

        if (_groundCheck != null)
        {
            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position,
                _consts.GroundCheckRadius,
                _consts.GroundLayer);
        }
        else
        {
            _isGrounded = false;
        }

        if (_isGrounded)
            _coyoteTimer = _consts.CoyoteTime;
        else if (_coyoteTimer > 0f)
            _coyoteTimer -= Time.fixedDeltaTime;

        // 着地した瞬間に下向き速度をリセットしておくと貼り付きが安定する
        if (_isGrounded && !wasGrounded && _rb.linearVelocity.y < 0f)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
    }

    /// <summary>水平移動を即時反映する(慣性・補間なし)。走り状態なら RunSpeed。</summary>
    public void ApplyHorizontalMovement()
    {
        // 入力が無くなったら走り状態を解除する
        if (Mathf.Approximately(_moveInput, 0f))
            _isRunning = false;

        var speed = _isRunning ? _consts.RunSpeed : _consts.MoveSpeed;
        _rb.linearVelocity = new Vector2(_moveInput * speed, _rb.linearVelocity.y);
    }

    /// <summary>手動重力を適用する。落下・低ジャンプ時は倍率を掛ける。</summary>
    public void ApplyGravity()
    {
        var velocityY = _rb.linearVelocity.y;
        var gravity = _consts.Gravity;

        if (velocityY < 0f)
            gravity *= _consts.FallGravityMultiplier;
        else if (velocityY > 0f && !_jumpHeld)
            gravity *= _consts.LowJumpMultiplier;

        velocityY -= gravity * Time.fixedDeltaTime;
        velocityY = Mathf.Max(velocityY, -_consts.MaxFallSpeed);

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, velocityY);
    }

    /// <summary>ジャンプ初速を与える(JumpState.Enter から呼ばれる)。</summary>
    public void Jump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _consts.JumpVelocity);
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
    }

    /// <summary>現在の状況に応じた locomotion ステートを返す(Dash/Attack 終了後の復帰先)。</summary>
    public PlayerState GetLocomotionState()
    {
        if (!_isGrounded)
            return FallState;

        return GetGroundedState();
    }

    /// <summary>接地中の locomotion ステート(Idle / Move)を返す。</summary>
    public PlayerState GetGroundedState()
    {
        return Mathf.Abs(_moveInput) > 0.01f ? MoveState : IdleState;
    }

    #endregion

    #region Dash (states 用)

    public void StartDash()
    {
        _isDashing = true;
        _dashCooldownTimer = _consts.DashCooldown;
    }

    public void ApplyDashMovement()
    {
        // 向いている方向へ一定速度で飛び出す。重力は無効
        _rb.linearVelocity = new Vector2(_facing.Value * _consts.DashSpeed, 0f);
    }

    public void EndDash()
    {
        _isDashing = false;

        // ダッシュ終了時に進行方向の入力が続いていれば走り状態へ移行する
        _isRunning = !Mathf.Approximately(_moveInput, 0f);
    }

    #endregion

    #region Attack (states 用)

    public void StartAttack()
    {
        _isAttacking = true;
    }

    public void EndAttack()
    {
        _isAttacking = false;
    }

    /// <summary>向いている方向の判定ボックスで対象を検知しダメージを与える。</summary>
    public void PerformAttackHit()
    {
        var center = (Vector2)transform.position
                     + new Vector2(_consts.AttackOffset.x * _facing.Value, _consts.AttackOffset.y);

        var hits = Physics2D.OverlapBoxAll(center, _consts.AttackBoxSize, 0f, _consts.AttackTargetLayer);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                continue; // 自分自身は無視

            if (hit.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(_consts.AttackDamage, center, gameObject);
        }
    }

    #endregion

    #region Visual

    private void UpdateFacing()
    {
        // ダッシュ・攻撃中は向きを固定する
        if (_isDashing || _isAttacking)
            return;

        if (_moveInput > 0.01f) _facing.Value = 1;
        else if (_moveInput < -0.01f) _facing.Value = -1;
        // localScale の更新は _facing の購読側で行うため、ここでは値のセットのみ
    }

    /// <summary>
    /// Animator のパラメータを更新する。ステート切り替えは Animator 側の遷移条件に委ねる。
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        _animator.SetFloat(ParamSpeed, Mathf.Abs(_rb.linearVelocity.x));
        _animator.SetFloat(ParamYVelocity, _rb.linearVelocity.y);
        _animator.SetBool(ParamIsGrounded, _isGrounded);
        _animator.SetBool(ParamIsDashing, _isDashing);
        _animator.SetBool(ParamIsAttacking, _isAttacking);
    }

    #endregion
}
