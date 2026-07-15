/// <summary>
/// 攻撃状態。現在のスタイルに応じたプロファイル(二刀流=速い/HP寄り、両手持ち=遅い/防御値寄り)で攻撃する。
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
        _profile = Player.Style == ScissorStyle.DualBlades
            ? Player.Consts.DualAttack
            : Player.Consts.HeavyAttack;
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

            // 黄ハサミ: 通常攻撃に合わせて斬撃波を飛ばす (遠距離対応)
            if (Player.Progression != null)
                Player.Progression.TrySpawnSlashWave(Player.transform.position, Player.Facing);

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
