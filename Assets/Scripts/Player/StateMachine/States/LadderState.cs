/// <summary>
/// 梯子に掴まっている状態。上下入力で上り下り (重力なし・横入力では動かない)。
/// ジャンプ: 方向なし = その場で小さく跳んで離れる / 横入力あり = その向きへ通常のジャンプ / 下入力あり = 手を離して落ちる。
/// 上端まで登ると上端のすり抜け床に立つ。下りて地面に着くと降りる。梯子の下端より下へ出たら落下。
/// 攻撃・ダッシュ・アイテム・回復は受け付けない (梯子の上では両手がふさがっている)。被弾すると HurtState が割り込んで落ちる。
/// </summary>
public class LadderState : PlayerState
{
    private ClimbableLadder _ladder;
    private bool _fromTop;

    public LadderState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    /// <summary>掴まる梯子。遷移前に渡す。fromTop = 上端から掴まって降り始める。</summary>
    public void SetLadder(ClimbableLadder ladder, bool fromTop)
    {
        _ladder = ladder;
        _fromTop = fromTop;
    }

    public override void Enter()
    {
        Player.BeginLadder(_ladder, _fromTop);
    }

    public override void Exit()
    {
        Player.EndLadder();
    }

    public override void LogicUpdate()
    {
        if (_ladder == null || !_ladder.isActiveAndEnabled)
        {
            StateMachine.ChangeState(Player.FallState);
            return;
        }

        var vertical = Player.VerticalInput;

        // ジャンプで離れる
        if (Player.TryConsumeLadderJump())
        {
            if (vertical < -0.5f)
            {
                // 下 + ジャンプ = 手を離して落ちる
                StateMachine.ChangeState(Player.FallState);
            }
            else if (UnityEngine.Mathf.Abs(Player.MoveInput) > 0.01f)
            {
                // 横入力あり = その向きへ通常のジャンプ
                StateMachine.ChangeState(Player.JumpState);
            }
            else
            {
                // 方向なし = 小さく跳んで離れる
                Player.LadderHop();
                StateMachine.ChangeState(Player.FallState);
            }

            return;
        }

        var feet = Player.FeetY;

        // 上端まで登った: 上端のすり抜け床に立つ
        if (vertical > 0.1f && feet >= _ladder.Top - 0.02f)
        {
            Player.StandOnLadderTop(_ladder);
            StateMachine.ChangeState(Player.IdleState);
            return;
        }

        // 下りて地面に着いた
        if (vertical < -0.1f && Player.IsGrounded)
        {
            StateMachine.ChangeState(Player.GetGroundedState());
            return;
        }

        // 梯子の下端より下 (宙に浮いた梯子の下から出た)
        if (feet < _ladder.Bottom - 0.2f)
            StateMachine.ChangeState(Player.FallState);
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyLadderClimb(_ladder);
    }
}
