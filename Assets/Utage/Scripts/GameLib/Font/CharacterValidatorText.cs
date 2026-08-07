using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

namespace Utage
{
    //文字化けを防ぐために、想定外の文字が含まれていないかチェックするクラス
    public class CharacterValidatorText : CharacterValidator
    {
        public TextAsset TextAsset { get; }
        public override Object TargetAsset => TextAsset;

        public CharacterValidatorText(TextAsset textAsset)
        {
            TextAsset = textAsset;
            AddEnableCharacters(TextAsset.text);
        }
    }
}
