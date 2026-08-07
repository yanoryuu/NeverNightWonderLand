
namespace Utage
{
	//ルビの情報
	public class TextMeshProRubyInfo
	{
		//ルビの文字列
		public string Ruby { get; set; }

		//ルビがつく本文の先頭文字インデックス
		public int BeginIndex { get; private set; }
		//ルビがつく本文の末尾文字インデックス
		public int EndIndex { get; private set; }

		//傍点かどうか
		public bool IsEmphasis { get; private set; }

		//再度ルビを作成する場合の文字間
		public float RemakeCspace { get; set; }

		internal TextMeshProRubyInfo(string ruby, int index, bool isEmphasis)
		{
			this.Ruby = ruby;
			this.BeginIndex = index;
			this.IsEmphasis = isEmphasis;
		}
		internal TextMeshProRubyInfo(string ruby, int index, int endIndex)
		{
			this.Ruby = ruby;
			this.BeginIndex = index;
			this.EndIndex = endIndex;
		}

		internal void SetEndIndex(int index)
		{
			EndIndex = index;
		}
	}
}
