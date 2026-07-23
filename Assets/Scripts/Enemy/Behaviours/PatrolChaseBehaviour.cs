using UnityEngine;

/// <summary>
/// 標準のうごき: 初期位置を中心に巡回し、範囲の端や壁で反転する。
/// ChaseRange が設定されたタイプ (俊敏型) は、検知範囲内のプレイヤーを追跡する。
/// EnemyBehaviour 未装着の敵には EnemyController がこれを自動で付ける (既存アセット互換)。
/// </summary>
public class PatrolChaseBehaviour : EnemyBehaviour
{
    private int _moveDir = 1;

    public override void PhysicsTick()
    {
        var consts = Enemy.Consts;
        var speed = consts.MoveSpeed;

        // 俊敏型: 発見中のプレイヤーを追跡する (巡回範囲は無視)。
        // 発見判定は EnemyPerception 装着でヒステリシス・記憶・視線つきになる
        var chasing = false;
        var player = Player;
        if (player != null && Enemy.IsPlayerDetected)
        {
            chasing = true;
            speed = consts.ChaseSpeed;
            _moveDir = player.position.x >= transform.position.x ? 1 : -1;
        }

        if (!chasing)
        {
            // 巡回範囲の端まで来たら反転する
            var x = transform.position.x;
            if (_moveDir > 0 && x > HomePosition.x + consts.PatrolHalfWidth) _moveDir = -1;
            else if (_moveDir < 0 && x < HomePosition.x - consts.PatrolHalfWidth) _moveDir = 1;

            // 壁に当たって動けていなければ反転する
            if (Enemy.HitWallLastStep)
                _moveDir = -_moveDir;
        }

        Enemy.Move(_moveDir * speed);
    }

    private void OnDrawGizmosSelected()
    {
        var enemy = Enemy != null ? Enemy : GetComponent<EnemyController>();
        if (enemy == null || enemy.Consts == null)
            return;

        // 巡回範囲
        var origin = Application.isPlaying ? HomePosition : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            origin + Vector2.left * enemy.Consts.PatrolHalfWidth,
            origin + Vector2.right * enemy.Consts.PatrolHalfWidth);

        // 追跡の検知範囲 (俊敏型)
        if (enemy.Consts.ChaseRange > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, enemy.Consts.ChaseRange);
        }
    }
}
