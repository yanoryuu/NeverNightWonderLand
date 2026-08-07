using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //フォントの設定
    //主にローカライズをするために使う
    [CreateAssetMenu(menuName = "Utage/Font/" + nameof(FontSettings))]
    public class FontSettings : ScriptableObject
    {
        //初期設定されてるフォント言語名
        public string DefaultFontLanguage
        {
            get => defaultFontLanguage;
            set => defaultFontLanguage = value;
        }
        [SerializeField] string defaultFontLanguage;

        //言語切り替えの際に、フォールバックの切り替えが必要になる言語名一覧
        public List<string> FontLanguages => fontLanguages;
        [SerializeField] List<string> fontLanguages;

        //その他の言語としてのフォント言語名がある場合は、設定する
        public string FontLanguageOthers => fontLanguageOthers;
        [SerializeField] string fontLanguageOthers;


        //現在のシステム言語に合致する、対応言語名を取得
        public string GetSystemLanguageName()
        {
            //システム言語名を取得
            var language = Application.systemLanguage.ToString();
            if (!TyGetFontLanguageName(language, out string fontLanguage))
            {
                //対応フォント名がない場合は、
                return DefaultFontLanguage;
            }
            return fontLanguage;
        }
        
        //指定の言語名に対応するフォント言語名を取得
        public bool TyGetFontLanguageName(string language, out string fontLanguage)
        {
            fontLanguage = language;
            if (!FontLanguages.Contains(language))
            {
                //フォント言語名一覧にない場合
                //FontLanguageOthersが設定されていなければ未対応で取得できない
                if (string.IsNullOrEmpty(FontLanguageOthers))
                {
                    fontLanguage = "";
                    return false;
                }
                //FontLanguageOthersが設定されていればそちらを使う
                fontLanguage = FontLanguageOthers;
            }
            return true;
        }
    }
}
