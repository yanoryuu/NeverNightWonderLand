using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定（アセット）のみを作成するデフォルト設定
    public abstract class AdvProjectTemplateSettingsAssetOnlyDefault
        : AdvProjectTemplateSettingsAssetOnly
    {
        [Serializable]
        protected abstract class AdvProjectCreatorAssetOnlyDefault<T>
            : AdvProjectCreator
                , IAdvProjectCreatorAssetOnly
                , IAdvProjectCreatorFont
            where T : AdvProjectTemplateSettingsAssetOnlyDefault
        {
            protected T Settings => TemplateSettings as T;

            protected AdvProjectCreatorAssetOnlyDefault(AdvProjectTemplateSettingsAssetOnlyDefault settings)
                :base(settings)
            {
            }

            public abstract AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator);

            public override bool EnableCreate()
            {
                //シーンアセットがない場合でも作成可能
                return true;
            }

            protected override void OnCreate()
            {
                //テンプレートからアセットをコピー
                var assetsCreator = new AdvProjectCreatorAssets(this);
                assetsCreator.Create();

                //フォントの変更処理
                AdvProjectCreatorFontChanger fontChanger = CreateFontChanger(assetsCreator);
                fontChanger.ChangeFontAsset();

                //シナリオデータを作成
                var scenarioDataCreator = new AdvProjectCreatorScenarioDataProject(this);
                scenarioDataCreator.CreateScenarioDataProject();
            }
        }
    }
}
