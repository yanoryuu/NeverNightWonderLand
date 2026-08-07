// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：背景表示・切り替え
	/// </summary>
	public class AdvCommandBgEvent : AdvCommandBgBase
	{
		//立ち絵表示をしないモードにするかどうか
		bool IsEventMode { get; }
		
		public AdvCommandBgEvent(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager)
		{
			IsEventMode = this.ParseCellOptional(AdvColumnName.Arg2, true);
		}

		public override void DoCommand(AdvEngine engine)
		{
			//CGギャラリーの解放
			var systemSaveData = engine.SystemSaveData;
			systemSaveData.GalleryData.AddCgLabel(label);
			//コマンド実行時のオートセーブが有効ならオートセーブ
			if (systemSaveData.IsAutoSaveBgEventCommand)
			{
				systemSaveData.Write();
			}
			engine.GraphicManager.IsEventMode = IsEventMode;
			
			//表示する
			AdvGraphicOperationArg graphicOperationArg = DoCommandBgSub(engine);
			//キャラクターは非表示にする
			if (IsEventMode)
			{
				engine.GraphicManager.CharacterManager.FadeOutAll(graphicOperationArg.GetSkippedFadeTime(engine));
			}
		}
	}
}
