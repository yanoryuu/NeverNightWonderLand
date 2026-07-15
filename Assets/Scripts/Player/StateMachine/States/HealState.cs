/// <summary>
/// 回復状態。Enter で回復ゲージのメモリを1消費して HP を回復する。
/// HealDuration の間は移動不可(隙を作るデザイン)。被弾すれば HurtState が割り込む。
/// </summary>
public class HealState : PlayerState
{
    private float _timer;

    public HealState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        _timer = Player.Consts.HealDuration;

        // 遷移条件 (CanHeal) を通ってきているはずだが、念のため失敗時は即復帰
        if (!Player.TryApplyHeal())
            _timer = 0f;
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;
        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }
}
