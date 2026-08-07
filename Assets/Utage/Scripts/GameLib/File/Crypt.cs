// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// 暗号化関連
	/// </summary>
	public class Crypt
	{

		/// <summary>
		/// XORで暗号化
		/// </summary>
		/// <param name="key">暗号キー</param>
		/// <param name="buffer">暗号化するバイト配列（この配列を上書きして書き換えるので注意）</param>
		public static void EncryptXor(byte[] key, byte[] buffer)
		{
			EncryptXor(key, buffer, 0, buffer.Length);
		}

		/// <summary>
		/// XORで暗号化
		/// </summary>
		/// <param name="key">暗号キー</param>
		/// <param name="buffer">暗号化するバイト配列（この配列を上書きして書き換えるので注意）</param>
		/// <param name="offset">暗号化範囲のバイト配列の先頭インデックス</param>
		/// <param name="count">暗号化するバイト長</param>
		public static void EncryptXor(byte[] key, byte[] buffer, int offset, int count)
		{
			if (key == null || key.Length <= 0) return;

			int keyMax = key.Length;
			for (int i = offset; i < (offset + count); i++)
			{
				if (buffer[i] != 0)
				{			//0をXORすると、キーが丸見えになるので回避。
					byte keyByte = key[i % keyMax];
					buffer[i] ^= keyByte;
					if (buffer[i] == 0)
					{
						buffer[i] = keyByte;
					}
				}
			}
		}

		/// <summary>
		/// XORで復号化
		/// </summary>
		/// <param name="key">復号キー</param>
		/// <param name="buffer">復号化するバイト配列（この配列を上書きして書き換えるので注意）</param>
		public static void DecryptXor(byte[] key, byte[] buffer )
		{
			//暗号化解除
			DecryptXor(key, buffer, 0, buffer.Length);
		}

		/// <summary>
		/// XORで復号化
		/// </summary>
		/// <param name="key">復号キー</param>
		/// <param name="buffer">復号化するバイト配列（この配列を上書きして書き換えるので注意）</param>
		/// <param name="offset">復号化範囲のバイト配列の先頭インデックス</param>
		/// <param name="count">復号化するバイト長</param>
		public static void DecryptXor(byte[] key, byte[] buffer, int offset, int count)
		{
			if (key == null || key.Length <= 0) return;

			int keyMax = key.Length;
			for (int i = offset; i < (offset + count); i++)
			{
				byte keyByte = key[i % keyMax];
				if (buffer[i] != 0 && buffer[i] != keyByte)
				{			//0をXORすると、キーが丸見えになるので回避。
					buffer[i] ^= keyByte;
				}
			}
		}
	}

}