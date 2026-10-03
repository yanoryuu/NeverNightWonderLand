// 出入り口の点検用: シーンを開き (保存しない)、各 SceneTransitionZone / TransportInteractable の位置を
// ゲームと同じ倍率 (ortho 5.5, 16:9) で撮って Assets/Screenshots/Area1/exits_<tag>_<scene>_<name>.png に保存する。
// unity command run_script ... --file Docs/Area1/SceneScripts/exit_shots.cs --entry ExitShots.Run --args '["A1_H","before"]'
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExitShots
{
    public static string Run(string sceneId, string tag)
    {
        var path = $"Assets/Scenes/Area1/{sceneId}.unity";
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != path)
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sb = new StringBuilder();
        Directory.CreateDirectory("Assets/Screenshots/Area1");
        foreach (var z in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
            sb.AppendLine(Shoot(sceneId, tag, z.name, z.transform.position, z.GetComponent<BoxCollider2D>()));
        foreach (var t in Object.FindObjectsByType<TransportInteractable>(FindObjectsSortMode.None))
            sb.AppendLine(Shoot(sceneId, tag, t.name, t.transform.position, null));
        return sb.ToString();
    }

    /// <summary>任意のパスのシーン (例: git の旧版を一時フォルダに出した物) を開いて撮る。</summary>
    public static string RunPath(string path, string sceneId, string tag)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sb = new StringBuilder();
        foreach (var z in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
            sb.AppendLine(Shoot(sceneId, tag, z.name, z.transform.position, z.GetComponent<BoxCollider2D>()));
        foreach (var t in Object.FindObjectsByType<TransportInteractable>(FindObjectsSortMode.None))
            sb.AppendLine(Shoot(sceneId, tag, t.name, t.transform.position, null));
        return sb.ToString();
    }

    public static string At(string sceneId, string tag, string name, float x, float y)
    {
        var path = $"Assets/Scenes/Area1/{sceneId}.unity";
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != path)
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        return Shoot(sceneId, tag, name, new Vector3(x, y, 0), null);
    }

    static string Shoot(string sceneId, string tag, string name, Vector3 pos, BoxCollider2D box)
    {
        var center = pos;
        // 横の出口 (縦長のゾーン) は床が画面の下 1/4 に来る高さで撮る (プレイヤーの目線)
        if (box != null && box.bounds.size.x < box.bounds.size.y)
            center = new Vector3(pos.x, box.bounds.min.y + 3.5f, 0);
        var go = new GameObject("__ExitShotCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(center.x, center.y, -10f);
        // 視差 (ParallaxLayer はプレイ中だけ動く) をカメラ位置に合わせて置き直す (撮影後に戻す)
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
        var file = $"Assets/Screenshots/Area1/exits_{tag}_{sceneId}_{name}.png";
        File.WriteAllBytes(file, tex.EncodeToPNG());
        cam.targetTexture = null;
        foreach (var (t, p0) in restore) t.position = p0;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
        return $"{file} at ({center.x:F1},{center.y:F1}) parallax={restore.Count}";
    }
}
