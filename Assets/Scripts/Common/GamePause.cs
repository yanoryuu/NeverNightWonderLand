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

    /// <summary>
    /// ポーズしていない時の Time.timeScale (通常 1)。テストプレイの速度調整 (TestPlaySpeed) が変更する。
    /// ポーズ解除・リセットはこの値に戻すので、メニューを閉じても選んだ速度が保たれる。
    /// </summary>
    public static float BaseTimeScale => _baseTimeScale;

    /// <summary>ポーズしていない時の速度倍率を設定する (ポーズ中なら解除時に適用)。</summary>
    public static void SetBaseTimeScale(float scale)
    {
        _baseTimeScale = Mathf.Max(0f, scale);
        if (_count == 0)
            Time.timeScale = _baseTimeScale;
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
        if (_count == 0)
            Time.timeScale = _baseTimeScale;
    }

    /// <summary>シーン開始時などに強制リセットする。</summary>
    public static void Reset()
    {
        _count = 0;
        Time.timeScale = _baseTimeScale;
    }
}
