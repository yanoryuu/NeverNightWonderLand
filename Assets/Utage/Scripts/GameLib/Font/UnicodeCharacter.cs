using System;
using System.Globalization;
using UnityEngine;


namespace Utage
{
    //ユニコードの文字情報
    public class UnicodeCharacter
    {
        //ユニコード値（code point）
        public uint Unicode { get; }

        //文字。サロゲートペア文字列が入る場合があるので、string型
        public string Character { get; }

        //Unicodeのカテゴリ
        public UnicodeCategory UnicodeCategory { get; }

        //サロゲートペア文字列かどうか
        public bool IsSurrogatePair { get; }

        //組み文字かどうか
        public bool IsCombiningMark { get; }

        public UnicodeCharacter(string code)
            : this(FontUtil.UnicodeStringToUnicode(code))
        {
        }

        public UnicodeCharacter(uint unicode)
        {
            Unicode = unicode;
            Character = FontUtil.UnicodeToCharacter(unicode);
            StringInfo stringInfo = new(Character);
            if (stringInfo.LengthInTextElements != 1 )
            {
                Debug.LogError($"{unicode} {Character}　のLengthInTextElements {stringInfo.LengthInTextElements}が1ではありません");
            }

            UnicodeCategory = char.GetUnicodeCategory(Character[0]);

            IsCombiningMark = false;
            IsSurrogatePair = false;
            switch (UnicodeCategory)
            {
                case UnicodeCategory.Surrogate:
                    IsSurrogatePair = true;
                    break;
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.EnclosingMark:
                    IsCombiningMark = true;
                    break;
                default:
                    break;
            }

            int length = Character.Length;
            if (length != 1)
            {
                if (length > 1 )
                {
                    if (!IsSurrogatePair)
                    {
                        Debug.LogError($"{unicode} : {Character}はサロゲートペア文字ではないのにcharが複数あります");
                    }
                }
                else
                {
                    Debug.LogError($"{unicode} : {Character}　のchar数{length}が1ではありません");
                }
            }
        }
    }
}
