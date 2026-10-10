#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public enum BotActionKind
{
    WalkTo,          // 同じ床の上で x まで歩く (walk_left / walk_right / 歩きでつながった隣の床)
    JumpTo,          // 計画どおりに跳ぶ/落ちる/すり抜ける (jump_to_platform / dash_jump / drop_through / go_exit)
    WaitForPlatform, // 動く足場が来るのを待って飛び乗る
    RideTo,          // 動く足場に乗って、跳べる位置に来たら跳ぶ
    Attack,
    Finisher,
    Dodge,
    Retreat,
    Heal,
    Interact,
    SuperJump,
    Climb,           // 梯子を上り下りして Target のセグメントへ (climb_ladder)
    Wait,
}

public enum BotStatus { Running, Succeeded, Failed }

/// <summary>Jev (またはローカル方針) が選んだ 1 つの行動。</summary>
public class BotAction
{
    public string Key;
    public BotActionKind Kind;
    public BotTarget Target;
    public BotJumpPlan Plan;
    public BotEntity Entity;
    public float WalkX;
    public bool GoalAction;     // 成功 = ゴールに触れる (遷移が始まる)
    public ClimbableLadder Ladder; // Climb の梯子
    public string Summary;      // Jev に見せる説明

    public override string ToString() => Key;
}

/// <summary>
/// 選ばれた行動をフレーム単位の入力に変える (VirtualPlayerInput へ書く)。毎フレームの実位置から舵を取り直す。
/// </summary>
public class BotExecutor
{
    private enum Phase { Start, Approach, PreDash, Takeoff, Air, Hold, Done }

    private readonly BotWorld _world;
    private readonly BotPhysics _physics;
    private readonly VirtualPlayerInput _input;

    public BotAction Action { get; private set; }
    public BotStatus Status { get; private set; } = BotStatus.Succeeded;
    public string Result = "";
    public string Step = "idle";
    public bool NeedsPrecision;   // 離陸の直前と空中 (高速時はフレームあたりの物理ステップを抑える)

    private Phase _phase;
    private float _t0, _phaseT0, _takeoffT;
    private float _lastProgressX, _lastProgressT;
    private bool _dashed, _airJumpUsed, _airDashUsed, _wasAirborne;
    private bool _climbUp;
    private int _attackTaps;
    private float _nextTap;
    private float _replanAt;
    private Vector2 _startPos;

    public BotExecutor(BotWorld world, BotPhysics physics, VirtualPlayerInput input)
    {
        _world = world;
        _physics = physics;
        _input = input;
    }

    private float _airSince;

    /// <summary>本当に空中か (すり抜け床の上などで接地判定が 1 フレームだけ切れるのを無視する)。</summary>
    private bool Airborne(PlayerController player) =>
        !player.IsGrounded && (Time.time - _airSince >= 0.07f || player.Rb.linearVelocity.y < -6f || player.Rb.linearVelocity.y > 3f);

    public bool Busy => Action != null && Status == BotStatus.Running;

    public void Begin(BotAction action, PlayerController player)
    {
        Action = action;
        Status = BotStatus.Running;
        Result = "";
        _phase = Phase.Start;
        _t0 = _phaseT0 = Time.time;
        _lastProgressT = Time.time;
        _lastProgressX = player.transform.position.x;
        _dashed = _airJumpUsed = _airDashUsed = _wasAirborne = false;
        _attackTaps = 0;
        _nextTap = 0f;
        _replanAt = 0f;
        _startPos = player.transform.position;
        _airSince = Time.time;
        _input.ReleaseAll();
        Step = action.Kind.ToString();
    }

    public void Abort(string reason)
    {
        if (Action == null) return;
        Status = BotStatus.Failed;
        Result = reason;
        _input.ReleaseAll();
    }

    /// <summary>結果を読み終えたら呼ぶ (次の行動まで空にする)。</summary>
    public void Clear()
    {
        Action = null;
        Status = BotStatus.Succeeded;
        NeedsPrecision = false;
    }

    private void Finish(BotStatus status, string result)
    {
        Status = status;
        Result = result;
        _input.ReleaseAll();
        NeedsPrecision = false;
        Step = "done: " + result;
    }

    /// <summary>毎フレーム (PlayerController より先に) 呼ぶ。</summary>
    public void Tick(PlayerController player)
    {
        NeedsPrecision = false;
        if (player == null || player.IsGrounded) _airSince = Time.time; // 接地していた最後の時刻
        if (Action == null || Status != BotStatus.Running || player == null)
            return;

        var elapsed = Time.time - _t0;
        if (elapsed > Timeout())
        {
            Finish(BotStatus.Failed, "timeout");
            return;
        }

        switch (Action.Kind)
        {
            case BotActionKind.WalkTo: TickWalk(player); break;
            case BotActionKind.JumpTo: TickJump(player); break;
            case BotActionKind.WaitForPlatform:
            case BotActionKind.RideTo: TickPlatform(player); break;
            case BotActionKind.Attack: TickAttack(player, false); break;
            case BotActionKind.Finisher: TickAttack(player, true); break;
            case BotActionKind.Dodge: TickDodge(player); break;
            case BotActionKind.Retreat: TickRetreat(player); break;
            case BotActionKind.Heal: TickSimple(player, PlayerButton.Heal, 1.1f); break;
            case BotActionKind.Interact: TickInteract(player); break;
            case BotActionKind.SuperJump: TickSuperJump(player); break;
            case BotActionKind.Climb: TickClimb(player); break;
            case BotActionKind.Wait:
                _input.Move = 0f;
                // 待っている間に敵が間合いに入ったら斬る
                foreach (var e in _world.Enemies)
                {
                    if (!EntityAlive(e)) continue;
                    var c = BotWorld.FirstCollider(e.Source);
                    var center = c != null ? (Vector2)c.bounds.center : e.Rect.center;
                    var d = center - (Vector2)player.transform.position;
                    if (Mathf.Abs(d.x) > 1.6f || Mathf.Abs(d.y) > 1.6f) continue;
                    _input.Move = Mathf.Sign(d.x) * 0.02f;
                    if (Time.time > _nextTap)
                    {
                        _input.Tap(PlayerButton.Attack);
                        _nextTap = Time.time + 0.22f;
                    }

                    break;
                }

                if (elapsed > Mathf.Max(0.3f, Action.WalkX)) Finish(BotStatus.Succeeded, "waited");
                break;
        }
    }

    private float Timeout() => Action.Kind switch
    {
        BotActionKind.WalkTo => 2.5f + Mathf.Abs(Action.WalkX - _startPos.x) / 5f,
        BotActionKind.JumpTo => 4f + (Action.Plan != null ? Mathf.Abs(Action.Plan.TakeoffX - _startPos.x) / 5f : 0f),
        BotActionKind.WaitForPlatform => 9f,
        BotActionKind.RideTo => 10f,
        BotActionKind.Attack => 3.5f + (Action.Entity != null ? Mathf.Abs(Action.Entity.Rect.center.x - _startPos.x) / 6f : 0f),
        BotActionKind.Interact => 6f,
        BotActionKind.Climb => 4f + Mathf.Abs(Action.WalkX - _startPos.x) / 5f
                                  + (Action.Ladder != null ? (Action.Ladder.Top - Action.Ladder.Bottom) / 3f : 0f),
        _ => 4f,
    };

    // ------------------------------------------------------------------ 歩き

    private static float Approach(float dx) => Mathf.Abs(dx) < 0.05f ? 0f : Mathf.Clamp(dx * 3f, -1f, 1f);

    private void TickWalk(PlayerController player)
    {
        var x = player.transform.position.x;
        var dx = Action.WalkX - x;
        if (Action.GoalAction && Action.Target != null && TouchesRect(player, Action.Target.Rect))
        {
            _input.Move = Mathf.Sign(dx == 0 ? 1 : dx);
            return; // 遷移を待つ (ボット本体が検出して成功にする)
        }

        if (Airborne(player))
        {
            _wasAirborne = true;
            NeedsPrecision = true;
            _input.Move = 0f;
            return;
        }

        if (_wasAirborne)
        {
            Finish(BotStatus.Failed, "fell while walking");
            return;
        }

        if (Mathf.Abs(dx) < 0.15f && !Action.GoalAction)
        {
            Finish(BotStatus.Succeeded, "arrived");
            return;
        }

        _input.Move = Action.GoalAction ? Mathf.Sign(dx) : Approach(dx);
        if (Mathf.Abs(dx) < 1.5f) NeedsPrecision = true;
        BreakBlocker(player, Mathf.Sign(dx));

        if (Mathf.Abs(x - _lastProgressX) > 0.3f)
        {
            _lastProgressX = x;
            _lastProgressT = Time.time;
        }
        else if (Time.time - _lastProgressT > 0.9f)
        {
            Finish(BotStatus.Failed, "blocked");
        }
    }

    /// <summary>行く手に壊せる物 (箱・隠し壁) があれば叩く。</summary>
    private void BreakBlocker(PlayerController player, float dir)
    {
        if (Time.time < _nextTap) return;
        // 箱・隠し壁は 1 セルずつ壊れるので、足元の高さと頭の高さの両方を見る (下の段を壊した後に頭の段が残る)
        var origin = (Vector2)player.transform.position;
        foreach (var dy in new[] { 0f, 0.45f })
        {
            var hit = Physics2D.Raycast(origin + new Vector2(0f, dy), new Vector2(dir, 0f), 1.4f, _world.GroundMask);
            if (hit.collider != null && BotWorld.IsBreakable(hit.collider) && hit.collider.GetComponent<SkillBreakable>() == null)
            {
                _input.Tap(PlayerButton.Attack);
                _nextTap = Time.time + 0.3f;
                return;
            }
        }
    }

    private bool TouchesRect(PlayerController player, Rect r)
    {
        var col = player.GetComponent<Collider2D>();
        if (col == null) return false;
        return BotWorld.ToRect(col.bounds).Overlaps(r);
    }

    // ------------------------------------------------------------------ 跳躍

    private void TickJump(PlayerController player)
    {
        var plan = Action.Plan;
        var pos = (Vector2)player.transform.position;
        var vel = player.Rb.linearVelocity;
        var target = Action.Target;

        switch (_phase)
        {
            case Phase.Start:
                _input.Move = 0f; // 走りを一度切る (計画が走り前提でなければ歩きの速さにそろえる)
                _phase = Phase.Approach;
                _phaseT0 = Time.time;
                return;

            case Phase.Approach:
            {
                if (Airborne(player))
                {
                    // 足場が消えた・押し出された: そのまま空中の舵取りへ
                    _phase = Phase.Air;
                    _takeoffT = Time.time;
                    return;
                }

                var dx = plan.TakeoffX - pos.x;
                // ダッシュ (走りの速さを得る)
                if (plan.Dash && !_dashed)
                {
                    var toward = Mathf.Sign(dx == 0 ? 1 : dx);
                    if (Mathf.Abs(dx) >= 4.2f || !SpaceBehind(player, toward))
                    {
                        _input.Move = toward;
                        _input.Tap(PlayerButton.Dash);
                    }
                    else
                    {
                        _input.Move = -toward;
                        _input.Tap(PlayerButton.Dash);
                    }

                    _dashed = true;
                    _phase = Phase.PreDash;
                    _phaseT0 = Time.time;
                    return;
                }

                if (plan.Drop)
                {
                    // 端から歩いて出る
                    _input.Move = Mathf.Sign(dx == 0 ? 1 : dx);
                    NeedsPrecision = Mathf.Abs(dx) < 2f;
                    return; // 空中になったら上の分岐で Air へ
                }

                var stepDist = Mathf.Abs(vel.x) * Mathf.Max(Time.deltaTime, 0.02f);
                if (Mathf.Abs(dx) <= Mathf.Max(0.12f, stepDist * 0.6f))
                {
                    _phase = Phase.Takeoff;
                    TickJump(player);
                    return;
                }

                var speedNeeded = plan.Dash ? 1f : 0f;
                var m = Approach(dx);
                if (plan.Dash && Mathf.Abs(m) < 0.05f) m = Mathf.Sign(dx) * 0.05f; // 走りを保つ
                _input.Move = Mathf.Abs(dx) > 0.5f ? Mathf.Sign(dx) : Mathf.Max(Mathf.Abs(m), speedNeeded * 0.3f) * Mathf.Sign(dx);
                NeedsPrecision = Mathf.Abs(dx) < 2.5f;

                if (Mathf.Abs(pos.x - _lastProgressX) > 0.3f) { _lastProgressX = pos.x; _lastProgressT = Time.time; }
                else if (Time.time - _lastProgressT > 1.0f)
                {
                    Finish(BotStatus.Failed, "blocked before takeoff");
                }

                BreakBlocker(player, Mathf.Sign(dx));
                return;
            }

            case Phase.PreDash:
                // ダッシュ中は入力をそのまま (終わったら走りで離陸点へ)
                if (Time.time - _phaseT0 > _world.DashDuration + 0.04f)
                {
                    _phase = Phase.Approach;
                    _lastProgressT = Time.time;
                }

                return;

            case Phase.Takeoff:
                NeedsPrecision = true;
                _takeoffT = Time.time;
                _takeoffPos = pos;
                if (plan.DropThrough)
                {
                    _input.Vertical = -1f;
                    _input.Move = 0f;
                    _input.Tap(PlayerButton.Jump);
                }
                else
                {
                    _input.Vertical = 0f;
                    _input.Hold(PlayerButton.Jump);
                    _input.Move = _physics.Steer(pos, new Vector2(0f, _world.JumpVelocity), target, plan.Dash ? _world.RunSpeed : _world.WalkSpeed);
                }

                _phase = Phase.Air;
                return;

            case Phase.Air:
            {
                NeedsPrecision = true;
                var t = Time.time - _takeoffT;
                if (plan != null && plan.DropThrough && t > 0.15f) _input.Vertical = 0f;

                // 遷移ゾーン (ゴール) に触れたら、そのまま遷移を待つ
                if (target.IsRect && TouchesRect(player, target.Rect))
                {
                    _input.Release(PlayerButton.Jump);
                    return;
                }

                if (plan != null && !plan.Drop && !plan.DropThrough && (t >= plan.HoldTime || vel.y <= 0f))
                    _input.Release(PlayerButton.Jump);

                if (plan != null && plan.DoubleJump && !_airJumpUsed && vel.y <= 0f && t > 0.1f)
                {
                    _airJumpUsed = true;
                    _input.Tap(PlayerButton.Jump);
                    _input.Hold(PlayerButton.Jump);
                    plan.HoldTime = t + 10f;
                }

                if (plan != null && plan.AirDash && !_airDashUsed && vel.y <= 0f && t > 0.1f)
                {
                    _airDashUsed = true;
                    _input.Tap(PlayerButton.Dash);
                }

                var speed = player.Rb != null && Mathf.Abs(vel.x) > _world.WalkSpeed + 0.5f ? _world.RunSpeed : _world.WalkSpeed;
                if (plan != null && plan.Dash) speed = _world.RunSpeed;
                var shift = target.Moving ? Vector2.zero : (Vector2?)null;
                _input.Move = _physics.Steer(pos, vel, target, speed, shift);

                if (player.IsGrounded && t > 0.08f && player.CurrentStateName != nameof(LedgeClimbState))
                {
                    _input.Release(PlayerButton.Jump);
                    if (target.IsRect)
                    {
                        if (!TouchesRect(player, target.Rect))
                            Finish(BotStatus.Failed, LandedText(player) + PlanNote());
                        return;
                    }

                    var landed = _world.SegmentUnder(player);
                    if (target.Moving ? player.GroundCollider == target.Segment.Collider : landed == target.Segment)
                        Finish(BotStatus.Succeeded, "landed " + target.Segment.Name);
                    else
                        Finish(BotStatus.Failed, LandedText(player) + PlanNote());
                }

                return;
            }
        }
    }

    private Vector2 _takeoffPos;

    private string PlanNote() =>
        Action.Plan == null ? "" : $" [plan: {Action.Plan.Describe()}; took off at ({_takeoffPos.x:F2},{_takeoffPos.y:F2})]";

    private string LandedText(PlayerController player)
    {
        var seg = _world.SegmentUnder(player);
        var p = player.transform.position;
        return $"landed on {(seg != null ? seg.Name : "?")} at ({p.x:F1},{p.y:F1})";
    }

    private bool SpaceBehind(PlayerController player, float toward)
    {
        var seg = _world.SegmentUnder(player);
        if (seg == null) return false;
        var x = player.transform.position.x;
        return toward > 0 ? x - seg.XMin > 4.2f : seg.XMax - x > 4.2f;
    }

    // ------------------------------------------------------------------ 動く足場

    private void TickPlatform(PlayerController player)
    {
        var pos = (Vector2)player.transform.position;
        if (_phase == Phase.Air || _phase == Phase.Takeoff || _phase == Phase.Approach && Action.Plan != null && Action.Plan.Valid)
        {
            TickJump(player);
            return;
        }

        _input.Move = 0f;
        if (Action.Kind == BotActionKind.WaitForPlatform)
        {
            // 待つ位置 (足場の動く範囲にいちばん近い所) まで歩く
            var dx = Action.WalkX - pos.x;
            if (Mathf.Abs(dx) > 0.2f)
            {
                _input.Move = Approach(dx);
                return;
            }
        }

        if (!player.IsGrounded) return;
        if (Time.time < _replanAt) return;
        _replanAt = Time.time + 0.06f;

        var from = _world.SegmentUnder(player);
        var plan = _physics.Plan(from, pos, Action.Target, 12, pos.x); // 今の位置から跳べるかだけを見る (足場は動き続ける)
        if (plan.Valid && Mathf.Abs(plan.TakeoffX - pos.x) < 0.3f && !plan.Dash)
        {
            Action.Plan = plan;
            _phase = Phase.Takeoff;
            TickJump(player);
        }
    }

    // ------------------------------------------------------------------ 戦闘など

    private void TickAttack(PlayerController player, bool finisher)
    {
        var e = Action.Entity;
        if (e == null || e.Source == null || !EntityAlive(e))
        {
            Finish(BotStatus.Succeeded, "target gone");
            return;
        }

        var enemyCol = BotWorld.FirstCollider(e.Source);
        var r = enemyCol != null ? BotWorld.ToRect(enemyCol.bounds) : e.Rect;
        var pos = (Vector2)player.transform.position;
        var nearX = Mathf.Clamp(pos.x, r.xMin, r.xMax);
        var dx = nearX - pos.x;
        var dir = Mathf.Sign(r.center.x - pos.x == 0 ? 1 : r.center.x - pos.x);
        var dy = r.center.y - pos.y;

        if (finisher)
        {
            _input.Move = dir * 0.02f;
            if (_attackTaps == 0) { _input.Tap(PlayerButton.Finisher); _attackTaps++; _nextTap = Time.time + 0.9f; }
            else if (Time.time > _nextTap) Finish(BotStatus.Succeeded, "finisher");
            return;
        }

        // 間合い (攻撃判定は前方 0.1..1.3、上下 ±0.5 程度)
        var inRange = Mathf.Abs(dx) < 1.0f && Mathf.Abs(dy) < 1.4f;
        if (!inRange)
        {
            _input.Move = Mathf.Abs(dx) > 0.6f ? Mathf.Sign(dx) : dir * 0.3f;
            if (dy > 1.2f && dy < 4f && player.IsGrounded && Mathf.Abs(dx) < 2.5f && Time.time > _nextTap)
            {
                _input.Tap(PlayerButton.Jump); // 上にいる敵へ跳んで斬る
                _nextTap = Time.time + 0.25f;
            }

            if (!player.IsGrounded && Mathf.Abs(dx) < 1.4f && Time.time > _nextTap)
            {
                _input.Tap(PlayerButton.Attack);
                _nextTap = Time.time + 0.22f;
            }

            return;
        }

        _input.Move = dir * 0.02f; // 敵の方を向く (ほぼ動かない)
        if (Time.time > _nextTap)
        {
            _input.Tap(PlayerButton.Attack);
            _attackTaps++;
            _nextTap = Time.time + 0.22f;
        }

        if (_attackTaps >= 8)
            Finish(BotStatus.Succeeded, "attacked x" + _attackTaps);
    }

    public static bool EntityAlive(BotEntity e)
    {
        if (e.Source == null) return false;
        if (e.Source is EnemyController enemy) return !enemy.IsDead && enemy.gameObject.activeInHierarchy;
        var col = BotWorld.FirstCollider(e.Source);
        return col != null && col.enabled && col.gameObject.activeInHierarchy;
    }

    private void TickDodge(PlayerController player)
    {
        var elapsed = Time.time - _t0;
        if (_attackTaps == 0)
        {
            var dir = Action.WalkX >= player.transform.position.x ? 1f : -1f;
            _input.Move = dir;
            _input.Tap(PlayerButton.Dash);
            _attackTaps = 1;
        }

        if (elapsed > 0.35f) Finish(BotStatus.Succeeded, "dodged");
    }

    private void TickRetreat(PlayerController player)
    {
        var dx = Action.WalkX - player.transform.position.x;
        _input.Move = Mathf.Sign(dx);
        if (Mathf.Abs(dx) < 0.3f || Time.time - _t0 > 1.2f) Finish(BotStatus.Succeeded, "retreated");
    }

    private void TickSimple(PlayerController player, PlayerButton button, float duration)
    {
        _input.Move = 0f;
        if (_attackTaps == 0) { _input.Tap(button); _attackTaps = 1; }
        if (Time.time - _t0 > duration) Finish(BotStatus.Succeeded, button.ToString());
    }

    private void TickInteract(PlayerController player)
    {
        var x = player.transform.position.x;
        var dx = Action.WalkX - x;
        if (Mathf.Abs(dx) > 0.4f && _attackTaps == 0)
        {
            _input.Move = Approach(dx);
            return;
        }

        _input.Move = 0f;
        if (_attackTaps < 3 && Time.time > _nextTap)
        {
            _input.Tap(PlayerButton.Interact);
            _attackTaps++;
            _nextTap = Time.time + 0.6f;
        }
        else if (_attackTaps >= 3 && Time.time > _nextTap)
        {
            Finish(BotStatus.Succeeded, "interacted");
        }
    }

    /// <summary>
    /// 梯子: 梯子の列まで歩く → 上 (上端からは下) + ジャンプで掴まる → 上下を押し続ける →
    /// 目的のセグメントに立ったら成功 (上は登り切って上端の床に立つ、下は下りて地面に着く)。
    /// </summary>
    private void TickClimb(PlayerController player)
    {
        var ladder = Action.Ladder;
        var target = Action.Target?.Segment;
        if (ladder == null || target == null || !ladder.isActiveAndEnabled)
        {
            Finish(BotStatus.Failed, "no ladder");
            return;
        }

        // 上るか下りるかは始めに決める (上端の近くで毎フレーム決め直すと上下に揺れて止まる)
        if (_phase == Phase.Start)
        {
            _climbUp = target.YAt(target.ClampX(ladder.CenterX)) > player.FeetY + 0.5f;
            _phase = Phase.Approach;
        }

        var up = _climbUp;
        if (!player.IsOnLadder)
        {
            if (_phase == Phase.Air)
            {
                // 掴まった後に離れた: 着いたか
                if (player.IsGrounded)
                {
                    var seg = _world.SegmentUnder(player);
                    Finish(seg == target ? BotStatus.Succeeded : BotStatus.Failed, LandedText(player));
                }

                _input.Vertical = 0f;
                return;
            }

            // 梯子の列まで歩く
            var dx = ladder.CenterX - player.transform.position.x;
            _input.Vertical = 0f;
            _input.Release(PlayerButton.Jump);
            if (Mathf.Abs(dx) > 0.3f)
            {
                _input.Move = Approach(dx);
                return;
            }

            _input.Move = 0f;
            if (player.IsGrounded && Time.time > _nextTap)
            {
                _input.Vertical = up ? 1f : -1f;
                _input.Tap(PlayerButton.Jump);
                _nextTap = Time.time + 0.4f;
            }

            return;
        }

        _phase = Phase.Air;
        _input.Move = 0f;
        _input.Vertical = up ? 1f : -1f;
    }

    private void TickSuperJump(PlayerController player)
    {
        var t = Time.time - _t0;
        if (_phase == Phase.Start)
        {
            _input.Move = 0f;
            _input.Vertical = 1f;
            _input.Hold(PlayerButton.Jump);
            if (t > 0.5f)
            {
                _input.Release(PlayerButton.Jump);
                _input.Vertical = 0f;
                _phase = Phase.Air;
                _takeoffT = Time.time;
            }

            return;
        }

        NeedsPrecision = true;
        var pos = (Vector2)player.transform.position;
        if (Action.Target != null)
            _input.Move = _physics.Steer(pos, player.Rb.linearVelocity, Action.Target, _world.WalkSpeed);
        if (player.IsGrounded && Time.time - _takeoffT > 0.2f)
            Finish(BotStatus.Succeeded, LandedText(player));
    }
}
#endif
