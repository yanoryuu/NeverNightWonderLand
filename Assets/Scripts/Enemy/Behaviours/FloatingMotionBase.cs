using UnityEngine;

/// <summary>
/// 浮遊する移動部品の共通基底。重力を切り、縦方向の速度も扱えるようにする。
/// 浮遊しながらの巡回/追跡 (FloatingPatrolMotion / FloatingChaseMotion) が継承する。
/// 水平は EnemyController.Move (向き反転・壁当たり検知つき) を使い、縦は Rigidbody2D へ直接書く。
/// </summary>
public abstract class FloatingMotionBase : EnemyMotionBase
{
    private Rigidbody2D _rb;

    /// <summary>浮遊体の Rigidbody2D。</summary>
    protected Rigidbody2D Rb => _rb;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; // 浮遊
    }

    /// <summary>水平は Move (向き反転つき)、縦は速度を直接与える。</summary>
    protected void MoveFloating(float vx, float vy)
    {
        Enemy.Move(vx);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, vy);
    }

    public override void OnInterrupted()
    {
        // ノックバック等の中断後、漂流しないよう止める (重力 0 のため)
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }
}
