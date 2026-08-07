using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utage
{
    [Serializable]
    public class AdvCustomDataSettings
    {
        List<AdvCustomDataContainerCreator> DataContainerCreators => dataContainerCreators;
        [SerializeField] List<AdvCustomDataContainerCreator> dataContainerCreators = new ();

        public bool IsCustomData(string customDataName)
        {
            return FindDataContainerCreator(customDataName) != null;
        }
        public AdvCustomDataContainerCreator FindDataContainerCreator(string customDataName)
        {
            return DataContainerCreators.Find(x => x.IsTargetDataName(customDataName));
        }
    }
}
