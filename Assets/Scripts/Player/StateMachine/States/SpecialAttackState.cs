using UnityEngine;

/// <summary>
/// 特殊攻撃状態 (△ボタン)。装備中の <see cref="SpecialAttackDefinition"/> に従い、
/// UseDelay 経過時に一度だけ Activate を呼ぶ。効果の中身 (弾の生成・パリィなど) は
/// 攻撃定義側が持つため、このステートは全特殊攻撃共通。
/// クールダウンは Enter 時に開始する。
/// </summary>
public class SpecialAttackState : PlayerState
{
    private SpecialAttackDefinition _attack;
    private float _timer;
    private bool _activated;

    public SpecialAttackState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _attack = Player.AttackLoadout != null ? Player.AttackLoadout.CurrentSpecial : null;
        _timer = _attack != null ? _attack.UseDuration : 0f;
        _activated = false;

        if (_attack != null)
            Player.BeginSpecialCooldown(_attack.Cooldown);
    }

    public override void Exit()
    {
        Player.EndAttack();
    }

    public override void LogicUpdate()
    {
        if (_attack == null)
        {
            StateMachine.ChangeState(Player.GetLocomotionState());
            return;
        }

        _timer -= Time.deltaTime;

        var elapsed = _attack.UseDuration - _timer;
        if (!_activated && elapsed >= _attack.UseDelay)
        {
            _attack.Activate(Player, Player.ComputeThrowOrigin(), Player.Facing);
            _activated = true;
        }

        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        // 使いながら移動できる
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
