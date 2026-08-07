using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //フォントのフォールバックの設定
    //アセットを直接参照しているのでメモリ消費が激しい。このScriptableObjectを直接シーンから参照させないように
    //Editor内で使うか、言語切り替えのタイミングのときだけResources等から一時的にロードして使うこと
    [CreateAssetMenu(menuName = "Utage/Font/" + nameof(FontFallbackSettingsRuntime))]
    public class FontFallbackSettingsRuntime : LanguageChangeEventFactory
    {
        //フォントの基本設定
        public FontSettings FontSettings => fontSettings;
        [SerializeField] FontSettings fontSettings;

        //対象となるフォント
        public TMP_FontAsset Font => font;
        [SerializeField] TMP_FontAsset font;

        //フォールバックフォントの置き場所のパス
        string FallbackFontsPath => fallbackFontsPath;
        [SerializeField] string fallbackFontsPath = "Fonts & Materials";

        //言語ごとのフォールバック設定
        public List<FallbackSettings> LocalizeFallbacks => localizeFallbacks;
        [SerializeField] List<FallbackSettings> localizeFallbacks = new();

        [Serializable]
        public class FallbackSettings
        {
            public string Language => language;
            [SerializeField] string language;

            public List<string> FallbackNames => fallbackNames;
            [SerializeField] List<string> fallbackNames = new();

            public void ReplaceFontNames(string oldValue, string newValue)
            {
                for (var i = 0; i < fallbackNames.Count; i++)
                {
                    fallbackNames[i] = fallbackNames[i].Replace(oldValue, newValue);
                }
            }
        }


        public void ReplaceFontNames(string oldValue, string newValue)
        {
            LocalizeFallbacks.ForEach(x => x.ReplaceFontNames(oldValue, newValue));
        }

        public FallbackSettings FindFallbackSettings(string language)
        {
            return LocalizeFallbacks.Find(x => x.Language == language);
        }

        //イベント用のインスタンスを作成
        public override ILanguageChangeEvent CreateEvent(LanguageManagerBase languageManager)
        {
            return new RuntimeFontAssetChanger(this, languageManager);
        }

        public bool TryLoadFallbackList(FallbackSettings fallbackSettings, out List<TMP_FontAsset> fallBacks)
        {
            fallBacks = new List<TMP_FontAsset>();
            foreach (var fallbackName in fallbackSettings.FallbackNames)
            {
                string path = Path.Combine(FallbackFontsPath, fallbackName);
                var fallbackFont = Resources.Load<TMP_FontAsset>(path);
                if (fallbackFont == null)
                {
                    //ロードエラー
                    Debug.LogError($"{path} is not found", this);
                    return false;
                }

                fallBacks.Add(fallbackFont);
            }

            return true;
        }

        //ランタイムでフォント変更処理を行うためのクラス
        class RuntimeFontAssetChanger : ILanguageChangeEvent
        {
            FontFallbackSettingsRuntime Settings { get; } 
            LanguageManagerBase LanguageManager { get; }
            string CurrentFontLanguage { get; set; }
            public RuntimeFontAssetChanger(FontFallbackSettingsRuntime settings, LanguageManagerBase languageManager)
            {
                Settings = settings;
                LanguageManager = languageManager;
                CurrentFontLanguage = Settings.FontSettings.DefaultFontLanguage;
            }

            //言語変更時の処理
            public void OnChangeLanguage()
            {
                ChangeLanguage(LanguageManager.CurrentLanguage);
            }

            //終了処理
            public void OnFinalize()
            {
                ChangeLanguage(Settings.FontSettings.DefaultFontLanguage);
            }

            //言語を指定の物に変える
            void ChangeLanguage(string language)
            {
                //フォント言語名一覧にない場合
                if (!Settings.FontSettings.TyGetFontLanguageName(language, out string nextFontLanguage))
                {
                    //対応できないので何もしない
                    return;
                }

                //フォント言語が変わっていなければ何もしない
                if (CurrentFontLanguage == nextFontLanguage) return;


                //パスが設定されてないのでエラー
                if (string.IsNullOrEmpty(Settings.FallbackFontsPath))
                {
                    Debug.LogError("FallbackSettingsPath is empty", Settings);
                    return;
                }


                //フォントアセットの言語設定をロードして、言語変更
                if (TryChangeLanguage(CurrentFontLanguage, nextFontLanguage))
                {
                    CurrentFontLanguage = nextFontLanguage;
                }
            }

            //指定言語のフォールバックに変更する
            bool TryChangeLanguage(string currentLanguage, string nextLanguage)
            {
                if (currentLanguage == nextLanguage) return false;

                var currentFallbackSettings = Settings.FindFallbackSettings(currentLanguage);
                if (currentFallbackSettings == null)
                {
                    Debug.LogError($"{currentLanguage} is not found in {nameof(FontFallbackSettingsInEditor)}", Settings);
                    return false;
                }

                var nextFallbackSettings = Settings.FindFallbackSettings(nextLanguage);
                if (nextFallbackSettings == null)
                {
                    Debug.LogError($"{nextLanguage} is not found in {nameof(FontFallbackSettingsInEditor)}", Settings);
                    return false;
                }

                if (currentFallbackSettings == nextFallbackSettings)
                {
                    return false;
                }
                
                if(!Settings.TryLoadFallbackList(nextFallbackSettings, out List<TMP_FontAsset> nextFallBacks))
                {
                    //予定のフォールバックフォントをロードできないので何もしない
                    return false;
                }

                //現在のフォールバックをクリア
                Settings.Font.fallbackFontAssetTable.RemoveAll(x => currentFallbackSettings.FallbackNames.Contains(x.name));

                //指定言語のフォールバックに変える
                Settings.Font.fallbackFontAssetTable.AddRange(nextFallBacks);
                return true;
            }
        }
    }
}
