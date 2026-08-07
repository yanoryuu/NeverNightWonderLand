using UnityEngine;

namespace Utage
{
    //インポートの後処理をするための設定データのScriptableObjectの基底クラス
    public abstract class ScenarioImportPostProcessorSettings : ScriptableObject
    {
        //後処理を実行するクラスを作成
        public abstract ScenarioImportPostProcessor CreatePostProcessor();
    }
}
