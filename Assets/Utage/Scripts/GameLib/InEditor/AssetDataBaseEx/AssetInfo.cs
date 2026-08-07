// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	/// <summary>
	/// アセット(メインアセットと、サブアセット共通)の情報
	/// </summary>
	public abstract class AssetInfo
	{
		//Objectから作成
		protected AssetInfo() { }

		//Objectから作成
		protected AssetInfo(Object asset)
		{
			Asset = asset;
		}

		//インスタンスID
#if UNITY_6000_4_OR_NEWER
		public EntityId InstanceID => this.Asset.GetEntityId();
#else
		public int InstanceID { get { return this.Asset.GetInstanceID(); } }
#endif

		//アセットオブジェクト
		public virtual Object Asset { get; protected set; }

		public abstract bool IsMainAsset { get;}

		//  アセットのラベルを全て削除します
		public void ClearLabels()
		{
			AssetDatabase.ClearLabels(Asset);
		}
		//     ラベルを設定します
		public void SetLabels(string[] labels)
		{
			AssetDatabase.SetLabels(Asset, labels);
		}

		//  関連付けられたアプリケーションでアセットを開く
		public bool OpenAsset()
		{
			return AssetDatabase.OpenAsset(Asset);
		}

		//  行を指定して、関連付けられたアプリケーションでアセットを開く
		public bool OpenAsset(int lineNumber)
		{
			return AssetDatabase.OpenAsset(Asset, lineNumber);
		}

		//  行を指定して、関連付けられたアプリケーションでアセットを開く
		public static string AssetPathToFullPath(string assetPath)
		{
			return Application.dataPath.Substring(0, Application.dataPath.Length - 6) + assetPath;
		}
	}
}
#endif
