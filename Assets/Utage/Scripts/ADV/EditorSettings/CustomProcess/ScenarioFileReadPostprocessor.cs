#if UNITY_EDITOR

using UnityEngine;

namespace Utage
{
    //シナリオファイル読み込み後の処理を行うため基底クラス
    public abstract class ScenarioFileReadPostprocessor : ScriptableObject
    {
        //シナリオファイル読み込み後の処理
        public abstract void OnScenarioFileReadPostprocess(AdvScenarioImporterInEditor importerInEditor, string path, StringGridDictionary gridDictionary);
    }
}

#endif
