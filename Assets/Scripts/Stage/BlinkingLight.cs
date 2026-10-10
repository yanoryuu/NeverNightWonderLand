using UnityEngine;

/// <summary>
/// 点滅する灯り (見た目だけ)。子を含む SpriteRenderer のアルファを周期で上げ下げする。
/// 本道の目印 (A1_D5) のように、並べた灯の _phase を少しずつずらすと「流れる」点滅になる。
/// </summary>
public class BlinkingLight : MonoBehaviour
{
    [Tooltip("1 周期の秒数")]
    [SerializeField] private float _period = 1.2f;

    [Tooltip("周期の開始オフセット (0..1, 周期に対する割合)")]
    [SerializeField] private float _phase;

    [Tooltip("消えている時のアルファ倍率")]
    [SerializeField] private float _minAlpha = 0.15f;

    [Tooltip("1 周期のうち点いている割合 (0..1)。境目はなめらかにつなぐ")]
    [SerializeField] private float _onRatio = 0.4f;

    private SpriteRenderer[] _renderers;
    private float[] _baseAlpha;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseAlpha = new float[_renderers.Length];
        for (var i = 0; i < _renderers.Length; i++)
            _baseAlpha[i] = _renderers[i].color.a;
    }

    private void Update()
    {
        if (_period <= 0f)
            return;
        var t = Mathf.Repeat(Time.time / _period + _phase, 1f);
        // 点いている区間 [0,_onRatio) は 1、残りは _minAlpha。境目 0.08 周期でなめらかに
        const float edge = 0.08f;
        float k;
        if (t < _onRatio)
            k = Mathf.Clamp01(Mathf.Min(t, _onRatio - t) / edge);
        else
            k = 0f;
        var a = Mathf.Lerp(_minAlpha, 1f, k);
        for (var i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null)
                continue;
            var c = r.color;
            c.a = _baseAlpha[i] * a;
            r.color = c;
        }
    }
}
