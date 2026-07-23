using UnityEngine;

/// <summary>
/// 崖際を登る状態。移動方向の壁の最上部付近に体が届いた時
/// (Grounded / Airborne から遷移) に、短時間で崖の上へ体を引き上げる。
/// 登っている間は重力も入力も受け付けない。被弾すれば HurtState が割り込む。
/// </summary>
public class LedgeClimbState : PlayerState
{
    // 引き上げにかける時間 (sec)
    private const float Duration = 0.15f;

    private Vector2 _start;
    private Vector2 _target;
    private float _timer;

    public LedgeClimbState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    /// <summary>登り先 (崖の上に立つ位置)。遷移前に TryGetLedgeTarget の結果を渡す。</summary>
    public void SetTarget(Vector2 target)
    {
        _target = target;
    }

    public override void Enter()
    {
        _start = Player.transform.position;
        _timer = 0f;
        Player.Rb.linearVelocity = Vector2.zero;
    }

    public override void PhysicsUpdate()
    {
        _timer += Time.fixedDeltaTime;
        var t = Mathf.Clamp01(_timer / Duration);

        // 先に縦へ引き上げ、後半で崖の上へ乗り込む (L字の軌道)
        var y = Mathf.Lerp(_start.y, _target.y, Mathf.Clamp01(t * 1.6f));
        var x = Mathf.Lerp(_start.x, _target.x, Mathf.Clamp01((t - 0.35f) / 0.65f));
        Player.Rb.position = new Vector2(x, y);
        Player.Rb.linearVelocity = Vector2.zero;

        if (t >= 1f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }
}
