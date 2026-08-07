using UnityEngine;

namespace Utage
{
    //言語変更時のイベントをScriptableObjectとして拡張可能にするための基底クラス
    public abstract class LanguageChangeEventFactory : ScriptableObject
    {
        //イベント用のインスタンスを作成
        public abstract ILanguageChangeEvent CreateEvent(LanguageManagerBase languageManager);
    }
}
