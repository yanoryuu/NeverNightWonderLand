// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// コマンド：キャプチャ画像の作成・表示
	/// </summary>
	public class AdvCommandCaptureImage : AdvCommand
	{
		public AdvCommandCaptureImage(StringGridRow row)
			: base(row)
		{
			this.objName = ParseCell<string>(AdvColumnName.Arg1); //キャプチャ画像名
			this.cameraName = ParseCell<string>(AdvColumnName.Arg2); //キャプチャ画像名
			this.layerName = ParseCell<string>(AdvColumnName.Arg3); //キャプチャ画像名

		}

		public override void DoCommand(AdvEngine engine)
		{
			isWaiting = true;
			engine.GraphicManager.CreateCaptureImageObject(objName, cameraName, layerName);
		}

		//コマンド終了待ち(1フレーム待つ)
		public override bool Wait(AdvEngine engine)
		{
			if (!isWaiting)
			{
				return false;
			}
			isWaiting = false;
			return true;
		}

		string objName;		//オブジェクト名
		string cameraName;  //カメラ名
		string layerName;   //レイヤー名
		bool isWaiting;
	}
}
