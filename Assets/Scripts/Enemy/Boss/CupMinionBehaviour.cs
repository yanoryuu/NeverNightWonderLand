using UnityEngine;

/// <summary>
/// 中ボス「カップ」第1段階の小さなカップ 1 体のうごき (Docs/EnemySpec.md「上の中ボス「カップ」」)。
/// ボスのルート (<see cref="CupBossBehaviour"/>) の子に置き、自分の EnemyController (HP 40 / 防御値 20 / 接触 1) を持つ。
///
/// - プレイヤーが闘技場に入るまで (<see cref="CupBossBehaviour.Engaged"/>) は初期位置でふわふわ揺れているだけ。
/// - 戦闘中はふわふわ浮いてプレイヤーに寄り、体当たり (接触ダメージ) だけで攻撃する。
///   寄る位置はプレイヤーの周りを回りながら近づいたり離れたりする (3 体は位相をずらす)。仲間とは少し間隔を取る。
/// - HP が下限 (EnemyController.HpFloor = 1) まで減ると倒れずに「ダウン」: 無敵・接触ダメージなし・当たり判定なしになり、
///   ルートが決めた集合点の空中へふらふら飛んでいって待つ。3 体そろうとルートが合体させる。
/// - 合体後 (第2段階) は絵だけの部品になり、ルートが位置を直接動かす (分離回転・撃破時)。
/// 小さなカップは決して死なないので、BossArena の撃破フラグ (ルートの EnemyController.OnDied) は立たない。
/// </summary>
public class CupMinionBehaviour : EnemyBehaviour
{
    #region Serialized Fields

    [Header("寄り方")]
    [Tooltip("寄る速さの上限 (units/sec)。0 以下なら定数アセットの ChaseSpeed")]
    [SerializeField] private float _chaseSpeed = 0f;

    [Tooltip("目標位置へ寄る強さ (目標までの距離 × これ = 速さ)")]
    [SerializeField] private float _steerGain = 2.5f;

    [Tooltip("プレイヤーの周りを回る角度の初期値 (度)。3 体でずらす")]
    [SerializeField] private float _circleAngle = 90f;

    [Tooltip("プレイヤーの周りを回る速さ (度/sec)")]
    [SerializeField] private float _circleSpeed = 35f;

    [Tooltip("プレイヤーからの距離の最小・最大 (units)。この間を行き来して、近づいた時に体当たりになる")]
    [SerializeField] private Vector2 _swoopRadius = new Vector2(0.2f, 2.6f);

    [Tooltip("近づいたり離れたりする速さ (rad/sec)")]
    [SerializeField] private float _swoopSpeed = 1.1f;

    [Tooltip("プレイヤーにこの距離 (units, 中心どうし) まで近づいたら体当たりした扱いにして、いったん離れる")]
    [SerializeField] private float _bumpDistance = 0.6f;

    [Tooltip("体当たりの後、離れている時間 (sec)。この間は最大距離 + 1 を目標にする")]
    [SerializeField] private float _backOffTime = 1.5f;

    [Tooltip("仲間のカップとこの距離 (units) より近いと離れる")]
    [SerializeField] private float _separation = 1.6f;

    [Tooltip("仲間から離れる強さ")]
    [SerializeField] private float _separationGain = 3f;

    [Header("揺れ")]
    [Tooltip("上下の揺れ幅 (units)")]
    [SerializeField] private float _bobAmplitude = 0.3f;

    [Tooltip("上下の揺れの速さ (rad/sec)")]
    [SerializeField] private float _bobSpeed = 2.4f;

    [Tooltip("揺れの位相 (rad)。3 体でずらす")]
    [SerializeField] private float _bobPhase = 0f;

    [Header("ダウン")]
    [Tooltip("ダウン中の色 (少し暗くする)")]
    [SerializeField] private Color _downTint = new Color(0.65f, 0.65f, 0.72f, 1f);

    [Tooltip("ダウンして集合点へ飛んでいく速さ (units/sec)")]
    [SerializeField] private float _downMoveSpeed = 5f;

    [Tooltip("ダウン中のふらつき (左右の揺れ幅 units)")]
    [SerializeField] private float _downWobble = 0.35f;

    [Tooltip("ダウン中の傾きの揺れ (度)")]
    [SerializeField] private float _downTilt = 18f;

    [Tooltip("頭上の HP 表示 (StatusCanvas)。ダウン・合体中は隠す。未設定なら子の Canvas")]
    [SerializeField] private GameObject _statusCanvas;

    #endregion

    #region Fields

    private CupBossBehaviour _owner;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private SpriteRenderer _sprite;
    private float _time;
    private bool _down;
    private bool _gathered;
    private bool _puppet;   // ルートが位置を直接動かしている (合体の演出・第2段階)
    private Vector2 _gatherTarget;
    private float _backOffTimer;

    #endregion

    #region Properties

    /// <summary>HP が下限まで減ってダウンしたか。</summary>
    public bool IsDown => _down;

    /// <summary>ダウンして集合点に着いたか。</summary>
    public bool IsGathered => _gathered;

    /// <summary>見た目の SpriteRenderer (撃破時の割れる演出で絵を借りる)。</summary>
    public SpriteRenderer Sprite => _sprite;

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _owner = GetComponentInParent<CupBossBehaviour>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _rb.gravityScale = 0f;
        if (_statusCanvas == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                _statusCanvas = canvas.gameObject;
        }
        _time = _bobPhase;
    }

    private void Start()
    {
        // HP 0 で倒れないよう下限を 1 にする (下限に達したらダウン)
        if (Enemy != null)
            Enemy.HpFloor = 1;
    }

    private void Update()
    {
        if (_down || Enemy == null || Enemy.Hp == null || Enemy.IsDead)
            return;

        // TakeDamage の途中 (購読コールバック) ではなくここで処理する (ブレイク処理と競合させない)
        if (Enemy.Hp.CurrentValue <= Mathf.Max(1, Enemy.HpFloor))
            GoDown();
    }

    private void FixedUpdate()
    {
        if (!_down || _puppet)
            return;

        // ダウン中: 集合点へふらふら飛んでいく (EnemyController は止めているのでここで動かす)
        var dt = Time.fixedDeltaTime;
        _time += dt;
        var pos = _rb.position;
        var wobble = new Vector2(Mathf.Sin(_time * 3.1f) * _downWobble, Mathf.Sin(_time * 4.3f) * _downWobble * 0.5f);
        var target = _gatherTarget + wobble;
        var next = Vector2.MoveTowards(pos, target, _downMoveSpeed * dt);
        _rb.MovePosition(next);
        _rb.MoveRotation(Mathf.Sin(_time * 2.7f) * _downTilt);
        if (!_gathered && (_gatherTarget - pos).magnitude <= _downWobble + 0.25f)
            _gathered = true;
    }

    #endregion

    #region EnemyBehaviour

    public override void PhysicsTick()
    {
        var dt = Time.fixedDeltaTime;
        _time += dt;
        var bob = Mathf.Cos(_time * _bobSpeed) * _bobAmplitude * _bobSpeed;

        // プレイヤーが闘技場に入るまでは初期位置で揺れているだけ
        if ((_owner != null && !_owner.Engaged) || Player == null)
        {
            var home = Vector2.ClampMagnitude((HomePosition - _rb.position) * _steerGain, 2f);
            _rb.linearVelocity = new Vector2(home.x, home.y + bob);
            return;
        }

        var speed = _chaseSpeed > 0f ? _chaseSpeed : Enemy.Consts.ChaseSpeed;
        var player = BossKit.PlayerCenter() ?? (Vector2)Player.position;
        var angle = (_circleAngle + _time * _circleSpeed) * Mathf.Deg2Rad;
        // 近づくタイミングは 3 体でずらす (回る角度の初期値 = 体ごとに違う値を位相にも使う)
        var t = 0.5f + 0.5f * Mathf.Sin(_time * _swoopSpeed + _bobPhase + _circleAngle * Mathf.Deg2Rad);
        var radius = Mathf.Lerp(_swoopRadius.x, _swoopRadius.y, t);

        // 体当たりしたら少しの間離れる (張り付いて当たり続けないように。2026-10-12 プレイ確認)
        _backOffTimer -= dt;
        if (_backOffTimer <= 0f && (player - _rb.position).magnitude <= _bumpDistance)
            _backOffTimer = _backOffTime;
        if (_backOffTimer > 0f)
            radius = _swoopRadius.y + 1f;
        var target = player + new Vector2(Mathf.Cos(angle), Mathf.Abs(Mathf.Sin(angle)) * 0.8f + 0.2f) * radius;
        if (_owner != null)
            target = _owner.ClampToArena(target, 0.8f);

        var velocity = Vector2.ClampMagnitude((target - _rb.position) * _steerGain, speed);
        velocity += Separation() * _separationGain;
        velocity.y += bob;
        _rb.linearVelocity = velocity;

        var dx = player.x - _rb.position.x;
        if (Mathf.Abs(dx) > 0.2f)
            Enemy.Face(Mathf.Sign(dx));
    }

    public override void OnInterrupted()
    {
        // ノックバック等の中断後、漂流しないよう止める (重力 0 のため)
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    /// <summary>ダウンしていない仲間から離れる向き (近いほど強い)。</summary>
    private Vector2 Separation()
    {
        if (_owner == null)
            return Vector2.zero;

        var push = Vector2.zero;
        var pos = _rb.position;
        foreach (var other in _owner.Minions)
        {
            if (other == null || other == this || other.IsDown)
                continue;
            var d = pos - (Vector2)other.transform.position;
            var dist = d.magnitude;
            if (dist >= _separation)
                continue;
            push += (dist > 0.01f ? d / dist : Vector2.up) * (1f - dist / _separation);
        }
        return push;
    }

    #endregion

    #region ダウン・合体 (ルートから呼ばれる)

    /// <summary>
    /// ダウン: 無敵・接触ダメージなし・当たり判定なしにして、集合点へ飛んでいく。
    /// EnemyController は止める (Active から外れて裁断の対象にもならない)。
    /// </summary>
    private void GoDown()
    {
        _down = true;
        _gathered = false;
        Enemy.ClearStun();
        Enemy.IsInvulnerable = true;
        Enemy.enabled = false;
        if (_col != null)
            _col.enabled = false;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        if (_statusCanvas != null)
            _statusCanvas.SetActive(false);
        _sprite.color = _downTint;
        // 先に知らせる (最初の 1 体のダウンで集合点が決まる)
        _owner?.NotifyDown(this);
        _gatherTarget = _owner != null ? _owner.GatherPoint(this) : _rb.position;
    }

    /// <summary>
    /// ルートが位置を直接動かす状態にする (合体の演出・第2段階の分離回転・撃破)。物理は止める。
    /// </summary>
    public void BecomePuppet()
    {
        _puppet = true;
        _down = true;
        _gathered = true;
        Enemy.IsInvulnerable = true;
        if (Enemy.enabled)
            Enemy.enabled = false;
        if (_col != null)
            _col.enabled = false;
        if (_statusCanvas != null)
            _statusCanvas.SetActive(false);
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
    }

    /// <summary>位置・傾き・大きさを直接置く (BecomePuppet の後に使う)。</summary>
    public void SetPose(Vector2 position, float angle, float scale)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>絵の表示・色を切り替える。</summary>
    public void SetVisible(bool visible, Color? tint = null)
    {
        _sprite.enabled = visible;
        if (tint.HasValue)
            _sprite.color = tint.Value;
    }

    #endregion
}
