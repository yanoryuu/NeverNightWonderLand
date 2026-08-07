#if UTAGE_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utage.InputSystem
{
    public class InputSystemStrategy : IInputStrategy
    {
	    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	    public static void RuntimeInitialize()
	    {
		    InputUtil.SetInputStrategy(new InputSystemStrategy());
	    }

	    //マウス座標を取得
		public Vector3 GetMousePosition()
		{
			if(Mouse.current==null) return Vector3.negativeInfinity;
			return Mouse.current.position.ReadValue();
		}

		//GUIを閉じる入力があったか
		//デフォルトでは右クリック
		public virtual bool IsInputGuiClose()
		{
			if (UtageToolKit.IsPlatformStandAloneOrEditor() || InputUtil.EnableWebGLInput())
			{
				if (Mouse.current == null) return false;
				return Mouse.current.rightButton.wasPressedThisFrame;
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
				if (Keyboard.current == null) return false;
				return Keyboard.current.ctrlKey.isPressed;
			}
			else
			{
				return false;
			}
		}


		protected virtual float GetMouseWheelAxis()
		{
			var mouse = Mouse.current;
			if (mouse == null) return 0;
			return mouse.scroll.ReadValue().y;
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
				if (Keyboard.current == null) return false;
				return Keyboard.current.enterKey.wasPressedThisFrame;
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
			if (Keyboard.current == null) return false;
			return Keyboard.current.escapeKey.wasPressedThisFrame;
		}


		//デバッグ用のポーズ入力（その0）があったか
		//デフォルトでは、マウス左ボタンを押した瞬間
		public virtual bool IsInputDebugPause0()
		{
			if (Mouse.current == null) return false;
			return Mouse.current.leftButton.wasPressedThisFrame;
		}

		//デバッグ用のポーズ入力（その1）があったか
		//デフォルトでは、マウス左ボタンが押されていたのを離した瞬間（クリック直後を想定）
		public virtual bool IsInputDebugPause1()
		{
			if (Mouse.current == null) return false;
			return Mouse.current.leftButton.wasReleasedThisFrame;
		}
    }
}

#endif
