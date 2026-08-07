// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 入力処理
	/// </summary>
	public static class InputUtil
	{
		//入力の有効・無効
		public static bool EnableInput { get { return enableInput; } set { enableInput = value; } }
		[RuntimeInitializeStaticField] static bool enableInput = true;

		//入力の処理方法
		static IInputStrategy InputStrategy
		{
			get
			{
				if (s_inputStrategy == null)
				{
					//何も設定されてないときはInputManagerを使う
					s_inputStrategy = new InputManagerStrategy();
				}
				return s_inputStrategy;
			}
		}
		[RuntimeInitializeStaticField] static IInputStrategy s_inputStrategy = null;
		
		//入力の処理方法を上書きして設定
		public static void SetInputStrategy(IInputStrategy strategy)
		{
			s_inputStrategy = strategy;
		}
		
		//WEBGLの場合、入力を有効にするか
		//UTAGE_DISABLE_WEBGL_INPUTをDefineすると無効になる
		public static bool EnableWebGLInput()
		{
#if UTAGE_DISABLE_WEBGL_INPUT
			return falase;
#else
			return (Application.platform == RuntimePlatform.WebGLPlayer);
#endif
		}

		//マウス座標を取得
		public static Vector3 GetMousePosition()
		{
			return InputStrategy.GetMousePosition();
		}

		//GUIを閉じる入力があったか（デフォルトでは右クリック）
		public static bool IsInputGuiClose()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputGuiClose();
		}

		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use " + nameof(IsInputGuiClose) + " instead")]
		public static bool IsMouseRightButtonDown()
		{
			return IsInputGuiClose();
		}
		[Obsolete("Use " + nameof(IsInputGuiClose) + " instead")]
		public static bool IsMousceRightButtonDown()
		{
			return IsInputGuiClose();
		}

		//強制スキップ入力があったか
		public static bool IsInputForceSkip()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputForceSkip();
		}
		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use "+nameof(IsInputForceSkip) + " instead")]
		public static bool IsInputControl()
		{
			return IsInputForceSkip();
		}



		//バックログを開く入力があったか
		public static bool IsInputOpenBackLog()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputOpenBackLog();
		}
		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use " + nameof(IsInputOpenBackLog) + " instead")]
		public static bool IsInputScrollWheelUp()
		{
			return IsInputOpenBackLog();
		}
		
		//バックログを閉じる入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public static bool IsInputCloseBackLog()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputCloseBackLog();
		}
		//マウススによる次へ読み進める入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public static bool IsInputNexByMouse()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputNexByMouse();
		}
		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use " + nameof(IsInputCloseBackLog) + " or " + nameof(IsInputNexByMouse) + " instead")]
		public static bool IsInputScrollWheelDown()
		{
			return IsInputNexByMouse();
		}

		//次へ読み進める入力があったか
		//デフォルトでは、リターンキー
		public static bool IsInputNextButton()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputNextButton();
		}
		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use " + nameof(IsInputNextButton) + " instead")]
		public static bool IsInputKeyboadReturnDown()
		{
			return IsInputNextButton();
		}


		//ゲーム終了ダイアログを開く入力があったか
		//デフォルトではEscapeキー
		internal static bool IsInputExit()
		{
			if (!EnableInput) return false;
			return InputStrategy.IsInputExit();
		}
		//UI入力を抽象化するため、メソッド名を変更
		[Obsolete("Use " + nameof(IsInputExit) + " instead")]
		internal static bool GetKeyDown(KeyCode keyCode)
		{
			return IsInputExit();
		}

		//デバッグ用のポーズ入力（その0）があったか
		//デフォルトでは、マウス左ボタンを押した瞬間
		public static bool IsInputDebugPause0()
		{
			return InputStrategy.IsInputDebugPause0();
		}

		//デバッグ用のポーズ入力（その1）があったか
		//デフォルトでは、マウス左ボタンが押されていたのを離した瞬間（クリック直後を想定）
		public static bool IsInputDebugPause1()
		{
			return InputStrategy.IsInputDebugPause1();
		}

	}

}
