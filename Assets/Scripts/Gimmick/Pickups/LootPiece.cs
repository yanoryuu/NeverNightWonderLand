using DG.Tweening;
using UnityEngine;

/// <summary>
/// 地面に散らばって、触れる/近づくと拾える小さな物の基底 (敵の糸 <see cref="ThreadPiece"/> / 箱のコイン <see cref="CoinPiece"/>)。
/// - 物理: Rigidbody2D (動的) + 円の当たり。当たるのは Ground レイヤー (10) だけ (プレイヤー・敵・トリガーとは接触しない)。
///   すり抜け床 (PlatformEffector2D) は上から落ちれば乗り、下から飛び上がる時はすり抜ける。
/// - 拾う: 出てから <see cref="_pickupDelay"/> 秒後から、プレイヤーが <see cref="_magnetRadius"/> 以内に来ると吸い寄せられ、
///   <see cref="_collectRadius"/> 以内で取得 (<see cref="OnCollected"/>)。
/// - 消えるのは拾われた時とシーンが変わった時だけ。地形の外へ落ちた (出た位置から <see cref="_lostFallDistance"/> 下) 物は
///   その場で取得扱いにする (取り逃しの事故を防ぐ)。
/// レイヤーは Ignore Raycast (2): プレイヤーの接地判定 (Default+Ground) や攻撃判定 (Enemy+Ground) に掛からない。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class LootPiece : MonoBehaviour
{
    private const int GroundLayer = 10;

    [Tooltip("出てから拾えるようになるまでの時間 (sec)")]
    [SerializeField] private float _pickupDelay = 0.35f;

    [Tooltip("吸い寄せが始まる距離 (プレイヤーの中心から)")]
    [SerializeField] private float _magnetRadius = 1.6f;

    [Tooltip("吸い寄せの速さ (units/sec, 近いほど速くなる)")]
    [SerializeField] private float _magnetSpeed = 9f;

    [Tooltip("取得する距離 (プレイヤーの中心から)")]
    [SerializeField] private float _collectRadius = 0.55f;

    [Tooltip("出た位置からこれだけ下へ落ちたら (地形の外へ抜けたとみなして) その場で取得扱いにする")]
    [SerializeField] private float _lostFallDistance = 40f;

    private static PlayerItemInventory s_player;

    private Rigidbody2D _rb;
    private float _age;
    private float _spawnY;
    private bool _magnet;
    private bool _collected;
    private bool _hasBounds;
    private Rect _bounds;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // 地形 (Ground) とだけ当たる。トリガー (遷移ゾーン・トゲ・水など) とも接触しない
        _rb.excludeLayers = ~(1 << GroundLayer);
        _spawnY = transform.position.y;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>飛び出す速度を与える (生成した側が呼ぶ)。</summary>
    public void Launch(Vector2 velocity)
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
        _rb.linearVelocity = velocity;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f)); // 向きを少しばらす (回転は固定なので転がらない)
        _spawnY = transform.position.y;
    }

    /// <summary>
    /// 飛んでいける範囲 (ワールド座標) を決める (ボスの糸: 闘技場の中)。左右の端では跳ね返り、上の端では落ち始める。
    /// 撃破でゲートが開いた出入口や天井の抜け穴から外へ飛び出さないようにするため。
    /// </summary>
    public void SetBounds(Rect bounds)
    {
        _bounds = bounds;
        _hasBounds = bounds.width > 0f && bounds.height > 0f;
    }

    private void FixedUpdate()
    {
        if (!_hasBounds || _collected || _magnet || _rb == null || !_rb.simulated)
            return;

        var pos = _rb.position;
        var vel = _rb.linearVelocity;
        var r = 0.25f;
        var changed = false;
        if (pos.x < _bounds.xMin + r && vel.x < 0f) { vel.x = -vel.x * 0.5f; pos.x = _bounds.xMin + r; changed = true; }
        else if (pos.x > _bounds.xMax - r && vel.x > 0f) { vel.x = -vel.x * 0.5f; pos.x = _bounds.xMax - r; changed = true; }
        if (pos.y > _bounds.yMax - r && vel.y > 0f) { vel.y = 0f; pos.y = _bounds.yMax - r; changed = true; }
        if (!changed)
            return;
        _rb.position = pos;
        _rb.linearVelocity = vel;
    }

    /// <summary>
    /// 地形の中に生成されないよう、pos が Ground の中なら上へずらした位置を返す (最大 3 単位)。
    /// 合成コライダー (輪郭) の中に生まれると床をすり抜けて落ちるため。
    /// </summary>
    public static Vector2 FreeSpawnPoint(Vector2 pos)
    {
        var mask = 1 << GroundLayer;
        for (var i = 0; i < 12 && Physics2D.OverlapPoint(pos, mask) != null; i++)
            pos.y += 0.25f;
        return pos;
    }

    private void Update()
    {
        if (_collected)
            return;

        _age += Time.deltaTime;

        if (transform.position.y < _spawnY - _lostFallDistance)
        {
            Collect();
            return;
        }

        if (_age < _pickupDelay)
            return;

        var player = Player();
        if (player == null)
            return;

        var target = (Vector2)player.transform.position;
        var pos = (Vector2)transform.position;
        var dist = Vector2.Distance(pos, target);

        if (dist <= _collectRadius)
        {
            Collect();
            return;
        }

        if (!_magnet && dist <= _magnetRadius)
        {
            // 吸い寄せ中は物理を止めて直接動かす (地形に引っかからない)
            _magnet = true;
            _rb.simulated = false;
        }

        if (_magnet)
        {
            var speed = _magnetSpeed * (1f + 2f * Mathf.Clamp01(1f - dist / _magnetRadius));
            transform.position = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
        }
    }

    private void Collect()
    {
        if (_collected)
            return;

        _collected = true;
        if (_rb != null)
            _rb.simulated = false;

        OnCollected(Player());

        // 取った手ごたえ: 一瞬ふくらんで消える
        transform.DOKill();
        transform.DOScale(transform.localScale * 1.6f, 0.08f)
            .OnComplete(() => transform.DOScale(Vector3.zero, 0.1f).OnComplete(() => Destroy(gameObject)));
    }

    /// <summary>取得時の効果 (player はシーンに居なければ null)。</summary>
    protected abstract void OnCollected(PlayerItemInventory player);

    protected static PlayerItemInventory Player()
    {
        if (s_player == null || !s_player.isActiveAndEnabled)
            s_player = FindAnyObjectByType<PlayerItemInventory>();
        return s_player;
    }
}
