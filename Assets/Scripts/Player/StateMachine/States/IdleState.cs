using UnityEngine;

/// <summary>
/// 接地・静止状態。水平入力が入ったら Move へ。
/// </summary>
public class IdleState : GroundedState
{
    public IdleState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 基底で別ステートへ遷移済みなら何もしない
        if (StateMachine.CurrentState != this)
            return;

        if (Mathf.Abs(Player.MoveInput) > 0.01f)
            StateMachine.ChangeState(Player.MoveState);
    }
}
