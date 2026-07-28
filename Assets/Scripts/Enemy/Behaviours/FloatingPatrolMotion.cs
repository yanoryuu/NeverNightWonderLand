using UnityEngine;

/// <summary>
/// 移動部品: 浮遊しながら初期位置を中心に水平巡回し、基準高さの周りで上下にふわふわ揺れる。
/// 常に IsActive なので、浮遊敵のフォールバック移動として一番下に置く。
/// 巡回幅・速度は EnemyConsts (PatrolHalfWidth / MoveSpeed) を使う。
/// </summary>
public class FloatingPatrolMotion : FloatingMotionBase
{
    [Tooltip("ふわふわ上下の振れ幅 (units)")]
    [SerializeField] private float _hoverAmplitude = 0.5f;

    [Tooltip("ふわふわ上下の速さ (rad/sec)")]
    [SerializeField] private float _hoverFrequency = 2f;

    private int _moveDir = 1;

    public override void PhysicsTick()
    {
        var consts = Enemy.Consts;

        // 巡回範囲の端や壁で反転する
        var x = transform.position.x;
        if (_moveDir > 0 && x > HomePosition.x + consts.PatrolHalfWidth) _moveDir = -1;
        else if (_moveDir < 0 && x < HomePosition.x - consts.PatrolHalfWidth) _moveDir = 1;
        if (Enemy.HitWallLastStep)
            _moveDir = -_moveDir;

        // 基準高さの周りへふわふわ寄せる
        var targetY = HomePosition.y + Mathf.Sin(Time.time * _hoverFrequency) * _hoverAmplitude;
        var vy = Mathf.Clamp((targetY - transform.position.y) * 3f, -2f, 2f);

        MoveFloating(_moveDir * consts.MoveSpeed, vy);
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
