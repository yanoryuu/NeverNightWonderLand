using UnityEngine;

/// <summary>
/// 移動部品: 発見中のプレイヤーを追跡する。
/// 発見判定は EnemyController.IsPlayerDetected (EnemyPerception があればヒステリシス・
/// 記憶・視線つき、無ければ ChaseRange の距離判定)。
/// 未発見では IsActive が false になるので、下に置いた移動部品 (巡回など) に切り替わる。
/// </summary>
public class ChaseMotion : EnemyMotionBase
{
    public override bool IsActive => Player != null && Enemy.IsPlayerDetected;

    public override void PhysicsTick()
    {
        var dir = Player.position.x >= transform.position.x ? 1 : -1;
        Enemy.Move(dir * Enemy.Consts.ChaseSpeed);
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
