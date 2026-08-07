// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// <summary>
	/// [UpdateFunction]アトリビュート（デコレーター）
	/// 描画タイミングで指定の関数を呼ぶ（Applyなど最後にUpdateをかけるときに）
	/// </summary>
	public class UpdateFunctionAttribute : PropertyAttribute
	{
		GuiDrawerFunction Function { get; }

		public UpdateFunctionAttribute(string function, bool nested = false, int order = 0)
		{
			Function = new GuiDrawerFunction(function, nested);
			this.order = order;
		}

#if UNITY_EDITOR
		// [UpdateFunction]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(UpdateFunctionAttribute))]
		class Drawer : PropertyDrawerEx<UpdateFunctionAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				Helper.CallFunction(property, Attribute.Function);
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return 0;
			}
		}
#endif
	}

}
