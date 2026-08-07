using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定を作成し、現在のシーンにAdvEngineを追加するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/Add Scene/Default(Legacy)")]
    public class AdvProjectTemplateSettingsAddSceneDefaultLegacy 
        : AdvProjectTemplateSettingsAddSceneDefault
    {
        [SerializeField] Font font;

        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorNewSceneDefaultLegacy(this);
        }
        
        [Serializable]
        protected class AdvProjectCreatorNewSceneDefaultLegacy
            : AdvProjectCreatorNewSceneDefault<AdvProjectTemplateSettingsAddSceneDefaultLegacy>
                , IAdvProjectCreatorFontLegacy
        {
            public Font DefaultFont => this.Settings.font; 
            [field: SerializeField] public Font Font { get; set; }

            public AdvProjectCreatorNewSceneDefaultLegacy(AdvProjectTemplateSettingsAddSceneDefaultLegacy settings)
                :base(settings)
            {
                Font = DefaultFont;
            }

            public override AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator)
            {
                return new AdvProjectCreatorFontChangerLegacy(this, assetsCreator);
            }
        }
    }
}
