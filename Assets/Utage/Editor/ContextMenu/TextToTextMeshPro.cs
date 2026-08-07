// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Utage
{
    //ヒエラルキーWindow内で、GameObjectの作成などをサポート
    public class TextToTextMeshPro
    {
        public class TextSetting
        {
            float FontSize { get; }
            float LineSpacing { get; }
            TextAnchor Alignment { get; }
            string Text { get; }
            Color Color { get; }
            public TextSetting(Text textComponent)
            {
                FontSize = textComponent.fontSize;
                LineSpacing = textComponent.lineSpacing;
                Alignment = textComponent.alignment;
                Text = textComponent.text;
                Color = textComponent.color;
            }

            public void Apply(TextMeshProUGUI tmp)
            {
                tmp.fontSize = FontSize;
                tmp.lineSpacing = LineSpacing;
                tmp.alignment = TextAnchorToTextAlignmentOptions(Alignment);
                tmp.text = Text;
                tmp.color = Color;
            }
            static TextAlignmentOptions TextAnchorToTextAlignmentOptions(TextAnchor textAnchor)
            {
                switch (textAnchor)
                {
                    case TextAnchor.UpperLeft:
                        return TextAlignmentOptions.TopLeft;

                    case TextAnchor.UpperCenter:
                        return TextAlignmentOptions.Top;

                    case TextAnchor.UpperRight:
                        return TextAlignmentOptions.TopRight;

                    case TextAnchor.MiddleLeft:
                        return TextAlignmentOptions.Left;

                    case TextAnchor.MiddleCenter:
                        return TextAlignmentOptions.Center;

                    case TextAnchor.MiddleRight:
                        return TextAlignmentOptions.Right;

                    case TextAnchor.LowerLeft:
                        return TextAlignmentOptions.BottomLeft;

                    case TextAnchor.LowerCenter:
                        return TextAlignmentOptions.Bottom;

                    case TextAnchor.LowerRight:
                        return TextAlignmentOptions.BottomRight;
                }

                Debug.LogWarning("Unhandled text anchor " + textAnchor);
                return TextAlignmentOptions.TopLeft;
            }
        }

//      [MenuItem("GameObject/TextToTextMeshPro",false,-1)]
        public static void SwapTextComponent()
        {
            var target = Selection.activeGameObject;
            if(target == null) return;

            var textComponent = target.GetComponent<Text>();
            if(textComponent==null) return;

            // アンドゥ操作をまとめるためのグループ
            using (new EditorUndoGroupScope(nameof(SwapTextComponent)))
            {
                var setting = new TextSetting(textComponent);
                Undo.DestroyObjectImmediate(textComponent);
                var textMeshPro = Undo.AddComponent<TextMeshProUGUI>(target);
                setting.Apply(textMeshPro);
            }
        }
    }
}
