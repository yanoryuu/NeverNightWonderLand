// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//エディタ上でロード対象のファイルが存在しているか（宴のインポート時に存在しないファイルがないかを主にチェックする）
	public class AssetFileManagerCheckerInEditor
	{
		Dictionary<string, AssetFileCheckerInEditor> AlliFiles { get { return alliFiles; } }
		Dictionary<string, AssetFileCheckerInEditor> alliFiles = new Dictionary<string, AssetFileCheckerInEditor>();

		public void Clear()
		{
			AlliFiles.Clear();
		}

		public void AddFile(string path, IAssetFileSettingData settingData)
		{
			if (AlliFiles.ContainsKey(path))
			{
				return;
			}

			AlliFiles.Add(path,new AssetFileCheckerInEditor(path,settingData));
		}

		public void CheckAll( string rootPath, bool checkExt )
		{
			foreach (var keyValue in AlliFiles)
			{
				keyValue.Value.CheckError(rootPath, checkExt);
			}
		}
	}
}