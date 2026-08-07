// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{
	// 演出の強制スキップコマンド
	public class AdvCommandSkipEffect : AdvCommand
	{
		//スキップタイプ
		enum SkipEffectType
		{
			All,
			NoWait,
		}

		private SkipEffectType SkipType { get; }
		private bool SkipLoop { get; }
		public AdvCommandSkipEffect(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			SkipType = ParseCellOptional(AdvColumnName.Arg1,SkipEffectType.All);
			SkipLoop = ParseCellOptional(AdvColumnName.Arg2,false);			
		}


		public override void DoCommand(AdvEngine engine)
		{
			switch (SkipType)
			{
				case SkipEffectType.All:
					CurrentTread.WaitManager.ForceSkipAllEffect(SkipLoop);
					break;
				case SkipEffectType.NoWait:
					CurrentTread.WaitManager.ForceSkipNoWaitEffect(SkipLoop);
					break;
			}
		}

	}
}
