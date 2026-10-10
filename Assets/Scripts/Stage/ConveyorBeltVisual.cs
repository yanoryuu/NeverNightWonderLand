using UnityEngine;

/// <summary>
/// ベルトコンベアの見た目 (当たりや押す力には関わらない)。親の <see cref="Conveyor"/> の流れ (反転フラグ込み) に合わせて
/// 矢印の帯 (_belt, Tiled の SpriteRenderer) を絵の 1 周期ぶんの範囲で横に流し、端のローラー (_rollers) を回す。
/// 帯は左右に 1 周期ぶん長く作り、両端はローラーの絵で隠す。矢印の向きは localScale.x の符号で流れに合わせる。
/// </summary>
public class ConveyorBeltVisual : MonoBehaviour
{
    [SerializeField] private Conveyor _conveyor;
    [SerializeField] private Transform _belt;
    [Tooltip("帯の絵の横の繰り返し周期 (ワールド単位)")]
    [SerializeField] private float _period = 1f / 3f;
    [SerializeField] private Transform[] _rollers;
    [Tooltip("ローラーの半径 (ワールド単位)。回転の速さ = 流れ / 半径")]
    [SerializeField] private float _rollerRadius = 0.5f;

    private float _baseX;
    private float _offset;

    private void Awake()
    {
        if (_conveyor == null)
            _conveyor = GetComponentInParent<Conveyor>();
        if (_belt != null)
            _baseX = _belt.localPosition.x;
    }

    private void Update()
    {
        if (_conveyor == null)
            return;
        var v = _conveyor.CurrentSpeed;
        var dt = Time.deltaTime;
        if (_belt != null && _period > 0f)
        {
            _offset = Mathf.Repeat(_offset + v * dt, _period);
            var p = _belt.localPosition;
            _belt.localPosition = new Vector3(_baseX + _offset - _period * 0.5f, p.y, p.z);
            var s = _belt.localScale;
            var sign = v >= 0f ? 1f : -1f;
            if (Mathf.Sign(s.x) != sign)
                _belt.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
        }

        if (_rollers != null && _rollerRadius > 0f)
        {
            var deg = -v / _rollerRadius * Mathf.Rad2Deg * dt;
            foreach (var r in _rollers)
                if (r != null)
                    r.Rotate(0f, 0f, deg);
        }
    }
}
