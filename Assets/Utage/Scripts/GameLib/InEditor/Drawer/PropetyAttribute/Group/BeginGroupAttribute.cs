// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// [BeginGroup]アトリビュート（デコレーター）
	/// グループ表示の開始を示す
	public class BeginGroupAttribute : PropertyAttribute
	{
		string Label { get; }

		public BeginGroupAttribute(string label, int order = 0)
		{
			Label = label;
			this.order = order;
		}
#if UNITY_EDITOR
		/// [HelpBox]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(BeginGroupAttribute))]
		class Drawer : PropertyDrawerEx<BeginGroupAttribute>
		{
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return base.GetPropertyHeight(property, label) + 4.0f;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				//グループの終了を示すプロパティを探す
				var endProperty = Helper.FindNextPropertyHasAttribute<EndGroupAttribute>(property);


				//グループ全体の高さを取得
				//インデントを考慮
				Rect boxRect = EditorGUI.IndentedRect(position);
				float height = Helper.GetPropertyHeightBetween(property, endProperty);
				boxRect.height = height;
				//背景となる矩形を描画
				EditorGUI.DrawRect(boxRect, ColorUtil.GetEditorGuiBoxBgColor());
				
				GUIStyle style = GUI.skin.label;
				style.richText = true;
				position.yMin += 2.0f;
				EditorGUI.LabelField(position, "<b>" + Attribute.Label + "</b>", style);

				//グループの各要素のインデントを下げる
				//グループ全体がUnfoldedSerializableの一部などの場合は有効に機能するが、基本的には機能しない
				EditorGUI.indentLevel++;
			}
		}
#endif
	}
}
