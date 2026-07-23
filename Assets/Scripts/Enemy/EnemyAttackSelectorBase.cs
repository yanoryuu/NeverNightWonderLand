using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数の攻撃から1つを選んで出す行動部品の基底 (パターン2: 排他選択・共有クールダウン)。
/// 攻撃リスト (<see cref="EnemyAttack"/>) を Inspector で定義し、
/// 「どれを出すか」の条件だけを <see cref="SelectAttack"/> のオーバーライドで記述する。
///
/// 流れ: 全体クールダウン明け → クールダウンが明けている攻撃を候補に SelectAttack →
///       Animator トリガー発火 + OnAttackStarted → Duration の間は攻撃中 → OnAttackFinished。
/// 被弾などで中断されると OnAttackInterrupted が呼ばれ、攻撃中状態は解除される。
/// 弾の生成・当たり判定は OnAttackStarted (または Animation Event) 側で行う。
/// </summary>
public abstract class EnemyAttackSelectorBase : EnemyActionBase
{
    [Tooltip("攻撃のリスト。選択条件は SelectAttack のオーバーライドで記述する")]
    [SerializeField] private EnemyAttack[] _attacks;

    [Tooltip("攻撃全体の共有クールダウン (sec)。攻撃終了からこの時間は次の攻撃を選択しない")]
    [SerializeField] private float _globalCooldown = 1.5f;

    private readonly List<EnemyAttack> _readyBuffer = new();
    private float _globalTimer;
    private float _attackTimer;

    /// <summary>定義されている全攻撃。</summary>
    protected IReadOnlyList<EnemyAttack> Attacks => _attacks;

    /// <summary>実行中の攻撃。攻撃していない間は null。</summary>
    protected EnemyAttack CurrentAttack { get; private set; }

    /// <summary>攻撃モーションの実行中か。</summary>
    public bool IsAttacking => CurrentAttack != null;

    /// <summary>プレイヤーとの距離 (選択条件用)。プレイヤー不在は float.MaxValue。</summary>
    protected float DistanceToPlayer =>
        Player != null ? Vector2.Distance(transform.position, Player.position) : float.MaxValue;

    public override void PhysicsTick() { }

    public override void LogicTick()
    {
        var dt = Time.deltaTime;
        foreach (var attack in _attacks)
            attack.TickCooldown(dt);

        // 攻撃中: Duration の経過を待つ
        if (CurrentAttack != null)
        {
            _attackTimer -= dt;
            if (_attackTimer > 0f)
                return;

            var finished = CurrentAttack;
            CurrentAttack = null;
            _globalTimer = _globalCooldown;
            OnAttackFinished(finished);
            return;
        }

        // 全体クールダウン中
        _globalTimer -= dt;
        if (_globalTimer > 0f)
            return;

        // クールダウンが明けている攻撃を候補にして、条件 (SelectAttack) で1つ選ぶ
        _readyBuffer.Clear();
        foreach (var attack in _attacks)
        {
            if (attack.IsReady)
                _readyBuffer.Add(attack);
        }

        if (_readyBuffer.Count == 0)
            return;

        var selected = SelectAttack(_readyBuffer);
        if (selected == null)
            return;

        Perform(selected);
    }

    public override void OnInterrupted()
    {
        // 被弾・拘束・ブレイクで攻撃を中断する (クールダウンは消費済みのまま)
        if (CurrentAttack == null)
            return;

        var interrupted = CurrentAttack;
        CurrentAttack = null;
        _globalTimer = _globalCooldown;
        OnAttackInterrupted(interrupted);
    }

    private void Perform(EnemyAttack attack)
    {
        CurrentAttack = attack;
        _attackTimer = attack.Duration;
        attack.BeginCooldown();

        Animation?.SetTrigger(attack.AnimationTrigger);
        OnAttackStarted(attack);
    }

    /// <summary>識別名で攻撃定義を探す (SelectAttack の実装用)。無ければ null。</summary>
    protected EnemyAttack FindAttack(string attackName)
    {
        foreach (var attack in _attacks)
        {
            if (attack.Name == attackName)
                return attack;
        }

        return null;
    }

    /// <summary>
    /// どの攻撃を出すかの条件 (ここだけ記述すればよい)。
    /// readyAttacks はクールダウンが明けている攻撃のみ。null を返すと今回は攻撃しない。
    /// </summary>
    protected abstract EnemyAttack SelectAttack(IReadOnlyList<EnemyAttack> readyAttacks);

    /// <summary>攻撃の開始時に呼ばれる。弾の生成・当たり判定の予約などを行う。</summary>
    protected virtual void OnAttackStarted(EnemyAttack attack) { }

    /// <summary>攻撃が Duration まで完走した時に呼ばれる。</summary>
    protected virtual void OnAttackFinished(EnemyAttack attack) { }

    /// <summary>攻撃が被弾などで中断された時に呼ばれる (後片付け用)。</summary>
    protected virtual void OnAttackInterrupted(EnemyAttack attack) { }
}
