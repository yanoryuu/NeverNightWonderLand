// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// <summary>
	/// [AddButtonAttribute]アトリビュート
	/// ボタンを追加表示する
	/// </summary>
	public class AddButtonAttribute : PropertyAttribute
	{
		// ボタンが押されたとき呼ばれる関数情報
		GuiDrawerFunction Function { get; }
		// ボタンに表示するラベル
		string Text { get; }

		public AddButtonAttribute(string function, string text = "Button", int order = 0)
			:this(function, false, text, order)
		{
		}

		public AddButtonAttribute(string function, bool nested, string text = "Button", int order = 0)
		{
			Function = new GuiDrawerFunction(function, nested);
			Text = text;
			this.order = order;
		}

#if UNITY_EDITOR
		// [AddButton]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(AddButtonAttribute))]
		class Drawer : PropertyDrawerEx<AddButtonAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//ボタンのラベル
				GUIContent buttonLabel = new GUIContent(Attribute.Text);
				//子要素のラベル部分の表示幅を調整
				float buttonWidth = GUI.skin.button.CalcSize(buttonLabel).x;
				const float space = 8.0f;
				position.width -= buttonWidth + space;

				EditorGUI.PropertyField(position, property, new GUIContent(property.displayName));

				position.x = position.xMax + space;
				position.width = buttonWidth;
				if (GUI.Button(position, buttonLabel))
				{
					Helper.CallFunction(property, Attribute.Function);
				}
			}
		}
#endif
	}

}
