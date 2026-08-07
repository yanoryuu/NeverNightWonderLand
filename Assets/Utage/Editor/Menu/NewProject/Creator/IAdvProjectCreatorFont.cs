using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    public interface IAdvProjectCreatorFont : IAdvProjectCreator
    {
        AdvProjectCreatorFontChanger CreateFontChanger(AdvProjectCreatorAssets assetsCreator);
    }
    //フォント(Legacy)の設定
    public interface IAdvProjectCreatorFontLegacy : IAdvProjectCreatorFont
    {
        public Font DefaultFont { get; }
        public Font Font { get; }
    }

    //フォント(TextMeshPro)の設定
    public interface IAdvProjectCreatorFontTMP : IAdvProjectCreatorFont
    {
        public AdvProjectTemplateFontSettingsTMP FontSettings { get; }
        public string FontLanguage { get; }
    }

    public static class AdvProjectCreatorFontExtensions
    {
        //フォント設定が有効かチェック
        public static bool EnableFont(this IAdvProjectCreatorFontLegacy font)
        {
            return font.Font != null;
        }

        //フォント設定が有効かチェック
        public static bool EnableFont(this IAdvProjectCreatorFontTMP font)
        {
            return !string.IsNullOrEmpty(font.FontLanguage);
        }
    }
}
