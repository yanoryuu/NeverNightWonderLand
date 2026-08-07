// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	[CustomEditor(typeof(AssetFileManager))]
	public class AssetFileManagerInspector : Editor
	{
		public SerializedObjectHelper SerializedObjectHelper
		{
			get
			{
				if (serializedObjectHelper == null)
				{
					serializedObjectHelper = new SerializedObjectHelper(this);
					OnSerializedObjectHelperInit();
				}
				return serializedObjectHelper;
			}
		}
		SerializedObjectHelper serializedObjectHelper;

		AssetFileManager AssetFileManager
		{
			get { return this.target as AssetFileManager; }
		}
		AssetFileManagerSettings Settings
		{
			get { return AssetFileManager.Settings; }
		}
		int tabIndex = 0;

		//ロード後初期化
		protected virtual void OnSerializedObjectHelperInit()
		{
			SerializedObjectHelper.AddGroupInfo("Debug", "isOutPutDebugLog", "isDebugBootDeleteChacheAll");
			SerializedObjectHelper.DrawCustomProperty = DrawCustomProperty;
			tabIndex = 0;
		}

		public override void OnInspectorGUI()
		{
			if (SerializedObjectHelper.OnGUI())
			{
			}
		}

		//描画をカスタムする部分
		bool DrawCustomProperty(SerializedProperty property)
		{
			switch (property.name)
			{
				case "settings":
					DrawAssetFileManagerSettingsProperty(property);
					return true;
				default:
					return false;
			}
		}

		//Settingsプロパティの描画
		void DrawAssetFileManagerSettingsProperty(SerializedProperty property)
		{
			property = property.Copy();
			string rootPath = property.propertyPath + ".";

			UtageEditorToolKit.BeginGroup("Load Setting");

			SerializedObjectHelper.DrawProperty(rootPath + "loadType");

			//タブの表示
			List<string> tabName = new List<string>();
			foreach( AssetFileSetting setting in Settings.FileSettings )
			{
				tabName.Add(setting.FileType.ToString());
			}
			tabIndex = GUILayout.Toolbar(tabIndex, tabName.ToArray(), EditorStyles.toolbarButton);

			//タブの中身の表示

			string arrayRootPath = rootPath + "fileSettings." + string.Format("Array.data[{0}]", tabIndex) + ".";
			bool isAdVanced = Settings.LoadTypeSetting == AssetFileManagerSettings.LoadType.Advanced;
			AssetFileSetting currentSetting = Settings.FileSettings[tabIndex];

			GUILayout.Space(-5f);
			EditorGUILayout.BeginVertical("box");
			EditorGUI.indentLevel++;
			{
				GUILayout.Space(4f);
				GUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
			
				//AdVanced以外では編集不可
				if (isAdVanced)
				{
					SerializedObjectHelper.DrawProperty(arrayRootPath + "isStreamingAssets");
					SerializedObjectHelper.DrawProperty(arrayRootPath + "encodeType");
				}
				else
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.Toggle("IsStreamingAssets", currentSetting.IsStreamingAssets );
					EditorGUI.EndDisabledGroup();
				}
				SerializedObjectHelper.DrawProperty(arrayRootPath + "extensions");
			}
			EditorGUI.indentLevel--;
			UtageEditorToolKit.EndGroup();


			UtageEditorToolKit.EndGroup();
		}
	}
}

 