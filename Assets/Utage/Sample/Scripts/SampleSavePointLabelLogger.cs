using System;
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //シナリオラベルの変更時に、セーブポイントが設定されているシナリオラベルだったらそれを記録しておくサンプル
    public class SampleSavePointLabelLogger : MonoBehaviour
    {
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing( ref engine );
        [SerializeField] AdvEngine engine;

        AdvScenarioThread MainThread => Engine.ScenarioPlayer.MainThread;

        public string SavedScenarioLabelLog
        {
            get;
            private set;
        }
        
        void Awake()
        {
            Engine.OnClear.AddListener(OnClear);
            MainThread.OnStartScenarioLabel.AddListener(OnStartScenarioLabel);
        }

        void OnStartScenarioLabel()
        {
            if (MainThread.CurrentLabelData.IsSavePoint)
            {
                SavedScenarioLabelLog = MainThread.CurrentLabelData.ScenarioLabel;
                Debug.Log("OnStartScenarioLabel" + SavedScenarioLabelLog);
            }
        }

        void OnClear(AdvEngine _)
        {
            //ログを消す
            SavedScenarioLabelLog = "";
        }
    }
}
