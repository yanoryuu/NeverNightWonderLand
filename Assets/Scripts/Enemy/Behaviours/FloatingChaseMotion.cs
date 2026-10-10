using UnityEngine;

/// <summary>
/// 移動部品: 浮遊しながら発見中のプレイヤーへ緩やかに追尾する (少し上を狙う)。
/// 発見判定は EnemyController.IsPlayerDetected (EnemyPerception があれば調整可能)。
/// 未発見では IsActive が false になるので、下に置いた浮遊巡回などに切り替わる。
/// 速度は EnemyConsts.ChaseSpeed を使う。
/// 任意で「間合いを取る」動きにできる (既定はすべて 0 = 従来どおりプレイヤーへまっすぐ寄る):
/// - 間合い (_keepDistance): プレイヤーから一定距離の位置を保って寄る (飛んで突進してくる敵)
/// - 陣取り (_standOffset): プレイヤーの斜め上 (自分がいる側) の位置に陣取る (飛んで弾を撃つ敵)
/// - 離れる距離 (_retreatDistance): これより近づかれたらプレイヤーから離れる
/// </summary>
public class FloatingChaseMotion : FloatingMotionBase
{
    [Tooltip("プレイヤーの何 units 上を狙うか")]
    [SerializeField] private float _aimHeight = 0.5f;

    [Header("間合い (すべて 0 なら従来どおりまっすぐ寄る)")]
    [Tooltip("プレイヤーからこの距離の位置を保って寄る (units)。0 で使わない")]
    [SerializeField] private float _keepDistance = 0f;

    [Tooltip("プレイヤーから見たこの位置に陣取る (x は自分がいる側へ反転, units)。(0,0) で使わない。_keepDistance より優先")]
    [SerializeField] private Vector2 _standOffset = Vector2.zero;

    [Tooltip("_keepDistance を使う時、目標位置をプレイヤーよりこの高さ以上の上に取る (units)。0 で使わない (突進の後に床をはわず浮き上がる)")]
    [SerializeField] private float _keepHeight = 0f;

    [Tooltip("プレイヤーがこれより近づいたら離れる (units)。0 で使わない")]
    [SerializeField] private float _retreatDistance = 0f;

    [Tooltip("目標位置のこの距離以内では減速する (units)。目標付近で震えないように")]
    [SerializeField] private float _arriveRadius = 1f;

    public override bool IsActive => Player != null && Enemy.IsPlayerDetected;

    private bool UsesSpacing => _keepDistance > 0f || _standOffset != Vector2.zero || _retreatDistance > 0f;

    public override void PhysicsTick()
    {
        var speed = Enemy.Consts.ChaseSpeed;
        var aim = (Vector2)Player.position + Vector2.up * _aimHeight;
        var self = (Vector2)transform.position;

        // 従来の挙動
        if (!UsesSpacing)
        {
            var to = aim - self;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.zero;
            MoveFloating(dir.x * speed, dir.y * speed);
            return;
        }

        var fromPlayer = self - aim;
        var distance = fromPlayer.magnitude;
        var side = self.x >= aim.x ? 1f : -1f;

        // 近づかれすぎたら離れる (全速)
        if (_retreatDistance > 0f && distance < _retreatDistance)
        {
            var away = distance > 0.01f ? fromPlayer / distance : new Vector2(side, 0f);
            MoveKeepFacing(away * speed, aim);
            return;
        }

        // 目標位置: 陣取り位置 > 間合い > プレイヤー
        Vector2 target;
        if (_standOffset != Vector2.zero)
            target = aim + new Vector2(_standOffset.x * side, _standOffset.y);
        else if (_keepDistance > 0f)
        {
            var dir = fromPlayer;
            if (_keepHeight > 0f)
                dir.y = Mathf.Max(dir.y, _keepHeight);
            target = aim + (dir.sqrMagnitude > 0.0001f ? dir.normalized : new Vector2(side, 0f)) * _keepDistance;
            if (_keepHeight > 0f)
                target.y = Mathf.Max(target.y, aim.y + _keepHeight);
        }
        else
            target = aim;

        var toTarget = target - self;
        var dist = toTarget.magnitude;
        var scale = _arriveRadius > 0f ? Mathf.Clamp01(dist / _arriveRadius) : 1f;
        var velocity = dist > 0.01f ? toTarget / dist * (speed * scale) : Vector2.zero;
        MoveKeepFacing(velocity, aim);
    }

    /// <summary>
    /// 間合いを取る動きは後退することがあるので、向きは常にプレイヤーへ向けたまま速度だけ与える。
    /// </summary>
    private void MoveKeepFacing(Vector2 velocity, Vector2 aim)
    {
        var face = aim.x >= transform.position.x ? 1 : -1;
        EnemyMotionUtil.FaceToward(Enemy, face);
        Rb.linearVelocity = velocity;
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
