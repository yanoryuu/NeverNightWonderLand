using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //TipsManagerのシステムセーブ用コンポーネント
    public class TipsManagerSystemSave : MonoBehaviour
        , IAdvSystemSaveDataCustom
    {
        TipsManager TipsManager => this.GetComponentCache(ref tipsManager);
        TipsManager tipsManager;
        public string SaveKey => "TipsManager";
        
        public void OnWrite(BinaryWriter writer)
        {
            TipsManager.OnWrite(writer, true);
        }

        public void OnRead(BinaryReader reader)
        {
            TipsManager.OnRead(reader, true);
        }
    }
}
