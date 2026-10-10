using UnityEngine;

/// <summary>
/// ボスの爆弾 (第2形態)。落下点へ放物線で飛び、落下点には投げた瞬間から印 (爆発範囲の円) を出す。
/// 着弾して fuse 秒後に半径 radius で爆発し、範囲内のプレイヤーにダメージを与える。
/// プレハブ: ルートに SpriteRenderer と本コンポーネントを付けるだけ (Rigidbody/Collider 不要)。
/// プレハブ未設定でも <see cref="CreateFallback"/> で実行時に仮の爆弾を作れる。
/// </summary>
public class BossBomb : BossHazard
{
    private enum Phase { Flying, Fuse, Exploding }

    [Tooltip("落下点の印の色 (爆発範囲を示す円)")]
    [SerializeField] private Color _markColor = new Color(1f, 0.25f, 0.2f, 0.25f);

    [Tooltip("爆発の見た目の色")]
    [SerializeField] private Color _explosionColor = new Color(1f, 0.75f, 0.3f, 0.8f);

    [Tooltip("爆発の見た目を出しておく時間 (sec)")]
    [SerializeField] private float _explosionShowTime = 0.25f;

    [Tooltip("爆発時の画面揺れの強さ (0 で揺らさない)")]
    [SerializeField] private float _shakeAmplitude = 0.12f;

    [Tooltip("爆発時の画面揺れの長さ (sec)")]
    [SerializeField] private float _shakeTime = 0.2f;

    private Phase _phase;
    private Vector2 _from;
    private Vector2 _landing;
    private float _flightTime;
    private float _arcHeight;
    private float _fuseTime;
    private float _radius;
    private float _timer;
    private SpriteRenderer _sprite;
    private SpriteRenderer _mark;
    private SpriteRenderer _explosion;
    private bool _launched;

    /// <summary>
    /// 投げる。生成直後に呼ぶこと。
    /// </summary>
    /// <param name="from">投げる位置</param>
    /// <param name="landing">落下点 (床の上の爆弾の中心)</param>
    /// <param name="flightTime">滞空時間 (sec)</param>
    /// <param name="arcHeight">放物線の高さ (始点と落下点を結ぶ線からの持ち上げ量)</param>
    /// <param name="fuseTime">着弾から爆発までの時間 (sec)</param>
    /// <param name="radius">爆発半径</param>
    /// <param name="damage">ダメージ</param>
    /// <param name="source">投げた者 (ボス)</param>
    public void Launch(Vector2 from, Vector2 landing, float flightTime, float arcHeight, float fuseTime,
        float radius, int damage, GameObject source)
    {
        _from = from;
        _landing = landing;
        _flightTime = Mathf.Max(0.05f, flightTime);
        _arcHeight = arcHeight;
        _fuseTime = fuseTime;
        _radius = radius;
        Damage = damage;
        Source = source;
        _timer = 0f;
        _phase = Phase.Flying;
        _launched = true;
        _sprite = GetComponentInChildren<SpriteRenderer>();
        transform.position = from;

        // 落下点の印 (爆発範囲がわかるよう半径どおりの円)
        _mark = BossKit.CreateMarker("BombMark", BossKit.CircleSprite, _markColor, landing,
            new Vector2(radius * 2f, radius * 2f), BossKit.MarkerSortingOrder - 1);
    }

    private void Update()
    {
        if (!_launched)
            return;

        _timer += Time.deltaTime;
        switch (_phase)
        {
            case Phase.Flying:
            {
                var t = Mathf.Clamp01(_timer / _flightTime);
                var pos = Vector2.Lerp(_from, _landing, t);
                pos.y += _arcHeight * 4f * t * (1f - t);
                transform.position = pos;
                if (t >= 1f)
                {
                    _phase = Phase.Fuse;
                    _timer = 0f;
                }
                break;
            }

            case Phase.Fuse:
            {
                // 爆発が近づくほど速く点滅させる
                if (_sprite != null)
                {
                    var rate = Mathf.Lerp(4f, 16f, Mathf.Clamp01(_timer / Mathf.Max(0.01f, _fuseTime)));
                    _sprite.enabled = Mathf.Repeat(_timer * rate, 1f) < 0.6f;
                }
                if (_timer >= _fuseTime)
                    Explode();
                break;
            }

            case Phase.Exploding:
            {
                if (_explosion != null)
                {
                    var c = _explosion.color;
                    c.a = _explosionColor.a * (1f - Mathf.Clamp01(_timer / _explosionShowTime));
                    _explosion.color = c;
                }
                if (_timer >= _explosionShowTime)
                    Destroy(gameObject);
                break;
            }
        }
    }

    private void Explode()
    {
        _phase = Phase.Exploding;
        _timer = 0f;
        if (_sprite != null)
            _sprite.enabled = false;
        BossKit.DestroyMarker(_mark);
        _mark = null;

        BossKit.HitPlayerCircle(_landing, _radius, Damage, Source);
        _explosion = BossKit.CreateMarker("BombExplosion", BossKit.CircleSprite, _explosionColor, _landing,
            new Vector2(_radius * 2f, _radius * 2f), BossKit.MarkerSortingOrder + 2);
        if (_shakeAmplitude > 0f)
            CameraShake.Shake(_shakeAmplitude, _shakeTime);
    }

    private void OnDestroy()
    {
        BossKit.DestroyMarker(_mark);
        BossKit.DestroyMarker(_explosion);
    }

    /// <summary>プレハブが無い時の仮の爆弾を実行時に作る (テスト用)。</summary>
    public static BossBomb CreateFallback(float visualSize)
    {
        var sr = BossKit.CreateMarker("BossBomb", BossKit.CircleSprite, new Color(0.15f, 0.15f, 0.2f, 1f),
            Vector2.zero, new Vector2(visualSize, visualSize), BossKit.MarkerSortingOrder + 1);
        return sr.gameObject.AddComponent<BossBomb>();
    }
}
