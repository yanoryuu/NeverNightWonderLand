using UnityEngine;

/// <summary>
/// 行動部品: 予備動作 (点滅) のあと、プレイヤーの足元へ爆弾を放物線で投げる (ポップコーン投げ / PopcornThrower)。
/// 流れは <see cref="EnemyShootAction"/> と同じ (発見中・射程内・視線が通っている時だけ予備動作を始め、
/// 予備動作中に見失ったら取りやめ。被弾・拘束・ブレイクで中断してクールダウンから再開)。
/// 投げるもの = <see cref="EnemyLobBomb"/> (プレハブ未設定なら小さな丸い仮表示)。
/// 軌道: 狙い = 投げた瞬間のプレイヤーの足元。頂点の高さ (_apexHeight) と滞空時間 (距離に応じて
/// _flightTimeNear〜_flightTimeFar) を決めてから、その 2 つを満たす重力と初速を求める
/// (狙いの高さを過ぎても床に着くまでは同じ重力で落ち続ける)。
/// </summary>
public class EnemyLobAction : EnemyActionBase
{
    #region Serialized Fields

    [Header("発動")]
    [Tooltip("投げ始める距離 (units)。0 なら発見中ならどこでも")]
    [SerializeField] private float _range = 10f;

    [Tooltip("地形に遮られていない時だけ投げる")]
    [SerializeField] private bool _requireLineOfSight = true;

    [Tooltip("予備動作の時間 (sec)")]
    [SerializeField] private float _windupTime = 0.5f;

    [Tooltip("次に投げられるまでの時間 (sec)。投げた時 または中断から数える")]
    [SerializeField] private float _cooldown = 3f;

    [Tooltip("発見してから最初に投げられるまでの時間 (sec)")]
    [SerializeField] private float _firstThrowDelay = 0.5f;

    [Header("軌道")]
    [Tooltip("頂点の高さ (units)。投げた敵の足元と狙い (プレイヤーの足元) の高い方から測る")]
    [SerializeField] private float _apexHeight = 3f;

    [Tooltip("近い相手への滞空時間 (sec)")]
    [SerializeField] private float _flightTimeNear = 0.7f;

    [Tooltip("遠い相手 (_farDistance 以上) への滞空時間 (sec)")]
    [SerializeField] private float _flightTimeFar = 0.9f;

    [Tooltip("滞空時間が最長になる横の距離 (units)")]
    [SerializeField] private float _farDistance = 9f;

    [Tooltip("投げる位置 (体の中心からのずれ, units。x は向きに合わせて反転する)")]
    [SerializeField] private Vector2 _muzzleOffset = new Vector2(0.2f, 0.3f);

    [Header("爆弾")]
    [Tooltip("爆弾のプレハブ (EnemyLobBomb)。未設定なら小さな丸い仮表示を作る")]
    [SerializeField] private EnemyLobBomb _bombPrefab;

    [Tooltip("着地から爆発までの時間 (sec)")]
    [SerializeField] private float _fuseTime = 0.8f;

    [Tooltip("爆発半径 (units)")]
    [SerializeField] private float _explosionRadius = 1.2f;

    [Tooltip("爆発のダメージ")]
    [SerializeField] private int _damage = 2;

    [Tooltip("仮表示の爆弾の色 (プレハブ未設定時)")]
    [SerializeField] private Color _fallbackBombColor = new Color(1f, 0.96f, 0.72f);

    [Tooltip("爆発パーティクルの素材 (URP の ParticlesUnlit)。空ならパーティクルは出さない (閃光の円だけ)")]
    [SerializeField] private Material _explosionMaterial;

    [Header("地形")]
    [Tooltip("視線を遮る・爆弾が止まる地形のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("見た目 (予備動作の点滅)")]
    [Tooltip("点滅させる SpriteRenderer。未設定なら本体の絵を自動で探す")]
    [SerializeField] private SpriteRenderer _blinkRenderer;

    [Tooltip("点滅の色")]
    [SerializeField] private Color _blinkColor = new Color(1f, 0.85f, 0.4f);

    [Tooltip("点滅の間隔 (sec)")]
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Animator トリガー (無ければ無視される)")]
    [SerializeField] private string _windupTrigger = "ThrowWindup";
    [SerializeField] private string _throwTrigger = "Throw";

    #endregion

    private Collider2D _body;
    private SpriteRenderer _visual;
    private EnemyTelegraphBlink _blink;
    private Collider2D _playerCollider;
    private Transform _playerCached;

    private bool _winding;
    private float _timer;
    private float _readyTime;
    private bool _wasDetected;

    /// <summary>予備動作中か。</summary>
    public bool IsWindingUp => _winding;

    public override bool IsActive => Player != null;

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Collider2D>();
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
    }

    #endregion

    public override void PhysicsTick() { }

    public override void LogicTick()
    {
        var detected = Enemy.IsPlayerDetected;

        // 発見した瞬間は少し待ってから投げる
        if (detected && !_wasDetected)
            _readyTime = Mathf.Max(_readyTime, Time.time + _firstThrowDelay);
        _wasDetected = detected;

        if (_winding)
        {
            // 見失った・視線が切れたら取りやめ
            if (!detected || !CanSee())
            {
                Cancel(false);
                return;
            }

            _blink.Tick(Time.deltaTime);
            _timer += Time.deltaTime;
            if (_timer >= _windupTime)
                Throw();
            return;
        }

        if (!detected || Time.time < _readyTime || !InRange() || !CanSee())
            return;

        _winding = true;
        _timer = 0f;
        _blink.Start();
        Animation?.SetTrigger(_windupTrigger);
    }

    public override void OnInterrupted()
    {
        if (_winding)
            Cancel(true);
    }

    private Vector2 BodyCenter => _body != null ? (Vector2)_body.bounds.center : (Vector2)transform.position;

    private Vector2 Muzzle
    {
        get
        {
            var side = Player != null && Player.position.x < transform.position.x ? -1f : 1f;
            return BodyCenter + new Vector2(_muzzleOffset.x * side, _muzzleOffset.y);
        }
    }

    private Collider2D PlayerCollider
    {
        get
        {
            if (_playerCached != Player)
            {
                _playerCached = Player;
                _playerCollider = Player != null ? Player.GetComponent<Collider2D>() : null;
            }
            return _playerCollider;
        }
    }

    private Vector2 PlayerCenter => PlayerCollider != null ? (Vector2)PlayerCollider.bounds.center : (Vector2)Player.position;

    /// <summary>プレイヤーの足元 (当たり判定の下端の中央)。</summary>
    private Vector2 PlayerFeet
    {
        get
        {
            if (PlayerCollider == null)
                return Player.position;
            var b = PlayerCollider.bounds;
            return new Vector2(b.center.x, b.min.y);
        }
    }

    private bool InRange() =>
        _range <= 0f || Vector2.Distance(BodyCenter, PlayerCenter) <= _range;

    private bool CanSee() =>
        !_requireLineOfSight
        || EnemyMotionUtil.HasLineOfSight(BodyCenter, PlayerCenter, EnemyMotionUtil.GroundOrDefault(_groundLayer));

    private void Throw()
    {
        _winding = false;
        _blink.Stop();
        _readyTime = Time.time + _cooldown;
        Animation?.SetTrigger(_throwTrigger);

        var from = Muzzle;
        var target = PlayerFeet;
        if (Player.position.x != transform.position.x)
            Enemy.Face(Player.position.x - transform.position.x);

        SolveArc(from, target, out var velocity, out var gravity);
        var bomb = EnemyLobBomb.Spawn(_bombPrefab, from, velocity, gravity, _fuseTime, _explosionRadius, _damage,
            gameObject, _groundLayer, _explosionMaterial, _visual, _fallbackBombColor);
        // 足場の上から下のプレイヤーへ投げた時は、狙い (足元) より高いすり抜け床を通り抜けて狙いの高さまで落ちる
        if (bomb != null)
            bomb.PassOneWayAbove(target.y + 0.5f);
    }

    /// <summary>
    /// from から target へ、頂点が (足元と狙いの高い方 + _apexHeight)、滞空が距離に応じた時間になる放物線の
    /// 初速と重力を求める。
    /// </summary>
    private void SolveArc(Vector2 from, Vector2 target, out Vector2 velocity, out float gravity)
    {
        var dx = target.x - from.x;
        var t = Mathf.Lerp(_flightTimeNear, _flightTimeFar,
            _farDistance > 0f ? Mathf.Clamp01(Mathf.Abs(dx) / _farDistance) : 1f);
        t = Mathf.Max(0.1f, t);

        var feetY = _body != null ? _body.bounds.min.y : from.y;
        var apexY = Mathf.Max(feetY, target.y) + Mathf.Max(0.1f, _apexHeight);
        apexY = Mathf.Max(apexY, from.y + 0.3f, target.y + 0.3f);

        // 上り a、下り b の高さを、合計 t 秒で上って下りる重力: sqrt(g) = (√2a + √2b) / t
        var a = apexY - from.y;
        var b = apexY - target.y;
        var sqrtG = (Mathf.Sqrt(2f * a) + Mathf.Sqrt(2f * b)) / t;
        gravity = sqrtG * sqrtG;
        velocity = new Vector2(dx / t, Mathf.Sqrt(2f * gravity * a));
    }

    private void Cancel(bool startCooldown)
    {
        _winding = false;
        _timer = 0f;
        _blink.Stop();
        if (startCooldown)
            _readyTime = Time.time + _cooldown;
    }

    private void HandleDied(EnemyController _) => Cancel(false);

    private void OnDrawGizmosSelected()
    {
        if (_range <= 0f)
            return;
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _range);
    }
}
