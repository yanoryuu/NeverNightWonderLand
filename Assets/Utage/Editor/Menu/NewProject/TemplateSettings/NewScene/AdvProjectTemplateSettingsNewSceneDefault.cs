using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Utage
{
    //テンプレートを元にプロジェクト設定と新規シーンと作成するデフォルト設定
    public abstract class AdvProjectTemplateSettingsNewSceneDefault 
        : AdvProjectTemplateSettingsNewScene
            , IAdvProjectTemplateSettingsScene
    {
        public SceneAsset Scene => scene;
        [SerializeField] SceneAsset scene;
        [SerializeField] int gameScreenWidth = 1280;
        [SerializeField] int gameScreenHeight = 720;

        //エディターwindowに表示する設定項目を持ち、プロジェクト作成を行うクリエーター
        [Serializable]
        protected abstract class AdvProjectCreatorNewSceneDefault<T>
            : AdvProjectCreator
                , IAdvProjectCreatorGameScreenSize
                , IAdvProjectCreatorSecurity
                , IAdvProjectCreatorFont
                , IAdvProjectCreatorUrpGraphicSettings
            where T : AdvProjectTemplateSettingsNewSceneDefault
        {
            protected T Settings => TemplateSettings as T;
            [field: SerializeField] public string SecretKey { get; set; } = "InputOriginalKey";
            [field: SerializeField] public int GameScreenWidth { get; set; }
            [field: SerializeField] public int GameScreenHeight { get; set; }
            [field: SerializeField] public bool AutoClearUrpVolumes { get; set; } = true;

            protected AdvProjectCreatorNewSceneDefault(T settings)
                :base(settings)
            {
                GameScreenWidth = settings.gameScreenWidth;
                GameScreenHeight = settings.gameScreenHeight;
            }

            public abstract AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator);

            public override bool EnableCreate()
            {
                return this.EnableCreateDefault();
            }

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
                var sceneCreator = new AdvProjectCreatorAdvSceneNew(this, assetsCreator);
                var scene = sceneCreator.Create();
                fontChanger.ChangeFontInScene(scene);


                Selection.activeObject = sceneCreator.SceneAsset;
            }
        }
    }
}
