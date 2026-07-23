using UnityEngine;

/// <summary>
/// 敵のうごき部品 (IEnemyModule) の便利な基底。コアへのアクセスを揃える。
/// 直接は使わず、EnemyMotionBase (移動) か EnemyActionBase (行動) を継承する。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public abstract class EnemyModuleBase : MonoBehaviour, IEnemyModule
{
    [Tooltip("どのフェーズで動くか (常時 / 索敵中のみ / 発見後のみ)")]
    [SerializeField] private EnemyPhaseFilter _phase = EnemyPhaseFilter.Always;

    /// <summary>どのフェーズ (索敵中/発見後) で動くか。</summary>
    public EnemyPhaseFilter Phase => _phase;

    /// <summary>コア機能 (定数・移動ヘルパー・状態) へのアクセス。</summary>
    protected EnemyController Enemy { get; private set; }

    /// <summary>配置時の初期位置 (巡回の基準などに使う)。</summary>
    protected Vector2 HomePosition { get; private set; }

    /// <summary>プレイヤーの Transform (シーンに1人想定)。不在なら null。</summary>
    protected Transform Player => Enemy != null ? Enemy.PlayerTransform : null;

    /// <summary>アニメーション制御 (任意)。未装着なら null なので ?. で使う。</summary>
    protected EnemyAnimator Animation => Enemy != null ? Enemy.Animation : null;

    /// <summary>この部品を今実行するか。既定は常に実行。</summary>
    public virtual bool IsActive => true;

    protected virtual void Awake()
    {
        Enemy = GetComponent<EnemyController>();
        HomePosition = transform.position;
    }

    public abstract void PhysicsTick();
    public virtual void LogicTick() { }
    public virtual void OnInterrupted() { }
}
