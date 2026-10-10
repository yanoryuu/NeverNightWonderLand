using UnityEngine;

/// <summary>
/// 画面揺れの要求口。CameraFollow が毎フレーム <see cref="Evaluate"/> でずらし量を受け取り、追従位置に足す
/// (追従の滑らかさには混ぜないので、揺れが終わればそのままの位置に戻る)。
/// 実時間で進むのでヒットストップ中も揺れる。見た目だけでゲーム進行には影響しない。
/// </summary>
public static class CameraShake
{
    // 揺れの細かさ (1 秒あたりの揺れ返しの回数の目安)
    private const float Frequency = 28f;

    private static float _amplitude;
    private static float _duration;
    private static float _remaining;
    private static float _seed;

    /// <summary>
    /// amplitude (ワールド単位) の揺れを duration 秒かけて減衰させる。揺れている最中なら強い方を残す。
    /// </summary>
    public static void Shake(float amplitude, float duration)
    {
        if (amplitude <= 0f || duration <= 0f)
            return;
        if (_remaining > 0f && CurrentStrength() > amplitude)
            return;

        _amplitude = amplitude;
        _duration = duration;
        _remaining = duration;
        _seed = Random.value * 100f;
    }

    /// <summary>揺れを止める (ステージ遷移・ワープ時)。</summary>
    public static void Stop() => _remaining = 0f;

    /// <summary>このフレームのずらし量を返し、残り時間を進める。</summary>
    public static Vector2 Evaluate(float unscaledDeltaTime)
    {
        if (_remaining <= 0f)
            return Vector2.zero;

        var strength = CurrentStrength();
        _remaining -= unscaledDeltaTime;

        // Perlin ノイズで滑らかにランダムな方向へ揺らす (-1..1)
        var t = (_duration - _remaining) * Frequency;
        var x = Mathf.PerlinNoise(_seed, t) * 2f - 1f;
        var y = Mathf.PerlinNoise(_seed + 37f, t) * 2f - 1f;
        return new Vector2(x, y) * strength;
    }

    // 減衰は 2 乗で、出始めが強く最後はすっと消える
    private static float CurrentStrength()
    {
        var k = Mathf.Clamp01(_remaining / _duration);
        return _amplitude * k * k;
    }
}
