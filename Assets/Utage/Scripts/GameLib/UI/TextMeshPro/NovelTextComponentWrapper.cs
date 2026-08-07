using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //NovelText系のコンポーネント(TextMeshPro対応とレガシー版）を兼用するためのラッパー
    //UguiNovelTextはTextを継承していて、UguiNovelTextではなくTextで型指定される場合があるので、ラッパーでもそれに倣う
    public static class NovelTextComponentWrapper
    {
        //テキストの設定
        public static void SetText(Text legacy, TextMeshProNovelText textMeshPro, string text)
        {
            if (legacy != null) legacy.text = text;
            if (textMeshPro != null) textMeshPro.SetText(text);
        }

        public static void SetTextDirect(UguiNovelText legacy, TextMeshProNovelText textMeshPro, string text)
        {
            if (legacy != null) legacy.text = text;
            if (textMeshPro != null) textMeshPro.SetTextDirect(text);
        }

        //カラーの設定
        public static void SetColor(Text legacy, TextMeshProNovelText textMeshPro, Color color)
        {
            if (legacy != null) legacy.color = color;
            if (textMeshPro != null) textMeshPro.Color = color;
        }
        //カラーの取得
        public static Color GetColor(Text legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null) return legacy.color;
            if (textMeshPro != null) return textMeshPro.Color;
            return Color.white;
        }

        //RectTransformの取得
        public static RectTransform GetRectTransform(Text legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null) return legacy.rectTransform;
            if (textMeshPro != null) return (RectTransform)textMeshPro.transform;
            return null;
        }

        //PreferredHeightの取得
        public static float GetPreferredHeight(Text legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null) return legacy.preferredHeight;
            if (textMeshPro != null)
            {
                //テキストが変更されていたら反映してから
                textMeshPro.UpdateIfChanged();
                return textMeshPro.TextMeshPro.preferredHeight;
            }
            return 0;
        }

        //テキストのクリア
        public static void Clear(UguiNovelText legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null)
            {
                legacy.text = "";
                legacy.LengthOfView = 0;
            }
            if (textMeshPro != null) textMeshPro.Clear();
        }

        public static void Clear(Text legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null)
            {
                legacy.text = "";
            }

            if (textMeshPro != null)
            {
                textMeshPro.Clear();
            }
        }

        public static void SetNovelTextData(UguiNovelText legacy, TextMeshProNovelText textMeshPro, TextData textData,
            int lengthOfView)
        {
            if (legacy != null)
            {
                //パラメーターを反映させるために、一度クリアさせてからもう一度設定
                legacy.text = "";
                legacy.text = textData.OriginalText;
                legacy.LengthOfView = lengthOfView;
            }

            if (textMeshPro != null)
            {
                //旧NovelTextの場合、「-1で全部のテキストを表示」だったので、それに合わせる
                textMeshPro.SetNovelTextData(textData, lengthOfView < 0 ? 99999 : lengthOfView);
            }
        }

        public static void SetMaxVisibleCharacters(UguiNovelText legacy, TextMeshProNovelText textMeshPro,
            int maxVisibleCharacters)
        {
            if (legacy != null) legacy.LengthOfView = maxVisibleCharacters;
            if (textMeshPro != null)
            {
                //旧NovelTextの場合、「-1で全部のテキストを表示」だったので、それに合わせる
                textMeshPro.MaxVisibleCharacters = maxVisibleCharacters < 0 ? 99999 : maxVisibleCharacters;
            }
        }

        public static Vector3 GetCurrentEndPosition(UguiNovelText legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null) return legacy.CurrentEndPosition;
            if (textMeshPro != null) return textMeshPro.CurrentEndPosition;
            return Vector3.zero;
        }

        public static string GetText(UguiNovelText legacy, TextMeshProNovelText textMeshPro)
        {
            if (legacy != null) return legacy.text;
            if (textMeshPro != null) return textMeshPro.GetText();
            return "";
        }

        public static bool TryCheckCharacterCount(UguiNovelText legacy, TextMeshProNovelText textMeshPro, string str, out int count, out string errorString)
        {
            if (legacy != null) return legacy.TextGenerator.EditorCheckRect(str, out count, out errorString);
            if (textMeshPro != null) return textMeshPro.TryCheckCharacterCount(str, out count, out errorString);

            errorString = "NovelText is missing.";
            count = 0;
            return true;
        }
    }
}
