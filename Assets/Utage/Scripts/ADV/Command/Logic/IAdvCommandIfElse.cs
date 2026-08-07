namespace Utage
{
    //if-else系コマンドに共通のインターフェース
    public interface IAdvCommandIfElse
    {
        string ToErrorString(string msg);
    }

    //if開始コマンド用のインターフェース
    public interface IAdvCommandIf : IAdvCommandIfElse
    {
    }

    //ElseIfコマンド用のインターフェース
    public interface IAdvCommandElseIf : IAdvCommandIfElse
    {

    }

    //Elseコマンド用のインターフェース
    public interface IAdvCommandElse : IAdvCommandIfElse
    {
    }

    //if終了コマンド用のインターフェース
    public interface IAdvCommandEndIf : IAdvCommandIfElse
    {
    }

}
