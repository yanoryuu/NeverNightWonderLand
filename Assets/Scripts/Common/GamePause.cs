using UnityEngine;

/// <summary>
/// ゲームの一時停止管理。メニューが複数重なっても破綻しないよう参照カウント式。
/// Time.timeScale を操作するため、メニュー側の演出は unscaled 時間で動かすこと。
/// </summary>
public static class GamePause
{
    private static int _count;
    private static float _baseTimeScale = 1f;

    public static bool IsPaused => _count > 0;

    private static float _hitStopRemaining;
    private static HitStopTicker _hitStopTicker;

    /// <summary>ヒットストップで止まっているか (メニューのポーズとは別扱い。操作プロンプトなどは消さない)。</summary>
    public static bool IsHitStopped => _hitStopRemaining > 0f;

    /// <summary>ポーズかヒットストップで時間が止まっているか。プレイヤーはこの間入力を読まない。</summary>
    public static bool IsFrozen => IsPaused || IsHitStopped;

    private static float _slowRemaining;
    private static float _slowScale = 1f;

    /// <summary>スロー演出中か (<see cref="SlowMotion"/>)。止まってはいないのでプレイヤーは動ける。</summary>
    public static bool IsSlowed => _slowRemaining > 0f;

    /// <summary>
    /// ポーズしていない時の Time.timeScale (通常 1)。テストプレイの速度調整 (TestPlaySpeed) が変更する。
    /// ポーズ解除・リセットはこの値に戻すので、メニューを閉じても選んだ速度が保たれる。
    /// </summary>
    public static float BaseTimeScale => _baseTimeScale;

    /// <summary>ポーズしていない時の速度倍率を設定する (ポーズ中なら解除時に適用)。</summary>
    public static void SetBaseTimeScale(float scale)
    {
        _baseTimeScale = Mathf.Max(0f, scale);
        ApplyTimeScale();
    }

    /// <summary>ポーズ要求を1つ積む (最初の1つで停止)。</summary>
    public static void Push()
    {
        _count++;
        if (_count == 1)
            Time.timeScale = 0f;
    }

    /// <summary>ポーズ要求を1つ下ろす (最後の1つで再開)。</summary>
    public static void Pop()
    {
        _count = Mathf.Max(0, _count - 1);
        ApplyTimeScale();
    }

    /// <summary>シーン開始時などに強制リセットする。</summary>
    public static void Reset()
    {
        _count = 0;
        _hitStopRemaining = 0f;
        _slowRemaining = 0f;
        ApplyTimeScale();
    }

    /// <summary>
    /// ヒットストップ: 実時間で seconds 秒だけ時間を止める。重なったら長い方を残す (足し合わせない)。
    /// プレイヤーは止まっている間入力を読まないので、テストプレイの記録・再生にもポーズと同じく
    /// 「入力を消費しないフレーム」として扱われ、ゲーム時間の進みは記録とそろう。
    /// </summary>
    public static void HitStop(float seconds)
    {
        if (seconds <= 0f)
            return;
        _hitStopRemaining = Mathf.Max(_hitStopRemaining, seconds);
        EnsureTicker();
        ApplyTimeScale();
    }

    /// <summary>
    /// スロー演出: 実時間で seconds 秒だけ時間の進みを scale 倍 (0〜1) にする (スピーカー破壊など)。
    /// ヒットストップと同じくポーズ・速度調整 (BaseTimeScale) とは掛け合わせで扱い、終われば元の速度に戻る。
    /// ポーズメニュー中は残り時間を減らさない。重なったら遅い方・長い方を残す。
    /// </summary>
    public static void SlowMotion(float scale, float seconds)
    {
        if (seconds <= 0f)
            return;
        scale = Mathf.Clamp01(scale);
        _slowScale = IsSlowed ? Mathf.Min(_slowScale, scale) : scale;
        _slowRemaining = Mathf.Max(_slowRemaining, seconds);
        EnsureTicker();
        ApplyTimeScale();
    }

    private static void EnsureTicker()
    {
        if (_hitStopTicker != null)
            return;
        var go = new GameObject("[HitStop]") { hideFlags = HideFlags.HideInHierarchy };
        Object.DontDestroyOnLoad(go);
        _hitStopTicker = go.AddComponent<HitStopTicker>();
    }

    private static void ApplyTimeScale()
    {
        Time.timeScale = IsFrozen ? 0f : _baseTimeScale * (IsSlowed ? _slowScale : 1f);
    }

    /// <summary>ヒットストップの残り時間を実時間 (timeScale 0 でも進む) で減らす。</summary>
    private sealed class HitStopTicker : MonoBehaviour
    {
        private void Update()
        {
            if (_slowRemaining > 0f && !IsPaused)
            {
                _slowRemaining -= Time.unscaledDeltaTime;
                if (_slowRemaining <= 0f)
                {
                    _slowRemaining = 0f;
                    ApplyTimeScale();
                }
            }

            if (_hitStopRemaining <= 0f)
                return;
            _hitStopRemaining -= Time.unscaledDeltaTime;
            if (_hitStopRemaining <= 0f)
            {
                _hitStopRemaining = 0f;
                ApplyTimeScale();
            }
        }
    }
}
