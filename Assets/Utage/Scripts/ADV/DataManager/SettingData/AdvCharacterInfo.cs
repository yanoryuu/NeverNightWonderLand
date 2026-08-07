// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// キャラクタの表示情報
	/// </summary>
	public class AdvCharacterInfo
	{
		public static AdvCharacterInfo Create( AdvCommand command, AdvSettingDataManager dataManager )
		{
			if (command.IsEmptyCell(AdvColumnName.Arg1))
			{
				return null;
			}

			//名前
			string nameText = command.ParseCell<string>(AdvColumnName.Arg1);
			string characterLabel = nameText;
			//第二引数を解析
			//基本的にはパターン名だが
			//キャラクターラベルの指定タグがあったり、非表示タグする
			bool isHide = false;
			string erroMsg = "";
			string pattern = ParserUtil.ParseTagTextToString(
				command.ParseCellOptional<string>(AdvColumnName.Arg2, ""),
				(tagName, arg) =>
				{
					bool failed = false;
					switch (tagName)
					{
						case "Off":
							//非表示タグ
							isHide = true;
							break;
						case "Character":
							//キャラクターラベルの指定タグ
							characterLabel = arg;
							break;
						default:
							erroMsg = "Unknown Tag <" + tagName + ">";
							failed = true;
							break;
					}
					return !failed;
				});

			if(!string.IsNullOrEmpty(erroMsg))
			{
				Debug.LogError(erroMsg);
				return null;
			}

			AdvCharacterSetting setting = dataManager.CharacterSetting;
			if (!setting.Contains(characterLabel))
			{
				//そもそもキャラ表示がない場合、名前表示のみになる
				return new AdvCharacterInfo(characterLabel, nameText, pattern, "", isHide, null);
			}

			AdvCharacterSettingData data = dataManager.CharacterSetting.GetCharacterData(characterLabel, pattern, out string newPattern, out string motion);
			//キャラの表示情報の記述エラー
			if (data == null)
			{
				Debug.LogError(command.ToErrorString(characterLabel + ", " + pattern + " is not contained in Character Sheet"));
				return null;
			}
			//名前テキストをキャラクターシートの定義に変更
			if (!string.IsNullOrEmpty(data.NameText) && nameText == characterLabel)
			{
				nameText = data.NameText;
			}
			return new AdvCharacterInfo(characterLabel, nameText, newPattern, motion, isHide, data.Graphic);
		}

		public static string ParsePatternOnly(AdvCommand command)
		{
			return ParserUtil.ParseTagTextToString(
				command.ParseCellOptional<string>(AdvColumnName.Arg2, ""),
				(tagName, arg) =>
				{
					bool failed = false;
					switch (tagName)
					{
						case "Off":
							//非表示タグ
							break;
						case "Character":
							//キャラクターラベルの指定タグ
							break;
						default:
							failed = true;
							break;
					}
					return !failed;
				});

		}


		AdvCharacterInfo(string label, string nameText, string pattern, string motion, bool isHide, AdvGraphicInfoList graphic)
		{
			this.Label = label;
			this.NameText = nameText;
			this.Pattern = pattern;
			this.Motion = motion;
			this.IsHide = isHide;
			this.Graphic = graphic;
		}

		public string Label { get; private set; }

		public string NameText { get; private set; }

		public string Pattern { get; private set; }

		public string Motion { get; private set; }

		public bool IsHide { get; private set; }

		public AdvGraphicInfoList Graphic { get; private set; }
		public string LocalizeNameText
		{
			get
			{
				return LanguageManager.Instance.LocalizeText( TextData.MakeLogText(this.NameText));
			}
		}
	}
}
