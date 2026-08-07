using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //フォントのフォールバックの設定
    //アセットを直接参照しているのでメモリ消費が激しい。このScriptableObjectを直接シーンから参照させないように
    //Editor内で使うか、言語切り替えのタイミングのときだけResources等から一時的にロードして使うこと
    [CreateAssetMenu(menuName = "Utage/Font/" + nameof(FontFallbackSettingsInEditor))]
    public class FontFallbackSettingsInEditor : ScriptableObject
    {
        //フォント
        public TMP_FontAsset Font => font;
        [SerializeField] TMP_FontAsset font;

        //言語ごとのフォールバック設定
        public List<FallbackSettings> LocalizeFallbacks => localizeFallbacks;
        [SerializeField] List<FallbackSettings> localizeFallbacks = new();

        [Serializable]
        public class FallbackSettings
        {
            public string Language => language;
            [SerializeField] string language;

            public List<TMP_FontAsset> Fallbacks => fallbacks;
            [SerializeField] List<TMP_FontAsset> fallbacks = new();
        }

        public FallbackSettings FindFallbackSettings(string language)
        {
            return LocalizeFallbacks.Find(x => x.Language == language);
        }

        //指定言語のフォールバックに変更する
        public virtual bool TryChangeLanguage(string currentLanguage, string nextLanguage)
        {
            if(currentLanguage == nextLanguage) return false;

            var currentFallbackSettings = FindFallbackSettings(currentLanguage);
            if (currentFallbackSettings == null)
            {
                Debug.LogError($"{currentLanguage} is not found in {nameof(FontFallbackSettingsInEditor)}", this);
                return false;
            }

            var nextFallbackSettings = FindFallbackSettings(nextLanguage);
            if (nextFallbackSettings == null)
            {
                Debug.LogError($"{nextLanguage} is not found in {nameof(FontFallbackSettingsInEditor)}", this);
                return false;
            }
            
            if (currentFallbackSettings == nextFallbackSettings)
            {
                return false;
            }

            //現在のフォールバックをクリア
            Font.fallbackFontAssetTable.RemoveAll(x=> currentFallbackSettings.Fallbacks.Contains(x));
            
            //指定言語のフォールバックに変える
            Font.fallbackFontAssetTable.AddRange(nextFallbackSettings.Fallbacks);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(Font);
#endif
            

            return true;
        }
    }
}
