// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Utage
{
	//リップシンクのデータ
	public class AdvEyeBlinkData : AdvSettingDictinoayItemBase
	{
		//瞬きと瞬きの間の時間
		//MinとMaxの間の秒数でランダムで決まる
		public float IntervalMin { get { return intervalMin; } set { intervalMin = value; } }
		[SerializeField]
		float intervalMin = 2;

		public float IntervalMax { get { return intervalMax; } set { intervalMax = value; } }
		[SerializeField]
		float intervalMax = 6f;

		//二連続瞬きする確率（0～1）
		public float RandomDoubleEyeBlink { get { return randomDoubleEyeBlink; } set { randomDoubleEyeBlink = value; } }
		[SerializeField]
		float randomDoubleEyeBlink = 0.2f;


		//画像切り替えに使うタグ
		public string Tag { get { return tag; } set { tag = value; } }
		[SerializeField]
		string tag = "eye";

		//アニメーションデータ
		public MiniAnimationData AnimationData { get { return animationData; } }
		[SerializeField]
		MiniAnimationData animationData = new MiniAnimationData();


		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			string key = AdvCommandParser.ParseScenarioLabel(row, AdvColumnName.Label);
			InitKey(key);
			this.IntervalMin = AdvParser.ParseCellOptional<float>(row, AdvColumnName.IntervalMin, 2);
			this.IntervalMax = AdvParser.ParseCellOptional<float>(row, AdvColumnName.IntervalMax, 6);
			this.RandomDoubleEyeBlink = AdvParser.ParseCellOptional<float>(row, AdvColumnName.RandomDouble, 0.2f);
			this.Tag = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Tag, "eye");

			int index;
			if (row.Grid.TryGetColumnIndex(AdvColumnName.Name0.QuickToString(), out index))
			{
				animationData.TryParse(row, index);
			}
			return true;
		}
	};

	/// <summary>
	/// キーフレームアニメーションの設定
	/// </summary>
	public class AdvEyeBlinkSetting : AdvSettingDataDictinoayBase<AdvEyeBlinkData>
	{
	}
}
