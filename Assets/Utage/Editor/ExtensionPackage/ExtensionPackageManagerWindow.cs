// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Object = UnityEngine.Object;

namespace Utage
{
	//拡張packageの管理のためのツールウィンドウ
	//URP用のアセットなどのプロジェクトによって追加で必要になるアセットをインポートしたり、バージョンアップのために再インポートしたりする
	public class ExtensionPackageManagerWindow : EditorWindowNoSave
	{
#pragma warning disable 414
		[SerializeField, Button(nameof(UpdatePackages), nameof(DisableUpdatePackages), false)]
		string update = "";

		[SerializeField, Button(nameof(ForceUpdatePackages), false)]
		string forceUpdate = "";

		[SerializeField, Button(nameof(ClearPackageLogs), false)]
		string clearPackageLogs = "";
#pragma warning restore 414

		void OnDisable()
		{
			ExtensionPackageManager.Instance.Save();
		}

		bool DisableUpdatePackages()
		{
			return !ExtensionPackageManager.Instance.IsNeedImportPackages();
		}
		
		void UpdatePackages()
		{
			ExtensionPackageManager.Instance.ImportPackages();
		}

		void ForceUpdatePackages()
		{
			ExtensionPackageManager.Instance.ForceImportPackages();
		}

		void ClearPackageLogs()
		{
			ExtensionPackageManager.Instance.ClearPackageSaveData();
		}

		//描画更新
		protected override void OnGUI()
		{
			var manager = ExtensionPackageManager.Instance;
			//拡張パッケージの一覧を表示
			using (new EditorGuiLayoutGroupScope("Extension Package"))
			{
				GUILayout.Space(4f);
				foreach (var package in manager.Packages)
				{
					EditorGUILayout.BeginHorizontal();

					using (new EditorGUI.DisabledScope(true))
					{
						EditorGUILayout.ObjectField(package.GetPackageAsset(), typeof(Object), false);
					}
//					GUILayout.Label(package.PackageName);
					GUILayout.Label($"Version = {package.ImportedVersion}");
					if(package.CheckNeedImportPackages())
					{
						if (GUILayout.Button($"UpdateTo = {package.Version}"))
						{
							package.ImportPackages();
						}
					}
					else
					{
						GUILayout.Label($"✓ Imported");
					}
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.Space(4f);
			base.OnGUI();

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=15306");
		}

	}
}
