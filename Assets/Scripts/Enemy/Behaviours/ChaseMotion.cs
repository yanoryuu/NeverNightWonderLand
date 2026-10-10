using UnityEngine;

/// <summary>
/// 移動部品: 発見中のプレイヤーを追跡する。
/// 発見判定は EnemyController.IsPlayerDetected (EnemyPerception があればヒステリシス・
/// 記憶・視線つき、無ければ ChaseRange の距離判定)。
/// 未発見では IsActive が false になるので、下に置いた移動部品 (巡回など) に切り替わる。
/// 任意で「振り向きの遅れ」(飛び越えて背後を取れる) と「足場の端で止まる」を付けられる
/// (既定はどちらも無効 = 従来どおりの挙動。既存プレハブ互換のため)。
/// </summary>
public class ChaseMotion : EnemyMotionBase
{
    [Tooltip("振り向きにかかる時間 (sec)。プレイヤーが背後へ回るとこの間その場で止まる。0 で即座に振り向く (従来の挙動)")]
    [SerializeField] private float _turnDelay = 0f;

    [Tooltip("足場の端で止まる (落ちない) か")]
    [SerializeField] private bool _stopAtLedge = false;

    [Tooltip("足場の端の判定に使う地面レイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    private Rigidbody2D _rb;
    private Collider2D _body;
    private int _facing;           // 今向いている方向 (0 = 不明)
    private float _turnTimer;      // 振り向き中の経過時間
    private float _lastTickTime = -1f;
    private bool _keepFacingOnResume; // 被弾明けはノックバックの速度で向きを読み直さない

    public override bool IsActive => Player != null && Enemy.IsPlayerDetected;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _body = GetComponent<Collider2D>();
    }

    public override void PhysicsTick()
    {
        var dir = Player.position.x >= transform.position.x ? 1 : -1;

        // 従来の挙動 (オプション無し)
        if (_turnDelay <= 0f && !_stopAtLedge)
        {
            Enemy.Move(dir * Enemy.Consts.ChaseSpeed);
            return;
        }

        // 巡回などから切り替わった直後は、今の進行方向を向きとみなす
        var dt = Time.fixedDeltaTime;
        if (_keepFacingOnResume)
        {
            _keepFacingOnResume = false;
            _turnTimer = 0f;
        }
        else if (_lastTickTime < 0f || Time.fixedTime - _lastTickTime > dt * 1.5f)
        {
            var vx = _rb != null ? _rb.linearVelocity.x : 0f;
            _facing = Mathf.Abs(vx) > 0.05f ? (vx > 0f ? 1 : -1) : 0;
            _turnTimer = 0f;
        }
        _lastTickTime = Time.fixedTime;

        // プレイヤーの真上/真下付近では向きを保つ (左右に震えないように)
        if (_facing != 0 && Mathf.Abs(Player.position.x - transform.position.x) < 0.1f)
            dir = _facing;

        // 振り向き: 逆側に回られたら _turnDelay の間その場で止まってから向きを変える
        if (_facing == 0 || _turnDelay <= 0f)
        {
            _facing = dir;
        }
        else if (dir != _facing)
        {
            _turnTimer += dt;
            if (_turnTimer < _turnDelay)
            {
                Enemy.Move(0f);
                return;
            }

            _facing = dir;
            _turnTimer = 0f;
        }
        else
        {
            _turnTimer = 0f;
        }

        // 足場の端: 接地している時だけ、先に地面が無ければ止まる
        var ground = EnemyMotionUtil.GroundOrDefault(_groundLayer);
        if (_stopAtLedge && EnemyMotionUtil.IsGrounded(_body, ground)
            && !EnemyMotionUtil.HasGroundAhead(_body, _facing, ground))
        {
            EnemyMotionUtil.FaceToward(Enemy, _facing);
            Enemy.Move(0f);
            return;
        }

        Enemy.Move(_facing * Enemy.Consts.ChaseSpeed);
    }

    public override void OnInterrupted()
    {
        // 被弾・拘束・ブレイク明けは中断前の向きのまま再開する (振り向き途中の経過は捨てる)
        // (追跡中に中断された時だけ。巡回中の中断なら切り替え時に読み直す)
        _keepFacingOnResume = _facing != 0 && _lastTickTime >= 0f
                              && Time.fixedTime - _lastTickTime <= Time.fixedDeltaTime * 1.5f;
        _turnTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        var enemy = Enemy != null ? Enemy : GetComponent<EnemyController>();
        if (enemy == null || enemy.Consts == null || enemy.Consts.ChaseRange <= 0f)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemy.Consts.ChaseRange);
    }
}
