using UnityEngine;

/// <summary>
/// ゲームの一時停止管理。メニューが複数重なっても破綻しないよう参照カウント式。
/// Time.timeScale を操作するため、メニュー側の演出は unscaled 時間で動かすこと。
/// </summary>
public static class GamePause
{
    private static int _count;

    public static bool IsPaused => _count > 0;

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
            Time.timeScale = 1f;
    }

    /// <summary>シーン開始時などに強制リセットする。</summary>
    public static void Reset()
    {
        _count = 0;
        Time.timeScale = 1f;
    }
}
