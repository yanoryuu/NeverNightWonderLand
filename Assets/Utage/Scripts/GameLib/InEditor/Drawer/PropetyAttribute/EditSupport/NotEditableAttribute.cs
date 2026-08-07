// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	// [NotEditable]アトリビュート
	// 表示のみで編集を不可能にする。
	public class NotEditableAttribute : PropertyAttribute
	{
		GuiDrawerFunction Function { get; }

		public NotEditableAttribute()
			: this("")
		{
		}
		public NotEditableAttribute(string function, bool nested = false)
		{
			Function = new GuiDrawerFunction(function, nested);
		}
#if UNITY_EDITOR

		// [NotEditable]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(NotEditableAttribute))]
		class Drawer : PropertyDrawerEx<NotEditableAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				bool isNotEditable = true;
				if (!Attribute.Function.Disable)
				{
					isNotEditable = Helper.CallFunction<bool>(property, Attribute.Function);
				}
				EditorGUI.BeginDisabledGroup(isNotEditable);
				EditorGUI.PropertyField(position, property, label);
				EditorGUI.EndDisabledGroup();
			}
		}
#endif 
	}
}
