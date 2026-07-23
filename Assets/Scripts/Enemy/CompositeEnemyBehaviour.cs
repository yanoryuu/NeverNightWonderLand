/// <summary>
/// 複数のうごき部品 (IEnemyMotion / IEnemyAction) を組み合わせる EnemyBehaviour。
/// 同じ GameObject に付いた部品を収集し、行動可能な間だけ合成して動かす:
/// - フェーズ: 索敵中/発見後 (EnemyPerception、無ければ ChaseRange の距離判定) で部品を絞り込む
/// - 移動 (IEnemyMotion): フェーズが合い IsActive な最初の1つだけ (Inspector の並び順が優先度。上が優先)
/// - 行動 (IEnemyAction): フェーズが合い IsActive なすべてが並行
/// 例: [ChaseMotion(発見後), PatrolMotion(索敵中), ShootAction(発見後)]
///     → 索敵中はゆっくり巡回、発見したら追跡しながら射撃。
/// </summary>
public class CompositeEnemyBehaviour : EnemyBehaviour
{
    private IEnemyMotion[] _motions;
    private IEnemyAction[] _actions;

    protected override void Awake()
    {
        base.Awake();
        _motions = GetComponents<IEnemyMotion>();
        _actions = GetComponents<IEnemyAction>();
    }

    public override void PhysicsTick()
    {
        var alert = Enemy.IsPlayerDetected;

        SelectMotion(alert)?.PhysicsTick();

        foreach (var action in _actions)
        {
            if (MatchesPhase(action, alert) && action.IsActive)
                action.PhysicsTick();
        }
    }

    public override void LogicTick()
    {
        var alert = Enemy.IsPlayerDetected;

        SelectMotion(alert)?.LogicTick();

        foreach (var action in _actions)
        {
            if (MatchesPhase(action, alert) && action.IsActive)
                action.LogicTick();
        }
    }

    public override void OnInterrupted()
    {
        foreach (var motion in _motions)
            motion.OnInterrupted();
        foreach (var action in _actions)
            action.OnInterrupted();
    }

    /// <summary>現在フェーズに合い IsActive な最初の移動部品を返す (無ければ null = その場に立つ)。</summary>
    private IEnemyMotion SelectMotion(bool alert)
    {
        foreach (var motion in _motions)
        {
            if (MatchesPhase(motion, alert) && motion.IsActive)
                return motion;
        }

        return null;
    }

    private static bool MatchesPhase(IEnemyModule module, bool alert)
    {
        return module.Phase switch
        {
            EnemyPhaseFilter.SearchOnly => !alert,
            EnemyPhaseFilter.AlertOnly => alert,
            _ => true,
        };
    }
}
