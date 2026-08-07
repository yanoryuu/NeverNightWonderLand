// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：Se停止
	/// </summary>
	public class AdvCommandStopSe : AdvCommand
	{
		public AdvCommandStopSe(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.label = ParseCellOptional<string>(AdvColumnName.Arg1,"");

			//存在しないSEのチェック
			if (!string.IsNullOrEmpty(this.label))
			{
				if (!dataManager.SoundSetting.Contains(label, SoundType.Se))
				{
					Debug.LogError(ToErrorString(label + " is not contained in file setting"));
				}
			}
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(label))
			{
				engine.SoundManager.StopSeAll(fadeTime);
			}
			else
			{
				engine.SoundManager.StopSe(label, fadeTime);
			}
		}

		string label;
		float fadeTime;
	}
}
