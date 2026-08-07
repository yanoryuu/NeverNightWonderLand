// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Utage
{
	//リップシンクのデータ
	public class AdvLipSynchData : AdvSettingDictinoayItemBase
	{
		public LipSynchType Type { get { return type; } set { type = value; } }
		[SerializeField]
		LipSynchType type = LipSynchType.TextAndVoice;

		//インターバル時間
		public float Interval { get { return interval; } set { interval = value; } }
		[SerializeField]
		float interval = 0.2f;

		//インターバル時間
		public float ScaleVoiceVolume { get { return scaleVoiceVolume; } set { scaleVoiceVolume = value; } }
		[SerializeField]
		float scaleVoiceVolume = 1;

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
			this.Type = AdvParser.ParseCellOptional<LipSynchType>(row, AdvColumnName.Type, LipSynchType.TextAndVoice);
			this.Interval = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Interval, 0.2f);
			this.ScaleVoiceVolume = AdvParser.ParseCellOptional<int>(row, AdvColumnName.ScaleVoiceVolume, 1);
			this.Tag = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Tag, "lip");

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
	public class AdvLipSynchSetting : AdvSettingDataDictinoayBase<AdvLipSynchData>
	{
	}
}
