// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	[CustomEditor(typeof(DicingTextures))]
	public class DicingTexturesInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.DrawDefaultInspector();

			if (GUILayout.Button("Build", EditorStyles.miniButton))
			{
				DicingTexturePacker.Pack( this.serializedObject.targetObject as DicingTextures, false);
				AssetDatabase.Refresh();
			}
			if (GUILayout.Button("Rebuild", EditorStyles.miniButton))
			{
				DicingTexturePacker.Pack(this.serializedObject.targetObject as DicingTextures, true);
				AssetDatabase.Refresh();
			}
		}

		//プレビュー表示する場合true
		public override bool HasPreviewGUI()
		{
			return true;
		}

		int previewIndex = 0;
		public override void OnPreviewSettings()
		{
			DicingTextures obj = this.target as DicingTextures;
			if (obj.TextureDataList.Count <= 0) return;

			previewIndex = EditorGUILayout.Popup(previewIndex, obj.GetPattenNameList().ToArray() );
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			DicingTextures obj = this.target as DicingTextures;
			if (previewIndex >= obj.TextureDataList.Count ) return;

			obj.OnPreviewGUI(obj.TextureDataList[previewIndex], r);
		}

	}
}
