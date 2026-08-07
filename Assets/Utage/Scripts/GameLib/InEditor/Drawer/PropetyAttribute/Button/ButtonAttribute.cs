// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

namespace Utage
{

	/// <summary>
	/// [Button]アトリビュート（デコレーター）
	/// ボタンを表示する
	/// </summary>
	public class ButtonAttribute : PropertyAttribute
	{
		// ボタンが押されたとき呼ばれる関数名
		GuiDrawerFunction Function { get; }

		//ボタンが押されたとき渡す引数の関数名（引数としてobjectの配列を返す）
		GuiDrawerFunction ArgsFunction { get; }
		
		//ボタンが無効かの判定関数名
		GuiDrawerFunction DisableFunction { get; }

		// ボタンの表示テキスト
		string Text { get; }

		public ButtonAttribute(string function, string text = "", int order = 0)
			: this(function, "", "", false, text, order)
		{
		}
		public ButtonAttribute(string function, bool nested, string text = "", int order = 0)
			: this(function, "", "", nested, text, order)
		{
		}

		public ButtonAttribute(string function, string disableFunction, bool nested, string text = "", int order = 0)
			:this(function, disableFunction, "", nested, text, order)
		{
		}

		public ButtonAttribute(string function, string disableFunction, string nonNestedArgsFunction, bool nested, string text = "", int order = 0)
		{
			Function = new GuiDrawerFunction(function, nested);
			DisableFunction = new GuiDrawerFunction(disableFunction, nested);
			ArgsFunction = new GuiDrawerFunction(nonNestedArgsFunction, false);
			Text = text;
			this.order = order;
		}

#if UNITY_EDITOR
		// [Button]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(ButtonAttribute))]
		class Drawer : PropertyDrawerEx<ButtonAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				if (!string.IsNullOrEmpty(Attribute.Text))
				{
					label = new GUIContent(Attribute.Text);
				}

				using (new EditorGUI.DisabledScope(IsDisable(property)))
				{
					if (GUI.Button(EditorGUI.IndentedRect(position), label))
					{
						var args = GetArgs(property);
						Helper.CallFunction(property, Attribute.Function, args);
					}
				}
			}
			
			//ボタンが無効かどうか
			bool IsDisable(SerializedProperty property)
			{
				if (Attribute.DisableFunction.Disable) return false;

				return Helper.CallFunction<bool>(property, Attribute.DisableFunction);
			}

			//実行する引数
			object[] GetArgs(SerializedProperty property)
			{
				if (Attribute.ArgsFunction.Disable) return null;

				var result = Helper.CallFunction(property, Attribute.ArgsFunction);
				if(result == null) return null;
				if (result is Array array)
				{
					return array.Cast<object>().ToArray();
				}
				else
				{
					return new object[] { result };
				}
			}

		}
#endif
	}
}
