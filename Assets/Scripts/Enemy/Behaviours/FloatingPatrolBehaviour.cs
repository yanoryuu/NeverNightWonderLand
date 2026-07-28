using UnityEngine;

/// <summary>
/// 空中に浮かぶ敵のうごき (仮実装・差し替え可)。重力なしで基準高さの周りをふわふわ上下
/// しながら水平に巡回し、プレイヤーを発見したら緩やかに追尾する。
/// 発見判定は EnemyController.IsPlayerDetected (EnemyPerception 装着で調整可能) を使う。
/// </summary>
public class FloatingPatrolBehaviour : EnemyBehaviour
{
    [Tooltip("巡回の折り返し幅 (基準位置から左右, units)")]
    [SerializeField] private float _patrolRange = 3f;

    [Tooltip("巡回の移動速度 (units/sec)")]
    [SerializeField] private float _moveSpeed = 2f;

    [Tooltip("追尾の移動速度 (units/sec)")]
    [SerializeField] private float _chaseSpeed = 3.2f;

    [Tooltip("ふわふわ上下の振れ幅 (units)")]
    [SerializeField] private float _hoverAmplitude = 0.5f;

    [Tooltip("ふわふわ上下の速さ (rad/sec)")]
    [SerializeField] private float _hoverFrequency = 2f;

    private Rigidbody2D _rb;
    private int _patrolDir = 1;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; // 浮遊
    }

    public override void PhysicsTick()
    {
        float vx;
        float vy;

        if (Enemy.IsPlayerDetected && Player != null)
        {
            // プレイヤーの少し上を狙って緩追尾
            var to = (Vector2)Player.position + Vector2.up * 0.5f - _rb.position;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.zero;
            vx = dir.x * _chaseSpeed;
            vy = dir.y * _chaseSpeed;
        }
        else
        {
            // 基準位置の周りを水平巡回 + 上下ふわふわ
            if (_rb.position.x > HomePosition.x + _patrolRange) _patrolDir = -1;
            else if (_rb.position.x < HomePosition.x - _patrolRange) _patrolDir = 1;
            if (Enemy.HitWallLastStep) _patrolDir = -_patrolDir;

            vx = _patrolDir * _moveSpeed;
            var targetY = HomePosition.y + Mathf.Sin(Time.time * _hoverFrequency) * _hoverAmplitude;
            vy = Mathf.Clamp((targetY - _rb.position.y) * 3f, -2f, 2f);
        }

        Enemy.Move(vx); // 向きの反転と壁当たり検知を揃える
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, vy);
    }

    public override void OnInterrupted()
    {
        // ノックバック等の中断後、浮遊へ戻す (重力 0 のため漂流しないよう止める)
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }
}
