// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Utage
{
#if UNITY_EDITOR
	/// <summary>
	/// [Folder]アトリビュート
	/// フォルダアセットのみを登録する
	/// </summary>
	public class FolderAttribute : PropertyAttribute
	{
		/// [Folder]表示のためのプロパティ描画
		[CustomPropertyDrawer(typeof(FolderAttribute))]
		class Drawer : PropertyDrawerEx<FolderAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(Object),
					false);
				if (obj != property.objectReferenceValue)
				{
					if (obj != null)
					{
						if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)))
						{
							//フォルダ以外のアセットは設定しない
							return;
						}
					}

					property.objectReferenceValue = obj;
				}
			}
		}
	}
#endif
}
