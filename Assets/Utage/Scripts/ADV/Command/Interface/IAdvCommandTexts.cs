using System.Collections.Generic;

namespace Utage
{
    //コマンドが使用する表示テキスト文字列を全て取得するためのインターフェース
    public interface IAdvCommandTexts
    {
        //表示するテキスト文字列を全て取得
        IEnumerable<string> GetTextStrings();
    }
}
