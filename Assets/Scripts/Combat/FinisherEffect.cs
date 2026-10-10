using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 裁断が決まった瞬間の演出 (ICEY の処刑演出が参考)。すべて実時間で進むのでヒットストップ中も動く。
/// - 白フラッシュ (全画面) と周囲の暗転 (プレイヤーと標的だけ明るいまま残す)
/// - 標的を貫く白い斬撃線
/// - 倒した時は標的のスプライトを斬撃線で 2 つに切り分け、ずらしながら消す
/// - 画面揺れ (CameraShake) とパッドの振動
/// 素材は使わずコードで描く (斬撃線は先細りのテクスチャを実行時に作る)。見た目だけでゲーム進行には影響しない。
/// </summary>
public static class FinisherEffect
{
    // 暗転・斬撃線・フラッシュを手前に出すため、演出中だけプレイヤーと標的の描画順をここまで持ち上げる
    private const int HighlightOrder = 200;
    private const int DimOrder = HighlightOrder - 10;
    private const int SlashOrder = HighlightOrder + 20;
    private const int FlashOrder = HighlightOrder + 30;

    private const float FlashAlpha = 0.85f;
    private const float FlashTime = 0.1f;
    private const float DimAlpha = 0.78f;
    private const float DimHoldTime = 0.28f;
    private const float DimFadeTime = 0.22f;
    private const float SlashGrowTime = 0.05f;
    private const float SlashHoldTime = 0.1f;
    private const float SlashFadeTime = 0.2f;
    private const float SlashLength = 9f;
    private const float SlashThickness = 0.22f;
    private const float SlashAngle = 18f;         // 水平からの傾き (度)。振り下ろす向きに合わせて左右反転する
    private const float SplitHoldTime = 0.12f;    // 割れる前に一瞬くっついたまま見せる
    private const float SplitTime = 0.55f;
    private const float SplitSlide = 0.9f;        // 斬撃線に沿ってずれる距離
    private const float SplitDrop = 0.6f;         // 下の片が落ちる距離
    private const float RumbleLow = 0.5f;
    private const float RumbleHigh = 1f;
    private const float RumbleTime = 0.22f;

    private static Runner _runner;
    private static Coroutine _rumbleStop;
    private static Sprite _whiteSprite;
    private static Sprite _slashSprite;

    /// <summary>
    /// 演出を出す。killed なら標的を切り分けて見せる (呼び出し側は標的の見た目を隠してよい)。
    /// 戻り値は切り分けの片を出したか (出せない素材の時は false。呼び出し側は通常の消え方にする)。
    /// </summary>
    public static bool Play(PlayerController player, SpriteRenderer target, Vector2 hitPoint, bool killed,
        float shakeAmplitude, float shakeTime)
    {
        EnsureRunner();
        CameraShake.Shake(shakeAmplitude, shakeTime);
        Rumble();

        var facing = player != null ? player.Facing : 1;
        // 振り下ろし: 前方が下がる斜めの線
        var angle = -SlashAngle * facing;

        var split = killed && target != null && CanSplit(target);
        _runner.StartCoroutine(Sequence(player, target, hitPoint, angle, split));
        return split;
    }

    private static IEnumerator Sequence(PlayerController player, SpriteRenderer target, Vector2 hitPoint,
        float angle, bool split)
    {
        var cam = Camera.main;
        var restore = new List<(SpriteRenderer renderer, int order)>();
        Highlight(player != null ? player.GetComponentsInChildren<SpriteRenderer>() : null, HighlightOrder, restore);
        if (!split && target != null)
            Highlight(target.GetComponentsInChildren<SpriteRenderer>(), HighlightOrder + 1, restore);

        var dim = CreateScreenQuad("[FinisherDim]", Color.black, DimOrder, cam);
        var flash = CreateScreenQuad("[FinisherFlash]", Color.white, FlashOrder, cam);
        var slash = CreateSlash(hitPoint, angle);
        var pieces = split ? SplitSprite(target, hitPoint, angle) : null;

        var elapsed = 0f;
        var total = Mathf.Max(DimHoldTime + DimFadeTime, SplitHoldTime + SplitTime,
            SlashGrowTime + SlashHoldTime + SlashFadeTime);
        while (elapsed < total)
        {
            // ポーズメニュー中は止める (実時間で進むので、開いている間に演出が終わらないように)
            if (!GamePause.IsPaused)
                elapsed += Time.unscaledDeltaTime;

            FollowCamera(dim, cam);
            FollowCamera(flash, cam);

            SetAlpha(flash, FlashAlpha * (1f - Mathf.Clamp01(elapsed / FlashTime)));
            var dimK = elapsed < DimHoldTime ? 1f : 1f - Mathf.Clamp01((elapsed - DimHoldTime) / DimFadeTime);
            SetAlpha(dim, DimAlpha * dimK);

            UpdateSlash(slash, elapsed);
            if (pieces != null)
                UpdatePieces(pieces, elapsed, angle);

            yield return null;
        }

        foreach (var (renderer, order) in restore)
            if (renderer != null)
                renderer.sortingOrder = order;

        Object.Destroy(dim.gameObject);
        Object.Destroy(flash.gameObject);
        Object.Destroy(slash.gameObject);
        if (pieces != null)
            foreach (var p in pieces)
            {
                if (p.Renderer != null)
                    Object.Destroy(p.Renderer.gameObject);
                Object.Destroy(p.Mesh);
            }
    }

    #region 描画順の持ち上げ

    private static void Highlight(SpriteRenderer[] renderers, int baseOrder,
        List<(SpriteRenderer, int)> restore)
    {
        if (renderers == null)
            return;
        // 子同士の前後関係 (本体と装備など) は保ったまま持ち上げる
        foreach (var r in renderers)
        {
            restore.Add((r, r.sortingOrder));
            r.sortingOrder = baseOrder + Mathf.Clamp(r.sortingOrder, -5, 5);
        }
    }

    #endregion

    #region 全画面の板 (暗転・フラッシュ)

    private static SpriteRenderer CreateScreenQuad(string name, Color color, int order, Camera cam)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_runner.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = new Color(color.r, color.g, color.b, 0f);
        sr.sortingOrder = order;
        FollowCamera(sr, cam);
        return sr;
    }

    private static void FollowCamera(SpriteRenderer quad, Camera cam)
    {
        if (cam == null)
            return;
        var p = cam.transform.position;
        quad.transform.position = new Vector3(p.x, p.y, 0f);
        // 画面揺れの分も覆えるよう少し大きめにする
        var h = cam.orthographic ? cam.orthographicSize * 2f + 2f : 40f;
        quad.transform.localScale = new Vector3(h * cam.aspect + 2f, h, 1f);
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        var c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    #endregion

    #region 斬撃線

    private static SpriteRenderer CreateSlash(Vector2 hitPoint, float angle)
    {
        var go = new GameObject("[FinisherSlash]");
        go.transform.SetParent(_runner.transform, false);
        go.transform.SetPositionAndRotation(hitPoint, Quaternion.Euler(0f, 0f, angle));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SlashSprite();
        sr.sortingOrder = SlashOrder;
        go.transform.localScale = Vector3.zero;
        return sr;
    }

    private static void UpdateSlash(SpriteRenderer slash, float t)
    {
        // 一気に伸びて、少し残ってから細くなって消える
        var grow = Mathf.Clamp01(t / SlashGrowTime);
        var fade = Mathf.Clamp01((t - SlashGrowTime - SlashHoldTime) / SlashFadeTime);
        var length = SlashLength * (1f - (1f - grow) * (1f - grow));
        var size = slash.sprite.bounds.size;
        slash.transform.localScale = new Vector3(length / size.x, SlashThickness * (1f - fade) / size.y, 1f);
        SetAlpha(slash, 1f - fade * fade);
    }

    #endregion

    #region 切り分け

    private sealed class Piece
    {
        public MeshRenderer Renderer;
        public Vector3 Start;
        public float Side;   // 斬撃線の上側 = +1, 下側 = -1
        public float Spin;
        public Mesh Mesh;
        public Color[] Colors;
    }

    private static bool CanSplit(SpriteRenderer target)
    {
        var sprite = target.sprite;
        // アトラスに詰め込まれた (回転・密着) スプライトは矩形から UV を出せないので切らない
        return sprite != null && sprite.texture != null
               && !(sprite.packed && sprite.packingMode == SpritePackingMode.Tight);
    }

    /// <summary>標的のスプライトの矩形を斬撃線で 2 つの多角形に切り、それぞれをメッシュにして置く。</summary>
    private static List<Piece> SplitSprite(SpriteRenderer target, Vector2 hitPoint, float angle)
    {
        var sprite = target.sprite;
        var tex = sprite.texture;
        var rect = sprite.textureRect;
        var bounds = sprite.bounds; // ローカル (ピボット込み) の矩形

        var corners = new[]
        {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.min.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.min.x, bounds.max.y),
        };
        Vector2 ToUv(Vector2 local)
        {
            var u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x);
            var v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, local.y);
            return new Vector2((rect.x + u * rect.width) / tex.width, (rect.y + v * rect.height) / tex.height);
        }

        // 斬撃線をスプライトのローカル座標へ (反転・拡縮・回転込み)
        var tr = target.transform;
        var flip = new Vector2(target.flipX ? -1f : 1f, target.flipY ? -1f : 1f);
        Vector2 WorldToLocal(Vector2 w)
        {
            var l = (Vector2)tr.InverseTransformPoint(w);
            return new Vector2(l.x * flip.x, l.y * flip.y);
        }
        var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        var a = WorldToLocal(hitPoint);
        var b = WorldToLocal(hitPoint + dir);
        var lineDir = (b - a).normalized;
        var normal = new Vector2(-lineDir.y, lineDir.x);
        // 標的の中心を通す (当たり位置が端だと片方が小さすぎるので)
        var center = (Vector2)bounds.center;
        var offset = Vector2.Dot(center - a, normal);
        a += normal * offset;

        var upper = ClipPolygon(corners, a, normal);
        var lower = ClipPolygon(corners, a, -normal);
        // 上側の判定は世界の上 (ローカルの法線を世界に戻して y 成分で決める)
        var worldNormal = (Vector2)tr.TransformDirection(new Vector3(normal.x * flip.x, normal.y * flip.y, 0f));
        var upperSide = worldNormal.y >= 0f ? 1f : -1f;

        var pieces = new List<Piece>();
        var material = target.sharedMaterial;
        foreach (var (poly, side) in new[] { (upper, upperSide), (lower, -upperSide) })
        {
            if (poly.Count < 3)
                continue;

            var mesh = new Mesh();
            var verts = new Vector3[poly.Count];
            var uvs = new Vector2[poly.Count];
            var colors = new Color[poly.Count];
            for (var i = 0; i < poly.Count; i++)
            {
                verts[i] = new Vector3(poly[i].x * flip.x, poly[i].y * flip.y, 0f);
                uvs[i] = ToUv(poly[i]);
                colors[i] = Color.white;
            }
            var tris = new int[(poly.Count - 2) * 3];
            for (var i = 0; i < poly.Count - 2; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            // 反転していると巻き順が逆になるが、スプライトのシェーダーは両面描画なのでそのままでよい
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = tris;

            var go = new GameObject("[FinisherPiece]");
            go.transform.SetParent(_runner.transform, false);
            go.transform.SetPositionAndRotation(tr.position, tr.rotation);
            go.transform.localScale = tr.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.sortingLayerID = target.sortingLayerID;
            mr.sortingOrder = HighlightOrder + 1;

            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", tex);
            mr.SetPropertyBlock(block);

            pieces.Add(new Piece
            {
                Renderer = mr,
                Start = go.transform.position,
                Side = side,
                Spin = side * Random.Range(6f, 14f),
                Mesh = mesh,
                Colors = colors,
            });
        }

        return pieces;
    }

    /// <summary>凸多角形を「keepNormal の向き側」だけ残して切る (Sutherland–Hodgman の 1 辺分)。</summary>
    private static List<Vector2> ClipPolygon(IReadOnlyList<Vector2> poly, Vector2 linePoint, Vector2 keepNormal)
    {
        var result = new List<Vector2>();
        for (var i = 0; i < poly.Count; i++)
        {
            var cur = poly[i];
            var next = poly[(i + 1) % poly.Count];
            var dc = Vector2.Dot(cur - linePoint, keepNormal);
            var dn = Vector2.Dot(next - linePoint, keepNormal);
            if (dc >= 0f)
                result.Add(cur);
            if (dc * dn < 0f)
                result.Add(Vector2.Lerp(cur, next, dc / (dc - dn)));
        }
        return result;
    }

    private static void UpdatePieces(List<Piece> pieces, float t, float angle)
    {
        var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        var k = Mathf.Clamp01((t - SplitHoldTime) / SplitTime);
        var ease = 1f - (1f - k) * (1f - k);
        foreach (var p in pieces)
        {
            if (p.Renderer == null)
                continue;
            // 上の片は斬撃線に沿って滑り、下の片はその場から崩れ落ちる
            var slide = p.Side > 0f ? dir * (SplitSlide * ease) : -dir * (SplitSlide * 0.3f * ease);
            var drop = p.Side > 0f ? 0f : -SplitDrop * k * k;
            p.Renderer.transform.position = p.Start + new Vector3(slide.x, slide.y + drop, 0f);
            p.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.Spin * ease);

            // スプライトのシェーダーは頂点カラーを掛けるので、頂点の不透明度で消す
            var fade = Mathf.Clamp01((k - 0.4f) / 0.6f);
            for (var i = 0; i < p.Colors.Length; i++)
                p.Colors[i] = new Color(1f, 1f, 1f, 1f - fade);
            p.Mesh.colors = p.Colors;
        }
    }

    #endregion

    #region 振動

    private static void Rumble()
    {
        if (InputDeviceTracker.Current != InputDeviceKind.Gamepad)
            return;
        var pad = Gamepad.current;
        if (pad == null)
            return;
        pad.SetMotorSpeeds(RumbleLow, RumbleHigh);
        if (_rumbleStop != null)
            _runner.StopCoroutine(_rumbleStop);
        _rumbleStop = _runner.StartCoroutine(_runner.StopRumble(pad, RumbleTime));
    }

    #endregion

    #region 共通

    private static void EnsureRunner()
    {
        if (_runner != null)
            return;
        var go = new GameObject("[FinisherEffect]") { hideFlags = HideFlags.HideInHierarchy };
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<Runner>();
    }

    private static Sprite WhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Bilinear };
        var px = new Color[16];
        for (var i = 0; i < px.Length; i++)
            px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _whiteSprite;
    }

    /// <summary>横長で両端が尖った白い線 (中央が一番太く明るい)。1 単位 × 1 単位で作り、拡縮で長さと太さを決める。</summary>
    private static Sprite SlashSprite()
    {
        if (_slashSprite != null)
            return _slashSprite;
        const int w = 256, h = 32;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (var x = 0; x < w; x++)
        {
            // 両端へ向かって細くなる (中央寄りが太い)
            var u = x / (w - 1f) * 2f - 1f;
            var half = (1f - u * u) * 0.5f;
            for (var y = 0; y < h; y++)
            {
                var v = Mathf.Abs(y / (h - 1f) * 2f - 1f) * 0.5f;
                var a = half <= 0f ? 0f : Mathf.Clamp01((half - v) / Mathf.Max(half * 0.35f, 0.01f));
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        _slashSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        return _slashSprite;
    }

    private sealed class Runner : MonoBehaviour
    {
        public IEnumerator StopRumble(Gamepad pad, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (pad != null)
                pad.SetMotorSpeeds(0f, 0f);
        }

        private void OnDisable()
        {
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }
    }

    #endregion
}
