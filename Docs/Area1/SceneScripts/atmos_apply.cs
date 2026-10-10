// 雰囲気 (§15): PlayerScene の組み立てと、エリア1 の 27 シーンへの StageAtmosphere の適用。
// 1) PlayerScene:  --entry AtmosApply.OpenPlayer → --entry AtmosApply.SetupPlayer (組んで保存)
// 2) ステージ:     --entry AtmosApply.OpenBatch --args '[0,9]' (index 0 から 9 枚を Additive で開く)
//                  → --entry AtmosApply.ApplyOpen (開いている A1_ 全部に Atmosphere(sceneId) → 保存 → 1 枚目以外を閉じる)
//    を [0,9] [9,9] [18,9] で繰り返す。種類の判定は Area1StageBuilder.AtmosphereKindOf。
// 種類だけ見る: --entry AtmosApply.Kinds
using System.Linq;
using System.Text;
using NeverNight.StageBuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class AtmosApply
{
    static string[] Scenes() => AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Area1" })
        .Select(AssetDatabase.GUIDToAssetPath).Where(p => System.IO.Path.GetFileName(p).StartsWith("A1_")).OrderBy(p => p).ToArray();

    public static string Kinds() => string.Join("\n", Scenes().Select(p =>
    {
        var id = System.IO.Path.GetFileNameWithoutExtension(p);
        return id + " = " + Area1StageBuilder.AtmosphereKindOf(id);
    }));

    public static string OpenPlayer()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PlayerScene.unity", OpenSceneMode.Single);
        return "opened PlayerScene";
    }

    public static string SetupPlayer()
    {
        var log = Area1StageBuilder.SetupPlayerAtmosphere();
        var s = SceneManager.GetSceneByName("PlayerScene");
        EditorSceneManager.SaveScene(s);
        return log + "\nsaved PlayerScene";
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
            sb.AppendLine(Area1StageBuilder.Atmosphere(s.name));
            if (!EditorSceneManager.SaveScene(s))
                sb.AppendLine("  SAVE FAILED " + s.name);
        }
        SceneManager.SetActiveScene(opened[0]);
        foreach (var s in opened.Skip(1))
            EditorSceneManager.CloseScene(s, true);
        return sb.ToString();
    }
}
