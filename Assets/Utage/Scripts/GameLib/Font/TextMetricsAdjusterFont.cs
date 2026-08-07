using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Utage
{
    //TextMetricsを調整ツールのフォントごとの情報
    public class TextMetricsAdjusterFont
    {
        //調整ツールの情報
        public TextMetricsAdjuster Settings { get; }

        //フォントアセット
        public TMP_FontAsset Font { get; }

        //グリフ情報
        Dictionary<uint, TextMetricsAdjusterFontGlyph> Glyphs { get; } = new ();

        List<TextMetricsAdjusterFontGlyph> EnableGlyphs { get; } = new();
        List<TextMetricsAdjusterFontGlyph> DisableGlyphs { get; } = new();

        //フォントのアセットの収録文字の中で、一番高い位置を持つグリフを取得
        public TextMetricsAdjusterFontGlyph AscenderGlyph { get; }

        //フォントのアセットの収録文字の中で、一番低い位置を持つグリフを取得
        public TextMetricsAdjusterFontGlyph DecentGlyph { get; }

        public TextMetricsAdjusterFont(TMP_FontAsset font, TextMetricsAdjuster settings)
        {
            Settings = settings;
            Font = font;
            foreach (Glyph glyph in Font.glyphTable)
            {
                Glyphs.Add(glyph.index,new TextMetricsAdjusterFontGlyph(glyph));
            }
            foreach (var character in Font.characterTable)
            {
                if (!Glyphs.TryGetValue(character.glyphIndex, out TextMetricsAdjusterFontGlyph glyph))
                {
                    //文字データはあるのにグリフデータがない
                    //本来あり得ないが、フォントのアセットのデータが壊れている可能性があるので、エラーを出す
                    Debug.LogError($"{Font.name} glyphIndexError={character.glyphIndex}/{Glyphs.Count} {FontUtil.UnicodeToCharacter(character.unicode)}",
                        Font);
                    continue;
                }
                glyph.AddCharacter(character);
            }

            foreach (var keyValue in Glyphs)
            {
                var glyph = keyValue.Value;
                if (glyph.Check(this) == TextMetricsAdjusterFontGlyph.CheckResult.Enable)
                {
                    EnableGlyphs.Add(glyph);
                }
                else
                {
                    DisableGlyphs.Add(glyph);
                }
            }
            DecentGlyph = GetDecentGlyphByAllCharactersInFontAsset(EnableGlyphs);
            AscenderGlyph = GetAscenderGlyphByAllCharactersInFontAsset(EnableGlyphs);
        }

        //フォントのアセットの収録文字の中で、一番低い位置を持つグリフを取得
        TextMetricsAdjusterFontGlyph GetDecentGlyphByAllCharactersInFontAsset(List<TextMetricsAdjusterFontGlyph> enableGlyphs)
        {
            return enableGlyphs.OrderBy(x=>x.Descent).FirstOrDefault();
        }
        //フォントのアセットの収録文字の中で、一番高い位置を持つグリフを取得
        TextMetricsAdjusterFontGlyph GetAscenderGlyphByAllCharactersInFontAsset(List<TextMetricsAdjusterFontGlyph> enableGlyphs)
        {
            return enableGlyphs.OrderByDescending(x=>x.Ascent).FirstOrDefault();
        }

        public bool CheckLine()
        {
            bool result = true;
            
            StringBuilder ascentOverCharacters = new StringBuilder();
            StringBuilder ascentOverErrorMsg = new StringBuilder();

            StringBuilder descentOverCharacters = new StringBuilder();
            StringBuilder descentOverErrorMsg = new StringBuilder();
            foreach (var glyph in EnableGlyphs)
            {
                if (Settings.MaxAscent != 0 && glyph.Ascent > Settings.MaxAscent)
                {
                    result = false;
                    string str = glyph.GetCharactersString();
                    ascentOverCharacters.Append(str);
                    ascentOverErrorMsg.AppendLine($"{str} AscentLine({glyph.Ascent}) is overed {nameof(Settings.MaxAscent)}({Settings.MaxAscent}).");
                }

                if (Settings.MinDescent != 0 && glyph.Descent < Settings.MinDescent)
                {
                    result = false;
                    string str = glyph.GetCharactersString();
                    descentOverCharacters.Append(str);
                    descentOverErrorMsg.AppendLine($"{str} DescentLine({glyph.Descent}) is overed {nameof(Settings.MinDescent)}({Settings.MinDescent}).");
                }
            }

            if (result) return true;    //問題なし

            if (ascentOverCharacters.Length > 0)
            {
                Debug.LogError($"{Font.name} AscentOver={ascentOverCharacters}\n {ascentOverErrorMsg}", Font);
            }

            if (descentOverCharacters.Length > 0)
            {
                Debug.LogError($"{Font.name} DescentOver={descentOverCharacters}\n {descentOverErrorMsg}", Font);
            }
            //エラー
            return false;
        }
    }
}
