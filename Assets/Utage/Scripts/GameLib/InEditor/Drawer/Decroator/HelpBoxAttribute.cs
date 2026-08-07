// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using System.Collections.Generic;
using UtageExtensions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

	/// [HelpBox]アトリビュート（デコレーター）
	/// ヘルプボックスを表示する
	public class HelpBoxAttribute : PropertyAttribute
	{
		/// ヘルプボックスの種類
		/// UnityEditor.MessageTypeをそのまま使うと、非エディタ環境でコンパイルエラーになるので
		/// 重複したものを書く
		public enum Type
		{
			None,           /// 通常
			Info,           /// 情報
			Warning,		/// 警告
			Error,          /// エラー
		}

		/// 表示するメッセージ
		string Message { get; }
		
		//ヘルプボックスに表示するメッセージ（Urlのための情報入り）
		string HelpBoxMessage { get; }

		/// メッセージのタイプ
		Type MessageType { get; }

		GuiDrawerLinkInfo[]	Links { get; }

		public HelpBoxAttribute(string message, Type type = Type.None, int order = 0)
			: this(message, type, order, Array.Empty<GuiDrawerLinkInfo>())
		{
		}

		public HelpBoxAttribute(string message, string url, Type type = Type.None, int order = 0)
			: this(message, type, order, new GuiDrawerLinkInfo(url))
		{
		}

		public HelpBoxAttribute(string message, GuiDrawerLinkInfo linkInfo, Type type = Type.None, int order = 0)
			: this(message, type, order, linkInfo)
		{
		}

		public HelpBoxAttribute(string message, string url, string linkLabel, Type type = Type.None, int order = 0)
			: this(message, type, order, new GuiDrawerLinkInfo(url,linkLabel))
		{
		}

		HelpBoxAttribute(string message, Type type, int order, params GuiDrawerLinkInfo[] links )
		{
			Message = message;
			MessageType = type;
			this.order = order;
			Links = links;
			var helpBoxMessage = Message;
			for (int i = 0; i < Links.Length; ++i)
			{
				if (i == 0 && Message.IsNullOrEmpty()) continue;
				helpBoxMessage += "\n";
			}
			HelpBoxMessage = helpBoxMessage;
		}
		

#if UNITY_EDITOR
		/// [HelpBox]を表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
		class Drawer : DecoratorDrawerEx<HelpBoxAttribute>
		{
			//デコレーターと、本来のプロパティとの間のスペース
			const int Space = 4;

			//heightの値を記録する
			//GetHeight()のときはインデントによる幅の違いがとれないので、OnGUIのときの計算を記録するしかない
			float Height { get; set; }= -1;
			
			public override void OnGUI(Rect position)
			{
				position = EditorGUI.IndentedRect(position);
				float h = GetHelpBoxHeight(position.width);
				position.height = h; 
				EditorGUI.HelpBox(position, Attribute.HelpBoxMessage, (MessageType)Attribute.MessageType);

				float y = position.yMax - GetLinksOffsetHeight();
				float x = position.xMin + GetIconWidth(h);
				foreach (var link in Attribute.Links)
				{
					var pos = position;
					pos.xMin = x;
					pos.yMin = y;
					pos.height = EditorGUIUtility.singleLineHeight;
					link.DrawHyperLinks(pos);
					y+= EditorGUIUtility.singleLineHeight;
				}
				this.Height = h + Space;
			}
			
			public override float GetHeight()
			{
				if (this.Height <= 0)
				{
					return GetHelpBoxHeight(Helper.GetIndentedViewWidth()) + Space;
				}
				else
				{
					return this.Height;
				}
			}


			float GetHelpBoxHeight(float width)
			{
				var content = new GUIContent(Attribute.HelpBoxMessage);
				//アイコンのあるなしでテキストエリアの幅が違うせい？
				//Unityのバグな気がする…
				float h = EditorStyles.helpBox.CalcHeight(content, width);
				width -= GetIconWidth(h);
				return EditorStyles.helpBox.CalcHeight(content, width);
			}

			float GetIconWidth(float height)
			{
				if (Attribute.MessageType == HelpBoxAttribute.Type.None)
				{
					return 0;
				}

				if (height > EditorGUIUtility.singleLineHeight*2 + 4)
				{
					return 36;
				}
				else if (height > EditorGUIUtility.singleLineHeight + 4)
				{
					return 28;
				}
				else
				{
					return 16;
				}
			}
			
			//ハイパーリンクの表示位置を調整するためのオフセット
			float GetLinksOffsetHeight()
			{
				int count = Attribute.Links.Length;
				//リンクがないならオフセットは不要
				if(count== 0) return 0;

				//行数文のオフセットを返す
				return EditorGUIUtility.singleLineHeight * count;
			}
		}
#endif
	}
}
