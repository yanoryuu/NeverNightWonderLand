/// <summary>
/// プレイヤーステートの基底クラス。
/// コンテキストである <see cref="PlayerController"/> と <see cref="PlayerStateMachine"/> を保持し、
/// 共通の遷移チェック(攻撃・ダッシュ・ジャンプ)をヘルパーとして提供する。
/// </summary>
public abstract class PlayerState
{
    #region Fields

    protected readonly PlayerController Player;
    protected readonly PlayerStateMachine StateMachine;

    #endregion

    #region Constructor

    protected PlayerState(PlayerController player, PlayerStateMachine stateMachine)
    {
        Player = player;
        StateMachine = stateMachine;
    }

    #endregion

    #region Lifecycle (override 用)

    /// <summary>ステートに入った瞬間に一度だけ呼ばれる。</summary>
    public virtual void Enter() { }

    /// <summary>ステートから出る瞬間に一度だけ呼ばれる。</summary>
    public virtual void Exit() { }

    /// <summary>Update から毎フレーム呼ばれる。入力判定・遷移を扱う。</summary>
    public virtual void LogicUpdate() { }

    /// <summary>FixedUpdate から毎回呼ばれる。物理(移動・重力)を扱う。</summary>
    public virtual void PhysicsUpdate() { }

    #endregion

    #region Shared Transition Helpers

    /// <summary>
    /// 裁断・特殊攻撃・近接攻撃・ダッシュへの遷移を試みる。どの状態からでも発生しうる共通アクション。
    /// 遷移したら true を返す。
    /// </summary>
    protected bool TryActionTransitions()
    {
        // 裁断はブレイク中の敵が範囲内にいる時のみ発動できる
        if (Player.TryConsumeFinisher() && Player.CanFinisher())
        {
            StateMachine.ChangeState(Player.FinisherState);
            return true;
        }

        // 特殊攻撃 (△/K)。装備済みでクールダウンが明けている時のみ発動できる
        if (Player.TryConsumeSpecial() && Player.CanSpecialAttack())
        {
            StateMachine.ChangeState(Player.SpecialAttackState);
            return true;
        }

        if (Player.TryConsumeAttack())
        {
            StateMachine.ChangeState(Player.AttackState);
            return true;
        }

        if (Player.TryConsumeDash())
        {
            StateMachine.ChangeState(Player.DashState);
            return true;
        }

        // アイテム使用 (L1/[I] ホールド + 下/左/右でスロットのアイテムを使う)
        if (Player.TryConsumeItemUse(out var slot) && Player.Inventory != null)
        {
            var item = Player.Inventory.GetSlotItem(slot);
            if (item == null)
            {
                Notifier.Notify($"{slot.DisplayName()}は空だ (メニューでセットできる)");
            }
            else if (Player.Inventory.TryConsume(item))
            {
                Player.SetPendingItem(item);
                StateMachine.ChangeState(item.Motion == ItemUseMotion.Dash
                    ? Player.ItemDashState
                    : (PlayerState)Player.ItemThrowState);
                return true;
            }
            else
            {
                Notifier.Notify($"{item.DisplayName}を使い切った (拠点で補充できる)");
            }
        }

        // 糸移動 (青ハサミ取得後)
        if (Player.TryConsumeGrapple() && Player.CanGrapple())
        {
            StateMachine.ChangeState(Player.GrappleState);
            return true;
        }

        return false;
    }

    /// <summary>
    /// ジャンプ(先行入力 + コヨーテタイム)が可能なら JumpState へ遷移する。
    /// 遷移したら true を返す。
    /// </summary>
    protected bool TryJumpTransition()
    {
        if (Player.HasBufferedJump())
        {
            StateMachine.ChangeState(Player.JumpState);
            return true;
        }

        return false;
    }

    #endregion
}
