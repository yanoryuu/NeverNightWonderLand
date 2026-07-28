using UnityEngine;

/// <summary>
/// 移動部品: 浮遊しながら発見中のプレイヤーへ緩やかに追尾する (少し上を狙う)。
/// 発見判定は EnemyController.IsPlayerDetected (EnemyPerception があれば調整可能)。
/// 未発見では IsActive が false になるので、下に置いた浮遊巡回などに切り替わる。
/// 速度は EnemyConsts.ChaseSpeed を使う。
/// </summary>
public class FloatingChaseMotion : FloatingMotionBase
{
    [Tooltip("プレイヤーの何 units 上を狙うか")]
    [SerializeField] private float _aimHeight = 0.5f;

    public override bool IsActive => Player != null && Enemy.IsPlayerDetected;

    public override void PhysicsTick()
    {
        var to = (Vector2)Player.position + Vector2.up * _aimHeight - (Vector2)transform.position;
        var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.zero;
        var speed = Enemy.Consts.ChaseSpeed;
        MoveFloating(dir.x * speed, dir.y * speed);
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
