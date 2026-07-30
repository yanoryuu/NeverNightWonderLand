using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

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

    // 裁断の先行入力を保持する時間 (sec)。攻撃モーション中に押しても直後に発動できる
    private const float FinisherBufferTime = 0.25f;

    // Animator のパラメータ名 (Animator に同名で追加すること)
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int ParamYVelocity = Animator.StringToHash("YVelocity");
    private static readonly int ParamIsDashing = Animator.StringToHash("IsDashing");
    private static readonly int ParamIsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int ParamIsHurt = Animator.StringToHash("IsHurt");
    private static readonly int ParamDeath = Animator.StringToHash("Death");

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
    private PlayerHealth _health;             // 任意 (無いと被弾しない)
    private PlayerHealGauge _healGauge;       // 任意 (無いと回復不可)
    private PlayerItemInventory _inventory;   // 任意 (無いとアイテム不可)
    private PlayerProgression _progression;   // 任意 (無いとハサミ強化なし)
    private PlayerAttackLoadout _attackLoadout; // 任意 (無いと特殊攻撃不可・近接はフォールバック)

    #endregion

    #region State Machine

    private PlayerStateMachine _stateMachine;

    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }
    public DashState DashState { get; private set; }
    public AttackState AttackState { get; private set; }
    public SpecialAttackState SpecialAttackState { get; private set; }
    public FinisherState FinisherState { get; private set; }
    public HealState HealState { get; private set; }
    public HurtState HurtState { get; private set; }
    public DeadState DeadState { get; private set; }
    public ItemThrowState ItemThrowState { get; private set; }
    public ItemDashState ItemDashState { get; private set; }
    public GrappleState GrappleState { get; private set; }
    public WallClingState WallClingState { get; private set; }
    public LedgeClimbState LedgeClimbState { get; private set; }

    #endregion

    #region Input State

    private float _moveInput;        // -1..1 の水平入力
    private float _verticalInput;    // -1..1 の垂直入力 (グラップルの狙い用)
    private bool _jumpHeld;          // ジャンプボタン押しっぱなし
    private bool _dashPressed;       // このフレームにダッシュ入力されたか
    private bool _attackPressed;     // このフレームに近接攻撃入力されたか
    private bool _specialPressed;    // このフレームに特殊攻撃入力されたか
    private float _finisherBufferTimer; // 裁断の先行入力の残り時間 (攻撃中の入力を拾うためバッファ式)
    private bool _healPressed;       // このフレームに回復入力されたか
    private bool _interactPressed;   // このフレームにインタラクト入力されたか
    private ItemSlot? _itemSlotPressed; // このフレームに押されたアイテムスロット (L1/[I] ホールド + 方向)
    private bool _grapplePressed;    // このフレームにグラップル入力されたか

    #endregion

    #region Runtime State

    private bool _isGrounded;
    private readonly ReactiveProperty<int> _facing = new(1);  // 1 = 右, -1 = 左

    private PlayerRuntime _playerRuntime; // UI (Presenter) へ自身を公開するための実行時参照

    private float _originalScaleX;
    private float _originalScaleY;
    private float _originalScaleZ;

    private float _coyoteTimer;      // 地面を離れてからの残り猶予
    private float _jumpBufferTimer;  // 先行入力の残り時間
    private float _dashCooldownTimer;
    private float _specialCooldownTimer;    // 特殊攻撃の再使用までの残り時間
    private float _specialCooldownDuration; // 直近のクールダウン全長 (HUD の割合表示用)

    private bool _isDashing;
    private bool _isAttacking;
    private bool _isHurt;
    private bool _isDead;
    private bool _isRunning;         // ダッシュ後に維持される走り状態
    private bool _airJumpUsed;       // 二段ジャンプ (赤ハサミ) を使用済みか。接地でリセット
    private bool _dashInvulnerable;  // 現在のダッシュが接触ダメージ無効 (回避) か

    // Animator に任意パラメータ(IsHurt/Death)が存在するか (無い場合は警告を出さずスキップ)
    private bool _hasIsHurtParam;
    private bool _hasDeathParam;

    private DamageInfo _lastDamage;  // HurtState がノックバック方向の決定に使う

    #endregion

    #region Public Accessors (states 用)

    public PlayerConsts Consts => _consts;
    public Rigidbody2D Rb => _rb;
    public float MoveInput => _moveInput;
    public bool IsGrounded => _isGrounded;
    public int Facing => _facing.Value;
    public bool IsDead => _isDead;

    /// <summary>攻撃方法の装備状況。無いシーンでは null。</summary>
    public PlayerAttackLoadout AttackLoadout => _attackLoadout;

    /// <summary>
    /// 装備中の近接攻撃プロファイル。ロードアウト未装備時は PlayerConsts のフォールバックを使う。
    /// </summary>
    public PlayerConsts.AttackProfile CurrentMeleeProfile =>
        _attackLoadout != null && _attackLoadout.CurrentMelee != null
            ? _attackLoadout.CurrentMelee.Profile
            : _consts.DualAttack;

    public PlayerHealth Health => _health;
    public PlayerHealGauge HealGauge => _healGauge;
    public PlayerItemInventory Inventory => _inventory;
    public PlayerProgression Progression => _progression;
    public float VerticalInput => _verticalInput;

    /// <summary>直近の被弾情報 (HurtState 用)。</summary>
    public DamageInfo LastDamage => _lastDamage;

    /// <summary>現在のステート名 (デバッグ表示用)。</summary>
    public string CurrentStateName => _stateMachine?.CurrentState?.GetType().Name ?? "-";

    #endregion

    #region Unity Callbacks

    [Inject]
    public void Construct(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ownColliders = GetComponents<Collider2D>();
        _animator = GetComponent<Animator>();
        _health = GetComponent<PlayerHealth>();
        _healGauge = GetComponent<PlayerHealGauge>();
        _inventory = GetComponent<PlayerItemInventory>();
        _progression = GetComponent<PlayerProgression>();
        _attackLoadout = GetComponent<PlayerAttackLoadout>();

        _originalScaleX = transform.localScale.x;
        _originalScaleY = transform.localScale.y;
        _originalScaleZ = transform.localScale.z;

        _facing.Subscribe(f =>
            transform.localScale = new Vector3(_originalScaleX * f, _originalScaleY, _originalScaleZ))
            .AddTo(_playerDisposables);

        // 重力は自前で適用するので Rigidbody2D 側の重力は切る
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        // 壁に向かって移動し続けても張り付かないよう、摩擦ゼロのマテリアルを適用する
        // (移動・重力とも速度を直接指定する方式なので、摩擦は不要)
        _rb.sharedMaterial = new PhysicsMaterial2D("PlayerFrictionless")
        {
            friction = 0f,
            bounciness = 0f,
        };

        if (_consts == null)
            Debug.LogError($"[{nameof(PlayerController)}] PlayerConsts が設定されていません。", this);

        foreach (var param in _animator.parameters)
        {
            if (param.nameHash == ParamIsHurt) _hasIsHurtParam = true;
            else if (param.nameHash == ParamDeath) _hasDeathParam = true;
        }

        // ステート生成
        _stateMachine = new PlayerStateMachine();
        IdleState = new IdleState(this, _stateMachine);
        MoveState = new MoveState(this, _stateMachine);
        JumpState = new JumpState(this, _stateMachine);
        FallState = new FallState(this, _stateMachine);
        DashState = new DashState(this, _stateMachine);
        AttackState = new AttackState(this, _stateMachine);
        SpecialAttackState = new SpecialAttackState(this, _stateMachine);
        FinisherState = new FinisherState(this, _stateMachine);
        HealState = new HealState(this, _stateMachine);
        HurtState = new HurtState(this, _stateMachine);
        DeadState = new DeadState(this, _stateMachine);
        ItemThrowState = new ItemThrowState(this, _stateMachine);
        ItemDashState = new ItemDashState(this, _stateMachine);
        GrappleState = new GrappleState(this, _stateMachine);
        WallClingState = new WallClingState(this, _stateMachine);
        LedgeClimbState = new LedgeClimbState(this, _stateMachine);

        // 初期化がすべて終わってから UI (Presenter) へ自身を公開する
        _playerRuntime?.Register(this);
    }

    private void Start()
    {
        _stateMachine.Initialize(IdleState);
    }

    private void OnDestroy()
    {
        _playerRuntime?.Unregister(this);
        _playerDisposables.Dispose();
        _facing.Dispose();
    }

    private void Update()
    {
        if (_consts == null)
            return;

        // ポーズ中もデバイス切替 (キーボード⇔パッド) は追跡する
        InputDeviceTracker.Poll();

        if (GamePause.IsPaused)
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

        // 攻撃判定ボックス (装備中の近接攻撃)
        var dir = Application.isPlaying ? _facing.Value : 1;
        var profile = Application.isPlaying ? CurrentMeleeProfile : _consts.DualAttack;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetAttackCenter(profile, dir), profile.BoxSize);

        // 裁断の発動可能範囲
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, _consts.FinisherRange);

        // インタラクト検出範囲
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _consts.InteractRadius);
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        _moveInput = 0f;
        _verticalInput = 0f;
        var jumpPressedThisFrame = false;
        var dashPressedThisFrame = false;
        var attackPressedThisFrame = false;
        var specialPressedThisFrame = false;
        var finisherPressedThisFrame = false;
        var healPressedThisFrame = false;
        var interactPressedThisFrame = false;
        ItemSlot? itemSlotPressedThisFrame = null;
        var grapplePressedThisFrame = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // [I] ホールド中は方向キーをアイテム選択に使う (移動・回復には反映しない)
            var itemHeld = keyboard.iKey.isPressed;
            if (itemHeld)
            {
                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                    itemSlotPressedThisFrame = ItemSlot.Down;
                else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                    itemSlotPressedThisFrame = ItemSlot.Left;
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                    itemSlotPressedThisFrame = ItemSlot.Right;
            }
            else
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) _moveInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) _moveInput += 1f;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) _verticalInput -= 1f;
                healPressedThisFrame = keyboard.sKey.wasPressedThisFrame;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) _verticalInput += 1f;

            _jumpHeld = keyboard.spaceKey.isPressed;
            jumpPressedThisFrame = keyboard.spaceKey.wasPressedThisFrame;

            // キーボード: Shift = ダッシュ, J = 近接攻撃, K = 特殊攻撃, L = 裁断,
            //             S = 回復, E = インタラクト, I ホールド+方向 = アイテム使用, F = 糸移動 (青)
            dashPressedThisFrame = keyboard.leftShiftKey.wasPressedThisFrame;
            attackPressedThisFrame = keyboard.jKey.wasPressedThisFrame;
            specialPressedThisFrame = keyboard.kKey.wasPressedThisFrame;
            finisherPressedThisFrame = keyboard.lKey.wasPressedThisFrame;
            interactPressedThisFrame = keyboard.eKey.wasPressedThisFrame;
            grapplePressedThisFrame = keyboard.fKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // L1 ホールド中は十字キーをアイテム選択に使う (移動・インタラクトには反映しない)。
            // スティックでの移動は L1 ホールド中も有効
            var itemHeld = gamepad.leftShoulder.isPressed;

            // 慣性なしの移動に合わせ、スティックはデッドゾーンを超えたら最大入力として扱う
            var stickX = gamepad.leftStick.x.ReadValue();
            if (stickX < -StickDeadZone || (!itemHeld && gamepad.dpad.left.isPressed)) _moveInput = -1f;
            else if (stickX > StickDeadZone || (!itemHeld && gamepad.dpad.right.isPressed)) _moveInput = 1f;

            var stickY = gamepad.leftStick.y.ReadValue();
            if (stickY > StickDeadZone || (!itemHeld && gamepad.dpad.up.isPressed)) _verticalInput = 1f;
            else if (stickY < -StickDeadZone || (!itemHeld && gamepad.dpad.down.isPressed)) _verticalInput = -1f;

            if (itemHeld)
            {
                if (gamepad.dpad.down.wasPressedThisFrame) itemSlotPressedThisFrame = ItemSlot.Down;
                else if (gamepad.dpad.left.wasPressedThisFrame) itemSlotPressedThisFrame = ItemSlot.Left;
                else if (gamepad.dpad.right.wasPressedThisFrame) itemSlotPressedThisFrame = ItemSlot.Right;
            }

            // ホロウナイト準拠の配置:
            // ジャンプ = ×(A), 近接攻撃 = □(X), 特殊攻撃 = △(Y), 回復 = ○(B/フォーカス枠),
            // ダッシュ = R2, 裁断 = R1(クイックキャスト枠), 糸移動 = L2(スーパーダッシュ枠),
            // アイテム = L1 ホールド+十字, インタラクト = 上入力
            _jumpHeld |= gamepad.buttonSouth.isPressed;
            jumpPressedThisFrame |= gamepad.buttonSouth.wasPressedThisFrame;
            dashPressedThisFrame |= gamepad.rightTrigger.wasPressedThisFrame;
            attackPressedThisFrame |= gamepad.buttonWest.wasPressedThisFrame;
            specialPressedThisFrame |= gamepad.buttonNorth.wasPressedThisFrame;
            finisherPressedThisFrame |= gamepad.rightShoulder.wasPressedThisFrame;
            healPressedThisFrame |= gamepad.buttonEast.wasPressedThisFrame;
            grapplePressedThisFrame |= gamepad.leftTrigger.wasPressedThisFrame;

            // インタラクト = 上入力 (十字キー上 / 左スティック上) — ホロウナイトと同じ
            interactPressedThisFrame |= (!itemHeld && gamepad.dpad.up.wasPressedThisFrame)
                                        || gamepad.leftStick.up.wasPressedThisFrame;
        }

        _moveInput = Mathf.Clamp(_moveInput, -1f, 1f);

        // 先行入力(ジャンプ/裁断バッファ)を更新
        if (jumpPressedThisFrame)
            _jumpBufferTimer = _consts.JumpBufferTime;
        if (finisherPressedThisFrame)
            _finisherBufferTimer = FinisherBufferTime;

        // 押されたフレームのみ true。各ステートが TryConsume で消費する
        _dashPressed = dashPressedThisFrame;
        _attackPressed = attackPressedThisFrame;
        _specialPressed = specialPressedThisFrame;
        _healPressed = healPressedThisFrame;
        _interactPressed = interactPressedThisFrame;
        _itemSlotPressed = itemSlotPressedThisFrame;
        _grapplePressed = grapplePressedThisFrame;
    }

    private void UpdateTimers()
    {
        var dt = Time.deltaTime;
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;
        if (_finisherBufferTimer > 0f) _finisherBufferTimer -= dt;
        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= dt;
        if (_specialCooldownTimer > 0f) _specialCooldownTimer -= dt;

        // 降り抜け中のすり抜け床: 時間が経ったら衝突を戻す
        for (var i = _dropThroughPlatforms.Count - 1; i >= 0; i--)
        {
            _dropThroughTimers[i] -= dt;
            if (_dropThroughTimers[i] > 0f)
                continue;

            if (_dropThroughPlatforms[i] != null)
            {
                foreach (var own in _ownColliders)
                    Physics2D.IgnoreCollision(own, _dropThroughPlatforms[i], false);
            }

            _dropThroughPlatforms.RemoveAt(i);
            _dropThroughTimers.RemoveAt(i);
        }
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

    /// <summary>特殊攻撃入力を消費する。発動可否 (<see cref="CanSpecialAttack"/>) は呼び出し側で確認する。</summary>
    public bool TryConsumeSpecial()
    {
        if (!_specialPressed)
            return false;

        _specialPressed = false;
        return true;
    }

    /// <summary>
    /// 裁断入力を消費する。先行入力バッファ式なので、攻撃モーション中に押しても
    /// モーション明けに発動できる。発動可否 (<see cref="CanFinisher"/>) は呼び出し側で確認する。
    /// </summary>
    public bool TryConsumeFinisher()
    {
        if (_finisherBufferTimer <= 0f)
            return false;

        _finisherBufferTimer = 0f;
        return true;
    }

    /// <summary>回復入力を消費する。発動可否 (<see cref="CanHeal"/>) は呼び出し側で確認する。</summary>
    public bool TryConsumeHeal()
    {
        if (!_healPressed)
            return false;

        _healPressed = false;
        return true;
    }

    /// <summary>インタラクト入力を消費する (PlayerInteractor 用)。</summary>
    public bool TryConsumeInteract()
    {
        if (!_interactPressed)
            return false;

        _interactPressed = false;
        return true;
    }

    /// <summary>
    /// アイテム使用入力を消費する (L1/[I] ホールド + 方向で押されたスロット)。
    /// 所持数チェックは呼び出し側で行う。
    /// </summary>
    public bool TryConsumeItemUse(out ItemSlot slot)
    {
        if (_itemSlotPressed == null)
        {
            slot = default;
            return false;
        }

        slot = _itemSlotPressed.Value;
        _itemSlotPressed = null;
        return true;
    }

    /// <summary>直前のアイテム使用で選ばれたアイテム定義 (ItemThrowState / ItemDashState が参照する)。</summary>
    public ItemDefinition PendingItem { get; private set; }

    public void SetPendingItem(ItemDefinition item) => PendingItem = item;

    /// <summary>グラップル (青ハサミ) 入力を消費する。</summary>
    public bool TryConsumeGrapple()
    {
        if (!_grapplePressed)
            return false;

        _grapplePressed = false;
        return true;
    }

    /// <summary>グラップルが使えるか (青ハサミを取得済みか)。</summary>
    public bool CanGrapple() => _progression != null && _progression.Has(ScissorUpgrade.Blue);

    /// <summary>指定方向へ入力しているか (壁張り付きの維持判定用)。</summary>
    public bool IsPressingToward(int direction) =>
        direction > 0 ? _moveInput > 0.01f : _moveInput < -0.01f;

    /// <summary>指定方向の壁に接しているか (Ground レイヤー)。</summary>
    public bool IsTouchingWall(int direction)
    {
        var origin = (Vector2)transform.position + new Vector2(0f, 0.3f);
        return Physics2D.Raycast(origin, new Vector2(direction, 0f),
            _consts.WallCheckDistance, _consts.GroundLayer);
    }

    /// <summary>
    /// 壁張り付き (黄ハサミ) が可能か。空中で壁方向へ入力しながら壁に接している時。
    /// </summary>
    public bool CanWallCling()
    {
        if (_isGrounded || _progression == null || !_progression.Has(ScissorUpgrade.Yellow))
            return false;

        if (Mathf.Abs(_moveInput) < 0.01f)
            return false;

        var dir = _moveInput > 0f ? 1 : -1;
        return IsTouchingWall(dir);
    }

    /// <summary>壁張り付き中のずり落ちを適用する (WallClingState.PhysicsUpdate から呼ばれる)。</summary>
    public void ApplyWallSlide()
    {
        _rb.linearVelocity = new Vector2(0f, -_consts.WallSlideSpeed);
    }

    /// <summary>壁と反対方向へ壁ジャンプする。direction は飛ぶ方向 (1=右, -1=左)。</summary>
    public void WallJump(int direction)
    {
        _rb.linearVelocity = new Vector2(direction * _consts.WallJumpVelocity.x, _consts.WallJumpVelocity.y);
        _facing.Value = direction;
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        _airJumpUsed = false; // 壁ジャンプで二段ジャンプを回復
    }

    /// <summary>
    /// 二段ジャンプ (赤ハサミ) を消費する。空中で未使用かつジャンプ先行入力があれば true。
    /// </summary>
    public bool TryConsumeDoubleJump()
    {
        if (_airJumpUsed || _jumpBufferTimer <= 0f)
            return false;

        if (_progression == null || !_progression.Has(ScissorUpgrade.Red))
            return false;

        _airJumpUsed = true;
        return true;
    }

    #endregion

    #region Movement Helpers (states 用)

    // 接地判定の結果バッファ (毎フレームの確保を避ける)
    private readonly Collider2D[] _groundHits = new Collider2D[8];

    // 降り抜け (下入力+ジャンプ) 中に衝突を無効化しているすり抜け床と残り時間
    private const float DropThroughTime = 0.3f;
    private Collider2D[] _ownColliders;
    private readonly System.Collections.Generic.List<Collider2D> _dropThroughPlatforms = new();
    private readonly System.Collections.Generic.List<float> _dropThroughTimers = new();

    private void UpdateGrounded()
    {
        var wasGrounded = _isGrounded;
        _isGrounded = false;

        if (_groundCheck != null)
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _consts.GroundLayer,
                useTriggers = false, // ヒントゾーン等のトリガーを地面と誤認しない
            };
            var count = Physics2D.OverlapCircle(
                (Vector2)_groundCheck.position, _consts.GroundCheckRadius, filter, _groundHits);

            for (var i = 0; i < count; i++)
            {
                // 降り抜け中の床は接地とみなさない (衝突無効中でもクエリには映るため)
                if (_dropThroughPlatforms.Contains(_groundHits[i]))
                    continue;

                // すり抜け床 (PlatformEffector2D の一方通行) は、下から通過している
                // 上昇中には接地とみなさない (通過中に Idle へ戻ってしまうのを防ぐ)
                if (_rb.linearVelocity.y > 0.1f
                    && _groundHits[i].usedByEffector
                    && _groundHits[i].GetComponent<PlatformEffector2D>() != null)
                {
                    continue;
                }

                _isGrounded = true;
                break;
            }

            // 坂の上では接地円が斜面から浮いて判定が途切れることがあるため、
            // 実際の接触 (上向き法線のコンタクト) でも接地とみなす。
            // 上昇中は除外する (ジャンプ直後の蹴り足や、すり抜け床の通過を接地扱いしない)
            if (!_isGrounded && _rb.linearVelocity.y <= 0.1f)
            {
                var contactFilter = new ContactFilter2D
                {
                    useLayerMask = true,
                    layerMask = _consts.GroundLayer,
                    useTriggers = false,
                    useNormalAngle = true,
                    minNormalAngle = 45f,
                    maxNormalAngle = 135f,
                };
                _isGrounded = _rb.IsTouching(contactFilter);
            }
        }

        if (_isGrounded)
        {
            _coyoteTimer = _consts.CoyoteTime;
            _airJumpUsed = false; // 接地で二段ジャンプを回復
        }
        else if (_coyoteTimer > 0f)
        {
            _coyoteTimer -= Time.fixedDeltaTime;
        }

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

    /// <summary>水平速度をゼロにする (裁断・回復・死亡など移動不可のステート用)。</summary>
    public void StopHorizontalMovement()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    /// <summary>手動重力を適用する。落下・低ジャンプ時は倍率を掛ける。赤ハサミ所持時は滑空できる。</summary>
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

        // 滑空 (赤ハサミ): 落下中にジャンプ長押しで落下速度を抑える
        if (velocityY < 0f && _jumpHeld && !_isGrounded
            && _progression != null && _progression.Has(ScissorUpgrade.Red))
        {
            velocityY = Mathf.Max(velocityY, -_consts.GlideFallSpeed);
        }

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, velocityY);
    }

    #region Ledge Climb

    // 崖登りの判定パラメータ
    private const float LedgeMinHeight = 0.4f;   // これ未満の段差は普通に歩ける/低すぎるので登らない
    private const float LedgeMaxHeight = 1.3f;   // 足元からこの高さまでに崖の上面があれば掴める (縁ぎりぎりのみ)
    private const float LedgeHeadClearance = 1.45f; // この高さの前方が塞がっていたら「高い壁」とみなし登らない

    private readonly RaycastHit2D[] _ledgeHits = new RaycastHit2D[4];

    /// <summary>
    /// 崖際 (壁の最上部付近) を登れるか判定し、登り先 (崖の上に立つ位置) を返す。
    /// 条件: 移動入力の方向に壁があり、頭上の高さは空いていて、壁の向こうの上面が
    /// 足元から LedgeMin〜MaxHeight の範囲にある。高い壁の途中では発動しない
    /// (壁登りはハサミ強化の領分)。すり抜け床は壁として扱わない。
    /// </summary>
    public bool TryGetLedgeTarget(out Vector2 target)
    {
        target = default;

        if (Mathf.Abs(_moveInput) < 0.01f || _groundCheck == null)
            return false;

        var dir = _moveInput > 0f ? 1 : -1;
        var feetY = _groundCheck.position.y - _consts.GroundCheckRadius;
        var x = transform.position.x;
        var wallDist = _consts.WallCheckDistance + 0.15f;

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _consts.GroundLayer,
            useTriggers = false,
        };

        // 1) 体の高さに壁があるか (すり抜け床は除外)
        var found = false;
        var count = Physics2D.Raycast(new Vector2(x, feetY + 0.3f), new Vector2(dir, 0f),
            filter, _ledgeHits, wallDist);
        for (var i = 0; i < count; i++)
        {
            if (_ledgeHits[i].collider.usedByEffector
                && _ledgeHits[i].collider.GetComponent<PlatformEffector2D>() != null)
                continue;

            found = true;
            break;
        }

        if (!found)
            return false;

        // 2) 頭上の高さの前方は空いているか (塞がっていたら高い壁 = 登れない)
        count = Physics2D.Raycast(new Vector2(x, feetY + LedgeHeadClearance), new Vector2(dir, 0f),
            filter, _ledgeHits, wallDist + 0.4f);
        for (var i = 0; i < count; i++)
        {
            if (_ledgeHits[i].collider.usedByEffector
                && _ledgeHits[i].collider.GetComponent<PlatformEffector2D>() != null)
                continue;

            return false;
        }

        // 3) 壁の向こう側で崖の上面を探す
        var overX = x + dir * (wallDist + 0.35f);
        count = Physics2D.Raycast(new Vector2(overX, feetY + LedgeHeadClearance + 0.1f), Vector2.down,
            filter, _ledgeHits, LedgeHeadClearance + 0.2f);
        if (count == 0)
            return false;

        var top = _ledgeHits[0].point.y;

        // 4) 上面が「掴める窓」(足元より少し上〜胸の高さ) にあること
        var height = top - feetY;
        if (height < LedgeMinHeight || height > LedgeMaxHeight)
            return false;

        // 5) 登り先に立つ空間があること
        var standCount = Physics2D.OverlapCircle(new Vector2(overX, top + 0.75f), 0.3f, filter, _groundHits);
        for (var i = 0; i < standCount; i++)
        {
            if (_groundHits[i].usedByEffector
                && _groundHits[i].GetComponent<PlatformEffector2D>() != null)
                continue;

            return false;
        }

        var pivotOffset = transform.position.y - feetY;
        target = new Vector2(overX, top + pivotOffset + 0.02f);
        return true;
    }

    #endregion

    /// <summary>
    /// 乗っているすり抜け床から降りる (下入力+ジャンプ)。
    /// 足元が全てすり抜け床の時のみ発動し、しばらく衝突を無効化して落下する。
    /// 通常の地面が混ざっている場合は何もしない。降下を開始したら true。
    /// </summary>
    public bool TryDropThroughPlatform()
    {
        if (_groundCheck == null)
            return false;

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _consts.GroundLayer,
            useTriggers = false,
        };
        var count = Physics2D.OverlapCircle(
            (Vector2)_groundCheck.position, _consts.GroundCheckRadius, filter, _groundHits);

        // 足元の全てがすり抜け床であることを確認する
        var anyPlatform = false;
        for (var i = 0; i < count; i++)
        {
            if (_dropThroughPlatforms.Contains(_groundHits[i]))
                continue;

            if (!_groundHits[i].usedByEffector
                || _groundHits[i].GetComponent<PlatformEffector2D>() == null)
            {
                return false;
            }

            anyPlatform = true;
        }

        if (!anyPlatform)
            return false;

        for (var i = 0; i < count; i++)
        {
            var platform = _groundHits[i];
            if (_dropThroughPlatforms.Contains(platform))
                continue;

            foreach (var own in _ownColliders)
                Physics2D.IgnoreCollision(own, platform, true);

            _dropThroughPlatforms.Add(platform);
            _dropThroughTimers.Add(DropThroughTime);
        }

        // 消費したジャンプ入力で跳ばないようにバッファを破棄する
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        return true;
    }

    /// <summary>コヨーテタイムを回復する (グラップルで壁に張り付いた時など)。</summary>
    public void RefreshCoyote()
    {
        _coyoteTimer = _consts.CoyoteTime;
        _airJumpUsed = false;
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

    /// <summary>
    /// ダッシュを開始する。敵との物理衝突は常時無効 (すり抜け) なので、
    /// ここでは回避 (接触ダメージ無効) かどうかだけを指定する。
    /// 布カッターの突進は「被弾でキャンセル」仕様のため無効化しない。
    /// </summary>
    public void StartDash(bool contactInvulnerable = true)
    {
        _isDashing = true;
        _dashInvulnerable = contactInvulnerable;
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
        _dashInvulnerable = false;

        // ダッシュ終了時に進行方向の入力が続いていれば走り状態へ移行する
        _isRunning = !Mathf.Approximately(_moveInput, 0f);
    }

    /// <summary>回避ダッシュ中 (接触ダメージを受けない) か。PlayerHealth が参照する。</summary>
    public bool IsDashInvulnerable => _isDashing && _dashInvulnerable;

    #endregion

    #region Attack / Style (states 用)

    public void StartAttack()
    {
        _isAttacking = true;
    }

    public void EndAttack()
    {
        _isAttacking = false;
    }

    /// <summary>特殊攻撃が発動可能か (装備済み かつ クールダウン明け)。</summary>
    public bool CanSpecialAttack()
    {
        return _attackLoadout != null
               && _attackLoadout.CurrentSpecial != null
               && _specialCooldownTimer <= 0f;
    }

    /// <summary>特殊攻撃のクールダウンを開始する (SpecialAttackState.Enter から呼ばれる)。</summary>
    public void BeginSpecialCooldown(float duration)
    {
        if (duration <= 0f)
            return;

        _specialCooldownTimer = duration;
        _specialCooldownDuration = duration;
    }

    /// <summary>
    /// 特殊攻撃クールダウンの残り割合 (1=使った直後, 0=使用可能)。HUD の円形表示用。
    /// </summary>
    public float SpecialCooldownRatio =>
        _specialCooldownDuration <= 0f
            ? 0f
            : Mathf.Clamp01(_specialCooldownTimer / _specialCooldownDuration);

    /// <summary>
    /// 投擲/特殊攻撃の発射位置を計算する。壁際で使った時に弾が壁の中に生成されないよう、
    /// 目の前に壁があれば手前へ寄せる (ItemThrowState / SpecialAttackState が使う)。
    /// </summary>
    public Vector2 ComputeThrowOrigin(float heightOffset = 0.3f)
    {
        var origin = (Vector2)transform.position + new Vector2(0.5f * _facing.Value, heightOffset);

        var rayStart = (Vector2)transform.position + new Vector2(0f, heightOffset);
        var wall = Physics2D.Raycast(rayStart, new Vector2(_facing.Value, 0f), 0.9f, _consts.GroundLayer);
        if (wall.collider != null)
            origin.x = wall.point.x - _facing.Value * 0.4f;

        return origin;
    }

    /// <summary>
    /// 向いている方向の判定ボックスで対象を検知しダメージを与える。
    /// 1体以上ヒットしたら true を返し、回復ゲージを蓄積する。
    /// </summary>
    public bool PerformAttackHit(PlayerConsts.AttackProfile profile, bool isFinisher = false)
    {
        var center = GetAttackCenter(profile, _facing.Value);
        var hits = Physics2D.OverlapBoxAll(center, profile.BoxSize, 0f, _consts.AttackTargetLayer);

        var anyHit = false;
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                continue; // 自分自身は無視

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                var info = new DamageInfo(profile.HpDamage, profile.GuardDamage, center, gameObject, isFinisher);
                damageable.TakeDamage(info);
                anyHit = true;
            }
        }

        // 「リスクを取って攻めるほど回復源が増える」: ヒット時に回復ゲージを蓄積
        if (anyHit && _healGauge != null)
            _healGauge.AddCharge();

        return anyHit;
    }

    private Vector2 GetAttackCenter(PlayerConsts.AttackProfile profile, int facing)
    {
        return (Vector2)transform.position
               + new Vector2(profile.Offset.x * facing, profile.Offset.y);
    }

    /// <summary>
    /// 突進系アイテムのヒット判定 (ItemDashState から毎物理ステップ呼ばれる)。
    /// ダメージ量は使用中アイテムの定義 (DashItemDefinition) から渡される。
    /// 同じ対象へ多段ヒットしないよう、ヒット済みコライダー集合を受け取る。
    /// </summary>
    public void PerformItemDashHit(System.Collections.Generic.HashSet<Collider2D> alreadyHit,
        int hpDamage, int guardDamage, float knockback)
    {
        var center = (Vector2)transform.position + new Vector2(0.5f * _facing.Value, 0f);
        var size = new Vector2(1.2f, 1.2f);

        var hits = Physics2D.OverlapBoxAll(center, size, 0f, _consts.AttackTargetLayer);
        foreach (var hit in hits)
        {
            if (alreadyHit.Contains(hit))
                continue;
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
                continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                alreadyHit.Add(hit);
                var info = new DamageInfo(hpDamage, guardDamage, center, gameObject,
                    knockbackPower: knockback);
                damageable.TakeDamage(info);

                if (_healGauge != null)
                    _healGauge.AddCharge();
            }
        }
    }

    /// <summary>
    /// 裁断の標的を探す。条件: 発動範囲 (FinisherRange) 内・高さが同じくらい (範囲の縦幅)・
    /// 向いている方向にいる・ブレイク中 — を満たす敵のうち一番近いもの。
    /// </summary>
    public bool TryGetFinisherTarget(out EnemyController target)
    {
        target = null;
        var bestSqr = float.MaxValue;

        var hits = Physics2D.OverlapBoxAll(
            transform.position, _consts.FinisherRange, 0f, _consts.AttackTargetLayer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<EnemyController>(out var enemy) || !enemy.IsBroken.CurrentValue)
                continue;

            // 向いている方向の敵のみ (重なっている場合は許容)
            var dx = enemy.transform.position.x - transform.position.x;
            if (dx * _facing.Value < -0.2f)
                continue;

            var sqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                target = enemy;
            }
        }

        return target != null;
    }

    /// <summary>裁断が発動可能か (発動範囲内にブレイク中の敵がいるか)。</summary>
    public bool CanFinisher() => TryGetFinisherTarget(out _);

    /// <summary>指定したワールド X 座標の方を向く (裁断の踏み込み用)。</summary>
    public void FaceTo(float worldX)
    {
        _facing.Value = worldX >= transform.position.x ? 1 : -1;
    }

    #endregion

    #region Heal (states 用)

    /// <summary>回復が可能か (メモリが1以上あり、HP が満タンでない)。</summary>
    public bool CanHeal()
    {
        return _healGauge != null && _health != null
               && _healGauge.Pips.CurrentValue > 0
               && _health.Hp.CurrentValue < _consts.MaxHp;
    }

    /// <summary>メモリを1消費して HP を回復する (HealState.Enter から呼ばれる)。成功したら true。</summary>
    public bool TryApplyHeal()
    {
        if (_healGauge == null || _health == null || !_healGauge.TryConsumePip())
            return false;

        _health.Heal(_consts.HealAmount);
        return true;
    }

    #endregion

    #region Damage (PlayerHealth から呼ばれる)

    /// <summary>被弾時に呼ばれる。HurtState へ遷移する。</summary>
    public void OnDamaged(in DamageInfo info)
    {
        if (_isDead)
            return;

        _lastDamage = info;
        _stateMachine.ChangeState(HurtState);
    }

    /// <summary>HP が 0 になった時に呼ばれる。DeadState へ遷移する。</summary>
    public void OnDied()
    {
        if (_isDead)
            return;

        _stateMachine.ChangeState(DeadState);
    }

    /// <summary>攻撃元と反対方向へノックバック初速を与える (HurtState.Enter から呼ばれる)。</summary>
    public void ApplyKnockback(Vector2 hitPoint)
    {
        var dir = transform.position.x >= hitPoint.x ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * _consts.KnockbackVelocity.x, _consts.KnockbackVelocity.y);
    }

    public void BeginHurt()
    {
        _isHurt = true;
        // ダッシュ・攻撃の割り込みで残ったフラグを掃除する
        _isDashing = false;
        _isAttacking = false;
    }

    public void EndHurt()
    {
        _isHurt = false;
    }

    public void BeginDeath()
    {
        _isDead = true;
        _isHurt = false;
        _isDashing = false;
        _isAttacking = false;
        if (_hasDeathParam)
            _animator.SetTrigger(ParamDeath);
    }

    #endregion

    #region Visual

    private void UpdateFacing()
    {
        // ダッシュ・攻撃・被弾・死亡中は向きを固定する
        if (_isDashing || _isAttacking || _isHurt || _isDead)
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
        if (_hasIsHurtParam)
            _animator.SetBool(ParamIsHurt, _isHurt);
    }

    #endregion
}
