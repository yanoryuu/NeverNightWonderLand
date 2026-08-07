// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	//「Utage」のシナリオデータ用のエクセルファイルの管理エディタウイドウ
	public class AdvScenarioDataBuilderWindow : EditorWindow
	{
		public static AdvScenarioDataProject ProjectData
		{
			get { return UtageEditorUserSettings.GetInstance().CurrentProject; }
			set { UtageEditorUserSettings.GetInstance().CurrentProject = value; }
		}

		/// <summary>
		/// 管理対象のリソースを再インポート
		/// </summary>
		public static void ReImportResources()
		{
			if (ProjectData)
			{
				ProjectData.ReImportResources();
			}
		}

		// インポートされたアセットが管理対象ならインポート
		public static void Import(string[] importedAssets)
		{
			//シナリオが設定されてないのでインポートしない
			if (ProjectData == null) return;

			var importSettings = UtageEditorUserSettings.GetInstance().ImportSetting;
			if (!importSettings.CheckAutoImportType())
			{
				//自動インポートが無効
				return;
			}

			if (!ProjectData.TryGetImportTarget(importedAssets, out List<string> targetFiles))
			{
				//現在のプロジェクトのアセットがないのでインポートしない
				return;
			}

			AdvScenarioImporterInEditor importer = new(ProjectData);
			if (importSettings.EnableQuickImport)
			{
				importer.Import(targetFiles);
			}
			else
			{
				importer.ImportAll();
			}
		}

		// 全てインポート
		public static void ImportAll()
		{
			//シナリオが設定されてないのでインポートしない
			if (ProjectData == null) return;

			ProjectData.ImportAll();
		}

		protected Vector2 ScrollPosition { get;set; }

		protected AdvScenarioDataProject GuiTarget
		{
			get => guiTarget;
			set
			{
				if (GuiTarget != value)
				{
					guiTarget = value;
					guiTargetEditor = Editor.CreateEditor(guiTarget);
				}
			}
		}
		AdvScenarioDataProject guiTarget;

		Editor GuiTargetEditor
		{
			get
			{
				if (guiTargetEditor == null)
				{
					guiTargetEditor = Editor.CreateEditor(GuiTarget);
				}
				return guiTargetEditor;
			}
		}
		Editor guiTargetEditor;
		
		void OnGUI()
		{
			//スクロール
			this.ScrollPosition = EditorGUILayout.BeginScrollView(this.ScrollPosition);

			GUILayout.Space(4f);
			EditorGUILayout.BeginHorizontal();
			UtageEditorToolKit.BoldLabel("Project", GUILayout.Width(80f));
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(4f);

			GuiTarget = EditorGUILayout.ObjectField("", ProjectData, typeof(AdvScenarioDataProject), false) as
					AdvScenarioDataProject;

			if (GuiTarget !=null && GuiTargetEditor != null)
			{
				//カスタム済みのOnInspectorGUIを呼ぶ
				GuiTargetEditor.OnInspectorGUI();
			}

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=230");

			EditorGUILayout.EndScrollView();
		}
	}
}
