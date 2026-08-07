// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Utage;
using System.Collections.Generic;

namespace UtageExtensions
{
	//シーンの拡張メソッド
	public static class UtageExtensionMethodsScene
	{

        //指定のシーン内から指定の型のコンポーネントを探す
        public static T GetComponentInScene<T>(this Scene scene, bool includeInactive)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                var component = go.GetComponentInChildren<T>(includeInactive);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        //シーン内の指定の型のコンポーネント1つを取得してキャッシュ
        public static T GetComponentCacheInScene<T>(this Scene scene, ref T component, bool includeInactive)
            where T : class
        {
            if (component != null) return component;
            return GetComponentInScene<T>(scene, includeInactive);
        }


        //指定のシーン内から指定の型の全コンポーネントを探す
        public static IEnumerable<T> GetComponentsInScene<T>(this Scene scene, bool includeInactive)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var component in go.GetComponentsInChildren<T>(includeInactive))
                {
                    yield return component;
                }
            }
        }

        //指定のシーン内から指定の型のコンポーネントを持つ、指定の名前のGameObjectを探し、そのコンポーネントを返す
        public static T FindComponentInScene<T>(this Scene scene, string name, bool includeInactive)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var item in go.GetComponentsInChildren<T>(includeInactive))
                {
                    if (item is Component component)
                    {
                        if (component.gameObject.name == name)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        //指定のシーン内の全GameObjectを取得
        public static IEnumerable<GameObject> GetAllGameObjectsInScene(this Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                using (UnityEngine.Pool.ListPool<Transform>.Get(out List<Transform> list))
                {
                    go.GetComponentsInChildren(true, list);
                    foreach (var t in list)
                    {
                        yield return t.gameObject;
                    }
                }
            }
        }


        //シーン内のルートGameObjectから指定の型のコンポーネント1つを取得
        public static T GetComponentInSceneRootGameObjects<T>(this Scene scene)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                T component = go.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
        //シーン内のルートGameObjectから指定の型のコンポーネント1つを取得してキャッシュ
        public static T GetComponentCacheInSceneRootGameObjects<T>(this Scene scene, ref T component)
            where T : class
        {
            if (component != null) return component;

            return GetComponentInSceneRootGameObjects<T>(scene);
        }
#if false
        //シーン内のルートGameObjectから指定の型のコンポーネントを取得してForeachInterface
        public static void ForeachInSceneRootGameObjects<T>(this Scene scene, System.Action<T> action)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                go.ForeachInterface(action);
            }
        }
#endif
        //シーン内のルートGameObjectからの指定の型のコンポーネントを全て取得
        public static IEnumerable<T> GetComponentsInSceneRootGameObjects<T>(this Scene scene)
            where T : class
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var component in go.GetComponents<T>())
                {
                    yield return component;
                }
            }
        }
	}
}
