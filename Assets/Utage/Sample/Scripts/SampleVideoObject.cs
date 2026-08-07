using UnityEngine;
using UtageExtensions;

namespace Utage
{
	public class SampleVideoObjectPlayToggle : MonoBehaviour
	{
		AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		bool IsPlaying { get => isPlaying; set => isPlaying = value; }
		[SerializeField] bool isPlaying = true; 
		
		//サンプル
		public void SampleClick()
		{
			if (!CheckVideoObject())
			{
				//ビデオオブジェクトがないなら再生処理のみする
				IsPlaying = true;
			}
			else
			{
				//再生中なら停止、停止中なら再生
				IsPlaying = !IsPlaying;
			}
			
			//再生と停止を切り替え
			ToggleVideo();
			ToggleInput();
		}
		
		//ビデオオブジェクトがあるかチェック
		bool CheckVideoObject()
		{
			return Engine.GraphicManager.GetComponentInChildren<AdvGraphicObjectVideo>(true) != null;
		}

		//ビデオの再生、停止を切り替え
		void ToggleVideo()
		{
			//GraphicManager以下のAdvGraphicObjectVideoコンポーネントを全て取得
			foreach (var advGraphicObjectVideo in
			         Engine.GraphicManager.GetComponentsInChildren<AdvGraphicObjectVideo>(true))
			{
				var videoPlayer = advGraphicObjectVideo.VideoPlayer;

				if (IsPlaying)
				{
					videoPlayer.Play();
				}
				else
				{
					videoPlayer.Pause();
				}
			}
		}
		
		//入力の有効、無効を切り替え
		void ToggleInput()
		{
			//最背面へのクリック処理の有効、無効を切り替え
			var reciever = Engine.UiManager.GetComponentInChildren<UguiBackgroundRaycastReciever>(true);
			if (reciever!=null)
			{
				reciever.enabled = IsPlaying;
			}
			
			//ADVエンジンの入力更新（文字送り）の有効、無効を切り替え
			Engine.UiManager.enabled = IsPlaying;
		}
	}
}
