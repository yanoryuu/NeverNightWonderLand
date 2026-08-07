// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 拡張子に関する制御
	/// </summary>
	public static class ExtensionUtil
	{
//		public const string Utage = ".utage";
		
		public const string Ogg = ".ogg";
		public const string Mp3 = ".mp3";
		public const string Wav = ".wav";

		public const string Txt = ".txt";
		
		public const string CSV = ".csv";
		public const string TSV = ".tsv";

		public const string AssetBundle = ".unity3d";
		public const string UtageFile = ".utage";
		public const string ConvertFileList = ".list.bytes";
		public const string ConvertFileListLog = ".list.log";
		public const string Log = ".log";

		/// <summary>
		/// オーディオのタイプを取得
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <returns>オーディオのタイプ</returns>
		public static AudioType GetAudioType(string path)
		{
			string ext = FilePathUtil.GetExtension(path).ToLower();
			switch (ext)
			{
				case Mp3:
					return AudioType.MPEG;
				case Ogg:
					return AudioType.OGGVORBIS;
				default:
					return AudioType.WAV;
			}
		}

		/// <summary>
		/// WebPlayer、StandAloneではOggが対応。MOBILEはMP3が非対応なので、拡張子を入れ替える
		/// http://docs-jp.unity3d.com/Documentation/ScriptReference/WWW.GetAudioClip.html
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <returns>対応するサウンドの拡張子を入れ替えたファイルパス</returns>
		public static string ChangeSoundExt(string path)
		{
			string ext = FilePathUtil.GetExtension(path).ToLower();
			switch (ext)
			{
				case Ogg:
					if (!IsSupportOggPlatform())
					{
						return FilePathUtil.ChangeExtension(path, Mp3);
					}
					break;
				case Mp3:
					if (IsSupportOggPlatform())
					{
						return FilePathUtil.ChangeExtension(path, Ogg);
					}
					break;
				default:
					break;
			}
			return path;
		}

		/// <summary>
		/// Oggをサポートしているプラットフォームかどうか
		/// WebPlayer、StandAloneではOggが対応。MOBILEはMP3が対応なので、拡張子を入れ替える
		/// http://docs-jp.unity3d.com/Documentation/ScriptReference/WWW.GetAudioClip.html
		/// </summary>
		/// <returns>サポートしていればtrue</returns>
		public static bool IsSupportOggPlatform()
		{
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
			return true;
#else
			return false;
#endif
		}
	}
}
