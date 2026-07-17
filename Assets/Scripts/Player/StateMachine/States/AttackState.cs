/// <summary>
/// 近接攻撃状態 (□ボタン)。装備中の近接攻撃 (MeleeAttackDefinition) のプロファイルで攻撃する。
/// 攻撃モーション中も水平移動・重力は適用される(移動しながら攻撃可能)。
/// 向きは固定し、攻撃判定は HitDelay 経過時に一度だけ出す。
/// モーション終了後に現在の状況へ応じた locomotion ステートへ戻る。
/// </summary>
public class AttackState : PlayerState
{
    private PlayerConsts.AttackProfile _profile;
    private float _timer;
    private bool _hitDone;

    public AttackState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _profile = Player.CurrentMeleeProfile;
        _timer = _profile.Duration;
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
        var elapsed = _profile.Duration - _timer;
        if (!_hitDone && elapsed >= _profile.HitDelay)
        {
            Player.PerformAttackHit(_profile);
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
