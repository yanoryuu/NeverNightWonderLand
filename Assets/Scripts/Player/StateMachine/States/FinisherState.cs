using UnityEngine;

/// <summary>
/// フィニッシャー「裁断」状態。発動範囲内・同じくらいの高さ・向いている方向にいる
/// 一番近いブレイク中の敵が標的になる (可否判定は PlayerState.TryActionTransitions 側)。
/// 標的が離れている場合は踏み込んでから合体ハサミの一撃を出すため、確実に当たる。
/// </summary>
public class FinisherState : PlayerState
{
    // 踏み込みをやめて振り下ろす距離 (units)
    private const float LungeStopDistance = 0.7f;

    private EnemyController _target;
    private float _timer;
    private bool _hitDone;

    public FinisherState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        Player.TryGetFinisherTarget(out _target);
        if (_target != null)
            Player.FaceTo(_target.transform.position.x);

        _timer = Player.Consts.FinisherProfile.Duration;
        _hitDone = false;
    }

    public override void Exit()
    {
        Player.EndAttack();
        _target = null;
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;

        var profile = Player.Consts.FinisherProfile;
        var elapsed = profile.Duration - _timer;
        if (!_hitDone && elapsed >= profile.HitDelay)
        {
            Player.PerformAttackHit(profile, isFinisher: true);
            _hitDone = true;
        }

        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        // 振り下ろす前に標的まで踏み込む (標的が消えたらその場で振る)
        if (!_hitDone && _target != null)
        {
            var dx = _target.transform.position.x - Player.transform.position.x;
            if (Mathf.Abs(dx) > LungeStopDistance)
            {
                Player.Rb.linearVelocity = new Vector2(
                    Mathf.Sign(dx) * Player.Consts.DashSpeed, Player.Rb.linearVelocity.y);
                Player.ApplyGravity();
                return;
            }
        }

        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }
}
