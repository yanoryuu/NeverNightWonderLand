// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//ノベル用のテキスト一行あたりの情報
	public class UguiNovelTextLine
	{
		public List<UguiNovelTextCharacter> Characters { get { return characters; } }
		List<UguiNovelTextCharacter> characters = new List<UguiNovelTextCharacter>();

		public List<UguiNovelTextCharacter> RubyCharacters { get { return rubyCharacters; } }
		List<UguiNovelTextCharacter> rubyCharacters = new List<UguiNovelTextCharacter>();

		//指定の範囲からはみ出しているか
		public bool IsOver { get { return isOver; } set { isOver = value; } }
		bool isOver;

		//行内の最大フォントサイズ
		public int MaxFontSize { get { return maxFontSize; } }
		int maxFontSize;

		//行の幅
		public float Width { get { return width; } }
		float width;

		//行の高さ（行間含む）
		public float TotalHeight { get { return totalHeight; } }
		float totalHeight;

		//列のY位置(Y座標はアンダーラインの位置)
		public float Y0 { get; set; }

		public void AddCharaData(UguiNovelTextCharacter charaData)
		{
			characters.Add(charaData);
		}

		public void EndCharaData( UguiNovelTextGenerator generator )
		{
			//幅と、最大フォントサイズなどを設定
			maxFontSize = 0;
			float left = 0;
			for (int i = 0; i < characters.Count; ++i)
			{
				UguiNovelTextCharacter character = characters[i];
				maxFontSize = Mathf.Max(maxFontSize, character.DefaultFontSize);
				if (i == 0)
				{
					left = character.PositionX - character.RubySpaceSize;
				}
			}

			float right = 0;
			for (int i = characters.Count - 1; i >= 0; --i)
			{
				UguiNovelTextCharacter character = characters[i];
				if (!character.IsBr)
				{
					right = character.PositionX + character.Width + character.RubySpaceSize;
					break;
				}
			}

			width = Mathf.Abs(right-left);
			//uGUIは行間の基本値1=1.2の模様
			totalHeight = generator.NovelText.GetTotalLineHeight(MaxFontSize);
		}

		//Y座標を設定
		public void SetLineY(float y, UguiNovelTextGenerator generator )
		{
			Y0 = y;			//描画するサイズと、フォントデータのサイズでY値のオフセットをとる
			foreach (UguiNovelTextCharacter character in characters)
			{
				character.InitPositionY(Y0);
			}
		}


		//X座標に対してテキストアンカーを適用する
		public void ApplyTextAnchorX(float pivotX, float maxWidth)
		{
			if (pivotX == 0) return;

			float offsetX = (maxWidth - Width) * pivotX;
			foreach (UguiNovelTextCharacter character in characters)
			{
				character.ApplyOffsetX(offsetX);
			}
		}

		//Y座標に対してテキストアンカーを適用する
		public void ApplyTextAnchorY( float offsetY )
		{
			Y0 += offsetY;
			foreach (UguiNovelTextCharacter character in characters)
			{
				character.ApplyOffsetY(offsetY);
			}			
		}

		//オフセットを適用する
		public void ApplyOffset( Vector2 offset)
		{
			Y0 += offset.y;
			foreach (UguiNovelTextCharacter character in characters)
			{
				character.ApplyOffsetX(offset.x);
				character.ApplyOffsetY(offset.y);
			}
		}
	}
}
