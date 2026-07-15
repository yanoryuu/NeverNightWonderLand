using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 突進系アイテム (布カッターなど) の使用状態。前方へ突進しながら触れた敵に
/// ダメージ+ノックバックを与える。速度・時間・ダメージは使用中アイテムの
/// <see cref="DashItemDefinition"/> から取る。
/// 同じ対象への多段ヒットはしない。被弾すると HurtState が割り込むため
/// 「被弾でキャンセル」は自然に成立する。アイテム消費は遷移時に済んでいる。
/// </summary>
public class ItemDashState : PlayerState
{
    private readonly HashSet<Collider2D> _alreadyHit = new();
    private DashItemDefinition _item;
    private float _timer;

    public ItemDashState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        // 突進系アイテムは「被弾でキャンセル」仕様なので、回避ダッシュ扱いにはしない
        Player.StartDash(contactInvulnerable: false);

        _item = Player.PendingItem as DashItemDefinition;
        _timer = _item != null ? _item.DashDuration : 0f;
        _alreadyHit.Clear();
    }

    public override void Exit()
    {
        Player.EndDash();
    }

    public override void LogicUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        if (_item == null)
            return;

        // 向いている方向へ突進。重力は無効
        Player.Rb.linearVelocity = new Vector2(Player.Facing * _item.DashSpeed, 0f);
        Player.PerformItemDashHit(_alreadyHit, _item.HpDamage, _item.GuardDamage, _item.Knockback);
    }
}
