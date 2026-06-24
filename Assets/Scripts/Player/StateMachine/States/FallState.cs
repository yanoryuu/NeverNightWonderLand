/// <summary>
/// 落下中状態。接地で地上ステートへ戻る(基底 AirborneState が処理)。
/// 専用の追加ロジックは不要だが、Animator 上では JumptoFall / Fall を YVelocity で出し分ける。
/// </summary>
public class FallState : AirborneState
{
    public FallState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }
}
