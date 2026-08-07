//FindObjectOfType系の新しいメソッドの定義バージョンは以下の通り
//Unity 2021.3.18~
//Unity 2022.2.5~
//Unity 2023.1 .0
//OR_NEWERの細かいバージョンはないので、
//2021.3と、2022.2は諦めて、2022.3移行を対象とする

#if UNITY_6000_4_OR_NEWER

#elif UNITY_2022_3_OR_NEWER
#define LEGACY_FIND_OBJECT2
#else
#define LEGACY_FIND_OBJECT
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtageExtensions;

namespace Utage
{
    //FindObjectOfType系のラップ処理
    public static class WrapperFindObject
    {
        public static T FindObjectOfType<T>()
            where T : UnityEngine.Object
        {
#if LEGACY_FIND_OBJECT
            return Object.FindObjectOfType<T>();
#elif LEGACY_FIND_OBJECT2
            //後方互換性のために速度は犠牲にして、FindFirstObjectByTypeを使う
            return Object.FindFirstObjectByType<T>();
#else
            //また変わったのでFindAnyObjectByTypeを使う
            return Object.FindAnyObjectByType<T>();
#endif
        }

        public static T[] FindObjectsOfType<T>()
            where T : UnityEngine.Object
        {

#if LEGACY_FIND_OBJECT
            return Object.FindObjectsOfType<T>();
#elif LEGACY_FIND_OBJECT2
            //後方互換性のために速度は犠牲にして、FindObjectsByTypeを使う
			return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            //また変わったのでFindObjectsInactive.Excludeを指定してFindAnyObjectByTypeを使う
            return Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
#endif
        }

        //非アクティブな物も含めてFindObjectsOfTypeする
        public static T FindObjectOfTypeIncludeInactive<T>()
            where T : UnityEngine.Object
        {
#if LEGACY_FIND_OBJECT
            //Unity2020.1以降なら、Inactiveも含めてFindObjectsOfTypeが可能
            return Object.FindObjectOfType<T>(true);
#else
            //後方互換性は気にせず、FindAnyObjectByTypeを使う
            return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
#endif
        }

//「全シーンのコンポーネント検索」を定義しようとしたが、SceneManagerではDontDestroyOnLoadのシーンを検索できないので、却下
//Object.FindObjectOfTypeはinterfaceが使えないので、将来的に必要になったときにのためにログを残しておく
#if false
        //全シーン内から指定の型のコンポーネントを探す
        public static T GetComponentInAllScenes<T>(bool includeInactive)
            where T : class
        {
            //全シーンを検索
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                var component = scene.GetComponentInScene<T>(includeInactive);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        //全シーン内から指定の型のコンポーネントを全て探す
        public static IEnumerable<T> GetComponentsInAllScenes<T>(bool includeInactive)
            where T : class
        {
            //全シーンを検索
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var component in scene.GetComponentsInScene<T>(includeInactive))
                {
                    yield return component;
                }
            }
        }

        //全シーン内の全GameObjectを取得
        public static IEnumerable<GameObject> GetAllGameObjectsInAllScene()
        {
            //全シーンを検索
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var go in scene.GetAllGameObjectsInScene())
                {
                    yield return go;
                }
            }
        }
#endif
    }
}
