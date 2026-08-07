// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UTAGE_URP_EDITOR

using UnityEditor;
using UnityEngine;

namespace Utage.RenderPipeline.Urp
{
	//URPのセットアップ用のウィンドウ
	//通常は自動実行されるが、必要に応じて手動で実行するためのウィンドウ
	public class UrpSetupWindow : EditorWindowNoSave
	{
		/// ツールウィンドウを開く
		[MenuItem(MenuTool.MenuToolRoot + "Setup Urp Project Settings", priority = MenuTool.PriorityPackage+9)]
		static void MenuToolSetupAllProjectRenderers()
		{
			EditorWindow.GetWindow(typeof(UrpSetupWindow), false, "Setup Urp Settings");
		}

#pragma warning disable 414

		[SerializeField, Button(nameof(ForceUpdatePackages), false)]
		string forceUpdate = "";

		[SerializeField, Button(nameof(SetupUrpProjectSettings), false)]
		string setupUrpProjectSettings = "";

#pragma warning restore 414
			
		void ForceUpdatePackages()
		{
			ExtensionPackageManager.Instance.ForceImportPackages();
		}

		void SetupUrpProjectSettings()
		{
			string message = SimpleLang.Select("プロジェクトのGraphic設定のすべてのRenderPipelineに、必要なRenderFeatureを設定しデフォルトのグローバルボリュームをNoneにします。\n実行してよろしいですか？","This will configure the required Render Features for all Render Pipelines in the project's Graphics settings and set the default global volume to None.\nDo you want to proceed?");
			bool ok = EditorUtility.DisplayDialog(
				SimpleLang.Select("確認","Confirmation"),
				message,
				SimpleLang.Select("実行","Execute"),
				SimpleLang.Select("キャンセル","Cancel")
			);

			if (!ok)
				return;

			UrpRendererConverter converter = new UrpRendererConverter();
			converter.ConvertProjectAllRenderPipelines(true);
		}


		//描画更新
		protected override void OnGUI()
		{
			base.OnGUI();
			
			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=16001");
		}

	}
}
#endif
