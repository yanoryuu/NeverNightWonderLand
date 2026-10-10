using UnityEngine;

/// <summary>
/// カメラを一時的に 1 点へ寄せる要求口 (スピーカー破壊などの見せ場用)。<see cref="CameraShake"/> と同じく
/// CameraFollow が毎フレーム <see cref="Evaluate"/> で寄せ具合を受け取り、追従位置・ズームに混ぜる
/// (追従の SmoothDamp には混ぜないので、寄せが終わればそのまま元の追従に戻る)。
/// 実時間で進むのでスロー・ヒットストップ中も同じ速さで寄る。ポーズメニュー中は止まる。見た目だけでゲーム進行には影響しない。
/// </summary>
public static class CameraFocus
{
    private static Vector2 _point;
    private static float _zoom = 1f;
    private static float _inTime;
    private static float _holdTime;
    private static float _outTime;
    private static float _elapsed;
    private static bool _active;

    /// <summary>寄せている最中か (寄る・留まる・戻るのどこか)。</summary>
    public static bool IsActive => _active;

    /// <summary>寄せる先 (ワールド座標)。</summary>
    public static Vector2 Point => _point;

    /// <summary>
    /// point へ inTime 秒で寄せ、holdTime 秒留まり、outTime 秒で元の追従に戻す。
    /// zoom = 寄せきった時の表示範囲の倍率 (1 未満でズームイン。0.75 なら 75% の範囲 = 1.33 倍に拡大)。
    /// 寄せている最中に呼ばれたら新しい要求で上書きする。
    /// </summary>
    public static void Request(Vector2 point, float zoom, float inTime, float holdTime, float outTime)
    {
        _point = point;
        _zoom = Mathf.Clamp(zoom, 0.2f, 2f);
        _inTime = Mathf.Max(0.01f, inTime);
        _holdTime = Mathf.Max(0f, holdTime);
        _outTime = Mathf.Max(0.01f, outTime);
        _elapsed = 0f;
        _active = true;
    }

    /// <summary>寄せを即座にやめる (ステージ遷移・ワープ時)。</summary>
    public static void Stop() => _active = false;

    /// <summary>
    /// このフレームの寄せ具合 (0 = 通常の追従, 1 = 寄せきり) と表示範囲の倍率を返し、時間を進める。
    /// paused なら時間を進めない。
    /// </summary>
    public static float Evaluate(float unscaledDeltaTime, bool paused, out float zoomScale)
    {
        zoomScale = 1f;
        if (!_active)
            return 0f;

        if (!paused)
            _elapsed += unscaledDeltaTime;

        float w;
        if (_elapsed < _inTime)
            w = _elapsed / _inTime;
        else if (_elapsed < _inTime + _holdTime)
            w = 1f;
        else if (_elapsed < _inTime + _holdTime + _outTime)
            w = 1f - (_elapsed - _inTime - _holdTime) / _outTime;
        else
        {
            _active = false;
            return 0f;
        }

        w = Mathf.SmoothStep(0f, 1f, w);
        zoomScale = Mathf.Lerp(1f, _zoom, w);
        return w;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _active = false;
}
