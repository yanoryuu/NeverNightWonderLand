// 敵の配置の点検用: シーンを開き (保存しない)、指定位置をゲームより広め (ortho 8, 16:9) に撮って
// Assets/Screenshots/Area1/enemies_<tag>_<scene>_<name>.png に保存する。敵の位置に赤い枠 (Gizmo 代わりの一時スプライトは使わず、撮影後に py で描く) は付けない。
// unity command run_script ... --file Docs/Area1/SceneScripts/enemy_shots.cs --entry EnemyShots.Many --args '["A1_W1","before","floor:30:4;air:20:14"]'
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyShots
{
    public static string Many(string sceneId, string tag, string spots)
    {
        var path = $"Assets/Scenes/Area1/{sceneId}.unity";
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != path)
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Directory.CreateDirectory("Assets/Screenshots/Area1");
        var sb = new StringBuilder();
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            sb.AppendLine($"enemy {e.name} ({e.transform.position.x:0.##},{e.transform.position.y:0.##})");
        foreach (var spot in spots.Split(';'))
        {
            var p = spot.Split(':');
            sb.AppendLine(Shoot(sceneId, tag, p[0], new Vector3(float.Parse(p[1]), float.Parse(p[2]), 0)));
        }
        return sb.ToString();
    }

    static string Shoot(string sceneId, string tag, string name, Vector3 center)
    {
        var go = new GameObject("__EnemyShotCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(center.x, center.y, -10f);
        var restore = new System.Collections.Generic.List<(Transform t, Vector3 p)>();
        foreach (var pl in Object.FindObjectsByType<NeverNight.Stage.ParallaxLayer>(FindObjectsSortMode.None))
        {
            var so = new UnityEditor.SerializedObject(pl);
            var f = so.FindProperty("_factor").vector2Value;
            var a = so.FindProperty("_anchor").vector2Value;
            var bp = pl.transform.position;
            restore.Add((pl.transform, bp));
            var d = (Vector2)center - a;
            pl.transform.position = new Vector3(bp.x + d.x * f.x, bp.y + d.y * f.y, bp.z);
        }
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        var file = $"Assets/Screenshots/Area1/enemies_{tag}_{sceneId}_{name}.png";
        File.WriteAllBytes(file, tex.EncodeToPNG());
        cam.targetTexture = null;
        foreach (var (t, p0) in restore) t.position = p0;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
        return $"{file} center ({center.x:F1},{center.y:F1})";
    }
}
