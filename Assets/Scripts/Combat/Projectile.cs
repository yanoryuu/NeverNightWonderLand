using DG.Tweening;
using UnityEngine;

/// <summary>
/// 汎用の飛び道具。まち針(直進・壁に刺さって足場化)・ミシン針(曲射・高威力)・
/// 黄ハサミの斬撃波が共用する。挙動は <see cref="Launch"/> の引数で決まる。
/// コライダーはトリガーにしておき、刺さった時だけ実体化して足場になる。
/// 足場化したまち針は、プレイヤーが一度踏むと少しの猶予の後に崩れて消える。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Tooltip("足場化した針をプレイヤーが踏んでから崩れるまでの猶予 (sec)")]
    [SerializeField] private float _stuckCrumbleDelay = 0.6f;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private SpriteRenderer _sprite;
    private bool _crumbling;

    private int _hpDamage;
    private int _guardDamage;
    private GameObject _source;
    private bool _stickAsPlatform;
    private LayerMask _damageLayer;
    private LayerMask _groundLayer;
    private float _lifetime;
    private float _bindDuration; // 敵を拘束する時間 (糸玉用。0 なら拘束なし)
    private bool _stuck;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _collider.isTrigger = true;
        _rb.freezeRotation = true;
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_sprite != null)
            _sprite.DOKill();
    }

    /// <summary>
    /// 発射する。生成直後に呼ぶこと。
    /// </summary>
    /// <param name="velocity">初速</param>
    /// <param name="gravityScale">重力 (0 = 直進、正 = 曲射)</param>
    /// <param name="lifetime">寿命 (sec)。刺さった後は寿命では消えない</param>
    /// <param name="hpDamage">HP ダメージ</param>
    /// <param name="guardDamage">防御値ダメージ</param>
    /// <param name="source">発射元 (自傷防止と PlayerProgression の参照に使う)</param>
    /// <param name="stickAsPlatform">壁 (Ground レイヤー) に当たった時に足場化するか</param>
    /// <param name="damageLayer">ダメージ対象のレイヤー</param>
    /// <param name="groundLayer">壁とみなすレイヤー</param>
    public void Launch(Vector2 velocity, float gravityScale, float lifetime,
        int hpDamage, int guardDamage, GameObject source, bool stickAsPlatform,
        LayerMask damageLayer, LayerMask groundLayer, float bindDuration = 0f)
    {
        _hpDamage = hpDamage;
        _guardDamage = guardDamage;
        _source = source;
        _stickAsPlatform = stickAsPlatform;
        _damageLayer = damageLayer;
        _groundLayer = groundLayer;
        _lifetime = lifetime;
        _bindDuration = bindDuration;

        _rb.gravityScale = gravityScale;
        _rb.linearVelocity = velocity;

        FaceVelocity();
    }

    private void Update()
    {
        if (_stuck)
            return;

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        FaceVelocity();
    }

    private void FaceVelocity()
    {
        var v = _rb.linearVelocity;
        if (v.sqrMagnitude > 0.01f)
            transform.right = v.normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_stuck)
            return;

        // 発射元 (プレイヤー) は無視
        if (_source != null)
        {
            if (other.gameObject == _source)
                return;
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == _source)
                return;
        }

        var otherLayerBit = 1 << other.gameObject.layer;

        // ダメージ対象 (敵・箱・リボンなど IDamageable)
        if ((_damageLayer.value & otherLayerBit) != 0 && other.TryGetComponent<IDamageable>(out var damageable))
        {
            var info = new DamageInfo(_hpDamage, _guardDamage, transform.position, _source);
            damageable.TakeDamage(info);

            // 糸玉: 当たった敵を糸で絡めて拘束する
            if (_bindDuration > 0f && other.TryGetComponent<EnemyController>(out var enemy))
                enemy.ApplyBind(_bindDuration);

            Destroy(gameObject);
            return;
        }

        // 壁 (地形)
        if ((_groundLayer.value & otherLayerBit) != 0)
        {
            // 刺さったまち針も Ground レイヤーの足場になるが、針同士はくっつかずすり抜ける
            if (other.GetComponent<Projectile>() != null)
                return;

            if (_stickAsPlatform)
                Stick(_rb.linearVelocity);
            else
                Destroy(gameObject);
        }
    }

    /// <summary>壁に刺さって足場になる (まち針)。めり込まないよう壁表面へ位置を補正する。</summary>
    private void Stick(Vector2 velocity)
    {
        _stuck = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;
        _collider.isTrigger = false;

        SnapToSurface(velocity);

        var groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            gameObject.layer = groundLayer;
    }

    /// <summary>
    /// 進行方向の手前から壁面を探し、針の中心が壁面に来るよう位置を補正する
    /// (半分だけ刺さった見た目になり、壁の中に埋まらない)。
    /// </summary>
    private void SnapToSurface(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.001f)
            return;

        var dir = velocity.normalized;
        var origin = (Vector2)transform.position - dir * 2f;

        // 手前に刺さっている針は無視して、本物の壁面を探す
        var hits = Physics2D.RaycastAll(origin, dir, 4f, _groundLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponent<Projectile>() != null)
                continue;

            transform.position = hit.point;
            return;
        }
    }

    /// <summary>足場化した針をプレイヤーが踏んだら、少しの猶予の後に崩して消す (1回限りの足場)。</summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_stuck || _crumbling)
            return;

        if (collision.gameObject.GetComponentInParent<PlayerController>() == null)
            return;

        _crumbling = true;

        // 崩れる予兆として点滅させ、猶予の後に消える (飛び移る時間を残す)
        if (_sprite != null)
        {
            _sprite.DOFade(0.3f, _stuckCrumbleDelay / 4f)
                .SetLoops(4, LoopType.Yoyo);
        }

        transform.DOScale(Vector3.zero, 0.15f)
            .SetDelay(_stuckCrumbleDelay)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
