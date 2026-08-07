namespace Utage
{
    //UIプログラムでインデックスを汎用的に扱うためのインターフェース
    public interface IUguiIndex
    {
        //現在のインデックス
        public int Index { get; }

        //インデックスを設定するためのコンポーネント
        //インデックスと同時に、最大長も設定することで最終インデックスかの判定などもできるようにする
        void SetIndex(int index, int length);
    }
}
