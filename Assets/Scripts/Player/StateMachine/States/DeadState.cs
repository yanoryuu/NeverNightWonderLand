using UnityEngine.SceneManagement;

/// <summary>
/// 死亡状態。入力を遮断して RespawnDelay 後にゲームオーバー画面を出す。
/// ゲームオーバー画面が無いシーンでは従来通りシーンを再読込する。
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
            else if (StageLoader.Instance != null)
                StageLoader.LoadWithPlayerScene(StageLoader.Instance.CurrentStageName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public override void PhysicsUpdate()
    {
        Player.StopHorizontalMovement();
        Player.ApplyGravity();
    }
}
