using UnityEngine;

/// <summary>
/// 接地・移動状態(歩き/走り)。水平入力が無くなったら Idle へ。
/// 走り(RunSpeed)かどうかは Player 側の走りフラグで決まる(ダッシュ後に維持される)。
/// </summary>
public class MoveState : GroundedState
{
    public MoveState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (StateMachine.CurrentState != this)
            return;

        if (Mathf.Approximately(Player.MoveInput, 0f))
            StateMachine.ChangeState(Player.IdleState);
    }
}
