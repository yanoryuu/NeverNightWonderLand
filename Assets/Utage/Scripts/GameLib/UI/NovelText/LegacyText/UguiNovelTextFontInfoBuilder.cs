// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;
using UnityEngine.Profiling;
using System;

namespace Utage
{

	/// <summary>
	/// ノベル用テキストのフォント情報を作成するビルダー
	/// </summary>
	internal class UguiNovelTextFontInfoBuilder
	{
		bool RequestingCharactersInTexture { get; set; }

		//フォントの文字画像を準備・設定
		internal void InitFontCharactes(Font font, List<UguiNovelTextCharacter> characterDataList, UguiNovelTextGeneratorAdditional additional)
		{
			bool isComplete = false;
			//再試行回数
			int retryCount = 5;
			for (int i = 0; i < retryCount; ++i)
			{
				if (TryeSetFontCharcters(font, characterDataList, additional))
				{
					isComplete = true;
					break;
				}
				else
				{
					RequestCharactersInTexture(font, characterDataList, additional);
					if (i == retryCount - 1)
					{
						SetFontCharcters(font, characterDataList);
					}
				}
			}
			if (!isComplete)
			{
				Debug.LogError("InitFontCharactes Error");
				TryeSetFontCharcters(font, characterDataList, additional);
			}
		}

		//フォントのテクスチャだけ再作成（つまり、文字情報のうちUV座標だけ変更）
		internal void RebuildFontTexture(Font font, List<UguiNovelTextCharacter> characterDataList, UguiNovelTextGeneratorAdditional additional)
		{
			throw new NotImplementedException();
		}

		//フォントの文字画像の設定を試行
		bool TryeSetFontCharcters(Font font, List<UguiNovelTextCharacter> characterDataList, UguiNovelTextGeneratorAdditional additional)
		{
			foreach (UguiNovelTextCharacter character in characterDataList)
			{
				if (!character.TrySetCharacterInfo(font))
				{
					return false;
				}
			}
			return additional.TrySetFontCharcters(font);
		}

		//フォントの文字画像を設定・エラーの場合もそのまま
		void SetFontCharcters(Font font, List<UguiNovelTextCharacter> characterDataList)
		{
			foreach (UguiNovelTextCharacter character in characterDataList)
			{
				character.SetCharacterInfo(font);
			}
		}


		//フォントの文字画像の作成リクエスト
		void RequestCharactersInTexture(Font font, List<UguiNovelTextCharacter> characterDataList, UguiNovelTextGeneratorAdditional additional)
		{
			List<RequestCharactersInfo> infoList = MakeRequestCharactersInfoList(characterDataList, additional);
			RequestingCharactersInTexture = true;

			Font.textureRebuilt += FontTextureRebuildCallback;
			foreach (RequestCharactersInfo info in infoList)
			{
				font.RequestCharactersInTexture(info.characters, info.size, info.style);
			}
			Font.textureRebuilt -= FontTextureRebuildCallback;
			RequestingCharactersInTexture = false;
		}

		void FontTextureRebuildCallback(Font font)
		{
//			Debug.LogError("FontTextureRebuildCallback");
		}

		//フォントの文字画像の作成のための情報を作成
		List<RequestCharactersInfo> MakeRequestCharactersInfoList(List<UguiNovelTextCharacter> characterDataList, UguiNovelTextGeneratorAdditional additional)
		{
			List<RequestCharactersInfo> infoList = new List<RequestCharactersInfo>();
			foreach (UguiNovelTextCharacter characterData in characterDataList)
			{
				AddToRequestCharactersInfoList(characterData, infoList);
			}
			List<UguiNovelTextCharacter> additionalCharacterList = additional.MakeCharacterList();
			foreach (UguiNovelTextCharacter characterData in additionalCharacterList)
			{
				AddToRequestCharactersInfoList(characterData, infoList);
			}
			return infoList;
		}

		//フォントの文字画像の作成のための情報を作成
		void AddToRequestCharactersInfoList(UguiNovelTextCharacter characterData, List<RequestCharactersInfo> infoList)
		{
			if (characterData.IsNoFontData) return;

			foreach (RequestCharactersInfo info in infoList)
			{
				if (info.TryAddData(characterData))
				{
					return;
				}
			}
			infoList.Add(new RequestCharactersInfo(characterData));
		}

		internal class RequestCharactersInfo
		{
			public string characters;
			public readonly int size;
			public readonly FontStyle style;

			public RequestCharactersInfo(UguiNovelTextCharacter data)
			{
				characters = "" + data.Char;
				size = data.FontSize;
				style = data.FontStyle;
			}
			public bool TryAddData(UguiNovelTextCharacter data)
			{
				if (size == data.FontSize && style == data.FontStyle)
				{
					characters += data.Char;
					return true;
				}
				else
				{
					return false;
				}
			}
		}

	}
}
