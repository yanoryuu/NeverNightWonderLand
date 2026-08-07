// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// ノベルテキストの当たり判定データ
	/// </summary>
	public class UguiNovelTextHitArea
	{
		public enum Type
		{
			Link,
			Sound,
		};

		UguiNovelText novelText;
		public CharData.HitEventType HitEventType { get; private set; }
		public string Arg { get; private set; }

		List<UguiNovelTextCharacter> characters = new List<UguiNovelTextCharacter>();
		public List<Rect> HitAreaList { get { return this.hitAreaList; } }
		List<Rect> hitAreaList = new List<Rect>();

		public UguiNovelTextHitArea(UguiNovelText novelText, CharData.HitEventType type, string arg, List<UguiNovelTextCharacter> characters)
		{
			this.novelText = novelText;
			this.HitEventType = type;
			this.Arg = arg;
			this.characters = characters;
		}

		//ヒットエリアを作成
		public void RefreshHitAreaList()
		{
			this.hitAreaList.Clear();
			
			List<UguiNovelTextCharacter> lineCharacters = new List<UguiNovelTextCharacter>();
			foreach (UguiNovelTextCharacter character in characters)
			{
				if(!character.IsBr && character.IsVisible)
				{
					lineCharacters.Add(character);
				}
				if (character.IsBrOrAutoBr)
				{
					if (lineCharacters.Count > 0) hitAreaList.Add(MakeHitArea(lineCharacters));
					lineCharacters.Clear();
				}
			}
			if (lineCharacters.Count > 0) hitAreaList.Add(MakeHitArea(lineCharacters));
		}

		//1行ぶんのヒットエリアを作成
		Rect MakeHitArea(List<UguiNovelTextCharacter> lineCharacters)
		{
			UguiNovelTextCharacter topCharacter = lineCharacters[0];
			float xMin = topCharacter.BeginPositionX;
			float xMax = topCharacter.EndPositionX;
			int fontSizeMax = 0;
			foreach (UguiNovelTextCharacter c in lineCharacters)
			{
				xMax = Mathf.Max( xMax, c.EndPositionX );
				fontSizeMax = Mathf.Max(fontSizeMax, c.FontSize);
			}

			//行間含んだ高さ
			int totalHeight = novelText.GetTotalLineHeight(fontSizeMax);
			float yMin = topCharacter.PositionY - (totalHeight - fontSizeMax) / 2.0f;
			return new Rect(xMin, yMin, xMax - xMin, totalHeight);
		}

		internal bool HitTest(Vector2 localPosition)
		{
			foreach( Rect rect in hitAreaList )
			{
				if (rect.Contains(localPosition))
				{
					return true;
				}
			}
			return false;
		}

		//クリック可能なことを示すエフェクトカラーを設定
		internal void ChangeEffectColor(Color effectColor)
		{
			foreach (var c in characters)
			{
				c.ChangeEffectColor(effectColor);
			}
			novelText.RemakeVerts();
		}

		//クリック可能なことを示すエフェクトカラーをリセット
		internal void ResetEffectColor()
		{
			foreach (var c in characters)
			{
				c.ResetEffectColor();
			}
			novelText.RemakeVerts();
		}
	}
}
