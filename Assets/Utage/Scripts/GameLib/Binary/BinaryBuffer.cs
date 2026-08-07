// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UtageExtensions;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Utage
{
	//セーブデータの入出力用のインターフェース
	public interface IBinaryIO
	{
		//キー
		string SaveKey { get; }

		//書き込み
		void OnWrite(BinaryWriter writer);

		//読み込み
		void OnRead(BinaryReader reader);
	}

	//セーブデータのバイナリ処理データ
	public class BinaryBuffer : BinaryBufferGeneric<IBinaryIO>
	{
	}

	//セーブデータのバイナリ処理データ
	public class BinaryBufferGeneric<T> where T : IBinaryIO
	{
		Dictionary<string, byte[]> Buffers { get { return buffers; } }
		Dictionary<string, byte[]> buffers = new Dictionary<string, byte[]>();

		//バイナリ化して書き込み
		static public void Write(BinaryWriter writer, List<T> ioList)
		{
			BinaryBufferGeneric<T> data = new BinaryBufferGeneric<T>();
			data.MakeBuffer(ioList);
			data.Write(writer);
		}

		//バイナリ化して読み込み
		static public void Read(BinaryReader reader, List<T> ioList)
		{
			BinaryBufferGeneric<T> data = new BinaryBufferGeneric<T>();
			data.Read(reader);
			data.Overrirde(ioList);
		}
		//データがあるか
		public bool IsEmpty { get { return Buffers.Count <= 0; } }

		//データを書き込み
		public void MakeBuffer(List<T> ioList)
		{
			Buffers.Clear();
			ioList.ForEach(
				x =>
				{
					if (Buffers.ContainsKey(x.SaveKey))
					{
						Debug.LogError(string.Format("Save data Key [{0}] is already exsits. Please use another key.", x.SaveKey));
					}
					else
					{
						Profiler.BeginSample("MakeBuffer : " + x.SaveKey);
						byte[] buffer = BinaryUtil.BinaryWrite(x.OnWrite);
						Buffers.Add(x.SaveKey, buffer);
						Profiler.EndSample();
					}
				}
				);
		}


		//バッファの内容をIO側に上書きして読み込む
		public void Overrirde(List<T> ioList)
		{
			ioList.ForEach(x => Overrirde(x));
		}

		//バッファの内容をIO側に上書きして読み込む
		public void Overrirde(T io)
		{
			if (Buffers.ContainsKey(io.SaveKey))
			{
				BinaryUtil.BinaryRead(Buffers[io.SaveKey], io.OnRead);
			}
			else
			{
				Debug.LogError(string.Format("Not found Save data Key [{0}] ", io.SaveKey));
			}
		}

		//IO側の内容を、バッファに上書きして書き込む
		public void WriteBuffer(T io)
		{
			Buffers[io.SaveKey] = BinaryUtil.BinaryWrite(io.OnWrite);;
		}

		//中身をコピーした新しいインスタンスを作成
		public TClone Clone<TClone>() where TClone : BinaryBufferGeneric<T>, new()
		{
			TClone clone = new TClone();
			foreach (string key in Buffers.Keys)
			{
				clone.Buffers.Add(key, Buffers[key]);
			}
			return clone;
		}

		//データのバイナリ読み込み
		public void Read(BinaryReader reader)
		{
			Buffers.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				Buffers.Add(reader.ReadString(), reader.ReadBuffer());
			}
		}

		//データのバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Buffers.Count);
			foreach (string key in Buffers.Keys)
			{
				writer.Write(key);
				writer.WriteBuffer(Buffers[key]);
			}
		}
	}
}