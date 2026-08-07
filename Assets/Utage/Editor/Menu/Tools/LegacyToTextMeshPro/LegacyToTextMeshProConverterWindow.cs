using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utage
{
    //LegacyTextコンポーネントを使っているものを、全てTextMeshProを使ったものに入れ替える
    public class LegacyToTextMeshProConverterWindow
        : EditorWindowNoSave
    {
        [MenuItem(MenuTool.MenuToolRoot + "Tools/LegacyText To TextMeshPro Converter", priority = 510)]
        static void OpenWindow()
        {
            GetWindow(typeof(LegacyToTextMeshProConverterWindow), false, "LegacyText To TextMeshPro Converter");
        }

        public readonly List<LegacyToTextMeshProTypeInfo> AllTextComponentTypes = new ()
        {
            new LegacyToTextMeshProTypeInfo(typeof(AdvUguiBacklog), typeof(AdvUguiBacklogTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(AdvUguiMessageWindow), typeof(AdvUguiMessageWindowTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(AdvUguiSelection), typeof(AdvUguiSelectionTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(SystemUiDebugMenu), typeof(SystemUiDebugMenuTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(SystemUiDialog1Button), typeof(SystemUiDialog1ButtonTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(SystemUiDialog2Button), typeof(SystemUiDialog2ButtonTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(SystemUiDialog3Button), typeof(SystemUiDialog3ButtonTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(SystemUiFramerateChanger), typeof(SystemUiFramerateChangerTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(UtageUguiCgGalleryItem), typeof(UtageUguiCgGalleryItemTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(UtageUguiSceneGalleryItem), typeof(UtageUguiSceneGalleryItemTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(UtageUguiSoundRoomItem), typeof(UtageUguiSoundRoomItemTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(UtageUguiLoadWait), typeof(UtageUguiLoadWaitTMP)),
            new LegacyToTextMeshProTypeInfo(typeof(UtageUguiSaveLoadItem), typeof(UtageUguiSaveLoadItemTMP)),
        };
        
        [SerializeField,UnfoldedSerializable] LegacyToTextMeshProFontAssetSettings fontAssetSettings = new ();

        //プレハブを対象としたコンバート処理
        [Serializable]
        class PrefabConverter
        {
            //コンバート対象のプレハブリスト
            public List<GameObject> prefabAssets = new();

            //プレハブアセット内にLegacyTextがあるかチェックする
            [SerializeField, Button(nameof(CheckPrefabs), nameof(DisableCheckPrefabs), false)]
            string checkPrefabs;

            //プレハブアセットをTextMeshPro対応にコンバートする
            [SerializeField, Space(8) ,Button(nameof(ConvertPrefabs), nameof(DisableConvertPrefabs), false)]
            string convertPrefabs;
        }
        [SerializeField,UnfoldedSerializable] PrefabConverter prefab = new();

        //シーンを対象としたコンバート処理
        [Serializable]
        class SceneConverter
        {
            //コンバート対象のシーンアセット
            public SceneAsset scene;
            
            //シーン内にLegacyTextがあるかチェックする
            [SerializeField, Button(nameof(CheckScene), nameof(DisableCheckScene), false)]
            string checkScene;

            //アクティブなシーンをTextMeshPro対応にコンバートする
            [SerializeField, Space(8), Button(nameof(ConvertScene), nameof(DisableConvertScene), false)]
            string convertScene;
        }
        [SerializeField, UnfoldedSerializable] SceneConverter scene = new();


        //プレハブアセット内にLegacyTextがあるかチェックする処理が可能か
        bool DisableCheckPrefabs()
        {
            if (prefab.prefabAssets.Count == 0) return true;
            return false;
        }

        //プレハブアセット内にLegacyTextがあるかチェックする
        void CheckPrefabs()
        {
            foreach (var prefabAsset in prefab.prefabAssets)
            {
                LegacyToTextMeshProChecker checker = new(fontAssetSettings, AllTextComponentTypes);
                checker.CheckInPrefabAsset(prefabAsset);
            }
        }
        
        //フォントアセットが準備済みかチェック
        bool DisableFontAssetSettings()
        {
            return !fontAssetSettings.EnableConvert();
        }

        //プレハブアセットをTextMeshPro対応にコンバートする処理が可能かチェック
        bool DisableConvertPrefabs()
        {
            if (DisableFontAssetSettings()) return true;
            if (prefab.prefabAssets.Count == 0) return true;
            return false;
        }

        //プレハブアセットをTextMeshPro対応にコンバートする
        void ConvertPrefabs()
        {
            LegacyToTextMeshProConverter converter = new (fontAssetSettings, AllTextComponentTypes);
            foreach (var prefabAsset in prefab.prefabAssets)
            {
                converter.ConvertPrefab(prefabAsset);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        //シーン内にLegacyTextがあるかチェックする処理が可能か
        bool DisableCheckScene()
        {
            if (scene.scene == null) return true;
            return false;
        }

        //シーン内にLegacyTextがあるかチェックする
        void CheckScene()
        {
            if (!WrapperUnityVersion.SaveCurrentSceneIfUserWantsTo())
            {
                return;
            }
            LegacyToTextMeshProChecker checker = new(fontAssetSettings,AllTextComponentTypes);
            checker.CheckInScene(scene.scene);
        }

        //シーンをTextMeshPro対応にコンバートする処理が可能かチェック
        bool DisableConvertScene()
        {
            if (DisableFontAssetSettings()) return true;
            if (scene.scene == null) return true;
            return false;
        }

        //シーンをTextMeshPro対応にコンバートする
        void ConvertScene()
        {
            if (!WrapperUnityVersion.SaveCurrentSceneIfUserWantsTo())
            {
                return;
            }
            LegacyToTextMeshProConverter converter = new(fontAssetSettings, AllTextComponentTypes);
            var targetScene = EditorSceneManagerEx.OpenSceneAsset(scene.scene);
            converter.ConvertScene(targetScene);
            EditorSceneManager.SaveScene(targetScene);
        }
    }
}
