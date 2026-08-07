// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Utage
{
	/// <summary>
	/// [Horizontal]アトリビュート
	/// 子要素を横並びに登録する
	/// </summary>
	public class HorizontalAttribute : PropertyAttribute
	{

#if UNITY_EDITOR
		// [Horizontal]表示のためのプロパティ描画
		[CustomPropertyDrawer(typeof(HorizontalAttribute))]
		class Drawer : PropertyDrawerEx<HorizontalAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				Helper.DrawHorizontalChildren(position, property, label);
			}
		}
#endif
	}
}
