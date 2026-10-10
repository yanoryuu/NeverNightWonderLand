using DG.Tweening;
using UnityEngine;

/// <summary>
/// ボス用の小道具 (静的)。
/// - 予告表示 (光・影・印) 用の実行時スプライト (白い四角・白い円) と SpriteRenderer の生成
/// - プレイヤーへの当たり判定 (円・矩形) とダメージ適用 (DamageInfo → PlayerHealth)
/// - 床の高さを調べるレイキャスト
/// 弾やボス本体の攻撃判定はすべてここを通すので、レイヤー衝突設定に依存しない
/// (EnemyController の接触ダメージと同じく、コライダーの重なりで判定する)。
/// </summary>
public static class BossKit
{
    /// <summary>予告表示の既定の描画順 (床や敵より手前に出す)。</summary>
    public const int MarkerSortingOrder = 50;

    private static Sprite s_square;
    private static Sprite s_circle;
    private static PlayerController s_player;
    private static PlayerHealth s_playerHealth;
    private static Collider2D s_playerCollider;

    #region 実行時スプライト

    /// <summary>1×1 ユニットの白い四角。</summary>
    public static Sprite SquareSprite
    {
        get
        {
            if (s_square != null)
                return s_square;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "BossKit_Square",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            s_square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            s_square.name = "BossKit_Square";
            return s_square;
        }
    }

    /// <summary>直径 1 ユニットの白い円 (縁を少しぼかす)。</summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (s_circle != null)
                return s_circle;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "BossKit_Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (size * 0.5f);
                    var a = Mathf.Clamp01((1f - d) * 12f); // 外周 1/12 だけぼかす
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            s_circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s_circle.name = "BossKit_Circle";
            return s_circle;
        }
    }

    /// <summary>
    /// 予告などの表示用 SpriteRenderer を生成する (UI ではなくワールド空間)。
    /// size はワールド単位の幅・高さ (スプライトは 1 ユニット基準)。
    /// </summary>
    public static SpriteRenderer CreateMarker(string name, Sprite sprite, Color color, Vector2 position, Vector2 size,
        int sortingOrder = MarkerSortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>予告表示を消す (DOTween の脈動も止める)。null なら何もしない。</summary>
    public static void DestroyMarker(Component marker)
    {
        if (marker == null)
            return;
        marker.transform.DOKill();
        if (marker.TryGetComponent<SpriteRenderer>(out var sr))
            sr.DOKill();
        Object.Destroy(marker.gameObject);
    }

    #endregion

    #region プレイヤーへの当たり判定

    /// <summary>プレイヤー (シーンに 1 人想定) の体力と当たり判定を取る。不在なら false。</summary>
    public static bool TryGetPlayer(out PlayerHealth health, out Collider2D collider)
    {
        if (s_player == null)
        {
            s_player = Object.FindFirstObjectByType<PlayerController>();
            s_playerHealth = s_player != null ? s_player.GetComponent<PlayerHealth>() : null;
            s_playerCollider = s_player != null ? s_player.GetComponent<Collider2D>() : null;
        }

        health = s_playerHealth;
        collider = s_playerCollider;
        return health != null && collider != null && collider.enabled;
    }

    /// <summary>プレイヤーの体の中心 (当たり判定の中心)。不在なら null。</summary>
    public static Vector2? PlayerCenter()
    {
        if (TryGetPlayer(out _, out var col))
            return col.bounds.center;
        return s_player != null ? (Vector2?)s_player.transform.position : null;
    }

    /// <summary>
    /// 円の範囲にプレイヤーが重なっていればダメージを与える (無敵時間中は PlayerHealth 側で無視される)。
    /// 重なっていたかを返す。ノックバックは center から離れる向き。
    /// </summary>
    public static bool HitPlayerCircle(Vector2 center, float radius, int damage, GameObject source)
    {
        if (!TryGetPlayer(out var health, out var col))
            return false;

        var closest = col.ClosestPoint(center);
        if ((closest - center).sqrMagnitude > radius * radius)
            return false;

        health.TakeDamage(new DamageInfo(damage, 0, center, source));
        return true;
    }

    /// <summary>矩形 (ワールド座標の中心と大きさ) にプレイヤーが重なっていればダメージを与える。</summary>
    public static bool HitPlayerBox(Vector2 center, Vector2 size, int damage, GameObject source)
    {
        if (!TryGetPlayer(out var health, out var col))
            return false;

        var box = new Bounds(center, new Vector3(size.x, size.y, 10f));
        var pb = col.bounds;
        pb.extents = new Vector3(pb.extents.x, pb.extents.y, 5f);
        if (!box.Intersects(pb))
            return false;

        health.TakeDamage(new DamageInfo(damage, 0, center, source));
        return true;
    }

    #endregion

    #region 地形

    /// <summary>レイヤーマスクが空なら "Ground" レイヤーを返す。</summary>
    public static LayerMask GroundMaskOrDefault(LayerMask mask) =>
        mask.value != 0 ? mask : (LayerMask)LayerMask.GetMask("Ground");

    /// <summary>
    /// (x, fromY) から真下へ床を探し、床の表面の高さを返す。見つからなければ fallback。
    /// </summary>
    public static float FindFloorY(float x, float fromY, float maxDistance, LayerMask groundMask, float fallback)
    {
        var hit = Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, maxDistance, groundMask);
        return hit.collider != null ? hit.point.y : fallback;
    }

    #endregion

    // Enter Play Mode Settings で Domain Reload を切っていても、前の再生のプレイヤー参照を持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_player = null;
        s_playerHealth = null;
        s_playerCollider = null;
    }
}
