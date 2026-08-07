using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Utage
{
    //構造化マクロ（マクロ引数をプロパティ参照可能にするための拡張）
    // マクロ引数を構造化されたプロパティとして参照できるようにするパーサークラス
    // %Arg.propertyName の形式でマクロ引数内のプロパティにアクセス可能
    [CreateAssetMenu(menuName = "Utage/Scenario/StructuredMacroParser")]
    public class StructuredMacroParser : ScriptableObject
    {
        // プロパティ名が見つからない場合にエラーを出力するかどうか
        protected bool OutputErrorPropertyName => outputErrorPropertyName;
        [SerializeField] bool outputErrorPropertyName = true;

        //複数要素があった場合の区切り文字
        protected char PairSeparator => pairSeparator;

        [SerializeField] char pairSeparator = ',';

        //プロパティ名と値の区切り文字
        protected char KeyValueSeparator => keyValueSeparator;
        [SerializeField] char keyValueSeparator = '=';

        // キーと値のペアの列挙文字列をパースして辞書に変換する
        public virtual Dictionary<string, string> ParseKeyValueDictionary(StringGridRow row, string key)
        {
            var text = row.ParseCellOptional(key, "");
            if (!ParserUtil.TryParseKeyValueDictionary(text, PairSeparator, KeyValueSeparator, out var dictionary))
            {
                Debug.LogError(row.ToErrorString($"Failed to parse property dictionary from '{text}'"));
            }
            return dictionary;
        }

        // 構造化マクロ引数のパース処理
        // str: パース対象の文字列全体（例: "%Arg.property"）
        // index: strのパース開始位置（例: 1）
        // key: マクロキー名（例: "Arg"）
        // args: マクロ引数セル（key0=value0,key1=value1形式）
        // header: マクロのヘッダ部分（マクロ名の行と同じで、引数が入る）
        // 戻り値: パース結果（成功時は値と終了位置、失敗時は失敗情報）
        public virtual StructuredMacroResult TryParseStructuredMacroArguments(string str, int index, string key, StringGridRow args, StructuredMacroParsedProperty parsedProperty)
        {
            // %Arg の直後が '.' でなければ対象外
            var dotIndex = index + 1 + key.Length;
            if (dotIndex >= str.Length || str[dotIndex] != '.')
            {
                return StructuredMacroResult.Fail(index);
            }

            // プロパティ名を取得（英数字とアンダースコアのみ許可）
            int propStart = dotIndex + 1;
            int i = propStart;

            while (i < str.Length)
            {
                char c = str[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    break;
                i++;
            }

            if (i == propStart)
            {
                // プロパティ名なし
                return StructuredMacroResult.Fail(index);
            }

            //プロパティ名を抽出
            string propertyName = str.Substring(propStart, i - propStart);
            int nextIndex = i - 1;
            
            if( parsedProperty.TryGetValue(key,args,propertyName, out var value) )
            {
                return StructuredMacroResult.Ok(value, nextIndex);
            }

            // プロパティ名が指定されているが、マクロ引数テキストにも、マクロヘッダー内にもそのプロパティ名がない場合、失敗
            if (OutputErrorPropertyName)
            {
                Debug.LogError(args.ToErrorString($"Property '{propertyName}' not found in macro arguments or header."));
            }

            return StructuredMacroResult.Fail(index);
        }
    }
}
