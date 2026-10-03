using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// デバッグ用: 今開いているシーンに関係なく、タイトル画面 (TitleScene) からプレイする
/// (メニュー: NeverNight/タイトルからプレイ, Ctrl+Alt+P)。
/// EditorSceneManager.playModeStartScene を一時的にタイトルに差し替えるだけなので、開いているシーンは閉じず・保存も求めない。
/// プレイを止めると差し替えを戻す (普段の ▶ は今までどおり開いているシーンから)。
/// </summary>
public static class PlayFromTitle
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string ActiveKey = "NeverNight.PlayFromTitle.Active";

    [MenuItem("NeverNight/タイトルからプレイ %&p")]
    private static void Play()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        var title = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
        if (title == null)
        {
            EditorUtility.DisplayDialog("タイトルからプレイ", $"{TitleScenePath} が見つかりません。", "OK");
            return;
        }

        EditorSceneManager.playModeStartScene = title;
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("NeverNight/タイトルからプレイ %&p", true)]
    private static bool PlayValidate()
    {
        Menu.SetChecked("NeverNight/タイトルからプレイ", EditorApplication.isPlaying && SessionState.GetBool(ActiveKey, false));
        return true;
    }

    [InitializeOnLoadMethod]
    private static void Hook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            // 止めたら普段の ▶ (開いているシーンから) に戻す
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false))
                return;
            EditorSceneManager.playModeStartScene = null;
            SessionState.SetBool(ActiveKey, false);
        };
    }
}
