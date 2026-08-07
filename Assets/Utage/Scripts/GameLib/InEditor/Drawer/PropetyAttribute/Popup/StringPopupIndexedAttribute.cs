// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	/// <summary>
	/// [StringPopup]アトリビュート
	/// 文字列リストから一つを選択するポップアップを表示する。文字列リストは、指定した関数名で取得できる
	/// </summary>
	public class StringPopupIndexedAttribute : PropertyAttribute
	{
		GuiDrawerFunction Function { get; }
		public StringPopupIndexedAttribute(string function, bool nested = false)
		{
			Function = new GuiDrawerFunction(function, nested);
		}

#if UNITY_EDITOR
		// [StringPopup]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(StringPopupIndexedAttribute))]
		class Drawer : PropertyDrawerEx<StringPopupIndexedAttribute>
		{
			// Draw the property inside the given rect
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//メソッド呼び出し
				List<string> stringList = Helper.CallFunction<List<string>>(property, Attribute.Function);
				if (stringList == null || stringList.Count <= 0)
				{
					EditorGUI.PropertyField(position, property, label);
				}
				else
				{
					int index = property.intValue;
					if (index < 0) index = 0;
					index = EditorGUI.Popup(position, label.text, index, stringList.ToArray());
					property.intValue = index;
				}
			}
		}
#endif
	}
}
