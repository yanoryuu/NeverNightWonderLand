// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage
{

	/// 自分用に使いやすくしたアセットデータベース
	public static class AssetDataBaseEx
	{
		//指定のGUIDがプロジェクト内に存在するアセットか判定
		public static bool IsExistAssetByGuid(string guid)
		{
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			return !string.IsNullOrEmpty(assetPath);
		}

		//GUIDでアセットをロード（ロードできない場合エラーを出す）
		public static T LoadAssetByGuid<T>(string guid)
			where T : UnityEngine.Object
		{
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogError($"{guid} is not valid");
				return null;
			}
				
			var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset == null)
			{
				Debug.LogError($"The type of Assets in {assetPath} is not {typeof(T).Name}");
				return null;
			}

			return asset;
		}

		//GUIDでアセットをロード（ロードできない場合エラーを出さない）
		public static T LoadAssetByGuidNullable<T>(string guid)
			where T : UnityEngine.Object
		{
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset == null)
			{
				return null;
			}
			return asset;
		}


		//指定パスのフォルダ以下にあるアセットから、指定の型のアセットを見つけてロード
		public static T FindAssetOfType<T>(string searchFolder)
			where T : Object
		{
			return FindAssetsOfType<T>(searchFolder).FirstOrDefault();
		}

		//指定パスのフォルダ以下にあるアセットから、指定の型のアセットを全て見つけてロード
		public static IEnumerable<T> FindAssetsOfType<T>(string searchFolder)
			where T : Object
		{
			string[] guids = AssetDatabase.FindAssets("t:" + typeof(T), new[] { searchFolder });
			foreach (string guid in guids)
			{
				T asset = LoadAssetByGuid<T>(guid);
				if (asset != null)
				{
					yield return asset;
				}
			}
		}

		//指定パスのフォルダ以下にあるアセットの全てのGUIDを取得
		public static IEnumerable<string> GetAllAssetGuids(string searchFolder)
		{
			return AssetDatabase.FindAssets("", new[] { searchFolder });
		}

		//指定パスのフォルダ以下にあるアセットの全てのパスを取得
		public static IEnumerable<string> GetAllAssetPaths(string searchFolder)
		{
			foreach (var guid in GetAllAssetGuids(searchFolder))
			{
				yield return AssetDatabase.GUIDToAssetPath(guid);
			}
		}

		//指定パスのフォルダ以下にある指定の型の全てのアセットの取得
		public static IEnumerable<Object> GetAllAssets(string searchFolder)
		{
			foreach (var guid in GetAllAssetGuids(searchFolder))
			{
				yield return LoadAssetByGuid<Object>(guid);
			}
		}

		//指定フォルダ以下にある全プレハブアセットを取得
		public static IEnumerable<GameObject> GetAllInPrefabAssets(string searchFolder)
		{
			foreach (var prefabAsset in FindAssetsOfType<GameObject>(searchFolder))
			{
				if (PrefabUtility.IsPartOfPrefabAsset(prefabAsset))
				{
					yield return prefabAsset;
				}
				else
				{
					Debug.LogWarning($"{AssetDatabase.GetAssetPath(prefabAsset)} is not prefab asset", prefabAsset);
				}
			}
		}

		//指定フォルダ以下にある全プレハブアセット内の、指定の型のコンポーネントをすべて取得
		public static IEnumerable<T> GetAllComponentInPrefabAssets<T>(string searchFolder) 
			where T : class
		{
			foreach (var prefabAsset in GetAllInPrefabAssets(searchFolder))
			{
				foreach (var component in prefabAsset.GetComponentsInChildren<T>(true))
				{
					yield return component;
				}
			}
		}

		//指定のアセットを削除
		public static void DeleteAsset(Object asset)
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);
			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogError($"{asset} is not asset", asset);
				return;
			}

			if (!AssetDatabase.DeleteAsset(assetPath))
			{
				Debug.LogError($"failed to delete {assetPath}",asset);
			}
		}

		//指定のアセットを指定パスにコピー
		public static T CopyAsset<T>(T asset, string dstPath)
			where T : Object
		{
			string path = AssetDatabase.GetAssetPath(asset);
			if (!AssetDatabase.CopyAsset(path, dstPath))
			{
				Debug.LogError($"Failed to copy from {path} to {dstPath} ");
				return null;
			}
			return AssetDatabase.LoadAssetAtPath<T>(dstPath);
		}

		//指定したオブジェクトがアセットかどうかを判定
		public static bool IsAsset(Object obj)
		{
			if(obj==null) return false;
			return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
		}

		//テンプレートのGUIDからアセットをロードして、コピーしたアセットを作成する
		public static T CopyTemplateAsset<T>(string guid, string dstPath)
			where T : Object
		{
			var asset = LoadAssetByGuid<T>(guid);
			//アセットをコピー
			var newAsset = AssetDataBaseEx.CopyAsset(asset, dstPath);
			EditorUtility.SetDirty(newAsset);
			return newAsset;
		}

		//テンプレートのGUIDからアセットをロードして、コピーしたアセットを、選択中のディレクトリ以下に指定の名前で作成する
		public static T CopyTemplateAssetInSelectedDirectory<T>(string guid, string assetName)
			where T : Object
		{
			string path = UtageEditorToolKit.GetSelectedDirectory();
			path = Path.Combine(path, assetName);
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			return CopyTemplateAsset<T>(guid, path);
		}

		//指定のGUIDのプレハブをロードして、シーン内にインスタンス化
		public static GameObject InstantiateFromPrefabGuid(string guid, string gameObjectName, Transform parent=null )
		{
			GameObject prefab = AssetDataBaseEx.LoadAssetByGuid<GameObject>(guid);
			var gameObject = (parent != null) ? parent.AddChildPrefab(prefab) : Object.Instantiate(prefab);
			gameObject.name = gameObjectName;
			return gameObject;
		}
		public static T InstantiateFromPrefabGuid<T>(string guid, string gameObjectName, Transform parent = null)
		{
			var gameObject = InstantiateFromPrefabGuid(guid, gameObjectName, parent);
			return gameObject.GetComponent<T>();
		}

		//GUIDでunity packageをインポート
		public static void ImportPackageByGuid(string guid, bool interactive)
		{
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogError($"{guid} is not valid");
				return;
			}
			WrapperUnityVersion.ImportPackage(assetPath, interactive);
		}
	}
}
#endif
