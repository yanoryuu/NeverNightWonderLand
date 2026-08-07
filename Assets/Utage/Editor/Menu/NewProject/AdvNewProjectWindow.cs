// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UtageExtensions;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Utage
{

	//「Utage」のシナリオデータ用のエクセルファイルの管理エディタウイドウ
	public class AdvNewProjectWindow : EditorWindowNoSave
	{
		[MenuItem(MenuTool.MenuToolRoot + "New Project", priority = 0)]
		static void CreateNewProject()
		{
			GetWindow(typeof(AdvNewProjectWindow), false, "New Project");
		}

		public enum ProjectType
		{
			CreateNewAdvScene,			//ADV用新規シーンを作成
			AddToCurrentScene,			//現在のシーンに追加
			CreateScenarioAssetOnly,	//シナリオ用プロジェクトファイルのみ作成
		};
		public ProjectType CreateType { get; private set; }

		public string NewProjectName { get; private set; } = "";
		
		AdvProjectTemplateSettings SelectedSettings
		{
			get
			{
				switch (CreateType)
				{
					case ProjectType.CreateNewAdvScene:
						return NewAdvSceneSettings;
					case ProjectType.AddToCurrentScene:
						return AddToCurrentSceneSettings;
					case ProjectType.CreateScenarioAssetOnly:
						return ScenarioAssetOnlySettings;
					default:
						Debug.LogError($"Unknown Type {CreateType}");
						return null;
				}
			}
		}

		AdvProjectTemplateSettingsNewScene NewAdvSceneSettings { get; set; }
		AdvProjectTemplateSettingsAddScene AddToCurrentSceneSettings { get; set; }
		AdvProjectTemplateSettingsAssetOnly ScenarioAssetOnlySettings { get; set; }

		[field: SerializeReference,UnfoldedSerializable, Space(10)]
		AdvProjectCreator CreateSettings { get; set; } = null;

		[SerializeField, Button(nameof(Create), nameof(DisableCreate), false), Space(10)]
		string create;


		void Awake()
		{
			//デフォルトのテンプレート設定をGUIDから初期化（パス指定だとファイルを移動されたときにエラーになるので）
			NewAdvSceneSettings = AssetDataBaseEx.LoadAssetByGuid<AdvProjectTemplateSettingsNewScene>("64c5c815d1387d642bf4d5ba974d9df0");
			AddToCurrentSceneSettings = AssetDataBaseEx.LoadAssetByGuid<AdvProjectTemplateSettingsAddScene>("508c38e141a78364f8cddc158967c32b");
			ScenarioAssetOnlySettings = AssetDataBaseEx.LoadAssetByGuid<AdvProjectTemplateSettingsAssetOnly>("fa3770bb8c21f884e81c10b456010c0e");
			OnChangeSelected();
		}

		void OnChangeSelected()
		{
			CreateSettings = SelectedSettings == null ? null : SelectedSettings.CreateProjectCreatorSettings();
			if (SelectedSettings == null)
			{
				Debug.LogError($"TemplateSettings is null");
				return;
			}

			if (!SelectedSettings.IsEnableCreate())
			{
				Debug.LogError($"{SelectedSettings.name} is invalid", SelectedSettings);
			}

			if (CreateSettings == null)
			{
				Debug.LogError($"{SelectedSettings.name} Failed Create settings", SelectedSettings);
			}
		}

		protected override void OnGUI()
		{
			//基本設定のUI
			DrawDefaultGui();
			
			//作成設定のUI
			base.OnGUI();
			
			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=227");
		}

		void DrawDefaultGui()
		{
			using (new EditorGuiLayoutGroupScope("Create New Project"))
			{
				GUILayout.Space(4f);
				UtageEditorToolKit.BoldLabel("Input New Project Name", GUILayout.Width(200f));
				NewProjectName = EditorGUILayout.TextField(NewProjectName);

				//作成するプロジェクトタイプの設定
				UtageEditorToolKit.BoldLabel("Select Create Type", GUILayout.Width(200f));
				ProjectType type = (ProjectType)EditorGUILayout.EnumPopup("Type", CreateType);
				if (CreateType != type)
				{
					CreateType = type;
					OnChangeSelected();
				}
			}

			GUILayout.Space(4f);
			switch (CreateType)
			{
				case ProjectType.CreateNewAdvScene:
					EditorGUILayoutUtility.ObjectField("Template Settings", NewAdvSceneSettings,
						(x)=>
						{
							NewAdvSceneSettings = x;
							OnChangeSelected();
						});
					break;
				case ProjectType.AddToCurrentScene:
					EditorGUILayoutUtility.ObjectField("Template Settings", AddToCurrentSceneSettings,
						(x) =>
						{
							AddToCurrentSceneSettings = x;
							OnChangeSelected();
						});
					break;
				case ProjectType.CreateScenarioAssetOnly:
					EditorGUILayoutUtility.ObjectField("Template Settings", ScenarioAssetOnlySettings,
						(x)=>
						{
							ScenarioAssetOnlySettings = x;
							OnChangeSelected();
						});
					break;
			}
		}


		bool DisableCreate()
		{
			if (!IsEnableProjectName()) return true;
			if (SelectedSettings == null) return true;
			if (CreateSettings == null) return true;
			return !CreateSettings.EnableCreate();
		}

		bool IsEnableProjectName()
		{
			if (string.IsNullOrEmpty(this.NewProjectName)) return false;
			if (System.IO.Directory.Exists(ToProjectDir(this.NewProjectName))) return false;
			return true;
		}

		public string ToProjectDir(string name)
		{
			return Application.dataPath + "/" + name + "/";
		}

		void Create()
		{
			CreateSettings.Create(this.NewProjectName);
		}
	}
}
