/// <summary>
/// 壁張り付き状態 (黄ハサミ)。空中で壁方向へ入力しながら壁に触れると張り付き、
/// ゆっくりずり落ちる。ジャンプ入力で壁と反対方向へ壁ジャンプする
/// (壁方向へ入力し続ければ連続壁ジャンプで登れる)。
/// 壁方向への入力をやめる・壁を離れる・接地で解除される。
/// </summary>
public class WallClingState : PlayerState
{
    private int _wallDirection; // 張り付いている壁の方向 (1=右, -1=左)

    public WallClingState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        _wallDirection = Player.MoveInput > 0f ? 1 : -1;
    }

    public override void LogicUpdate()
    {
        // 張り付いている間はいつでもジャンプできるようにする (コヨーテを維持)
        Player.RefreshCoyote();

        if (TryActionTransitions())
            return;

        // 壁ジャンプ: 壁と反対方向へ飛ぶ
        if (Player.HasBufferedJump())
        {
            Player.WallJump(-_wallDirection);
            StateMachine.ChangeState(Player.FallState);
            return;
        }

        if (Player.IsGrounded)
        {
            StateMachine.ChangeState(Player.GetGroundedState());
            return;
        }

        // 壁方向への入力をやめた、または壁を離れたら落下へ
        if (!Player.IsPressingToward(_wallDirection) || !Player.IsTouchingWall(_wallDirection))
            StateMachine.ChangeState(Player.FallState);
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyWallSlide();
    }
}
