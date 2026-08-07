// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	//ハイパーリンクの表示をする場合に使用
	public class GuiDrawerLinkInfo
	{
		public string Url { get; }
		public string Label { get; }

		public GuiDrawerLinkInfo(string url)
			: this(url, "")
		{
			Url = url;
		}

		public GuiDrawerLinkInfo(string url, string label)
		{
			Url = url;
			Label = string.IsNullOrEmpty(label) ? Url : label;
		}

#if UNITY_EDITOR
		public void DrawHyperLinks(Rect position)
		{
			GUIStyle style = EditorStyles.linkLabel;
			Vector2 size = style.CalcSize(new GUIContent(Label));
			position.width = size.x;

			//マウスカーソルを変化
			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
			if (GUI.Button(position, Label, EditorStyles.linkLabel))
			{
				Application.OpenURL(Url);
			}
		}
#endif

	}
}
