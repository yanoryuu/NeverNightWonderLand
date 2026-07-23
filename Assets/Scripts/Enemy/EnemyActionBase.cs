/// <summary>
/// 行動の部品 (IEnemyAction) の基底。継承して射撃・ジャンプなどを実装する。
/// IsActive なすべてが移動と並行して毎 Tick 動く。
/// </summary>
public abstract class EnemyActionBase : EnemyModuleBase, IEnemyAction { }
