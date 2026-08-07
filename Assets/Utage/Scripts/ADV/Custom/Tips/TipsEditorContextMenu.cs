#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //TIPS拡張のUnityエディター内のコンテキストメニュー
    public class TipsEditorContextMenu
    {
        //HierarchyWindow内のコンテキストメニューのルートパス
        const string HierarchyWindowRoot = "GameObject/Utage/";

        //シーン内にTipsMangerを作成
        [MenuItem(HierarchyWindowRoot + "Tips/TipsManger")]
        public static void CreateTipsManger()
        {
            //シーン内にAdvEngineがあるかチェック
            AdvEngine engine = SceneManagerEx.GetComponentInActiveScene<AdvEngine>(true);
            if (engine == null)
            {
                Debug.LogError("Not find AdvEngine");
                return;
            }
            //AdvEngine以下にテンプレートのGUIDからプレハブをロードしてコピー
            Selection.activeGameObject = AssetDataBaseEx.InstantiateFromPrefabGuid("79888b6029d29134e905244b89bbef2d", 
                "TipsManger", engine.transform);
        }

        //シーン内にTips詳細画面のUIを作成
        [MenuItem(HierarchyWindowRoot + "Tips/TipsDetail")]
        public static void CreateTipsDetail()
        {
            //選択オブジェクト以下にテンプレートのGUIDからプレハブをロードしてコピー
            var go = AssetDataBaseEx.InstantiateFromPrefabGuid("eb017c5f43bea404e8617087a337a4ec",
                "TipsDetail", Selection.activeTransform);
            go.SetActive(false);
            Selection.activeGameObject = go;
        }

        //シーン内にTips一覧画面のUIを作成
        [MenuItem(HierarchyWindowRoot + "Tips/TipsList")]
        public static void CreateTipsList()
        {
            //選択オブジェクト以下にテンプレートのGUIDからプレハブをロードしてコピー
            var go = AssetDataBaseEx.InstantiateFromPrefabGuid("8fcbc2f30e2abec40b075ba2744483a1",
                "TipsList", Selection.activeTransform);
            go.SetActive(false);
            Selection.activeGameObject = go;
        }

        //ProjectWindow内のコンテキストメニューのルートパス
        const string ProjectWindowRoot = "Assets/Create/Utage/Tips";

        //Tips一覧画面のボタンのプレハブを作成
        [MenuItem(ProjectWindowRoot + "TipsListButtonPrefab")]
        public static void CreateTipsListButton()
        {
            //テンプレートのGUIDからプレハブをロードしてコピー
            AssetDataBaseEx.CopyTemplateAssetInSelectedDirectory<GameObject>("98b0c0f387bc1ff42912eaaca7e8173f","TipsListButton.prefab");
        }

    }
}
#endif
