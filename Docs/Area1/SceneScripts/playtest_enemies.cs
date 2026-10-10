// 敵の挙動のプレイ確認 (EnemyTestScene / A1_B1 / A1_AB)。PlayA と同じ流儀。
// 例: --entry PlayEnemy.Status / --entry PlayEnemy.Tp --args '[3,1]'
//     --entry PlayEnemy.Watch --args '["Enemy_Charger", 4]' → 4 秒後に PlayEnemy.Log (0.1 秒ごとの敵と自機の状態)
//     --entry PlayEnemy.Hit --args '["Enemy_Charger", 5, 1]' (HP ダメージ, 防御ダメージ, プレイヤーの攻撃として)
//     --entry PlayEnemy.Boss / PlayEnemy.BossHits (ボスの状態 / 撃破までの手数の目安)
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class PlayEnemy
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string PlayerState()
    {
        var p = P;
        if (p == null) return "no player";
        return $"P=({p.transform.position.x:F2},{p.transform.position.y:F2}) hp={p.Health.Hp.CurrentValue}/{p.Health.MaxHp} inv={p.Health.IsInvincible} g={p.IsGrounded}";
    }

    static EnemyController Find(string name) =>
        Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.StartsWith(name));

    static string Field(Object o, string field)
    {
        if (o == null) return "-";
        var f = o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? f.GetValue(o)?.ToString() : "-";
    }

    static string EnemyState(EnemyController e)
    {
        if (e == null) return "gone";
        var rb = e.GetComponent<Rigidbody2D>();
        var sb = new StringBuilder();
        sb.Append($"{e.name} ({e.transform.position.x:F2},{e.transform.position.y:F2}) v=({rb.linearVelocity.x:F1},{rb.linearVelocity.y:F1}) hp={e.Hp.CurrentValue} gd={e.Guard.CurrentValue} brk={e.IsBroken.CurrentValue} hurt={e.IsHurt} det={e.IsPlayerDetected} ctrl={Field(e, "_state")}");
        foreach (var m in e.GetComponents<MonoBehaviour>())
        {
            if (m is IEnemyModule mod && mod.IsActive)
                sb.Append($" [{m.GetType().Name}:{Field(m, "_state")}]");
            if (m is GramineBossBehaviour gb)
                sb.Append($" [boss step={Field(gb, "_step")} stance={Field(gb, "_stance")} p2={gb.IsPhase2} inv={e.IsInvulnerable} floor={e.HpFloor}]");
        }
        return sb.ToString();
    }

    public static string Status()
    {
        Application.runInBackground = true;
        var sb = new StringBuilder(PlayerState()).AppendLine();
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x))
            sb.AppendLine(EnemyState(e));
        sb.Append($"bullets={Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length} hazards={Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length}");
        return sb.ToString();
    }

    public static string Tp(float x, float y)
    {
        Application.runInBackground = true;
        P.Teleport(new Vector2(x, y));
        P.Health.Heal(99);
        return "tp " + PlayerState();
    }

    /// <summary>seconds 秒間、HP が 3 以下になったら回復し続ける (テスト中に倒れないように)。</summary>
    public static string KeepAlive(float seconds)
    {
        P.StartCoroutine(KeepAliveCo(seconds));
        return "keepalive " + seconds;
    }

    static IEnumerator KeepAliveCo(float seconds)
    {
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            if (P.Health.Hp.CurrentValue <= 3)
                P.Health.Heal(99);
            yield return null;
        }
    }

    /// <summary>実パッド・キーボードの入力を混ぜない (つながっているパッドで勝手に歩いて部屋を出ないように)。</summary>
    public static string Isolate()
    {
        PlayerInputSource.Override = new VirtualPlayerInput { MergeDevice = false };
        return "isolated";
    }

    public static string Go(string scene, string entrance)
    {
        StageLoader.Instance.TransitionTo(scene, entrance);
        return "go " + scene;
    }

    /// <summary>まとめて: 入力の分離 + 倒れない + (x,y) へ移動 + 敵 name の記録 (interval ごと) + 被ダメージの記録 (seconds 秒)。</summary>
    public static string Setup(string name, float x, float y, float seconds, float interval)
    {
        Isolate();
        KeepAlive(3000);
        var r = Tp(x, y);
        WatchEvery(name, seconds, interval);
        DamageProbe(seconds);
        return r;
    }

    public static string Heal()
    {
        P.Health.Heal(99);
        return PlayerState();
    }

    /// <summary>敵にダメージ (プレイヤーの攻撃として)。</summary>
    public static string Hit(string name, int hp, int guard)
    {
        var e = Find(name);
        if (e == null) return "no " + name;
        e.TakeDamage(new DamageInfo(hp, guard, (Vector2)P.transform.position, P.gameObject));
        return EnemyState(e);
    }

    /// <summary>敵を移動 (テストの配置替え)。</summary>
    public static string Move(string name, float x, float y)
    {
        var e = Find(name);
        if (e == null) return "no " + name;
        e.GetComponent<Rigidbody2D>().position = new Vector2(x, y);
        e.transform.position = new Vector3(x, y, 0);
        return EnemyState(e);
    }

    /// <summary>seconds 秒間、0.1 秒ごとに敵と自機の状態を PlayerPrefs に記録する (PlayEnemy.Log で読む)。</summary>
    public static string Watch(string name, float seconds) => WatchEvery(name, seconds, 0.1f);

    public static string WatchEvery(string name, float seconds, float interval)
    {
        Application.runInBackground = true;
        PlayerPrefs.SetString("PE_log", "");
        P.StartCoroutine(WatchCo(name, seconds, interval));
        return "watching " + name;
    }

    static IEnumerator WatchCo(string name, float seconds, float interval)
    {
        var sb = new StringBuilder();
        var t0 = Time.time;
        var lastHp = P.Health.Hp.CurrentValue;
        while (Time.time - t0 < seconds)
        {
            var e = Find(name);
            var shadows = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Count(s => s.enabled && (s.name.Contains("Shadow") || s.name.Contains("Mark") || s.name.Contains("Glow") || s.name.Contains("Spot") || s.name.Contains("Range")));
            var hp = P.Health.Hp.CurrentValue;
            var p = P.transform.position;
            sb.AppendLine($"t={Time.time - t0:F1} P=({p.x:F1},{p.y:F1}) hp={hp}{(hp < lastHp ? " <<DMG " + (lastHp - hp) : "")} | {EnemyState(e).Replace("brk=False ", "").Replace("hurt=False ", "").Replace("ctrl=Acting ", "")} b={Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length} hz={Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length} mk={shadows}");
            lastHp = hp;
            if (hp <= 4) { P.Health.Heal(99); lastHp = P.Health.Hp.CurrentValue; } // 倒れないよう回復 (被ダメージの記録は続ける)
            yield return new WaitForSeconds(interval);
        }
        PlayerPrefs.SetString("PE_log", sb.ToString());
    }

    /// <summary>
    /// seconds 秒間、敵の弾が自機の 1.6 以内に来たら実際の近接攻撃の判定 (PerformAttackHit, いまの攻撃の設定) を出す。
    /// 弾が攻撃で消えたかを PlayerPrefs "PE_swing" に記録する。
    /// </summary>
    public static string SwingAtBullets(float seconds)
    {
        PlayerPrefs.SetString("PE_swing", "");
        P.StartCoroutine(SwingCo(seconds));
        return "swing armed";
    }

    static IEnumerator SwingCo(float seconds)
    {
        var sb = new StringBuilder();
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            var center = (Vector2)P.GetComponent<Collider2D>().bounds.center;
            var b = Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None)
                .FirstOrDefault(x => Vector2.Distance(x.transform.position, center) < 1.6f);
            if (b != null)
            {
                var id = b.GetInstanceID();
                Face(b.transform.position.x >= center.x ? 1 : -1);
                var hit = P.PerformAttackHit(P.CurrentMeleeProfile);
                yield return null;
                var alive = Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Any(x => x.GetInstanceID() == id);
                sb.AppendLine($"t={Time.time:F1} swing hit={hit} bulletAlive={alive} hp={P.Health.Hp.CurrentValue}");
                PlayerPrefs.SetString("PE_swing", sb.ToString());
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForFixedUpdate();
        }
        sb.AppendLine("done");
        PlayerPrefs.SetString("PE_swing", sb.ToString());
    }

    /// <summary>自機の向きを変える (テスト用。PlayerController._facing を直接書く)。</summary>
    public static void Face(int dir)
    {
        var f = typeof(PlayerController).GetField("_facing", BindingFlags.NonPublic | BindingFlags.Instance);
        var rp = (R3.ReactiveProperty<int>)f.GetValue(P);
        rp.Value = dir;
    }

    public static string SwingLog() => PlayerPrefs.GetString("PE_swing", "(empty)");

    /// <summary>実際の近接攻撃の判定を 1 回出す (向いている方向)。</summary>
    public static string Swing(string enemyName)
    {
        var e = Find(enemyName);
        if (e != null) Face(e.transform.position.x >= P.transform.position.x ? 1 : -1);
        var hit = P.PerformAttackHit(P.CurrentMeleeProfile);
        return $"hit={hit} profileHp={P.CurrentMeleeProfile.HpDamage} guard={P.CurrentMeleeProfile.GuardDamage} | " + EnemyState(Find(enemyName));
    }

    /// <summary>
    /// 敵の部品の _state が state になった瞬間に被弾 (HP 0・防御 0 = 怯むだけ) させ、前後の状態を PlayerPrefs "PE_int" に記録する。
    /// </summary>
    public static string InterruptAt(string name, string state, float timeout)
    {
        PlayerPrefs.SetString("PE_int", "(waiting)");
        P.StartCoroutine(InterruptCo(name, state, timeout));
        return "armed " + name + " @" + state;
    }

    static IEnumerator InterruptCo(string name, string state, float timeout)
    {
        var end = Time.time + timeout;
        var e = Find(name);
        var sb = new StringBuilder();
        while (Time.time < end && e != null)
        {
            var hit = e.GetComponents<MonoBehaviour>().Any(m => m is IEnemyModule && Field(m, "_state") == state);
            if (hit)
            {
                sb.AppendLine("before: " + EnemyState(e) + " shadowsOn=" + ShadowCount());
                e.TakeDamage(new DamageInfo(0, 0, (Vector2)P.transform.position, null));
                yield return new WaitForSeconds(0.05f);
                sb.AppendLine("after0.05: " + EnemyState(e) + " shadowsOn=" + ShadowCount() + " grav=" + e.GetComponent<Rigidbody2D>().gravityScale);
                yield return new WaitForSeconds(0.6f);
                sb.AppendLine("after0.65: " + EnemyState(e) + " shadowsOn=" + ShadowCount() + " grav=" + e.GetComponent<Rigidbody2D>().gravityScale);
                yield return new WaitForSeconds(2.5f);
                sb.AppendLine("after3.1: " + EnemyState(e) + " shadowsOn=" + ShadowCount());
                PlayerPrefs.SetString("PE_int", sb.ToString());
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
        PlayerPrefs.SetString("PE_int", "timeout " + EnemyState(e));
    }

    static int ShadowCount() => Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(s => s.enabled && s.name.Contains("Shadow"));

    public static string IntLog() => PlayerPrefs.GetString("PE_int", "(empty)");

    /// <summary>
    /// seconds 秒間、自機の HP が減るたびに「減った量・その時に重なっていた敵と状態・近くの弾」を PlayerPrefs "PE_dmg" に記録する。
    /// </summary>
    public static string DamageProbe(float seconds)
    {
        PlayerPrefs.SetString("PE_dmg", "");
        P.StartCoroutine(DamageProbeCo(seconds));
        return "probe";
    }

    static IEnumerator DamageProbeCo(float seconds)
    {
        var sb = new StringBuilder();
        var end = Time.time + seconds;
        var last = P.Health.Hp.CurrentValue;
        var t0 = Time.time;
        while (Time.time < end)
        {
            yield return new WaitForFixedUpdate();
            var hp = P.Health.Hp.CurrentValue;
            if (hp < last)
            {
                var pb = P.GetComponent<Collider2D>().bounds;
                sb.Append($"t={Time.time - t0:F2} -{last - hp} P=({pb.center.x:F1},{pb.center.y:F1}):");
                foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                {
                    var c = e.GetComponent<Collider2D>();
                    if (c != null && Vector2.Distance(c.bounds.center, pb.center) < 4f)
                        sb.Append($" [{EnemyState(e)} overlap={c.bounds.Intersects(pb)}]");
                }
                foreach (var b in Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None))
                    if (Vector2.Distance(b.transform.position, pb.center) < 2f) sb.Append(" [bullet]");
                foreach (var h in Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None))
                    if (Vector2.Distance(h.transform.position, pb.center) < 3f) sb.Append($" [{h.name}]");
                sb.AppendLine();
                PlayerPrefs.SetString("PE_dmg", sb.ToString());
            }
            last = P.Health.Hp.CurrentValue;
        }
        sb.AppendLine("done");
        PlayerPrefs.SetString("PE_dmg", sb.ToString());
    }

    public static string DmgLog() => PlayerPrefs.GetString("PE_dmg", "(empty)");

    public static string Log() => PlayerPrefs.GetString("PE_log", "(empty)");

    // ---- ボス
    static EnemyController BossE => Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).FirstOrDefault(e => e.GetComponent<GramineBossBehaviour>() != null);

    public static string Boss()
    {
        var b = BossE;
        var gates = Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name == "Gate" && g.scene.IsValid()).Select(g => g.activeSelf);
        return (b == null ? "no boss" : EnemyState(b)) + $" gates=[{string.Join(",", gates)}] | {PlayerState()}";
    }

    /// <summary>ボスへ n 回 (hp, guard) のダメージ。</summary>
    public static string HitBoss(int n, int hp, int guard)
    {
        var b = BossE;
        if (b == null) return "no boss";
        for (var i = 0; i < n && b != null && !b.IsDead; i++)
            b.TakeDamage(new DamageInfo(hp, guard, (Vector2)b.transform.position, P.gameObject));
        return Boss();
    }

    /// <summary>ボスの _step が step になってから delay 秒後にゲーム画面を path (プロジェクト相対) に保存する (ScreenCapture)。</summary>
    public static string ShotAtStep(string step, float delay, string path, float timeout)
    {
        PlayerPrefs.SetString("PE_shot", "(waiting " + step + ")");
        P.StartCoroutine(ShotCo(step, delay, path, timeout));
        return "armed shot @" + step;
    }

    static IEnumerator ShotCo(string step, float delay, string path, float timeout)
    {
        var end = Time.time + timeout;
        while (Time.time < end)
        {
            var b = BossE;
            var gb = b != null ? b.GetComponent<GramineBossBehaviour>() : null;
            if (gb != null && Field(gb, "_step") == step)
            {
                yield return new WaitForSeconds(delay);
                ScreenCapture.CaptureScreenshot(path);
                PlayerPrefs.SetString("PE_shot", $"shot {path} at t={Time.time:F1} " + Markers().Replace("\n", " / "));
                yield break;
            }
            yield return null;
        }
        PlayerPrefs.SetString("PE_shot", "timeout " + step);
    }

    /// <summary>
    /// ボスの _step が steps (カンマ区切り) のどれかになるたびに delay 秒後に撮る (同じ step は 1 回だけ)。prefix_<step>.png。
    /// </summary>
    public static string ShotSteps(string steps, float delay, string prefix, float timeout)
    {
        PlayerPrefs.SetString("PE_shot", "");
        P.StartCoroutine(ShotStepsCo(steps.Split(','), delay, prefix, timeout));
        return "armed " + steps;
    }

    static IEnumerator ShotStepsCo(string[] steps, float delay, string prefix, float timeout)
    {
        var done = new System.Collections.Generic.HashSet<string>();
        var sb = new StringBuilder();
        var end = Time.time + timeout;
        while (Time.time < end && done.Count < steps.Length)
        {
            var b = BossE;
            var gb = b != null ? b.GetComponent<GramineBossBehaviour>() : null;
            var st = gb != null ? Field(gb, "_step") : "";
            if (steps.Contains(st) && !done.Contains(st))
            {
                done.Add(st);
                yield return new WaitForSeconds(delay);
                var path = $"{prefix}_{st}.png";
                ScreenCapture.CaptureScreenshot(path);
                sb.AppendLine($"shot {path} t={Time.time:F1}: " + Markers().Replace("\n", " / "));
                PlayerPrefs.SetString("PE_shot", sb.ToString());
            }
            yield return null;
        }
        sb.AppendLine("done " + string.Join(",", done));
        PlayerPrefs.SetString("PE_shot", sb.ToString());
    }

    public static string ShotLog() => PlayerPrefs.GetString("PE_shot", "(empty)");

    public static string Flags(string csv) => string.Join(" ", csv.Split(',').Select(f => $"{f}={GameProgress.Has(f)}"));
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }

    /// <summary>予告表示 (影・印・光) の描画順と、そこに重なる地形の描画順。</summary>
    public static string Markers()
    {
        var sb = new StringBuilder();
        foreach (var s in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                     .Where(s => s.enabled && (s.name.Contains("Shadow") || s.name.Contains("Mark") || s.name.Contains("Glow") || s.name.Contains("Spot") || s.name.Contains("Range") || s.name.Contains("Bullet") || s.name.Contains("Knife") || s.name.Contains("Balloon") || s.name.Contains("Bomb"))))
            sb.AppendLine($"{s.name} pos=({s.transform.position.x:F1},{s.transform.position.y:F1}) layer={s.sortingLayerName} order={s.sortingOrder} col={s.color}");
        return sb.ToString();
    }
}
