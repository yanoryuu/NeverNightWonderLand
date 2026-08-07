// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{
	//宴のビューワー表示ウィンドウ
	public class AdvFileManagerViewer : CustomEditorWindow
	{
		const int CellHeight = 16;
		const int LineHeight = CellHeight;

		void OnEnable()
		{
			//シーン変更で描画をアップデートする
			this.autoRepaintOnSceneChange = true;
			//スクロールを有効にする
			this.isEnableScroll = true;

			this.scrollView = new OptimizedScrollView("FileManager",this, LineHeight, CellHeight);
		}

		OptimizedScrollView scrollView;


		protected override void OnGUISub()
		{
			EditorGUILayout.Space();
			int count = AssetFileManager.FileCount();
			scrollView.OnGui(count, AssetFileManager.OnGuiViewerInEditor);
			EditorGUILayout.Space();
		}
	}
}
