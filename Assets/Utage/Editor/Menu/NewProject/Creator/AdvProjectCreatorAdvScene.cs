// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
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

	//宴のシーンを作る処理
	public abstract class AdvProjectCreatorAdvScene
	{
		protected AdvProjectCreator Creator { get; }
		protected AdvProjectCreatorAssets CreatorAssets { get; }
		protected IAdvProjectTemplateSettingsScene SceneSettings { get; }
		protected SceneAsset TemplateSceneAsset => SceneSettings.Scene;

		//ルートオブジェクト
		protected AdvEngine Engine { get; set; }
		protected AdvEngineStarter Starter { get; set; }
		protected BootCustomProjectSetting Manager { get; set; }

		protected AdvProjectCreatorAdvScene(AdvProjectCreator creator, AdvProjectCreatorAssets creatorAssets )
		{
			Creator = creator;
			CreatorAssets = creatorAssets;
			SceneSettings = Creator.TemplateSettings as IAdvProjectTemplateSettingsScene;
			if (SceneSettings ==null)
			{
				Debug.LogError($"{Creator.TemplateSettings} is not {nameof(IAdvProjectTemplateSettingsScene)}",Creator.TemplateSettings);
			}
		}

		//ルートオブジェクトの初期化設定
		protected virtual void InitRootObjects()
		{
			Engine = SceneManagerEx.GetComponentInActiveScene<AdvEngine>(true);
			Starter = SceneManagerEx.GetComponentInActiveScene<AdvEngineStarter>(true);
			Manager = SceneManagerEx.GetComponentInActiveScene<BootCustomProjectSetting>(true);
		}


		//シーン内のAdvエンジンの初期設定
		protected virtual void InitUtageEngine()
		{
			//ルートオブジェクトの初期化設定
			InitRootObjects();


//			AdvScenarioDataExported exportedScenarioAsset = UtageEditorToolKit.LoadAssetAtPath<AdvScenarioDataExported>(GetScenarioAssetRelativePath());
//			AdvScenarioDataExported[] exportedScenarioDataTbl = { exportedScenarioAsset };
			Starter.InitOnCreate(Engine, AdvScenarioDataBuilderWindow.ProjectData.Scenarios, Creator.ProjectName);
			Starter.ScenarioProject = AdvScenarioDataBuilderWindow.ProjectData;

			if (Creator is IAdvProjectCreatorGameScreenSize gameScreenSize)
			{
				foreach (LetterBoxCamera camera in Manager.GetComponentsInChildren<LetterBoxCamera>(true))
				{
					camera.Width = camera.MaxWidth = gameScreenSize.GameScreenWidth;
					camera.Height = camera.MaxHeight = gameScreenSize.GameScreenHeight;
				}

				var screenResolution = SceneManagerEx.GetComponentInActiveScene<ScreenResolution>(true);
				screenResolution.DefaultWindowWidth = gameScreenSize.GameScreenWidth;
				screenResolution.DefaultWindowHeight = gameScreenSize.GameScreenHeight;
			}

			//セーブファイルの場所の設定
			AdvSaveManager saveManager = Engine.SaveManager;
			saveManager.DirectoryName = "Save" + Creator.ProjectName;

			AdvSystemSaveData systemSaveData = Engine.SystemSaveData;
			systemSaveData.DirectoryName = "Save" + Creator.ProjectName;

			//シークレットキーの設定
			if (Creator is IAdvProjectCreatorSecurity secretKey)
			{
				foreach (FileIOManager item in Manager.GetComponentsInChildren<FileIOManager>(true))
				{
					item.SetCryptKey(secretKey.SecretKey);
				}
			}

			//シーン内の全てのテンプレートアセットをクローンアセットに置き換える
			ReplaceAssetsFromTemplateToCloneInScene(CreatorAssets.CloneAssetPair);
		}

		
		//シーン内の全てのテンプレートアセットをクローンアセットに置き換える
		protected virtual void ReplaceAssetsFromTemplateToCloneInScene(Dictionary<Object, Object> cloneAssetPair)
		{
//			Debug.Log(System.DateTime.Now + " プレハブインスタンスを検索");
			//プレハブインスタンスを検索
			List<GameObject> prefabInstanceList = new List<GameObject>();
			foreach (GameObject go in SceneManagerEx.GetAllGameObjectsInActiveScene())
			{
				if (WrapperUnityVersion.CheckPrefabInstance(go))
				{
					GameObject prefabInstance = WrapperUnityVersion.GetOutermostPrefabInstanceRoot(go);
					if(!prefabInstanceList.Contains(prefabInstance))
					{
						prefabInstanceList.Add(prefabInstance);
					}
				}
			}

			//			Debug.Log(System.DateTime.Now + " prefabInstanceList");
			//プレハブインスタンスはいったん削除して、クローンプレハブからインスタンスを作って置き換える
			foreach (GameObject go in prefabInstanceList)
			{
				//プレハブの元となるアセットを取得
				GameObject prefabAsset = WrapperUnityVersion.GetPrefabParent(go);
				if (prefabAsset == null)
				{
					Debug.LogError(go.name + " Not fount parent Prefab.");
				}

				//プレハブをクローンしたものと入れ替えるために、クローンプレハブでアセットを作り直す
				if (cloneAssetPair.TryGetValue(prefabAsset, out var clonePrefabAsset))
				{
					GameObject cloneInstance = PrefabUtility.InstantiatePrefab(clonePrefabAsset) as GameObject;
					cloneInstance.transform.SetParent(go.transform.parent);
					cloneInstance.transform.localPosition = prefabAsset.transform.localPosition;
					cloneInstance.transform.localRotation = prefabAsset.transform.localRotation;
					cloneInstance.transform.localScale = prefabAsset.transform.localScale;
					GameObject.DestroyImmediate(go);
				}
				else
				{
					Debug.LogError( go.name + " Not Find Clone Prefab.");
				}
			}

//			Debug.Log(System.DateTime.Now + "ReplaceSerializedProperties");
			//オブジェクト内のコンポーネントからのリンクを全て、テンプレートからクローンに置き換える
			var replacer = new DependencyReplacer(SceneManager.GetActiveScene(), cloneAssetPair);
			replacer.Replace();
			//			Debug.Log(System.DateTime.Now);
		}

	}
}
