using UnityEngine;

namespace Utage
{
	public class UtageRuntimeInitialize
	{
		//Domain Reloadingがオフになる場合の対応
		//ランタイム初期化時に、staticな変数やコールバックを初期化する
		//RuntimeInitializeStaticFieldを設定しているものは、以下で初期化が必要なものとする
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void RuntimeInitialize()
		{
			AdvCommand.IsEditorErrorCheck = false;
			AdvCommand.IsEditorErrorCheckWaitType = false;
			AdvCommandParser.OnCreateCustomCommandFromID = null;
			AdvCharacterSettingData.CallbackParseCustomFileTypeRootDir = null;
			AdvTextureSettingData.CallbackParseCustomFileTypeRootDir = null;
			AdvGraphicInfo.CallbackExpression = null;
			AdvGraphicInfo.CallbackCreateCustom = null;
			CustomProjectSetting.Instance = null;
			AssetFileManager.IsEditorErrorCheck = false;
			TextData.CreateCustomTextParser = null;
			TextData.MakeCustomLogText = null;
			TextParser.CallbackCalcExpression = null;
			InputUtil.EnableInput = true;
			InputUtil.SetInputStrategy(null);
			iTweenData.CallbackGetValue = null;
		}
	}
}
