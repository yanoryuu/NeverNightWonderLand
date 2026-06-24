/// <summary>
/// 攻撃状態。攻撃モーション中も水平移動・重力は適用される(移動しながら攻撃可能)。
/// 向きは固定し、攻撃判定は AttackHitDelay 経過時に一度だけ出す。
/// モーション終了後に現在の状況へ応じた locomotion ステートへ戻る。
/// </summary>
public class AttackState : PlayerState
{
    private float _timer;
    private bool _hitDone;

    public AttackState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _timer = Player.Consts.AttackDuration;
        _hitDone = false;
    }

    public override void Exit()
    {
        Player.EndAttack();
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;

        // 攻撃開始から一定時間後に当たり判定を一度だけ出す
        var elapsed = Player.Consts.AttackDuration - _timer;
        if (!_hitDone && elapsed >= Player.Consts.AttackHitDelay)
        {
            Player.PerformAttackHit();
            _hitDone = true;
        }

        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        // 攻撃中も移動できる
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
