using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Utage
{
    //Adv用の、テキストを検証するクラス。
    //文字あふれ（表示テキストが一定範囲を超えていないか）をチェックします。
    //多言語の場合、全ての言語をチェック可能。同時に、翻訳言語が足りない場合のチェックも可能。
    public class AdvTextValidator
    {
        AdvTextValidatorUserSettings UserSettings { get; }
        
        public AdvTextValidator(AdvTextValidatorUserSettings userSettings)
        {
            UserSettings = userSettings;
        }
        
        //検証する
        public void Validate(List<AdvScenarioData> scenarioList)
        {
	        switch (UserSettings.ValidateType)
	        {
		        case AdvTextValidatorUserSettings.ValidatorType.Enable:
			        CheckCharacterCount(scenarioList, "");
			        break;
		        case AdvTextValidatorUserSettings.ValidatorType.AllLanguage:
			        var langManager = LanguageManagerBase.Instance;
			        if (langManager != null)
			        {
				        string defLanguage = langManager.CurrentLanguage;
				        foreach (var language in langManager.Languages)
				        {
					        langManager.CurrentLanguage = language;
					        CheckCharacterCount(scenarioList, language);
				        }
				        langManager.CurrentLanguage = defLanguage;
			        }
			        break;
		        case AdvTextValidatorUserSettings.ValidatorType.Disable:
			        break;
		        default:
			        Debug.LogError($"Unknown ValidatorType {UserSettings.ValidateType}");
			        break;
	        }
			
		}

		void CheckCharacterCount(List<AdvScenarioData> scenarioList, string language)
		{
			var engine = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngine>();
			if(engine==null) return;
			Debug.Log($"Validate Text {language} …");
			AdvScenarioCharacterCountChecker checker = new AdvScenarioCharacterCountChecker(engine);
			if (checker.TryCheckCharacterCount(scenarioList, out var count))
			{
				if (string.IsNullOrEmpty(language))
				{
					Debug.Log(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.ChacacterCountOnImport, count));
				}
				else
				{
					Debug.Log(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.ChacacterCountOnImport, count) + "  in " + language);
				}
			}
		}

    }

}
