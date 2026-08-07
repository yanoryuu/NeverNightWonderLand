using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Utage
{
    //TextMeshProのフォントアセットのチェッカー
    [CreateAssetMenu(menuName = "Utage/Font/" + nameof(FontAssetChecker))]
    public class FontAssetChecker : ScriptableObject
    {
        [SerializeField] List<TMP_FontAsset> fonts = new ();
        [SerializeField] float maxLineHeightPerPointSize = 1.05f;

        [SerializeField, Button(nameof(Check), nameof(DisableCheck), false)]
        string check;

        void Check()
        {
            foreach (var font in fonts)
            {
                if (font == null) continue;
                var checker = new Checker(font, this);
                checker.DoCheck();
            }
        }

        bool DisableCheck()
        {
            return fonts.Count <= 0;
        }
        
        class Checker
        {
            FontAssetChecker CheckerSettings { get; }
            TMP_FontAsset Font { get; }
            public Checker(TMP_FontAsset font, FontAssetChecker checkerSettings)
            {
                this.Font = font;
                CheckerSettings = checkerSettings;
            }

            public bool DoCheck()
            {
                bool result = true;
                result &= CheckGlyph();
                DebugLogMinMaxY();
                return result;
            }

            bool CheckGlyph()
            {
                var faceInfo = Font.faceInfo;
                var fontSize = faceInfo.pointSize + Font.atlasPadding * 2;
                float maxLineHeight = faceInfo.pointSize * CheckerSettings.maxLineHeightPerPointSize;
                if (faceInfo.lineHeight <= maxLineHeight)
                {
                    return true;
                }

                //LineHeightが、PointSizeに対して大きすぎます。このままだと、行間が大きくなりすぎてしまいます。
                Debug.LogError(
                    $"LineHeight is too large. LineHeight is {faceInfo.lineHeight} but PointSize(added padding) is {fontSize}",
                    Font);

                foreach (var glyph in Font.glyphTable)
                {
                    float height = glyph.metrics.height;
                    if (height > maxLineHeight)
                    {
                        //グリフの高さが、PointSizeに対して大きすぎます。このままだと、行間が大きくなりすぎてしまいます。
                        string c = FindCharacterFromGlyphIndex(glyph.index);
                        Debug.LogError(
                            $"Glyph({glyph.index}) height is too large. Glyph height is {height} Character={c}", Font);
                    }
                }

                return false;
            }

            void DebugLogMinMaxY()
            {
                float maxHeight = float.MinValue;
                uint maxHeightGlyphIndex = 0;
                
                float minY = float.MaxValue;
                uint minGlyphIndex = 0;
                float maxY = float.MinValue;
                uint maxGlyphIndex = 0;
                foreach (var glyph in Font.glyphTable)
                {
                    var metrics = glyph.metrics;
                    float y0 = metrics.horizontalBearingY - metrics.height; 
                    float y1 = metrics.horizontalBearingY;
                    
                    if(maxHeight < metrics.height)
                    {
                        maxHeight = metrics.height;
                        maxHeightGlyphIndex = glyph.index;
                    }
                    
                    
                    if(minY > y0)
                    {
                        minY = y0;
                        minGlyphIndex = glyph.index;
                    }

                    if (maxY < y1)
                    {
                        maxY = y1;
                        maxGlyphIndex = glyph.index;
                    }
                }
                Debug.Log($"maxHeight={maxHeight} Glyph({maxHeightGlyphIndex}) {FindCharacterFromGlyphIndex(maxHeightGlyphIndex)}  \n"
                          + $"minY={minY} Glyph({minGlyphIndex}) {FindCharacterFromGlyphIndex(minGlyphIndex)}  \n"
                          + $"maxY={maxY} Glyph({maxGlyphIndex}) {FindCharacterFromGlyphIndex(maxGlyphIndex)}  \n");
            }

            string FindCharacterFromGlyphIndex(uint glyphIndex)
            {
                var character = Font.characterTable.Find(x => x.glyphIndex == glyphIndex);
                string c = character == null ? "" : FontUtil.UnicodeToCharacter(character.unicode);
                return c;
            }
        }
    }
}
