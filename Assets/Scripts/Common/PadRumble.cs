using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッドを短時間だけ振動させる。最後の入力がパッドの時だけ振動する (キーボード操作中は何もしない)。
/// 実時間で止めるのでヒットストップ中・一時停止中でも振動が残り続けない。
/// 振動中にもう一度呼ばれたら、強さと時間をその呼び出しで上書きする。
/// </summary>
public static class PadRumble
{
    private static Runner _runner;
    private static Coroutine _stop;

    /// <param name="low">低周波 (左) モーターの強さ 0〜1</param>
    /// <param name="high">高周波 (右) モーターの強さ 0〜1</param>
    /// <param name="seconds">振動させる時間 (実時間 sec)</param>
    public static void Play(float low, float high, float seconds)
    {
        if (seconds <= 0f || InputDeviceTracker.Current != InputDeviceKind.Gamepad)
            return;
        var pad = Gamepad.current;
        if (pad == null)
            return;

        EnsureRunner();
        pad.SetMotorSpeeds(low, high);
        if (_stop != null)
            _runner.StopCoroutine(_stop);
        _stop = _runner.StartCoroutine(_runner.StopAfter(pad, seconds));
    }

    private static void EnsureRunner()
    {
        if (_runner != null)
            return;
        var go = new GameObject("[PadRumble]") { hideFlags = HideFlags.HideInHierarchy };
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<Runner>();
    }

    private sealed class Runner : MonoBehaviour
    {
        public IEnumerator StopAfter(Gamepad pad, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (pad != null)
                pad.SetMotorSpeeds(0f, 0f);
            _stop = null;
        }

        private void OnDisable()
        {
            // プレイ終了・破棄の時に振動が残らないようにする
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }
    }
}
