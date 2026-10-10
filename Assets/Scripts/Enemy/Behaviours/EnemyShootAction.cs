using UnityEngine;

/// <summary>
/// 行動部品: 予備動作 (点滅) のあとプレイヤーへ向けて弾を 1 発撃つ (飛んで弾を撃つ敵 / FlyingShooter)。
/// 移動部品 (FloatingChaseMotion の陣取りなど) と並行して動く。
/// 発見中・射程内・視線が通っている時だけ撃ち始め、予備動作中に見失ったら取りやめる。
/// 弾は <see cref="EnemyBullet"/> (プレハブ未設定なら丸い仮表示の弾)。
/// 被弾・拘束・ブレイクで予備動作は中断され、クールダウンから再開する。
/// 単発・1 種類の攻撃なので EnemyAttackSelectorBase (複数攻撃の排他選択) は使わず、
/// 予備動作→発射の流れだけを持つ。
/// </summary>
public class EnemyShootAction : EnemyActionBase
{
    #region Serialized Fields

    [Header("発動")]
    [Tooltip("撃ち始める距離 (units)。0 なら発見中ならどこでも")]
    [SerializeField] private float _range = 9f;

    [Tooltip("地形に遮られていない時だけ撃つ")]
    [SerializeField] private bool _requireLineOfSight = true;

    [Tooltip("予備動作の時間 (sec)")]
    [SerializeField] private float _windupTime = 0.4f;

    [Tooltip("次に撃てるまでの時間 (sec)。発射 または中断から数える")]
    [SerializeField] private float _cooldown = 2.5f;

    [Tooltip("発見してから最初に撃てるまでの時間 (sec)")]
    [SerializeField] private float _firstShotDelay = 0.5f;

    [Header("弾")]
    [Tooltip("弾のプレハブ (EnemyBullet)。未設定なら丸い仮表示の弾を作る")]
    [SerializeField] private EnemyBullet _bulletPrefab;

    [Tooltip("弾の速さ (units/sec)")]
    [SerializeField] private float _bulletSpeed = 7f;

    [Tooltip("弾のダメージ")]
    [SerializeField] private int _bulletDamage = 1;

    [Tooltip("弾の寿命 (sec)")]
    [SerializeField] private float _bulletLifetime = 4f;

    [Tooltip("発射位置 (本体の中心からのずれ, units。x は向きに合わせて反転しない)")]
    [SerializeField] private Vector2 _muzzleOffset = Vector2.zero;

    [Tooltip("仮表示の弾の色 (プレハブ未設定時)")]
    [SerializeField] private Color _fallbackBulletColor = new Color(1f, 0.4f, 0.7f);

    [Header("地形")]
    [Tooltip("視線を遮る・弾が消える地形のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("見た目 (予備動作の点滅)")]
    [Tooltip("点滅させる SpriteRenderer。未設定なら本体の絵を自動で探す")]
    [SerializeField] private SpriteRenderer _blinkRenderer;

    [Tooltip("点滅の色")]
    [SerializeField] private Color _blinkColor = new Color(1f, 0.45f, 0.45f);

    [Tooltip("点滅の間隔 (sec)")]
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Animator トリガー (無ければ無視される)")]
    [SerializeField] private string _windupTrigger = "ShootWindup";
    [SerializeField] private string _shootTrigger = "Shoot";

    #endregion

    private Collider2D _body;
    private SpriteRenderer _visual;
    private EnemyTelegraphBlink _blink;
    private readonly PlayerHitProbe _aimProbe = new();

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

        // 発見した瞬間は少し待ってから撃つ
        if (detected && !_wasDetected)
            _readyTime = Mathf.Max(_readyTime, Time.time + _firstShotDelay);
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
                Fire();
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

    private Vector2 Muzzle => (Vector2)_body.bounds.center + _muzzleOffset;

    private bool InRange() =>
        _range <= 0f || Vector2.Distance(Muzzle, _aimProbe.GetCenter(Player)) <= _range;

    private bool CanSee() =>
        !_requireLineOfSight
        || EnemyMotionUtil.HasLineOfSight(Muzzle, _aimProbe.GetCenter(Player), EnemyMotionUtil.GroundOrDefault(_groundLayer));

    private void Fire()
    {
        _winding = false;
        _blink.Stop();
        _readyTime = Time.time + _cooldown;
        Animation?.SetTrigger(_shootTrigger);

        var from = Muzzle;
        var to = _aimProbe.GetCenter(Player) - from;
        var dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;

        EnemyBullet.Spawn(_bulletPrefab, from, dir * _bulletSpeed, _bulletDamage, _bulletLifetime,
            gameObject, _groundLayer, _visual, _fallbackBulletColor);
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
        Gizmos.color = new Color(1f, 0.3f, 0.8f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _range);
    }
}
