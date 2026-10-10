#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// テストプレイ機能の常駐オブジェクト (エディタ/Development Build で起動時に自動生成、DontDestroyOnLoad)。
/// - 他のスクリプトより先に Update する (記録/再生の開始判定)
/// - LateUpdate でコマ送りの再停止・再生の速度合わせを行う
/// - ホットキー (F1〜F8, パッドは R3 長押し) を処理する
/// - 実行時オーバーレイ (TestPlayOverlay) を持つ
/// </summary>
[DefaultExecutionOrder(-10000)]
public class TestPlayRunner : MonoBehaviour
{
    private static TestPlayRunner _instance;

    /// <summary>パッドでオーバーレイを切り替える R3 (右スティック押し込み) の長押し時間 (秒, 実時間)。</summary>
    public const float OverlayHoldSeconds = 0.6f;

    // R3 を押し始めた実時間 (押していない間は null)。長押しで 1 回切り替えたら離すまで切り替えない
    private static float? _r3PressedAt;
    private static bool _r3Toggled;

    public static TestPlayRunner Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate() => Ensure();

    /// <summary>常駐オブジェクトを用意する (何度呼んでもよい)。</summary>
    public static TestPlayRunner Ensure()
    {
        if (_instance != null)
            return _instance;

        var go = new GameObject("[TestPlay]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<TestPlayRunner>();
        go.AddComponent<TestPlayOverlay>();
        return _instance;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        HandleHotkeys();
        TestPlayRecorder.EarlyTick();
    }

    private void LateUpdate()
    {
        TestPlaySpeed.LateTick();
        TestPlayRecorder.LateTick();
    }

    private static void HandleHotkeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f1Key.wasPressedThisFrame) TestPlayOverlay.Visible = !TestPlayOverlay.Visible;
            if (keyboard.f2Key.wasPressedThisFrame) TestPlaySpeed.Slower();
            if (keyboard.f3Key.wasPressedThisFrame) TestPlaySpeed.Faster();
            if (keyboard.f4Key.wasPressedThisFrame) TestPlaySpeed.ResetScale();
            if (keyboard.f5Key.wasPressedThisFrame) TestPlaySpeed.ToggleFrozen();
            if (keyboard.f6Key.wasPressedThisFrame) TestPlaySpeed.Step();
            if (keyboard.f7Key.wasPressedThisFrame) ToggleRecording();
            if (keyboard.f8Key.wasPressedThisFrame) ToggleReplay();
        }

        // パッド: R3 長押し (Select はアイテム画面専用。Docs/UI/UIAssignment.md「3回目の決定」8)。
        // 一時停止・メニュー中も切り替えられるよう実時間で測る
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.rightStickButton.isPressed)
        {
            _r3PressedAt ??= Time.unscaledTime;
            if (!_r3Toggled && Time.unscaledTime - _r3PressedAt.Value >= OverlayHoldSeconds)
            {
                _r3Toggled = true;
                TestPlayOverlay.Visible = !TestPlayOverlay.Visible;
            }
        }
        else
        {
            _r3PressedAt = null;
            _r3Toggled = false;
        }
    }

    /// <summary>記録の開始 (ステージを読み直して開始) / 停止して保存。</summary>
    public static void ToggleRecording()
    {
        if (TestPlayRecorder.IsRecording) TestPlayRecorder.StopRecording();
        else if (!TestPlayRecorder.IsBusy) TestPlayRecorder.StartRecording();
    }

    /// <summary>直前の記録を再生 / 再生中なら停止。</summary>
    public static void ToggleReplay()
    {
        if (TestPlayRecorder.IsReplaying) TestPlayRecorder.StopReplay();
        else if (!TestPlayRecorder.IsBusy) TestPlayRecorder.ReplayLast();
    }
}
#endif
