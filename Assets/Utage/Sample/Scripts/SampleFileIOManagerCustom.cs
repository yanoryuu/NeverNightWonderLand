// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.IO;
using UnityEngine;
using Utage;

namespace Utage
{

	/// <summary>
	/// 主にセーブデータなどに使う、ファイルの読み書きをカスタムするときのサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleFileIOManagerCustom")]
	public class SampleFileIOManagerCustom : FileIOManager
	{
		/// ディレクトリを作成
		public override void CreateDirectory( string path ){	
		}

		/// ディレクトリを削除
		public override void DeleteDirectory(string path)
		{
		}

		/// <summary>
		/// ファイルがあるかチェック
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>あればtrue、なければfalse</returns>
		public override bool Exists( string path ){
			Debug.Log("Custom File Check");
//			return PlayerPrefs.HasKey(path);
			return false;
		}

		//ファイル読み込み
		protected override byte[] FileReadAllBytes(string path)
		{
			Debug.Log("Custom FileRead");
//			string str = PlayerPrefs.GetString(path);
			string str = "";
			return System.Convert.FromBase64String( str );
		}

		//ファイル書き込み
		protected override void FileWriteAllBytes(string path, byte[] bytes)
		{
			System.Convert.ToBase64String(bytes);
			Debug.Log("Custom File Write");
			//string str = System.Convert.ToBase64String(bytes);
			//PlayerPrefs.SetString(path, str);
			//PlayerPrefs.Save();

		}

		/// ファイルを削除
		public override void Delete(string path)
		{
			Debug.Log("Custom File Delete");
			//PlayerPrefs.DeleteKey(path);
		}
	}
}
