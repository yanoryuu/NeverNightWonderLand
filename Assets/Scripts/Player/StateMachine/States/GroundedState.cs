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
        // パリィ (スキル): 方向入力なしでダッシュ (通常ダッシュより優先)
        if (Player.HasSkill(PlayerSkill.Parry)
            && Mathf.Abs(Player.MoveInput) < 0.01f && Mathf.Abs(Player.VerticalInput) < 0.01f
            && Player.TryConsumeDash())
        {
            StateMachine.ChangeState(Player.ParryState);
            return;
        }

        // 横突進 (スキル): 下入力 + ダッシュ長押しで溜めに入る (通常ダッシュより優先)
        if (Player.HasSkill(PlayerSkill.ChargeRush)
            && Player.VerticalInput < -0.5f && Player.TryConsumeDash())
        {
            StateMachine.ChangeState(Player.ChargeRushChargeState);
            return;
        }

        if (TryActionTransitions())
            return;

        // 下入力 + ジャンプ = すり抜け床から降りる (通常のジャンプより優先)
        if (Player.VerticalInput < -0.5f && Player.HasBufferedJump()
            && Player.TryDropThroughPlatform())
        {
            StateMachine.ChangeState(Player.FallState);
            return;
        }

        // 大ジャンプ (スキル): 上入力 + ジャンプで溜めに入る (通常ジャンプより優先)
        if (Player.HasSkill(PlayerSkill.SuperJump)
            && Player.VerticalInput > 0.5f && Player.HasBufferedJump())
        {
            StateMachine.ChangeState(Player.SuperJumpChargeState);
            return;
        }

        if (TryJumpTransition())
            return;

        // 崖登りは空中 (ジャンプ) からのみ発生する (歩きで段差に触れても登らない)

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
