using UnityEngine;

/// <summary>
/// 見た目だけのゆらぎ (蒸気の柱・水面など)。localScale と位置を sin で小さく揺らす。
/// 有効になるたびに元の値から始める (HazardZone の _activeVisual に付けても崩れない)。
/// </summary>
public class SpriteWobble : MonoBehaviour
{
    [Tooltip("スケールの揺れ幅 (x, y の割合)")]
    [SerializeField] private Vector2 _scaleAmplitude = new Vector2(0.08f, 0.05f);
    [Tooltip("位置の揺れ幅 (ローカル)")]
    [SerializeField] private Vector2 _moveAmplitude = Vector2.zero;
    [Tooltip("揺れの速さ (rad/s)")]
    [SerializeField] private float _speed = 9f;
    [SerializeField] private float _phase;

    private Vector3 _baseScale;
    private Vector3 _basePos;
    private bool _captured;

    private void OnEnable()
    {
        if (!_captured)
        {
            _baseScale = transform.localScale;
            _basePos = transform.localPosition;
            _captured = true;
        }
    }

    private void Update()
    {
        var t = Time.time * _speed + _phase;
        var sx = 1f + Mathf.Sin(t) * _scaleAmplitude.x;
        var sy = 1f + Mathf.Sin(t * 1.37f + 1.1f) * _scaleAmplitude.y;
        transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
        if (_moveAmplitude != Vector2.zero)
            transform.localPosition = _basePos + new Vector3(Mathf.Sin(t * 0.7f) * _moveAmplitude.x, Mathf.Sin(t * 0.9f + 0.5f) * _moveAmplitude.y, 0f);
    }
}
