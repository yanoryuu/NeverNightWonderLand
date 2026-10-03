#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

/// <summary>跳躍の的 (セグメント・ゴールの矩形・動く足場)。</summary>
public class BotTarget
{
    public BotSegment Segment;   // 足場 (null ならゴールの矩形)
    public Rect Rect;            // ゴールの矩形 (体が触れれば成功)

    public bool IsRect => Segment == null;
    public bool Moving => Segment != null && Segment.Moving;
    public bool OneWay => Segment != null && Segment.AllOneWay;

    public float XMin => IsRect ? Rect.xMin : Segment.XMin;
    public float XMax => IsRect ? Rect.xMax : Segment.XMax;
    public float CenterX => (XMin + XMax) * 0.5f;

    public override string ToString() => IsRect ? $"rect({Rect.xMin:F1},{Rect.yMin:F1} {Rect.width:F1}x{Rect.height:F1})" : Segment.ToString();
}

/// <summary>跳躍の計画 (BotPhysics.Plan の結果。BotExecutor が実行する)。</summary>
public class BotJumpPlan
{
    public bool Valid;
    public float TakeoffX;
    public bool Dash;          // 先にダッシュして走り (12) で跳ぶ
    public bool Drop;          // 跳ばずに端から落ちる
    public bool DropThrough;   // すり抜け床を ↓+ジャンプで降りる
    public float HoldTime;     // ジャンプ長押しの秒数 (大きい値 = 頂点まで)
    public bool DoubleJump;    // 頂点で二段ジャンプ
    public bool AirDash;       // 頂点で空中ダッシュ
    public float AirTime;
    public Vector2 Landing;
    public int Simulations;
    public readonly List<Vector2> Path = new();

    public string Describe() =>
        !Valid ? "no plan" :
        $"{(DropThrough ? "drop-through" : Drop ? "walk-off" : Dash ? "dash-jump" : "jump")} at x{TakeoffX:F1}" +
        $"{(DoubleJump ? " +double" : "")}{(AirDash ? " +airdash" : "")}{(!Drop && !DropThrough && HoldTime < 1f ? $" hold{HoldTime:F2}" : "")} " +
        $"-> ({Landing.x:F1},{Landing.y:F1}) {AirTime:F2}s";
}

/// <summary>
/// プレイヤーの空中挙動のシミュレーション (PlayerController の ApplyGravity / ApplyHorizontalMovement と同じ式) と
/// 跳躍の計画。地形との当たりは本物の物理クエリ (CapsuleCast) で調べる。空中の舵取り (Steer) は計画と実行で共用し、
/// 実行時は毎フレームの実位置から舵を取り直すので、小さなずれは自動で補正される。
/// </summary>
public class BotPhysics
{
    public const float Dt = 0.02f;
    private const float Skin = 0.015f;

    private readonly BotWorld _world;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
    private readonly HashSet<Collider2D> _ignore = new();

    public Vector2 CapsuleOffset;   // transform → カプセル中心
    public Vector2 CapsuleSize;
    public float WallCheckDistance = 0.45f;
    public float GlideFallSpeed = 2.5f;
    public bool CanGlide;

    public int LastSimulations;

    private Vector2 Off(int facing) => new(CapsuleOffset.x * (facing == 0 ? 1 : facing), CapsuleOffset.y);
    public float LedgeFeetOffset = 1f; // transform.y - (GroundCheck.y - 半径) = 崖登り判定の足元

    public BotPhysics(BotWorld world) => _world = world;

    public void ReadPlayer(PlayerController player)
    {
        var capsule = player.GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            var off = (Vector2)capsule.bounds.center - (Vector2)player.transform.position;
            CapsuleOffset = new Vector2(off.x * player.Facing, off.y); // 右向きの時の値 (左向きでは x が反転する)
            CapsuleSize = (Vector2)capsule.bounds.size - new Vector2(0.03f, 0.03f);
        }
        else
        {
            CapsuleOffset = Vector2.zero;
            CapsuleSize = new Vector2(_world.PlayerWidth, _world.PlayerHeight);
        }

        var groundCheck = BotWorld.GetField<Transform>(player, "_groundCheck");
        LedgeFeetOffset = groundCheck != null
            ? player.transform.position.y - (groundCheck.position.y - player.Consts.GroundCheckRadius)
            : _world.PivotToFeet;
        WallCheckDistance = player.Consts.WallCheckDistance;
        GlideFallSpeed = player.Consts.GlideFallSpeed;
        CanGlide = player.Progression != null && player.Progression.Has(ScissorUpgrade.Red);
    }

    // ------------------------------------------------------------------ 舵取り (計画と実行で共用)

    /// <summary>
    /// 空中の横入力 (-1..1)。的の上に乗る/触れるように寄せる。固体の足場の上面より足が下にある間は、
    /// 足場の側面にぶつからないよう脇で待つ (上面を越えてから寄せる)。
    /// </summary>
    public float Steer(Vector2 pos, Vector2 vel, BotTarget target, float speed, Vector2? platformShift = null)
    {
        var x = pos.x;
        var feet = pos.y - _world.PivotToFeet;
        var shift = platformShift ?? Vector2.zero;
        float desired;

        if (target.IsRect)
        {
            var r = target.Rect;
            desired = Mathf.Clamp(x, r.xMin + Mathf.Min(0.4f, r.width * 0.5f), r.xMax - Mathf.Min(0.4f, r.width * 0.5f));
        }
        else
        {
            var xMin = target.XMin + shift.x;
            var xMax = target.XMax + shift.x;
            var margin = Mathf.Min(0.45f, (xMax - xMin) * 0.5f);
            desired = Mathf.Clamp(x, xMin + margin, xMax - margin);
            var top = (target.Moving ? target.Segment.YAt(0f) : target.Segment.YAt(desired)) + shift.y;
            var half = _world.PlayerWidth * 0.5f;
            if (!target.OneWay && feet < top - 0.05f && x > xMin - half - 0.1f && x < xMax + half + 0.1f && vel.y > -0.5f)
            {
                // 上面より下で、足場の真下/側面に掛かっている → 外側で待つ (上がりきったら寄せる)
                desired = x < (xMin + xMax) * 0.5f ? xMin - half - 0.05f : xMax + half + 0.05f; // 壁沿いに上がる (崖登りも掛かる)
            }
        }

        var dx = desired - x;
        if (Mathf.Abs(dx) < 0.04f) return 0.0001f * Mathf.Sign(dx == 0 ? 1 : dx); // 0 にすると走りが切れる。ほぼ止まる
        return Mathf.Clamp(dx * 3.5f, -1f, 1f);
    }

    // ------------------------------------------------------------------ シミュレーション

    private struct Sim
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public bool Running;
        public bool Held;
        public bool AirJumpUsed;
        public float DashTimer;
        public int DashDir;
        public bool Landed;
        public Collider2D LandedOn;
        public int Facing;
        public bool ViaLedge;
    }

    /// <summary>跳躍計画を探す。見つからなければ Valid=false。</summary>
    public BotJumpPlan Plan(BotSegment from, Vector2 playerPos, BotTarget target, int budget = 360)
    {
        var plan = new BotJumpPlan();
        LastSimulations = 0;
        _ignore.Clear();

        if (from == null)
            return plan;

        // 離陸点の候補 (今の位置に近い順)
        var xs = new List<float>();
        if (from.Moving)
        {
            xs.Add(playerPos.x);
        }
        else
        {
            var lo = from.XMin + 0.15f;
            var hi = from.XMax - 0.15f;
            // 的から遠すぎる所は試さない
            lo = Mathf.Max(lo, target.XMin - 16f);
            hi = Mathf.Min(hi, target.XMax + 16f);
            for (var x = lo; x <= hi + 0.001f; x += 0.25f) xs.Add(x);
            xs.Add(Mathf.Clamp(playerPos.x, from.XMin, from.XMax));
            xs.Sort((a, b) => Mathf.Abs(a - playerPos.x).CompareTo(Mathf.Abs(b - playerPos.x)));
        }

        var canDouble = _world.CanDoubleJump;
        var variants = new List<(bool dash, float hold, bool dj, bool airDash)>
        {
            (false, 10f, false, false),
            (false, 0.12f, false, false),
            (true, 10f, false, false),
        };
        if (canDouble)
        {
            variants.Add((false, 10f, true, false));
            variants.Add((true, 10f, true, false));
        }
        variants.Add((true, 10f, false, true));

        // 落ちる (端から歩いて出る) / すり抜け
        if (!from.Moving)
        {
            var targetBelow = TargetTop(target) < from.YAt(target.CenterX) - 0.5f;
            if (targetBelow)
            {
                foreach (var edgeX in new[] { from.XMin - _world.PlayerWidth * 0.5f - 0.05f, from.XMax + _world.PlayerWidth * 0.5f + 0.05f })
                {
                    if (Mathf.Abs(edgeX - target.CenterX) > 14f) continue;
                    var y = from.YAt(Mathf.Clamp(edgeX, from.XMin, from.XMax));
                    var start = new Sim { Pos = new Vector2(edgeX, y + _world.PivotToFeet + 0.02f), Vel = Vector2.zero };
                    if (Simulate(start, target, false, 0f, false, false, plan))
                    {
                        plan.Drop = true;
                        plan.TakeoffX = edgeX;
                        return plan;
                    }
                }

                if (from.AllOneWay)
                {
                    foreach (var x in xs)
                    {
                        if (LastSimulations > budget) break;
                        CollectFloor(x, from);
                        var start = new Sim { Pos = new Vector2(x, from.YAt(x) + _world.PivotToFeet - 0.05f), Vel = Vector2.zero };
                        if (Simulate(start, target, false, 0f, false, false, plan))
                        {
                            plan.DropThrough = true;
                            plan.TakeoffX = x;
                            return plan;
                        }

                        _ignore.Clear();
                    }
                }
            }
        }

        foreach (var v in variants)
        {
            // 着地できる離陸点のうち、余裕 (的の上を越える高さ・縁からの距離) が大きく、歩く距離が短いものを選ぶ
            var bestQ = float.NegativeInfinity;
            var bestX = 0f;
            var bestLanding = Vector2.zero;
            var bestAir = 0f;
            List<Vector2> bestPath = null;
            foreach (var x in xs)
            {
                if (LastSimulations > budget) break;
                var y = from.Moving ? playerPos.y : from.YAt(x) + _world.PivotToFeet + 0.02f;
                var start = new Sim
                {
                    Pos = new Vector2(x, y),
                    Vel = new Vector2(0f, _world.JumpVelocity),
                    Running = v.dash,
                    Held = true,
                };
                if (!Simulate(start, target, v.dash, v.hold, v.dj, v.airDash, plan))
                    continue;

                var q = Quality(target, plan.Landing) - 0.04f * Mathf.Abs(x - playerPos.x);
                if (q > bestQ)
                {
                    bestQ = q;
                    bestX = x;
                    bestLanding = plan.Landing;
                    bestAir = plan.AirTime;
                    bestPath = new List<Vector2>(plan.Path);
                }

                if (q > 1.5f) break; // 十分な余裕
            }

            if (bestPath != null)
            {
                plan.Valid = true;
                plan.TakeoffX = bestX;
                plan.Landing = bestLanding;
                plan.AirTime = bestAir;
                plan.Path.Clear();
                plan.Path.AddRange(bestPath);
                plan.Dash = v.dash;
                plan.HoldTime = v.hold;
                plan.DoubleJump = v.dj;
                plan.AirDash = v.airDash;
                return plan;
            }

            plan.Valid = false;
        }

        return plan;
    }

    private float _lastMaxAbove;
    private bool _lastViaLedge;

    /// <summary>計画の余裕: 着地点が縁からどれだけ内側か + 的の上面をどれだけ高く越えたか (各 1 まで)。</summary>
    private float Quality(BotTarget target, Vector2 landing)
    {
        if (target.IsRect) return 1.5f;
        var depth = Mathf.Clamp01(Mathf.Min(landing.x - target.XMin, target.XMax - landing.x));
        var clearance = _lastViaLedge ? 0.4f : Mathf.Clamp01(_lastMaxAbove);
        return depth + clearance;
    }

    private void CollectFloor(float x, BotSegment from)
    {
        _ignore.Clear();
        var filter = _world.GroundFilter();
        var cols = new Collider2D[8];
        var n = Physics2D.OverlapBox(new Vector2(x, from.YAt(x) - 0.1f), new Vector2(_world.PlayerWidth + 0.4f, 0.3f), 0f, filter, cols);
        for (var i = 0; i < n; i++)
            if (BotWorld.IsOneWay(cols[i])) _ignore.Add(cols[i]);
    }

    private float TargetTop(BotTarget t) => t.IsRect ? t.Rect.yMax : t.Moving ? t.Segment.Collider.bounds.max.y : t.Segment.YAt(t.CenterX);

    private bool Simulate(Sim s, BotTarget target, bool running, float hold, bool doubleJump, bool airDash, BotJumpPlan plan)
    {
        LastSimulations++;
        plan.Path.Clear();
        var t = 0f;
        var platformVel = target.Moving ? target.Segment.Body.linearVelocity : Vector2.zero;
        var targetBottom = target.IsRect ? target.Rect.yMin : target.Moving ? target.Segment.Collider.bounds.max.y - 3f : target.Segment.YMin;
        var speed = running ? _world.RunSpeed : _world.WalkSpeed;
        s.Running = running;
        var maxAbove = float.NegativeInfinity;
        var start0Feet = s.Pos.y - _world.PivotToFeet;

        while (t < 2.6f)
        {
            t += Dt;
            var shift = platformVel * t;
            var move = Steer(s.Pos, s.Vel, target, speed, shift);

            // 二段ジャンプ・空中ダッシュは頂点で
            if (doubleJump && !s.AirJumpUsed && s.Vel.y <= 0f && t > 0.1f)
            {
                s.AirJumpUsed = true;
                s.Vel.y = _world.JumpVelocity;
                s.Held = true;
                hold = t + 10f;
            }

            if (airDash && s.DashTimer <= 0f && s.DashDir == 0 && s.Vel.y <= 0f && t > 0.1f)
            {
                s.DashDir = move >= 0f ? 1 : -1;
                s.DashTimer = _world.DashDuration;
            }

            s.Held = t < hold;

            if (s.DashTimer > 0f)
            {
                s.DashTimer -= Dt;
                s.Vel = new Vector2(s.DashDir * _world.DashSpeed, 0f);
                if (s.DashTimer <= 0f) s.Running = true;
            }
            else
            {
                var sp = s.Running ? _world.RunSpeed : _world.WalkSpeed;
                s.Vel.x = move * sp;
                var g = _world.Gravity;
                if (s.Vel.y < 0f) g *= _world.FallMul;
                else if (s.Vel.y > 0f && !s.Held) g *= _world.LowJumpMul;
                s.Vel.y = Mathf.Max(s.Vel.y - g * Dt, -_world.MaxFall);
                if (CanGlide && s.Vel.y < 0f && s.Held) s.Vel.y = Mathf.Max(s.Vel.y, -GlideFallSpeed);
            }

            if (!s.Landed && s.DashTimer <= 0f && LedgeTarget(s.Pos, move, out var ledge))
            {
                s.Pos = ledge;
                s.Vel = Vector2.zero;
                s.Landed = true;
                s.ViaLedge = true;
            }
            else
                MoveAndCollide(ref s, move);
            plan.Path.Add(s.Pos);

            // 的の上をどれだけ高く越えたか (ぎりぎりの跳躍は本番で外れやすいので、余裕のある計画だけ採る)
            if (!target.IsRect)
            {
                var f = s.Pos.y - _world.PivotToFeet;
                var sh = platformVel * t;
                if (s.Pos.x >= target.XMin + sh.x - 0.2f && s.Pos.x <= target.XMax + sh.x + 0.2f)
                {
                    var top = target.Moving ? target.Segment.Collider.bounds.max.y + sh.y : target.Segment.YAt(s.Pos.x);
                    maxAbove = Mathf.Max(maxAbove, f - top);
                }
            }

            // 成功判定
            if (target.IsRect)
            {
                var body = new Rect(s.Pos + Off(s.Facing) - CapsuleSize * 0.5f, CapsuleSize);
                if (body.Overlaps(target.Rect))
                    return Success(plan, s, t, maxAbove);
                if (s.Landed && t > 0.05f)
                    return false;
            }
            else if (target.Moving)
            {
                var feet = s.Pos.y - _world.PivotToFeet;
                var b = target.Segment.Collider.bounds;
                var top = b.max.y + shift.y;
                if (s.Vel.y <= 0f && maxAbove >= 0.2f && Mathf.Abs(feet - top) < 0.35f && s.Pos.x > b.min.x + shift.x + 0.1f && s.Pos.x < b.max.x + shift.x - 0.1f)
                    return Success(plan, s, t, maxAbove);
                if (s.Landed) return false;
            }
            else if (s.Landed)
            {
                var feet = s.Pos.y - _world.PivotToFeet;
                var seg = target.Segment;
                var ok = s.Pos.x >= seg.XMin - 0.3f && s.Pos.x <= seg.XMax + 0.3f && Mathf.Abs(seg.YAt(s.Pos.x) - feet) < 0.45f;
                ok &= s.ViaLedge || maxAbove >= 0.25f || seg.YAt(s.Pos.x) < start0Feet - 0.5f;
                return ok && Success(plan, s, t, maxAbove);
            }

            var feetY = s.Pos.y - _world.PivotToFeet;
            if (s.Vel.y < 0f && feetY < targetBottom - 2.5f)
                return false;
        }

        return false;
    }

    private bool Success(BotJumpPlan plan, Sim s, float t, float maxAbove)
    {
        _lastMaxAbove = maxAbove;
        _lastViaLedge = s.ViaLedge;
        plan.Valid = true;
        plan.AirTime = t;
        plan.Landing = s.Pos;
        return true;
    }

    private void MoveAndCollide(ref Sim s, float move)
    {
        if (Mathf.Abs(move) > 0.01f) s.Facing = move > 0f ? 1 : -1; // 向きで当たり判定がずれる (localScale の反転)
        var center = s.Pos + Off(s.Facing);

        // 横
        var dx = s.Vel.x * Dt;
        if (Mathf.Abs(dx) > 0.0001f)
        {
            var dir = new Vector2(Mathf.Sign(dx), 0f);
            var hit = Cast(center, dir, Mathf.Abs(dx) + Skin, false);
            if (hit.HasValue)
            {
                var travel = Mathf.Max(0f, hit.Value - Skin);
                center.x += dir.x * travel;
                s.Vel.x = 0f;
            }
            else center.x += dx;
        }

        // 縦
        var dy = s.Vel.y * Dt;
        if (Mathf.Abs(dy) > 0.0001f)
        {
            var down = dy < 0f;
            var dir = new Vector2(0f, Mathf.Sign(dy));
            var hit = Cast(center, dir, Mathf.Abs(dy) + Skin, down);
            if (hit.HasValue)
            {
                center.y += dir.y * Mathf.Max(0f, hit.Value - Skin);
                s.Vel.y = 0f;
                if (down) s.Landed = true;
            }
            else center.y += dy;
        }

        s.Pos = center - Off(s.Facing);
    }

    /// <summary>
    /// 崖登り: PlayerController.TryGetLedgeTarget と同じ判定 (空中で移動入力がある間、毎フレーム)。
    /// 壁に触れる前 (体の中心から WallCheckDistance+0.15) から発動するので、着地の予測に欠かせない。
    /// </summary>
    public bool LedgeTarget(Vector2 pos, float move, out Vector2 target)
    {
        target = default;
        if (Mathf.Abs(move) < 0.01f) return false;
        var dir = move > 0f ? 1 : -1;
        var feetY = pos.y - LedgeFeetOffset;
        var x = pos.x;
        var wallDist = WallCheckDistance + 0.15f;
        var filter = _world.GroundFilter();

        if (!AnyHit(new Vector2(x, feetY + 0.3f), new Vector2(dir, 0f), wallDist, filter, false)) return false;
        if (AnyHit(new Vector2(x, feetY + 1.45f), new Vector2(dir, 0f), wallDist + 0.4f, filter, false)) return false;

        var overX = x + dir * (wallDist + 0.35f);
        var n = Physics2D.Raycast(new Vector2(overX, feetY + 1.45f + 0.1f), Vector2.down, filter, _hits, 1.45f + 0.2f);
        if (n == 0) return false;
        var top = _hits[0].point.y;
        var best = _hits[0].distance;
        for (var i = 1; i < n; i++)
            if (_hits[i].distance < best) { best = _hits[i].distance; top = _hits[i].point.y; }

        var height = top - feetY;
        if (height < 0.4f || height > 1.3f) return false;

        var cols = new Collider2D[8];
        var c = Physics2D.OverlapCircle(new Vector2(overX, top + 0.75f), 0.3f, filter, cols);
        for (var i = 0; i < c; i++)
            if (!BotWorld.IsOneWay(cols[i])) return false;

        target = new Vector2(overX, top + LedgeFeetOffset + 0.02f);
        return true;
    }

    private bool AnyHit(Vector2 origin, Vector2 dir, float distance, ContactFilter2D filter, bool includeOneWay)
    {
        var n = Physics2D.Raycast(origin, dir, filter, _hits, distance);
        for (var i = 0; i < n; i++)
        {
            if (!includeOneWay && BotWorld.IsOneWay(_hits[i].collider)) continue;
            return true;
        }

        return false;
    }

    /// <summary>カプセルを dir へ distance だけ動かした時に当たる距離 (当たらなければ null)。</summary>
    private float? Cast(Vector2 center, Vector2 dir, float distance, bool movingDown)
    {
        var filter = _world.GroundFilter();
        var n = Physics2D.CapsuleCast(center, CapsuleSize, CapsuleDirection2D.Vertical, 0f, dir, filter, _hits, distance);
        float? best = null;
        for (var i = 0; i < n; i++)
        {
            var h = _hits[i];
            var c = h.collider;
            if (c == null || _ignore.Contains(c) || _world.IsDynamic(c)) continue;
            if (h.distance <= 0f) continue; // 始めから重なっている (すり抜け床の通過中・数値誤差)
            if (BotWorld.IsOneWay(c))
            {
                if (!movingDown || h.normal.y < 0.5f) continue;
            }

            if (best == null || h.distance < best.Value) best = h.distance;
        }

        return best;
    }
}
#endif
