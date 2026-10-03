// QA 用: Area1 の全 27 シーンを順に開いて (保存しない) 構造データを TSV に書き出す。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/audit_scan.cs --entry AuditScan.Run --args '["<出力パス>"]' --timeout_ms 900000 --timeout 920
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class AuditScan
{
    static string F(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    static string S(string s) => (s ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " | ");

    public static string Run(string outPath)
    {
        var sb = new StringBuilder();
        foreach (var s in EditorBuildSettings.scenes)
            sb.AppendLine($"BUILD\t{s.path}\t{s.enabled}");

        var files = Directory.GetFiles("Assets/Scenes/Area1", "A1_*.unity").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToList();
        sb.AppendLine($"FILES\t{files.Count}");
        foreach (var path in files)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            string val;
            try { val = NeverNight.StageBuild.Area1StageBuilder.Validate(); } catch (System.Exception e) { val = "EXC " + e.Message; }
            foreach (var line in val.Split('\n'))
            {
                var t = line.TrimEnd('\r');
                if (t.StartsWith("ERR") || t.StartsWith("WARN") || t.StartsWith("==") || t.StartsWith("EXC"))
                    sb.AppendLine($"VAL\t{id}\t{S(t)}");
            }

            var terrain = GameObject.Find("Grid/Terrain")?.GetComponent<Tilemap>();
            var plat = GameObject.Find("Grid/Platforms")?.GetComponent<Tilemap>();
            bool Solid(float x, float y)
            {
                var c = new Vector3Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y), 0);
                if ((terrain != null && terrain.HasTile(c)) || (plat != null && plat.HasTile(c))) return true;
                foreach (var col in Physics2D.OverlapPointAll(new Vector2(x, y)))
                    if (!col.isTrigger && (col.gameObject.layer == 10 || col.gameObject.layer == 0)) return true;
                return false;
            }

            foreach (var sp in Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = sp.transform.position;
                var inside = Solid(p.x, p.y);
                float g = float.NaN;
                for (float d = 0.25f; d <= 3.01f; d += 0.25f) if (Solid(p.x, p.y - d)) { g = d; break; }
                var id2 = new SerializedObject(sp).FindProperty("_id").stringValue;
                sb.AppendLine($"SPAWN\t{id}\t{sp.name}\t{id2}\t{F(p.x)}\t{F(p.y)}\tinside={inside}\tgroundBelow={(float.IsNaN(g) ? "NONE" : F(g))}");
            }
            foreach (var z in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(z);
                var b = z.GetComponent<BoxCollider2D>().bounds;
                var missing = new System.Collections.Generic.List<int>();
                for (int x = Mathf.FloorToInt(b.min.x); x < Mathf.CeilToInt(b.max.x); x++)
                    if (!Solid(x + 0.5f, b.min.y - 0.5f)) missing.Add(x);
                sb.AppendLine($"ZONE\t{id}\t{z.name}\t{so.FindProperty("_sceneName").stringValue}\t{so.FindProperty("_entranceId").stringValue}\t{F(b.min.x)}\t{F(b.min.y)}\t{F(b.max.x)}\t{F(b.max.y)}\tmissingFloorX={string.Join(",", missing)}\tactive={z.gameObject.activeInHierarchy}");
            }
            foreach (var t in Object.FindObjectsByType<TransportInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(t);
                var req = new System.Collections.Generic.List<string>();
                var rp = so.FindProperty("_requiredFlags");
                for (int i = 0; i < rp.arraySize; i++) req.Add(rp.GetArrayElementAtIndex(i).stringValue);
                var p = t.transform.position;
                sb.AppendLine($"TRANSPORT\t{id}\t{t.name}\t{so.FindProperty("_sceneName").stringValue}\t{so.FindProperty("_entranceId").stringValue}\t{string.Join(",", req)}\t{so.FindProperty("_activateFlag").stringValue}\t{F(p.x)}\t{F(p.y)}");
            }
            foreach (var sk in Object.FindObjectsByType<SkillBreakable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(sk); var p = sk.transform.position;
                var rs = so.FindProperty("_requiredSkill");
                sb.AppendLine($"SKILLBLOCK\t{id}\t{sk.name}\t{rs.enumNames[rs.enumValueIndex]}\t{so.FindProperty("_flagId").stringValue}\t{F(p.x)}\t{F(p.y)}");
            }
            foreach (var up in Object.FindObjectsByType<UpgradePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(up); var p = up.transform.position;
                var u = so.FindProperty("_upgrade");
                sb.AppendLine($"UPGRADE\t{id}\t{up.name}\t{u.enumNames[u.enumValueIndex]}\t{so.FindProperty("_pickupId").stringValue}\t{F(p.x)}\t{F(p.y)}\tactive={up.gameObject.activeInHierarchy}");
            }
            foreach (var pk in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = pk.transform.position;
                var par = pk.transform.parent != null ? pk.transform.parent.name : "";
                sb.AppendLine($"PICKUP\t{id}\t{pk.name}\t{pk.GetType().Name}\t{new SerializedObject(pk).FindProperty("_pickupId").stringValue}\t{F(p.x)}\t{F(p.y)}\tactive={pk.gameObject.activeInHierarchy}\tparent={par}");
            }
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null) continue;
                var tn = mb.GetType().Name;
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = true;
                    if (it.propertyType != SerializedPropertyType.String) continue;
                    var pp = it.propertyPath;
                    if (pp.IndexOf("flag", System.StringComparison.OrdinalIgnoreCase) < 0 && pp.IndexOf("pickupId", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (string.IsNullOrEmpty(it.stringValue)) continue;
                    var p = mb.transform.position;
                    sb.AppendLine($"FLAG\t{id}\t{tn}\t{pp}\t{S(it.stringValue)}\t{mb.name}\t{F(p.x)}\t{F(p.y)}");
                }
            }
            foreach (var bb in Object.FindObjectsByType<BreakableBox>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = bb.transform.position;
                sb.AppendLine($"BOX\t{id}\t{bb.name}\t{new SerializedObject(bb).FindProperty("_flagId").stringValue}\t{F(p.x)}\t{F(p.y)}");
            }
        }
        File.WriteAllText(outPath, sb.ToString());
        return $"done lines={sb.ToString().Split('\n').Length}";
    }
}
