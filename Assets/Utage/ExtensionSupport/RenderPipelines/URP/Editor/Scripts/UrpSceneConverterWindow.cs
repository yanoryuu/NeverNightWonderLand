#if UTAGE_URP_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
	//URPへのシーン変換ツールウィンドウ
	public class UrpSceneConverterWindow : EditorWindowNoSave
	{
		/// URPへのシーン変換ツールウィンドウを開く
		[MenuItem(MenuTool.MenuToolRoot + "Urp Scene Converter", priority = MenuTool.PriorityPackage + 10)]
		static void OpenExtensionPackageImporter()
		{
			EditorWindow.GetWindow(typeof(UrpSceneConverterWindow), false, "Urp Scene Converter");
		}

		[SerializeField] SceneAsset targetScene;
		[SerializeField] bool novelGameType = true;
		
#pragma warning disable 414
		[SerializeField, Button(nameof(Convert), nameof(DisableConvert), false)]
		string convert = "";
#pragma warning restore 414
		
		bool DisableConvert()
		{
			if(targetScene == null) return true;

			return false;
		}

		void Convert()
		{
			if (!UrpRenderPipelineAssetsCreator.CheckTemplateAssets() || !UrpSceneConverter.CheckTemplateAssets())
			{
				Debug.LogError("Not found Urp Template Asset. Please import Extension Package\n Menu/Utage/Extension Package Manager");
				return;
			}
			string scenePath = AssetDatabase.GetAssetPath(targetScene);
			string renderPipelineAssetsPath = Path.Combine( Path.GetDirectoryName(scenePath) ?? string.Empty, "RenderPipeline");
			string projectName = Path.GetFileNameWithoutExtension(scenePath);

			//URPに必要なアセットを作成
			UrpRenderPipelineAssetsCreator creator = new ();
			var assetsDictionary = creator.CreateAssets(renderPipelineAssetsPath,projectName);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			//URPのプロジェクト設定を初期化
			int rendererIndex = -1;
			if (!RenderPipelineUtil.IsUrp())
			{
				//現在のRenderPipelineがURPじゃない
				Debug.LogError("Current RenderPipeline is not URP");
			}
			else
			{
				//デフォルト設定されているRendererDataを取得
				(ScriptableRendererData renderer,int index) = UrpProjectSettingsUtil.GetDefaultRendererData();
				if (renderer != null)
				{
					rendererIndex = index;
					//UTAGEで必要になるRendererを追加する
					new UrpRendererConverter().AddRenderFeatures(renderer);
				}
			}

			//指定のシーンを開く
			var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			
			//URPのシーンをコンバート
			UrpSceneConverter sceneConverter = new ()
			{
				RendererIndex = rendererIndex,
				BaseCameraClearBackGroundColor = novelGameType,
				FlexibleScreenSize = !novelGameType,
			};
			if (sceneConverter.SetUp())
			{
				sceneConverter.SwapVolumeProfileAssets(assetsDictionary);
			}
			EditorSceneManager.MarkSceneDirty(scene);
		}

        protected override void OnGUI()
        {
			base.OnGUI();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=15328");
        }
	}
}
#endif
