using System.Collections;
using UnityEngine;

/// <summary>
/// 敵を倒した時の糸を、その場に散らばる粒 (<see cref="ThreadPiece"/>) にする (参考: ホロウナイトのジオ)。
/// <see cref="EnemyController.ThreadDropped"/> を購読し、量に応じた数の粒を上・外向きに飛ばす。
/// 粒は地面で弾んで止まり、触れる/近づくと拾える (糸の加算は拾った時)。
/// ボス (<see cref="EnemyController.LootArea"/> がある) は粒を増やし、闘技場いっぱいへ数フレームに分けて噴き上げる
/// (ホロウナイトの大きなジオの噴出。2026-10-04 PendingChanges 25)。普通の敵は従来どおり。
/// プレイヤー (Player.prefab) に付ける。粒はアクティブなシーン (= ステージ) に生成されるので、シーンが変わると消える。
/// </summary>
public class ThreadDropSpawner : MonoBehaviour
{
    [Tooltip("糸の粒のプレハブ")]
    [SerializeField] private ThreadPiece _piecePrefab;

    [Tooltip("粒の最大数 (糸がこれより多い時は 1 粒の値を増やす)")]
    [SerializeField] private int _maxPieces = 12;

    [Tooltip("1 粒の基本の大きさ (値 1 の粒の scale)")]
    [SerializeField] private float _pieceScale = 0.38f;

    [Tooltip("値が 1 増えるごとの大きさの増分 (倍率)")]
    [SerializeField] private float _scalePerValue = 0.15f;

    [Tooltip("飛び出す速さ: 横 (± の最大, units/sec)")]
    [SerializeField] private float _burstX = 3.2f;

    [Tooltip("飛び出す速さ: 上 (最小〜最大, units/sec)")]
    [SerializeField] private Vector2 _burstY = new Vector2(5f, 8.5f);

    [Header("ボス (闘技場いっぱいに噴き出す)")]
    [Tooltip("ボスの粒の数 = 糸 × これ (最小〜最大で丸める)。20 → 12, 25 → 15, 100 → 30")]
    [SerializeField] private float _bossPiecesPerThread = 0.6f;

    [Tooltip("ボスの粒の数の最小")]
    [SerializeField] private int _bossMinPieces = 12;

    [Tooltip("ボスの粒の数の最大")]
    [SerializeField] private int _bossMaxPieces = 30;

    [Tooltip("ボスの粒の横の速さの上限 (± units/sec)。遠くへ落とす粒はこれを超えないよう高く打ち上げる")]
    [SerializeField] private float _bossMaxSpeedX = 16f;

    [Tooltip("ボスの粒の上の速さ (最小〜最大, units/sec)。闘技場の天井を越えない高さに抑える")]
    [SerializeField] private Vector2 _bossBurstY = new Vector2(8f, 12.5f);

    [Tooltip("ボスの粒を 1 フレームに出す数 (噴水のように少しずつ)")]
    [SerializeField] private int _bossPiecesPerFrame = 3;

    [Tooltip("ボスの粒の落ちる先を闘技場の左右の端からこれだけ内側にする")]
    [SerializeField] private float _bossEdgeMargin = 1.5f;

    private void OnEnable()
    {
        EnemyController.ThreadDropped += OnThreadDropped;
    }

    private void OnDisable()
    {
        EnemyController.ThreadDropped -= OnThreadDropped;
    }

    private void OnThreadDropped(Vector2 position, int amount, float size, Rect area)
    {
        if (amount <= 0)
            return;
        if (area.width > 0f && area.height > 0f && _piecePrefab != null && isActiveAndEnabled)
            StartCoroutine(BossFountain(position, amount, area));
        else
            Scatter(position, amount, size);
    }

    /// <summary>
    /// amount の糸を粒にして position から散らす。size = 落とした敵の大きさ (ボスは 2) で、大きいほど広く飛ぶ。
    /// 粒の数 = min(amount, _maxPieces)、値は均等に分けて余りを先頭の粒に足す。
    /// </summary>
    public void Scatter(Vector2 position, int amount, float size = 1f)
    {
        if (_piecePrefab == null)
        {
            // プレハブが無い時は従来どおりその場で加算 (粒にできない)
            if (TryGetComponent<PlayerItemInventory>(out var inventory))
                inventory.AddThread(amount, notify: false);
            return;
        }

        var count = Mathf.Clamp(amount, 1, Mathf.Max(1, _maxPieces));
        var baseValue = amount / count;
        var remainder = amount % count;
        var spread = Mathf.Max(1f, size) * (1f + 0.04f * count);
        var origin = LootPiece.FreeSpawnPoint(position + Vector2.up * 0.3f);

        for (var i = 0; i < count; i++)
        {
            var value = baseValue + (i < remainder ? 1 : 0);
            var piece = Instantiate(_piecePrefab, origin + Random.insideUnitCircle * 0.15f, Quaternion.identity);
            piece.Value = value;
            piece.transform.localScale = Vector3.one * (_pieceScale * (1f + _scalePerValue * (value - 1)));

            // 横は左右に均等に散らし (少しランダム)、上は速さに幅を持たせる
            var t = count == 1 ? 0f : i / (count - 1f) * 2f - 1f;
            var vx = (t * 0.8f + Random.Range(-0.35f, 0.35f)) * _burstX * spread;
            var vy = Random.Range(_burstY.x, _burstY.y) * Mathf.Lerp(1f, 1.2f, Mathf.Clamp01(size - 1f));
            piece.Launch(new Vector2(vx, vy));
        }
    }

    /// <summary>
    /// ボスの糸: 粒を増やし、闘技場 area (ワールド座標) の端から端までの落ちる先を均等に割り振って、
    /// そこへ届く速さで数フレームに分けて噴き上げる。粒には area を渡し、壁際で跳ね返る/天井を越えないようにする。
    /// </summary>
    private IEnumerator BossFountain(Vector2 position, int amount, Rect area)
    {
        var count = Mathf.Clamp(Mathf.RoundToInt(amount * _bossPiecesPerThread), Mathf.Max(1, _bossMinPieces), Mathf.Max(1, _bossMaxPieces));
        count = Mathf.Min(count, amount);
        var baseValue = amount / count;
        var remainder = amount % count;

        var groundMask = 1 << 10;
        var origin = LootPiece.FreeSpawnPoint(position + Vector2.up * 0.3f);
        origin.x = Mathf.Clamp(origin.x, area.xMin + 0.5f, area.xMax - 0.5f);
        origin.y = Mathf.Clamp(origin.y, area.yMin + 0.3f, area.yMax - 1f);

        var rbPrefab = _piecePrefab.GetComponent<Rigidbody2D>();
        var g = Mathf.Max(1f, -Physics2D.gravity.y * (rbPrefab != null ? rbPrefab.gravityScale : 1f));
        // 天井 (area の上端) の少し下までしか上がらない速さ
        var maxVy = Mathf.Sqrt(2f * g * Mathf.Max(0.5f, area.yMax - 1.2f - origin.y));

        // 落ちる先: 左右の端から端までを count 等分し、順番は混ぜる (左右交互に噴き出す)
        var xMin = area.xMin + _bossEdgeMargin;
        var xMax = area.xMax - _bossEdgeMargin;
        var targets = new float[count];
        for (var i = 0; i < count; i++)
            targets[i] = Mathf.Lerp(xMin, xMax, (i + Random.Range(0.15f, 0.85f)) / count);
        for (var i = count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (targets[i], targets[j]) = (targets[j], targets[i]);
        }

        var perFrame = Mathf.Max(1, _bossPiecesPerFrame);
        for (var i = 0; i < count; i++)
        {
            if (i > 0 && i % perFrame == 0)
                yield return null;

            var value = baseValue + (i < remainder ? 1 : 0);
            var piece = Instantiate(_piecePrefab, origin + Random.insideUnitCircle * 0.2f, Quaternion.identity);
            piece.Value = value;
            piece.transform.localScale = Vector3.one * (_pieceScale * (1f + _scalePerValue * (value - 1)));
            piece.SetBounds(area);

            // 落ちる先の床の高さ (無ければ闘技場の下端)
            var tx = targets[i];
            var floorY = area.yMin;
            var hit = Physics2D.Raycast(new Vector2(tx, origin.y), Vector2.down, origin.y - area.yMin + 1f, groundMask);
            if (hit.collider != null)
                floorY = hit.point.y;

            // 遠くへ落ちる粒は横の速さの上限を超えないよう高く打ち上げる (滞空を延ばす。天井の高さまで)
            var dx = Mathf.Abs(tx - origin.x) * 1.08f; // 1.08 = 空気抵抗 (0.2) の分
            var vy = Mathf.Min(Random.Range(_bossBurstY.x, _bossBurstY.y), maxVy);
            var flight = FlightTime(vy, origin.y, floorY, g);
            while (vy < maxVy && dx / Mathf.Max(0.2f, flight) > _bossMaxSpeedX)
            {
                vy = Mathf.Min(vy + 0.5f, maxVy);
                flight = FlightTime(vy, origin.y, floorY, g);
            }
            var vx = Mathf.Clamp(Mathf.Sign(tx - origin.x) * dx / Mathf.Max(0.2f, flight), -_bossMaxSpeedX, _bossMaxSpeedX);
            piece.Launch(new Vector2(vx, vy));
        }
    }

    /// <summary>上へ vy で打ち上げて y0 から floorY に落ちるまでの時間 (重力 g)。</summary>
    private static float FlightTime(float vy, float y0, float floorY, float g)
    {
        var apex = y0 + vy * vy / (2f * g);
        return vy / g + Mathf.Sqrt(2f * Mathf.Max(0f, apex - floorY) / g);
    }
}
