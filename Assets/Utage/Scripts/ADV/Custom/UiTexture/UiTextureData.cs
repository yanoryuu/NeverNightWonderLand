using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //Ui用の追加テクスチャのデータ
    //セーブデータのサムネイル拡張などに使用
    public class UiTextureData
    {
        public StringGridRow RowData { get; }
        public string Label { get; }
        public string FilePath { get; }

        protected UiTextureDataContainer DataContainer { get; set; }
        public UiTextureData(UiTextureDataContainer dataContainer, StringGridRow rowData)
        {
            DataContainer = dataContainer;
            RowData = rowData;
            Label = RowData.ParseCell<string>("Label");
            var path = RowData.ParseCell<string>("FilePath");
            FilePath = string.IsNullOrEmpty(path) ? string.Empty : FilePathUtil.Combine(DataContainer.CustomDataManager.SettingDataManager.BootSetting.ResourceDir, path);
        }

        public IEnumerable<AssetFile> GetAllFiles()
        {
            yield return AssetFileManager.GetFileCreateIfMissing(FilePath);
        }
    }
}
