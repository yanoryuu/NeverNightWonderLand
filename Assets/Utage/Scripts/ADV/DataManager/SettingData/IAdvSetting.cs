// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// 設定データの基本クラス
	/// </summary>
	public interface IAdvSetting
	{
		List<StringGrid> GridList { get; }
		void ParseGrid(StringGrid grid);

		//起動時の初期化
		void BootInit(AdvSettingDataManager dataManager);

		/// <summary>
		/// 全てのリソースをダウンロード
		/// </summary>
		void DownloadAll();
	};
}
