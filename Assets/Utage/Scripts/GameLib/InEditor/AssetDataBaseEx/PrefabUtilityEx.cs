#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;

namespace Utage
{
	//PrefabUtilityの独自拡張
    public static class PrefabUtilityEx
    {
		public static bool IsScriptableObjectOrPrefabAsset(Object asset)
		{
			if (asset is ScriptableObject so)
			{
				return true;
			}
			else if(PrefabUtility.IsPartOfPrefabAsset(asset))
			{
				return true;
			}
			return false;
		}

		public static bool IsPrefabAssetRoot(Object asset)
		{
			if (PrefabUtility.IsPartOfPrefabAsset(asset))
			{
				if (asset is GameObject go)
				{
					if (go.transform.root.gameObject == go)
					{
						//プレハブのルートの場合のみ
						return true;
					}
				}
			}
			return false;
		}
		
		//指定のプレハブアセットのルートオブジェクトを取得
		public static GameObject GetPrefabAssetRoot(Object asset)
		{
			if (PrefabUtility.IsPartOfPrefabAsset(asset))
			{
				if (asset is GameObject go) return go.transform.root.gameObject;
				if( asset is Component component) return component.gameObject.transform.root.gameObject;
			}
			return null;
		}
		
		//指定のオブジェクトがプレハブアセットの一部ならそのルートオブジェクトを取得
		public static Object GetPrefabAssetRootIfPrefabPart(Object obj)
		{
			if (PrefabUtility.IsPartOfPrefabAsset(obj))
			{
				if (obj is GameObject go) return go.transform.root.gameObject;
				if (obj is Component component) return component.gameObject.transform.root.gameObject;
			}
			return obj;
		}

		//指定のオブジェクトがプレハブアセットの一部なら、指定の型のコンポーネントを返す
		public static Object GetPrefabAssetComponentIfPrefabPart(Object asset, Type type)
		{
			if (asset == null) return asset;
			if (PrefabUtility.IsPartOfPrefabAsset(asset))
			{
				if (type == typeof(GameObject)) return asset;
				GameObject root = GetPrefabAssetRoot(asset);
				if( root.TryGetComponent(type, out Component component))
				{
					return component;
				}
				Debug.LogError($"{asset.name} has no component {type.Name}",asset);
				return null;
			}
			return asset;
		}

		//プレハブアセットのコンポーネントを削除する
		public static void RemoveComponentPrefabAssetOrNormalInstance(Component prefabComponent)
		{
			if (PrefabUtility.IsPartOfPrefabAsset(prefabComponent))
			{
				//プレハブアセットの一部
				Object.DestroyImmediate(prefabComponent, true);
			}
			else if(PrefabUtility.IsPartOfPrefabInstance(prefabComponent))
			{
				//プレハブinstanceなら何もしない
				Debug.LogWarning($"{prefabComponent.name} is prefab instance");
			}
			else
			{
				Object.DestroyImmediate(prefabComponent);
			}
		}

		//プレハブアセットのコンポーネントを削除する
		public static void RemovePrefabAssetComponent(Component prefabComponent)
		{
			if (!PrefabUtility.IsPartOfPrefabAsset(prefabComponent))
			{
				//プレハブアセットの一部でないなら何もしない
				Debug.LogError($"{prefabComponent.name} is not prefab asset");
				return;
			}
			Object.DestroyImmediate(prefabComponent, true);
		}

    }
}
#endif
