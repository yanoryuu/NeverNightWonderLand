// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Utage
{

	/// <summary>
	/// エクセルシートの解析。シナリオなのか、設定シートなのか…など
	/// ここでまとめる
	/// </summary>
	public class AdvSheetParser
	{
		public const string SheetNameBoot = "Boot";
		public const string SheetNameScenario = "Scenario";

		const string SheetNameCharacter = "Character";
		const string SheetNameTexture = "Texture";
		const string SheetNameSound = "Sound";
		const string SheetNameLayer = "Layer";
		const string SheetNameSceneGallery = "SceneGallery";
		const string SheetNameLocalize = "Localize";
		const string SheetNameAnimation = "Animation";
		const string SheetNameLipSynch = "LipSynch";
		const string SheetNameEyeBlink = "EyeBlink";
		const string SheetNameParticle = "Particle";		


		/// <summary>
		/// 無効なシート名か判定
		/// </summary>
		/// <param name="sheetName">シート名</param>
		/// <returns>設定用ならtrue。違うならfalse</returns>
		public static bool IsDisableSheetName(string sheetName)
		{
			switch (sheetName)
			{
				case SheetNameBoot:
				case SheetNameScenario:
					return true;
				default:
					return false;
			}
		}


		/// <summary>
		/// 設定用のエクセルシートか判定
		/// </summary>
		/// <param name="sheetName">シート名</param>
		/// <returns>設定用ならtrue。違うならfalse</returns>
		public static bool IsSettingsSheet(string sheetName)
		{
			switch (sheetName)
			{
				case SheetNameScenario:
				case SheetNameCharacter:
				case SheetNameTexture:
				case SheetNameSound:
				case SheetNameLayer:
				case SheetNameSceneGallery:
				case SheetNameLocalize:
				case SheetNameEyeBlink:
				case SheetNameLipSynch:
				case SheetNameParticle:
					return true;
				default:
					return IsParamSheetName(sheetName) || IsAnimationSheetName(sheetName);
			}
		}


		public static bool IsScenarioSheet(string sheetName)
		{
			if (IsDisableSheetName(sheetName)) return false;
			if (IsSettingsSheet(sheetName)) return false;

			//後付けなので強引・・・
			var customProjectSetting = CustomProjectSetting.Instance;
			if (customProjectSetting != null)
			{
				if (customProjectSetting.CustomDataSettings.IsCustomData(sheetName))
				{
					return false;
				}
			}
			return true;
		}


		static readonly Regex SheetNameRegex = new Regex(@"(.+)\{\}", RegexOptions.IgnorePatternWhitespace);
		//パラメーターシート名か
		public static bool IsParamSheetName(string sheetName)
		{
			if (sheetName == AdvParamManager.DefaultSheetName) return true;
			Match match = SheetNameRegex.Match(sheetName);
			return match.Success;
		}

		static readonly Regex AnimationSheetNameRegix = new Regex(@"(.+)\[\]", RegexOptions.IgnorePatternWhitespace);
		//アニメーションシート名か
		static bool IsAnimationSheetName(string sheetName)
		{
			if (sheetName == SheetNameAnimation) return true;
			Match match = AnimationSheetNameRegix.Match(sheetName);
			return match.Success;
		}

		//シート名から、起動TSVのタグ名に変換
		public static string ToBootTsvTagName(string sheetName)
		{
			string tagName = sheetName;
			//シート名＝タグ名ではない場合も考慮
			if (IsParamSheetName(sheetName))
			{
				tagName = "Param";
			}
			else if (IsAnimationSheetName(sheetName))
			{
				tagName = "Animation";
			}
			return tagName + "Setting";
		}

		/// <summary>
		/// 設定データを探す
		/// </summary>
		public static IAdvSetting FindSettingData(AdvSettingDataManager settingManager, string sheetName)
		{
			switch (sheetName)
			{
				case SheetNameCharacter:
					return settingManager.CharacterSetting;
				case SheetNameTexture:
					return settingManager.TextureSetting;
				case SheetNameSound:
					return settingManager.SoundSetting;
				case SheetNameLayer:
					return settingManager.LayerSetting;
				case SheetNameSceneGallery:
					return settingManager.SceneGallerySetting;
				case SheetNameLocalize:
					return settingManager.LocalizeSetting;
				case SheetNameEyeBlink:
					return settingManager.EyeBlinkSetting;
				case SheetNameLipSynch:
					return settingManager.LipSynchSetting;
				case SheetNameParticle:
					return settingManager.ParticleSetting;
				default:
					if (IsParamSheetName(sheetName))
					{
						return settingManager.DefaultParam;
					}
					else if (IsAnimationSheetName(sheetName))
					{
						return settingManager.AnimationSetting;
					}
					else
					{
						Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotSettingSheet, sheetName));
						return null;
					}
			}
		}
	}
}
