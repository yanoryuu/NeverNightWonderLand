using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 敵が放物線で投げる爆弾 (ポップコーン投げ / PopcornThrower のポップコーン)。
/// - 飛行: 自前の重力で放物線。床・足場 (すり抜け床は上から落ちてきた時だけ) に着くと跳ねずに止まる。
///   壁に当たったら横の勢いを失ってそこで落ち、床に着いたら着地扱い。天井に当たったら落ち始める。
/// - 空中でプレイヤーに直接当たったら、その場で即爆発。
/// - 着地: 着地点に爆発範囲の円の予告を出し、fuse 秒点滅 → 爆発 (半径 radius、範囲内のプレイヤーにダメージ、
///   小さな爆発パーティクル + 軽い画面揺れ)。
/// - プレイヤーの攻撃が当たると、爆発せずに消える (IDamageable。EnemyBullet と同じく Enemy レイヤーのトリガーを持つ)。
/// 敵とプレイヤーは物理衝突しない設定なので、当たり判定は物理コールバックに頼らず毎ステップ自前で行う
/// (壁・床: 進む分の CircleCast / プレイヤー: Bounds の重なり / 爆発: <see cref="BossKit.HitPlayerCircle"/>)。
/// プレハブ (任意) に付けて使うか、プレハブ未設定なら <see cref="Spawn"/> が小さな丸い仮表示を作る。
/// </summary>
public class EnemyLobBomb : MonoBehaviour, IDamageable
{
    private enum Phase { Flying, Fuse, Exploding }

    [Tooltip("地形・プレイヤーへの当たり判定の半径 (units)。仮表示の大きさ (直径) にも使う")]
    [SerializeField] private float _radius = 0.18f;

    [Tooltip("プレイヤーの攻撃で斬られる判定の半径 (units)。コライダーが無い時に作る CircleCollider2D の大きさ。" +
             "床に落ちた小さな粒でも通常攻撃 (足元より少し上の箱) で斬れるよう、見た目より大きめにする")]
    [SerializeField] private float _slashRadius = 0.5f;

    [Tooltip("着地点の予告 (爆発範囲の円) の色")]
    [SerializeField] private Color _markColor = new Color(1f, 0.3f, 0.2f, 0.28f);

    [Tooltip("予告の点滅で明るくなった時の色")]
    [SerializeField] private Color _markBlinkColor = new Color(1f, 0.55f, 0.3f, 0.5f);

    [Tooltip("爆発の閃光 (範囲を示す円) の色")]
    [SerializeField] private Color _flashColor = new Color(1f, 0.85f, 0.45f, 0.75f);

    [Tooltip("爆発の閃光を出しておく時間 (sec)")]
    [SerializeField] private float _flashTime = 0.2f;

    [Tooltip("爆発時の画面揺れの強さ (0 で揺らさない)")]
    [SerializeField] private float _shakeAmplitude = 0.08f;

    [Tooltip("爆発時の画面揺れの長さ (sec)")]
    [SerializeField] private float _shakeTime = 0.15f;

    [Tooltip("落下の最高速 (units/sec)。足場より下へ落ち続ける時の上限")]
    [SerializeField] private float _maxFallSpeed = 20f;

    [Tooltip("寿命 (sec)。着地しないまま落ち続けた時に消す")]
    [SerializeField] private float _maxLifetime = 5f;

    // 予告・閃光の描画順 (投げた敵の絵より手前。着地後の爆弾は予告の円のさらに手前)
    private const int MarkOrderOffset = 1;
    private const int FlashOrderOffset = 3;

    private Phase _phase;
    private Rigidbody2D _rb;
    private Vector2 _velocity;
    private float _gravity;
    private float _fuseTime;
    private float _explosionRadius;
    private int _damage;
    private GameObject _source;
    private LayerMask _groundLayer;
    private Material _particleMaterial;
    private float _timer;
    private float _life;
    private bool _launched;
    private CircleCollider2D _ownCollider;
    private float _passOneWayAbove = float.PositiveInfinity;

    private SpriteRenderer _sprite;
    private SpriteRenderer _mark;
    private SpriteRenderer _flash;
    private Transform _player;
    private Collider2D _playerCollider;

    private static readonly RaycastHit2D[] s_hits = new RaycastHit2D[8];
    private static Material s_additive;
    private static Material s_additiveSource;

    /// <summary>
    /// 上面が y より高いすり抜け床は通り抜けて、狙いの高さまで落ちるようにする (足場の上から下のプレイヤーへ投げた時、
    /// 自分の乗っている足場に着地してしまわないように)。Spawn の後に呼ぶ。狙いと同じ高さの足場には今までどおり乗る。
    /// </summary>
    public void PassOneWayAbove(float y) => _passOneWayAbove = y;

    /// <summary>着地して爆発待ちか (テスト・デバッグ用)。</summary>
    public bool IsLanded => _phase == Phase.Fuse;

    /// <summary>
    /// 爆弾を生成して投げる。prefab が null なら小さな丸い仮表示を作る。
    /// </summary>
    /// <param name="prefab">爆弾のプレハブ (任意)</param>
    /// <param name="position">投げる位置</param>
    /// <param name="velocity">初速</param>
    /// <param name="gravity">重力加速度 (正の値, units/sec²)</param>
    /// <param name="fuseTime">着地から爆発までの時間 (sec)</param>
    /// <param name="explosionRadius">爆発半径</param>
    /// <param name="damage">爆発のダメージ</param>
    /// <param name="source">投げた敵</param>
    /// <param name="groundLayer">床・壁とみなすレイヤー</param>
    /// <param name="particleMaterial">爆発パーティクルの素材 (URP ParticlesUnlit。null ならパーティクルは出さない)</param>
    /// <param name="sortingFrom">並び順を合わせる絵 (任意)</param>
    /// <param name="fallbackColor">仮表示の色</param>
    public static EnemyLobBomb Spawn(EnemyLobBomb prefab, Vector2 position, Vector2 velocity, float gravity,
        float fuseTime, float explosionRadius, int damage, GameObject source, LayerMask groundLayer,
        Material particleMaterial, SpriteRenderer sortingFrom, Color fallbackColor)
    {
        EnemyLobBomb bomb;
        if (prefab != null)
        {
            bomb = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            var go = new GameObject("EnemyLobBomb");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemyFxSprites.Circle;
            sr.color = fallbackColor;
            bomb = go.AddComponent<EnemyLobBomb>();
            go.transform.localScale = Vector3.one * (bomb._radius * 2f);
        }

        var visual = bomb.GetComponentInChildren<SpriteRenderer>();
        if (visual != null && sortingFrom != null)
        {
            visual.sortingLayerID = sortingFrom.sortingLayerID;
            visual.sortingOrder = sortingFrom.sortingOrder + 1;
        }

        if (source != null && source.scene.IsValid() && bomb.gameObject.scene != source.scene)
            SceneManager.MoveGameObjectToScene(bomb.gameObject, source.scene);

        bomb.Launch(velocity, gravity, fuseTime, explosionRadius, damage, source, groundLayer, particleMaterial);
        return bomb;
    }

    private void Awake()
    {
        // プレイヤーの攻撃判定 (AttackTargetLayer) に入るよう Enemy レイヤーにする (EnemyBullet と同じ)
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
            // 大きさは Launch で合わせる (仮表示は Awake の後に scale を決めるため)
            _ownCollider = gameObject.AddComponent<CircleCollider2D>();
            col = _ownCollider;
        }

        col.isTrigger = true;
        _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>投げる (Spawn から呼ばれる。プレハブを自前で Instantiate した時は直接呼んでもよい)。</summary>
    public void Launch(Vector2 velocity, float gravity, float fuseTime, float explosionRadius, int damage,
        GameObject source, LayerMask groundLayer, Material particleMaterial)
    {
        _velocity = velocity;
        _gravity = Mathf.Max(0f, gravity);
        _fuseTime = Mathf.Max(0f, fuseTime);
        _explosionRadius = explosionRadius;
        _damage = damage;
        _source = source;
        _groundLayer = EnemyMotionUtil.GroundOrDefault(groundLayer);
        _particleMaterial = particleMaterial;
        _phase = Phase.Flying;
        _timer = 0f;
        _life = 0f;
        _launched = true;

        // 斬られる判定 (自前で作ったコライダーだけ。ワールド半径 = _slashRadius)
        if (_ownCollider != null)
            _ownCollider.radius = Mathf.Max(_radius, _slashRadius) / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));

        var player = FindFirstObjectByType<PlayerController>();
        _player = player != null ? player.transform : null;
    }

    private void FixedUpdate()
    {
        if (!_launched || _phase != Phase.Flying)
            return;

        var dt = Time.fixedDeltaTime;
        _life += dt;
        if (_life >= _maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        _velocity.y = Mathf.Max(_velocity.y - _gravity * dt, -_maxFallSpeed);
        var pos = _rb.position;
        var step = _velocity * dt;

        // 地形: 壁で横の勢いを失う / 天井で落ち始める / 床・足場で着地
        for (var iter = 0; iter < 2; iter++)
        {
            var distance = step.magnitude;
            if (distance <= 0.0001f)
                break;

            if (!CastTerrain(pos, step / distance, distance, out var hit))
                break;

            var n = hit.normal;
            if (n.y > 0.5f)
            {
                // 床・足場の上面: 跳ねずに止まる
                Land(hit.centroid);
                return;
            }

            // その手前まで進めて、当たった面に沿う向きの勢いだけ残す
            var travel = Mathf.Max(0f, hit.distance - 0.01f);
            pos += step / distance * travel;
            if (Mathf.Abs(n.x) > 0.5f)
                _velocity.x = 0f; // 壁: そこで落ちる
            if (n.y < -0.5f)
                _velocity.y = Mathf.Min(_velocity.y, 0f); // 天井
            step = _velocity * dt * (1f - travel / distance);
        }

        pos += step;
        _rb.MovePosition(pos);

        // 空中でプレイヤーに直接当たったら即爆発
        if (OverlapsPlayer(new Bounds(pos, new Vector3(_radius * 2f, _radius * 2f, 1f))))
        {
            transform.position = pos;
            Explode(pos);
        }
    }

    /// <summary>進む方向へ CircleCast して地形 (すり抜け床は上から落ちてきた時の上面だけ) に当たるか。</summary>
    private bool CastTerrain(Vector2 pos, Vector2 dir, float distance, out RaycastHit2D result)
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = _groundLayer, useTriggers = false };
        var count = Physics2D.CircleCast(pos, _radius, dir, filter, s_hits, distance);
        for (var i = 0; i < count; i++)
        {
            var h = s_hits[i];
            if (h.distance <= 0f && h.fraction <= 0f && EnemyMotionUtil.IsOneWayPlatform(h.collider))
                continue; // 足場の中から始まった (下から抜けている途中)
            if (EnemyMotionUtil.IsOneWayPlatform(h.collider) && (dir.y >= 0f || h.normal.y <= 0.5f))
                continue; // すり抜け床は上から落ちてきた時だけ乗る
            if (EnemyMotionUtil.IsOneWayPlatform(h.collider) && h.point.y > _passOneWayAbove)
                continue; // 狙いより高いすり抜け床は通り抜ける (PassOneWayAbove)
            result = h;
            return true;
        }

        result = default;
        return false;
    }

    private bool OverlapsPlayer(Bounds area)
    {
        if (_player == null)
            return false;
        if (_playerCollider == null)
            _playerCollider = _player.GetComponent<Collider2D>();
        return _playerCollider != null && _playerCollider.enabled && area.Intersects(_playerCollider.bounds);
    }

    private void Land(Vector2 at)
    {
        _phase = Phase.Fuse;
        _timer = 0f;
        _velocity = Vector2.zero;
        transform.position = at;
        _rb.position = at;

        // 着地点に爆発範囲の円の予告
        _mark = BossKit.CreateMarker("PopcornMark", BossKit.CircleSprite, _markColor, at,
            new Vector2(_explosionRadius * 2f, _explosionRadius * 2f));
        if (_sprite != null)
        {
            // 予告の円は爆弾のすぐ後ろ (円で爆弾の粒が隠れないように、爆弾を 1 つ手前へ出す)
            _mark.sortingLayerID = _sprite.sortingLayerID;
            _mark.sortingOrder = _sprite.sortingOrder + MarkOrderOffset;
            _sprite.sortingOrder = _mark.sortingOrder + 1;
        }

        if (_fuseTime <= 0f)
            Explode(at);
    }

    private void Update()
    {
        if (!_launched)
            return;

        switch (_phase)
        {
            case Phase.Fuse:
            {
                _timer += Time.deltaTime;
                // 爆発が近づくほど速く点滅させる (爆弾と予告の円)
                var rate = Mathf.Lerp(5f, 16f, Mathf.Clamp01(_timer / Mathf.Max(0.01f, _fuseTime)));
                var on = Mathf.Repeat(_timer * rate, 1f) < 0.5f;
                if (_sprite != null)
                    _sprite.enabled = on || _timer < 0.05f;
                if (_mark != null)
                    _mark.color = on ? _markBlinkColor : _markColor;
                if (_timer >= _fuseTime)
                    Explode(transform.position);
                break;
            }

            case Phase.Exploding:
            {
                _timer += Time.deltaTime;
                if (_flash != null)
                {
                    var t = Mathf.Clamp01(_timer / Mathf.Max(0.01f, _flashTime));
                    var c = _flashColor;
                    c.a *= 1f - t;
                    _flash.color = c;
                    _flash.transform.localScale = Vector3.one * (_explosionRadius * 2f * Mathf.Lerp(0.85f, 1.05f, t));
                }
                if (_timer >= _flashTime)
                    Destroy(gameObject);
                break;
            }
        }
    }

    private void Explode(Vector2 at)
    {
        if (_phase == Phase.Exploding)
            return;

        _phase = Phase.Exploding;
        _timer = 0f;
        if (_sprite != null)
            _sprite.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false; // 爆発後は斬れない
        BossKit.DestroyMarker(_mark);
        _mark = null;

        BossKit.HitPlayerCircle(at, _explosionRadius, _damage, _source != null ? _source : gameObject);

        _flash = BossKit.CreateMarker("PopcornBlast", BossKit.CircleSprite, _flashColor, at,
            new Vector2(_explosionRadius * 2f, _explosionRadius * 2f));
        if (_sprite != null)
        {
            _flash.sortingLayerID = _sprite.sortingLayerID;
            _flash.sortingOrder = _sprite.sortingOrder + FlashOrderOffset;
        }

        if (_particleMaterial != null)
            SpawnParticles(at);
        if (_shakeAmplitude > 0f)
            CameraShake.Shake(_shakeAmplitude, _shakeTime);
    }

    /// <summary>プレイヤーの攻撃 (近接・飛び道具・設置物) が当たったら、爆発せずに消える。</summary>
    public void TakeDamage(in DamageInfo info)
    {
        if (_phase == Phase.Exploding)
            return;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        BossKit.DestroyMarker(_mark);
        BossKit.DestroyMarker(_flash);
    }

    #region 爆発パーティクル (SpeakerBreakEffect の作りを小さくしたもの)

    private void SpawnParticles(Vector2 at)
    {
        var layerId = _sprite != null ? _sprite.sortingLayerID : 0;
        var order = (_sprite != null ? _sprite.sortingOrder : 0) + FlashOrderOffset + 1;
        var root = new GameObject("PopcornExplosion");
        root.transform.position = at;
        if (gameObject.scene.IsValid())
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        var additive = Additive(_particleMaterial);
        var r = Mathf.Max(0.3f, _explosionRadius);

        // 火の玉: 小さな橙の玉が広がって消える
        var fire = NewSystem("Fire", root.transform, at, additive, layerId, order + 1);
        {
            var main = fire.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(r * 0.35f, r * 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(1f, 0.55f, 0.2f));
            var shape = fire.shape;
            shape.radius = r * 0.15f;
            Burst(fire, 10);
            var col = fire.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(1f, 0.95f, 0.7f), new Color(0.9f, 0.3f, 0.05f), 0.4f);
            Drag(fire, 4f);
        }

        // はじけた粒 (ポップコーンのかけら): 白〜黄の小粒が飛んで落ちる
        var bits = NewSystem("Bits", root.transform, at, _particleMaterial, layerId, order + 2);
        {
            var main = bits.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.92f), new Color(1f, 0.85f, 0.4f));
            main.gravityModifier = 1.6f;
            var shape = bits.shape;
            shape.radius = r * 0.1f;
            Burst(bits, 16);
            var col = bits.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(Color.white, new Color(1f, 0.8f, 0.4f), 0.6f);
        }

        // 煙: 少しだけ残る灰色の玉
        var smoke = NewSystem("Smoke", root.transform, at, _particleMaterial, layerId, order);
        {
            var main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(r * 0.4f, r * 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.58f, 0.62f, 0.6f),
                new Color(0.75f, 0.73f, 0.78f, 0.5f));
            main.gravityModifier = -0.1f;
            var shape = smoke.shape;
            shape.radius = r * 0.2f;
            Burst(smoke, 5);
            var col = smoke.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(0.8f, 0.78f, 0.82f), new Color(0.5f, 0.48f, 0.52f), 0.3f);
            Drag(smoke, 3f);
        }

        fire.Play();
        bits.Play();
        smoke.Play();
        Destroy(root, 1.5f);
    }

    private static ParticleSystem NewSystem(string name, Transform parent, Vector2 position, Material material,
        int layerId, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 50;

        var em = ps.emission;
        em.rateOverTime = 0f;

        // 2D なので XY 平面の円から外向きに出す
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        shape.rotation = Vector3.zero;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.sortingLayerID = layerId;
        r.sortingOrder = order;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    private static void Burst(ParticleSystem ps, int count)
    {
        var em = ps.emission;
        em.enabled = true;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void Drag(ParticleSystem ps, float drag)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 1000f;
        limit.drag = drag;
    }

    /// <summary>from → to の色で、fadeOutStart から消えていくグラデーション。</summary>
    private static ParticleSystem.MinMaxGradient Fade(Color from, Color to, float fadeOutStart)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    /// <summary>素材の加算合成版 (火の玉用)。素材ごとに 1 つ作って使い回す。</summary>
    private static Material Additive(Material source)
    {
        if (s_additive != null && s_additiveSource == source)
            return s_additive;
        s_additiveSource = source;
        s_additive = new Material(source) { name = source.name + " (Additive)" };
        s_additive.SetFloat("_Blend", 2f);
        s_additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        s_additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        return s_additive;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_additive = null;
        s_additiveSource = null;
    }

    #endregion
}
