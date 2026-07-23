/// <summary>
/// 移動の部品 (IEnemyMotion) の基底。継承して PhysicsTick に移動を実装する。
/// 複数付けても IsActive な最初の1つだけが動く (Inspector の並び順が優先度)。
/// </summary>
public abstract class EnemyMotionBase : EnemyModuleBase, IEnemyMotion { }
