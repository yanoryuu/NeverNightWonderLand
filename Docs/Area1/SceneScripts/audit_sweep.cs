// 連続撮影の点検 (§16 Audit2)。エディット中のシーンを、カメラを動かしながら連続で撮る (ParallaxLayer の式をその場で再現)。
// Sweep.Plan(step) → カメラ位置の一覧を作る / Sweep.Render(from, count, outDir) → 撮る。保存はしない (視差の位置は最後に戻す)。
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeverNight.Stage;
using NeverNight.StageBuild;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Sweep
{
    public static List<Vector3> Pts = new List<Vector3>();   // x, y, room index
    public static List<string> RoomIds = new List<string>();

    static float MinY()
    {
        var cl = Object.FindAnyObjectByType<CameraLimit>();
        if (cl == null) return 2f;
        return new SerializedObject(cl).FindProperty("_minY").floatValue;
    }

    /// <summary>部屋ごとに、プレイヤーが居られる範囲 + カメラのずらし (2,2) でカメラ位置を step ごとに並べる (横に掃く行を下から)。</summary>
    public static string Plan(float step)
    {
        var id = SceneManager.GetActiveScene().name;
        var rooms = Area1Layout.Load().RoomsInScene(id).ToList();
        var minY = MinY();
        Pts.Clear(); RoomIds.Clear();
        var seen = new HashSet<(int, int)>();
        var terrain = GameObject.Find("Grid/Terrain")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        bool Open(float px, float py) => terrain == null || (!terrain.HasTile(terrain.WorldToCell(new Vector3(px, py))) && !terrain.HasTile(terrain.WorldToCell(new Vector3(px, py + 1f))));
        var sb = new StringBuilder();
        for (var ri = 0; ri < rooms.Count; ri++)
        {
            RoomIds.Add(rooms[ri].Id);
            foreach (var r in rooms[ri].Rects)
            {
                var y0 = Mathf.Max(r.yMin + 1f + 2f, minY);
                var y1 = Mathf.Max(r.yMax - 1f + 2f, y0);
                var x0 = r.xMin - 1f; var x1 = r.xMax + 3f;
                for (var y = y0; y <= y1 + 0.01f; y += step)
                for (var x = x0; x <= x1 + 0.01f; x += step)
                {
                    var key = (Mathf.RoundToInt(x / step * 2f), Mathf.RoundToInt(y / step * 2f));
                    // プレイヤー (= カメラ − (2,2)) が地形の外 (空き) に居られる位置だけ。カメラの下限の分は y を下げて探す
                    var py = Mathf.Min(y - 2f, r.yMax - 1f);
                    var ok = false;
                    for (var yy = py; yy >= r.yMin + 0.5f && yy >= py - 3f; yy -= 0.5f) if (Open(x - 2f, yy)) { ok = true; break; }
                    if (!ok) continue;
                    if (!seen.Add(key)) continue;
                    Pts.Add(new Vector3(x, y, ri));
                }
            }
            sb.Append($"{rooms[ri].Id}:{string.Join("|", rooms[ri].Rects.Select(r => $"{r.xMin},{r.yMin},{r.xMax},{r.yMax}"))} ");
        }
        return $"{id} minY={minY} pts={Pts.Count} {sb}";
    }

    class PL { public Transform T; public Vector3 Base; public Vector2 F, A; }

    public static string Render(int from, int count, string outDir, int w, int h)
    {
        var id = SceneManager.GetActiveScene().name;
        Directory.CreateDirectory(outDir);
        var pls = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).Select(p =>
        {
            var so = new SerializedObject(p);
            return new PL { T = p.transform, Base = p.transform.position, F = so.FindProperty("_factor").vector2Value, A = so.FindProperty("_anchor").vector2Value };
        }).ToList();
        var camGo = new GameObject("AuditCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 5.5f; cam.aspect = 16f / 9f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.14f, 0.14f, 0.2f);
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        var n = 0;
        try
        {
            for (var i = from; i < Mathf.Min(Pts.Count, from + count); i++)
            {
                var c = Pts[i];
                camGo.transform.position = new Vector3(c.x, c.y, -10f);
                foreach (var p in pls)
                {
                    var d = (Vector2)camGo.transform.position - p.A;
                    p.T.position = new Vector3(p.Base.x + d.x * p.F.x, p.Base.y + d.y * p.F.y, p.Base.z);
                }
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"{id}_{i:0000}_r{(int)c.z}_x{c.x:F1}_y{c.y:F1}.png"), tex.EncodeToPNG());
                n++;
            }
        }
        finally
        {
            foreach (var p in pls) p.T.position = p.Base;
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(tex);
        }
        return $"rendered {n} ({from}..{from + n - 1}) of {Pts.Count}";
    }

    /// <summary>線分に沿って step ごとに撮る (連続の確認用)。room index = 99。</summary>
    public static string Line(float x0, float y0, float x1, float y1, float step, string outDir, int w, int h)
    {
        Pts.Clear();
        var len = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        var n = Mathf.Max(1, Mathf.CeilToInt(len / step));
        for (var i = 0; i <= n; i++)
        {
            var p = Vector2.Lerp(new Vector2(x0, y0), new Vector2(x1, y1), i / (float)n);
            Pts.Add(new Vector3(p.x, p.y, 99));
        }
        return Render(0, 100000, outDir, w, h);
    }

    /// <summary>Plan + 全部 Render を 1 回で。</summary>
    public static string All(float step, string outDir, int w, int h)
    {
        var plan = Plan(step);
        return plan + "\n" + Render(0, 100000, outDir, w, h);
    }
}
