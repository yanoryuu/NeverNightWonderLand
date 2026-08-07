// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// シナリオのジャンプデータ
	/// </summary>
	public class AdvScenarioJumpData
	{
		public AdvScenarioJumpData( string toLabel, StringGridRow fromRow )
		{
			ToLabel = toLabel;
			FromRow = fromRow;
		}

		/// <summary>
		/// ジャンプ先のシナリオラベル
		/// </summary>
		public string ToLabel { get; private set; }

		/// <summary>
		/// ジャンプをする元の行
		/// </summary>
		public StringGridRow FromRow { get; private set; }
	}
}
