// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// [EndGroup]アトリビュート（デコレーター）
	/// グループ表示の終了を示す
	public class EndGroupAttribute : PropertyAttribute
	{
		public EndGroupAttribute(int order = 0)
		{
			this.order = 100;
		}
#if UNITY_EDITOR
		/// [HelpBox]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(EndGroupAttribute))]
		class Drawer : PropertyDrawerEx<EndGroupAttribute>
		{
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return 8.0f;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//インデントを戻す
				EditorGUI.indentLevel--;
			}
		}
#endif
	}
}
