// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// ロード処理を独自カスタムするためのマネージャー
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/CustomLoadManager")]
	public class CustomLoadManager : MonoBehaviour
	{
		public AssetFileBase Find(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
		{
			if (OnFindAsset != null)
			{
				AssetFileBase asset = null;
				OnFindAsset(mangager, fileInfo, settingData, ref asset);
				if (asset != null) return asset;
			}
			return null;
		}

		public delegate void FindAsset(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData, ref AssetFileBase asset);
		public FindAsset OnFindAsset{ get; set; }
	}
}
