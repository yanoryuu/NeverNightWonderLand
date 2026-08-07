// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.IO;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 圧縮・暗号化などの符号化つきでファイルの読み書き処理
	/// </summary>
	public abstract class FileIOManagerBase : MonoBehaviour
	{
		//4文字の識別IDをintに変換
		public static int ToMagicID( char id0, char id1, char id2, char id3 ) 
		{
			return (id3 << 24) + (id2 << 16) + (id1 << 8) + (id0);
		}

		public static string ApplicationPersistentDataPath
		{
			get
			{
#if UTAGE_DISABLE_APP_PATH
				//Application.persistentDataPathにアクセスするとクラッシュするプラットフォームの回避策
				return "";
#else
				return Application.persistentDataPath;
#endif
			}
		}

		public static string ApplicationTemporaryCachePath
		{
			get
			{
#if UTAGE_DISABLE_APP_PATH
				//Application.temporaryCachePathにアクセスするとクラッシュするプラットフォームの回避策
				return "";
#else
				return Application.temporaryCachePath;
#endif
			}
		}


		//SDKに固有の永続的なデータディレクトリ名（セーブデータとかはこっちを使う）
		public static string SdkPersistentDataPath { get { return FilePathUtil.Combine(ApplicationPersistentDataPath, sdkDirectoryName); } }

		//SDKに固有のキャッシュディレクトリ名（DLしたテクスチャファイルとかはこっちを使う）
		public static string SdkTemporaryCachePath { get { return FilePathUtil.Combine(ApplicationTemporaryCachePath, sdkDirectoryName); } }

		//SDKに固有のディレクトリ名
		const string sdkDirectoryName = "Utage/";

		/// <summary>
		/// エンコード処理（独自に設定したい場合は上書きする）
		/// </summary>
		public static Func<byte[], byte[], byte[]> CustomEncode { get { return customEncode; } set { customEncode = value; } }
		[StaticField] static Func<byte[], byte[], byte[]> customEncode = DefaultEncode;

		/// <summary>
		/// デコード処理（独自に設定したい場合は上書きする）
		/// </summary>
		public static Func<byte[], byte[], byte[]> CustomDecode { get { return customDecode; } set { customDecode = value; } }
		[StaticField] static Func<byte[], byte[], byte[]> customDecode = DefaultDecode;

		/// <summary>
		/// 非圧縮・高速のエンコード処理（独自に設定したい場合は上書きする）
		/// </summary>
		public static Action<byte[], byte[], int, int> CustomEncodeNoCompress { get { return customEncodeNoCompress; } set { customEncodeNoCompress = value; } }
		[StaticField] static Action<byte[], byte[], int, int> customEncodeNoCompress = DefaultEncodeNoCompress;

		/// <summary>
		/// 非圧縮・高速のデコード処理（独自に設定したい場合は上書きする）
		/// </summary>
		public static Action<byte[], byte[], int, int> CustomDecodeNoCompress { get { return customDecodeNoCompress; } set { customDecodeNoCompress = value; } }
		[StaticField] static Action<byte[], byte[], int, int> customDecodeNoCompress = DefaultDecodeNoCompress;

		//デフォルトのエンコード処理
		static byte[] DefaultEncode(byte[] keyBytes, byte[] bytes)
		{
			//圧縮
			byte[] encodeBuffer = Compression.Compress(bytes);
			//暗号化
			Crypt.EncryptXor(keyBytes, encodeBuffer);
			return encodeBuffer;
		}

		//デフォルトのデコード処理
		static byte[] DefaultDecode(byte[] keyBytes, byte[] bytes)
		{
			//暗号化解除
			Crypt.DecryptXor(keyBytes, bytes);
			//解凍
			byte[] decodeBuffer = Compression.Decompress(bytes);
			return decodeBuffer;
		}

		//デフォルトのエンコード処理(非圧縮。高速)
		static void DefaultEncodeNoCompress(byte[] keyBytes, byte[] bytes, int offset, int count)
		{
			//暗号化
			Crypt.EncryptXor(keyBytes, bytes, offset, count);
		}
		//デフォルトのデコード処理(非圧縮。高速)
		static void DefaultDecodeNoCompress(byte[] keyBytes, byte[] bytes, int offset, int count)
		{
			//暗号化解除
			Crypt.DecryptXor(keyBytes, bytes, offset, count);
		}


		protected enum SoundHeader
		{
			Samples,
			Channels,
			Frequency,
			Max,
		};

		[StaticField] static protected int[] audioHeader = new int[(int)SoundHeader.Max];
		protected const int audioHeaderSize = (int)(SoundHeader.Max) * 4;
		protected const int maxWorkBufferSize = 256 * 1024;
		protected const int maxAudioWorkSize = maxWorkBufferSize / 2;
		[StaticField] static protected byte[] workBufferArray = new byte[maxWorkBufferSize];
		[StaticField] static protected short[] audioShortWorkArray = new short[maxAudioWorkSize];
		[StaticField] static protected float[] audioSamplesWorkArray = new float[maxAudioWorkSize];

		/// <summary>
		/// デコード
		/// </summary>
		/// <param name="bytes">デコードするバイト配列</param>
		/// <returns>デコード済みのバイト配列</returns>
		public abstract byte[] Decode(byte[] bytes);
		
		/// <summary>
		/// デコード（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="bytes">デコードするバイト配列（）</param>
		public abstract void DecodeNoCompress(byte[] bytes);

		/// <summary>
		/// エンコード
		/// </summary>
		/// <param name="bytes">エンコードするバイト配列</param>
		/// <returns>エンコード済みのバイト配列</returns>
		public abstract byte[] Encode(byte[] bytes);

		/// <summary>
		/// エンコード（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="bytes">エンコードするバイト配列</param>
		/// <returns>エンコード済みのバイト配列</returns>
		public abstract byte[] EncodeNoCompress (byte[] bytes);		

		/// <summary>
		/// ファイル書き込み（ある程度大きなサイズのファイルを省メモリで）
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="bytes">ファイルのバイナリ</param>
		/// <returns>成否</returns>
		public abstract bool Write(string path, byte[] bytes);

		/// <summary>
		/// 独自符号化つきバイナリ読み込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="callbackRead">バイナリ読みこみ処理のコールバック</param>
		/// <returns>成否</returns>
		public abstract bool ReadBinaryDecode(string path, Action<BinaryReader> callbackRead);

		/// <summary>
		/// 独自符号化つきバイナリ書き込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="callbackWrite">バイナリ書き込み処理のコールバック</param>
		/// <returns>成否</returns>
		public abstract bool WriteBinaryEncode(string path, Action<BinaryWriter> callbackWrite);

		/// <summary>
		/// 独自符号化つき書き込み
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="bytes">書き込むバイナリ</param>
		/// <returns>成否</returns>
		public abstract bool WriteEncode(string path, byte[] bytes);

		/// <summary>
		/// 独自符号化つき書き込み（非圧縮だけど、高速・省メモリで）
		/// </summary>
		/// <param name="path">パス</param>
		/// <param name="bytes">書き込むバイナリ</param>
		/// <returns>成否</returns>
		public abstract bool WriteEncodeNoCompress(string path, byte[] bytes);

		/// <summary>
		/// サウンドファイルの書き込み（暗号化つきサウンドファイル）（ある程度大きなサイズのファイルを省メモリで）
		/// 注*）　サウンドを符号化して読み書きするのは非常に処理速度が重くメモリも大きく使うので、非推奨。
		/// どうしても必要な場合以外は、符号化なしでIOするのを推奨
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="audioClip">書き込むサウンド</param>
		/// <returns>成否</returns>
		public abstract bool WriteSound(string path, AudioClip audioClip);
		/// <summary>
		/// /サウンドファイルの読み込み（暗号化つきサウンドファイル）（ある程度大きなサイズのファイルを省メモリで）
		/// 注*）　サウンドを符号化して読み書きするのは非常に処理速度が重くメモリも大きく使うので、非推奨。
		/// どうしても必要な場合以外は、符号化なしでIOするのを推奨
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="bytes">読み込むバイナリ</param>
		/// <returns>読み込んだサウンド</returns>
		public static AudioClip ReadAudioFromMem(string name, byte[] bytes)
		{
			return ReadAudioFromMem(name, bytes, false);
		}

		/// <summary>
		/// /サウンドファイルの読み込み（暗号化つきサウンドファイル）（ある程度大きなサイズのファイルを省メモリで）
		/// 注*）　サウンドを符号化して読み書きするのは非常に処理速度が重くメモリも大きく使うので、非推奨。
		/// どうしても必要な場合以外は、符号化なしでIOするのを推奨
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="bytes">読み込むバイナリ</param>
		/// <param name="is3D">3Dサウンドか</param>
		/// <returns>読み込んだサウンド</returns>
		public static AudioClip ReadAudioFromMem(string name, byte[] bytes, bool is3D)
		{
			//ヘッダの読み込み
			Buffer.BlockCopy(bytes, 0, audioHeader, 0, audioHeaderSize);
			AudioClip audioClip = WrapperUnityVersion.CreateAudioClip(
				name
				, audioHeader[(int)SoundHeader.Samples]
				, audioHeader[(int)SoundHeader.Channels]
				, audioHeader[(int)SoundHeader.Frequency]
				, is3D, false);

			int audioSize = audioHeader[(int)SoundHeader.Samples] * audioHeader[(int)SoundHeader.Channels];
			int offsetSamples = 0;
			int offsetBuffer = audioHeaderSize;
			while (true)
			{
				//一定のサイズずつ読み込む
				int countSample = Math.Min(audioSamplesWorkArray.Length, audioSize - offsetSamples);

				//バッファを読み込み
				Buffer.BlockCopy(bytes, offsetBuffer, audioShortWorkArray, 0, countSample * 2);
				offsetBuffer += countSample * 2;
				//サウンドのサンプリングデータに変換
				float[] audioSamplesTmpArray = (countSample == audioSamplesWorkArray.Length) ? audioSamplesWorkArray : new float[countSample];
				for (int i = 0; i < countSample; i++)
				{
					audioSamplesTmpArray[i] = 1.0f * audioShortWorkArray[i] / short.MaxValue;
				}

				//オーディオに読み込む
				audioClip.SetData(audioSamplesTmpArray, offsetSamples / audioClip.channels);

				offsetSamples += countSample;
				if (offsetSamples >= audioSize) break;
			}
			return audioClip;
		}

		/// <summary>
		/// ディレクトリを作成
		/// </summary>
		/// <param name="path">パス</param>
		public abstract void CreateDirectory(string path);

		/// <summary>
		/// ディレクトリを削除
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public abstract void DeleteDirectory(string path);

		/// <summary>
		/// ファイルがあるかチェック
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>あればtrue、なければfalse</returns>
		public abstract bool Exists(string path);

		/// <summary>
		/// ファイルを削除
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public abstract void Delete(string path);
	}
}
