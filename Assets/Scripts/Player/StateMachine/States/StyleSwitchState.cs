/// <summary>
/// スタイル切り替え状態。切り替え自体が攻撃判定を持つ(切り替え攻撃)。
/// 二刀流へ(分割): 一閃 — 横に広く発生が速い・HP寄り。
/// 両手持ちへ(合体): 振り下ろし — 範囲は狭いが防御値寄り。
/// 構造は AttackState と同じ(移動可・HitDelay で一度だけ判定)。
/// </summary>
public class StyleSwitchState : PlayerState
{
    private PlayerConsts.AttackProfile _profile;
    private float _timer;
    private bool _hitDone;

    public StyleSwitchState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        Player.ToggleStyle();

        // 切り替え後のスタイルに応じた切り替え攻撃を選ぶ
        _profile = Player.Style == ScissorStyle.DualBlades
            ? Player.Consts.SplitSwitchAttack
            : Player.Consts.MergeSwitchAttack;
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
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
