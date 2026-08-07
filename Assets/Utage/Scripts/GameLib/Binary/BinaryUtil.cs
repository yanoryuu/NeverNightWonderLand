// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Utage
{

	/// <summary>
	/// バイナリへの読み書き用の便利クラス 
	/// </summary>
	public class BinaryUtil
	{
		/// <summary>
		/// Stringを変換してバイナリ読み込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static void BinaryReadFromString(string str, Action<BinaryReader> onRead)
		{
			BinaryRead( System.Convert.FromBase64String(str), onRead);
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static void BinaryRead(byte[] bytes, Action<BinaryReader> onRead)
		{
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				//バイナリ化
				using (BinaryReader reader = new BinaryReader(stream))
				{
					onRead(reader);
				}
			}
		}


		/// <summary>
		/// バイナリ書き込みをStringに変換
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static string BinaryWriteToString(Action<BinaryWriter> onWrite)
		{
			return System.Convert.ToBase64String(BinaryWrite(onWrite));
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static byte[] BinaryWrite(Action<BinaryWriter> onWrite)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				//バイナリ化
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					onWrite(writer);
				}
				return stream.ToArray();
			}
		}
	}
}
