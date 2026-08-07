using System.Collections.Generic;

namespace Utage
{
    //コマンドの初期化を遅延させるためのインターフェース
    //シナリオデータ大量にあると、起動時に一気に初期化すると処理が重くなりすぎるので
    //特に初期化の負荷が高いコマンドを中心に、初期化をコマンド実行時（またはインポート時）まで遅らせる処理を行う
    public interface IAdvCommandDelayInitialize
    {
        //遅延初期化処理
        void DelayInitialize(AdvSettingDataManager dataManager);
    }
}
