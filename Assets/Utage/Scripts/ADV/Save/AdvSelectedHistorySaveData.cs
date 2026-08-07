// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ゲームで共通して使う「選択済み」のデータ
	/// </summary>
	public class AdvSelectedHistorySaveData: IBinaryIO
	{
		class AdvSelectedHistoryData
		{
			string Label { get; set; }
			string Text { get; set; }
			string JumpLabel { get; set; }
			public AdvSelectedHistoryData(AdvSelection selection)
			{
				this.Label = selection.Label;
				this.Text = selection.Text;
				this.JumpLabel = selection.JumpLabel;
			}

			public bool Check(AdvSelection selection)
			{
				if (!string.IsNullOrEmpty(this.Label) || !string.IsNullOrEmpty(selection.Label))
				{
					return this.Label == selection.Label;
				}

				return (this.Text == selection.Text ) && (this.JumpLabel == selection.JumpLabel);
			}

			/// <summary>
			/// バイナリ書き込み
			/// </summary>
			/// <param name="writer">バイナリライター</param>
			public void Write(BinaryWriter writer)
			{
				writer.Write(this.Label);
				writer.Write(this.Text);
				writer.Write(this.JumpLabel);
			}

			/// <summary>
			/// バイナリ読み込みするコンストラクタ
			/// </summary>
			/// <param name="reader">バイナリリーダー</param>
			public AdvSelectedHistoryData(BinaryReader reader,  int version)
			{
				if (version == VERSION)
				{
					this.Label = reader.ReadString();
					this.Text = reader.ReadString();
					this.JumpLabel = reader.ReadString();
				}
				else
				{
					Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				}
			}
		}

		List<AdvSelectedHistoryData> dataList = new List<AdvSelectedHistoryData>();


		const string Ignore = "Alaways";

		/// <summary>
		/// 「選択済み」のデータ追加
		/// </summary>
		public void AddData( AdvSelection selection )
		{
			//選択済みにしない
			if (selection.Label == Ignore) return;

			//既に選択済みなのでそのまま
			if (Check(selection)) return;

			dataList.Add(new AdvSelectedHistoryData(selection));
		}

		/// <summary>
		/// 既読チェック
		/// </summary>
		public bool Check(AdvSelection selection)
		{
			//選択済みにしない
			if (selection.Label == Ignore) return false;

			return dataList.Find(x => x.Check(selection)) != null;
		}

		//データのキー
		public string SaveKey{ get { return "AdvSelectedHistorySaveData"; } }

		const int VERSION = 0;
		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(VERSION);
			writer.Write(dataList.Count);
			foreach (var item in dataList)
			{
				item.Write(writer);
			}
		}
		
		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public void OnRead(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version == VERSION)
			{
				this.dataList.Clear();
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i)
				{
					this.dataList.Add(new AdvSelectedHistoryData(reader,version ) );
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

	}
}