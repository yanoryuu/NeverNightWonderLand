using UnityEngine;

/// <summary>
/// 移動部品: 初期位置を中心に巡回し、範囲の端や壁で反転する。
/// 常に IsActive なので、優先度の低いフォールバック移動として一番下に置く。
/// 任意で「足場の端で引き返す」を付けられる (既定は無効 = 従来どおり。既存プレハブ互換のため)。
/// </summary>
public class PatrolMotion : EnemyMotionBase
{
    [Tooltip("足場の端で引き返す (落ちない) か")]
    [SerializeField] private bool _turnAtLedge = false;

    [Tooltip("足場の端の判定に使う地面レイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _groundLayer;

    private int _moveDir = 1;
    private Collider2D _body;

    protected override void Awake()
    {
        base.Awake();
        _body = GetComponent<Collider2D>();
    }

    public override void PhysicsTick()
    {
        var consts = Enemy.Consts;

        // 巡回範囲の端まで来たら反転する
        var x = transform.position.x;
        if (_moveDir > 0 && x > HomePosition.x + consts.PatrolHalfWidth) _moveDir = -1;
        else if (_moveDir < 0 && x < HomePosition.x - consts.PatrolHalfWidth) _moveDir = 1;

        // 壁に当たって動けていなければ反転する
        if (Enemy.HitWallLastStep)
            _moveDir = -_moveDir;

        // 足場の端で引き返す (接地している時だけ)
        if (_turnAtLedge)
        {
            var ground = EnemyMotionUtil.GroundOrDefault(_groundLayer);
            if (EnemyMotionUtil.IsGrounded(_body, ground) && !EnemyMotionUtil.HasGroundAhead(_body, _moveDir, ground))
                _moveDir = -_moveDir;
        }

        Enemy.Move(_moveDir * consts.MoveSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        var enemy = Enemy != null ? Enemy : GetComponent<EnemyController>();
        if (enemy == null || enemy.Consts == null)
            return;

        var origin = Application.isPlaying ? HomePosition : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            origin + Vector2.left * enemy.Consts.PatrolHalfWidth,
            origin + Vector2.right * enemy.Consts.PatrolHalfWidth);
    }
}
