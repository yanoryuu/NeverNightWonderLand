using UnityEngine;

/// <summary>
/// 敵のうごき (AI) の抽象基底。EnemyController と同じ GameObject に付ける。
/// EnemyController はコア (HP/防御値/ブレイク/被弾/死亡/接触ダメージ/撃破記録) を担い、
/// 「行動できる状態」の間だけ本クラスの Tick を呼ぶ
/// (被弾硬直・糸玉拘束・ブレイク・死亡中は呼ばれない)。
/// 新しい敵のうごきはこのクラスを継承し、<see cref="PhysicsTick"/> に移動を実装する。
/// 移動は <see cref="EnemyController.Move"/> を使うと向きの反転・壁当たり検知が揃う。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public abstract class EnemyBehaviour : MonoBehaviour
{
    /// <summary>コア機能 (定数・移動ヘルパー・状態) へのアクセス。</summary>
    protected EnemyController Enemy { get; private set; }

    /// <summary>配置時の初期位置 (巡回の基準などに使う)。</summary>
    protected Vector2 HomePosition { get; private set; }

    /// <summary>プレイヤーの Transform (シーンに1人想定)。不在なら null。</summary>
    protected Transform Player => Enemy != null ? Enemy.PlayerTransform : null;

    /// <summary>アニメーション制御 (任意)。未装着なら null なので ?. で使う。</summary>
    protected EnemyAnimator Animation => Enemy != null ? Enemy.Animation : null;

    protected virtual void Awake()
    {
        Enemy = GetComponent<EnemyController>();
        HomePosition = transform.position;
    }

    /// <summary>行動可能な間、毎物理ステップ (FixedUpdate) 呼ばれる。移動を実装する。</summary>
    public abstract void PhysicsTick();

    /// <summary>行動可能な間、毎フレーム (Update) 呼ばれる。判定やタイマーが必要な時だけオーバーライドする。</summary>
    public virtual void LogicTick() { }

    /// <summary>被弾硬直・拘束・ブレイクなどで行動が中断された時に呼ばれる。内部状態のリセットに使う。</summary>
    public virtual void OnInterrupted() { }
}
