using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定（アセット）のみを作成するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/Asset Only/Default(Text Mesh Pro)")]
    public class AdvProjectTemplateSettingsAssetOnlyDefaultTMP : AdvProjectTemplateSettingsAssetOnlyDefault
    {
        [SerializeField] AdvProjectTemplateFontSettingsTMP fontSettings;

        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorAssetOnlyDefaultTMP(this);
        }

        [Serializable]
        class AdvProjectCreatorAssetOnlyDefaultTMP
            : AdvProjectCreatorAssetOnlyDefault<AdvProjectTemplateSettingsAssetOnlyDefaultTMP>
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

            public AdvProjectCreatorAssetOnlyDefaultTMP(AdvProjectTemplateSettingsAssetOnlyDefault settings) 
                : base(settings)
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
