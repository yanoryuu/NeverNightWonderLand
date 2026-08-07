using UnityEngine;

namespace Utage
{
    //言語変更時のイベントをScriptableObjectとして拡張可能にするための基底クラス
    public interface ILanguageChangeEvent
    {
        //言語が変更されたとき
        public void OnChangeLanguage();

        //終了時（主にフォントアセットなどをランタイムで書き換えてしまったものを戻すのに使用）
        public void OnFinalize();
    }
}
