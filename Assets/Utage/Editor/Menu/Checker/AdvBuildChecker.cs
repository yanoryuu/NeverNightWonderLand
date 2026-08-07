// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

namespace Utage
{
	//アプリのビルド時に呼ばれる
	//Utage/Sample/Resources/Sample　アセットがあったら警告
	public class AdvBuildChecker
	{
		[PostProcessBuildAttribute(1)]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			string path = MonoScriptHelper.CurrentUtageRootDirectory + "/Sample/Resources/Sample";
			if (System.IO.Directory.Exists(path))
			{
				Debug.LogWarning( ColorUtil.ToColorTagErrorMsg( "Sample resources exist. Delete or move folder " + path));
			}
		}
	}
}
