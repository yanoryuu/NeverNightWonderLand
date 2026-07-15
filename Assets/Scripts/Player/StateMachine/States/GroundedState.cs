using UnityEngine;

/// <summary>
/// 接地中ステートの共通基底。Idle / Move が継承する。
/// 共通遷移: 攻撃・ダッシュ → アクション、ジャンプ入力 → Jump、接地が外れたら → Fall。
/// </summary>
public abstract class GroundedState : PlayerState
{
    protected GroundedState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        if (TryActionTransitions())
            return;

        if (TryJumpTransition())
            return;

        // 回復は接地中のみ(空中で無防備に回復できると不自然なため)
        if (Player.CanHeal() && Player.TryConsumeHeal())
        {
            StateMachine.ChangeState(Player.HealState);
            return;
        }

        // 地面を離れたら落下へ(歩いて崖から出た場合など)
        if (!Player.IsGrounded)
            StateMachine.ChangeState(Player.FallState);
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
