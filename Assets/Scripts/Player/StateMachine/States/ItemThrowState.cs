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
            // 発射位置は壁めり込み補正付き (PlayerController.ComputeThrowOrigin)
            _item.Activate(Player, Player.ComputeThrowOrigin(), Player.Facing);
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
}
