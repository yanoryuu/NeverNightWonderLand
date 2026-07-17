using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

/// <summary>
/// チュートリアル「準備室」の進行管理。手順1〜8を UniTask の逐次シーケンスで進める:
/// 1. 歩く → 2. 走る(ダッシュ) → 3. ジャンプ → 4. 箱に攻撃(二刀流/両手持ち/切替) →
/// 5. 実戦(ブレイク+裁断) → 6. 回復案内 → 7. レバーでドア開放 → 8. 終了。
/// 達成判定は各オブジェクトのイベントと UniTask.WaitUntil で行う。
/// 死亡時はシーンごとリロードされるので最初からやり直しになる。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("参照 (プレイヤー)")]
    [SerializeField] private PlayerController _player;

    [Header("参照 (UI)")]
    [SerializeField] private TutorialMessageView _message;

    [Header("参照 (ステージ)")]
    [Tooltip("手順4で壊す練習用の箱")]
    [SerializeField] private BreakableBox[] _boxes;

    [Tooltip("手順5で倒す敵")]
    [SerializeField] private EnemyController[] _enemies;

    [SerializeField] private LeverSwitch _lever;
    [SerializeField] private Door _door;

    [Header("参照 (通過ゾーン)")]
    [Tooltip("手順1: 歩いて到達するゾーン")]
    [SerializeField] private TutorialStepTrigger _walkGoal;

    [Tooltip("手順3: ジャンプで段差を越えた先のゾーン")]
    [SerializeField] private TutorialStepTrigger _jumpGoal;

    [Tooltip("手順8: ドアの先の終了ゾーン")]
    [SerializeField] private TutorialStepTrigger _endGoal;

    private int _boxesBroken;
    private int _enemiesDefeated;
    private bool _healUsed;
    private bool _doorOpened;
    private bool _inCombatStep;

    // 現在表示中の案内文 (キーボード用 / パッド用)。デバイス切替時に出し直す
    private string _stepKeyboardText;
    private string _stepGamepadText;

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        // 達成イベントの購読
        foreach (var box in _boxes)
        {
            if (box != null)
                box.OnBroken += () => _boxesBroken++;
        }

        foreach (var enemy in _enemies)
        {
            if (enemy == null)
                continue;

            enemy.OnDied += _ => _enemiesDefeated++;

            // 実戦ステップ中にブレイクしたら裁断を促す
            enemy.IsBroken.Subscribe(broken =>
            {
                if (broken && _inCombatStep)
                    ShowStep("今だ! [L] で フィニッシャー「裁断」!",
                             "今だ! R1 で フィニッシャー「裁断」!");
            }).AddTo(_disposables);
        }

        if (_player != null && _player.HealGauge != null)
            _player.HealGauge.OnHealUsed += () => _healUsed = true;

        if (_door != null)
            _door.OnOpened += () => _doorOpened = true;

        // 最後に使ったデバイスに合わせて案内文を出し直す
        InputDeviceTracker.OnChanged += OnDeviceChanged;

        RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy()
    {
        InputDeviceTracker.OnChanged -= OnDeviceChanged;
        _disposables.Dispose();
    }

    /// <summary>案内文をデバイス別テキスト付きで表示する。デバイス切替時に自動で出し直される。</summary>
    private void ShowStep(string keyboardText, string gamepadText = null)
    {
        _stepKeyboardText = keyboardText;
        _stepGamepadText = gamepadText ?? keyboardText;
        RenderStep();
    }

    private void RenderStep()
    {
        if (_stepKeyboardText == null)
            return;

        _message.Show(InputDeviceTracker.Current == InputDeviceKind.Gamepad
            ? _stepGamepadText
            : _stepKeyboardText);
    }

    private void OnDeviceChanged(InputDeviceKind _)
    {
        RenderStep();
    }

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        // 1. 歩く
        ShowStep("[A] / [D] で移動して、右へ進もう",
                 "左スティックで移動して、右へ進もう");
        await UniTask.WaitUntil(() => _walkGoal == null || _walkGoal.Passed, cancellationToken: ct);

        // 2. 走る (ダッシュ)
        ShowStep("[Shift] でダッシュ! ダッシュ後は走り続けられる",
                 "R2 でダッシュ! ダッシュ後は走り続けられる");
        await UniTask.WaitUntil(IsDashingOrRunning, cancellationToken: ct);

        // 3. ジャンプ
        ShowStep("[Space] でジャンプして段差を越えよう",
                 "×(A) でジャンプして段差を越えよう");
        await UniTask.WaitUntil(() => _jumpGoal == null || _jumpGoal.Passed, cancellationToken: ct);

        // 4. 箱に攻撃 (近接と特殊攻撃の練習)
        ShowStep("[J] で近接攻撃、[K] で特殊攻撃 (遠距離)。箱を全部壊そう",
                 "□(X) で近接攻撃、△(Y) で特殊攻撃 (遠距離)。箱を全部壊そう");
        await UniTask.WaitUntil(() => _boxesBroken >= _boxes.Length, cancellationToken: ct);

        // 5. 実戦 (ブレイクと裁断)
        _inCombatStep = true;
        ShowStep("敵だ! 特殊攻撃で防御値(白)を削ってブレイクさせよう");
        await UniTask.WaitUntil(() => _enemiesDefeated >= _enemies.Length, cancellationToken: ct);
        _inCombatStep = false;

        // 6. 回復案内
        if (_player != null && _player.CanHeal())
        {
            ShowStep("攻撃を当てると回復ゲージが溜まる。[S] で HP を回復しよう",
                     "攻撃を当てると回復ゲージが溜まる。○(B) で HP を回復しよう");
            await UniTask.WaitUntil(() => _healUsed || !_player.CanHeal(), cancellationToken: ct);
        }
        else
        {
            // HP 満タン or ゲージ無しなら説明だけして進む
            ShowStep("攻撃を当てると回復ゲージが溜まり、[S] で HP を回復できる",
                     "攻撃を当てると回復ゲージが溜まり、○(B) で HP を回復できる");
            await UniTask.Delay(System.TimeSpan.FromSeconds(4), cancellationToken: ct);
        }

        // 7. レバーでドアを開ける
        ShowStep("[E] でレバーを引いて、ドアを開けよう",
                 "十字キー↑ でレバーを引いて、ドアを開けよう");
        await UniTask.WaitUntil(() => _doorOpened, cancellationToken: ct);

        // 8. 終了
        ShowStep("チュートリアル完了! ドアの先へ進もう");
        await UniTask.WaitUntil(() => _endGoal == null || _endGoal.Passed, cancellationToken: ct);

        ShowStep("おつかれさま! 「準備室」クリア!");
        await UniTask.Delay(System.TimeSpan.FromSeconds(4), cancellationToken: ct);

        // 表示終了 (以降はデバイス切替でも出し直さない)
        _stepKeyboardText = null;
        _stepGamepadText = null;
        _message.Hide();
    }

    /// <summary>ダッシュ (または走り) 中か。通常移動より速く動いていれば達成とみなす。</summary>
    private bool IsDashingOrRunning()
    {
        if (_player == null)
            return true;

        return Mathf.Abs(_player.Rb.linearVelocity.x) > _player.Consts.MoveSpeed + 0.5f;
    }
}
