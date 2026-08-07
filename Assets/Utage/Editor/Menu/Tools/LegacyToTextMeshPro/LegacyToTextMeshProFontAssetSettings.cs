using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
    //LegacyTextコンポーネントを使っているものを、全てTextMeshProを使ったものに入れ替える
    [Serializable]
    public class LegacyToTextMeshProFontAssetSettings
    {
        [Serializable]
        public class FontAssetSetting
        {
            public Font LegacyFontAsset => legacyFontAsset;
            [SerializeField] Font legacyFontAsset;

            public TMP_FontAsset FontAsset => fontAsset;
            [SerializeField] TMP_FontAsset fontAsset;

            public Material OutlineMaterial => outlineMaterial;
            [SerializeField] Material outlineMaterial;

            public TextMeshProRuby RubyPrefab => rubyPrefab;
            [SerializeField] TextMeshProRuby rubyPrefab;

            //コンバート可能な設定がされているか
            public bool EnableConvert()
            {
                if (FontAsset == null) return false;
                if (OutlineMaterial == null) return false;
                return true;
            }
        }

        public List<FontAssetSetting> FontAssetSettings => fontAssetSettings;
        [SerializeField] List<FontAssetSetting> fontAssetSettings = new List<FontAssetSetting>();

        //コンバート可能な設定がされているか
        public bool EnableConvert()
        {
            if (FontAssetSettings.Count == 0) return false;
            return FontAssetSettings.All(setting => setting.EnableConvert());
        }

        //コンバート可能な設定がされているか
        public FontAssetSetting FindSetting(Font font)
        {
            var setting = FontAssetSettings.Find(x => x.LegacyFontAsset == font);
            if(setting!=null) return setting;
            setting = FontAssetSettings.Find(x => x.FontAsset == null);
            if (setting != null) return setting;
            return FontAssetSettings[0];
        }

    }
}
