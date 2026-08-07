
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utage;
using UtageExtensions;
using System;

namespace Utage
{
	//TextMeshProでルビを表示する際のルビ部分の表示制御コンポーネント
	[AddComponentMenu("Utage/TextMeshPro/TextMeshProRuby")]
	public class TextMeshProRuby : MonoBehaviour
	{
		public TMP_Text TextMeshPro { get { return this.GetComponentCache(ref textMeshPro); } }
		TMP_Text textMeshPro;

		public float OffsetY { get { return offsetY; } set { offsetY = value; } }
		[SerializeField]
		float offsetY;

		//ルビの表示幅
		internal float RubyWidth { get; private set; }
		//本文テキストの表示幅
		internal float TextWidth { get; private set; }

		//各文字が、表示可能となる元テキストのインデックスのリスト
		List<int> VisibleIndexList { get; } = new List<int>();

		TextMeshProRubyInfo RubyInfo { get; set; }

		internal void SetColor(Color color)
		{
			TextMeshPro.color = color;
		}

		//メインテキストの色が変更されたら、ルビの色も変更するための処理
		internal void UpdateColor(TMP_TextInfo textInfo)
		{
			if(RubyInfo == null) return;
			int index = RubyInfo.BeginIndex;
			if(index < 0 || index >= textInfo.characterInfo.Length) return;
			var beginInfo = textInfo.characterInfo[index];
			TextMeshPro.color = beginInfo.color;

		}

		//表示文字数に応じて、ルビの表示のオン、オフを更新
		internal void SetVisibleIndex(int index)
		{
			int visible = 0;
			for (int i = 0; i < VisibleIndexList.Count; ++i)
			{
				if (index < VisibleIndexList[i])
				{
					break;
				}
				visible = i + 1;
			}
			TextMeshPro.maxVisibleCharacters = visible;
		}

		internal void Init(TMP_TextInfo textInfo, TextMeshProRubyInfo rubyInfo, Vector2 pivotOffset)
		{
			RubyInfo = rubyInfo;
			MakeRubyText(textInfo, rubyInfo, pivotOffset);
			InitVisibleIndexList(textInfo, rubyInfo);
		}

		//ルビのテキストオブジェクトを作成
		internal void MakeRubyText(TMP_TextInfo textInfo, TextMeshProRubyInfo rubyInfo, Vector2 pivotOffset)
		{
			int len = textInfo.characterInfo.Length;
			//配列オーバーのチェック
			if (rubyInfo.BeginIndex < 0 || rubyInfo.BeginIndex >= len || rubyInfo.EndIndex < 0 ||
			    rubyInfo.EndIndex >= len)
			{
				Debug.LogError("TextMeshProRubyInfo is index out of range.\n"
				               + $"ruby target index= {rubyInfo.BeginIndex} ~ {rubyInfo.EndIndex}.  text length ={len}");
				return;
			}

			var beginInfo = textInfo.characterInfo[rubyInfo.BeginIndex];
			var endInfo = textInfo.characterInfo[rubyInfo.EndIndex];
			float left = beginInfo.bottomLeft.x;
			float right = endInfo.bottomRight.x;
			float centerX = (left + right) / 2;
			float width = right - left;
			TMP_LineInfo line = textInfo.lineInfo[beginInfo.lineNumber];
			//Debug.LogFormat("baseline={0} lineHeight={1}", line.baseline, line.lineHeight);
			//			float y = line.baseline + line.lineHeight;
			float y = line.ascender + OffsetY;

			//pivotを考慮して位置をずらす必要がある
			
			MakeRubyText(centerX + pivotOffset.x, y + pivotOffset.y, width, rubyInfo.Ruby, beginInfo.color);
		}

		//ルビのテキストオブジェクトを指定の位置に作成
		void MakeRubyText(float x, float y, float textWidth, string text, Color color)
		{
			TextWidth = textWidth;

			TextMeshPro.rectTransform.pivot = new Vector2(0.5f,0 );
			TextMeshPro.rectTransform.anchoredPosition = new Vector2(x, y);
			TextMeshPro.text = text;
			TextMeshPro.color = color;

			RubyWidth = TextMeshPro.preferredWidth;
//			Debug.LogFormat("{2} rubyWidth = {0} stringWidth = {1}", rubyWidth, stringWidth, text);
			if (RubyWidth >= TextWidth)
			{
				//ルビの幅が、ルビを振る本文の文字列の幅よりも広い場合
				TextMeshPro.rectTransform.SetWidth(RubyWidth);
				//本当は本文のほうの文字間を広げたいところ…
				//あらかじめ　cspaceタグを設定してなんとかする？
			}
			else
			{
				TextMeshPro.rectTransform.SetWidth(TextWidth);

				//ルビの余白を調整する
				//左右のマージンと、テキストの均等アラインメントを行う
				int len = text.LengthWithSurrogatePairs();
				float space = (TextWidth - RubyWidth) / len / 2;
				Vector4 margin = TextMeshPro.margin;
				margin.x = space;
				margin.z = space;
				TextMeshPro.margin = margin;
			}
			TextMeshPro.alignment = TextAlignmentOptions.BottomFlush;
			TextMeshPro.ForceMeshUpdate();
		}

		//本文の最後の文字の一番右の座標が、ルビの各文字の一番左以上になったら、そのルビ文字は表示する
		//という処理を行うための各文字ごとの表示インデックスのリストを作る
		void InitVisibleIndexList(TMP_TextInfo textInfo, TextMeshProRubyInfo rubyInfo)
		{
			List<float> rightList = new List<float>();
			for (int i = rubyInfo.BeginIndex; i <= rubyInfo.EndIndex; ++i)
			{
				rightList.Add(textInfo.characterInfo[i].bottomRight.x);
			}

			VisibleIndexList.Clear();
			var rubyTextInfo = TextMeshPro.ForceGetTextInfo();
			float x0 = TextMeshPro.rectTransform.anchoredPosition.x;
			int length = rubyTextInfo.characterCount;
			for (int i = 0; i < length; ++i)
			{
				float left = rubyTextInfo.characterInfo[i].bottomLeft.x + x0;
				int index = rightList.FindIndex(right => (right >= left));
				if (index < 0)
				{
					index = rightList.Count - 1;
				}
				index += rubyInfo.BeginIndex;
				VisibleIndexList.Add(index);
			}
		}

	}

}
