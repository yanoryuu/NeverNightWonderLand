using System;
using System.Collections.Generic;

namespace Utage
{
    //文字列操作全般のユーティリティ
    public static class StringUtil
    {
        //指定の文字列が、指定の接尾辞で終わっている場合、その接尾辞を削除した文字列を返す
        public static string RemoveSuffix(string str, string suffix)
        {
            return RemoveSuffix(str, suffix, StringComparison.InvariantCultureIgnoreCase);
        }
        public static string RemoveSuffix(string str, string suffix, StringComparison comparisonType)
        {
            if (str.EndsWith(suffix, comparisonType))
            {
                return str.Substring(0, str.Length - suffix.Length);
            }
            else
            {
                return str;
            }
        }

        //指定の文字列が、指定の接頭辞で始まっている場合、その接頭辞を削除した文字列を返す
        public static string RemovePrefix(string str, string prefix)
        {
            return RemovePrefix(str, prefix, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string RemovePrefix(string str, string prefix, StringComparison comparisonType)
        {
            if (str.StartsWith(prefix, comparisonType))
            {
                return str.Substring(prefix.Length, str.Length - prefix.Length);
            }
            else
            {
                return str;
            }
        }


        //改行文字をエスケープ処理
        public static string EscapeNewlines(string msg)
        {
            msg = msg.Replace("\r\n", @"\n");
            msg = msg.Replace("\n", @"\n");
            msg = msg.Replace("\r", @"\n");
            return msg;
        }

        //ダブルクオーテーションで囲まれている場合はエスケープ処理を行う
        public static string EscapeQuotes(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                // ダブルクオーテーションで囲まれている場合はエスケープ処理を行う
                return input.Substring(1, input.Length - 2); // 先頭と末尾のダブルクオーテーションを削除
            }
            else
            {
                // ダブルクオーテーションで囲まれていない場合はそのまま返す
                return input;
            }
        }
        
        
        //配列をカンマで分割し、各要素の前後の空白を削除する
        public static string[] SplitByComma(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();

            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return parts;
        }
    }
}
