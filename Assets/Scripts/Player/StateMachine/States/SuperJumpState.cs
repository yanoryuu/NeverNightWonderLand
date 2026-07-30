using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 大ジャンプ (スキル) の上昇。SuperJumpVelocity で垂直に打ち上がり、天井 (砕けない障害物) に
/// 当たるまで昇り続ける (最高到達高さ SuperJumpHeight = 通常ジャンプの7倍)。
/// 途中の SkillBreakable(大ジャンプ) は砕き、敵にはダメージを与えながら貫通する
/// (上昇中は接触ダメージを受けない)。
/// ボタンは発動時点で離されているため、通常の可変ジャンプ減速 (LowJumpMultiplier) は
/// 適用せず基準重力のみで上昇する。頂点に達したら Fall へ。
/// </summary>
public class SuperJumpState : PlayerState
{
    private readonly HashSet<Collider2D> _damaged = new();

    public SuperJumpState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        _damaged.Clear();
        Player.ClearJumpBuffers();
        Player.SetSkillInvulnerable(true);
        Player.Rb.linearVelocity = new Vector2(Player.Rb.linearVelocity.x, Player.Consts.SuperJumpVelocity);
    }

    public override void Exit()
    {
        Player.SetSkillInvulnerable(false);
    }

    public override void LogicUpdate()
    {
        // 天井に当たる (衝突で上昇が止まる) か頂点に達したら落下へ
        if (Player.Rb.linearVelocity.y <= 0f)
            StateMachine.ChangeState(Player.FallState);
    }

    public override void PhysicsUpdate()
    {
        Player.ApplyHorizontalMovement();

        // 基準重力を自前で適用 (ApplyGravity はボタン解放中の上昇に LowJumpMultiplier を掛けてしまう)
        var velocity = Player.Rb.linearVelocity;
        velocity.y -= Player.Consts.Gravity * Time.fixedDeltaTime;
        Player.Rb.linearVelocity = velocity;

        HitAbove();
    }

    /// <summary>頭上の SkillBreakable(大ジャンプ) を砕き、敵に1回ずつダメージを与える。</summary>
    private void HitAbove()
    {
        // 上昇が速いのでフレーム間の移動量ぶん縦に長めの判定にする
        var stepHeight = Mathf.Max(0.9f, Player.Rb.linearVelocity.y * Time.fixedDeltaTime + 0.6f);
        var center = (Vector2)Player.transform.position + new Vector2(0f, 0.9f + stepHeight / 2f);
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(0.9f, stepHeight), 0f,
            Player.Consts.AttackTargetLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<SkillBreakable>(out var breakable))
            {
                breakable.TryBreak(PlayerSkill.SuperJump);
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
