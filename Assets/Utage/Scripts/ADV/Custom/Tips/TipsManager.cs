using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //Tipsの管理コンポーネント
    public class TipsManager : MonoBehaviour
        , IAdvEngineCustomEventBootInit
    {
        AdvEngine Engine => this.GetComponentCacheInParent(ref engine);
        [SerializeField] AdvEngine engine;
        
        //TIPSを初めて解放したときのイベント
        public UnityEvent<TipsInfo> OnOpenTips => onOpenTips;
        [SerializeField] UnityEvent<TipsInfo> onOpenTips = new ();

        //IDをキーにしたTipsの情報
        public Dictionary<string, TipsInfo> TipsMap { get; } = new ();


        public void OnBootInit()
        {
            TipsMap.Clear();
            var dataContainer = Engine.DataManager.SettingDataManager.CustomDataManager.GetCustomData<TipsDataContainer>();
            if (dataContainer == null)
            {
                //Tips用のデータコンテナが見つからないのでエラー
                Debug.LogError($"{nameof(TipsDataContainer)} is not found" );
                return;
            }
            foreach (var keyValue in dataContainer.TipsDataList)
            {
                var info = keyValue.Value.CreateInfo();
                TipsMap.Add(info.Id,info);
            }
        }

        
        public TipsInfo GetTipsInfo(string id)
        {
            if (TipsMap.TryGetValue(id, out var tipsInfo)) return tipsInfo;
            return null;
        }
        //TIPSを解放
        public TipsInfo OpenTips(string id)
        {
            TipsInfo info = GetTipsInfo(id);
            if (info == null) return null;
            
            //解放済み
            if (info.IsOpened) return info;
            
            //初めて解放
            info.Open();
            OnOpenTips.Invoke(info);
            return info;
        }
        
        //デバッグ用。全てのTIPSを解放        
        public void DebugOpenAllTips()
        {
            foreach (var keyValue in TipsMap)
            {
                var tipsInfo = keyValue.Value;
                OpenTips(tipsInfo.Id);
            }
        }        
        
        
        const int Version = 0;

        public void OnClearSave(bool isSystemSaveData)
        {
            foreach (var keyValue in TipsMap)
            {
                var tipsInfo = keyValue.Value;
                if (tipsInfo.SystemSave == isSystemSaveData)
                {
                    tipsInfo.Clear();
                }
            }
        }

        public void OnWrite(BinaryWriter writer, bool isSystemSaveData)
        {
            writer.Write(Version);
            int count = 0;
            foreach (var keyValue in TipsMap)
            {
                var tipsInfo = keyValue.Value; 
                if (tipsInfo.SystemSave == isSystemSaveData)
                {
                    count++;
                }
            }
            writer.Write(count);
            foreach (var keyValue in TipsMap)
            {
                var tipsInfo = keyValue.Value;
                if (tipsInfo.SystemSave == isSystemSaveData)
                {
                    writer.Write(tipsInfo.Id);
                    writer.WriteBuffer(tipsInfo.WriteSave);
                }
            }
        }

        public void OnRead(BinaryReader reader, bool isSystemSaveData)
        {
            //バージョンチェック
            int version = reader.ReadInt32();
            if (version == Version)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    string key = reader.ReadString();
                    byte[] buffer = reader.ReadBytes(reader.ReadInt32());
                    var tipsInfo = GetTipsInfo(key);
                    if (tipsInfo == null)
                    {
                        Debug.LogError($"{key} is not found in {nameof(TipsManager)}", this);
                    }
                    else
                    {
                        if (tipsInfo.SystemSave != isSystemSaveData)
                        {
                            Debug.LogWarning($"tipsInfo[{key}] is not equal IsSystemSaveData ({isSystemSaveData}).");
                        }
                        BinaryUtil.BinaryRead(buffer, tipsInfo.ReadSave);
                    }
                }
            }
            else
            {
                Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
            }
        }
    }
}
