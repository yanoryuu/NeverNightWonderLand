/// <summary>
/// 近接攻撃状態 (□ボタン)。装備中の近接攻撃 (MeleeAttackDefinition) のプロファイルで攻撃する。
/// 地上では攻撃モーション中も水平移動・重力は適用される(移動しながら攻撃可能)。
/// 空中で攻撃を始めた場合はモーション中その場に滞空する (落下しながら振らない)。
/// 向きは固定し、攻撃判定は HitDelay 経過時に一度だけ出す。
/// モーション終了後に現在の状況へ応じた locomotion ステートへ戻る。
/// </summary>
public class AttackState : PlayerState
{
    private PlayerConsts.AttackProfile _profile;
    private float _timer;
    private bool _hitDone;
    private bool _airStall;

    public AttackState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _profile = Player.CurrentMeleeProfile;
        _timer = _profile.Duration;
        _hitDone = false;

        _airStall = !Player.IsGrounded;
        if (_airStall)
            Player.Rb.linearVelocity = UnityEngine.Vector2.zero;
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
        // 空中攻撃はその場に滞空する
        if (_airStall)
        {
            Player.Rb.linearVelocity = UnityEngine.Vector2.zero;
            return;
        }

        // 地上攻撃は移動しながら振れる
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
