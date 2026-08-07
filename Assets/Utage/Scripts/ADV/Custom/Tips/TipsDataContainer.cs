using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //Tipsのデータコンテナ
    public class TipsDataContainer : AdvCustomDataContainer
        , IAdvCustomDataContainerAssetFiles
    {
        public Dictionary<string,TipsData> TipsDataList { get; } = new ();

        public TipsDataContainer(AdvCustomDataManager customDataManager)
            : base(customDataManager)
        {
        }
        
        public override void AddAndInitData(StringGrid customData)
        {
            base.AddAndInitData(customData);
            foreach (var row in customData.Rows)
            {
                if (row.RowIndex < customData.DataTopRow) continue; //データの行じゃない
                if (row.IsEmptyOrCommantOut) continue; //データがない

                var tipsData = CreateTipsData(row);
                if (TipsDataList.ContainsKey(tipsData.Id))
                {
                    Debug.LogError($"{tipsData.Id} is already contains", customData.SourceAssetInEditor);
                }
                else
                {
                    TipsDataList.Add(tipsData.Id,tipsData);
                }
            }
        }

        public virtual TipsData CreateTipsData(StringGridRow row)
        {
            return new TipsData(this,row);
        }

        public IEnumerable<AssetFile> GetAllFiles()
        {
            foreach (var keyValuePair in TipsDataList)
            {
                foreach (var assetFile in keyValuePair.Value.GetAllFiles())
                {
                    yield return assetFile;
                }
            }
        }
    }
}
