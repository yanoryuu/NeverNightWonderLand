// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Video;
using UtageExtensions;

namespace Utage
{
	
	// コマンド：動画の再生終了待ち
	public class AdvCommandWaitVideo : AdvCommandWaitBase
		, IAdvCommandEffect
		, IAdvCommandUpdateWait
	{
		string VideoName { get; set; }
		AdvEngine Engine { get; set; }
		AdvGraphicObjectVideo VideoObject { get; set; }

		internal AdvCommandWaitVideo(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.VideoName = ParseCellOptional<string>(AdvColumnName.Arg1,"");
			WaitType = ParseCellOptional(AdvColumnName.WaitType, AdvCommandWaitType.Default);
		}

		//開始時のコールバック
		protected override void OnStart(AdvEngine engine, AdvScenarioThread thread)
		{
			Engine = engine;
			
			var targetObject = Engine.GraphicManager.FindObject(VideoName);
			if (targetObject == null)
			{
				Debug.LogError($"{VideoName} is not found");
				return;
			}

			if (!targetObject.RenderObject.TryGetComponent(out AdvGraphicObjectVideo objectVideo))
			{
				Debug.LogError($"{VideoName} is not video object", targetObject);
				return;
			}
			VideoObject = objectVideo;
			if (VideoObject.VideoPlayer.isLooping)
			{
				Debug.LogWarning($"{VideoName} is looping video",targetObject);
			}
		}

		//Wait待機のチェック
		public bool UpdateCheckWait()
		{
			if (VideoObject == null) return false;
			return VideoObject.IsPlayingOrPreparing;
		}
		
		public void OnEffectFinalize()
		{
			Engine = null;
			VideoObject = null;
		}

		public void OnEffectSkip()
		{
			VideoObject.VideoPlayer.time = VideoObject.VideoPlayer.length;
		}
	}
}
