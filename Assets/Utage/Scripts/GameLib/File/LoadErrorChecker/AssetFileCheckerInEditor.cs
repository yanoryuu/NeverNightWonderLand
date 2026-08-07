// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	//エディタ上でロード対象のファイルが存在しているかチェックをする（宴のインポート時に存在しないファイルがないかを主にチェックする）
	public class AssetFileCheckerInEditor
	{
		private string path;
		private IAssetFileSettingData settingData;

		public AssetFileCheckerInEditor(string path, IAssetFileSettingData settingData)
		{
			this.path = path;
			this.settingData = settingData;
			if (path.Contains(" "))
			{
				Debug.LogWarning(ToErrorString("[" + path + "] contains white space"));
			}
		}

		internal void CheckError(string rootPath, bool checkExt)
		{
			string fullPath = FilePathUtil.Combine(rootPath, path);
			if (!Exist(fullPath, checkExt))
			{
				string errorMsg = string.Format("{0} is not exit", fullPath);
				Debug.LogError(ToErrorString(errorMsg));
			}
		}

		internal bool Exist(string path, bool checkExt)
		{
#if UNITY_EDITOR
			if (checkExt)
			{
				return System.IO.File.Exists(path);
//				return AssetDatabase.GetMainAssetTypeAtPath(path) != null;
			}
			else
			{
				string dir = System.IO.Path.GetDirectoryName(path);
				if (!System.IO.Directory.Exists(dir))
				{
					return false;
				}
				string filename = System.IO.Path.GetFileNameWithoutExtension(path) + ".*";
				string[] files = System.IO.Directory.GetFiles(dir, filename, System.IO.SearchOption.TopDirectoryOnly);
				return files.Length > 0;
			}
#else
			return true;
#endif
		}

		string ToErrorString(string msg)
		{
			if (settingData != null && settingData.RowData!=null)
			{
				return settingData.RowData.ToErrorString(msg);
			}
			return msg;
		}

	}
}