// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	/// <summary>
	/// [LimitEnum]アトリビュート
	/// 指定された名前のenumだけ表示する
	/// </summary>
	public class LimitEnumAttribute : PropertyAttribute
	{
		public string[] Args { get; }
		public LimitEnumAttribute(params string[] args)
		{
			Args = args;
		}

#if UNITY_EDITOR

		// [LimitEnum]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(LimitEnumAttribute))]
		class Drawer : PropertyDrawerEx<LimitEnumAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				int index = Mathf.Clamp(property.intValue, 0, property.enumNames.Length - 1);
				string lastName = property.enumNames[index];
				int lastIndex = ArrayUtility.FindIndex<string>(Attribute.Args, (x) => (x == lastName));
				index = EditorGUI.Popup(position, label.text, lastIndex, Attribute.Args);
				if (lastIndex != index)
				{
					property.intValue =
						ArrayUtility.FindIndex<string>(property.enumNames, (x) => (x == Attribute.Args[index]));
				}
			}
		}
#endif
	}
}
