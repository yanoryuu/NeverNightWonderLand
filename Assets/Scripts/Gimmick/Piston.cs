using UnityEngine;

/// <summary>
/// タイマーで伸び縮みする押しつぶしピストン。
/// 縮んだ状態で待機 → 予兆 (震え+着色) → 伸びる → 伸びたまま待機 → 縮む、を繰り返す。
/// 本体は Kinematic の Rigidbody2D を速度で動かすので、動いている間も実体として押し・乗れる
/// (上に乗ったプレイヤーは MovingPlatform と同じく運ばれる)。
/// 押しつぶしのダメージは子の HazardZone (_crushHazard, 先端の面に置くトリガー) が担当し、
/// 伸びている最中だけ有効になる。
/// </summary>
[DefaultExecutionOrder(-100)] // プレイヤーより先に今ステップの速度を決める
[RequireComponent(typeof(Rigidbody2D))]
public class Piston : MonoBehaviour
{
    private enum Phase
    {
        Retracted,
        Telegraph,
        Extending,
        Extended,
        Retracting,
    }

    [Tooltip("伸びた位置 (縮んだ位置=配置位置からのオフセット, units)。下へ押しつぶすなら (0,-3)")]
    [SerializeField] private Vector2 _extendOffset = new Vector2(0f, -3f);

    [Tooltip("縮んだ状態で待つ時間 (sec)")]
    [SerializeField] private float _retractedWait = 1.5f;

    [Tooltip("伸びる前の予兆の時間 (sec)")]
    [SerializeField] private float _telegraphTime = 0.5f;

    [Tooltip("伸びるのにかける時間 (sec)。短いほど速く叩きつける")]
    [SerializeField] private float _extendDuration = 0.15f;

    [Tooltip("伸びた状態で待つ時間 (sec)")]
    [SerializeField] private float _extendedWait = 0.8f;

    [Tooltip("縮むのにかける時間 (sec)")]
    [SerializeField] private float _retractDuration = 0.8f;

    [Tooltip("最初の周期の開始遅延 (sec)。並んだピストンの位相をずらす用")]
    [SerializeField] private float _startDelay;

    [Tooltip("押しつぶし判定 (先端の子に置いた HazardZone)。伸びている最中だけ有効になる")]
    [SerializeField] private HazardZone _crushHazard;

    [Tooltip("予兆で震わせる見た目 (子の SpriteRenderer の Transform など, 任意)。本体は震わせない")]
    [SerializeField] private Transform _shakeVisual;

    [Tooltip("予兆の震えの振れ幅 (units)")]
    [SerializeField] private float _shakeAmplitude = 0.06f;

    [Tooltip("予兆中の着色 (子の SpriteRenderer 全てに掛かる)")]
    [SerializeField] private Color _telegraphColor = new Color(1f, 0.55f, 0.45f);

    private Rigidbody2D _rb;
    private SpriteRenderer[] _sprites;
    private Color[] _baseColors;
    private Vector2 _retractedPos;
    private Vector2 _extendedPos;
    private Vector3 _shakeBaseLocal;
    private Phase _phase = Phase.Retracted;
    private float _timer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _retractedPos = _rb.position;
        _extendedPos = _retractedPos + _extendOffset;

        _sprites = GetComponentsInChildren<SpriteRenderer>();
        _baseColors = new Color[_sprites.Length];
        for (var i = 0; i < _sprites.Length; i++)
            _baseColors[i] = _sprites[i].color;

        if (_shakeVisual != null)
            _shakeBaseLocal = _shakeVisual.localPosition;

        _timer = _retractedWait + _startDelay;
        if (_crushHazard != null)
            _crushHazard.SetHazardEnabled(false);
    }

    private void OnDisable()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;
        _timer -= dt;

        var target = _rb.position;
        switch (_phase)
        {
            case Phase.Retracted:
                target = _retractedPos;
                if (_timer <= 0f)
                    Enter(Phase.Telegraph, _telegraphTime);
                break;

            case Phase.Telegraph:
                target = _retractedPos;
                if (_shakeVisual != null)
                    _shakeVisual.localPosition = _shakeBaseLocal + (Vector3)(Random.insideUnitCircle * _shakeAmplitude);
                if (_timer <= 0f)
                    Enter(Phase.Extending, _extendDuration);
                break;

            case Phase.Extending:
                target = Vector2.Lerp(_extendedPos, _retractedPos, Progress(_extendDuration));
                if (_timer <= 0f)
                {
                    target = _extendedPos;
                    Enter(Phase.Extended, _extendedWait);
                }
                break;

            case Phase.Extended:
                target = _extendedPos;
                if (_timer <= 0f)
                    Enter(Phase.Retracting, _retractDuration);
                break;

            case Phase.Retracting:
                target = Vector2.Lerp(_retractedPos, _extendedPos, Progress(_retractDuration));
                if (_timer <= 0f)
                {
                    target = _retractedPos;
                    Enter(Phase.Retracted, _retractedWait);
                }
                break;
        }

        _rb.linearVelocity = (target - _rb.position) / dt;
    }

    /// <summary>残り時間の割合 (1 = 始まったばかり, 0 = 終わり)。Lerp(終点, 始点, これ) で位置になる。</summary>
    private float Progress(float duration) => duration > 0f ? Mathf.Clamp01(_timer / duration) : 0f;

    private void Enter(Phase phase, float duration)
    {
        _phase = phase;
        _timer = duration;

        var telegraph = phase == Phase.Telegraph;
        for (var i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null)
                _sprites[i].color = telegraph ? _baseColors[i] * _telegraphColor : _baseColors[i];
        }

        if (!telegraph && _shakeVisual != null)
            _shakeVisual.localPosition = _shakeBaseLocal;

        if (_crushHazard != null)
            _crushHazard.SetHazardEnabled(phase == Phase.Extending);
    }

    private void OnDrawGizmosSelected()
    {
        var from = Application.isPlaying ? _retractedPos : (Vector2)transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.9f);
        Gizmos.DrawLine(from, from + _extendOffset);
        Gizmos.DrawWireSphere(from + _extendOffset, 0.25f);
    }
}
