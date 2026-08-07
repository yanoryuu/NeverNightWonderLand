// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// 設定データの基本クラス
	/// </summary>
	public abstract class AdvSettingBase : IAdvSetting
	{
		public List<StringGrid> GridList { get { return gridList; } }
		List<StringGrid> gridList = new List<StringGrid>();

		public virtual void ParseGrid(StringGrid grid)
		{
			GridList.Add(grid);
			grid.InitLink();
			OnParseGrid(grid);
		}

		/// <summary>
		/// 文字列グリッドから、データ解析
		/// </summary>
		/// <param name="grid"></param>
		protected abstract void OnParseGrid(StringGrid grid);

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		public virtual void BootInit(AdvSettingDataManager dataManager)
		{
		}

		/// <summary>
		/// 全てのリソースをダウンロード
		/// </summary>
		public virtual void DownloadAll()
		{
		}
	}
}
