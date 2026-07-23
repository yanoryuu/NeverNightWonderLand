/// <summary>
/// 空中ステートの共通基底。Jump / Fall が継承する。
/// 共通遷移: 攻撃・ダッシュ → アクション、コヨーテ中のジャンプ → Jump、接地したら → 地上ステート。
/// </summary>
public abstract class AirborneState : PlayerState
{
    protected AirborneState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        if (TryActionTransitions())
            return;

        // コヨーテタイム中なら空中でもジャンプを許可する
        if (TryJumpTransition())
            return;

        // 二段ジャンプ (赤ハサミ取得後)
        if (Player.TryConsumeDoubleJump())
        {
            StateMachine.ChangeState(Player.JumpState);
            return;
        }

        // 崖際に体が届いたら登る (ジャンプが縁にわずかに届かない時の救済。壁張り付きより優先)
        if (Player.TryGetLedgeTarget(out var ledgeTarget))
        {
            Player.LedgeClimbState.SetTarget(ledgeTarget);
            StateMachine.ChangeState(Player.LedgeClimbState);
            return;
        }

        // 壁張り付き (黄ハサミ取得後): 壁方向へ入力しながら壁に触れている
        if (Player.CanWallCling())
        {
            StateMachine.ChangeState(Player.WallClingState);
            return;
        }

        if (Player.IsGrounded)
            StateMachine.ChangeState(Player.GetGroundedState());
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
