//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	//既にロード済みの自作のファイルマネージャーと連結するサンプル
	public class SampleCustomFileManager : MonoBehaviour
	{
		//ロードを上書きするコールバックを登録
		void Awake()
		{
			AssetFileManager.GetCustomLoadManager().OnFindAsset += FindAsset;
		}

		void FindAsset(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData, ref AssetFileBase asset)
		{
			asset = new SampleCustomFile(mangager, fileInfo, settingData);
		}
	}
}
