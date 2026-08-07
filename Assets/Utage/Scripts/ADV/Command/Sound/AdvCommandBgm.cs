// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：BGM再生
	/// </summary>
	public class AdvCommandBgm : AdvCommand
	{
		public AdvCommandBgm(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			string label = ParseCell<string>(AdvColumnName.Arg1);
			if (!dataManager.SoundSetting.Contains(label, SoundType.Bgm))
			{
				Debug.LogError(ToErrorString(label + " is not contained in file setting"));
			}

			this.file = AddLoadFile(dataManager.SoundSetting.LabelToFilePath(label, SoundType.Bgm), dataManager.SoundSetting.FindData(label));
			this.isLoop = ParseCellOptional<bool>(AdvColumnName.Arg2, true);
			this.volume = ParseCellOptional<float>(AdvColumnName.Arg3, 1.0f);
			this.fadeOutTime = ParseCellOptional<float>(AdvColumnName.Arg5,0.2f);
			this.fadeInTime = ParseCellOptional<float>(AdvColumnName.Arg6,0);
		}
		public override void DoCommand(AdvEngine engine)
		{
			engine.SoundManager.PlayBgm(file, volume, isLoop, fadeInTime, fadeOutTime);
		}
		protected AssetFile file;
		protected bool isLoop;
		protected float volume;
		protected float fadeInTime;
		protected float fadeOutTime;
	}
}
