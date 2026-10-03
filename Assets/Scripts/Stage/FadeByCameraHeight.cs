using UnityEngine;

/// <summary>
/// 見た目だけ: カメラ (Camera.main) の高さで子の SpriteRenderer をまとめて薄くする (polish B)。
/// 中景の観覧車 (FerrisMidground) に付け、本物の外周 (y≈162) が画面に入る高さ (W3 の上端〜W4) では消す
/// = 「縮めた輪」と「本物の輪」が同時に見えて矛盾しないようにする。
/// BlinkingLight の子 (毎フレーム自分でアルファを書く) は、その値に掛けるだけにする。
/// </summary>
public class FadeByCameraHeight : MonoBehaviour
{
    [Tooltip("この高さ以下では元のまま")]
    [SerializeField] private float _fadeStartY = 135f;
    [Tooltip("この高さ以上では見えない")]
    [SerializeField] private float _fadeEndY = 152f;

    private SpriteRenderer[] _renderers;
    private float[] _baseAlpha;
    private bool[] _blinking;
    private Camera _camera;
    private float _lastK = 1f;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseAlpha = new float[_renderers.Length];
        _blinking = new bool[_renderers.Length];
        for (var i = 0; i < _renderers.Length; i++)
        {
            _baseAlpha[i] = _renderers[i].color.a;
            _blinking[i] = _renderers[i].GetComponentInParent<BlinkingLight>() != null;
        }
    }

    private void LateUpdate()
    {
        if (_camera == null || !_camera.isActiveAndEnabled)
        {
            _camera = Camera.main;
            if (_camera == null)
                return;
        }

        var k = 1f - Mathf.InverseLerp(_fadeStartY, _fadeEndY, _camera.transform.position.y);
        if (Mathf.Approximately(k, _lastK) && k >= 1f)
            return;
        _lastK = k;
        for (var i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null)
                continue;
            r.enabled = k > 0.001f;
            var c = r.color;
            // BlinkingLight の子はこのフレームに書かれた値に掛ける。それ以外は元のアルファに掛ける
            c.a = (_blinking[i] ? c.a : _baseAlpha[i]) * k;
            r.color = c;
        }
    }
}
