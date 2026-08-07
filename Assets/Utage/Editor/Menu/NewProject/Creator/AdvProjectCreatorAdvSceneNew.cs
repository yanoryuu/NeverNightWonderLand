// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Utage
{

	//宴のシーン新しく作る処理
	public class AdvProjectCreatorAdvSceneNew : AdvProjectCreatorAdvScene 
	{
		public AdvProjectCreatorAdvSceneNew(AdvProjectCreator creator, AdvProjectCreatorAssets creatorAssets )
			:base(creator, creatorAssets)
		{
		}

		public SceneAsset SceneAsset { get; protected set; }

		//ADV用新規シーンを作成
		public Scene Create()
		{
			//シーンを開く
			string scenePath = this.Creator.GetRelativeProjectNameFilePath(".unity");
			SceneAsset = AssetDataBaseEx.CopyAsset(this.TemplateSceneAsset, scenePath);
			Scene scene = EditorSceneManagerEx.OpenSceneAsset(SceneAsset);
//			EditorSceneManagerEx.SaveActiveSceneScene();
			
			//「宴」エンジンの初期化
			InitUtageEngine();
			if (!EditorSceneManager.SaveScene(scene))
			{
				//シーンのセーブに失敗
				Debug.LogError("Failed save scene");
			}

			return scene;
		}
	}
}
