using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Utage
{
    //文字化けを防ぐために、想定外の文字が含まれていないかチェックするクラス
    public abstract class CharacterValidator
    {
        //使用可能な文字
        protected UnicodeCharacterCollection EnableCharSet { get; } = new();


        //エラーになった文字
        public UnicodeCharacterCollection MissingCharacters { get; } = new();
        
        //文字の収録リストの対象のアセット（フォントやテキスト）
        public abstract Object TargetAsset { get; }


        //使用可能な文字を文字列の形で追加する
        public void AddEnableCharacters(string enableCharacters)
        {
            foreach (uint unicode in FontUtil.ToUnicodeCharacters(enableCharacters))
            {
                AddEnableCharacter(unicode);
            }
        }

        //使用可能な文字を追加する
        protected virtual void AddEnableCharacter(uint unicode)
        {
            EnableCharSet.AddCharacter(unicode);
        }

        //文字列が使用可能な文字セット内に全て含まれているかチェック
        public bool Validate(uint unicode)
        {
            bool result = EnableCharSet.Contains(unicode);
            if(!result)
            {
                //エラー文字として追加
                MissingCharacters.AddCharacter(unicode);
            }
            return result;
        }
    }
}
