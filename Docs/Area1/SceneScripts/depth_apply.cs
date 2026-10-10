// 奥行き (§16): 素材の準備と、エリア1 の 27 シーンへの Area1StageBuilder.Depth の適用 (シーンを作り直さない)。
// 0) 素材:   --entry DepthApply.Prep  (atmos/ の全 PNG を再インポート + 霧のマテリアル)
// 1) 1 枚ずつ: --entry DepthApply.Open --args '["A1_D2"]' → 別の呼び出しで --entry DepthApply.Apply (適用 → 保存)
// 2) まとめて: --entry DepthApply.OpenBatch --args '[0,9]' → --entry DepthApply.ApplyOpen (開いている A1_ 全部に適用 → 保存 → 1 枚目以外を閉じる)
//    [0,9] [9,9] [18,9] で 27 枚。
// 3) A1_FP の浮いた観覧車の遠景 (ferris_wheel_bg) を消す: --entry DepthApply.RemoveFpWheel (A1_FP を開いた後に)
using System.Linq;
using System.Text;
using NeverNight.StageBuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DepthApply
{
    static string[] Scenes() => AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Area1" })
        .Select(AssetDatabase.GUIDToAssetPath).Where(p => System.IO.Path.GetFileName(p).StartsWith("A1_")).OrderBy(p => p).ToArray();

    public static string Prep() => Area1AssetPrep.PrepareAtmos(true);

    public static string Open(string sceneId)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/Area1/{sceneId}.unity", OpenSceneMode.Single);
        return "opened " + sceneId;
    }

    public static string Apply()
    {
        var s = SceneManager.GetActiveScene();
        var log = Area1StageBuilder.Depth(s.name);
        return log + (EditorSceneManager.SaveScene(s) ? " saved" : " SAVE FAILED");
    }

    public static string OpenBatch(int start, int count)
    {
        var paths = Scenes().Skip(start).Take(count).ToArray();
        for (var i = 0; i < paths.Length; i++)
            EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        return "opened " + string.Join(",", paths.Select(System.IO.Path.GetFileNameWithoutExtension));
    }

    public static string ApplyOpen()
    {
        var sb = new StringBuilder();
        var opened = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Where(s => s.isLoaded && s.path.StartsWith("Assets/Scenes/Area1/A1_")).ToList();
        foreach (var s in opened)
        {
            SceneManager.SetActiveScene(s);
            sb.AppendLine(Area1StageBuilder.Depth(s.name));
            if (!EditorSceneManager.SaveScene(s))
                sb.AppendLine("  SAVE FAILED " + s.name);
        }
        SceneManager.SetActiveScene(opened[0]);
        foreach (var s in opened.Skip(1))
            EditorSceneManager.CloseScene(s, true);
        return sb.ToString();
    }

    public static string RemoveFpWheel()
    {
        var s = SceneManager.GetActiveScene();
        if (s.name != "A1_FP") return "A1_FP を開いてから";
        var bg = s.GetRootGameObjects().FirstOrDefault(g => g.name == "Background");
        var t = bg != null ? bg.transform.Find("ferris_wheel_bg") : null;
        if (t == null) return "ferris_wheel_bg なし";
        Object.DestroyImmediate(t.gameObject);
        return "removed ferris_wheel_bg" + (EditorSceneManager.SaveScene(s) ? " saved" : " SAVE FAILED");
    }
}
