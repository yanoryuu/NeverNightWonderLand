using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //Ui用の追加テクスチャのデータコンテナ
    public class UiTextureDataContainer : AdvCustomDataContainer
        , IAdvCustomDataContainerAssetFiles
    {
        public Dictionary<string, UiTextureData> DataDictionary { get; } = new ();

        public UiTextureDataContainer(AdvCustomDataManager customDataManager)
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

                var data = CreateData(row);
                if (!DataDictionary.TryAdd(data.Label, data))
                {
                    Debug.LogError($"{data.Label} is already contains", customData.SourceAssetInEditor);
                }
            }
        }

        public virtual UiTextureData CreateData(StringGridRow row)
        {
            return new UiTextureData(this,row);
        }

        public IEnumerable<AssetFile> GetAllFiles()
        {
            foreach (var keyValuePair in DataDictionary)
            {
                foreach (var assetFile in keyValuePair.Value.GetAllFiles())
                {
                    yield return assetFile;
                }
            }
        }
    }
}
