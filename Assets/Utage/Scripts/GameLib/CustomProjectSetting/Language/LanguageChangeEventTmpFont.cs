#if false
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Utage
{
    //言語変更時にTextMeshProフォントを変更するためのScriptableObject
    [CreateAssetMenu(menuName = "Utage/Language/" + nameof(LanguageChangeEventTmpFont))]
    public class LanguageChangeEventTmpFont : LanguageChangeEvent
    {
        //対象のフォント
        TMP_FontAsset TargetFont => targetFont;
        [SerializeField] TMP_FontAsset targetFont;

        //言語によってフォールバックを設定する際の情報クラス
        [Serializable]
        class FallbackInfo
        {
            //対象言語名
            public string Language => language;
            [SerializeField] string language;

            //フォールバックとして追加するフォントのResources以下のパス
            public List<string> FallbackFontPaths => fallbackFontPaths;
            [SerializeField] List<string> fallbackFontPaths;
        }

        //デフォルトのフォールバック情報
        public List<string> DefaultFallbackFontPaths => defaultFallbackFontPaths;
        [SerializeField] List<string> defaultFallbackFontPaths;

        //言語ごとのフォールバック情報
        List<FallbackInfo> Fallbacks => fallbacks;
        [SerializeField] List<FallbackInfo> fallbacks = new ();
        
        //初期化済みかのフラグ
        bool IsInitialized { get; set; }
        
        //言語変更時の処理
        public override void OnChangeLanguage(LanguageManagerBase languageManager)
        {
            if(TargetFont ==null) return;

            if (!Application.isPlaying)
            {
                Debug.Log("OnChangeLanguage called in not Playing.",this);
                return;
            }
            Debug.Log("OnChangeLanguage called in Playing.", this);
            
            
            string language = languageManager.CurrentLanguage;
            var fallbackInfo = GetFallBackInfo(language);
            if (fallbackInfo == null)
            {
                ResetToDefault();
                return;
            }

            LoadFallback(fallbackInfo.FallbackFontPaths);
        }
        
        //指定の情報のフォールバックをロード
        void LoadFallback(List<string> fallbackFontPaths)
        {
            //いったんフォールバックをすべて削除
            TargetFont.fallbackFontAssetTable.Clear();
            foreach (var path in fallbackFontPaths)
            {
                var fallbackFont = Resources.Load<TMP_FontAsset>(path);
                if (fallbackFont == null)
                {
                    //ロードエラー
                    Debug.LogError($"Not found fallback font {path}", this);
                    continue;
                }
                TargetFont.fallbackFontAssetTable.Add(fallbackFont);
            }
        }
        
        //言語名に一致するフォールバックを取得
        FallbackInfo GetFallBackInfo(string language)
        {
            var result = Fallbacks.Find(x => x.Language == language) ?? Fallbacks.Find(x => string.IsNullOrEmpty(x.Language));
            return result;
        }
        
        //終了処理
        public override void OnFinalize(LanguageManagerBase languageManager)
        {
            //デフォルト状態にリセット
            ResetToDefault();
        }

        //デフォルト状態にリセット
        void ResetToDefault()
        {
            LoadFallback(DefaultFallbackFontPaths);
        }
    }
}
#endif
