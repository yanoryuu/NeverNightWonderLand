// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    // [Hide]アトリビュート
    // 非表示にする。メソッド名を指定すると、条件によって表示非表示を切り替えることができる
    public class HideAttribute : PropertyAttribute
    {
	    GuiDrawerFunction Function { get; }
		public HideAttribute(string function = "", bool nested = false)
        {
			this.Function = new GuiDrawerFunction(function, nested);
        }

#if UNITY_EDITOR
		// 表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(HideAttribute))]
		class Drawer : PropertyDrawerEx<HideAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				if (!IsHide(property))
				{
					EditorGUI.PropertyField(position, property, label);
				}
			}

			bool IsHide(SerializedProperty property)
			{
				if (Attribute.Function.Disable)
				{
					return true;
				}
				else
				{
					return Helper.CallFunction<bool>(property, Attribute.Function);
				}
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				if (IsHide(property))
				{
					return 0;
				}
				else
				{
					return base.GetPropertyHeight(property, label);
				}
			}
		}
#endif

    }

}
