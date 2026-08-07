using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtageExtensions;


namespace Utage
{
    //シーンマネージャーの独自拡張
    public static class SceneManagerEx
    {
        //現在のアクティブシーン内から指定の型のコンポーネントを探す
        public static T GetComponentInActiveScene<T>(bool includeInactive)
            where T : class
        {
            return SceneManager.GetActiveScene().GetComponentInScene<T>(includeInactive);
        }


        //現在のアクティブシーン内から指定の型の全コンポーネントを探す
        public static IEnumerable<T> GetComponentsInActiveScene<T>(bool includeInactive)
            where T : class
        {
            return SceneManager.GetActiveScene().GetComponentsInScene<T>(includeInactive);
        }

        //現在のアクティブシーン内の全GameObjectを取得
        public static IEnumerable<GameObject> GetAllGameObjectsInActiveScene()
        {
            return SceneManager.GetActiveScene().GetAllGameObjectsInScene();
        }
    }
}
