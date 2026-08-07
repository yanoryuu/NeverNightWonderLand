#if UTAGE_URP_EDITOR
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
	//UTAGEで必要になるRenderFeaturesを追加する
	public class UrpRendererConverter
	{
		// プロジェクトウィンドウの右クリックメニューに追加
		public class ContextMenu : EditorWindow
		{
			const string AddRenderFeaturesMenuPath = "Assets/Utage/AddRenderFeatures";
			
			//現在選択しているScriptableRendererDataにUTAGEで必要になるRenderFeatureを追加する
			[MenuItem(AddRenderFeaturesMenuPath)]
			static void AddRenderFeaturesToSelected()
			{
				if (Selection.activeObject is ScriptableRendererData rendererData)
				{
					UrpRendererConverter converter = new UrpRendererConverter();
					converter.AddRenderFeatures(rendererData);
				}
			}
			[MenuItem(AddRenderFeaturesMenuPath, true)]
			static bool IsValidate()
			{
				return Selection.activeObject is ScriptableRendererData;
			}

		}

		//プロジェクトのGraphicsSettings内のすべてのRenderPipelineに、UTAGEで必要になる設定をする
		public void ConvertProjectAllRenderPipelines(bool clearVolumeProfile)
		{
			var pipelineAssets = GraphicsSettings.allConfiguredRenderPipelines;
			foreach (var renderPipelineAsset in pipelineAssets)
			{
				if (renderPipelineAsset is UniversalRenderPipelineAsset universalRenderPipelineAsset)
				{
					ConvertRenderPipelines(universalRenderPipelineAsset,clearVolumeProfile);
				}
			}
		}

		//指定のRenderPipelineに、UTAGEで必要になる設定をする
		public void ConvertRenderPipelines(UniversalRenderPipelineAsset renderPipelineAsset, bool clearVolumeProfile)
		{
			if (clearVolumeProfile)
			{
#if UNITY_6000_0_OR_NEWER				
				//シーン全てに適用されるグローバルなvolumeProfileをクリアする
				renderPipelineAsset.volumeProfile = null;
#endif
			}
			var renderer = renderPipelineAsset.scriptableRenderer;
			if(renderer !=null)
			{
				//デフォルトのScriptableRendererDataにのみRenderFeatureを追加する
				(ScriptableRendererData rendererData,int index) = UrpProjectSettingsUtil.GetDefaultRendererData(renderPipelineAsset);
				if (rendererData != null)
				{
					AddRenderFeatures(rendererData);
				}
			}
			EditorUtility.SetDirty(renderPipelineAsset);
		}

		
		//UTAGEで必要になるRenderFeatureを追加する
		public void AddRenderFeatures(ScriptableRendererData rendererData)
		{
			AddRenderFeatureIfMissing<CaptureRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<GrayScaleRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<MosaicRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<NegaPosiRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<SepiaRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<FishEyeRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<TwirlRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<VortexRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<BlurRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<ColorFadeRenderFeature>(rendererData);
			AddRenderFeatureIfMissing<RuleFadeRenderFeature>(rendererData);
		
			//リフレクションを使ってValidateRendererFeaturesを呼び出す
			const string MethodName = "ValidateRendererFeatures";
			Type rendererDataType = rendererData.GetType();
			MethodInfo validateMethod = rendererDataType.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (validateMethod != null)
			{
				validateMethod.Invoke(rendererData, null);
			}
			else
			{
				Debug.LogError($"{MethodName}method not found");
			}

			EditorUtility.SetDirty(rendererData);
		}
		
		//RendererDataにT型のRenderFeatureがなければ追加
		void AddRenderFeatureIfMissing<T>(ScriptableRendererData rendererData)
			where T: ScriptableRendererFeature
		{
			foreach (var feature in rendererData.rendererFeatures)
			{
				if(feature!=null && feature.GetType() == typeof(T))
				{
					return;
				}
			}
			var renderFeature = ScriptableObject.CreateInstance<T>();
			renderFeature.name = typeof(T).Name;
			rendererData.rendererFeatures.Add(renderFeature);
			if (EditorUtility.IsPersistent(rendererData))
			{
				AssetDatabase.AddObjectToAsset(renderFeature, rendererData);
			}
		}
	}
}
#endif
