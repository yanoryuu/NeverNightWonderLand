// 敵を置ける場所の候補を部屋ごとに列挙する (読み取りだけ。シーンは変えない)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/enemy_probe.cs --entry EnemyProbe.Run --args '["A1_W1"]' --timeout_ms 600000 --timeout 620
// - 地上の敵 (Enemy.prefab, 巡回 ±3): 同じ高さの床が中心 ±4 まで続き、上 2.5 が空いている所 (動く足場・壊れる壁の上は除く)。
// - 浮遊する敵 (FlyingEnemy.prefab): 中心 ±4 × ±2 が空中 (地形・すり抜け床なし) で、床から 2 以上離れた所。
// - どちらも スポーン/出口/移動装置 から 7、セーブ・NPC・鍛冶屋・闘技場・ボス・トゲ/蒸気/ピストン・既存の敵から 6 以上離す。
// 結果は「部屋 / 種類 / y / x の連続区間」。
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyProbe
{

    const int GroundMask = 1 << 10;

    static bool IsStableFloor(Collider2D c)
    {
        if (c == null || c.isTrigger) return false;
        if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType != RigidbodyType2D.Static) return false;
        if (c.GetComponentInParent<MovingPlatform>() || c.GetComponentInParent<SkillBreakable>() || c.GetComponentInParent<BreakableBox>()
            || c.GetComponentInParent<Door>() || c.GetComponentInParent<FlagDoor>() || c.GetComponentInParent<Piston>()
            || c.GetComponentInParent<FlagActivator>())
            return false;
        return true;
    }

    static bool SolidAt(Vector2 p, bool stableOnly)
    {
        foreach (var c in Physics2D.OverlapPointAll(p, GroundMask))
            if (!c.isTrigger && (!stableOnly || IsStableFloor(c))) return true;
        return false;
    }

    static bool BoxFree(Vector2 center, Vector2 size)
    {
        foreach (var c in Physics2D.OverlapBoxAll(center, size, 0f, GroundMask))
            if (!c.isTrigger) return false;
        return true;
    }

    public static string Run(string sceneId)
    {
        var path = $"Assets/Scenes/Area1/{sceneId}.unity";
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != sceneId)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Physics2D.SyncTransforms();

        var rooms = NeverNight.StageBuild.Area1Layout.Load().RoomsInScene(sceneId).ToList();

        // 避ける物
        var avoid = new List<(Bounds b, float d, string what)>();
        foreach (var s in Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
            avoid.Add((new Bounds(s.transform.position, Vector3.one), 7f, "spawn"));
        foreach (var t in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
            avoid.Add((Bnd(t), 7f, "exit"));
        foreach (var t in Object.FindObjectsByType<TransportInteractable>(FindObjectsSortMode.None))
            avoid.Add((Bnd(t), 7f, "transport"));
        foreach (var t in Object.FindObjectsByType<SavePoint>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 6f, "save"));
        foreach (var t in Object.FindObjectsByType<NpcTalkPoint>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 6f, "npc"));
        foreach (var t in Object.FindObjectsByType<NpcTalk>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 6f, "npc"));
        foreach (var t in Object.FindObjectsByType<Blacksmith>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 6f, "smith"));
        foreach (var t in Object.FindObjectsByType<ShopKeeper>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 6f, "shop"));
        foreach (var t in Object.FindObjectsByType<WaveArena>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 2f, "arena"));
        foreach (var t in Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 2f, "boss"));
        foreach (var t in Object.FindObjectsByType<HazardZone>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 3f, "hazard"));
        foreach (var t in Object.FindObjectsByType<Piston>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 3f, "piston"));
        foreach (var t in Object.FindObjectsByType<FallingObjectSpawner>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 1f, "falling"));
        foreach (var t in Object.FindObjectsByType<WaterZone>(FindObjectsSortMode.None)) avoid.Add((Bnd(t), 0.5f, "water"));
        foreach (var t in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            avoid.Add((new Bounds(t.transform.position, Vector3.one), 8f, "enemy:" + t.name));

        string Blocked(Vector2 p, float halfW)
        {
            foreach (var a in avoid)
            {
                var b = a.b;
                b.Expand(new Vector3(halfW * 2f, 0f, 0f));
                var dist = Mathf.Sqrt(b.SqrDistance(new Vector3(p.x, p.y, b.center.z)));
                if (dist < a.d) return a.what;
            }
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{sceneId}: enemies now = {Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length}");
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            sb.AppendLine($"  existing {e.name} ({e.transform.position.x:0.#},{e.transform.position.y:0.#})");
        foreach (var r in rooms)
        {
            foreach (var w in r.Rects)
            {
                sb.AppendLine($"[{r.Id}] x {w.xMin}..{w.xMax} y {w.yMin}..{w.yMax}");
                // 地上
                for (var y = (float)w.yMin; y < w.yMax - 2f; y += 1f)
                {
                    var xs = new List<float>();
                    for (var x = w.xMin + 4.5f; x <= w.xMax - 4.5f; x += 0.5f)
                    {
                        var ok = true;
                        for (var dx = -4f; dx <= 4f && ok; dx += 0.5f)
                        {
                            var px = x + dx;
                            if (!SolidAt(new Vector2(px, y - 0.15f), true) || SolidAt(new Vector2(px, y + 0.15f), false)) ok = false;
                        }
                        if (ok && !BoxFree(new Vector2(x, y + 1.35f), new Vector2(8f, 2.4f))) ok = false;
                        if (ok && Blocked(new Vector2(x, y + 1f), 3f) != null) ok = false;
                        if (ok) xs.Add(x);
                    }
                    Segs(sb, "ground", y, xs);
                }
                // 空中 (浮遊)
                for (var y = (float)w.yMin + 3f; y < w.yMax - 1.5f; y += 1f)
                {
                    var xs = new List<float>();
                    for (var x = w.xMin + 4.5f; x <= w.xMax - 4.5f; x += 0.5f)
                    {
                        var p = new Vector2(x, y);
                        if (!BoxFree(p, new Vector2(8f, 4f))) continue;
                        if (Blocked(p, 3f) != null) continue;
                        xs.Add(x);
                    }
                    Segs(sb, "air", y, xs);
                }
            }
        }
        return sb.ToString();
    }

    static Bounds Bnd(Component c)
    {
        var col = c.GetComponent<Collider2D>();
        if (col != null) return col.bounds;
        return new Bounds(c.transform.position, Vector3.one);
    }

    static void Segs(StringBuilder sb, string kind, float y, List<float> xs)
    {
        if (xs.Count == 0) return;
        var parts = new List<string>();
        var a = xs[0]; var prev = xs[0];
        for (var i = 1; i <= xs.Count; i++)
        {
            if (i < xs.Count && Mathf.Abs(xs[i] - prev - 0.5f) < 0.01f) { prev = xs[i]; continue; }
            parts.Add(a == prev ? $"{a}" : $"{a}..{prev}");
            if (i < xs.Count) { a = xs[i]; prev = xs[i]; }
        }
        sb.AppendLine($"  {kind} y{y}: {string.Join(", ", parts)}");
    }
}
