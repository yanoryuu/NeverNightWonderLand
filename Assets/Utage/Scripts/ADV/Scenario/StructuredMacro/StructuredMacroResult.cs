namespace Utage
{
    //構造化マクロの解析結果
    public struct StructuredMacroResult
    {
        //解析が成功したかどうか
        public bool Success;
        
        //マクロ展開後の文字列
        public string Expanded;
        //マクロ展開後の新しいインデックス
        public int NewIndex;

        //失敗した場合の結果
        public static StructuredMacroResult Fail(int index)
        {
            return new StructuredMacroResult
            {
                Success = false,
                Expanded = null,
                NewIndex = index
            };
        }

        //成功した場合の結果
        public static StructuredMacroResult Ok(string expanded, int newIndex)
        {
            return new StructuredMacroResult
            {
                Success = true,
                Expanded = expanded,
                NewIndex = newIndex
            };
        }
    }

}