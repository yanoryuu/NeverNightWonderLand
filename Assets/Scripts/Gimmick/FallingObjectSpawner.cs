using UnityEngine;

/// <summary>
/// 上から物 (荷物など) を一定間隔で落とすスポナー。スポナーの位置 (高さ) から、
/// 横方向に [_minX, _maxX] (スポナーからのオフセット) の範囲のランダムな位置で生成する。
/// 生成物は _prefab (任意) か、_sprite から自動で作る四角。どちらにも FallingObject が付き、
/// プレイヤーに当たればダメージ、地面に当たるか寿命で消える。
/// スポナーは天井 (岩) の中ではなく、空中 (天井のすぐ下) に置くこと。
/// </summary>
public class FallingObjectSpawner : MonoBehaviour
{
    [Header("生成物")]
    [Tooltip("落とすプレハブ (任意)。未設定なら _sprite から生成する。FallingObject は自動で付く")]
    [SerializeField] private GameObject _prefab;

    [Tooltip("_prefab 未設定時に使うスプライト")]
    [SerializeField] private Sprite _sprite;

    [Tooltip("_prefab 未設定時の色")]
    [SerializeField] private Color _color = new Color(0.75f, 0.55f, 0.35f);

    [Tooltip("当たり判定の大きさ (units)。_prefab 未設定時は見た目もこの大きさにする")]
    [SerializeField] private Vector2 _size = new Vector2(1f, 1f);

    [Tooltip("_prefab 未設定時の描画順")]
    [SerializeField] private int _sortingOrder = 5;

    [Header("タイミング")]
    [Tooltip("生成の間隔 (sec)")]
    [SerializeField] private float _interval = 2f;

    [Tooltip("間隔のばらつき (± sec)")]
    [SerializeField] private float _intervalJitter = 0.5f;

    [Tooltip("最初の生成までの遅延 (sec)")]
    [SerializeField] private float _startDelay = 1f;

    [Tooltip("生成範囲の左端 (スポナーからの X オフセット)")]
    [SerializeField] private float _minX = -3f;

    [Tooltip("生成範囲の右端 (スポナーからの X オフセット)")]
    [SerializeField] private float _maxX = 3f;

    [Tooltip("プレイヤーとの水平距離がこれ以下の時だけ生成する (0 なら常に)")]
    [SerializeField] private float _activationDistance = 20f;

    [Header("落下")]
    [Tooltip("与えるダメージ (HP)")]
    [SerializeField] private int _damage = 1;

    [Tooltip("落ち始める前に震える時間 (予兆, sec)")]
    [SerializeField] private float _warningTime = 0.5f;

    [Tooltip("初速 (units/sec, 下向き)")]
    [SerializeField] private float _initialSpeed = 0f;

    [Tooltip("重力加速度 (units/sec²)")]
    [SerializeField] private float _gravity = 30f;

    [Tooltip("最大落下速度 (units/sec)")]
    [SerializeField] private float _maxFallSpeed = 18f;

    [Tooltip("寿命 (sec)。地面に当たらなくてもこの時間で消える")]
    [SerializeField] private float _lifetime = 6f;

    [Tooltip("当たると消える地面のレイヤー (既定: Default と Ground)")]
    [SerializeField] private LayerMask _groundMask = (1 << 0) | (1 << 10);

    [Tooltip("このフラグ (GameProgress) が立ったら生成をやめる。空なら止まらない")]
    [SerializeField] private string _stopFlag = "";

    private float _timer;
    private Transform _player;

    private void Start()
    {
        _timer = _startDelay;
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _player = player.transform;
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(_stopFlag) && GameProgress.Has(_stopFlag))
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = Mathf.Max(0.1f, _interval + Random.Range(-_intervalJitter, _intervalJitter));

        if (_activationDistance > 0f && _player != null
            && Mathf.Abs(_player.position.x - transform.position.x) > _activationDistance)
            return;

        Spawn();
    }

    private void Spawn()
    {
        var pos = transform.position + new Vector3(Random.Range(_minX, _maxX), 0f, 0f);

        GameObject go;
        if (_prefab != null)
        {
            go = Instantiate(_prefab, pos, Quaternion.identity);
        }
        else
        {
            go = new GameObject("FallingObject");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = _color;
            sr.sortingOrder = _sortingOrder;

            // スプライトの大きさを当たり判定の大きさへ合わせる
            if (_sprite != null)
            {
                var spriteSize = _sprite.bounds.size;
                if (spriteSize.x > 0f && spriteSize.y > 0f)
                    go.transform.localScale = new Vector3(_size.x / spriteSize.x, _size.y / spriteSize.y, 1f);
            }
        }

        var falling = go.GetComponent<FallingObject>();
        if (falling == null)
            falling = go.AddComponent<FallingObject>();

        falling.Init(_size, _damage, _initialSpeed, _gravity, _maxFallSpeed, _lifetime, _warningTime, _groundMask);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        var p = transform.position;
        Gizmos.DrawLine(p + new Vector3(_minX, 0f), p + new Vector3(_maxX, 0f));
        Gizmos.DrawWireCube(p + new Vector3(_minX, 0f), _size);
        Gizmos.DrawWireCube(p + new Vector3(_maxX, 0f), _size);
    }
}
