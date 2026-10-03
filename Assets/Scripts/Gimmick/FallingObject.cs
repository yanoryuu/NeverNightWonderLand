using DG.Tweening;
using UnityEngine;

/// <summary>
/// 上から落ちてくる物 (荷物など)。FallingObjectSpawner が生成・初期化する。
/// 物理ボディは使わず自前で落下し、毎ステップの重なり判定でプレイヤーに当たればダメージ、
/// 地面 (_groundMask) に当たるか寿命が尽きたら消える。
/// 落ち始める前に _warningTime だけその場で震える (予兆)。
/// </summary>
public class FallingObject : MonoBehaviour
{
    private readonly Collider2D[] _hits = new Collider2D[8];

    private Vector2 _size;
    private int _damage;
    private float _gravity;
    private float _maxFallSpeed;
    private float _lifetime;
    private float _warningTimer;
    private LayerMask _groundMask;
    private Vector3 _hangPosition;
    private float _velocityY;
    private bool _done;

    /// <summary>生成直後に呼ぶ。size は当たり判定の大きさ (ワールド単位)。</summary>
    public void Init(Vector2 size, int damage, float initialSpeed, float gravity, float maxFallSpeed,
        float lifetime, float warningTime, LayerMask groundMask)
    {
        _size = size;
        _damage = damage;
        _velocityY = -Mathf.Abs(initialSpeed);
        _gravity = gravity;
        _maxFallSpeed = maxFallSpeed;
        _lifetime = lifetime;
        _warningTimer = warningTime;
        _groundMask = groundMask;
        _hangPosition = transform.position;
    }

    private void OnDestroy()
    {
        transform.DOKill(); // シーン遷移などで消滅演出の途中に消えても警告を出さない
    }

    private void FixedUpdate()
    {
        if (_done)
            return;

        var dt = Time.fixedDeltaTime;

        // 予兆: その場で小刻みに震える
        if (_warningTimer > 0f)
        {
            _warningTimer -= dt;
            transform.position = _hangPosition + (Vector3)(Random.insideUnitCircle * 0.05f);
            if (_warningTimer <= 0f)
                transform.position = _hangPosition;
            return;
        }

        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            Vanish();
            return;
        }

        _velocityY = Mathf.Max(_velocityY - _gravity * dt, -_maxFallSpeed);
        transform.position += new Vector3(0f, _velocityY * dt, 0f);

        var count = Physics2D.OverlapBox(transform.position, _size, 0f,
            new ContactFilter2D().NoFilter(), _hits);
        for (var i = 0; i < count; i++)
        {
            var hit = _hits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(new DamageInfo(_damage, 0, transform.position, gameObject));
                Vanish();
                return;
            }

            if (!hit.isTrigger && (_groundMask.value & (1 << hit.gameObject.layer)) != 0)
            {
                Vanish();
                return;
            }
        }
    }

    private void Vanish()
    {
        _done = true;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, _size);
    }
}
