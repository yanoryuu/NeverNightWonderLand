// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// [HyperLink]アトリビュート（デコレーター）
	/// ハイパーリンクを表示する
	public class HyperLinkAttribute : PropertyAttribute
	{
		/// リンク情報
		GuiDrawerLinkInfo LinkInfo { get; }
		
		public HyperLinkAttribute(string url, string label="", int order = 0)
			: this( new GuiDrawerLinkInfo(url, label), order)
		{
		}

		public HyperLinkAttribute(GuiDrawerLinkInfo linkInfo, int order = 0)
		{
			LinkInfo = linkInfo;
			this.order = order;
		}

#if UNITY_EDITOR
		/// [HyperLink]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(HyperLinkAttribute))]
		class Drawer : DecoratorDrawerEx<HyperLinkAttribute>
		{
			public override void OnGUI(Rect position)
			{
				position = EditorGUI.IndentedRect(position);
				Attribute.LinkInfo.DrawHyperLinks(position);
			}

			public override float GetHeight()
			{
				return EditorGUIUtility.singleLineHeight;
			}
		}
#endif
	}
}
