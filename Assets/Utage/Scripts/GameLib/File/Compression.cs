// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 圧縮処理
	/// 「ゲームプログラマになる前に覚えておきたい技術」 著 平山尚氏
	/// のコードを元にC#用に改変してある
	/// </summary>
	public class Compression
	{

		/// <summary>
		/// 圧縮
		/// </summary>
		/// <param name="bytes">圧縮するバイト配列</param>
		/// <returns>圧縮後のバイト配列</returns>
		public static byte[] Compress(byte[] bytes)
		{
			int iSize = bytes.Length;
			byte[] headerBuff = System.BitConverter.GetBytes(iSize);
			int headerSize = headerBuff.Length;

			int worstSize = iSize + (iSize / 128) + 1; //最悪サイズ計算。これだけ確保。+1は小数点以下切り上げ分。
			byte[] work = new byte[worstSize];
			int size;
			Compress(work, out size, bytes);

			//最初4バイトを展開後のファイルサイズとして記録
			byte[] buffer = new byte[size + 4];
			System.Buffer.BlockCopy(headerBuff, 0, buffer, 0, headerSize);
			System.Buffer.BlockCopy(work, 0, buffer, headerSize, size);
			return buffer;
		}

		/// <summary>
		/// 展開
		/// </summary>
		/// <param name="bytes">展開するバイト配列</param>
		/// <returns>展開後のバイト配列</returns>
		public static byte[] Decompress(byte[] bytes)
		{
			//最初4バイトは展開後のファイルサイズ
			int size = System.BitConverter.ToInt32(bytes, 0);
			byte[] work = new byte[size];
			Decompress(work, out size, bytes);
			return work;
		}

		//ビット割り当て定数
		const int DIC_BITS = 11; //位置bit数
		//以下自動計算定数
		const int LENGTH_BITS = 16 - 1 - DIC_BITS; //長さ
		const int DIC_MASK = (1 << DIC_BITS) - 1;
		const int DIC_MASK_HIGH = (int)(DIC_MASK & 0xffffff00); //下8bitをつぶしたもの
		const int DIC_MASK_SHIFTED = (DIC_MASK >> 8) << LENGTH_BITS;
		const int LENGTH_MASK = (1 << LENGTH_BITS) - 1;
		const int DIC_SIZE = DIC_MASK + 1; //辞書サイズ(1引いて格納する関係で1多く使える)
		const int MAX_LENGTH = (LENGTH_MASK + 3); //最大一致長(3引いて格納する関係で3多く使える)

		//よく使う最小値と最大値
		static int min(int a, int b)
		{
			return (a < b) ? a : b;
		}

		static int max(int a, int b)
		{
			return (a > b) ? a : b;
		}

		/*
		高速化手法は、特許に引っかかりそうもないくらい単純なしくみに抑えたが、それでも結構効く。
		辞書について、「aはここ」「bはここ」というような表を作る。
		0-255までで256種類の文字について、それぞれ双方向リストを作っておく。
		探す文字列がabcならaから始まるので、aがある場所をリストから探して、
		そこから検索を始める。aでない場所は無視するため、文字が散らばっていれば相当速くなる。

		下のNode,Indexクラスはそのための表を表現するクラスで、
		リストの各エントリがNode、Nodeを管理する表全体がIndexクラスとなる。
		NodeがもっているmPosが、その文字がある場所を表す。場所はファイル全体の中での位置で、
		辞書内での位置ではない。
		*/
		class Node
		{
			public int mNext;
			public int mPrev;
			public int mPos; //ファイル内の位置
		};

		class Index
		{ //DIC_SIZE番までが使用中ノード。その後ろ256個はダミーヘッド。DIC_SIZE+cというのは、cという文字のリストのヘッド、の意味。
			Node[] mNodes = new Node[DIC_SIZE + 256];
			int[] mStack = new int[DIC_SIZE];
			int mStackPos;

			public Index()
			{
				//ダミーヘッドの初期化。next,prevは自分を指すように初期化
				for (int i = 0; i < (DIC_SIZE + 256); ++i)
				{
					mNodes[i] = new Node();
				}
				//ダミーヘッドの初期化。next,prevは自分を指すように初期化
				for (int i = DIC_SIZE; i < (DIC_SIZE + 256); ++i)
				{
					mNodes[i].mNext = mNodes[i].mPrev = i;
				}
				//空いている場所を記録したスタックの初期化。DIC_SIZEまで。
				for (int i = 0; i < DIC_SIZE; ++i)
				{
					mStack[i] = i;
				}
				mStackPos = DIC_SIZE;
			}
			public int getFirst(byte c)
			{
				return mNodes[DIC_SIZE + c].mNext;
			}
			public Node getNode(int i)
			{
				return mNodes[i];
			}
			//先頭に足す。新しいものほど早く検索対象になる。
			public void add(byte c, int pos)
			{
				--mStackPos;
				int newIdx = mStack[mStackPos];
				Node newNode = mNodes[newIdx];
				Node head = mNodes[DIC_SIZE + c];
				newNode.mNext = head.mNext;
				newNode.mPrev = DIC_SIZE + c;
				newNode.mPos = pos;
				mNodes[head.mNext].mPrev = newIdx;
				head.mNext = newIdx;
			}
			//末尾から探して消す。消すのは一番最初にaddしたものなので、絶対に末尾にある。
			//つまり、リストをいちいち回す必要はない。
			public void remove(byte c, int pos)
			{
				int idx = mNodes[DIC_SIZE + c].mPrev;
				Node n = mNodes[idx];
				if (n.mPos != pos)
				{
					//絶対にここにあるはずで、この条件が満たされなければバグだ。
					Debug.LogError("n.mPos != pos");
				}
				mStack[mStackPos] = mNodes[n.mPrev].mNext; //削除
				++mStackPos;
				mNodes[n.mPrev].mNext = n.mNext;
				mNodes[n.mNext].mPrev = n.mPrev;
			}
			public bool isEnd(int idx)
			{ //ダミーヘッドかどうかを返す。DIC_SIZE以上ならダミー。
				return (idx >= DIC_SIZE);
			}
		};


		/*
		辞書圧縮。LZ77。

		圧縮領域は2バイトで、位置と長さに分配してある。
		1バイト目 : 0x80+サイズ+位置の上位ビット、
		2バイト目 : 位置の下位8bit。
		非圧縮領域は、非圧縮領域サイズ、非圧縮文字列(1から128)文字

		圧縮領域は、位置、長さ共に-3した数を格納してある。
		展開時は取り出してから3を足す。
		非圧縮領域は長さを-1してあり、取り出す時には+1する。
		*/
		static void Compress(
			byte[] oData,
			out int oSize,
			byte[] iData)
		{

			int iSize = iData.Length;

			int oPos = 0; //書き込み側の書き込む位置
			int i = 0;
			int unmatchBegin = 0; //非一致領域の開始位置
			Index index = new Index();
			while (i < iSize)
			{
				//辞書から検索
				int matchLength = 0;
				int matchPos = 0;
				//辞書の先頭から探していく。jはiを越えない。
				//最大検索長
				int maxL = min(MAX_LENGTH, iSize - i); //ファイル末尾より後は検索できないので、maxLを制限する。

				//検索開始 一文字目を探す
				int idx = index.getFirst(iData[i]);
				while (!index.isEnd(idx))
				{
					Node n = index.getNode(idx);
					int p = n.mPos;
					//一致長を調べる
					int l = 1; //1文字一致状態から始める。
					while (l < maxL)
					{
						//次の文字がマッチしなければ終わる
						if (iData[p + l] != iData[i + l])
						{
							break;
						}
						++l; //1文字成長
					}
					//前より長く一致したなら記録。いろんなマッチの仕方があるはずだから、最大のものを記録する。
					if (matchLength < l)
					{
						matchPos = p;
						matchLength = l;
						if (matchLength == maxL)
						{ //一致長が最大になったらそこで終わる。
							break;
						}
					}
					idx = n.mNext;
				}
				//さて、一致が3文字以上あれば圧縮モードで記録する。
				if (matchLength >= 3)
				{
					//辞書更新。消して、足す。進んだ文字数だけ削除して、進んだ文字数だけ足す。
					for (int j = 0; j < matchLength; ++j)
					{
						int delPos = i + j - DIC_SIZE;
						if (delPos >= 0)
						{
							index.remove(iData[delPos], delPos);
						}
						index.add(iData[i + j], i + j);
					}
					//非圧縮ヘッダ書き込み
					if (unmatchBegin < i)
					{
						oData[oPos] = (byte)(i - unmatchBegin - 1); //最低1なので1引いて保存
						++oPos;
						for (int j = unmatchBegin; j < i; ++j)
						{
							oData[oPos] = iData[j];
							++oPos;
						}
					}
					//圧縮部分を記録
					int wl = matchLength - 3; //3引いて格納
					int wp = i - matchPos - 1; //1引いて格納
					int tmp = 0x80 | wl; //長さに圧縮フラグを追加
					tmp |= (wp & DIC_MASK_HIGH) >> (8 - LENGTH_BITS); //maskと&し、これをサイズに使っているビットの分だけずらす。
					oData[oPos + 0] = (byte)(tmp);
					oData[oPos + 1] = (byte)(wp & 0xff);
					oPos += 2;
					i += matchLength;
					unmatchBegin = i; //非一致位置は次から
				}
				else
				{ //マッチしなかった。書き込みはまとめてやるので、今は進める。
					//辞書更新。消して、足す。
					int delPos = i - DIC_SIZE;
					if (delPos >= 0)
					{
						index.remove(iData[delPos], delPos);
					}
					index.add(iData[i], i);
					++i;
					if (i - unmatchBegin == 128)
					{ //限界数溜まってしまった。書き込む
						//非圧縮ヘッダ書き込み
						oData[oPos] = (byte)(i - unmatchBegin - 1); //最低1なので1引いて保存
						++oPos;
						for (int j = unmatchBegin; j < i; ++j)
						{
							oData[oPos] = iData[j];
							++oPos;
						}
						unmatchBegin = i;
					}
				}
			}

			//非一致位置が残っていれば最後の書き込み
			if (unmatchBegin < i)
			{
				//非圧縮ヘッダ書き込み
				oData[oPos] = (byte)(i - unmatchBegin - 1); //最低1なので1引いて保存
				++oPos;
				for (int j = unmatchBegin; j < i; ++j)
				{
					oData[oPos] = iData[j];
					++oPos;
				}
			}
			oSize = oPos;
		}

		//展開はとっても簡単です。
		static void Decompress(byte[] oData, out int oSize, byte[] iData)
		{
			int oPos = 0;
			int iSize = iData.Length;

			//最初4バイトは展開後のファイルサイズ
			for (int i = 4; i < iSize; ++i)
			{
				int length;
				if ((iData[i] & 0x80) != 0)
				{ //圧縮モード
					length = iData[i] & LENGTH_MASK;
					length += 3; //3文字少なく記録されている。
					int position = ((iData[i] & DIC_MASK_SHIFTED) << (8 - LENGTH_BITS)) | iData[i + 1]; //複雑なのでよく分解して考えよう
					position += 1; //1文字少なく記録されている。
					for (int j = 0; j < length; ++j)
					{
						oData[oPos + j] = oData[oPos - position + j]; //outから移すのが気持ち悪いかもしれないが、すでに書いた部分のoutは辞書なのである。
					}
					i += 1; //1バイト余分に進めてやる。
				}
				else
				{ //非圧縮モード
					length = iData[i] + 1; //1文字少なく記録してある
					for (int j = 0; j < length; ++j)
					{
						oData[oPos + j] = iData[i + 1 + j];
					}
					i += length; //ほうっておいても1は足される。生データlength分だけ進め、その前の1バイトは自然にまかせよう
				}
				oPos += length;
			}
			oSize = oPos;
		}
	};
}