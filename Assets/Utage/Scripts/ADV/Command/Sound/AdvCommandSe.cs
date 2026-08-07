// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：SE再生
	/// </summary>
	public class AdvCommandSe : AdvCommand
	{

		public AdvCommandSe(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.label = ParseCell<string>(AdvColumnName.Arg1);
			if (!dataManager.SoundSetting.Contains(label, SoundType.Se))
			{
				Debug.LogError(ToErrorString(label + " is not contained in file setting"));
			}
			this.isLoop = ParseCellOptional<bool>(AdvColumnName.Arg2, false);
			this.volume = ParseCellOptional<float>(AdvColumnName.Arg3, 1.0f);

			this.file = AddLoadFile(dataManager.SoundSetting.LabelToFilePath(label, SoundType.Se), dataManager.SoundSetting.FindData(label) );
		}

		public override void DoCommand(AdvEngine engine)
		{
			bool skip = false;
			//スキップ中のSE無効か判定
			if (engine.Page.CheckSkip() && engine.Config.SkipVoiceAndSe)
			{
				if (isLoop && engine.Config.DontSkipLoopVoiceAndSe)
				{
					//ループの場合はスキップしない
					skip = false;
				}
				else
				{
					//スキップする
					skip = true;
				}
			}
			if (!skip) 
			{
				engine.SoundManager.PlaySe(file, volume, label, SoundPlayMode.Add, isLoop);
			}
		}

		string label;
		AssetFile file;
		float volume;
		bool isLoop;
	}
}
