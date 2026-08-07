using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //カスタムデータの管理クラス
    public class AdvCustomDataManager
    {
        public AdvSettingDataManager SettingDataManager { get; private set; }
        //カスタムデータコンテナのリスト
        public List<AdvCustomDataContainer> CustomDataContainers { get; } = new();
        
        //指定の型のカスタムデータコンテナを取得
        public T GetCustomData<T>()
            where T : AdvCustomDataContainer
        {
            foreach (var dataContainer in CustomDataContainers)
            {
                if (dataContainer is T container) return container;
            }
            return null;
        }

        //指定の型のカスタムデータコンテナを取得
        AdvCustomDataContainer GetCustomData(System.Type type)
        {
            foreach (var dataContainer in CustomDataContainers)
            {
                if (type.IsInstanceOfType(dataContainer))
                {
                    return dataContainer;
                }
            }
            return null;
        }

        public void BootInit(AdvSettingDataManager settingDataManager)
        {
            this.SettingDataManager = settingDataManager;
        }

        //チャプターデータを追加・初期化する
        //起動時に呼ばれる
        public void AddAndInitChapterData(AdvCustomDataSettings customDataSettings,  AdvChapterData chapterData)
        {
            foreach (var customData in chapterData.CustomDataList)
            {
                string customDataName = customData.SheetName;
                var creator = customDataSettings.FindDataContainerCreator(customDataName);
                if (creator==null)
                {
                    Debug.LogError($"{customDataName} is not custom data target name", chapterData);
                    continue;
                }

                AdvCustomDataContainer dataContainer = GetCustomData(creator.DataContainerType()); 
                if(dataContainer==null)
                {
                    dataContainer = creator.CreateCustomDataContainer(this);
                    CustomDataContainers.Add(dataContainer);
                }
                customData.InitLink();
                dataContainer.AddAndInitData(customData);
            }
        }

        public void DownloadAll()
        {
            foreach (var file in GetAllFiles())
            {
                AssetFileManager.Download(file);
            }
        }

        public IEnumerable<AssetFile> GetAllFiles()
        {
            foreach (var dataContainer in CustomDataContainers)
            {
                if (dataContainer is IAdvCustomDataContainerAssetFiles files)
                {
                    foreach (var file in files.GetAllFiles())
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}
