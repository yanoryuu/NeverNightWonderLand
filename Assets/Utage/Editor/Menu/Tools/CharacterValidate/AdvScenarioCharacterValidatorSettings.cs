#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Utage
{
    //ADV用の、想定外の文字が含まれていないか検証するためのユーザーごとの設定データ
    [System.Serializable]
    public class AdvScenarioCharacterValidatorSettings
    {
        public enum LanguageType
        {
            Single,
            Multiple,
        }
        public LanguageType Language => languageType;
        [SerializeField] LanguageType languageType = LanguageType.Single;

        //対象となるフォント
        public TMP_FontAsset Font => font;
        [SerializeField, Hide(nameof(IsNotSingle),true)] TMP_FontAsset font;

        public LanguageManagerBase LanguageManager => languageManager;
        [SerializeField,Hide(nameof(IsNotMultiple),true)] LanguageManagerBase languageManager = null;

        public FontFallbackSettingsRuntime FontFallbackSettings => fontFallbackSettings;
        [SerializeField, Hide(nameof(IsNotMultiple),true)] FontFallbackSettingsRuntime fontFallbackSettings = null;

        public List<TMP_SpriteAsset> SpriteAssets => spriteAssets;
        [SerializeField] List<TMP_SpriteAsset> spriteAssets = new ();

        //ログの出力先
        public string LogOutputPath => logOutputPath;
        [SerializeField, PathDialog(PathDialogAttribute.DialogType.Directory)]
        string logOutputPath = "";
        
        bool IsNotSingle => Language != LanguageType.Single;
        bool IsNotMultiple => Language != LanguageType.Multiple;

        public bool DisableValidate()
        {
            switch (Language)
            {
                case LanguageType.Single:
                    return Font == null;
                case LanguageType.Multiple:
                    return FontFallbackSettings == null || LanguageManager == null;
                default:
                    Debug.LogError($"LanguageType {Language} is not supported.");
                    return true;
            }
        }

        public AdvScenarioCharacterValidator CreateValidator()
        {
            return new AdvScenarioCharacterValidator(this);
        }
    }
}
#endif
