// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using System;
using System.Collections.Generic;
using Utage;

namespace UtageExtensions
{
    //子オブジェクトを追加する拡張メソッド
    public static class UtageExtensionMethodsPrefab
    {
		/// <summary>
		/// 子の追加
		/// </summary>
		/// <param name="child">子</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChild(this Transform t, GameObject child)
		{
			return t.AddChild(child, Vector3.zero, Vector3.one);
		}

		/// <summary>
		/// 子の追加
		/// </summary>
		/// <param name="child">子</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChild(this Transform t, GameObject child, Vector3 localPosition)
		{
			return t.AddChild(child, localPosition, Vector3.one);
		}
		/// <summary>
		/// 子の追加
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="go">子</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <param name="localScale">子に設定するローカルスケール</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChild(this Transform t, GameObject child, Vector3 localPosition, Vector3 localScale)
		{
			child.transform.SetParent(t);
			child.transform.localScale = localScale;
			child.transform.localPosition = localPosition;
			if (child.transform is RectTransform)
			{
				(child.transform as RectTransform).anchoredPosition = localPosition;
			}
			child.transform.localRotation = Quaternion.identity;
			child.ChangeLayerDeep(t.gameObject.layer);
			return child;
		}

		/// <summary>
		/// プレハブからGameObjectを作成して子として追加する
		/// </summary>
		/// <param name="prefab">子を作成するためのプレハブ</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChildPrefab(this Transform t, GameObject prefab)
		{
			return t.AddChildPrefab(prefab, Vector3.zero, Vector3.one);
		}

		/// <summary>
		/// プレハブからGameObjectを作成して子として追加する
		/// </summary>
		/// <param name="prefab">子を作成するためのプレハブ</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChildPrefab(this Transform t, GameObject prefab, Vector3 localPosition)
		{
			return t.AddChildPrefab(prefab, localPosition, Vector3.one);
		}
		/// <summary>
		/// プレハブからGameObjectを作成して子として追加する
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="prefab">子を作成するためのプレハブ</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <param name="localScale">子に設定するローカルスケール</param>
		public static GameObject AddChildPrefab(this Transform t, GameObject prefab, Vector3 localPosition, Vector3 localScale)
		{
			GameObject go = GameObject.Instantiate(prefab, t) as GameObject;
			go.transform.localScale = localScale;
			go.transform.localPosition = localPosition;
			go.ChangeLayerDeep(t.gameObject.layer);
			return go;
		}



		/// <summary>
		/// プレハブからGameObjectを作成して子として追加する
		/// </summary>
		/// <param name="prefab">子を作成するためのプレハブ</param>
		/// <returns>追加済みの子</returns>
		public static T AddChildPrefab<T>(this Transform t, T prefab) where T : Component
		{
			return t.AddChildPrefab(prefab, Vector3.zero, Vector3.one);
		}

		public static T AddChildPrefab<T>(this Transform t, T prefab, Vector3 localPosition) where T : Component
		{
			return t.AddChildPrefab(prefab, localPosition, Vector3.one);
		}

		public static T AddChildPrefab<T>(this Transform t, T prefab, Vector3 localPosition, Vector3 localScale)
			where T : Component
		{
			GameObject go = t.AddChildPrefab(prefab.gameObject, localPosition, localScale);
			return go.GetComponent<T>();
		}


		/// <summary>
		/// UnityオブジェクトからGameObjectを作成して子として追加する
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="obj">子を作成するためのUnityオブジェクト</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChildUnityObject(this Transform t, UnityEngine.Object obj)
		{
			GameObject go = GameObject.Instantiate(obj, t) as GameObject;
			return go;
		}


		/// <summary>
		/// GameObjectを作成し、子として追加
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="name">作成する子の名前</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChildGameObject(this Transform t, string name)
		{
			return t.AddChildGameObject(name, Vector3.zero, Vector3.one);
		}

		/// <summary>
		/// GameObjectを作成し、子として追加
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="name">作成する子の名前</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <returns>追加済みの子</returns>
		public static GameObject AddChildGameObject(this Transform t, string name, Vector3 localPosition)
		{
			return t.AddChildGameObject(name, localPosition, Vector3.one);
		}

		/// <summary>
		/// GameObjectを作成し、子として追加
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="name">作成する子の名前</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <returns>追加済みの子</returns>
		/// <param name="localScale">子に設定するローカルスケール</param>
		public static GameObject AddChildGameObject(this Transform t, string name, Vector3 localPosition, Vector3 localScale)
		{
			GameObject go = (t is RectTransform) ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
			t.AddChild(go, localPosition, localScale);
			return go;
		}

		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを作成して子として追加
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="name">作成する子の名前</param>
		/// <returns>追加済みの子</returns>
		public static T AddChildGameObjectComponent<T>(this Transform t, string name) where T : Component
		{
			return t.AddChildGameObjectComponent<T>(name, Vector3.zero, Vector3.one);
		}

		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを作成して子として追加
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="name">作成する子の名前</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <returns>追加済みの子</returns>
		public static T AddChildGameObjectComponent<T>(this Transform t, string name, Vector3 localPosition) where T : Component
		{
			return t.AddChildGameObjectComponent<T>(name, localPosition, Vector3.one);
		}
		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを作成して子として追加
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="name">作成する子の名前</param>
		/// <param name="localPosition">子に設定するローカル座標</param>
		/// <param name="localScale">子に設定するローカルスケール</param>
		/// <returns>追加済みの子</returns>
		public static T AddChildGameObjectComponent<T>(this Transform t, string name, Vector3 localPosition, Vector3 localScale) where T : Component
		{
			GameObject go = t.AddChildGameObject(name, localPosition, localScale);
			T component = go.GetComponent<T>();
			if (component == null)
			{
				return go.AddComponent<T>();
			}
			else
			{
				return component;
			}
		}

		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを子オブジェトの先頭からコピーして指定の数になるように追加
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="count">子の数</param>
		public static void InitCloneChildren<T>(this Transform t, int count) where T : Component
		{
			//今ある子
			T[] chidlren = t.GetComponentsInChildren<T>(true);
			if (chidlren.Length <= 0)
			{
				Debug.LogError(typeof(T).Name + " is not under " + t.gameObject.name);
				return;
			}
			int addCount = Mathf.Max(0, count - chidlren.Length);
			for (int i = 0; i < addCount; ++i)
			{
				t.AddChildPrefab(chidlren[0].gameObject, chidlren[0].gameObject.transform.localPosition, chidlren[0].gameObject.transform.localScale);
			}
		}

		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを子オブジェトの先頭からコピーして指定の数になるように追加
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="count">子の数</param>
		public static void InitCloneChildren<T>(this Transform t, int count, Action<T,int> callback) where T : Component
		{
			t.InitCloneChildren<T>(count);

			T[] chidlren = t.GetComponentsInChildren<T>(true);
			for (int i = 0; i < chidlren.Length; ++i)
			{
				if (i < count)
				{
					chidlren[i].gameObject.SetActive(true);
					callback(chidlren[i], i);
				}
				else
				{
					chidlren[i].gameObject.SetActive(false);
				}
			}
		}

		/// <summary>
		/// 指定のコンポーネントつきのGameObjectを子オブジェトの先頭からコピーして、リストの指定の数になるようにし、初期化コールバックを返す
		/// </summary>
		/// <typeparam name="T">コンポーネントの型</typeparam>
		/// <param name="count">子の数</param>
		public static void InitCloneChildren<TComponent,TList>(this Transform t, IReadOnlyList<TList> list, Action<TComponent, TList> callback) where TComponent : Component
		{
			t.InitCloneChildren<TComponent>(
				Mathf.Max( list.Count,1),
				(item, index) =>
				{
					if (index < list.Count)
					{
						item.gameObject.SetActive(true);
						callback(item, list[index]);
					}
					else
					{
						item.gameObject.SetActive(false);
					}
				});
		}




        //指定のTransform以下の全ての子要素を別のTransform以下に移動する
        public static void MoveAllChildren(this Transform source, Transform destination)
        {
	        int childCount = source.childCount;
	        for (int i = childCount - 1; i >= 0; i--)
	        {
		        Transform child = source.GetChild(i);
		        child.SetParent(destination, true);
	        }
        }

        /// <summary>
        /// 子要素の全削除
        /// </summary>
        public static void DestroyChildren(this Transform t)
        {
	        List<Transform> list = new List<Transform>();
	        foreach (Transform child in t)
	        {
		        child.gameObject.SetActive(false);
		        list.Add(child);
	        }

	        t.DetachChildren();
	        foreach (Transform child in list)
	        {
		        GameObject.Destroy(child.gameObject);
	        }
        }

        /// <summary>
        /// 子要素の全削除(エディタ上ではDestroyImmediateを使う)
        /// </summary>
        /// <param name="parent">親要素</param>
        public static void DestroyChildrenInEditorOrPlayer(this Transform t)
        {
	        List<Transform> list = new List<Transform>();
	        foreach (Transform child in t)
	        {
		        child.gameObject.SetActive(false);
		        list.Add(child);
	        }

	        t.DetachChildren();
	        foreach (Transform child in list)
	        {
		        if (Application.isPlaying)
		        {
			        GameObject.Destroy(child.gameObject);
		        }
		        else
		        {
			        GameObject.DestroyImmediate(child.gameObject);
		        }
	        }
        }

    }
}
