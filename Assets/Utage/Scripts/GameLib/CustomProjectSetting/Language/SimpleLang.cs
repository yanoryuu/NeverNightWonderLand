using UnityEngine;

namespace Utage
{
    //Unityエディタ上でのローカライズ処理をするためのクラス
    //安定して使えるように、初期化やロードなどが必要ないようにハードコーディングでテキストなどを設定する簡易的なもの
    public static class SimpleLang
    {
        //日本語と英語のどちらかを選択して返すだけの関数
        public static string Select(string jp, string en)
        {
            return Application.systemLanguage == SystemLanguage.Japanese
                ? jp : en;
        }
    }
}

