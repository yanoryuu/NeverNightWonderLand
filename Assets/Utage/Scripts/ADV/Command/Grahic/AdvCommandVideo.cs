// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：ムービー再生(Unity5.6以降のVideoClip版)
	/// </summary>
	public class AdvCommandVideo : AdvCommand
	{
		public AdvCommandVideo(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.label = ParseCell<string>(AdvColumnName.Arg1);
			this.cameraName = ParseCell<string>(AdvColumnName.Arg2);
			this.loop = ParseCellOptional<bool>(AdvColumnName.Arg3, false);
			this.cancel = ParseCellOptional<bool>(AdvColumnName.Arg4, true);

			string path = FilePathUtil.Combine(dataManager.BootSetting.ResourceDir, "Video");
			path = FilePathUtil.Combine(path, label);
			this.file = AddLoadFile(path,new AdvCommandSetting(this));

		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.GraphicManager.VideoManager.Play(label, cameraName, file, loop, cancel);
			isEndPlay = false;
		}

		public override bool Wait(AdvEngine engine)
		{
			//1フレーム遅らせてカメラのクリア処理を挟む
			if (!isEndPlay)
			{
				if (engine.UiManager.IsInputTrig)
				{
					engine.GraphicManager.VideoManager.Cancel(label);
				}
				isEndPlay = engine.GraphicManager.VideoManager.IsEndPlay(label);
				if (isEndPlay)
				{
					engine.GraphicManager.VideoManager.Complete(label);
					Camera camera = engine.EffectManager.FindTarget(AdvEffectManager.TargetType.Camera, cameraName).GetComponentInChildren<Camera>();
					cameraClearFlags = camera.clearFlags;
					cameraClearColor = camera.backgroundColor;
					camera.clearFlags = CameraClearFlags.Color;
					camera.backgroundColor = Color.black;
				}
				return true;
			}
			else
			{
				Camera camera = engine.EffectManager.FindTarget(AdvEffectManager.TargetType.Camera, cameraName).GetComponentInChildren<Camera>();
				camera.clearFlags = cameraClearFlags;
				camera.backgroundColor = cameraClearColor;
				return false;
			}
		}

		bool isEndPlay = true;
		CameraClearFlags cameraClearFlags;
		Color cameraClearColor;

		AssetFile file;
		string label;
		bool loop;
		bool cancel;
		string cameraName;
	}
}
