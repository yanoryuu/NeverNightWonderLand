/// <summary>
/// 上昇中(ジャンプ)状態。Enter でジャンプ初速を与え、上昇が止まったら Fall へ。
/// </summary>
public class JumpState : AirborneState
{
    public JumpState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.Jump();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (StateMachine.CurrentState != this)
            return;

        // 上昇が止まったら落下へ
        if (Player.Rb.linearVelocity.y <= 0f)
            StateMachine.ChangeState(Player.FallState);
    }
}
