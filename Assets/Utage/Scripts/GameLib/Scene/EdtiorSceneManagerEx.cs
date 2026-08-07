#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Utage
{
    //エディターシーンマネージャーの独自拡張
    public static class EditorSceneManagerEx
    {
        public static bool SaveActiveScene()
        {
            return EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        public static Scene OpenSceneAsset(SceneAsset sceneAsset, OpenSceneMode mode = OpenSceneMode.Single)
        {
            return EditorSceneManager.OpenScene( AssetDatabase.GetAssetPath(sceneAsset), mode);
        }
        
        //指定のシーンアセットを開いて、アクティブシーンにマージする
        public static Scene MergeSceneAssetToActiveScene(SceneAsset sceneAsset)
        {
            Scene scene = OpenSceneAsset(sceneAsset, OpenSceneMode.Additive);
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.MergeScenes(scene, activeScene);
            return activeScene;
        }

        //シーンアセットが参照するアセットをすべて取得
        public static UnityEngine.Object[] CollectDependencies(SceneAsset sceneAsset)
        {
            return EditorUtility.CollectDependencies(new[] { sceneAsset });
        }
        public static UnityEngine.Object[] CollectDependencies(Scene scene)
        {
            return CollectDependencies(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));
        }
    }
}
#endif
