using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //Tipsのデータコンテナのクリエイター
    //CustomProjectSettingに設定することで、Tipsデータを扱えるようになる
    [CreateAssetMenu(menuName = "Utage/CustomData/" + nameof(TipsDataContainerCreator))]
    public class TipsDataContainerCreator : AdvCustomDataContainerCreator<TipsDataContainer>
    {
        //TIPSデータとして扱うシート名
        protected List<string> TargetNames => targetNames;
        [SerializeField] protected List<string> targetNames = new() { "Tips" };
        
        public override bool IsTargetDataName(string customDataName)
        {
            return TargetNames.Contains(customDataName);
        }

        public override AdvCustomDataContainer CreateCustomDataContainer(AdvCustomDataManager customDataManager)
        {
            return new TipsDataContainer(customDataManager);
        }
    }
}
