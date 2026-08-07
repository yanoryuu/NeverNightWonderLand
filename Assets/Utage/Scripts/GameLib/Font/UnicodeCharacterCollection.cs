using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Utage
{
    //Unicode文字のコレクション(重複しないユニコード文字の集合)
    //使用文字リストを扱うために利用
    //TextMeshProのフォントアセットの作成で指定するテキストファイルを作ったり、使用文字が抜けていないかチェックするのに使う
    public class UnicodeCharacterCollection
    {
        //全文字(ユニコードをキーにする)
        public Dictionary<uint, UnicodeCharacter> Characters { get; } = new();

        //ログメッセージ作成用
        StringBuilder LogMessageBuilder { get; } = new();


        public void Clear()
        {
            Characters.Clear();
            LogMessageBuilder.Clear();
        }

        public bool Contains(uint unicode)
        {
            return Characters.ContainsKey(unicode);
        }

        public bool AddCharacter(uint unicode)
        {
            if (Contains(unicode))
            {
                return false;
            }
            return Characters.TryAdd(unicode, new UnicodeCharacter(unicode));
        }

        public void AddCharacters(string charactersText)
        {
            foreach (uint unicode in FontUtil.ToUnicodeCharacters(charactersText))
            {
                AddCharacter(unicode);
            }
        }

        public void AddCharacters(UnicodeCharacterCollection collection)
        {
            foreach (var keyValue in collection.Characters)
            {
                AddCharacter(keyValue.Key);
            }
        }

        public bool RemoveCharacter(uint unicode)
        {
            return Characters.Remove(unicode);
        }

        //指定のコレクションと同じ文字を削除(実際に削除されたものをremovedCollectionに追加)
        public void RemoveCharacters(UnicodeCharacterCollection removeCollection, UnicodeCharacterCollection removedCollection)
        {
            foreach (var keyValue in removeCollection.Characters)
            {
                if (Characters.Remove(keyValue.Key))
                {
                    removedCollection.AddCharacter(keyValue.Key);
                }
            }
        }

        //全文字コレクションを出力
        public void Output(string path)
        {
            FileIoUtil.CreateFilePathDirectoryIfNotExists(path);

            //全文字をテキストに出力
            StringBuilder builder = new();
            //キー（Unicode）でソート
            var characters = Characters.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            foreach (var character in characters)
            {
                builder.Append(character.Character);
                
                //ペアになっていないサロゲート文字
                if (FontUtil.IsSurrogate(character.Unicode))
                {
                    if (!FontUtil.IsReplacementCharacter(character.Unicode))
                    {
                        //置き換え文字ではないのは想定外なので警告を出す
                        Debug.LogWarning($"single surrogate character {character.Character} is not replacement character");
                    }
                    //サロゲートペアの場合は空白を入れる
//                  builder.Append(" ");
                }
            }

            string characterSet = builder.ToString();
            File.WriteAllText(path, characterSet, Encoding.UTF8);
        }

        //内容のログを出力
        public void OutputLog(string path, string logHeader, bool debugLog = false)
        {
            FileIoUtil.CreateFilePathDirectoryIfNotExists(path);

            LogMessageBuilder.Clear();
            LogMessageBuilder.AppendLine(logHeader);

            var characters = Characters.OrderBy(x => x.Key).Select(x => x.Value).ToList();

            var normalCharacters = characters.Where(x => !x.IsSurrogatePair && !x.IsCombiningMark).ToList();
            var surrogatePairs = characters.Where(x => x.IsSurrogatePair).ToList();
            var combiningMarks = characters.Where(x => x.IsCombiningMark).ToList();

            //文字数のログ
            LogMessageBuilder.AppendLine($"character count : {characters.Count}");
            LogMessageBuilder.AppendLine($"normal character count : {normalCharacters.Count}");
            LogMessageBuilder.AppendLine($"surrogate pair count : {surrogatePairs.Count}");
            LogMessageBuilder.AppendLine($"combining mark count : {combiningMarks.Count}");
            if (debugLog)
            {
                Debug.Log(LogMessageBuilder.ToString());
            }

            LogMessageBuilder.AppendLine($"Normal Characters...");
            LogMessageBuilder.AppendLine($"");
            foreach (var c in normalCharacters)
            {
                LogMessageBuilder.AppendLine(c.Character);
            }


            LogMessageBuilder.AppendLine($"");
            LogMessageBuilder.AppendLine($"");
            LogMessageBuilder.AppendLine($"Surrogate pair list...");
            foreach (var surrogatePair in surrogatePairs)
            {
                LogMessageBuilder.AppendLine(surrogatePair.Character);
            }

            LogMessageBuilder.AppendLine($"");
            LogMessageBuilder.AppendLine($"Combining Mark list...");
            foreach (var combiningMark in combiningMarks)
            {
                LogMessageBuilder.AppendLine(combiningMark.Character);
            }


            var log = LogMessageBuilder.ToString();
            File.WriteAllText(path, log, Encoding.UTF8);

            LogMessageBuilder.Clear();
        }
    }
}
