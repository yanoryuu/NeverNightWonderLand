using UnityEngine;

/// <summary>
/// ボスの弾 (ナイフ・風船)。Rigidbody/Collider は不要で、Transform を速度で動かし、
/// プレイヤーとの重なりを <see cref="BossKit"/> で判定する (レイヤー衝突設定に依存しない)。
/// プレハブ: ルートに SpriteRenderer (絵は右向き) と本コンポーネントを付けるだけ。
/// プレハブ未設定でも <see cref="CreateFallback"/> で実行時に四角/円の仮の弾を作れる。
/// </summary>
public class BossProjectile : BossHazard
{
    [Tooltip("プレイヤーとの当たり判定の半径 (ワールド単位)")]
    [SerializeField] private float _hitRadius = 0.25f;

    [Tooltip("進行方向へ絵を回すか (ナイフ = true、風船 = false)")]
    [SerializeField] private bool _faceVelocity = true;

    private Vector2 _velocity;
    private float _gravity;
    private float _lifetime;
    private bool _destroyOnGround;
    private LayerMask _groundMask;
    private bool _launched;

    // 流れ弾モード (LaunchAt): プレイヤー・地形には当たらず、目標に届いたら onArrive を呼んで消える
    private Vector2 _targetPoint;
    private float _speed;
    private System.Action _onArrive;

    /// <summary>
    /// 発射する。生成直後に呼ぶこと。
    /// </summary>
    /// <param name="velocity">初速</param>
    /// <param name="gravity">下向きの加速度 (units/sec²)。0 で直進</param>
    /// <param name="damage">プレイヤーへのダメージ</param>
    /// <param name="lifetime">寿命 (sec)</param>
    /// <param name="source">発射元 (ボス)</param>
    /// <param name="destroyOnGround">地形に当たったら消えるか</param>
    /// <param name="groundMask">地形のレイヤー</param>
    public void Launch(Vector2 velocity, float gravity, int damage, float lifetime, GameObject source,
        bool destroyOnGround, LayerMask groundMask)
    {
        _velocity = velocity;
        _gravity = gravity;
        Damage = damage;
        _lifetime = lifetime;
        Source = source;
        _destroyOnGround = destroyOnGround;
        _groundMask = groundMask;
        _launched = true;
        FaceVelocity();
    }

    /// <summary>
    /// 流れ弾として目標へまっすぐ飛ばす (演出用)。プレイヤー・地形には当たらず、届いたら (寿命切れでも) onArrive を呼んで消える。
    /// </summary>
    public void LaunchAt(Vector2 targetPoint, float speed, float lifetime, GameObject source, System.Action onArrive)
    {
        _targetPoint = targetPoint;
        _speed = speed;
        _onArrive = onArrive;
        _lifetime = lifetime;
        Source = source;
        Damage = 0;
        _velocity = (targetPoint - (Vector2)transform.position).normalized * speed;
        _launched = true;
        FaceVelocity();
    }

    /// <summary>当たり判定の半径を上書きする (実行時生成の仮の弾用)。</summary>
    public void SetHitRadius(float radius) => _hitRadius = radius;

    private void Update()
    {
        if (!_launched)
            return;

        var dt = Time.deltaTime;
        if (_onArrive != null)
        {
            UpdateStray(dt);
            return;
        }

        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _velocity.y -= _gravity * dt;
        transform.position += (Vector3)(_velocity * dt);
        FaceVelocity();

        var pos = (Vector2)transform.position;
        if (BossKit.HitPlayerCircle(pos, _hitRadius, Damage, Source))
        {
            Destroy(gameObject);
            return;
        }

        if (_destroyOnGround && Physics2D.OverlapCircle(pos, _hitRadius * 0.5f, _groundMask) != null)
            Destroy(gameObject);
    }

    private void UpdateStray(float dt)
    {
        _lifetime -= dt;
        var pos = (Vector2)transform.position;
        var toTarget = _targetPoint - pos;
        var step = _speed * dt;
        if (toTarget.magnitude <= step || _lifetime <= 0f)
        {
            transform.position = _targetPoint;
            Arrive();
            return;
        }

        _velocity = toTarget.normalized * _speed;
        transform.position += (Vector3)(_velocity * dt);
        FaceVelocity();
    }

    private void Arrive()
    {
        var onArrive = _onArrive;
        _onArrive = null;
        _launched = false;
        onArrive?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>弾消しでは流れ弾は消さない (撤退の演出で必ず届かせる)。</summary>
    public override void Clear()
    {
        if (_onArrive != null)
            return;
        base.Clear();
    }

    private void FaceVelocity()
    {
        if (_faceVelocity && _velocity.sqrMagnitude > 0.01f)
            transform.right = _velocity.normalized;
    }

    /// <summary>
    /// プレハブが無い時の仮の弾を実行時に作る (テスト用)。size はワールド単位の見た目の大きさ。
    /// </summary>
    public static BossProjectile CreateFallback(string name, Sprite sprite, Color color, Vector2 size,
        float hitRadius, bool faceVelocity)
    {
        var sr = BossKit.CreateMarker(name, sprite, color, Vector2.zero, size, BossKit.MarkerSortingOrder + 1);
        var projectile = sr.gameObject.AddComponent<BossProjectile>();
        projectile._hitRadius = hitRadius;
        projectile._faceVelocity = faceVelocity;
        return projectile;
    }
}
