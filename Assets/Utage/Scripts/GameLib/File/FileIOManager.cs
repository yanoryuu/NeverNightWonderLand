// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.IO;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 圧縮・暗号化などの符号化つきでファイルの読み書き処理
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/FileIOManager")]
	public class FileIOManager : FileIOManagerBase
	{
		/// <summary>
		/// ファイルの暗号化のキー
		/// </summary>
		public byte[] CryptKeyBytes
		{
			get
			{
				if (cryptKeyBytes == null || cryptKeyBytes.Length == 0)
				{
					cryptKeyBytes = System.Text.Encoding.UTF8.GetBytes(cryptKey);
				}
				return cryptKeyBytes;
			}
		}
		byte[] cryptKeyBytes;
		[SerializeField]
		string cryptKey = "InputOriginalKey";
#if UNITY_EDITOR
		public void SetCryptKey(string cryptKey)
		{
			this.cryptKey = cryptKey;
			cryptKeyBytes = System.Text.Encoding.UTF8.GetBytes(cryptKey);
		}
#endif
		void OnValidate()
		{
			cryptKeyBytes = System.Text.Encoding.UTF8.GetBytes(cryptKey);
		}


		/// <summary>
		/// デコード
		/// </summary>
		/// <param name="bytes">デコードするバイト配列</param>
		/// <returns>デコード済みのバイト配列</returns>
		public override byte[] Decode(byte[] bytes)
		{
			return CustomDecode(CryptKeyBytes, bytes);
		}


		/// <summary>
		/// デコード（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="bytes">デコードするバイト配列（）</param>
		public override void DecodeNoCompress(byte[] bytes)
		{
			CustomDecodeNoCompress(CryptKeyBytes, bytes, 0, bytes.Length);
		}

		/// <summary>
		/// エンコード
		/// </summary>
		/// <param name="bytes">エンコードするバイト配列</param>
		/// <returns>エンコード済みのバイト配列</returns>
		public override byte[] Encode(byte[] bytes)
		{
			return CustomEncode(CryptKeyBytes, bytes);
		}

		/// <summary>
		/// エンコード（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="bytes">エンコードするバイト配列</param>
		/// <returns>エンコード済みのバイト配列</returns>
		public override byte[] EncodeNoCompress (byte[] bytes)
		{
			CustomEncodeNoCompress(CryptKeyBytes, bytes, 0, bytes.Length);
			return bytes;
		}


		/// <summary>
		/// 独自符号化つきバイナリ読み込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public virtual byte[] ReadBytes(string path)
		{
			//ファイル読み込み
			return Decode(FileReadAllBytes(path));
		}

		/// <summary>
		/// ファイル書き込み（ある程度大きなサイズのファイルを省メモリで）
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="bytes">ファイルのバイナリ</param>
		/// <returns>成否</returns>
		public override bool Write(string path, byte[] bytes)
		{
			try
			{
				using (FileStream fstream = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					int offset = 0;
					//一定のサイズずつ書き込む
					while (true)
					{
						int count = Math.Min(maxWorkBufferSize, bytes.Length - offset);
						fstream.Write(bytes, offset, count);
						offset += count;
						if (offset >= bytes.Length) break;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
				return false;
			}
		}

		/// <summary>
		/// 独自符号化つきバイナリ読み込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="callbackRead">バイナリ読みこみ処理のコールバック</param>
		/// <returns>成否</returns>
		public override bool ReadBinaryDecode(string path, Action<BinaryReader> callbackRead)
		{
			try
			{
				if (!Exists(path)) return false;
				//ファイル読み込み
				byte[] bytes = CustomDecode(CryptKeyBytes, FileReadAllBytes(path));
				//各パラメーター読み込み
				using (MemoryStream stream = new MemoryStream(bytes))
				{
					using (BinaryReader reader = new BinaryReader(stream))
					{
						callbackRead(reader);
					}
				}
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileRead, path, e.ToString()));
				return false;
			}
		}

		/// <summary>
		/// 独自符号化つきバイナリ書き込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="callbackWrite">バイナリ書き込み処理のコールバック</param>
		/// <returns>成否</returns>
		public override bool WriteBinaryEncode(string path, Action<BinaryWriter> callbackWrite)
		{
			try
			{
				using (MemoryStream stream = new MemoryStream())
				{
					//バイナリ化
					using (BinaryWriter writer = new BinaryWriter(stream))
					{
						callbackWrite(writer);
					}
					//ファイル書き込み
					FileWriteAllBytes(path, CustomEncode(CryptKeyBytes, stream.ToArray()));
				}
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileWrite, path, e.ToString()));
				return false;
			}
		}

		/// <summary>
		/// 独自符号化つき書き込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="bytes">書き込むバイナリ</param>
		/// <returns>成否</returns>
		public override bool WriteEncode(string path, byte[] bytes)
		{
			try
			{
				//ファイル書き込み
				FileWriteAllBytes(path, CustomEncode(CryptKeyBytes, bytes));
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileWrite, path, e.ToString()));
				return false;
			}
		}

		/// <summary>
		/// 独自符号化つき書き込み（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="path">パス</param>
		/// <param name="bytes">書き込むバイナリ</param>
		/// <returns>成否</returns>
		public override bool WriteEncodeNoCompress(string path, byte[] bytes)
		{
			try
			{
				using (FileStream fstream = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					int offset = 0;
					while (true)
					{
						//一定のサイズずつ書き込む
						int count = Math.Min(maxWorkBufferSize, bytes.Length - offset);
						//暗号化
						Buffer.BlockCopy(bytes, offset, workBufferArray, 0, count);
						CustomEncodeNoCompress(CryptKeyBytes, workBufferArray, 0, count);
						//書き込む
						fstream.Write(workBufferArray, 0, count);
						offset += count;
						if (offset >= bytes.Length) break;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
				return false;
			}
		}

		/// <summary>
		/// サウンドファイルの書き込み（暗号化つきサウンドファイル）（ある程度大きなサイズのファイルを省メモリで）
		/// 注*）　サウンドを符号化して読み書きするのは非常に処理速度が重くメモリも大きく使うので、非推奨。
		/// どうしても必要な場合以外は、符号化なしでIOするのを推奨
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="audioClip">書き込むサウンド</param>
		/// <returns>成否</returns>
		public override bool WriteSound(string path, AudioClip audioClip)
		{
			try
			{
				audioHeader[(int)SoundHeader.Samples] = audioClip.samples;
				audioHeader[(int)SoundHeader.Frequency] = audioClip.frequency;
				audioHeader[(int)SoundHeader.Channels] = audioClip.channels;

				int audioSize = audioClip.samples * audioClip.channels;
				using (FileStream fstream = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					//ヘッダ書き込み
					Buffer.BlockCopy(audioHeader, 0, workBufferArray, 0, audioHeaderSize);
					CustomEncodeNoCompress(CryptKeyBytes, workBufferArray, 0, audioHeaderSize);
					fstream.Write(workBufferArray, 0, audioHeaderSize);

					int offsetSamples = 0;
					while (true)
					{
						//一定のサイズずつ書き込む
						int countSample = Math.Min(audioSamplesWorkArray.Length, audioSize - offsetSamples);

						audioClip.GetData(audioSamplesWorkArray, offsetSamples / audioClip.channels);

						//サウンドのサンプリングデータをバッファに変換
						for (int i = 0; i < countSample; i++)
						{
							audioShortWorkArray[i] = (short)(short.MaxValue * audioSamplesWorkArray[i]);
						}
						int count = countSample * 2;
						Buffer.BlockCopy(audioShortWorkArray, 0, workBufferArray, 0, count);

						//暗号化
						CustomEncodeNoCompress(CryptKeyBytes, workBufferArray, 0, count);
						//書き込む
						fstream.Write(workBufferArray, 0, count);
						offsetSamples += countSample;
						if (offsetSamples >= audioSize) break;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
				return false;
			}
		}

#if !(UNITY_WEBPLAYER || UNITY_WEBGL || UTAGE_DISABLE_FILEIO)
		/// <summary>
		/// ディレクトリを作成
		/// </summary>
		/// <param name="path">パス</param>
		public override void CreateDirectory(string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
		}
		/// <summary>
		/// ディレクトリを削除
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public override void DeleteDirectory(string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir,true);
			}
		}

		/// <summary>
		/// ファイルがあるかチェック
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>あればtrue、なければfalse</returns>
		public override bool Exists(string path)
		{
			return File.Exists(path);
		}

		protected virtual byte[] FileReadAllBytes(string path)
		{
			return File.ReadAllBytes(path);
		}

		protected virtual void FileWriteAllBytes(string path, byte[] bytes)
		{
			File.WriteAllBytes(path, bytes);
		}

		/// <summary>
		/// ファイルを削除
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public override void Delete(string path)
		{
			File.Delete(path);
		}

#else
		/// <summary>
		/// ディレクトリを作成(Webplayerでは必要ない)
		/// </summary>
		/// <param name="path">パス</param>
		public override void CreateDirectory( string path ){	
		}

		/// <summary>
		/// ディレクトリを削除
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public override void DeleteDirectory(string path)
		{
		}

		/// <summary>
		/// ファイルがあるかチェック(WebplayerではPlayerPrefsを使う)
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>あればtrue、なければfalse</returns>
		public override bool Exists( string path ){	
			return PlayerPrefs.HasKey(path);
		}

		//ファイル読み込み(WebplayerではPlayerPrefsを使う)
		protected virtual byte[] FileReadAllBytes(string path)
		{
			string str = PlayerPrefs.GetString(path);
			return System.Convert.FromBase64String( str );
		}

		//ファイル書き込み(WebplayerではPlayerPrefsを使う)
		protected virtual void FileWriteAllBytes(string path, byte[] bytes)
		{
			string str = System.Convert.ToBase64String(bytes);
			PlayerPrefs.SetString(path, str);
			PlayerPrefs.Save();
		}
		
		/// <summary>
		/// ファイルを削除(WebplayerではPlayerPrefsを使う)
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public override void Delete(string path)
		{
			PlayerPrefs.DeleteKey(path);
		}
#endif
	}
}