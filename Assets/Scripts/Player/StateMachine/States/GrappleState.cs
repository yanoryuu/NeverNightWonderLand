using UnityEngine;

/// <summary>
/// 青ハサミの糸移動 (グラップル) 状態。向いている方向 (上入力で斜め45°) へハサミを飛ばし、
/// 壁 (Ground レイヤー) に当たればその地点へ高速移動する。
/// 到達すると張り付き (コヨーテタイム回復) となり、そこからジャンプで乗り越えられる。
/// 外れた場合は短い硬直だけで戻る。
/// </summary>
public class GrappleState : PlayerState
{
    private const float ArriveDistance = 0.45f;
    private const float MaxDuration = 1f;   // 安全弁 (引っかかり防止)
    private const float WhiffDuration = 0.15f;

    private Vector2 _target;
    private bool _hasTarget;
    private float _timer;

    public GrappleState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartDash(); // 向き固定・ダッシュ扱いの見た目

        var consts = Player.Consts;
        var direction = Player.VerticalInput > 0.5f
            ? new Vector2(Player.Facing, 1f).normalized // 斜め上45°
            : new Vector2(Player.Facing, 0f);

        var hit = Physics2D.Raycast(
            Player.transform.position, direction, consts.GrappleRange, consts.GroundLayer);

        if (hit.collider != null)
        {
            _hasTarget = true;
            // めり込まないよう少し手前を目標にする
            _target = hit.point - direction * 0.3f;
            _timer = MaxDuration;
        }
        else
        {
            _hasTarget = false;
            _timer = WhiffDuration;
        }
    }

    public override void Exit()
    {
        Player.EndDash();
    }

    public override void LogicUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            EndGrapple();
        }
    }

    public override void PhysicsUpdate()
    {
        if (!_hasTarget)
        {
            // 外れ: その場で硬直するだけ
            Player.StopHorizontalMovement();
            Player.ApplyGravity();
            return;
        }

        var toTarget = _target - (Vector2)Player.transform.position;
        if (toTarget.magnitude <= ArriveDistance)
        {
            Arrive();
            return;
        }

        Player.Rb.linearVelocity = toTarget.normalized * Player.Consts.GrappleSpeed;
    }

    private void Arrive()
    {
        // 張り付き: 速度を殺し、コヨーテ回復でそのままジャンプできるようにする
        Player.Rb.linearVelocity = Vector2.zero;
        Player.RefreshCoyote();
        EndGrapple();
    }

    private void EndGrapple()
    {
        if (StateMachine.CurrentState == this)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }
}
