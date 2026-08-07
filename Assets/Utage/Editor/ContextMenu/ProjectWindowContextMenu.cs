// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Utage
{
    //プロジェクトビュー内で、スクリプタブルオブジェクトの作成などをサポート
    public class ProjectWindowContextMenu
    {
        const string root = "Assets/Create/Utage/";

        [MenuItem(root + "CustomProjectSetting")]
        public static void CreateCustomProjectSetting()
        {
            UtageEditorToolKit.CreateNewUniqueAsset<CustomProjectSetting>();
        }

        [MenuItem(root + "LanguageManager")]
        public static void CreateLanguageManager()
        {
            UtageEditorToolKit.CreateNewUniqueAsset<LanguageManager>();
        }

        [MenuItem(root + "TextSettings")]
        public static void CreateTextSettings()
        {
            UtageEditorToolKit.CreateNewUniqueAsset<UguiNovelTextSettings>();
        }

        [MenuItem(root + "EmojiData")]
        public static void CreateEmojiData()
        {
            UtageEditorToolKit.CreateNewUniqueAsset<UguiNovelTextEmojiData>();
        }

        [MenuItem(root + "AudioMixers")]
        public static void CreateAudioMixers()
        {
            //テンプレートのGUIDからロード
            var asset = AssetDataBaseEx.LoadAssetByGuid<AudioMixer>("97efb0d6949540d4db5242c08da13745");
            string path = UtageEditorToolKit.GetSelectedDirectory();
            path = Path.Combine(path, "AudioMixer.mixer");
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            //アセットをコピー
            var newAsset = AssetDataBaseEx.CopyAsset(asset, path);
            EditorUtility.SetDirty(newAsset);
        }

    }
}
