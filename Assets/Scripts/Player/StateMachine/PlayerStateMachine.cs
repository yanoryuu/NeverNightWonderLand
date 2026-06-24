/// <summary>
/// プレイヤーのステートマシン。現在のステートを保持し、切り替えを行う。
/// 更新ループ(LogicUpdate/PhysicsUpdate)の呼び出しは <see cref="PlayerController"/> から行う。
/// </summary>
public class PlayerStateMachine
{
    #region Properties

    /// <summary>現在アクティブなステート。</summary>
    public PlayerState CurrentState { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// 初期ステートを設定する。Enter は一度だけ呼ばれる。
    /// </summary>
    public void Initialize(PlayerState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    /// <summary>
    /// ステートを切り替える。旧ステートの Exit → 新ステートの Enter の順で呼ぶ。
    /// </summary>
    public void ChangeState(PlayerState newState)
    {
        if (newState == null || newState == CurrentState)
            return;

        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    #endregion
}
