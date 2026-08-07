using UnityEngine;

namespace Utage
{
	public class InputManagerStrategy : IInputStrategy
	{
		//マウス座標を取得
		public virtual Vector3 GetMousePosition()
		{
			return Input.mousePosition;
		}

		//GUIを閉じる入力があったか
		//デフォルトでは右クリック
		public virtual bool IsInputGuiClose()
		{
			if (UtageToolKit.IsPlatformStandAloneOrEditor() || InputUtil.EnableWebGLInput())
			{
				return Input.GetMouseButtonDown(1);
			}
			else
			{
				return false;
			}
		}

		//強制スキップ入力があったか
		//デフォルトでは、Ctrlキー
		public virtual bool IsInputForceSkip()
		{
			if (UtageToolKit.IsPlatformStandAloneOrEditor() || InputUtil.EnableWebGLInput())
			{
				return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
			}
			else
			{
				return false;
			}
		}


		protected float GetMouseWheelAxis()
		{
			return Input.GetAxis("Mouse ScrollWheel");
		}
		protected const float WheelSensitive = 0.1f;

		//バックログを開く入力があったか
		//デフォルトでは、マウスホイールを上に入力
		public virtual bool IsInputOpenBackLog()
		{
			return GetMouseWheelAxis() >= WheelSensitive;
		}
		
		//バックログを閉じる入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public virtual bool IsInputCloseBackLog()
		{
			return GetMouseWheelAxis() <= -WheelSensitive;
		}
		//マウスによる次へ読み進める入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public virtual bool IsInputNexByMouse()
		{
			return GetMouseWheelAxis() <= -WheelSensitive;
		}

		//次へ読み進める入力があったか
		//デフォルトでは、リターンキー
		public virtual bool IsInputNextButton()
		{
			if (UtageToolKit.IsPlatformStandAloneOrEditor() || InputUtil.EnableWebGLInput())
			{
				return Input.GetKeyDown(KeyCode.Return);
			}
			else
			{
				return false;
			}
		}

		//ゲーム終了ダイアログを開く入力があったか
		//デフォルトではEscapeキー
		public virtual bool IsInputExit()
		{
			return Input.GetKeyDown(KeyCode.Escape);
		}

		//デバッグ用のポーズ入力（その0）があったか
		//デフォルトでは、マウス左ボタンを押した瞬間
		public virtual bool IsInputDebugPause0()
		{
			return Input.GetMouseButtonDown(0);
		}

		//デバッグ用のポーズ入力（その1）があったか
		//デフォルトでは、マウス左ボタンが押されていたのを離した瞬間（クリック直後を想定）
		public virtual bool IsInputDebugPause1()
		{
			return Input.GetMouseButtonUp(0);
		}

	}
}
