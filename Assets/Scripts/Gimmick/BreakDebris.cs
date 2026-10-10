using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 壊れた時に飛び散る破片 (見た目だけ。PendingChanges 36)。
/// 破片は飛び出して回転しながら重力で落ち、Ground レイヤーの床で 1〜2 回はねて、約 1〜1.4 秒で消える。
/// プレイヤー・敵とは当たらない (Collider/Rigidbody を持たず、床だけを線分キャストで調べる自前の簡易物理)。
/// 破片の GameObject は使い回す (DontDestroyOnLoad の置き場にプール)。
/// 呼び出し: <see cref="BreakableBox"/> (木箱 = 木の破片 / 隠し壁 = 岩の破片) と <see cref="SkillBreakable"/> (能力ブロック)。
/// </summary>
public static class BreakDebris
{
    public enum Kind
    {
        Wood,
        Rock,
        Slam,
        Rush,
        Super,
    }

    private static BreakDebrisCatalog s_catalog;
    private static bool s_catalogLoaded;
    private static Transform s_root;
    private static readonly Stack<DebrisPiece> s_pool = new();

    private static BreakDebrisCatalog Catalog
    {
        get
        {
            if (!s_catalogLoaded)
            {
                s_catalog = Resources.Load<BreakDebrisCatalog>(BreakDebrisCatalog.ResourcePath);
                s_catalogLoaded = true;
            }

            return s_catalog;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_catalog = null;
        s_catalogLoaded = false;
        s_root = null;
        s_pool.Clear();
    }

    /// <summary>
    /// 破片を飛ばす。source = 壊れた物の SpriteRenderer (位置・大きさ・描画順・色の基準)。
    /// away = 飛ばす向きの偏り (攻撃した側から離れる向き。zero なら上へ均等)。
    /// 絵が無ければ何もせず false (呼び出し側は従来の縮小フェードにする)。
    /// tintOverride = 種類ごとの色の代わりに使う色、countOverride = 破片の数 (0 なら種類ごとの既定)。
    /// </summary>
    public static bool Burst(SpriteRenderer source, Kind kind, Vector2 away, Color? tintOverride = null,
        int countOverride = 0)
    {
        var catalog = Catalog;
        if (catalog == null || source == null)
            return false;

        var sprites = kind switch
        {
            Kind.Wood => catalog.Wood,
            Kind.Slam => catalog.Slam,
            _ => catalog.Rock,
        };
        if (sprites == null || sprites.Length == 0)
            return false;

        var tint = kind switch
        {
            Kind.Rock => catalog.WallTint,
            Kind.Rush => catalog.RushTint,
            Kind.Super => catalog.SuperTint,
            _ => Color.white,
        };
        // 木箱は箱の色 (CrateColor) も掛ける。被弾フラッシュ中の白は使わない
        if (kind == Kind.Wood && source.TryGetComponent<BreakableBox>(out var box))
            tint *= box.BaseColor;
        if (tintOverride.HasValue)
            tint = tintOverride.Value;

        var bounds = source.bounds;
        var center = (Vector2)bounds.center;
        var size = Mathf.Max(0.5f, Mathf.Min(bounds.size.x, bounds.size.y));
        var count = countOverride > 0 ? countOverride : kind == Kind.Wood ? catalog.CrateCount : catalog.CellCount;
        var order = Mathf.Max(source.sortingOrder + 1, 4);
        var awayDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero;

        // 同じ絵が続かないよう、絵の並びを回しながら使う
        var first = Random.Range(0, sprites.Length);
        for (var i = 0; i < count; i++)
        {
            var sprite = sprites[(first + i) % sprites.Length];
            if (sprite == null)
                continue;

            var piece = Rent();
            var offset = new Vector2(Random.Range(-0.32f, 0.32f), Random.Range(-0.3f, 0.3f)) * size;
            // 中心から外へ + 上へ。攻撃された側から離れる向きに少し寄せる
            var velocity = new Vector2(offset.x * 4.5f + Random.Range(-1.6f, 1.6f), Random.Range(3.8f, 7f))
                           + awayDir * Random.Range(1.5f, 3f);
            var scale = Random.Range(0.55f, 0.8f) * size;
            piece.Launch(sprite, center + offset, velocity, Random.Range(-1f, 1f) * Random.Range(360f, 900f), scale,
                tint, source.sortingLayerID, order, Random.Range(1.0f, 1.4f));
        }

        return true;
    }

    private static DebrisPiece Rent()
    {
        while (s_pool.Count > 0)
        {
            var p = s_pool.Pop();
            if (p != null)
                return p;
        }

        if (s_root == null)
        {
            var root = new GameObject("[BreakDebris]");
            Object.DontDestroyOnLoad(root);
            s_root = root.transform;
        }

        var go = new GameObject("Debris");
        go.transform.SetParent(s_root, false);
        go.AddComponent<SpriteRenderer>();
        return go.AddComponent<DebrisPiece>();
    }

    internal static void Return(DebrisPiece piece)
    {
        piece.gameObject.SetActive(false);
        s_pool.Push(piece);
    }
}
