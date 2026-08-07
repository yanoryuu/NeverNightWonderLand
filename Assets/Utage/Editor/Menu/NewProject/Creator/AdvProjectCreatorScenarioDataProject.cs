// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

namespace Utage
{

	//新しい宴プロジェクト設定を作る処理
	public class AdvProjectCreatorScenarioDataProject
	{
		protected AdvProjectCreator Creator { get; }

		public AdvProjectCreatorScenarioDataProject(AdvProjectCreator creator)
		{
			Creator = creator;
		}
		
		//新たなプロジェクトを作成
		public AdvScenarioDataProject CreateScenarioDataProject()
		{
			//プロジェクトファイルを設定してインポート
			var projectData = AssetDataBaseEx.FindAssetOfType<AdvScenarioDataProject>(Creator.GetProjectRelativeDir());
			AdvScenarioDataBuilderWindow.ProjectData = projectData; 
			AdvScenarioDataBuilderWindow.ImportAll();
			return projectData;
		}
	}
}
