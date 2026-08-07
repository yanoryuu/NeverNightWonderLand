using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定を作成し、現在のシーンにAdvEngineを追加するデフォルト設定
    public abstract class AdvProjectTemplateSettingsAddSceneDefault 
        : AdvProjectTemplateSettingsAddScene
            , IAdvProjectTemplateSettingsScene
    {
        public SceneAsset Scene => scene;
        [SerializeField] SceneAsset scene;
        [SerializeField] int gameScreenWidth = 1280;
        [SerializeField] int gameScreenHeight = 720;

        [Serializable]
        protected abstract class AdvProjectCreatorNewSceneDefault<T>
            : AdvProjectCreator
                , IAdvProjectCreatorAddScene
                , IAdvProjectCreatorGameScreenSize
                , IAdvProjectCreatorSecurity
                , IAdvProjectCreatorLayerNames
                , IAdvProjectCreatorFont
                , IAdvProjectCreatorUrpGraphicSettings
            where T : AdvProjectTemplateSettingsAddSceneDefault
        {
            protected T Settings => TemplateSettings as T;

            [field: SerializeField] public string SecretKey { get; set; } = "InputOriginalKey";

            [field:SerializeField] public int GameScreenWidth { get; set; }

            [field: SerializeField] public int GameScreenHeight { get; set; }

            [field: SerializeField] public string LayerName { get; set; } = "Utage";

            public string DefaultLayerName => "Default";

            [field: SerializeField] public string LayerNameUI { get; set; } = "UtageUI";

            public string DefaultLayerNameUI => "UI";

            [field: SerializeField] public bool AutoClearUrpVolumes { get; set; } = true;

            protected AdvProjectCreatorNewSceneDefault(T settings)
                :base(settings)
            {
                GameScreenWidth = settings.gameScreenWidth;
                GameScreenHeight = settings.gameScreenHeight;
            }

            public override bool EnableCreate()
            {
                return this.EnableCreateDefault();
            }

            public abstract AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator);

            protected override void OnCreate()
            {
                if (!WrapperUnityVersion.SaveCurrentSceneIfUserWantsTo())
                {
                    return;
                }
                
                //テンプレートからアセットをコピー
                var assetsCreator = new AdvProjectCreatorAssets(this);
                assetsCreator.Create();

                //フォントの変更処理
                AdvProjectCreatorFontChanger fontChanger = CreateFontChanger(assetsCreator);
                fontChanger.ChangeFontAsset();

                //シナリオデータを作成
                var scenarioDataCreator = new AdvProjectCreatorScenarioDataProject(this);
                scenarioDataCreator.CreateScenarioDataProject();

                //シーンを作成
                var sceneCreator = new AdvProjectCreatorAdvSceneAdditive(this, assetsCreator);
                var scene = sceneCreator.Create();
                fontChanger.ChangeFontInScene(scene);
            }
        }
    }
}
