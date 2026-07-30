using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 横突進 (スキル)。向いている方向へ、砕けない障害物に当たるまで突き進み続ける。
/// 途中の SkillBreakable(横突進) は砕き、敵にはダメージを与えながら貫通する
/// (突進中は接触ダメージを受けない)。
/// </summary>
public class ChargeRushState : PlayerState
{
    private readonly HashSet<Collider2D> _damaged = new();

    public ChargeRushState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        _damaged.Clear();
        Player.StartDash(contactInvulnerable: false);
        Player.SetSkillInvulnerable(true);
    }

    public override void Exit()
    {
        // 被弾などの割り込みでも確実に解除する
        Player.EndDash();
        Player.SetSkillInvulnerable(false);
    }

    public override void LogicUpdate()
    {
        // 砕けない壁に当たったら終了 (SkillBreakable(横突進) は HitAhead が先に砕くので止まらない)
        if (Player.IsTouchingWall(Player.Facing))
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        Player.Rb.linearVelocity = new Vector2(Player.Facing * Player.Consts.ChargeRushSpeed, 0f);
        HitAhead();
    }

    /// <summary>正面の SkillBreakable(横突進) を砕き、敵に1回ずつダメージを与える。</summary>
    private void HitAhead()
    {
        var center = (Vector2)Player.transform.position + new Vector2(Player.Facing * 0.9f, 0f);
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(1.2f, 1.5f), 0f,
            Player.Consts.AttackTargetLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<SkillBreakable>(out var breakable))
            {
                breakable.TryBreak(PlayerSkill.ChargeRush);
                continue;
            }

            if (_damaged.Contains(hit))
                continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                _damaged.Add(hit);
                var profile = Player.CurrentMeleeProfile;
                damageable.TakeDamage(new DamageInfo(
                    profile.HpDamage, profile.GuardDamage, hit.transform.position, Player.gameObject));
            }
        }
    }
}
