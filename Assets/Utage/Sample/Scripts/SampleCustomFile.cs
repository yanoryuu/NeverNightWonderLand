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
	//自作のファイルマネージャーと連結するサンプル
	public class SampleCustomFile : AssetFileBase
	{
		public SampleCustomFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
			: base(mangager, fileInfo, settingData)
		{
		}


		//ロード処理
		public override IEnumerator LoadAsync(System.Action onComplete, System.Action onFailed)
		{
			IsLoadEnd = true;
			InitFromCustomFileManager();
			onComplete();
			yield break;
		}

		//ローカルまたはキャッシュあるか（つまりサーバーからDLする必要があるか）
		public override bool CheckCacheOrLocal() { return false; }

		//アンロード処理
		public override void Unload()
		{
			IsLoadEnd = false;

			//宴からの参照がなくなったということ
			//自作のファイルマネージャーのアンロード処理を呼ぶ
			//このタイミングで行う必要がなければここでおわり
		}


		//以下、自作のファイルマネージャーから、オブジェクトの参照を行う
		void InitFromCustomFileManager()
		{
			//Resources.Loadの部分を、自作のファイルマネージャーからのオブジェクト参照に切り替える
			string path = FilePathUtil.GetPathWithoutExtension(FileInfo.FileName);
			switch (FileType)
			{
				case AssetFileType.Text:        //テキスト
					Text = Resources.Load<TextAsset>(path);
					break;
				case AssetFileType.Texture:     //テクスチャ
					Texture = Resources.Load<Texture2D>(path);
					break;
				case AssetFileType.Sound:       //サウンド
					Sound = Resources.Load<AudioClip>(path);
					break;
				case AssetFileType.UnityObject:     //Unityオブジェクト（プレハブとか）
					this.UnityObject = Resources.Load(path);
					break;
				default:
					break;
			}
		}
	}
}
