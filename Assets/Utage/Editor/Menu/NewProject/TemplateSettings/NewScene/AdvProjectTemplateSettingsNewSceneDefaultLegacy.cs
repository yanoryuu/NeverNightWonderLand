using System;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //テンプレートを元にプロジェクト設定と新規シーンと作成するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/New Scene/Default(Legacy)")]
    public class AdvProjectTemplateSettingsNewSceneDefaultLegacy 
        : AdvProjectTemplateSettingsNewSceneDefault
    {
        [SerializeField] Font font;
        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorNewSceneDefault(this);
        }

        //エディターwindowに表示する設定項目を持ち、プロジェクト作成を行うクリエーター
        [Serializable]
        protected class AdvProjectCreatorNewSceneDefault
            : AdvProjectCreatorNewSceneDefault<AdvProjectTemplateSettingsNewSceneDefaultLegacy>
                , IAdvProjectCreatorFontLegacy
        {
            public Font DefaultFont => this.Settings.font;
            [field: SerializeField] public Font Font { get; set; }

            public AdvProjectCreatorNewSceneDefault(AdvProjectTemplateSettingsNewSceneDefaultLegacy settings)
            :base(settings)
            {
                Font = settings.font;
            }

            public override AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator)
            {
                return new AdvProjectCreatorFontChangerLegacy(this, assetsCreator);
            }
        }
    }
}
