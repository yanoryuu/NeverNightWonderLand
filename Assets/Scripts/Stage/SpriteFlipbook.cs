using UnityEngine;

/// <summary>
/// 見た目だけのコマ送り (噴水の水・スピーカーの鳴動など)。同じ GameObject の SpriteRenderer の sprite を
/// _frames の順に一定の速さで差し替え続ける (色・向き・拡大は触らないので、被弾フラッシュ等と併用できる)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [Tooltip("コマ (この順に繰り返す)")]
    [SerializeField] private Sprite[] _frames = new Sprite[0];
    [Tooltip("1 秒あたりのコマ数")]
    [SerializeField] private float _fps = 4f;
    [Tooltip("開始のずれ (秒)。並べた物が揃って動かないように")]
    [SerializeField] private float _phase;

    private SpriteRenderer _sprite;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_frames == null || _frames.Length == 0 || _fps <= 0f)
            return;
        var i = Mathf.FloorToInt((Time.time + _phase) * _fps) % _frames.Length;
        var s = _frames[i];
        if (s != null && _sprite.sprite != s)
            _sprite.sprite = s;
    }
}
