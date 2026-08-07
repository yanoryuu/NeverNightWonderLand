using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Utage
{
    public static class FontUtil
    {
        public const uint ReplacementCharacter = 0xFFFD;
        public const uint MinUnicodeCodePoint = 0x0;
        public const uint MaxUnicodeCodePoint = 0x10FFFF;
        
        //16進数のUnicode文字列を、Unicodeに変換
        public static uint UnicodeStringToUnicode(string code)
        {
            return Convert.ToUInt32(code, 16);
        }

        //Unicodeを、サロゲートペアを考慮した文字（string)に変換
        public static string UnicodeToCharacter(uint unicode)
        { 
            return char.ConvertFromUtf32((int)unicode);
        }
        
        //サロゲートペア文字を、Unicode値に変換
        public static uint SurrogatePairsToUnicode(char highSurrogate, char lowSurrogate)
        {
            return (uint)char.ConvertToUtf32(highSurrogate, lowSurrogate);
        }

        //指定のUnicodeCharacterのリストを連結して、stringに変換
        public static string UnicodeCharactersToString(IEnumerable<UnicodeCharacter> unicodeCharacters)
        {
            string str = "";
            foreach (var item in unicodeCharacters)
            {
                str += item.Character;
            }
            return str;
        }

        //Unicodeがサロゲート文字かどうか
        //ペアになっていないサロゲート文字を扱う場合に
        //通常は使わないが、ReplacementCharacter(0xDFFF)の場合はフォントとしてグリフをもっているので、特殊な処理が必要になることがある
        public static bool IsSurrogate(uint unicode)
        {
            if(unicode > char.MaxValue)
            {
                return false;
            }
            return char.IsSurrogate((char)unicode);
        }
        
        //ReplacementCharacterかのチェック
        public static bool IsReplacementCharacter(uint unicode)
        {
            return unicode == ReplacementCharacter;
        }

        //サロゲートペアを考慮したstringのLengthを取得
        public static int LengthWithSurrogatePairs(string str)
        {
            //str.LengthInTextElementsは組み文字も含めての文字数なのでこの場合は使用しない

            int len = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsSurrogatePair(str, i))
                {
                    i++;
                }

                ++len;
            }

            return len;
        }


        //サロゲートペアを考慮して、stringをユニコードの列挙に変換
        public static IEnumerable<uint> ToUnicodeCharacters(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsSurrogatePair(str, i))
                {
                    yield return SurrogatePairsToUnicode(str[i], str[i + 1]);
                    i++;
                }
                else
                {
                    yield return str[i];
                }
            }
        }
#if false
        
        //サロゲートペアを考慮したstringのforeach
        //コールバックに、文字と、文字の開始位置と終了位置を渡す
        public static void ForeachWithSurrogatePairs(string str, Action<string, int, int> callback)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsSurrogatePair(str, i))
                {
                    callback?.Invoke(str, i, 2);
                    i++;
                }
                else
                {
                    callback?.Invoke(str, i, 1);
                }
            }
        }

        //サロゲートペアを考慮したstringのforeach
        //コールバックに、stringとして文字を渡す
        public static void ForeachWithSurrogatePairs(string str, Action<string> callback)
        {
            ForeachWithSurrogatePairs(str, (s, i, len) => callback.Invoke(s.Substring(i, len)));
        }

        //サロゲートペアを考慮したstringのforeach
        //コールバックに、uintのユニコードを渡す
        public static void ForeachWithSurrogatePairs(string str, Action<uint> callback)
        {
            ForeachWithSurrogatePairs(
                str,
                (c) => callback(c),
                (highSurrogate, lowSurrogate) =>
                {
                    callback(FontUtil.SurrogatePairsToUnicode(highSurrogate, lowSurrogate));
                });
        }

        //サロゲートペアを考慮したstringのforeach
        //コールバックで通常の文字かサロゲートペアかを分ける
        public static void ForeachWithSurrogatePairs(string str, Action<char> callbackNormalCharacter,
            Action<char, char> callbackSurrogatePairs)
        {
            ForeachWithSurrogatePairs(str, (s, i, len) =>
            {
                if (len == 1)
                {
                    callbackNormalCharacter?.Invoke(s[i]);
                }
                else
                {
                    callbackSurrogatePairs?.Invoke(s[i], s[i + 1]);
                }
            });
        }
#endif
    }
}
