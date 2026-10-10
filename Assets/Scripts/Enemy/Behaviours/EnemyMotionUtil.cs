using UnityEngine;

/// <summary>
/// うごき部品が共通で使う地形判定ヘルパー (接地・足場の端・視線・真下の地面)。
/// 地面レイヤーは部品の SerializeField で指定し、未設定 (0) なら "Ground" を使う
/// (EnemyPerception と同じ既定)。
/// </summary>
public static class EnemyMotionUtil
{
    private static readonly RaycastHit2D[] s_hits = new RaycastHit2D[8];
    private static readonly Collider2D[] s_cols = new Collider2D[8];

    /// <summary>未設定 (0) なら Ground レイヤーを返す。</summary>
    public static LayerMask GroundOrDefault(LayerMask mask) =>
        mask.value != 0 ? mask : (LayerMask)LayerMask.GetMask("Ground");

    private static ContactFilter2D Filter(LayerMask mask) => new ContactFilter2D
    {
        useLayerMask = true,
        layerMask = mask,
        useTriggers = false, // ゾーン等のトリガーを地面と誤認しない
    };

    /// <summary>すり抜け床 (PlatformEffector2D の一方通行) か。弾・視線はこれを無視する。</summary>
    public static bool IsOneWayPlatform(Collider2D col)
    {
        if (col == null || !col.usedByEffector)
            return false;
        var effector = col.GetComponent<PlatformEffector2D>();
        return effector != null && effector.useOneWay;
    }

    /// <summary>体の足元のすぐ下に地面 (すり抜け床を含む) があるか。</summary>
    public static bool IsGrounded(Collider2D body, LayerMask ground)
    {
        if (body == null)
            return false;

        var b = body.bounds;
        var center = new Vector2(b.center.x, b.min.y - 0.05f);
        var size = new Vector2(b.size.x * 0.8f, 0.1f);
        return Physics2D.OverlapBox(center, size, 0f, Filter(ground), s_cols) > 0;
    }

    /// <summary>
    /// 進行方向 (dir = ±1) の少し先の足元に地面があるか (足場の端の検知)。
    /// 体の前端から ahead だけ先で、足元から depth だけ下まで調べる。
    /// </summary>
    public static bool HasGroundAhead(Collider2D body, int dir, LayerMask ground, float ahead = 0.15f, float depth = 0.6f)
    {
        if (body == null || dir == 0)
            return true;

        var b = body.bounds;
        var x = dir > 0 ? b.max.x + ahead : b.min.x - ahead;
        var origin = new Vector2(x, b.min.y + 0.1f);
        return Physics2D.Raycast(origin, Vector2.down, Filter(ground), s_hits, 0.1f + depth) > 0;
    }

    /// <summary>from から to へ地形 (すり抜け床は除く) に遮られていないか。</summary>
    public static bool HasLineOfSight(Vector2 from, Vector2 to, LayerMask ground)
    {
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        var count = Physics2D.Raycast(from, delta / distance, Filter(ground), s_hits, distance);
        for (var i = 0; i < count; i++)
        {
            if (!IsOneWayPlatform(s_hits[i].collider))
                return false;
        }

        return true;
    }

    /// <summary>from の真下 maxDistance 以内の地面 (すり抜け床を含む) の高さを返す。</summary>
    public static bool TryGetGroundBelow(Vector2 from, float maxDistance, LayerMask ground, out float groundY)
    {
        var count = Physics2D.Raycast(from, Vector2.down, Filter(ground), s_hits, maxDistance);
        if (count > 0)
        {
            // Raycast の結果は距離順
            groundY = s_hits[0].point.y;
            return true;
        }

        groundY = from.y;
        return false;
    }

    /// <summary>
    /// 位置を変えずに向きだけ変える (EnemyController.Move の向き反転を微小速度で使う)。
    /// 直後に Move(0) などで速度を上書きしてよい。
    /// </summary>
    public static void FaceToward(EnemyController enemy, int dir)
    {
        if (enemy != null && dir != 0)
            enemy.Move(dir * 0.02f);
    }
}

/// <summary>
/// 部品からプレイヤーへ直接ダメージを与えるためのヘルパー (突進・踏みつけ・弾)。
/// 敵とプレイヤーは物理衝突しないため、EnemyController.CheckContactDamage と同じく
/// コライダーの重なり (Bounds) で判定し、PlayerHealth.TakeDamage に DamageInfo を渡す。
/// プレイヤーは被弾後に無敵時間が付くので、同じ物理ステップでコアの接触ダメージ (1) より先に
/// 部品がこれで大きいダメージを入れれば、接触ダメージは無敵で弾かれ「突進中だけ 2」になる。
/// </summary>
public sealed class PlayerHitProbe
{
    private Transform _player;
    private PlayerHealth _health;
    private Collider2D _collider;

    private bool Resolve(Transform player)
    {
        if (player == null)
            return false;

        if (_player != player)
        {
            _player = player;
            _health = player.GetComponent<PlayerHealth>();
            _collider = player.GetComponent<Collider2D>();
        }

        return _health != null && _collider != null;
    }

    /// <summary>プレイヤーの体の中心 (狙い用)。コライダーが無ければ Transform の位置。</summary>
    public Vector2 GetCenter(Transform player)
    {
        if (Resolve(player))
            return _collider.bounds.center;
        return player != null ? (Vector2)player.position : Vector2.zero;
    }

    /// <summary>判定範囲がプレイヤーの体と重なっていればダメージを与える。重なったら true。</summary>
    public bool TryHit(Transform player, Bounds area, int damage, Vector2 hitPoint, GameObject source)
    {
        if (damage <= 0 || !Resolve(player))
            return false;

        if (!area.Intersects(_collider.bounds))
            return false;

        _health.TakeDamage(new DamageInfo(damage, 0, hitPoint, source));
        return true;
    }
}

/// <summary>
/// 予備動作の点滅 (色の切り替え)。専用の絵が無くても「来るぞ」が伝わる最小限の表示。
/// 止めた時は「自分が付けた色のままなら」元の色に戻す (被弾フラッシュやブレイク色など、
/// EnemyController が先に色を変えていた場合はそちらを尊重して触らない)。
/// </summary>
public sealed class EnemyTelegraphBlink
{
    private readonly SpriteRenderer _renderer;
    private readonly Color _blinkColor;
    private readonly float _interval;

    private Color _savedColor;
    private Color _lastApplied;
    private float _timer;
    private bool _on;

    public bool IsBlinking { get; private set; }

    public EnemyTelegraphBlink(SpriteRenderer renderer, Color blinkColor, float interval)
    {
        _renderer = renderer;
        _blinkColor = blinkColor;
        _interval = Mathf.Max(0.02f, interval);
    }

    /// <summary>
    /// 点滅させる SpriteRenderer を決める: 指定があればそれ、無ければ
    /// 子 "Visual" → 本体の絵 (スプライトあり・有効) → 子のスプライトありの最初の 1 つ。
    /// </summary>
    public static SpriteRenderer ResolveVisual(Component owner, SpriteRenderer explicitRenderer)
    {
        if (explicitRenderer != null)
            return explicitRenderer;

        // 絵を子 "Visual" に置く敵 (野良ウマ = EnemyController._visual と同じ) を優先
        var visual = owner.transform.Find("Visual");
        if (visual != null && visual.TryGetComponent<SpriteRenderer>(out var visualRenderer))
            return visualRenderer;

        var root = owner.GetComponent<SpriteRenderer>();
        if (root != null && root.sprite != null && root.enabled)
            return root;

        foreach (var sr in owner.GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr.sprite != null)
                return sr;
        }

        return root;
    }

    public void Start()
    {
        if (_renderer == null)
            return;
        if (IsBlinking)
            return;

        IsBlinking = true;
        _savedColor = _renderer.color;
        _timer = 0f;
        _on = false;
        Apply(true);
    }

    public void Tick(float deltaTime)
    {
        if (!IsBlinking || _renderer == null)
            return;

        _timer += deltaTime;
        if (_timer < _interval)
            return;

        _timer = 0f;
        Apply(!_on);
    }

    public void Stop()
    {
        if (!IsBlinking)
            return;

        IsBlinking = false;
        if (_renderer != null && _renderer.color == _lastApplied)
            _renderer.color = _savedColor;
    }

    private void Apply(bool on)
    {
        _on = on;
        _lastApplied = on ? _blinkColor * new Color(1f, 1f, 1f, _savedColor.a) : _savedColor;
        _renderer.color = _lastApplied;
    }
}

/// <summary>実行時に作る簡易スプライト (影・弾の仮表示)。素材が無くても動きが伝わるように。</summary>
public static class EnemyFxSprites
{
    private static Sprite s_softEllipse;
    private static Sprite s_circle;

    /// <summary>横長でふちがぼけた楕円 (幅 1 unit × 高さ 0.25 unit)。着地点の影に使う。</summary>
    public static Sprite SoftEllipse
    {
        get
        {
            if (s_softEllipse == null)
                s_softEllipse = Create(64, 16, 0.35f);
            return s_softEllipse;
        }
    }

    /// <summary>丸 (直径 1 unit)。弾の仮表示に使う。</summary>
    public static Sprite Circle
    {
        get
        {
            if (s_circle == null)
                s_circle = Create(32, 32, 0.1f);
            return s_circle;
        }
    }

    private static Sprite Create(int width, int height, float softness)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "EnemyFx",
        };

        var pixels = new Color32[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var nx = (x + 0.5f) / width * 2f - 1f;
                var ny = (y + 0.5f) / height * 2f - 1f;
                var d = Mathf.Sqrt(nx * nx + ny * ny);
                var a = Mathf.Clamp01((1f - d) / Mathf.Max(0.01f, softness));
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
    }
}
