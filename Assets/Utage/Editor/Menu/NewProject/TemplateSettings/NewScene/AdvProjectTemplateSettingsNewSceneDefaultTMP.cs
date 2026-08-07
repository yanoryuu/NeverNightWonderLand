using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定と新規シーンと作成するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/New Scene/Default(Text Mesh Pro)")]
    public class AdvProjectTemplateSettingsNewSceneDefaultTMP 
        : AdvProjectTemplateSettingsNewSceneDefault
    {
        [SerializeField] AdvProjectTemplateFontSettingsTMP fontSettings;

        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorNewSceneDefaultTMP(this);
        }

        //エディターwindowに表示する設定項目を持ち、プロジェクト作成を行うクリエーター
        [Serializable]
        protected class AdvProjectCreatorNewSceneDefaultTMP
            : AdvProjectCreatorNewSceneDefault<AdvProjectTemplateSettingsNewSceneDefaultTMP>
                , IAdvProjectCreatorFontTMP
        {
            public AdvProjectTemplateFontSettingsTMP FontSettings => Settings.fontSettings;
            [field: SerializeField,StringPopupFunction(nameof(GetLanguages),true)] public string FontLanguage { get; set; }

            //対応言語名のリストを取得
            List<string> GetLanguages
            {
                get
                {
                    if(FontSettings.Font==null) return new List<string>();
                    return FontSettings.Font.FontLanguages;
                }
            }

            public AdvProjectCreatorNewSceneDefaultTMP(AdvProjectTemplateSettingsNewSceneDefaultTMP settings)
                :base(settings)
            {
                //PCの言語設定にあう言語を初期値として設定する
                FontLanguage = FontSettings.Font.GetSystemLanguageName();
            }
            

            public override AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator)
            {
                return new AdvProjectCreatorFontChangerTMP(this, assetsCreator);
            }
        }
    }
}
