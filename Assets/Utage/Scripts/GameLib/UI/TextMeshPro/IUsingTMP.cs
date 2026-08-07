namespace Utage
{
   
    //TextMeshProを使っている目印となるインターフェース
    //レガシーなTextコンポーネントを使うUIコンポーネントを継承するものの、テキスト表示にはTextMeshProを使うコンポーネントを作るときに使う
    //「これを持っているコンポーネントの方は、レガシーなTextコンポーネントのほうを非表示にする」などのために使用する
    public interface IUsingTextMeshPro 
    {
    }

    //TextMeshProとLegacyTextを併用している特種コンポーネントの目印となるインターフェース
    public interface IUsingTextMeshProAndLegacyText
    {
    }

}
