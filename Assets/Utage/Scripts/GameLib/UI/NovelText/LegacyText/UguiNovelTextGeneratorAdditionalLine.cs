// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//ノベル用の線（アンダーラインや取り消し線）描画情報
	public class UguiNovelTextGeneratorAdditionalLine
	{
		//タイプ
		public enum Type
		{
			UnderLine,
			Strike,
		};
		public Type LineType{ get { return type; }}
		Type type;

		//線の文字データ
		public UguiNovelTextCharacter CharacteData { get { return characteData; } }
		UguiNovelTextCharacter characteData;

		//線を引く文字列データ
		List<UguiNovelTextCharacter> stringData = new List<UguiNovelTextCharacter>();

		//線を引く先頭の文字
		internal UguiNovelTextCharacter TopCharacter { get { return stringData[0]; } }

		//表示行
		UguiNovelTextLine textLine;

		internal UguiNovelTextGeneratorAdditionalLine( Type type, List<UguiNovelTextCharacter> characters, int index, UguiNovelTextGenerator generator)
		{
			InitSub(type,generator);
			stringData.Add(characters[index]);
			for (int i = index + 1; i < characters.Count; ++i)
			{
				UguiNovelTextCharacter c = characters[i];
				if (IsLineEnd(c)) break;
				stringData.Add(c);
			}
		}

		//自動改行などのために線を増やす必要がある場合
		UguiNovelTextGeneratorAdditionalLine(UguiNovelTextGeneratorAdditionalLine srcLine, int index, int count, UguiNovelTextGenerator generator)
		{
			InitSub(srcLine.type, generator);
			for (int i = 0; i < count; ++i)
			{
				stringData.Add(srcLine.stringData[index+i]);
			}
		}

		void InitSub(Type type, UguiNovelTextGenerator generator)
		{
			this.type = type;
			//ダッシュ（'—'）の文字を作成
			CharData data = new CharData(generator.DashChar);
			data.CustomInfo.IsDash = true;
			data.CustomInfo.DashSize = 1;
			characteData = new UguiNovelTextCharacter(data, generator);
		}

		//
		bool IsLineEnd(UguiNovelTextCharacter c)
		{
			switch(LineType)
			{
				case Type.Strike:
					return (!c.CustomInfo.IsStrike || c.CustomInfo.IsStrikeTop);
				case Type.UnderLine:
					return (!c.CustomInfo.IsUnderLine || c.CustomInfo.IsUnderLineTop);
				default:
					return false;
			}
		}

		//改行などで別の行に線を引く必要があるばあい、その線を作成
		internal List<UguiNovelTextGeneratorAdditionalLine> MakeOtherLineInTextLine(UguiNovelTextGenerator generator)
		{
			InitTextLine(generator);
			return MakeOtherLineInTextLineSub(generator);
		}

		void InitTextLine(UguiNovelTextGenerator generator)
		{
			foreach (UguiNovelTextLine line in generator.LineDataList)
			{
				if (line.Characters.IndexOf(TopCharacter) >= 0)
				{
					this.textLine = line;
				}
			}
		}
		
		//改行などで別の行に線を引く必要があるばあい、その線を作成
		internal List<UguiNovelTextGeneratorAdditionalLine> MakeOtherLineInTextLineSub(UguiNovelTextGenerator generator)
		{
			List<UguiNovelTextGeneratorAdditionalLine> newLineList = new List<UguiNovelTextGeneratorAdditionalLine>();

			int endIndex = stringData.Count - 1;
			foreach (UguiNovelTextLine line in generator.LineDataList)
			{
				if ( textLine == line ) continue;

				bool isFind = false;
				int index = 0;
				int count = 0;
				for (int i = 0; i < stringData.Count; ++i )
				{
					//他の行に文字がある
					if (line.Characters.IndexOf(stringData[i]) >= 0)
					{
						if (!isFind)
						{
							index = i;
							endIndex = Mathf.Min(i, endIndex);
							isFind = true;
						}
						++count;
					}
				}
				if (isFind)
				{
					UguiNovelTextGeneratorAdditionalLine newLine = new UguiNovelTextGeneratorAdditionalLine(this, index, count,generator);
					newLineList.Add(newLine);
					newLine.InitTextLine(generator);
					if (!newLine.characteData.TrySetCharacterInfo(generator.NovelText.font))
					{
						Debug.LogError("Line Font Missing");
					}
				}
			}
			if(newLineList.Count> 0)
			{
				stringData.RemoveRange(endIndex, stringData.Count-endIndex);
			}
			return newLineList;
		}

		//位置情報の初期化
		internal void InitPosition(UguiNovelTextGenerator generator)
		{
			//文字からの表示位置のオフセット
			Vector2 offset = Vector2.zero;

			float height = textLine.MaxFontSize;

			//高さを合わせる
			switch( LineType )
			{
				case Type.UnderLine:
					offset.y -= height / 2;
					break;
				case Type.Strike:
					offset.y += generator.TextSettings.StrikeOffset * textLine.MaxFontSize;
					break;
			}

			CharacteData.InitPositionX(TopCharacter.PositionX + offset.x);
			CharacteData.InitPositionY(TopCharacter.PositionY + offset.y);
		}

		//描画用の頂点情報を追加(文字送りに対応)
		internal void AddDrawVertex(List<UIVertex> verts, Vector2 endPosition, UguiNovelTextGenerator generator)
		{
			if (!TopCharacter.IsVisible) return;

			float xMin = TopCharacter.PositionX;
			float xMax = TopCharacter.EndPositionX;

			foreach (UguiNovelTextCharacter c in stringData)
			{
				if (!c.IsVisible) break;
				xMax = Mathf.Max( xMax, c.EndPositionX);
			}
			Color color = Color.white;
			foreach (UguiNovelTextCharacter c in stringData)
			{
				if (!c.IsVisible) break;
				if (c.Verts != null)
				{
					color = c.Verts[0].color;
				}
			}
			CharacteData.Width = xMax - xMin;
			CharacteData.MakeVerts(color, generator);
			verts.AddRange(CharacteData.Verts);
		}

	};
}
