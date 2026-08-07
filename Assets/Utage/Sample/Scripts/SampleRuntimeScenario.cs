using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utage;

namespace Utage
{

    //ランタイムでプログラム上で作成したシナリオを読み込むサンプル
    public class SampleRuntimeScenario : MonoBehaviour
    {
        public AdvEngine engine;
        public RuntimeScenarioImporter scenarioImporter;
        public UtageUguiTitle tile;
        public UtageUguiMainGame mainGame;
        const string ScenarioName = "SampleRuntimeScenario";

        public void OnClick()
        {
            SampleMakeScenarioAndLoad();

            //指定のラベルでゲームを開始する
            engine.StartScenarioLabel = ScenarioName;
            tile.Close();
            mainGame.OpenStartGame();
        }
        
        public void SampleMakeScenarioAndLoad()
        {
            //シナリオ名。エクセルのシート名に相当
            const string scenarioName = "SampleRuntimeScenario";
            StringGrid scenario = MakeScenario(scenarioName);


            //StringGridDictionary scenarioをインポートして、シナリオをロード
            if (!scenarioImporter.TryImportAndLoadScenario(scenario))
            {
                Debug.LogError("Failed Import Scenario " + scenarioName, this);
                return;
            }
        }

        //「宴」形式のシナリオを作成
        StringGrid MakeScenario(string scenarioName)
        {

            StringGrid grid = new StringGrid(scenarioName, scenarioName, CsvType.Csv);
            grid.AddRow(new List<string>
            {
                //ヘッダー部分
                "Command", "Arg1", "Arg2", "Arg3", "Arg4", "Arg5", "Arg6", "WaitType", "Text", "PageCtrl", "Voice",
                "WindowType"
            });
            grid.ParseHeader();

            //キャラと台詞表示の例
            grid.AddRow(new List<string> { "", "こはく", "喜", "", "", "", "", "", "こんにちは！私Unityちゃん!", "", "", "" });

            //コマンド記述の例
            grid.AddRow(new List<string>
            {
                AdvCommandParser.IdPauseScenario, "", "", "", "", "", "", "", "", "", "", ""
            });
            return grid;
        }
    }
}


