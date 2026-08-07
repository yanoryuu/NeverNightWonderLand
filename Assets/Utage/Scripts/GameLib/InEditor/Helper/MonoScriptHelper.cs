// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{

	/// <summary>
	/// 便利クラス
	/// </summary>
	public class MonoScriptHelper
	{
		//エディタースクリプトを取得
		public static MonoScript FindEditorScript(System.Type type)
		{
			string typeName = type.Name;
			string[] paths = AssetDatabase.GetAllAssetPaths();
			foreach (string path in paths)
			{
				if (Path.GetFileNameWithoutExtension(path) == typeName)
				{
					MonoScript script = AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript)) as MonoScript;
					if (script != null && script.GetClass() == type)
					{
						return script;
					}
				}
			}
			return null;
		}

		//現在の宴アセットのルートディレクトリを取得
		//アセット丸ごと移動しても動作するように、絶対パスではなくこのディレクトリの相対を使う
		public static string CurrentUtageRootDirectory
		{
			get
			{
				MonoScript script = FindEditorScript(typeof(MonoScriptHelper));
				MainAssetInfo textAsset = new MainAssetInfo(script);
				int index = textAsset.AssetPath.LastIndexOf("Utage");
				return textAsset.AssetPath.Substring(0, index) + "Utage/";
			}
		}
	}
}
#endif
