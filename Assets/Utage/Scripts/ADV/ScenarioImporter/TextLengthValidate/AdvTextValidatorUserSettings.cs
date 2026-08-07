using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Utage
{
    //Adv用の、テキストの長さを検証するためのユーザーごとの設定データ
    [System.Serializable]
    public class AdvTextValidatorUserSettings
    {
        public enum ValidatorType
        {
            Disable,        //チェックしない
            Enable,         //文字あふれをチェックする（現在の言語に対してのみ）
            AllLanguage,    //全言語でチェックする
        }

        public ValidatorType ValidateType => validateType;
        [SerializeField] ValidatorType validateType;
        
        public AdvTextValidator CreateValidator()
        {
            return new AdvTextValidator(this);
        }
    }

}
