using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Utage
{
    //テキストコンポーネントのプロパティ情報を一時保存して、TextMeshPro系のコンポーネントに設定するためのクラス
    public class LegacyToTextMeshProTextInfo
    {
        string Text { get; }

        float FontSize { get; }
        float LineSpacing { get; }
        bool RichText { get; }

        TextAnchor Alignment { get; }
        HorizontalWrapMode HorizontalWrapMode { get; }
        VerticalWrapMode VerticalWrapMode { get; }
        
        bool BestFit { get; }
        int ResizeTextMinSize { get; }
        int ResizeTextMaxSize { get; }
        
        Color Color { get; }
        Vector4 RaycastPadding { get; }
        bool RaycastTarget { get; }
        bool Maskable { get; }

        public LegacyToTextMeshProTextInfo(Text textComponent)
        {
            Text = textComponent.text;

            FontSize = textComponent.fontSize;
            LineSpacing = textComponent.lineSpacing;
            RichText = textComponent.supportRichText;

            Alignment = textComponent.alignment;
            HorizontalWrapMode = textComponent.horizontalOverflow;
            VerticalWrapMode = textComponent.verticalOverflow;
            
            BestFit = textComponent.resizeTextForBestFit;
            ResizeTextMinSize = textComponent.resizeTextMinSize;
            ResizeTextMaxSize = textComponent.resizeTextMaxSize;
            
            Color = textComponent.color;
            RaycastPadding = textComponent.raycastPadding;
            RaycastTarget = textComponent.raycastTarget;
            Maskable = textComponent.maskable;
        }

        public void Apply(TextMeshProUGUI tmp)
        {
            tmp.text = Text;

            tmp.fontSize = FontSize;
            tmp.lineSpacing = (LineSpacing - 1.0f)*FontSize;
            tmp.richText = RichText;
            
            tmp.alignment = TextAnchorToTextAlignmentOptions(Alignment);
#if UNITY_6000_0_OR_NEWER
            tmp.textWrappingMode = (TextWrappingModes)(HorizontalWrapModeToWordWrapping(HorizontalWrapMode) ? 1 : 0);
#else
            tmp.enableWordWrapping = HorizontalWrapModeToWordWrapping(HorizontalWrapMode);
#endif
            tmp.overflowMode = VerticalWrapModeToOverflowMode(VerticalWrapMode);
            
            tmp.enableAutoSizing = BestFit;
            tmp.fontSizeMin = ResizeTextMinSize;
            tmp.fontSizeMax = ResizeTextMaxSize;

            tmp.color = Color;
            tmp.raycastPadding = RaycastPadding;
            tmp.raycastTarget = RaycastTarget;
            tmp.maskable = Maskable;
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

        static bool HorizontalWrapModeToWordWrapping(HorizontalWrapMode horizontalWrapMode)
        {
            switch (horizontalWrapMode)
            {
                case HorizontalWrapMode.Overflow:
                    return false;
                case HorizontalWrapMode.Wrap:
                    return true;
                default:
                    Debug.LogError($"Unhandled {nameof(HorizontalWrapMode)} : " + horizontalWrapMode);
                    return true;
            }
        }

        static TextOverflowModes VerticalWrapModeToOverflowMode(VerticalWrapMode verticalWrapMode)
        {
            switch (verticalWrapMode)
            {
                case VerticalWrapMode.Overflow:
                    return TextOverflowModes.Overflow;
                case VerticalWrapMode.Truncate:
                    return TextOverflowModes.Truncate;
                default:
                    Debug.LogError($"Unhandled {nameof(VerticalWrapMode)} : " + verticalWrapMode);
                    return TextOverflowModes.Truncate;
            }
        }

    }
}
