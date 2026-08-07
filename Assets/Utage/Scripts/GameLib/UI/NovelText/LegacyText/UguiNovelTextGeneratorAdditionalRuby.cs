// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//ノベル用のルビ描画情報
	public class UguiNovelTextGeneratorAdditionalRuby
	{
		//ルビのデータリスト
		public List<UguiNovelTextCharacter> RubyList { get { return rubyList; } }
		List<UguiNovelTextCharacter> rubyList = new List<UguiNovelTextCharacter>();

		//ルビをつける文字列データ
		List<UguiNovelTextCharacter> stringData = new List<UguiNovelTextCharacter>();

		//ルビをつける先頭の文字
		internal UguiNovelTextCharacter TopCharacter{ get{ return stringData[0]; } }

		//ルビ文字列の幅
		public float RubyWidth { get { return rubyWidth; } }
		float rubyWidth;

		//ルビをつける文字列の幅
		public float StringWidth { get { return stringWidth; } }
		float stringWidth;

		//ルビのほうが文字列よりも長いか？
		public bool IsWideType { get { return (RubyWidth > StringWidth); } }

		//行のデータ
		UguiNovelTextLine textLine;

		internal UguiNovelTextGeneratorAdditionalRuby( List<UguiNovelTextCharacter> characters, int index, UguiNovelTextGenerator generator )
		{
			Font rubyFont = generator.NovelText.font;
			float rubySizeScale = generator.RubySizeScale;		
			UguiNovelTextCharacter original = characters[index];
			int rubySize = Mathf.CeilToInt(rubySizeScale * original.FontSize);
			stringData.Add(original);
			for (int i = index + 1; i < characters.Count; ++i)
			{
				UguiNovelTextCharacter c = characters[i];
				if (!c.CustomInfo.IsRuby || c.CustomInfo.IsRubyTop) break;
				stringData.Add(c);
			}
			
			//カラー情報のみコピー
			CharData.CustomCharaInfo rubyInfo = new CharData.CustomCharaInfo();
			rubyInfo.IsColor = original.charData.CustomInfo.IsColor;
			rubyInfo.color = original.charData.CustomInfo.color;
			if (original.charData.CustomInfo.IsEmphasisMark)
			{
				for (int i = 0; i < stringData.Count; ++i)
				{
					CharData data = new CharData(original.charData.CustomInfo.rubyStr[0], rubyInfo);
					rubyList.Add(new UguiNovelTextCharacter(data, rubyFont, rubySize, generator.BmpFontSize, original.FontStyle));
				}
			}
			else
			{
				foreach (char c in original.charData.CustomInfo.rubyStr)
				{
					CharData data = new CharData(c, rubyInfo);
					rubyList.Add(new UguiNovelTextCharacter(data, rubyFont, rubySize, generator.BmpFontSize, original.FontStyle));
				}
			}
		}

		//文字情報を取得した後の初期化
		internal void InitAfterCharacterInfo(UguiNovelTextGenerator generator)
		{
			//ルビの幅を初期化
			float w = 0;
			foreach (UguiNovelTextCharacter ruby in rubyList)
			{
				w += ruby.Width;
			}
			this.rubyWidth = w;

			//ルビをつける文字列の幅を初期化
			w = 0;
			foreach (UguiNovelTextCharacter charcter in stringData)
			{
				w += charcter.Width + generator.LetterSpaceSize;
			}
			this.stringWidth = w;

			//ルビの幅が本文の幅よりも長いなら
			//ルビの幅にあわせて本文をスペースをあけて表示する
			if (IsWideType)
			{
				float diff = RubyWidth - (StringWidth - (stringData.Count * generator.LetterSpaceSize));
				float space = diff / stringData.Count / 2;
				foreach (UguiNovelTextCharacter charcter in stringData)
				{
					charcter.RubySpaceSize = space;
				}
			}
		}
		//位置情報の初期化
		internal void InitPosition(UguiNovelTextGenerator generator)
		{
			foreach( UguiNovelTextLine line in generator.LineDataList )
			{
				if( line.Characters.IndexOf(TopCharacter) >= 0 )
				{
					this.textLine = line;
				}
			}
			float currentSpace = generator.LetterSpaceSize;
	
			//ルビをつける最初の文字からの表示位置のオフセット
			Vector2 offset;

			//ルビ同士の文字間
			float rubySpace = 0;

			//ルビの幅が本文の幅よりも長いなら
			//ルビの幅にあわせて本文をスペースをあけて表示する
			if (IsWideType)
			{
				offset.x = -TopCharacter.RubySpaceSize;
			}
			else
			{
				rubySpace = (StringWidth - RubyWidth) / rubyList.Count;
				offset.x = -currentSpace / 2 + rubySpace / 2;
			}
			//高さを合わせる
			offset.y = this.textLine.MaxFontSize;


			float x = offset.x + TopCharacter.PositionX;
			float y = offset.y + TopCharacter.PositionY;
			foreach (UguiNovelTextCharacter ruby in rubyList)
			{
				ruby.InitPositionX(x);
				ruby.InitPositionY(y);
				x += ruby.Width + rubySpace;
			}
		}

		//描画用の頂点情報を取得(文字送りに対応)
		internal void AddDrawVertex(List<UIVertex> verts, Vector2 endPosition)
		{
			if (!TopCharacter.IsVisible) return;

			try
			{
				foreach (UguiNovelTextCharacter ruby in rubyList)
				{
					if (textLine.Y0 > endPosition.y || (ruby.PositionX + ruby.Width / 2) < endPosition.x)
					{
						if (ruby.Verts != null)
						{
							verts.AddRange(ruby.Verts);
						}
					}
				}
			}
			catch(System.Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

	};
}
