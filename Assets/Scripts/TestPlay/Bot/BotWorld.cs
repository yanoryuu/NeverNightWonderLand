#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>立てる面 (床・足場) の 1 区間。プレイヤーの中心 x がこの範囲にあれば立てる。</summary>
public class BotSegment
{
    public int Id;
    public string Name => Moving ? "M" + Id : "P" + Id;
    public float X0;              // 最初の列の中心 x
    public readonly List<float> Ys = new();  // 列ごとの面の高さ
    public bool AllOneWay = true; // 足元がすべてすり抜け床 (↓+ジャンプで降りられる)
    public bool AnyOneWay;
    public bool TimedHazard;      // 周期で出る危険 (蒸気など) が上にある
    public bool HasBreakable;     // 壊せる物の上

    // 動く足場
    public bool Moving;
    public Rigidbody2D Body;
    public Collider2D Collider;
    public Rect TravelRange;      // 動く範囲 (足場の上面の中心の範囲)

    public float Res;
    public float XMin => Moving ? Collider.bounds.min.x + 0.2f : X0 - Res * 0.5f;
    public float XMax => Moving ? Collider.bounds.max.x - 0.2f : X0 + Res * (Ys.Count - 0.5f);
    public float CenterX => (XMin + XMax) * 0.5f;
    public float Width => XMax - XMin;

    public float YAt(float x)
    {
        if (Moving)
            return Collider.bounds.max.y;
        if (Ys.Count == 0)
            return 0f;
        var i = Mathf.Clamp(Mathf.RoundToInt((x - X0) / Res), 0, Ys.Count - 1);
        return Ys[i];
    }

    public float YMin => Moving ? TravelRange.yMin : Ys.Min();
    public float YMax => Moving ? TravelRange.yMax : Ys.Max();
    public float YCenter => YAt(CenterX);

    public float ClampX(float x, float margin = 0f)
    {
        var lo = XMin + margin;
        var hi = XMax - margin;
        return lo > hi ? CenterX : Mathf.Clamp(x, lo, hi);
    }

    public override string ToString() => $"{Name} x[{XMin:F1}..{XMax:F1}] y{YCenter:F1}{(AllOneWay ? " oneway" : "")}";
}

/// <summary>セグメント間の移動 (解析的な見積もり。実際に跳べるかは BotPhysics のシミュレーションで確かめる)。</summary>
public struct BotEdge
{
    public int From;
    public int To;              // -1 = ゴール
    public float Cost;          // 秒の目安
    public BotMove Move;
}

public enum BotMove
{
    Walk,       // 同じ高さで歩いて移れる (段差なし)
    Jump,
    DashJump,   // 走り (ダッシュ後 12) + 空中ダッシュが要る
    DoubleJump,
    Drop,       // 端から落ちる
    DropThrough,
    Platform,   // 動く足場へ / から (待ちが要る)
}

/// <summary>ボットが見る物 (敵・壊せる物・出口など)。</summary>
public class BotEntity
{
    public string Id;
    public string Kind;      // enemy / boss / breakable / speaker / exit / transport / lever / hazard / pickup / arena / wind
    public string Label;
    public Rect Rect;
    public Component Source;
    public bool Active = true;

    public Vector2 Center => Rect.center;
}

/// <summary>
/// ボットの「目」: 今のステージの地形 (Ground レイヤーのコライダー) を格子で調べ、立てる面のセグメントと
/// 解析的な移動グラフを作る。ゴール (矩形) からの逆 Dijkstra で各セグメントの「ゴールまでの見積もり秒」を出す。
/// 敵・危険・出口・移動装置などの一覧も持つ。
/// </summary>
public class BotWorld
{
    public const float Res = 0.5f;

    public string SceneName;
    public Rect Bounds;
    public readonly List<BotSegment> Segments = new();
    public readonly List<BotEdge> Edges = new();
    public readonly List<BotEntity> Entities = new();
    public float[] CostToGoal = Array.Empty<float>();
    public readonly Dictionary<int, BotEdge> NextHop = new(); // セグメント → ゴールへの最短の次の辺
    public Rect Goal;
    public bool HasGoal;

    // 能力 (グラフの見積もりに使う)
    public bool CanDoubleJump;
    public bool CanSuperJump;

    // プレイヤーの形
    public float PlayerWidth = 0.8f;
    public float PlayerHeight = 1.8f;
    public float PivotToFeet = 0.9f; // transform.y - 足元

    // 物理の定数 (PlayerConsts)
    public float WalkSpeed = 8f, RunSpeed = 12f, DashSpeed = 20f, DashDuration = 0.18f;
    public float Gravity = 43.75f, JumpVelocity = 17.5f, FallMul = 1.8f, LowJumpMul = 2.5f, MaxFall = 20f;
    public float JumpHeight = 3.5f;

    public int ScanVersion;      // 走査し直すたびに増える
    public float ScanMilliseconds;

    private byte[] _grid;        // 0 空 / 1 固体 / 2 すり抜け床 / 3 壊せる固体
    private int _cols, _rows;
    private LayerMask _groundMask;
    private readonly HashSet<Collider2D> _dynamic = new(); // 動く足場・ピストン (格子に入れない)
    private readonly Collider2D[] _hits = new Collider2D[16];
    private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[16];

    // ------------------------------------------------------------------ 走査

    /// <summary>今のステージを走査してセグメントと物の一覧を作る。</summary>
    public void Scan(PlayerController player)
    {
        var started = Time.realtimeSinceStartup;
        SceneName = TestPlayActions.CurrentStageName;
        ReadPlayer(player);

        var scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        // 地形コライダー
        var colliders = new List<Collider2D>();
        foreach (var root in scene.GetRootGameObjects())
            colliders.AddRange(root.GetComponentsInChildren<Collider2D>(false));
        colliders.RemoveAll(c => c == null || c.isTrigger || !c.enabled || ((1 << c.gameObject.layer) & _groundMask) == 0);

        _dynamic.Clear();
        foreach (var c in colliders)
        {
            var body = c.attachedRigidbody;
            if (body != null && (body.GetComponent<MovingPlatform>() != null || body.GetComponent<Piston>() != null))
                _dynamic.Add(c);
            else if (c.GetComponentInParent<MovingPlatform>() != null || c.GetComponentInParent<Piston>() != null)
                _dynamic.Add(c);
        }

        var staticColliders = colliders.Where(c => !_dynamic.Contains(c)).ToList();
        if (staticColliders.Count == 0)
            return;

        var b = staticColliders[0].bounds;
        foreach (var c in staticColliders)
            b.Encapsulate(c.bounds);
        // 大きすぎる外周の岩は切る (プレイヤーから 260 以内)
        var p = player != null ? (Vector2)player.transform.position : (Vector2)b.center;
        var xMin = Mathf.Max(b.min.x, p.x - 260f);
        var xMax = Mathf.Min(b.max.x, p.x + 260f);
        var yMin = Mathf.Max(b.min.y, p.y - 260f);
        var yMax = Mathf.Min(b.max.y, p.y + 260f);
        Bounds = Rect.MinMaxRect(Mathf.Floor(xMin), Mathf.Floor(yMin), Mathf.Ceil(xMax), Mathf.Ceil(yMax));

        _cols = Mathf.CeilToInt(Bounds.width / Res);
        _rows = Mathf.CeilToInt(Bounds.height / Res);
        _grid = new byte[_cols * _rows];

        var filter = GroundFilter();
        for (var i = 0; i < _cols; i++)
        {
            var x = Bounds.xMin + (i + 0.5f) * Res;
            for (var j = 0; j < _rows; j++)
            {
                var y = Bounds.yMin + (j + 0.5f) * Res;
                var n = Physics2D.OverlapPoint(new Vector2(x, y), filter, _hits);
                _grid[j * _cols + i] = Classify(n);
            }
        }

        // 薄いコライダー (格子の点に掛からない足場) の上面を書き込む
        foreach (var c in staticColliders)
        {
            var cb = c.bounds;
            if (cb.size.y > 0.75f || cb.size.x < 0.3f)
                continue;
            var type = IsOneWay(c) ? (byte)2 : IsBreakable(c) ? (byte)3 : (byte)1;
            var j = Row(cb.max.y - 0.05f);
            for (var x = cb.min.x + Res * 0.5f; x < cb.max.x; x += Res)
            {
                var i = Col(x);
                if (InGrid(i, j) && _grid[j * _cols + i] == 0)
                    _grid[j * _cols + i] = type;
            }
        }

        BuildSegments();
        CollectEntities(scene);
        AddMovingSegments();
        EdgePenalty.Clear();
        BuildEdges();
        ScanVersion++;
        ScanMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
    }

    private void ReadPlayer(PlayerController player)
    {
        if (player == null)
            return;
        var consts = player.Consts;
        _groundMask = consts.GroundLayer;
        WalkSpeed = consts.MoveSpeed;
        RunSpeed = consts.RunSpeed;
        DashSpeed = consts.DashSpeed;
        DashDuration = consts.DashDuration;
        Gravity = consts.Gravity;
        JumpVelocity = consts.JumpVelocity;
        FallMul = consts.FallGravityMultiplier;
        LowJumpMul = consts.LowJumpMultiplier;
        MaxFall = consts.MaxFallSpeed;
        JumpHeight = consts.JumpHeight;

        var capsule = player.GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            var cb = capsule.bounds;
            PlayerWidth = cb.size.x;
            PlayerHeight = cb.size.y;
            PivotToFeet = player.transform.position.y - cb.min.y;
        }

        CanDoubleJump = player.Progression != null && player.Progression.Has(ScissorUpgrade.Red);
        CanSuperJump = player.HasSkill(PlayerSkill.SuperJump);
    }

    public ContactFilter2D GroundFilter() => new()
    {
        useLayerMask = true,
        layerMask = _groundMask,
        useTriggers = false,
    };

    public LayerMask GroundMask => _groundMask;

    private byte Classify(int count)
    {
        byte result = 0;
        for (var k = 0; k < count; k++)
        {
            var c = _hits[k];
            if (c == null || _dynamic.Contains(c))
                continue;
            if (IsOneWay(c))
            {
                if (result == 0) result = 2;
                continue;
            }

            if (IsBreakable(c))
            {
                if (result != 1) result = 3;
                continue;
            }

            return 1;
        }

        return result;
    }

    public static bool IsOneWay(Collider2D c) => c.usedByEffector && c.GetComponent<PlatformEffector2D>() != null;

    public static bool IsBreakable(Collider2D c) =>
        c.GetComponent<BreakableBox>() != null || c.GetComponentInParent<BreakableBox>() != null ||
        c.GetComponent<SkillBreakable>() != null;

    public bool IsDynamic(Collider2D c) => _dynamic.Contains(c);

    private int Col(float x) => Mathf.FloorToInt((x - Bounds.xMin) / Res);
    private int Row(float y) => Mathf.FloorToInt((y - Bounds.yMin) / Res);
    private bool InGrid(int i, int j) => i >= 0 && j >= 0 && i < _cols && j < _rows;

    /// <summary>格子で固体か (すり抜け床は含めない。壊せる物は固体)。範囲外は空。</summary>
    public bool SolidAt(float x, float y)
    {
        if (_grid == null) return false;
        int i = Col(x), j = Row(y);
        if (!InGrid(i, j)) return false;
        var v = _grid[j * _cols + i];
        return v == 1 || v == 3;
    }

    public byte CellAt(float x, float y)
    {
        if (_grid == null) return 0;
        int i = Col(x), j = Row(y);
        return InGrid(i, j) ? _grid[j * _cols + i] : (byte)0;
    }

    private void BuildSegments()
    {
        Segments.Clear();
        var need = Mathf.CeilToInt((PlayerHeight + 0.1f) / Res);
        var filter = GroundFilter();
        var hazards = CollectStaticHazardRects();

        // 列ごとの立てる点
        var open = new List<BotSegment>();   // 直前の列まで伸びているセグメント
        var nextOpen = new List<BotSegment>();
        for (var i = 0; i < _cols; i++)
        {
            nextOpen.Clear();
            var x = Bounds.xMin + (i + 0.5f) * Res;
            for (var j = 0; j < _rows - need; j++)
            {
                var v = _grid[j * _cols + i];
                if (v == 0)
                    continue;

                var clear = true;
                for (var k = 1; k <= need; k++)
                {
                    var above = _grid[(j + k) * _cols + i];
                    if (above == 1 || above == 3) { clear = false; break; }
                    if (above == 2 && k <= 1) { clear = false; break; } // すり抜け床が重なっている (厚い足場の内部)
                }

                if (!clear)
                    continue;

                // 正確な面の高さ (坂の上でも合うよう上から当てる)
                var cellTop = Bounds.yMin + (j + 1) * Res;
                var y = cellTop;
                var n = Physics2D.Raycast(new Vector2(x, cellTop + 0.6f), Vector2.down, filter, _rayHits, 1.2f);
                var best = float.MaxValue;
                for (var h = 0; h < n; h++)
                {
                    if (_rayHits[h].distance <= 0f || _dynamic.Contains(_rayHits[h].collider)) continue;
                    if (_rayHits[h].distance < best) { best = _rayHits[h].distance; y = _rayHits[h].point.y; }
                }

                // 動かない危険 (トゲ) の上は立てない
                var feet = new Rect(x - 0.2f, y, 0.4f, 0.6f);
                if (hazards.Any(r => r.Overlaps(feet)))
                    continue;

                // 前の列の、高さの近いセグメントへつなぐ
                BotSegment seg = null;
                var bestDy = float.MaxValue;
                foreach (var s in open)
                {
                    var dy = Mathf.Abs(s.Ys[s.Ys.Count - 1] - y);
                    if (dy < bestDy && s.X0 + Res * s.Ys.Count - x < 0.01f) { bestDy = dy; seg = s; }
                }

                // すり抜け床と固体の床は別のセグメントにする (下から抜けられるかが違う)
                if (seg == null || bestDy > 0.55f || nextOpen.Contains(seg) || seg.AllOneWay != (v == 2))
                {
                    seg = new BotSegment { Id = Segments.Count, X0 = x, Res = Res };
                    Segments.Add(seg);
                }

                seg.Ys.Add(y);
                if (v == 2) seg.AnyOneWay = true;
                else seg.AllOneWay = false;
                if (v == 3) seg.HasBreakable = true;
                nextOpen.Add(seg);
            }

            (open, nextOpen) = (nextOpen, open);
        }

        // 狭すぎるもの (体が乗らない) は除く。Id を詰め直す
        Segments.RemoveAll(s => s.Ys.Count < 2);
        for (var k = 0; k < Segments.Count; k++)
            Segments[k].Id = k;
    }

    private List<Rect> CollectStaticHazardRects()
    {
        var list = new List<Rect>();
        var scene = SceneManager.GetSceneByName(SceneName);
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var hz in root.GetComponentsInChildren<HazardZone>(false))
            {
                if (GetField<bool>(hz, "_cycle"))
                    continue;
                var col = hz.GetComponent<Collider2D>();
                if (col != null && hz.IsHazardActive)
                    list.Add(ToRect(col.bounds));
            }
        }

        return list;
    }

    private void AddMovingSegments()
    {
        var seen = new HashSet<Rigidbody2D>();
        foreach (var c in _dynamic)
        {
            var body = c.attachedRigidbody;
            if (body == null || !seen.Add(body))
                continue;
            if (body.GetComponent<MovingPlatform>() == null)
                continue; // ピストンは足場にしない (危険として扱う)
            if (IsOneWay(c) == false && c.bounds.size.y > 3f)
                continue;

            var seg = new BotSegment
            {
                Id = Segments.Count, Moving = true, Body = body, Collider = c, Res = Res,
                AllOneWay = IsOneWay(c), AnyOneWay = IsOneWay(c),
                TravelRange = MovingRange(body.GetComponent<MovingPlatform>(), c),
            };
            Segments.Add(seg);
        }
    }

    /// <summary>動く足場の上面の中心が動く範囲 (経由点・円軌道から)。</summary>
    private static Rect MovingRange(MovingPlatform mp, Collider2D c)
    {
        var top = new Vector2(c.bounds.center.x, c.bounds.max.y);
        var offset = top - (Vector2)mp.transform.position;
        var mode = GetField<object>(mp, "_mode")?.ToString();
        var points = new List<Vector2>();
        if (mode == "Orbit")
        {
            var center = GetField<Transform>(mp, "_orbitCenter");
            var origin = center != null ? (Vector2)center.position : GetField<Vector2>(mp, "_origin");
            var r = GetField<float>(mp, "_radius");
            for (var a = 0; a < 360; a += 15)
                points.Add(origin + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r);
        }
        else
        {
            var origin = GetField<Vector2>(mp, "_origin");
            var wps = GetField<Vector2[]>(mp, "_waypoints");
            if (wps != null)
                points.AddRange(wps.Select(w => origin + w));
        }

        if (points.Count == 0)
            return new Rect(top, Vector2.zero);
        var minX = points.Min(q => q.x) + offset.x;
        var maxX = points.Max(q => q.x) + offset.x;
        var minY = points.Min(q => q.y) + offset.y;
        var maxY = points.Max(q => q.y) + offset.y;
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    // ------------------------------------------------------------------ 物

    private void CollectEntities(Scene scene)
    {
        Entities.Clear();
        var bossSet = new HashSet<EnemyController>();
        var n = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var arena in root.GetComponentsInChildren<BossArena>(false))
            {
                var boss = GetField<EnemyController>(arena, "_boss");
                if (boss != null) bossSet.Add(boss);
                var col = arena.GetComponent<Collider2D>();
                if (col != null && arena.enabled)
                    Entities.Add(new BotEntity
                    {
                        Id = "A" + (++n), Kind = "arena", Label = GetField<string>(arena, "_flagId"),
                        Rect = ToRect(col.bounds), Source = arena,
                    });
            }

            foreach (var zone in root.GetComponentsInChildren<SceneTransitionZone>(false))
            {
                var col = zone.GetComponent<Collider2D>();
                if (col == null) continue;
                Entities.Add(new BotEntity
                {
                    Id = "X" + (++n), Kind = "exit", Label = GetField<string>(zone, "_sceneName"),
                    Rect = ToRect(col.bounds), Source = zone,
                });
            }

            foreach (var t in root.GetComponentsInChildren<TransportInteractable>(false))
            {
                var col = t.GetComponent<Collider2D>();
                var rect = col != null ? ToRect(col.bounds) : new Rect((Vector2)t.transform.position - Vector2.one, Vector2.one * 2f);
                Entities.Add(new BotEntity
                {
                    Id = "T" + (++n), Kind = "transport", Label = GetField<string>(t, "_sceneName"),
                    Rect = rect, Source = t, Active = t.IsUnlocked,
                });
            }

            foreach (var lever in root.GetComponentsInChildren<LeverSwitch>(false))
            {
                Entities.Add(new BotEntity
                {
                    Id = "L" + (++n), Kind = "lever", Label = GetField<string>(lever, "_progressFlag"),
                    Rect = new Rect((Vector2)lever.transform.position - new Vector2(0.5f, 0.5f), Vector2.one),
                    Source = lever, Active = lever.CanInteract,
                });
            }

            foreach (var hz in root.GetComponentsInChildren<HazardZone>(false))
            {
                var col = hz.GetComponent<Collider2D>();
                if (col == null) continue;
                Entities.Add(new BotEntity
                {
                    Id = "H" + (++n), Kind = "hazard",
                    Label = GetField<bool>(hz, "_cycle") ? "timed" : GetField<bool>(hz, "_returnToSafeGround") ? "pit" : "spikes",
                    Rect = ToRect(col.bounds), Source = hz, Active = hz.IsHazardActive,
                });
            }

            foreach (var piston in root.GetComponentsInChildren<Piston>(false))
            {
                var col = piston.GetComponentInChildren<Collider2D>();
                if (col == null) continue;
                var ext = GetField<Vector2>(piston, "_extendOffset");
                var r = ToRect(col.bounds);
                var r2 = new Rect(r.position + ext, r.size);
                Entities.Add(new BotEntity
                {
                    Id = "H" + (++n), Kind = "hazard", Label = "piston",
                    Rect = Rect.MinMaxRect(Mathf.Min(r.xMin, r2.xMin), Mathf.Min(r.yMin, r2.yMin),
                        Mathf.Max(r.xMax, r2.xMax), Mathf.Max(r.yMax, r2.yMax)),
                    Source = piston,
                });
            }

            foreach (var wind in root.GetComponentsInChildren<WindArea>(false))
            {
                var col = wind.GetComponent<Collider2D>();
                if (col == null) continue;
                var v = GetField<Vector2>(wind, "_velocity");
                Entities.Add(new BotEntity
                {
                    Id = "W" + (++n), Kind = "wind", Label = v.x < 0 ? "blows_left" : v.x > 0 ? "blows_right" : "blows_vertical",
                    Rect = ToRect(col.bounds), Source = wind,
                });
            }

            foreach (var wave in root.GetComponentsInChildren<WaveArena>(false))
            {
                var col = wave.GetComponent<Collider2D>();
                if (col == null) continue;
                Entities.Add(new BotEntity
                {
                    Id = "A" + (++n), Kind = "wavearena", Label = GetField<string>(wave, "_flagId"),
                    Rect = ToRect(col.bounds), Source = wave,
                });
            }

            foreach (var box in root.GetComponentsInChildren<BreakableBox>(false))
                AddBreakable(box, "breakable", "box", ref n);
            foreach (var sb in root.GetComponentsInChildren<SkillBreakable>(false))
                AddBreakable(sb, "breakable", "needs_" + sb.RequiredSkill, ref n);
            foreach (var sp in root.GetComponentsInChildren<BreakableSpeaker>(false))
                AddBreakable(sp, "speaker", GetField<string>(sp, "_flagId"), ref n);

            foreach (var pickup in root.GetComponentsInChildren<ItemPickup>(false))
            {
                if (!pickup.CanInteract) continue;
                Entities.Add(new BotEntity
                {
                    Id = "I" + (++n), Kind = "pickup", Label = pickup.GetType().Name,
                    Rect = new Rect((Vector2)pickup.transform.position - new Vector2(0.5f, 0.5f), Vector2.one), Source = pickup,
                });
            }
        }

        _bosses = bossSet;
        RefreshEnemies();
    }

    private HashSet<EnemyController> _bosses = new();

    private void AddBreakable(Component c, string kind, string label, ref int n)
    {
        var col = FirstCollider(c);
        if (col == null || !col.enabled) return;
        Entities.Add(new BotEntity { Id = "B" + (++n), Kind = kind, Label = label, Rect = ToRect(col.bounds), Source = c });
    }

    /// <summary>敵の一覧と動く物の位置を更新する (判断のたびに呼ぶ。地形の走査はしない)。</summary>
    public void RefreshEnemies()
    {
        Entities.RemoveAll(e => e.Kind == "enemy" || e.Kind == "boss");
        Entities.RemoveAll(e => e.Source == null);
        foreach (var e in Entities)
        {
            if (e.Source is HazardZone hz) e.Active = hz.IsHazardActive;
            else if (e.Source is TransportInteractable t) e.Active = t.IsUnlocked;
            else if (e.Source is LeverSwitch l) e.Active = l.CanInteract;
            else if (e.Source is ItemPickup p) e.Active = p.CanInteract;
            else if (e.Source is BossArena a) e.Active = a.enabled;

            if (e.Source != null && (e.Kind == "breakable" || e.Kind == "speaker"))
            {
                var col = FirstCollider(e.Source);
                e.Active = col != null && col.enabled && col.gameObject.activeInHierarchy;
            }
        }

        var n = 0;
        foreach (var enemy in EnemyController.Active)
        {
            if (enemy == null || enemy.IsDead || enemy.gameObject.scene.name != SceneName && enemy.gameObject.scene.name != StageLoader.PlayerSceneName)
                continue;
            var col = enemy.GetComponent<Collider2D>();
            var rect = col != null ? ToRect(col.bounds) : new Rect((Vector2)enemy.transform.position - Vector2.one * 0.5f, Vector2.one);
            Entities.Add(new BotEntity
            {
                Id = "E" + (++n), Kind = _bosses.Contains(enemy) ? "boss" : "enemy", Label = enemy.name,
                Rect = rect, Source = enemy,
            });
        }
    }

    public IEnumerable<BotEntity> Enemies => Entities.Where(e => (e.Kind == "enemy" || e.Kind == "boss") && e.Source != null);

    // ------------------------------------------------------------------ 位置からセグメント

    /// <summary>プレイヤーが立っているセグメント (空中なら null)。</summary>
    public BotSegment SegmentUnder(PlayerController player)
    {
        if (player == null) return null;
        var ground = player.GroundCollider;
        if (ground != null && _dynamic.Contains(ground))
            return Segments.FirstOrDefault(s => s.Moving && s.Collider == ground);
        var feet = (Vector2)player.transform.position - new Vector2(0f, PivotToFeet);
        return SegmentAt(feet, 0.6f);
    }

    public BotSegment SegmentAt(Vector2 feet, float tolerance)
    {
        BotSegment best = null;
        var bestD = tolerance;
        foreach (var s in Segments)
        {
            if (s.Moving) continue;
            if (feet.x < s.XMin - 0.35f || feet.x > s.XMax + 0.35f) continue;
            var d = Mathf.Abs(s.YAt(feet.x) - feet.y);
            if (d < bestD) { bestD = d; best = s; }
        }

        return best;
    }

    // ------------------------------------------------------------------ グラフ

    private float FallGravity => Gravity * FallMul;

    /// <summary>真上へ跳んだ時に dy (正 = 上) の高さで下降中に通過するまでの時間。届かなければ -1。</summary>
    public float AirTime(float dy, bool doubleJump)
    {
        var apex = doubleJump ? JumpHeight * 1.9f : JumpHeight;
        var tUp = doubleJump ? 0.75f : JumpVelocity / Gravity;
        if (dy > apex) return -1f;
        return tUp + Mathf.Sqrt(2f * (apex - dy) / FallGravity);
    }

    private readonly List<BotEdge> _segmentEdges = new();
    public float GraphMilliseconds;

    /// <summary>セグメント間の辺を作る (走査のたびに 1 回)。</summary>
    public void BuildEdges()
    {
        var started = Time.realtimeSinceStartup;
        _segmentEdges.Clear();
        var segs = Segments;
        for (var a = 0; a < segs.Count; a++)
        {
            for (var b = 0; b < segs.Count; b++)
            {
                if (a == b) continue;
                if (TryEdge(segs[a], segs[b], out var edge))
                    _segmentEdges.Add(edge);
            }
        }

        GraphMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
    }

    /// <summary>追加の辺の重み (失敗した跳躍を重くする)。key = from*100000+to。</summary>
    public readonly Dictionary<int, float> EdgePenalty = new();

    public static int EdgeKey(int from, int to) => from * 100000 + (to + 1);

    /// <summary>ゴール (矩形) を設定し、ゴールまでの見積もり (逆 Dijkstra) を作り直す。</summary>
    public void BuildGraph(Rect? goal)
    {
        HasGoal = goal.HasValue;
        Goal = goal ?? default;
        Edges.Clear();
        foreach (var e in _segmentEdges)
        {
            var edge = e;
            if (EdgePenalty.TryGetValue(EdgeKey(e.From, e.To), out var p)) edge.Cost += p;
            Edges.Add(edge);
        }

        var segs = Segments;
        if (HasGoal)
        {
            for (var a = 0; a < segs.Count; a++)
            {
                if (TryGoalEdge(segs[a], Goal, out var g))
                {
                    if (EdgePenalty.TryGetValue(EdgeKey(g.From, -1), out var p)) g.Cost += p;
                    Edges.Add(g);
                }
            }
        }

        // 逆 Dijkstra (ゴールから)
        var n = segs.Count;
        CostToGoal = Enumerable.Repeat(float.PositiveInfinity, n).ToArray();
        NextHop.Clear();
        if (!HasGoal) return;

        var incoming = new List<BotEdge>[n];
        for (var i = 0; i < n; i++) incoming[i] = new List<BotEdge>();
        foreach (var e in Edges)
        {
            if (e.To < 0)
            {
                if (e.Cost < CostToGoal[e.From])
                {
                    CostToGoal[e.From] = e.Cost;
                    NextHop[e.From] = e;
                }
            }
            else incoming[e.To].Add(e);
        }

        var done = new bool[n];
        while (true)
        {
            var u = -1;
            var best = float.PositiveInfinity;
            for (var i = 0; i < n; i++)
                if (!done[i] && CostToGoal[i] < best) { best = CostToGoal[i]; u = i; }
            if (u < 0) break;
            done[u] = true;
            foreach (var e in incoming[u])
            {
                var c = best + e.Cost;
                if (c < CostToGoal[e.From])
                {
                    CostToGoal[e.From] = c;
                    NextHop[e.From] = e;
                }
            }
        }
    }

    public float Cost(BotSegment s) => s != null && s.Id < CostToGoal.Length ? CostToGoal[s.Id] : float.PositiveInfinity;

    private bool TryEdge(BotSegment a, BotSegment b, out BotEdge edge)
    {
        edge = new BotEdge { From = a.Id, To = b.Id };

        // 離陸点 (a の上) と着地点 (b の上) の近い組
        float ax, bx;
        var aboveSolid = !b.AllOneWay && !b.Moving && !a.Moving && b.YAt(b.CenterX) > a.YAt(Mathf.Clamp(b.CenterX, a.XMin, a.XMax)) + 0.5f;
        if (b.XMin >= a.XMax - 0.01f) { ax = a.XMax - 0.3f; bx = b.XMin + 0.3f; }
        else if (b.XMax <= a.XMin + 0.01f) { ax = a.XMin + 0.3f; bx = b.XMax - 0.3f; }
        else if (aboveSolid)
        {
            // 上の固体の足場に真下から重なる: 脇 (広い方) から回り込む
            var leftRoom = b.XMin - a.XMin;
            var rightRoom = a.XMax - b.XMax;
            if (Mathf.Max(leftRoom, rightRoom) < PlayerWidth * 0.5f + 0.3f)
                return false; // 真下からしか届かない (天井)
            if (leftRoom >= rightRoom) { ax = b.XMin - PlayerWidth * 0.5f - 0.15f; bx = b.XMin + 0.4f; }
            else { ax = b.XMax + PlayerWidth * 0.5f + 0.15f; bx = b.XMax - 0.4f; }
        }
        else { ax = bx = (Mathf.Max(a.XMin, b.XMin) + Mathf.Min(a.XMax, b.XMax)) * 0.5f; }

        ax = a.ClampX(ax, Mathf.Min(0.3f, a.Width * 0.5f));
        bx = b.ClampX(bx, Mathf.Min(0.3f, b.Width * 0.5f));
        var gap = Mathf.Abs(bx - ax);
        if (gap > 16f) return false;

        float ay, by;
        if (a.Moving || b.Moving)
        {
            // 動く足場は範囲の中でいちばん都合のよい高さ
            ay = a.Moving ? Mathf.Clamp(b.YAt(bx), a.YMin, a.YMax) : a.YAt(ax);
            by = b.Moving ? Mathf.Clamp(ay, b.YMin, b.YMax) : b.YAt(bx);
            if (b.Moving)
            {
                var dxr = Mathf.Max(0f, Mathf.Max(b.TravelRange.xMin - a.XMax, a.XMin - b.TravelRange.xMax) - b.Width * 0.5f);
                gap = dxr;
            }
        }
        else
        {
            ay = a.YAt(ax);
            by = b.YAt(bx);
        }

        var dy = by - ay;
        if (dy > 8f || dy < -45f) return false;

        // 同じ高さで隣り合う (つながった床の切れ目): 歩き
        if (!a.Moving && !b.Moving && gap < 0.8f && Mathf.Abs(dy) < 0.6f)
        {
            edge.Move = BotMove.Walk;
            edge.Cost = 0.2f;
            return true;
        }

        var margin = 0.4f;
        var overlap = !(b.XMin > a.XMax || b.XMax < a.XMin);

        // すり抜け床から真下へ
        if (a.AllOneWay && dy < -0.5f && overlap && !b.Moving)
        {
            edge.Move = BotMove.DropThrough;
            edge.Cost = 0.3f + Mathf.Sqrt(2f * -dy / FallGravity);
            // 途中に別の床 (すり抜け床を含む) があれば、そこに着いてしまう
            var lo = Mathf.Max(a.XMin, b.XMin) + 0.3f;
            var hi = Mathf.Min(a.XMax, b.XMax) - 0.3f;
            for (var x = lo; x <= hi + 0.01f; x += Res)
                if (ColumnEmptyBelow(x, ay, by + 0.3f)) return true;
            return false;
        }

        var ledge = !b.AllOneWay && !b.Moving ? 1.1f : 0f;
        foreach (var mode in new[] { BotMove.Jump, BotMove.DashJump, BotMove.DoubleJump })
        {
            if (mode == BotMove.DoubleJump && !CanDoubleJump) continue;
            var dj = mode == BotMove.DoubleJump;
            var apex = dj ? JumpHeight * 1.9f : JumpHeight;
            if (dy > apex + ledge) continue;
            var t = AirTime(Mathf.Min(dy, apex - 0.05f), dj);
            if (dy > apex) t = 0.3f;
            var v = mode == BotMove.DashJump ? RunSpeed : WalkSpeed;
            var reach = v * t + (mode == BotMove.DashJump ? DashSpeed * DashDuration : 0f) - margin;
            if (gap > reach) continue;

            if (!a.Moving && !b.Moving && !ArcClear(ax, ay, bx, by, apex))
                continue;

            // 下へ跳ぶ時は、着地点の真上に別の床が無いこと (先にそちらへ着いてしまう)
            if (dy < -0.5f && !a.Moving && !b.Moving &&
                !LandingClear(a, bx, ay, by) && !LandingClear(a, b.CenterX, ay, by))
                continue;

            edge.Move = b.Moving || a.Moving ? BotMove.Platform : mode;
            edge.Cost = 0.35f + gap / v + Mathf.Max(0f, dy) * 0.08f
                        + (mode == BotMove.DashJump ? 0.6f : 0f) + (mode == BotMove.DoubleJump ? 0.3f : 0f)
                        + (edge.Move == BotMove.Platform ? 2.5f : 0f) + (b.TimedHazard ? 1f : 0f);
            return true;
        }

        // 端から落ちる (下の足場へ。空中で寄せる)
        if (dy < -0.5f && !a.Moving && !b.Moving)
        {
            var tDown = Mathf.Sqrt(2f * -dy / FallGravity);
            var reach = WalkSpeed * tDown + 0.6f;
            foreach (var end in new[] { a.XMin - PlayerWidth * 0.5f - 0.1f, a.XMax + PlayerWidth * 0.5f + 0.1f })
            {
                var d = end < b.XMin ? b.XMin - end : end > b.XMax ? end - b.XMax : 0f;
                if (d > reach) continue;
                if (!ColumnEmpty(end, a.YAt(end < a.CenterX ? a.XMin : a.XMax) + 0.3f, by + 1.0f)) continue;
                edge.Move = BotMove.Drop;
                edge.Cost = 0.3f + tDown + Mathf.Abs(end - a.CenterX) * 0.02f;
                return true;
            }
        }

        return false;
    }

    private bool TryGoalEdge(BotSegment a, Rect goal, out BotEdge edge)
    {
        edge = new BotEdge { From = a.Id, To = -1 };
        var g = new Rect(goal.xMin - 0.1f, goal.yMin - 0.1f, goal.width + 0.2f, goal.height + 0.2f);

        // 立ったまま触れる
        var ax = a.ClampX(g.center.x);
        var ay = a.YAt(ax);
        var body = new Rect(ax - PlayerWidth * 0.5f, ay, PlayerWidth, PlayerHeight);
        if (!a.Moving && body.Overlaps(g))
        {
            edge.Move = BotMove.Walk;
            edge.Cost = 0.1f;
            return true;
        }

        var gap = g.xMin > a.XMax ? g.xMin - a.XMax : g.xMax < a.XMin ? a.XMin - g.xMax : 0f;
        if (gap > 14f) return false;

        // 跳んで触れる (頭が届く / 落ちて触れる)
        var apex = CanDoubleJump ? JumpHeight * 1.9f : JumpHeight;
        var headTop = ay + apex + PlayerHeight;
        if (g.yMin > headTop) return false;

        var targetY = Mathf.Clamp(ay, g.yMin, g.yMax);
        var dy = targetY - ay;
        var t = AirTime(Mathf.Min(dy, apex - 0.05f), CanDoubleJump);
        if (t < 0f) t = 0.3f;
        if (g.yMax < ay) t = Mathf.Sqrt(2f * (ay - g.yMax) / FallGravity) + 0.3f; // 下の穴へ落ちる
        var reach = RunSpeed * t + DashSpeed * DashDuration;
        if (gap > reach) return false;

        // 通り道が岩で塞がっていないか (地図の外周の岩の上などから「落ちて触れる」誤りを除く)
        var gx = Mathf.Clamp(ax, g.xMin + 0.3f, g.xMax - 0.3f);
        var gFeet = Mathf.Clamp(ay, g.yMin - PlayerHeight + 0.4f, g.yMax - 0.3f);
        var clear = !a.Moving && ArcClear(ax, ay, gx, gFeet, apex);
        if (!clear && g.yMax < ay && !a.Moving)
        {
            foreach (var end in new[] { a.XMin - PlayerWidth * 0.5f - 0.1f, a.XMax + PlayerWidth * 0.5f + 0.1f })
                if (Mathf.Abs(end - gx) <= reach && ColumnClear(end, ay + 0.3f, gFeet + 1f)) clear = true;
        }

        if (!clear && !a.Moving) return false;

        edge.Move = gap > WalkSpeed * t ? BotMove.DashJump : g.yMax < ay ? BotMove.Drop : BotMove.Jump;
        edge.Cost = 0.4f + gap / WalkSpeed + Mathf.Max(0f, dy) * 0.08f;
        return true;
    }

    /// <summary>x の真上 (着地の直前に通る所) に別の床が無いか。離陸する床 a 自身の厚みは除く。</summary>
    private bool LandingClear(BotSegment a, float x, float ay, float by)
    {
        if (x >= a.XMin && x <= a.XMax) return ColumnEmptyBelow(x, a.YAt(x), by + 0.3f);
        return ColumnEmpty(x, Mathf.Min(ay + 1f, by + 4.5f), by + 0.3f);
    }

    /// <summary>床 (面の高さ ay) の厚みの下から yBottom まで何も無いか (すり抜けて落ちる時)。</summary>
    private bool ColumnEmptyBelow(float x, float ay, float yBottom)
    {
        var y = ay - Res * 0.5f;
        while (y > yBottom && CellAt(x, y) != 0) y -= Res;
        return ColumnEmpty(x, y, yBottom);
    }

    /// <summary>縦の柱に何も無いか (すり抜け床も含めて。落ちる途中で別の床に着かないか)。</summary>
    private bool ColumnEmpty(float x, float yTop, float yBottom)
    {
        if (yTop < yBottom) (yTop, yBottom) = (yBottom, yTop);
        for (var y = yBottom; y <= yTop; y += Res)
            if (CellAt(x, y) != 0) return false;
        return true;
    }

    /// <summary>縦の柱 (体の幅の中心線) が固体で塞がっていないか。</summary>
    private bool ColumnClear(float x, float yTop, float yBottom)
    {
        if (yTop < yBottom) (yTop, yBottom) = (yBottom, yTop);
        for (var y = yBottom; y <= yTop; y += Res)
            if (SolidAt(x, y)) return false;
        return true;
    }

    /// <summary>
    /// 跳躍の大まかな通り道が固体で塞がっていないか (格子で見る)。2 通り試す:
    /// 斜めに真っ直ぐ (放物線の近似) / 真上へ上がってから横へ (段の上へ)。
    /// </summary>
    private bool ArcClear(float ax, float ay, float bx, float by, float apex)
    {
        var top = Mathf.Max(ay, by) + 0.6f;
        top = Mathf.Min(top, ay + apex);
        var h = PlayerHeight;

        // 1) 放物線の近似: 離陸 → 頂点 (中間) → 着地
        var mx = (ax + bx) * 0.5f;
        if (SegmentClear(ax, ay + 0.2f, mx, top, h) && SegmentClear(mx, top, bx, by + 0.2f, h))
            return true;

        // 2) 真上 → 横
        return SegmentClear(ax, ay + 0.2f, ax, top, h) && SegmentClear(ax, top, bx, top, h) &&
               SegmentClear(bx, top, bx, by + 0.2f, h);
    }

    private bool SegmentClear(float x0, float feet0, float x1, float feet1, float h)
    {
        var len = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(feet1 - feet0));
        var steps = Mathf.Max(1, Mathf.CeilToInt(len / (Res * 0.75f)));
        for (var s = 0; s <= steps; s++)
        {
            var t = s / (float)steps;
            var x = Mathf.Lerp(x0, x1, t);
            var f = Mathf.Lerp(feet0, feet1, t);
            if (SolidAt(x, f + 0.35f) || SolidAt(x, f + h * 0.5f) || SolidAt(x, f + h - 0.3f))
                return false;
        }

        return true;
    }

    // ------------------------------------------------------------------ 補助

    /// <summary>自身か子のコライダー (Unity の偽 null に ?? を使わない)。</summary>
    public static Collider2D FirstCollider(Component c)
    {
        if (c == null) return null;
        var col = c.GetComponent<Collider2D>();
        if (col != null) return col;
        return c.GetComponentInChildren<Collider2D>();
    }

    public static Rect ToRect(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>非公開フィールドを読む (ボット専用。ゲーム側のコードは変えない)。</summary>
    public static T GetField<T>(object target, string name)
    {
        if (target == null) return default;
        var type = target.GetType();
        while (type != null)
        {
            var f = type.GetField(name, Flags);
            if (f != null)
            {
                var v = f.GetValue(target);
                return v is T t ? t : default;
            }

            type = type.BaseType;
        }

        return default;
    }
}
#endif
