/// <summary>
/// ダッシュ状態。向いている方向へ一定速度で飛び出す。重力無効・向き固定・入力無視。
/// 終了時、進行方向の入力が続いていれば走り状態へ移行する(Player 側で処理)。
/// </summary>
public class DashState : PlayerState
{
    private float _timer;

    public DashState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartDash();
        _timer = Player.Consts.DashDuration;
    }

    public override void Exit()
    {
        // 被弾などの割り込みでも確実にダッシュ状態を解除できるよう Exit で行う
        Player.EndDash();
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;
        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyDashMovement();
    }
}
