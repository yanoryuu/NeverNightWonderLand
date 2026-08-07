// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Pool;
using Utage;
using System.Collections.Generic;
using System.Linq;

namespace UtageExtensions
{
	//コンポーネントを削除する拡張コンポーネント
	public static class UtageExtensionMethodsRemoveComponent
	{
		//コンポーネンがあったら削除
        public static void SafeRemoveComponent<T>(this GameObject target, bool immediate = true) where T : Component
        {
	        T component = target.GetComponent<T>();
	        if (component != null)
	        {
		        component.RemoveComponentMySelf(immediate);
	        }
        }

        //コンポーネンを削除
        public static void RemoveComponent<T>(this GameObject target, bool immediate = true) where T : Component
        {
	        T component = target.GetComponent<T>();
	        component.RemoveComponentMySelf(immediate);
        }

        //コンポーネンを削除
        public static void RemoveComponents<T>(this GameObject target, bool immediate = true) where T : Component
        {
	        T[] components = target.GetComponents<T>();
	        foreach (var component in components)
	        {
		        component.RemoveComponentMySelf(immediate);
	        }
        }

        //コンポーネンを削除
        public static void RemoveComponentMySelf(this Component target, bool immediate = true)
        {
	        if (target != null)
	        {
		        if (immediate)
		        {
			        UnityEngine.Object.DestroyImmediate(target);
		        }
		        else
		        {
			        UnityEngine.Object.Destroy(target);
		        }
	        }
        }

	}
}
