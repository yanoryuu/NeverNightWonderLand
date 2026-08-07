using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //Tipsのデータ
    public class TipsData
    {
        public StringGridRow RowData { get; }
        public string Id { get; }
        public bool SystemSave { get; }
        public string ImageFilePath { get; }
        public bool DefaultOpened { get; } //デフォルトで開放済みか
        public bool DefaultRead { get; } //デフォルトで既読済みか

        protected TipsDataContainer DataContainer { get; set; }
        public TipsData(TipsDataContainer dataContainer, StringGridRow rowData)
        {
            DataContainer = dataContainer;
            RowData = rowData;
            Id = RowData.ParseCell<string>("ID");
            SystemSave = RowData.ParseCellOptional<bool>("SystemSave",true);
            DefaultRead = RowData.ParseCellOptional<bool>("Read", false);
            DefaultOpened = RowData.ParseCellOptional<bool>("Opened", false);
            
            var path = RowData.ParseCellOptional<string>("ImageFilePath", string.Empty);
            ImageFilePath = string.IsNullOrEmpty(path) ? string.Empty : FilePathUtil.Combine(DataContainer.CustomDataManager.SettingDataManager.BootSetting.ResourceDir, path);
            //インポート時のエラーチェックのために
            LocalizedTitle();
            LocalizedText();
        }

        //現在の設定言語にローカライズされたタイトルを取得
        public virtual string LocalizedTitle()
        {
            return AdvParser.ParseCellSuffixedLanguageNameToLocalizedText(this.RowData, "Title");
        }

        //現在の設定言語にローカライズされたテキストを取得
        public virtual string LocalizedText()
        {
            return AdvParser.ParseCellSuffixedLanguageNameToLocalizedText(this.RowData, "Text");
        }

        public IEnumerable<AssetFile> GetAllFiles()
        {
            if (!string.IsNullOrEmpty(ImageFilePath))
            {
                yield return AssetFileManager.GetFileCreateIfMissing(ImageFilePath);
            }
        }

        public virtual TipsInfo CreateInfo()
        {
            return new TipsInfo(this);
        }
    }
}
