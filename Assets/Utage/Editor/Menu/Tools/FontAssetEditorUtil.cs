// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Utage
{
	//フォントアセットのエディタ用の拡張
	public static class FontAssetEditorUtil
	{
		public static bool RenameFontAssets( TMP_FontAsset fontAsset, string newName )
		{
			(string path,  string newPath) = GetAssetPath(fontAsset,newName);
			var material = AssetDatabase.LoadAssetAtPath<Material>(path);
			material.name = newName + " Material";

			var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
			texture.name = newName + " Atlas";
			
			var msg = AssetDatabase.RenameAsset(path, newName);
			if (!string.IsNullOrEmpty(msg))
			{
				Debug.LogError(msg);
				return false;
			}
			return true;
		}
		
		//今のフォント名に合わせて、サブアセットの名前を変更
		public static void RenameFontSubAssets(TMP_FontAsset fontAsset)
		{
			string path = AssetDatabase.GetAssetPath(fontAsset);
			var material = AssetDatabase.LoadAssetAtPath<Material>(path);
			material.name = fontAsset.name + " Material";

			var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
			texture.name = fontAsset.name + " Atlas";
		}

		public static bool EnableRename(TMP_FontAsset fontAsset, string newName)
		{
			if (fontAsset == null) return false;
			if (string.IsNullOrEmpty(newName)) return false;

			(string path, string newPath) = GetAssetPath(fontAsset, newName);
			return string.IsNullOrEmpty(AssetDatabase.ValidateMoveAsset(path, newPath));
		}

		public static (string assetPah, string newAssetPath) GetAssetPath(TMP_FontAsset fontAsset, string newName)
		{
			string path = AssetDatabase.GetAssetPath(fontAsset);
			string dir = Path.GetDirectoryName(path);
			return (path, Path.Combine(dir, newName + ".asset"));
		}
	}
}
