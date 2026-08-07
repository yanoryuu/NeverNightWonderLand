using UnityEngine;
using Utage;

namespace Utage
{
    //ランタイムでシナリオファイルを読み込んで実行するサンプル
    public class SampleRuntimeScenarioFile : MonoBehaviour
    {
        public UtageUguiTitle tile;
        public UtageUguiMainGame mainGame;
        public AdvEngine engine;
        public RuntimeScenarioFileReader scenarioFileReader;
        public RuntimeScenarioImporter scenarioImporter;
        public string startScenarioLabel = "test";
        public string directoryPath = @"DynamicScenario";


        public void OnClick()
        {
            //指定のディレクトリ以下のシナリオファイルをロードして、StringGridDictionary scenarioとして取得
            if (!scenarioFileReader.TryReadScenario(directoryPath, out StringGridDictionary scenario))
            {
                Debug.LogError("Failed Read Scenario " + directoryPath, this);
                return;
            }

            //StringGridDictionary scenarioをインポートして、シナリオをロード
            if (!scenarioImporter.TryImportAndLoadScenario(scenario))
            {
                Debug.LogError("Failed Import Scenario " + directoryPath, this);
                return;
            }

            //指定のラベルでゲームを開始する
            engine.StartScenarioLabel = startScenarioLabel;
            tile.Close();
            mainGame.OpenStartGame();
        }
    }
}
