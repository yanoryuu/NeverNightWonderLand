using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //Text系のコンポーネント(TextMeshProとレガシーText）を兼用するためのラッパー
    public static class TextComponentWrapper
    {
        public static void SetText(Text textComponent, TextMeshProUGUI textMeshPro, string text)
        {
            if (textComponent != null) textComponent.text = text;
            if (textMeshPro != null) textMeshPro.text = text;
        }

        public static void AppendText(Text textComponent, TextMeshProUGUI textMeshPro, string text)
        {
            if (textComponent != null)
            {
                textComponent.text += text;
            }

            if (textMeshPro != null)
            {
                textMeshPro.text += text;
            }
        }

        public static string GetText(Text textComponent, TextMeshProUGUI textMeshPro)
        {
            if (textComponent != null) return textComponent.text;
            if (textMeshPro != null) return textMeshPro.text;
            return "";
        }

        public static void SetText(Component textComponent, string text)
        {
            if (textComponent == null) return;

            switch (textComponent)
            {
                case TextMeshProUGUI textMeshProUGUI:
                    textMeshProUGUI.text = text;
                    break;
                case Text legacyText:
                    legacyText.text = text;
                    break;
                default:
                    break;
            }
        }

        public static string GetText(Component textComponent)
        {
            if (textComponent == null) return "";

            switch (textComponent)
            {
                case TextMeshProUGUI textMeshProUGUI:
                    return textMeshProUGUI.text;
                case Text legacyText:
                    return legacyText.text;
                default:
                    return "";
            }
        }

        //TextComponentを取得
        public static Component GetTextComponentCache(MonoBehaviour monoBehaviour, ref Text cachedText, ref TextMeshProUGUI cachedTmp)
        {
            if (cachedTmp != null) return cachedTmp;
            if (cachedText != null) return cachedText;
            if (monoBehaviour.GetComponentCache(ref cachedTmp) != null) return cachedTmp;
            if (monoBehaviour.GetComponentCache(ref cachedText) != null) return cachedText;
            return null;
        }

        public static void SetTextInChildren(GameObject go, string text)
        {
            TextMeshProUGUI textMeshComponent = go.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshComponent)
            {
                SetText(textMeshComponent,text);
                return;
            }

            Text textComponent = go.GetComponentInChildren<Text>();
            if (textComponent)
            {
                SetText(textComponent, text);
                return;
            }
        }
    }
}
