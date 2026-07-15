/// <summary>
/// 被弾硬直状態。攻撃元と反対方向へノックバックし、HurtDuration の間は入力を受け付けない。
/// 無敵時間の管理と点滅は PlayerHealth 側が行う。
/// </summary>
public class HurtState : PlayerState
{
    private float _timer;

    public HurtState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.BeginHurt();
        Player.ApplyKnockback(Player.LastDamage.HitPoint);
        _timer = Player.Consts.HurtDuration;
    }

    public override void Exit()
    {
        Player.EndHurt();
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;
        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        // ノックバックの水平速度は維持したまま重力だけ適用する
        Player.ApplyGravity();
    }
}
