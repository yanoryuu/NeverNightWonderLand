/// <summary>
/// 死亡状態。入力を遮断して RespawnDelay 後にゲームオーバー画面を出す。
/// ゲームオーバー画面が無いシーンではそのままリトライ (GameOverView.RetryCurrentStage) する。
/// ※ リスポーンにはシーンが Build Settings に登録されている必要がある。
/// </summary>
public class DeadState : PlayerState
{
    private float _timer;
    private bool _handled;

    public DeadState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.BeginDeath();
        _timer = Player.Consts.RespawnDelay;
        _handled = false;
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;
        if (_timer <= 0f && !_handled)
        {
            _handled = true;

            var gameOver = UnityEngine.Object.FindFirstObjectByType<GameOverView>();
            if (gameOver != null)
                gameOver.Show();
            else
                GameOverView.RetryCurrentStage(); // 能力・持ち物を引き継いで最後に休んだ拠点から (PendingChanges 38/39)
        }
    }

    public override void PhysicsUpdate()
    {
        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }
}
