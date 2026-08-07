using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定を作成し、現在のシーンにAdvEngineを追加するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/Add Scene/Default(Text Mesh Pro)")]
    public class AdvProjectTemplateSettingsAddSceneDefaultTMP 
        : AdvProjectTemplateSettingsAddSceneDefault
    {
        [SerializeField] AdvProjectTemplateFontSettingsTMP fontSettings;

        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorNewSceneDefaultTMP(this);
        }

        [Serializable]
        protected class AdvProjectCreatorNewSceneDefaultTMP
            : AdvProjectCreatorNewSceneDefault<AdvProjectTemplateSettingsAddSceneDefaultTMP>
                , IAdvProjectCreatorFontTMP
        {
            public AdvProjectTemplateFontSettingsTMP FontSettings => Settings.fontSettings;
            [field: SerializeField, StringPopupFunction(nameof(GetLanguages), true)] public string FontLanguage { get; set; }

            //対応言語名のリストを取得
            List<string> GetLanguages
            {
                get
                {
                    if (FontSettings.Font == null) return new List<string>();
                    return FontSettings.Font.FontLanguages;
                }
            }

            public AdvProjectCreatorNewSceneDefaultTMP(AdvProjectTemplateSettingsAddSceneDefaultTMP settings)
            :base(settings)
            {
                FontLanguage = FontSettings.Font.GetSystemLanguageName();
            }

            public override AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator)
            {
                return new AdvProjectCreatorFontChangerTMP(this, assetsCreator);
            }
        }
    }
}
