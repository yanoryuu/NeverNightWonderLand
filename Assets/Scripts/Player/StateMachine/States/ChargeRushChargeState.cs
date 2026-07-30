using UnityEngine;

/// <summary>
/// 横突進 (スキル) の溜め。地上で下+ダッシュ長押しで入り、その場で力を溜める。
/// 溜め完了後にボタンを離すと ChargeRushState、完了前に離すと通常ダッシュ。
/// 溜め中はスプライトをスキル色 (紫) に近づけ、完了で白フラッシュして知らせる。
/// </summary>
public class ChargeRushChargeState : PlayerState
{
    private static readonly Color ChargeColor = new Color(0.65f, 0.4f, 0.85f);

    private float _timer;
    private Color _baseColor;

    public ChargeRushChargeState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    private bool IsCharged => _timer >= Player.Consts.SkillChargeTime;

    public override void Enter()
    {
        _timer = 0f;
        Player.StopHorizontalMovement();
        if (Player.Sprite != null)
            _baseColor = Player.Sprite.color;
    }

    public override void Exit()
    {
        if (Player.Sprite != null)
            Player.Sprite.color = _baseColor;
    }

    public override void LogicUpdate()
    {
        _timer += Time.deltaTime;
        UpdateChargeVisual();

        // ボタンを離したら発動 (溜め不足なら通常ダッシュ)
        if (!Player.DashHeld)
        {
            StateMachine.ChangeState(IsCharged ? Player.ChargeRushState : (PlayerState)Player.DashState);
            return;
        }

        // 足場が消えた等で空中になったら中断
        if (!Player.IsGrounded)
            StateMachine.ChangeState(Player.FallState);
    }

    public override void PhysicsUpdate()
    {
        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }

    private void UpdateChargeVisual()
    {
        if (Player.Sprite == null)
            return;

        Player.Sprite.color = IsCharged
            ? Color.Lerp(ChargeColor, Color.white, Mathf.PingPong(Time.time * 8f, 1f))
            : Color.Lerp(_baseColor, ChargeColor, _timer / Player.Consts.SkillChargeTime);
    }
}
