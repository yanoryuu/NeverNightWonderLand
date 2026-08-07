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
	public class StringPopupFunctionAttribute : PropertyAttribute
	{
		public GuiDrawerFunction Function { get; }
		public StringPopupFunctionAttribute(string function, bool nested = false)
		{
			Function = new GuiDrawerFunction(function, nested);
		}

#if UNITY_EDITOR
		// [StringPopup]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(StringPopupFunctionAttribute))]
		class Drawer : PropertyDrawerEx<StringPopupFunctionAttribute>
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
					string oldStr = property.stringValue;
					int index = stringList.FindIndex(x => x == oldStr);
					if (index < 0) index = 0;
					index = EditorGUI.Popup(position, label.text, index, stringList.ToArray());
					property.stringValue = stringList[index];
				}
			}
		}
#endif
	}
}
