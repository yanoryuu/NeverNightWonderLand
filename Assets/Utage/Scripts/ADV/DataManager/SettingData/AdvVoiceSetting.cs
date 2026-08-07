// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// サウンドファイル設定（ラベルとファイルの対応）
	/// </summary>
	public class AdvVoiceSetting : IAssetFileSoundSettingData
	{
		public StringGridRow RowData { get; private set; }

		/// <summary>
		/// イントロループ用のループポイント
		/// </summary>
		public float IntroTime { get { return 0; } }

		/// <summary>
		/// ボリューム
		/// </summary>
		public float Volume { get { return 1.0f; } }

		public AdvVoiceSetting(StringGridRow row)
		{
			this.RowData = row;
		}
	}
}