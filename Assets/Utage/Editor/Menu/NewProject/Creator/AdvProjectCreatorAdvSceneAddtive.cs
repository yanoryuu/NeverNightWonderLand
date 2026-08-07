// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Utage
{

	//既存のシーンに加算する形で宴のシーンを作る処理
	public class AdvProjectCreatorAdvSceneAdditive : AdvProjectCreatorAdvScene 
	{
		public AdvProjectCreatorAdvSceneAdditive(AdvProjectCreator creator, AdvProjectCreatorAssets creatorAssets )
			: base(creator, creatorAssets)
		{
		}

		public Scene Create()
		{
			//加算対象のシーンのカメラの宴関係のレイヤー設定を無効化する
			//レイヤーマスクがAll等になっている場合、宴のレイヤーを無効化し描画対象外とすることが主な目的
			ChangeCameraMaskInScene();

			//すでにイベントシステムがある場合、加算した宴シーン内のイベントシステムを削除するためにとっておく
			var defaultEventSystems = SceneManagerEx.GetComponentsInActiveScene<UnityEngine.EventSystems.EventSystem>(true).ToArray();
			
			//テンプレートシーンを現在のシーンに合成
			var scene = EditorSceneManagerEx.MergeSceneAssetToActiveScene(TemplateSceneAsset);


			//余分なオブジェクトを削除
			UtageUguiTitle title = SceneManagerEx.GetComponentInActiveScene<UtageUguiTitle>(true);
			Object.DestroyImmediate(title.transform.root.gameObject);
			SystemUi systemUi = SceneManagerEx.GetComponentInActiveScene<SystemUi>(true);
			Object.DestroyImmediate(systemUi.gameObject);


			//「宴」エンジンの初期化
			InitUtageEngine();

			//エンジン休止状態に
			Engine.gameObject.SetActive(false);

			ChangeLayerInCurrentScene();

			//すでにイベントシステムがある場合は、新しいほうを削除する
			if (defaultEventSystems.Length>0)
			{
				var eventSystems = SceneManagerEx.GetComponentsInActiveScene<UnityEngine.EventSystems.EventSystem>(true).ToArray();
				foreach( var item in eventSystems )
				{
					if (!defaultEventSystems.Contains(item))
					{
						GameObject.DestroyImmediate(item.gameObject);
						break;
					}
				}
			}

			return scene;
		}

		//加算対象のシーンのカメラレイヤー設定を変更する
		//レイヤーマスクがAll等になっている場合、宴の描画対象レイヤーを無効化し描画対象外とすることが主な目的
		void ChangeCameraMaskInScene()
		{
			if(Creator is not IAdvProjectCreatorLayerNames layerNames) return;
			
			Camera[] cameras = SceneManagerEx.GetComponentsInActiveScene<Camera>(true).ToArray();

			List<string> changeLayers = new List<string>();
			if (layerNames.LayerName != layerNames.DefaultLayerName)
			{
				changeLayers.Add(layerNames.LayerName);
				LayerMaskEditor.TryAddLayerName(layerNames.LayerName);
			}
			if (layerNames.LayerNameUI != layerNames.DefaultLayerNameUI)
			{
				changeLayers.Add(layerNames.LayerNameUI);
				LayerMaskEditor.TryAddLayerName(layerNames.LayerNameUI);
			}

			//既存のカメラの、宴関係のレイヤー設定を無効化する
			int mask = LayerMask.GetMask(changeLayers.ToArray());
			foreach (Camera camera in cameras)
			{
				camera.cullingMask &= ~mask;
			}
		}

		void ChangeLayerInCurrentScene()
		{
			if (Creator is not IAdvProjectCreatorLayerNames layerNames) return;

			//レイヤー設定を変える
			SwapLayerInChildren(Engine.gameObject, layerNames.DefaultLayerName, layerNames.LayerName);
			SwapLayerInChildren(Engine.gameObject, layerNames.DefaultLayerNameUI, layerNames.LayerNameUI);
			SwapLayerInChildren(Manager.gameObject, layerNames.DefaultLayerNameUI, layerNames.LayerNameUI);

			foreach (Camera camera in Manager.GetComponentsInChildren<Camera>())
			{
				ChangeCameraLayer(camera);
			}
		}

		void ChangeCameraLayer( Camera camera )
		{
			if (Creator is not IAdvProjectCreatorLayerNames layerNames) return;

			switch (camera.gameObject.name)
			{
				case "SpriteCamera":
					SwapLayerInChildren(camera.gameObject, layerNames.DefaultLayerName, layerNames.LayerName);
					camera.cullingMask = LayerMask.GetMask(new string[] { layerNames.LayerName });

					//AudioListenerが二つなら削除
					AudioListener[] audioListeners = SceneManagerEx.GetComponentsInActiveScene<AudioListener>(true).ToArray();
					if (audioListeners.Length > 1)
					{
						UnityEngine.Object.DestroyImmediate( camera.GetComponent<AudioListener>() );
					}
					break;
				case "UICamera":
				case "ClearCamera":
					SwapLayerInChildren(camera.gameObject, layerNames.DefaultLayerNameUI, layerNames.LayerNameUI);
					camera.cullingMask = LayerMask.GetMask(new string[] { layerNames.LayerNameUI });
					break;
			}
		}

		void SwapLayerInChildren( GameObject go, string oldLayerName, string newLayerName)
		{
			int oldLayer = LayerMask.NameToLayer(oldLayerName);
			int newLayer = LayerMask.NameToLayer(newLayerName);

			foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
			{
				if( child.gameObject.layer == oldLayer )
				{
					child.gameObject.layer = newLayer;
				}
			}
		}
	}
}
