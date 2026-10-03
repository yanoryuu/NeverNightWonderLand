using UnityEngine;

/// <summary>
/// 敵を倒した時の糸を、その場に散らばる粒 (<see cref="ThreadPiece"/>) にする (参考: ホロウナイトのジオ)。
/// <see cref="EnemyController.ThreadDropped"/> を購読し、量に応じた数の粒を上・外向きに飛ばす。
/// 粒は地面で弾んで止まり、触れる/近づくと拾える (糸の加算は拾った時)。
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

    private void OnEnable()
    {
        EnemyController.ThreadDropped += OnThreadDropped;
    }

    private void OnDisable()
    {
        EnemyController.ThreadDropped -= OnThreadDropped;
    }

    private void OnThreadDropped(Vector2 position, int amount, float size)
    {
        if (amount > 0)
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
}
