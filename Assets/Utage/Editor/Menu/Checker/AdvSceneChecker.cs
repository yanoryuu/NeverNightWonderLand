// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.IO;
using UtageExtensions;

namespace Utage
{
	//シーン変更したときに呼ばれる
	[InitializeOnLoad]
	public static class AdvSceneChecker
	{
		static AdvSceneChecker()
		{
			PostProcessEditorSceneChanged.CallbackChangeScene += OnChangeScene;
		}

		static void OnChangeScene()
		{
			AdvEngineStarter starter = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngineStarter>();
			if (starter == null) return;
			starter.OnChangeEditorScene();

			AdvScenarioDataProject project = starter.ScenarioProject;
			if (project == null)
			{
				Selection.activeObject = starter;
				Debug.LogWarning("Set using project asset to 'Editor > Scenario Data Project' ", starter);
				return;
			}

			UtageEditorUserSettings setting = UtageEditorUserSettings.GetInstance();
			if (setting == null) return;

			//宴のシーンが切り替わったら、自動で「AdvScenarioDataBuilderWindow」に設定されているプロジェクトを変更するか
			if (setting.AutoChangeProject)
			{
				CheckCurrentProject(project);
			}
		}

		//現在の宴プロジェクトを変更
		static void CheckCurrentProject(AdvScenarioDataProject project)
		{
			if (AdvScenarioDataBuilderWindow.ProjectData != project)
			{
				AdvScenarioDataBuilderWindow.ProjectData = project;
			}
		}
	}
}
