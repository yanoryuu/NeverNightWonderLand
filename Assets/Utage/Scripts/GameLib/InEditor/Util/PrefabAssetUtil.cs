// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace Utage
{
    //プレハブアセット用の便利static関数など
    public static class PrefabAssetUtil
    {
        //プレハブかどうか不明なアセットから、プレハブのルートとなるGameObjectを取得する
        public static bool TryGetPrefabAssetRoot(UnityEngine.Object asset, out GameObject prefabAssetRoot)
        {
            prefabAssetRoot = null;
            if (!PrefabUtility.IsPartOfPrefabAsset(asset)) return false;
            if (asset is GameObject gameObject)
            {
                prefabAssetRoot = gameObject.transform.root.gameObject;
                return true;
            }
            else if (asset is Component component)
            {
                prefabAssetRoot = component.transform.root.gameObject;
                return true;
            }
            else
            {
                Debug.LogWarning($"{asset.name} {asset.GetType()} は不明な型のプレハブの一部です", asset);
                return false;
            }
        }
    }
}
#endif
