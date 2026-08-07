using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Utage
{
    //TextMetricsを調整ツールのグリフごとの情報
    public class TextMetricsAdjusterFontGlyph
    {
        //グリフ情報
        public Glyph Glyph { get; }

        //一番高い位置
        public float Descent { get; }

        //一番低い位置
        public float Ascent { get; }
        
        //グリフに対応する文字
        List<TMP_Character> Characters { get; } = new ();
        
        
        public enum CheckResult
        {
            Enable,
            CombiningMark,
            DisableCharacters,
        }
        public TextMetricsAdjusterFontGlyph(Glyph glyph)
        {
            Glyph = glyph;
            Descent = glyph.metrics.horizontalBearingY - glyph.metrics.height;
            Ascent = glyph.metrics.horizontalBearingY;
        }
        
        public void AddCharacter(TMP_Character character)
        {
            Characters.Add(character);
        }

        //条件に従って有効なグリフか判別する
        public CheckResult Check(TextMetricsAdjusterFont adjusterFont)
        {
            TextMetricsAdjuster.DisableCharacterSettings disableSettings = adjusterFont.Settings.DisableCharacters;
            if (disableSettings.disableCombiningMark)
            {
                foreach (var character in Characters)
                {
                    UnicodeCharacter c = new UnicodeCharacter(character.unicode);
                    if (c.IsCombiningMark)
                    {
                        return CheckResult.CombiningMark;
                    }
                }
            }

            foreach (var character in Characters)
            {
                if (FontUtil.ToUnicodeCharacters(disableSettings.disableCharacters).Any(unicodeCharacter => unicodeCharacter == character.unicode))
                {
                    //無効な文字か判定
                    return CheckResult.DisableCharacters;
                }
            }

            return CheckResult.Enable;
        }
        
        public string GetCharactersString()
        {
            string result = "";
            foreach (var character in Characters)
            {
                result += FontUtil.UnicodeToCharacter(character.unicode);
            }
            return result;
        }
    }
}
