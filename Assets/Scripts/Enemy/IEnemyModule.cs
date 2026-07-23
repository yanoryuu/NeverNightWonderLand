/// <summary>
/// うごき部品が動くフェーズの指定。フェーズは索敵状態
/// (EnemyPerception、無ければ ChaseRange の距離判定) で決まる。
/// </summary>
public enum EnemyPhaseFilter
{
    /// <summary>索敵中・発見後の両方で動く。</summary>
    Always,

    /// <summary>索敵中 (プレイヤー未発見) のみ動く。</summary>
    SearchOnly,

    /// <summary>発見後 (プレイヤー発見中) のみ動く。</summary>
    AlertOnly,
}

/// <summary>
/// 敵のうごき部品の共通インターフェース。CompositeEnemyBehaviour が収集して合成する。
/// 実装は MonoBehaviour として敵の GameObject に付ける
/// (便利な基底は EnemyMotionBase / EnemyActionBase)。
/// </summary>
public interface IEnemyModule
{
    /// <summary>どのフェーズ (索敵中/発見後) で動くか。</summary>
    EnemyPhaseFilter Phase { get; }

    /// <summary>この部品を今実行するか (フェーズ以外の発動条件)。</summary>
    bool IsActive { get; }

    /// <summary>行動可能な間、毎物理ステップ呼ばれる。</summary>
    void PhysicsTick();

    /// <summary>行動可能な間、毎フレーム呼ばれる。</summary>
    void LogicTick();

    /// <summary>被弾硬直・拘束・ブレイクなどで行動が中断された時に呼ばれる。</summary>
    void OnInterrupted();
}

/// <summary>
/// 移動の部品。複数付けても IsActive な最初の1つだけが動く
/// (優先度 = コンポーネントの並び順。Inspector で上にあるものが優先)。
/// </summary>
public interface IEnemyMotion : IEnemyModule { }

/// <summary>
/// 行動の部品 (射撃・ジャンプなど)。IsActive なすべてが移動と並行して毎 Tick 動く。
/// </summary>
public interface IEnemyAction : IEnemyModule { }
