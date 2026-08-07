using UnityEngine;

namespace Utage
{
    public abstract class ScenarioFileReaderSettings : ScriptableObject
    {
        public abstract IAdvScenarioFileReader CreateReader();
    }
}
