// エリア1 の中ボス (A1_MB1 カップ / A1_MB2 歯車の重馬) のプレイ確認。PlayEnemy (playtest_enemies.cs) と同じ流儀。
// シーンを開いて editor_play した後に:
//   --entry PlayMidboss.Setup --args '[54, 344]'         入力の分離 + 倒れない + (x,y) へ移動
//   --entry PlayMidboss.Trace --args '[20, 0.1]'         20 秒間、ボスの段階・行動・位置・HP と自機の被弾を記録 → PlayMidboss.Log
//   --entry PlayMidboss.Status                            今の状態
//   --entry PlayMidboss.Hit --args '["Cup1", 39, 0]'      敵にダメージ (プレイヤーの攻撃として。名前は前方一致)
//   --entry PlayMidboss.Has --args '["A1_MidBossUpper"]'  撃破フラグが立ったか
//   --entry PlayMidboss.Flag --args '["A1_MidBossUpper", false]'  撃破フラグを外す (再生し直すとボスが出る)
// 注意: 入力を分離して立たせたままだと、体当たりで押されて足場の隙間から落ち、A1_W8 へ移ることがある (MB1)。
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class PlayMidboss
{
    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    static string PlayerState()
    {
        var p = P;
        if (p == null) return "no player";
        return $"P=({p.transform.position.x:F2},{p.transform.position.y:F2}) hp={p.Health.Hp.CurrentValue}/{p.Health.MaxHp}";
    }

    static string Field(Object o, string field)
    {
        if (o == null) return "-";
        for (var t = o.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f.GetValue(o)?.ToString();
        }
        return "-";
    }

    static string EnemyState(EnemyController e)
    {
        if (e == null) return "gone";
        var sb = new StringBuilder($"{e.name} on={e.isActiveAndEnabled} ({e.transform.position.x:F1},{e.transform.position.y:F1}) hp={e.Hp.CurrentValue} gd={e.Guard.CurrentValue} brk={e.IsBroken.CurrentValue} inv={e.IsInvulnerable}");
        foreach (var b in e.GetComponents<MonoBehaviour>())
        {
            var step = Field(b, "_step");
            if (step == "-") continue;
            sb.Append($" {b.GetType().Name}:{step}");
            var phase = Field(b, "_phase");
            if (phase != "-") sb.Append($"/{phase}");
        }
        return sb.ToString();
    }

    static EnemyController[] Bosses() =>
        Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(e => e.name.StartsWith("Boss") || e.name.StartsWith("Cup") || e.transform.parent != null && e.transform.parent.name.StartsWith("Boss"))
            .OrderBy(e => e.name).ToArray();

    public static string Status()
    {
        Application.runInBackground = true;
        var sb = new StringBuilder(PlayerState()).AppendLine();
        foreach (var e in Bosses()) sb.AppendLine(EnemyState(e));
        sb.Append($"hazards={Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length}");
        return sb.ToString();
    }

    public static string Setup(float x, float y)
    {
        Application.runInBackground = true;
        PlayerInputSource.Override = new VirtualPlayerInput { MergeDevice = false };
        P.StartCoroutine(KeepAlive(3000f));
        P.Teleport(new Vector2(x, y));
        P.Health.Heal(99);
        return "setup " + PlayerState();
    }

    static IEnumerator KeepAlive(float seconds)
    {
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            if (P != null && P.Health.Hp.CurrentValue <= 3) P.Health.Heal(99);
            yield return null;
        }
    }

    public static string Tp(float x, float y)
    {
        P.Teleport(new Vector2(x, y));
        return "tp " + PlayerState();
    }

    public static string Hit(string name, int hp, int guard)
    {
        var e = Bosses().FirstOrDefault(b => b.name.StartsWith(name) && b.isActiveAndEnabled);
        if (e == null) return "no active " + name;
        e.TakeDamage(new DamageInfo(hp, guard, (Vector2)P.transform.position, P.gameObject));
        return EnemyState(e);
    }

    public static string Has(string flag) => $"{flag}={GameProgress.Has(flag)}";

    public static string Flag(string flag, bool on)
    {
        if (on) GameProgress.Set(flag); else GameProgress.Unset(flag);
        return $"{flag}={GameProgress.Has(flag)}";
    }

    public static string Trace(float seconds, float interval)
    {
        PlayerPrefs.SetString("PM_trace", "");
        P.StartCoroutine(TraceCo(seconds, interval));
        return "trace " + seconds;
    }

    static IEnumerator TraceCo(float seconds, float interval)
    {
        var sb = new StringBuilder();
        var start = Time.time;
        var lastHp = P.Health.Hp.CurrentValue;
        var lastLine = "";
        while (Time.time - start < seconds)
        {
            var hp = P.Health.Hp.CurrentValue;
            var line = string.Join(" | ", Bosses().Where(b => b.isActiveAndEnabled).Select(EnemyState));
            // 位置の細かい変化は間引き、行動が変わった時と被弾した時だけ書く
            var key = System.Text.RegularExpressions.Regex.Replace(line, @"\([-0-9.,]+\)", "");
            if (key != lastLine || hp < lastHp)
            {
                sb.AppendLine($"{Time.time - start:F1}s {(hp < lastHp ? $"HIT {lastHp}->{hp} " : "")}{PlayerState()} :: {line}");
                lastLine = key;
            }
            lastHp = hp;
            yield return new WaitForSeconds(interval);
        }
        sb.AppendLine("end " + Status().Replace("\n", " / "));
        PlayerPrefs.SetString("PM_trace", sb.ToString());
    }

    public static string Log() => PlayerPrefs.GetString("PM_trace", "(empty)");
}
