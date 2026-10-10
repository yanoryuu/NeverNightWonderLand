// 飾りの木箱を壊せる木箱にする候補位置 (PendingChanges 28) の下調べ。エディット中に呼ぶ (シーンは保存しない)。
// --entry CrateProbe.Run --args '["A1_H","250.6,-54;252.2,-54"]'  (x,床面y を ; 区切り)
// 床面 (下向きレイ)・箱の範囲と Ground レイヤーの重なり・近くのスポーン/出口/敵/ピックアップを出す。
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CrateProbe
{
    /// <summary>タイトルを開く (この後 editor_play で「はじめから」の経路を通せる)。</summary>
    public static string OpenTitle()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);
        return "title";
    }

    public static string Run(string scene, string points)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/Area1/{scene}.unity", OpenSceneMode.Single);
        Physics2D.SyncTransforms();
        var ground = 1 << 10;
        var sb = new StringBuilder();
        foreach (var p in points.Split(';'))
        {
            var xy = p.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            float x = xy[0], fy = xy[1];
            var ray = Physics2D.Raycast(new Vector2(x, fy + 1.5f), Vector2.down, 6f, ground);
            var floor = ray.collider ? $"{ray.point.y:F2} ({ray.collider.name})" : "none";
            var over = Physics2D.OverlapBoxAll(new Vector2(x, fy + 0.5f), new Vector2(0.9f, 0.9f), 0f, ground)
                .Select(c => c.name).ToList();
            var head = Physics2D.OverlapBoxAll(new Vector2(x, fy + 1.5f), new Vector2(0.9f, 0.9f), 0f, ground).Select(c => c.name).ToList();
            sb.Append($"({x},{fy}) floor={floor} overlap=[{string.Join(",", over)}] above=[{string.Join(",", head)}]");
            var near = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.parent != null && Vector2.Distance(t.position, new Vector2(x, fy + 0.5f)) < 3f)
                .Where(t => t.GetComponent<PlayerSpawnPoint>() || t.GetComponent<SceneTransitionZone>() || t.GetComponent<Collider2D>() && t.GetComponent<Collider2D>().isTrigger)
                .Select(t => $"{t.name}({t.position.x:F1},{t.position.y:F1})");
            sb.AppendLine(" near=[" + string.Join(", ", near) + "]");
        }
        return sb.ToString();
    }
}
