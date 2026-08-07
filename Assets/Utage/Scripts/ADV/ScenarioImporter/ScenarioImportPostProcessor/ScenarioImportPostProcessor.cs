using UnityEngine;

namespace Utage
{
    //インポートの後処理
    public abstract class ScenarioImportPostProcessor
    {
        public abstract void OnImportPostProcess();
    }

    public abstract class ScenarioImportPostProcessor<T>
        where T : ScenarioImportPostProcessorSettings
    {
        protected T Settings { get; }
        protected ScenarioImportPostProcessor(T settings)
        {
            Settings = settings;
        }
    }
}
