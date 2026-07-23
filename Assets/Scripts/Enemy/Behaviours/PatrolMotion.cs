using UnityEngine;

/// <summary>
/// 移動部品: 初期位置を中心に巡回し、範囲の端や壁で反転する。
/// 常に IsActive なので、優先度の低いフォールバック移動として一番下に置く。
/// </summary>
public class PatrolMotion : EnemyMotionBase
{
    private int _moveDir = 1;

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
