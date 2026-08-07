// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// ノベル用のテキスト設定
	/// </summary>
	public class UguiNovelTextSettings : ScriptableObject
	{
		//区切り文字
		[SerializeField]
		string wordWrapSeparator = "!#%&(),-.:<=>?@[]{}";
		internal string WordWrapSeparator
		{
			get { return wordWrapSeparator; }
		}

		//行頭の禁則文字
		[SerializeField]
		string kinsokuTop =
			",)]}、〕〉》」』】〙〗〟’”｠»"
			+ "ゝゞーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇷ゚ㇺㇻㇼㇽㇾㇿ々〻"
			+ "‐゠–〜～"
			+ "?!‼⁇⁈⁉"
			+ "・:;/"
			+ "。."
			+ "，）］｝＝？！：；／"	//全角を追加
			;
		internal string KinsokuTop
		{
			get { return kinsokuTop; }
		}
		
		//行末の禁則文字
		[SerializeField]
		string kinsokuEnd =
			"([{〔〈《「『【〘〖〝‘“｟«"
			+ "（［｛"	//全角を追加
			;
		internal string KinsokuEnd
		{
			get { return kinsokuEnd; }
		}

		//同じ文字が続く場合、文字間が無視される文字
		[SerializeField]
		string ignoreLetterSpace = "…‒–—―⁓〜〰";
		internal string IgnoreLetterSpace
		{
			get { return ignoreLetterSpace; }
		}

		//		string kinsokuBurasage = "、。";						//ぶら下げ組み文字

		// 
		[SerializeField]
		string autoIndentation = "";
		internal string AutoIndentation
		{
			get { return autoIndentation; }
		}

		[SerializeField]
		bool forceIgonreJapaneseKinsoku = false;

		//取り消し線のオフセット
		[SerializeField]
		float strikeOffset = -0.1f;
		internal float StrikeOffset
		{
			get { return strikeOffset; }
		}

		//禁則のチェック
		internal bool IsIgonreLetterSpace(UguiNovelTextCharacter current, UguiNovelTextCharacter next)
		{
			if (current == null || next == null) return false;

			if (current.Char == next.Char)
			{ 
				if( IgnoreLetterSpace.IndexOf(current.Char) >= 0  )
				{
					return true;
				}
			}
			return false;
		}

		//禁則のチェック
		internal bool CheckWordWrap( UguiNovelTextGenerator generator, UguiNovelTextCharacter current, UguiNovelTextCharacter prev)
		{
			//文字間無視文字は改行できない
			if (IsIgonreLetterSpace(prev, current))
			{
				return true;
			}

			bool isWordRapDefault = (generator.WordWrapType & UguiNovelTextGenerator.WordWrap.Default) == UguiNovelTextGenerator.WordWrap.Default;
			bool isJapaneseKinsoku = !forceIgonreJapaneseKinsoku && (generator.WordWrapType & UguiNovelTextGenerator.WordWrap.JapaneseKinsoku) == UguiNovelTextGenerator.WordWrap.JapaneseKinsoku;

			//英文文字のWordWrap
			if (isWordRapDefault)
			{
				if (isJapaneseKinsoku)
				{
					//日本語の禁則がある場合は、半角かどうかで改行チェックを
					if (CheckWordWrapDefaultEnd(prev) && CheckWordWrapDefaultTop(current))
					{
						return true;
					}
				}
				else
				{
					//日本語の禁則がない場合は、
					if (!char.IsSeparator(prev.Char) && !char.IsSeparator(current.Char))
					{
						return true;
					}
				}
			}

			//日本語のWordWrap
			if (isJapaneseKinsoku)
			{
				if ((CheckKinsokuEnd(prev) || CheckKinsokuTop(current)))
				{
					return true;
				}
			}

			return false;
		}

		internal bool IsAutoIndentation (char character)
		{
			return (autoIndentation.IndexOf(character) >= 0);
		}
		

		//英単語のワードラップチェック(行末)
		bool CheckWordWrapDefaultEnd(UguiNovelTextCharacter character)
		{
			char c = character.Char;
			return UtageToolKit.IsHankaku(c) && !char.IsSeparator(c) && !(wordWrapSeparator.IndexOf(c) >= 0);
		}

		//英単語のワードラップチェック(行頭)
		bool CheckWordWrapDefaultTop(UguiNovelTextCharacter character)
		{
			char c = character.Char;
			return UtageToolKit.IsHankaku(c) && !char.IsSeparator(c);
		}

		//ぶら下げ文字のチェック
		bool CheckKinsokuBurasage(UguiNovelTextCharacter c)
		{
			return false;
		}

		//行頭の禁則文字のチェック
		bool CheckKinsokuTop(UguiNovelTextCharacter character)
		{
			return (kinsokuTop.IndexOf(character.Char) >= 0);
		}
		//行末の禁則文字のチェック
		bool CheckKinsokuEnd(UguiNovelTextCharacter character)
		{
			return (kinsokuEnd.IndexOf(character.Char) >= 0);
		}
	}
}
