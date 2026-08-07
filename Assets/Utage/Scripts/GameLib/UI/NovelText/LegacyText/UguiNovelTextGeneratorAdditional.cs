// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//ノベル用テキストの付加的な描画情報（ルビやアンダーラインなど）
	public class UguiNovelTextGeneratorAdditional
	{
		//表示の最大幅
		public List<UguiNovelTextGeneratorAdditionalRuby> RubyList
		{
			get { return rubyList; }
		}
		List<UguiNovelTextGeneratorAdditionalRuby> rubyList = new List<UguiNovelTextGeneratorAdditionalRuby>();

		//線（アンダーラインや取り消し線）
		public List<UguiNovelTextGeneratorAdditionalLine> LineList
		{
			get { return lineList; }
		}
		List<UguiNovelTextGeneratorAdditionalLine> lineList = new List<UguiNovelTextGeneratorAdditionalLine>();

		internal UguiNovelTextGeneratorAdditional(List<UguiNovelTextCharacter> characters, UguiNovelTextGenerator generataor )
		{
			for (int i = 0; i < characters.Count; ++i  )
			{
				UguiNovelTextCharacter character = characters[i];

				//線の作成
				if (character.CustomInfo.IsStrikeTop)
				{
					lineList.Add(new UguiNovelTextGeneratorAdditionalLine(UguiNovelTextGeneratorAdditionalLine.Type.Strike, characters, i, generataor));
				}
				if (character.CustomInfo.IsUnderLineTop)
				{
					lineList.Add(new UguiNovelTextGeneratorAdditionalLine(UguiNovelTextGeneratorAdditionalLine.Type.UnderLine, characters, i, generataor));
				}

				//ルビ情報の作成
				if (character.CustomInfo.IsRubyTop)
				{
					rubyList.Add(new UguiNovelTextGeneratorAdditionalRuby(characters, i, generataor));
				}
			}		
		}

		//フォントから文字の情報を設定する
		internal bool TrySetFontCharcters(Font font)
		{
			//線用のフォント設定
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				if (!line.CharacteData.TrySetCharacterInfo(font))
				{
					return false;
				}
			}

			//ルビのフォント設定
			foreach (UguiNovelTextGeneratorAdditionalRuby rubyGroup in rubyList)
			{
				foreach (UguiNovelTextCharacter ruby in rubyGroup.RubyList)
				{
					if (!ruby.TrySetCharacterInfo(font))
					{
						return false;
					}
				}
			}
			return true;
		}

		//全文字情報リストを作成
		internal List<UguiNovelTextCharacter> MakeCharacterList()
		{
			List<UguiNovelTextCharacter> characterList = new List<UguiNovelTextCharacter>();

			//線用のフォント設定
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				characterList.Add(line.CharacteData);
			}

			//ルビのフォント設定
			foreach (UguiNovelTextGeneratorAdditionalRuby rubyGroup in rubyList)
			{
				foreach (UguiNovelTextCharacter ruby in rubyGroup.RubyList)
				{
					characterList.Add(ruby);
				}
			}
			return characterList;
		}

		//文字情報を取得した後の初期化
		internal void InitAfterCharacterInfo(UguiNovelTextGenerator generator)
		{
			foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
			{
				ruby.InitAfterCharacterInfo(generator);
			}
		}

		//行の先頭だった場合のスペースサイズを取得
		internal float GetTopLetterSpace(UguiNovelTextCharacter lineTopCharacter, UguiNovelTextGenerator generator)
		{
			float space = 0;
			foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
			{
				if (!ruby.IsWideType && ruby.TopCharacter == lineTopCharacter)
				{
					space = generator.LetterSpaceSize / 2;
					break;
				}
			}
			return space;
		}

		//文字の最大幅を取得
		internal float GetMaxWidth(UguiNovelTextCharacter currentData)
		{
			if (currentData.CustomInfo.IsRubyTop)
			{
				foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
				{
					if (ruby.TopCharacter == currentData)
					{
						return Mathf.Max(ruby.StringWidth, ruby.RubyWidth);
					}
				}
			}
			return currentData.Width;
		}

		//表示位置の初期化
		internal void InitPosition(UguiNovelTextGenerator generator)
		{
			
			//改行などによって複数の線が必要な場合を考慮
			List<UguiNovelTextGeneratorAdditionalLine> newLineList = new List<UguiNovelTextGeneratorAdditionalLine>();
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				newLineList.AddRange(line.MakeOtherLineInTextLine(generator));
			}
			//新たな線を追加
			lineList.AddRange(newLineList);

			//位置の初期化
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				line.InitPosition(generator);
			}

			foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
			{
				ruby.InitPosition(generator);
			}
		}

		//頂点を作成
		internal void MakeVerts(Color color, UguiNovelTextGenerator generator)
		{
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				line.CharacteData.MakeVerts(color, generator);
			}

			foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
			{
				foreach (UguiNovelTextCharacter character in ruby.RubyList)
				{
					character.MakeVerts(color, generator);
				}
			}
		}

		//描画頂点を追加
		internal void AddVerts(List<UIVertex> verts, Vector2 endPosition, UguiNovelTextGenerator generator)
		{
			foreach (UguiNovelTextGeneratorAdditionalLine line in lineList)
			{
				line.AddDrawVertex(verts,endPosition, generator);
			}

			foreach (UguiNovelTextGeneratorAdditionalRuby ruby in RubyList)
			{
				ruby.AddDrawVertex(verts,endPosition);
			}
		}

	};
}
