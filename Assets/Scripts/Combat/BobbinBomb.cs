using DG.Tweening;
using UnityEngine;

/// <summary>
/// アイテム「ボビン爆弾」。曲射で投げ、時限で爆発して範囲内の防御値を大きく削る (ブレイク支援)。
/// 地形の上で転がって止まり、導火線が切れる直前は点滅が速くなる。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BobbinBomb : MonoBehaviour
{
    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Color _baseColor;

    private int _hpDamage;
    private int _guardDamage;
    private float _radius;
    private float _fuse;
    private float _fuseTotal;
    private GameObject _source;
    private LayerMask _damageLayer;
    private bool _exploded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
    }

    private void OnDestroy()
    {
        transform.DOKill();
        _sprite.DOKill();
    }

    /// <summary>発射する。生成直後に呼ぶこと。</summary>
    public void Launch(Vector2 velocity, float gravityScale, float fuse,
        int hpDamage, int guardDamage, float radius, GameObject source, LayerMask damageLayer)
    {
        _hpDamage = hpDamage;
        _guardDamage = guardDamage;
        _radius = radius;
        _fuse = fuse;
        _fuseTotal = fuse;
        _source = source;
        _damageLayer = damageLayer;

        _rb.gravityScale = gravityScale;
        _rb.linearVelocity = velocity;
    }

    private void Update()
    {
        if (_exploded)
            return;

        _fuse -= Time.deltaTime;

        // 導火線: 残りが短いほど速く点滅する
        var blinkSpeed = Mathf.Lerp(20f, 6f, Mathf.Clamp01(_fuse / Mathf.Max(_fuseTotal, 0.01f)));
        var on = Mathf.PingPong(Time.time * blinkSpeed, 1f) > 0.5f;
        _sprite.color = on ? Color.white : _baseColor;

        if (_fuse <= 0f)
            Explode();
    }

    private void Explode()
    {
        _exploded = true;

        var hits = Physics2D.OverlapCircleAll(transform.position, _radius, _damageLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                var info = new DamageInfo(_hpDamage, _guardDamage, transform.position, _source);
                damageable.TakeDamage(info);
            }
        }

        // 爆発の見た目: 一瞬膨らんでフェード
        _rb.simulated = false;
        _sprite.color = new Color(1f, 0.8f, 0.3f, 0.9f);
        transform.DOScale(transform.localScale * (_radius * 2.5f), 0.18f).SetEase(Ease.OutQuad);
        _sprite.DOFade(0f, 0.22f).OnComplete(() => Destroy(gameObject));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _radius > 0f ? _radius : 2f);
    }
}
