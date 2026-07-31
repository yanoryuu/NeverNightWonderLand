using UnityEngine;

/// <summary>
/// 地面落下攻撃 (スキル)。空中で下+攻撃で発動し、真下へ急降下する。
/// 降下中に触れた SkillBreakable(落下攻撃) を連鎖的に砕いて突き抜け、
/// 地面に着いたら足元の範囲攻撃 (SlamAttack) を出して接地ステートへ戻る。
/// </summary>
public class GroundSlamState : PlayerState
{
    public GroundSlamState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        Player.Rb.linearVelocity = Vector2.zero;
    }

    public override void Exit()
    {
        Player.EndAttack();
    }

    public override void LogicUpdate()
    {
        if (!Player.IsGrounded)
            return;

        // すり抜け床は衝撃で突き抜ける (着地扱いにしない)
        if (Player.TryDropThroughPlatform())
            return;

        // 着地衝撃: 足元中心の範囲攻撃
        Player.PerformAttackHit(Player.Consts.SlamAttack);
        StateMachine.ChangeState(Player.GetGroundedState());
    }

    public override void PhysicsUpdate()
    {
        Player.Rb.linearVelocity = new Vector2(0f, -Player.Consts.SlamFallSpeed);
        Player.TryDropThroughPlatform(); // 足元に来たすり抜け床を接触前に無効化して貫通する
        BreakBelow();
    }

    /// <summary>足元の SkillBreakable(落下攻撃) を砕く。砕けたブロックは当たりが消え、そのまま落下が続く。</summary>
    private void BreakBelow()
    {
        var center = (Vector2)Player.transform.position + Player.Consts.SlamBreakOffset;
        var hits = Physics2D.OverlapBoxAll(center, Player.Consts.SlamBreakSize, 0f,
            Player.Consts.GroundLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<SkillBreakable>(out var breakable))
                breakable.TryBreak(PlayerSkill.GroundSlam);
        }
    }
}
