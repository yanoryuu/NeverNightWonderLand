using UnityEngine;

/// <summary>
/// パリィ (スキル)。方向入力なしでダッシュボタンを押すと発動する。
/// 受付時間 (ParryWindow) 内に受けた攻撃を無効化し、成功すると無敵時間を得て即座に行動できる。
/// 受付を過ぎると硬直 (ParryRecovery) が残り、その間は無防備。
/// 受付/硬直/成功時無敵の長さは PlayerConsts で調整する。
/// </summary>
public class ParryState : PlayerState
{
    private static readonly Color WindowColor = new Color(0.5f, 0.9f, 1f);   // 受付中: 水色
    private static readonly Color RecoveryColor = new Color(0.6f, 0.6f, 0.6f); // 硬直中: 灰色

    private float _timer;
    private Color _baseColor;
    private bool _succeeded;

    public ParryState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    private bool InWindow => _timer <= Player.Consts.ParryWindow;

    public override void Enter()
    {
        _timer = 0f;
        _succeeded = false;
        Player.StopHorizontalMovement();
        Player.BeginDashCooldown(); // ダッシュと同じクールダウンで連打を防ぐ
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

        if (Player.Sprite != null)
            Player.Sprite.color = _succeeded ? Color.white : (InWindow ? WindowColor : RecoveryColor);

        // 成功したら硬直を残さず即座に行動可能へ
        if (_succeeded || _timer >= Player.Consts.ParryWindow + Player.Consts.ParryRecovery)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }

    /// <summary>
    /// 攻撃の無効化を試みる (PlayerHealth.TakeDamage から呼ばれる)。
    /// 受付中なら成功: ダメージを無効化し、無敵時間を得る。
    /// </summary>
    public bool TryAbsorb(in DamageInfo info)
    {
        if (!InWindow)
            return false;

        _succeeded = true;
        Player.Health?.GrantInvincibility(Player.Consts.ParrySuccessInvincible);
        Notifier.Notify("パリィ!");
        return true;
    }
}
