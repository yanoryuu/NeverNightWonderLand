using UnityEngine;

/// <summary>
/// 中ボス「カップ」の紅茶の滴 (飛沫撒き・紅茶の雨)。自前の重力で飛び、床・壁 (地形) に触れたら消える。
/// プレイヤーへの当たりは <see cref="BossKit.HitPlayerCircle"/> (レイヤー衝突設定に依存しない)。
/// slashable のときは Enemy レイヤーのトリガーを持つ IDamageable になり、プレイヤーの攻撃で消せる
/// (<see cref="EnemyBullet"/> と同じ作法)。すり抜け床は上へ飛んでいる間だけ素通りする (上から落ちたら消える)。
/// ボスの危険物 (<see cref="BossHazard"/>) なので、撃破時の弾消しでまとめて消える。
/// 見た目は実行時の丸 (素材が来るまでの最小限の表示)。
/// </summary>
public class CupDroplet : BossHazard, IDamageable
{
    private const int SortingOrder = BossKit.MarkerSortingOrder + 1;

    private static readonly Collider2D[] s_cols = new Collider2D[4];

    private Rigidbody2D _rb;
    private Vector2 _pos;
    private Vector2 _velocity;
    private float _gravity;
    private float _radius;
    private float _lifetime;
    private LayerMask _groundMask;
    private bool _launched;

    /// <summary>
    /// 滴を作って飛ばす。
    /// </summary>
    /// <param name="position">出す位置</param>
    /// <param name="velocity">初速</param>
    /// <param name="gravity">下向きの加速度 (units/sec²)</param>
    /// <param name="radius">当たり判定・見た目の半径 (units)</param>
    /// <param name="damage">プレイヤーへのダメージ</param>
    /// <param name="lifetime">寿命 (sec)</param>
    /// <param name="source">発射元 (ボス)</param>
    /// <param name="groundMask">床・壁のレイヤー</param>
    /// <param name="slashable">プレイヤーの攻撃で消せるか</param>
    /// <param name="color">見た目の色</param>
    public static CupDroplet Spawn(Vector2 position, Vector2 velocity, float gravity, float radius, int damage,
        float lifetime, GameObject source, LayerMask groundMask, bool slashable, Color color)
    {
        var sr = BossKit.CreateMarker("CupDroplet", BossKit.CircleSprite, color, position,
            new Vector2(radius * 2f, radius * 2.3f), SortingOrder);
        var go = sr.gameObject;
        if (slashable)
        {
            // プレイヤーの攻撃判定 (AttackTargetLayer) に入るよう Enemy レイヤーのトリガーにする
            var enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                go.layer = enemyLayer;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            // localScale = 直径なので、ローカル半径 0.5 = ワールド半径 radius。斬りやすいよう少し大きめ
            col.radius = 0.7f;
        }

        var droplet = go.AddComponent<CupDroplet>();
        droplet._rb = go.GetComponent<Rigidbody2D>();
        droplet._pos = position;
        droplet._velocity = velocity;
        droplet._gravity = gravity;
        droplet._radius = radius;
        droplet._lifetime = lifetime;
        droplet._groundMask = BossKit.GroundMaskOrDefault(groundMask);
        droplet.Damage = damage;
        droplet.Source = source;
        droplet._launched = true;
        return droplet;
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

        _velocity.y -= _gravity * dt;
        _pos += _velocity * dt;
        if (_rb != null)
            _rb.MovePosition(_pos);
        else
            transform.position = _pos;

        if (BossKit.HitPlayerCircle(_pos, _radius, Damage, Source))
        {
            Destroy(gameObject);
            return;
        }

        if (TouchesGround())
            Destroy(gameObject);
    }

    /// <summary>床・壁に触れたか (すり抜け床は上昇中だけ無視する)。</summary>
    private bool TouchesGround()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = _groundMask, useTriggers = false };
        var count = Physics2D.OverlapCircle(_pos, _radius * 0.6f, filter, s_cols);
        for (var i = 0; i < count; i++)
        {
            if (_velocity.y > 0f && EnemyMotionUtil.IsOneWayPlatform(s_cols[i]))
                continue;
            return true;
        }
        return false;
    }

    /// <summary>プレイヤーの攻撃が当たったら消える (slashable の時だけコライダーを持つ)。</summary>
    public void TakeDamage(in DamageInfo info)
    {
        Destroy(gameObject);
    }
}
