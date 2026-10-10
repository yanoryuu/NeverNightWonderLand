using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 敵の弾 (直進)。プレイヤーに当たるとダメージ、壁 (地形。すり抜け床は除く) で消える、
/// プレイヤーの攻撃で消せる (IDamageable。プレイヤーの攻撃判定は AttackTargetLayer で OverlapBoxAll →
/// TryGetComponent&lt;IDamageable&gt; なので、弾は Enemy レイヤーのコライダー (トリガー) を持つ)。
/// 敵とプレイヤーは物理衝突しない設定のため、当たり判定は物理コールバックに頼らず毎ステップ自前で行う
/// (プレイヤー: Bounds の重なり / 壁: 進む分の CircleCast)。
/// プレハブ (任意) に付けて使うか、プレハブ未設定なら <see cref="Spawn"/> が丸い仮表示の弾を作る。
/// </summary>
public class EnemyBullet : MonoBehaviour, IDamageable
{
    [Tooltip("当たり判定の半径 (units)。コライダーが無い時に作る CircleCollider2D の半径にも使う")]
    [SerializeField] private float _radius = 0.2f;

    private Rigidbody2D _rb;
    private Vector2 _velocity;
    private int _damage;
    private float _lifetime;
    private GameObject _source;
    private LayerMask _groundLayer;
    private readonly PlayerHitProbe _hitProbe = new();
    private Transform _player;
    private bool _launched;

    private static readonly RaycastHit2D[] s_hits = new RaycastHit2D[8];

    /// <summary>
    /// 弾を生成して発射する。prefab が null なら丸い仮表示の弾を作る。
    /// </summary>
    /// <param name="prefab">弾のプレハブ (任意)</param>
    /// <param name="position">発射位置</param>
    /// <param name="velocity">速度</param>
    /// <param name="damage">プレイヤーへのダメージ</param>
    /// <param name="lifetime">寿命 (sec)</param>
    /// <param name="source">撃った敵</param>
    /// <param name="groundLayer">壁とみなすレイヤー</param>
    /// <param name="sortingFrom">仮表示の並び順を合わせる絵 (任意)</param>
    /// <param name="fallbackColor">仮表示の色</param>
    public static EnemyBullet Spawn(EnemyBullet prefab, Vector2 position, Vector2 velocity, int damage,
        float lifetime, GameObject source, LayerMask groundLayer, SpriteRenderer sortingFrom, Color fallbackColor)
    {
        EnemyBullet bullet;
        if (prefab != null)
        {
            bullet = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            var go = new GameObject("EnemyBullet");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemyFxSprites.Circle;
            sr.color = fallbackColor;
            if (sortingFrom != null)
            {
                sr.sortingLayerID = sortingFrom.sortingLayerID;
                sr.sortingOrder = sortingFrom.sortingOrder + 1;
            }

            bullet = go.AddComponent<EnemyBullet>();
            go.transform.localScale = Vector3.one * (bullet._radius * 2f);
        }

        if (source != null && source.scene.IsValid() && bullet.gameObject.scene != source.scene)
            SceneManager.MoveGameObjectToScene(bullet.gameObject, source.scene);

        bullet.Launch(velocity, damage, lifetime, source, groundLayer);
        return bullet;
    }

    private void Awake()
    {
        // プレイヤーの攻撃判定 (AttackTargetLayer) に入るよう Enemy レイヤーにする
        var enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            // 仮表示は scale = 直径 で作るので、ローカル半径 0.5 = ワールド半径 _radius
            circle.radius = 0.5f;
            col = circle;
        }

        col.isTrigger = true;
    }

    /// <summary>発射する (Spawn から呼ばれる。プレハブを自前で Instantiate した時は直接呼んでもよい)。</summary>
    public void Launch(Vector2 velocity, int damage, float lifetime, GameObject source, LayerMask groundLayer)
    {
        _velocity = velocity;
        _damage = damage;
        _lifetime = lifetime;
        _source = source;
        _groundLayer = EnemyMotionUtil.GroundOrDefault(groundLayer);
        _launched = true;

        var player = FindFirstObjectByType<PlayerController>();
        _player = player != null ? player.transform : null;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.right = velocity.normalized;
    }

    private void FixedUpdate()
    {
        if (!_launched)
            return;

        var dt = Time.fixedDeltaTime;
        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        var pos = _rb.position;
        var step = _velocity * dt;
        var distance = step.magnitude;

        // 壁 (すり抜け床は抜ける)
        if (distance > 0f)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _groundLayer, useTriggers = false };
            var count = Physics2D.CircleCast(pos, _radius, step / distance, filter, s_hits, distance);
            for (var i = 0; i < count; i++)
            {
                if (EnemyMotionUtil.IsOneWayPlatform(s_hits[i].collider))
                    continue;

                Destroy(gameObject);
                return;
            }
        }

        _rb.MovePosition(pos + step);

        // プレイヤー
        var area = new Bounds(pos + step, new Vector3(_radius * 2f, _radius * 2f, 1f));
        if (_hitProbe.TryHit(_player, area, _damage, pos + step, _source != null ? _source : gameObject))
            Destroy(gameObject);
    }

    /// <summary>プレイヤーの攻撃 (近接・飛び道具・設置物) が当たったら消える。</summary>
    public void TakeDamage(in DamageInfo info)
    {
        Destroy(gameObject);
    }
}
