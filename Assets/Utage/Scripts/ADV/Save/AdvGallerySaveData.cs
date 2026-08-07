// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ギャラリー用セーブデータ
	/// </summary>
	public class AdvGallerySaveData : IBinaryIO
	{
		List<string> eventSceneLabels = new List<string>();
		List<string> eventCGLabels = new List<string>();


		/// <summary>
		/// イベントCG追加
		/// </summary>
		/// <param name="label">イベントCGラベル</param>
		public void AddCgLabel(string label)
		{
			if (!CheckCgLabel(label))
			{
				eventCGLabels.Add(label);
			}
		}

		//登録された指定のイベントCGをクリア(主にデバッグ用)
		public void RemoveCgLabel(string label)
		{
			eventCGLabels.Remove(label);
		}

		//登録されたイベントCGを全てクリア(主にデバッグ用)
		public void ClearCgLabels()
		{
			eventCGLabels.Clear();
		}

		/// <summary>
		/// 回想シーン追加
		/// </summary>
		/// <param name="label">回想シーンラベル</param>
		public void AddSceneLabel(string label)
		{
			if (!CheckSceneLabels(label))
			{
				eventSceneLabels.Add(label);
			}
		}

		//登録された指定の回想シーンラベルをクリア(主にデバッグ用)
		public void RemoveSceneLabel(string label)
		{
			eventSceneLabels.Remove(label);
		}

		//登録された回想シーンラベルを全てクリア(主にデバッグ用)
		public void ClearSceneLabels()
		{
			eventSceneLabels.Clear();
		}

		/// <summary>
		/// シーン解放チェック
		/// </summary>
		/// <param name="label">シーンラベル</param>
		/// <returns>シーンが解放されていればture。解放されていないければfalse</returns>
		public bool CheckSceneLabels(string label)
		{
			return eventSceneLabels.Contains(label);
		}

		/// <summary>
		/// イベントCG解放チェック
		/// </summary>
		/// <param name="label">イベントCGラベル</param>
		/// <returns>イベントCGが解放されていればture。解放されていないければfalse</returns>
		public bool CheckCgLabel(string label)
		{
			return eventCGLabels.Contains(label);
		}

		//データのキー
		public string SaveKey { get { return "AdvGallerySaveData"; } }

		const int VERSION = 0;

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public void OnRead(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version == VERSION)
			{
				int max;

				eventSceneLabels.Clear();
				max = reader.ReadInt32();
				for (int i = 0; i < max; ++i)
				{
					eventSceneLabels.Add(reader.ReadString());
				}

				eventCGLabels.Clear();
				max = reader.ReadInt32();
				for (int i = 0; i < max; ++i)
				{
					eventCGLabels.Add(reader.ReadString());
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(VERSION);

			writer.Write(eventSceneLabels.Count);
			foreach (string item in eventSceneLabels)
			{
				writer.Write(item);
			}

			writer.Write(eventCGLabels.Count);
			foreach (string item in eventCGLabels)
			{
				writer.Write(item);
			}
		}
	}
}