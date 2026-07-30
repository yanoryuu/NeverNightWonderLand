/// <summary>
/// 近接攻撃状態 (□ボタン)。装備中の近接攻撃 (MeleeAttackDefinition) のプロファイルで攻撃する。
/// スイング中に攻撃を先行入力すると次の段へつながり、最大 <see cref="PlayerController.MaxCombo"/> 段
/// (基本3、鍛冶強化で最大5) までコンボできる。
/// 地上では攻撃モーション中も水平移動・重力は適用される(移動しながら攻撃可能)。
/// 空中で振り始めた段はその場に滞空する (落下しながら振らない)。
/// 向きは固定し、攻撃判定は各段の HitDelay 経過時に一度だけ出す。
/// モーション終了後に現在の状況へ応じた locomotion ステートへ戻る。
/// </summary>
public class AttackState : PlayerState
{
    private PlayerConsts.AttackProfile _profile;
    private float _timer;
    private bool _hitDone;
    private bool _airStall;
    private int _comboIndex;   // 現在のコンボ段数 (0 始まり)
    private bool _comboQueued; // スイング中に次の段が先行入力されたか

    public AttackState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        Player.StartAttack();
        _comboIndex = 0;
        _comboQueued = false;
        BeginSwing();
    }

    public override void Exit()
    {
        Player.EndAttack();
    }

    /// <summary>コンボ1段ぶんのスイングを開始する。</summary>
    private void BeginSwing()
    {
        _profile = Player.CurrentMeleeProfile;
        _timer = _profile.Duration;
        _hitDone = false;

        _airStall = !Player.IsGrounded;
        if (_airStall)
            Player.Rb.linearVelocity = UnityEngine.Vector2.zero;
    }

    public override void LogicUpdate()
    {
        _timer -= UnityEngine.Time.deltaTime;

        // 攻撃開始から一定時間後に当たり判定を一度だけ出す
        var elapsed = _profile.Duration - _timer;
        if (!_hitDone && elapsed >= _profile.HitDelay)
        {
            Player.PerformAttackHit(_profile);
            _hitDone = true;
        }

        // コンボの先行入力 (最終段では受け付けない)
        if (!_comboQueued && _comboIndex + 1 < Player.MaxCombo && Player.TryConsumeAttack())
            _comboQueued = true;

        if (_timer <= 0f)
        {
            if (_comboQueued)
            {
                _comboIndex++;
                _comboQueued = false;
                BeginSwing();
                return;
            }

            StateMachine.ChangeState(Player.GetLocomotionState());
        }
    }

    public override void PhysicsUpdate()
    {
        // 空中攻撃はその場に滞空する
        if (_airStall)
        {
            Player.Rb.linearVelocity = UnityEngine.Vector2.zero;
            return;
        }

        // 地上攻撃は移動しながら振れる
        Player.ApplyHorizontalMovement();
        Player.ApplyGravity();
    }
}
