using UnityEngine;

/// <summary>
/// 投擲/設置系アイテムの使用状態。アイテム消費と種類の決定は遷移時に済んでいる
/// (PlayerController.PendingItem)。UseDelay 経過時に一度だけ ItemDefinition.Activate を呼ぶ。
/// 効果の中身 (弾の生成・設置など) はアイテム定義側が持つため、このステートは全アイテム共通。
/// </summary>
public class ItemThrowState : PlayerState
{
    private ItemDefinition _item;
    private float _timer;
    private bool _activated;

    public ItemThrowState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _item = Player.PendingItem;
        _timer = _item != null ? _item.UseDuration : 0f;
        _activated = false;
    }

    public override void Exit()
    {
        Player.EndAttack();
    }

    public override void LogicUpdate()
    {
        if (_item == null)
        {
            StateMachine.ChangeState(Player.GetLocomotionState());
            return;
        }

        _timer -= Time.deltaTime;

        var elapsed = _item.UseDuration - _timer;
        if (!_activated && elapsed >= _item.UseDelay)
        {
            _item.Activate(Player, GetSpawnOrigin(), Player.Facing);
            _activated = true;
        }

        if (_timer <= 0f)
            StateMachine.ChangeState(Player.GetLocomotionState());
    }

    public override void PhysicsUpdate()
    {
        // 使いながら移動できる
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }

    /// <summary>
    /// 発射位置を計算する。壁際で使った時に弾が壁の中に生成されないよう、
    /// 目の前に壁があれば手前へ寄せる。
    /// </summary>
    private Vector2 GetSpawnOrigin()
    {
        var facing = Player.Facing;
        var origin = (Vector2)Player.transform.position + new Vector2(0.5f * facing, 0.3f);

        var rayStart = (Vector2)Player.transform.position + new Vector2(0f, 0.3f);
        var wall = Physics2D.Raycast(rayStart, new Vector2(facing, 0f), 0.9f, Player.Consts.GroundLayer);
        if (wall.collider != null)
            origin.x = wall.point.x - facing * 0.4f;

        return origin;
    }
}
