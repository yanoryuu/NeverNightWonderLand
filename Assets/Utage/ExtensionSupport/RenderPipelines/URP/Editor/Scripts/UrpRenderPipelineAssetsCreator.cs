// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UTAGE_URP_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using Object = UnityEngine.Object;

namespace Utage.RenderPipeline.Urp
{
    //URPに必要なアセットを作成するクラス
    public class UrpRenderPipelineAssetsCreator
    {
        //クローン元のアセットのGUID
        static string[] AssetGuids { get; } =
        {
            "c21b87b03d0fde34096b48a2cfe70f82",//Volume Profile ImageEffect.asset
            "7bfc92539c9537a4f8d1b20cf243ab1d",//Volume Profile Capture.asset
            "9409cca0241ad5140af934dd862a23f2",//Volume Profile Fade.asset
        };
        public static bool CheckTemplateAssets()
        {
            foreach (var guid in AssetGuids)
            {
                if (!AssetDataBaseEx.IsExistAssetByGuid(guid))
                {
                    return false;
                }
            }
            return true;
        }


        //指定のフォルダ以下に、クローン元のアセットをコピーして新しいアセットを作成
        //クローン元のアセットをキーにして、クローンしたアセットを値にしたDictionaryを作成して返す
        public Dictionary<Object, Object> CreateAssets(string path, string projectName)
        {
            Dictionary < Object, Object > assetDictionary = new Dictionary<Object, Object>();
            FileIoUtil.CreateDirectoryIfNotExists(path);
            foreach (var assetGuid in AssetGuids)
            {
                var asset = AssetDataBaseEx.LoadAssetByGuid<Object>(assetGuid);
                if(asset==null) continue;
                var newAsset =CloneAsset(asset, path, projectName);
                if(newAsset!=null)
                {
                    assetDictionary.Add(asset, newAsset);
                }
            }
            return assetDictionary;
        }

        T CloneAsset<T>(T asset, string path, string projectName)
            where T : Object
        {
            string sourceFileName = Path.GetFileName(AssetDatabase.GetAssetPath(asset));
            var filePath = Path.Combine(path, sourceFileName.Replace("UtageTemplate", projectName));

            //既に同じ名前のアセットがあればそれを返す
            T copied = AssetDatabase.LoadAssetAtPath<T>(filePath);
            if (copied !=null)
            {
                return copied;
            }
            
            //アセットをコピーして作成したものを返す
            return AssetDataBaseEx.CopyAsset(asset, filePath);
        }
    }
}
#endif
