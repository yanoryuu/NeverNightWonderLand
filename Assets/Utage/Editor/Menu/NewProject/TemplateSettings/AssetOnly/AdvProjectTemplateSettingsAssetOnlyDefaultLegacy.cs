using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定（アセット）のみを作成するデフォルト設定
    [CreateAssetMenu(menuName = "Utage/Editor/Project Template Settings/Asset Only/Default(Legacy)")]
    public class AdvProjectTemplateSettingsAssetOnlyDefaultLegacy : AdvProjectTemplateSettingsAssetOnlyDefault
    {
        [SerializeField] Font font;
        public override AdvProjectCreator CreateProjectCreatorSettings()
        {
            return new AdvProjectCreatorAssetOnlyDefaultLegacy(this);
        }

        [Serializable]
        class AdvProjectCreatorAssetOnlyDefaultLegacy
            : AdvProjectCreatorAssetOnlyDefault<AdvProjectTemplateSettingsAssetOnlyDefaultLegacy>
                , IAdvProjectCreatorFontLegacy
        {
            public Font DefaultFont => this.Settings.font;
            [field: SerializeField] public Font Font { get; set; }

            public AdvProjectCreatorAssetOnlyDefaultLegacy(AdvProjectTemplateSettingsAssetOnlyDefault settings) 
                : base(settings)
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
