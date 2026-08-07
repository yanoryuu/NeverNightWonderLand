using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //構造化マクロ（マクロ引数をプロパティ参照可能にするための拡張）の際に使用する、プロパティ情報を保持するクラス
    public class StructuredMacroParsedProperty
    {
        AdvMacroData MacroData { get; }
        //ヘッダ部分のプロパティ情報
        Dictionary<string, Dictionary<string, string>> HeaderProperties { get; set; } = new();
        //引数一行のプロパティ情報
        Dictionary<string, Dictionary<string, string>> ArgsProperties { get; set; } = new();
        
        StructuredMacroParser StructuredMacroParser { get; }

        public StructuredMacroParsedProperty(AdvMacroData macroData,StructuredMacroParser parser)
        {
            MacroData = macroData;
            StructuredMacroParser = parser;
        }
        
        public void StartRowParse()
        {
            ArgsProperties.Clear();
        }

        
        public bool TryGetValue(string key, StringGridRow args, string propertyName, out string value)
        {
            var argProperties = GetArgsProperties(args, key);
            //argProperties内にpropertyNameと一致する変数がある
            if (argProperties.TryGetValue(propertyName, out value))
            {
                return true;
            }

            //headerProperties内にpropertyNameと一致する変数がある
            var headerProperties = GetHeaderProperties(key);
            if (headerProperties.TryGetValue(propertyName, out value))
            {
                return true;
            }
            return false;
        }

        Dictionary<string, string> GetHeaderProperties(string key)
        {
            if (HeaderProperties.TryGetValue(key, out var properties))
            {
                return properties;
            }
            //ヘッダーをプロパティ名と値のDictionaryに展開
            properties = StructuredMacroParser.ParseKeyValueDictionary(MacroData.Header,key);
            HeaderProperties.Add(key, properties);
            return properties;
        }
        
        Dictionary<string, string> GetArgsProperties(StringGridRow args, string key)
        {
            if (ArgsProperties.TryGetValue(key, out var properties))
            {
                return properties;
            }
            //Argをプロパティ名と値のDictionaryに展開
            properties = StructuredMacroParser.ParseKeyValueDictionary(args,key);
            ArgsProperties.Add(key, properties);
            //スペルミスチェック
            DetectMisspelledPropertyNames(properties, GetHeaderProperties(key), args);
            return properties;
        }
        
        //ArgsPropertiesのキー（プロパティ名）のスペルミスチェック。
        //Header内に存在しないキーなら、エラーログを出す
        void DetectMisspelledPropertyNames(Dictionary<string, string> properties, Dictionary<string, string> headerProperties, StringGridRow args)
        {
            foreach (var keyValue in properties)
            {
                if (!headerProperties.ContainsKey(keyValue.Key))
                {
                    Debug.LogError(args.ToErrorString($"Property '{keyValue.Key}' not found in macro header."));
                }
            }
        }
    }
}
