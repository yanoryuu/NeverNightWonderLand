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
	//指定したTMP_FontAssetと、サブアセットの名前を変更するエディターウィンドウ
	public class TmpFontAssetRenameWindow : EditorWindowWithSave
	{

		[SerializeField]
		TMP_FontAsset fontAsset = null;

		[SerializeField]
		string newName = "";

#pragma warning disable 414
		[SerializeField, Button(nameof(Rename), nameof(DisableRename), false)]
		string rename = "";
#pragma warning restore 414


		protected override string SaveKey => nameof(TmpFontAssetRenameWindow);

		
		bool DisableRename()
		{
			return !FontAssetEditorUtil.EnableRename(fontAsset, newName);
		}
		
		void Rename()
		{
			if (FontAssetEditorUtil.RenameFontAssets(fontAsset, newName))
			{
				AssetDatabase.SaveAssets();
			}
		}
	}
}
