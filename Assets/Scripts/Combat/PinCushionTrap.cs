using DG.Tweening;
using UnityEngine;

/// <summary>
/// アイテム「針山」。足元前方に設置するトラップで、上を通る敵に一定間隔でダメージを与える。
/// 寿命が切れると縮んで消える。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PinCushionTrap : MonoBehaviour
{
    private Collider2D _collider;

    private int _hpDamage;
    private float _tickInterval;
    private float _lifetime;
    private GameObject _source;
    private LayerMask _damageLayer;

    private float _tickTimer;
    private bool _expired;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>設置する。生成直後に呼ぶこと。</summary>
    public void Place(int hpDamage, float tickInterval, float lifetime,
        GameObject source, LayerMask damageLayer)
    {
        _hpDamage = hpDamage;
        _tickInterval = tickInterval;
        _lifetime = lifetime;
        _source = source;
        _damageLayer = damageLayer;

        // 設置の見た目: 少し弾む
        transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
    }

    private void Update()
    {
        if (_expired)
            return;

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Expire();
            return;
        }

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f)
            return;

        // 重なっている敵に一定間隔でダメージ
        var bounds = _collider.bounds;
        var hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f, _damageLayer);
        var anyHit = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                var info = new DamageInfo(_hpDamage, 0, hit.transform.position, _source);
                enemy.TakeDamage(info);
                anyHit = true;
            }
        }

        if (anyHit)
            _tickTimer = _tickInterval;
    }

    private void Expire()
    {
        _expired = true;
        transform.DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
