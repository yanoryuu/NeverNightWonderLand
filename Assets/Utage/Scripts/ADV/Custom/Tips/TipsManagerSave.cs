using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //TipsManagerのセーブ用コンポーネント
    public class TipsManagerSave : MonoBehaviour
        , IAdvSaveData
    {
        TipsManager TipsManager => this.GetComponentCache(ref tipsManager);
        TipsManager tipsManager;
        public string SaveKey => "TipsManager";
        
        public void OnWrite(BinaryWriter writer)
        {
            TipsManager.OnWrite(writer, false);
        }

        public void OnRead(BinaryReader reader)
        {
            TipsManager.OnRead(reader, false);
        }

        public void OnClear()
        {
            //個別のセーブデータのリセット
            TipsManager.OnClearSave(false);
        }
    }
}
